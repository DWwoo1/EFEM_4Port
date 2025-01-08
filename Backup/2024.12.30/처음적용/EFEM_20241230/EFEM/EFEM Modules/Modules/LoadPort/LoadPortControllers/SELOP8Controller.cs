using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Define.DefineEnumBase.Common;
using FrameOfSystem3.Functional;
using EFEM.Defines.Common;
using EFEM.Defines.LoadPort;

namespace EFEM.Modules.LoadPort.LoadPortControllers
{
    // 2024.11.14. by dwlim [ADD] SELOP8 LED I/F 추가
    public enum LoadPortLEDTypes
    {
        // LoadPort 상단 LED
        //None = 0,
        Load_Top = 1,
        Unload_Top = 2,
        Manual = 3,
        Auto = 4,
        Reserved = 5,
        Alarm = 6,

        // LoadPort 중간 커버 LED
        Load_Middle = 11,
        Unload_Middle = 12,
        MAC = 13,
        Metal = 14,
    }
    public enum LEDLightingTypes
    {
        None = 0,
        On = 1,
        Off = 2,
        Blink = 3,
    }
    // 2024.11.14. by dwlim [END]
    class SELOP8Controller : LoadPortController
    {
        #region <Constructors>
        public SELOP8Controller(int portId, string name, EN_CONNECTION_TYPE interfaceType, int commIndex) : base(portId, name, interfaceType, commIndex)
        {
            // 2024.11.13. by dwlim [MOD] ACCESS MODE 저장
            _PortIndex = portId;
            ReadAccessModeFile();
        }
        #endregion </Constructors>

        #region Status Types
        private const int FoupPlacementStatus = 6;
        private const int FoupClampStatus = 7;
        private const int FoupDockStatus = 13;
        private const int ZaxisPosition = 15;
        #endregion

        #region Event Types
        private const int ClampPositionSensorMask = 1 << 0;
        private const int FoupDetectSensorMask1 = 1 << 24;
        private const int FoupStabilizedSensorMask1 = 1 << 26;
        private const int FoupStabilizedSensorMask2 = 1 << 27;
        private const int FoupStabilizedSensorMask3 = 1 << 28;

        #endregion

        #region <Fields>
        private const char SendStartToken = 's';
        private const char EndToken = ';';
        private const string LoadPortADR = "00";

        private const string AckMessage = "ACK";
        private const string NakMessage = "NAK";
        private const string StatusMessage = "STATE";
        private const string MapStatusMessage = "MAPRD";
        private const string VerMessage = "VERSN";
        private const string ParameterMessage = "PARAM";
        private const string LEDStatusMessage = "LEDST";
     
        private const string NormalCompleteMessage = "INF";
        private const string RetransmissionINF = "RIF";
        private const string AbnormalCompleteMessage = "ABS";
        private const string RetransmissionABS = "RAS";
        private const string FINmessage = "FIN";
        private const string EventMessage = "INPUT";

        private const string CarrierExist = "PODON";
        private const string CarrierNotexist = "PODOF";
        private const string CarrierPresented = "SMTON";

        private const string LightOn = "LON";
        private const string LightOff = "LOF";
        private const string LightBlink = "LBL";

        private const char SlotStatusEmpty = '0';
        private const char SlotStatusCorrectlyOccupied = '1';
        private const char SlotStatusCross = '2';
        private const char SlotStatusUpDownTilted = '4';
        private const char SlotStatusThicknessErrorThick = 'W';
        private const char SlotStatusThicknessErrorThin = 'T';

        private const int StatusMessageLength = 20;
        private const int LEDStatusMessageLength = 14;

        private readonly ConcurrentDictionary<LoadPortCommands, CommandResults> _commandResults
            = new ConcurrentDictionary<LoadPortCommands, CommandResults>();

        private LoadPortLoadingMode _currentCarrierMode;
        private LoadPortAccessMode _currentAccessMode;      // 2024.11.13. by dwlim [ADD] ACCESS MODE 저장
        private LoadPortLEDTypes _LEDType;                  // 2024.11.14. by dwlim [ADD] SELOP8 LED I/F 추가
        private LoadPortLEDTypes _ConflictingLEDType;       // 2024.11.20. by dwlim [ADD] SELOP8 LED I/F 추가
        private LEDLightingTypes _lightingType;             // 2024.11.14. by dwlim [ADD] SELOP8 LED I/F 추가
        readonly StringBuilder StringBuilder = new StringBuilder();

        private bool _initialized = false;
        private int _capacity = 0;
        private int _setCapacity = 0;
        private string[] _doingActionData;
        private string _doingActionName = string.Empty;

        private const uint TimeLong = 30000;
        private const uint TimeMiddle = 10000;
        private const uint TimeShort = 5000;

        private const int SlotMaxCount = 30;    // CYMECHS 26
        private const int SlotMinCount = 4;     // CYMECHS 1
                                                //private const string CommandAmpOn = "AMPON\n";

        // 2024.11.13. by dwlim [ADD] ACCESS MODE 저장
        #region <Config>
        private readonly int _PortIndex;
        private readonly string SECTION_NAME = "LOADPORT";
        private const string KEY_ACCESS_MODE = "ACCESS_MODE";
        private int _accessMode;
        #endregion </Config>
        // 2024.11.13. by dwlim [END]

        #region <Status Fields>
        private bool _temporaryPresent = false;
        private bool _temporaryPlaced = false;
        private bool _temporaryClamped = false;
        private bool _temporaryDocked = false;
        private bool _temporaryDoorState = false;

        //private bool _temporaryInitialized = false;
        private bool _temporaryAutoMode = false;
        private bool _temporaryInitialized = false;

        // 2024.10.11. by dwlim [ADD] _temporaryMode값 '1'로 초기화
        //private char _temporaryMode = '1';                      // 0 : Host Mode            1 : Manual Mode
        #endregion </Status Fields>

        #region <Event Fields>
        private const string LoadButtonPushed = "LODSW";
        private const string UnloadButtonPushed = "ULDSW";
        private const string FoupModeButtonPushed = "MACSW";
        private const string CassetteModeButtonPushed = "MTLSW";

        private const int FoupPresence1 = 1 << 24;
        private const int FoupPlacementMask1 = 1 << 26;
        private const int FoupPlacementMask2 = 1 << 27;
        private const int FoupPlacementMask3 = 1 << 28;
        #endregion </Event Fields>

        #endregion </Fields>

        #region <Enumerations>
        #endregion </Enumerations>

        #region <Methods>

        #region <Init/Close>
        public override bool InitController()
        {
            //System.Threading.Tasks.Task.Run(() => DoInitController());

            return true;
            //throw new NotImplementedException();
        }

        public override bool CloseController()
        {
            throw new NotImplementedException();
        }
        #endregion </Init/Close>

        #region <Actions>
        public override CommandResults DoInitialize()
        {
            //return ExecuteCommand(LoadPortCommands.Initialize);
            return ExecuteLEDInitialization(LoadPortCommands.Initialize);
        }

        public override CommandResults DoLoad()
        {
            return ExecuteLEDCommandWithMap(LoadPortCommands.Load);
        }

        public override CommandResults DoUnload()
        {
            return ExecuteLEDCommandWithMap(LoadPortCommands.Unload);
        }

        public override CommandResults DoClamp()
        {
            return ExecuteCommand(LoadPortCommands.Clamp);
        }

        public override CommandResults DoUnClamp()
        {
            return ExecuteCommand(LoadPortCommands.Unclamp);
        }

        public override CommandResults DoDock()
        {
            return ExecuteCommand(LoadPortCommands.Dock);
        }

        public override CommandResults DoUnDock()
        {
            return ExecuteCommand(LoadPortCommands.Undock);
        }

        public override CommandResults DoOpenDoor()
        {
            return ExecuteCommandWithMap(LoadPortCommands.DoorOpen);
        }

        public override CommandResults DoCloseDoor()
        {
            return ExecuteCommandWithMap(LoadPortCommands.DoorClose);
        }
        public override CommandResults DoScan()
        {
            return ExecuteCommandWithMap(LoadPortCommands.ScanDown);
        }
        public override CommandResults DoGetSlotMap()
        {
            return ExecuteCommandWithMap(LoadPortCommands.GetMap);
        }
        // TODO : 현재 로딩 모드가 뭔지 반환하는 명령 -> 찾아서 채워놓을 것
        public override CommandResults DoFindLoadingMode()
        {
            return ExecuteFindingLoadingMode(LoadPortCommands.FindLoadingMode);
        }
        public override CommandResults DoChangeLoadingMode(LoadPortLoadingMode mode)
        {
            return ExecuteChangingMode(mode);
        }

        public override CommandResults DoClearAlarm()
        {
            return ExecuteCommand(LoadPortCommands.Reset);
        }
        public override CommandResults DoAmpControl(bool enabled)
        {
            if (enabled)
                return ExecuteCommand(LoadPortCommands.AmpOn);
            else
                return ExecuteCommand(LoadPortCommands.AmpOff);
        }

        public override CommandResults DoChangeAccessMode(LoadPortAccessMode mode)
        {
            return ExecuteChangingAccessMode(mode);
        }

        #endregion </Actions>

        #region <States>
        public override void OnIndicatorChanged(LoadPortIndicatorTypes indicator, LoadPortIndicatorStates state)
        {
            throw new NotImplementedException();
        }
        public override string GetTriggeredControllerAlarm()
        {
            return _triggeredAlarm;
        }
        #endregion </States>

        #region <Thread>
        protected override bool RemoveTokens(string receivedMessage, ref string newString)
        {
            int index = receivedMessage.IndexOf(EndToken);
            if (index < 0)
                return false;

            string strMessage = receivedMessage.Remove(index);
            newString = strMessage.Remove(strMessage.IndexOf(SendStartToken), (SendStartToken + LoadPortADR).Length);

            return true;
        }

        // 받은 메시지를 파싱한다.
        protected override void ParseMessages(string receivedMessage)
        {
            // 1. 받은 메시지를 이용하여 파싱
            // 일단 Ack는 파싱할 필요가 없을 것 같다.
            if (false == ParseDatas(receivedMessage))
            {
                return;
            }
            if (receivedMessage.StartsWith(NormalCompleteMessage) || receivedMessage.StartsWith(RetransmissionINF))
            {
                if (_doingActionData[1].Equals(LoadButtonPushed))
                {
                    OnButtonPressed(LoadPortButtonTypes.Load);
                }
                else if (_doingActionData[1].Equals(UnloadButtonPushed))
                {
                    OnButtonPressed(LoadPortButtonTypes.Unload);
                }
                // TODO : 하드코딩
                else if(_doingActionData[1].Equals(CarrierNotexist))
                {
                    ChangePlacedState(false);
                    ChangePresentState(false);
                }
                else if (_doingActionData[1].Equals(CarrierExist))
                {
                    ChangePlacedState(true);
                    ChangePresentState(true);
                }
                // 2024.12.19. dwlim [ADD] Carrier를 제거하면서 마지막에 Present가 감지됨. Place와 같이 봐야할듯
                else if (_doingActionData[1].Equals(CarrierPresented))
                {
                    //ChangePresentState(true);
                }
                // PODON : PRESENT, PLACE 감지
                // END

                _commandResults.TryAdd(_doingAction, new CommandResults(_doingAction.ToString(), CommandResult.Completed));

                if (_doingAction == LoadPortCommands.Initialize)
                {
                    ChangeInitializationState(true);
                }

                _doingAction = LoadPortCommands.Idle;
            }
            else if (receivedMessage.StartsWith(NakMessage))
            {
                _commandResults.TryAdd(_doingAction, new CommandResults(_doingAction.ToString(), CommandResult.Error));
                _doingAction = LoadPortCommands.Idle;
            }
            else if (receivedMessage.StartsWith(AckMessage))
            {
                if (_doingActionData[1].StartsWith(StatusMessage))
                {
                    UpdateLogicalStates(_doingActionData[2]);

                    if (_doingAction.Equals(LoadPortCommands.GetState) ||
                        _doingAction.Equals(LoadPortCommands.FindLoadingMode))
                    {
                        _commandResults.TryAdd(_doingAction, new CommandResults(_doingAction.ToString(), CommandResult.Completed));
                        _doingAction = LoadPortCommands.Idle;
                    }
                }

                else if (receivedMessage.Contains(MapStatusMessage))
                {
                    // Map
                    UpdateSlotMap(_doingActionData[2]);

                    if (_doingAction.Equals(LoadPortCommands.GetMap) ||
                        _doingAction.Equals(LoadPortCommands.Load) ||
                        _doingAction.Equals(LoadPortCommands.Unload))
                    {
                        _commandResults.TryAdd(_doingAction, new CommandResults(_doingAction.ToString(), CommandResult.Completed));
                        _doingAction = LoadPortCommands.Idle;
                    }
                }

                else if (receivedMessage.Contains(VerMessage))
                {
                    //_firmwareVersion = receivedMessage.Substring(VerMessage.Length);
                    _doingAction = LoadPortCommands.Idle;
                }

                //else if (receivedMessage.StartsWith(HostMode) || receivedMessage.StartsWith(ManualMode))
                //{
                //    if (_doingActionData[1] == HostMode)
                //    {
                //        ChangeMode('0');
                //    }
                //    else if (_doingActionData[1] == ManualMode)
                //    {
                //        ChangeMode('1');
                //    }
                //    _doingAction = LoadPortCommands.Idle;
                //}

                else if (receivedMessage.Contains(ParameterMessage))
                {
                    int nIndexCount;

                    if (_doingActionData[2].StartsWith("SLN"))
                    {
                        _capacity = 0;
                        if (false == string.IsNullOrEmpty(_doingActionData[2]))
                        {
                            nIndexCount = _doingActionData[2].Length;
                            _capacity = int.Parse(_doingActionData[2].Substring(nIndexCount - 2));
                        }
                    }

                    _commandResults.TryAdd(_doingAction, new CommandResults(_doingAction.ToString(), CommandResult.Completed));
                    _doingAction = LoadPortCommands.Idle;
                }

                else if (receivedMessage.Contains(LightOn) || receivedMessage.Contains(LightBlink) || receivedMessage.Contains(LightOff))
                {
                    //string.Format("SET:{0}{1}", LightOn, ((int)_LEDType).ToString("D2"));
                    if ((int)_LEDType == int.Parse(_doingActionData[1].Substring(3,2)))
                    {
                        _commandResults.TryAdd(_doingAction, new CommandResults(_doingAction.ToString(), CommandResult.Completed));
                        _doingAction = LoadPortCommands.Idle;
                    }
                }
                else if (receivedMessage.Contains(LEDStatusMessage))
                {
                    UpdateLEDStates(_doingActionData[2]);

                    if (_doingAction.Equals(LoadPortCommands.LedStatus))
                    {
                        _commandResults.TryAdd(_doingAction, new CommandResults(_doingAction.ToString(), CommandResult.Completed));
                        _doingAction = LoadPortCommands.Idle;
                    }
                }
                else
                {
                    //_commandResults.TryAdd(_doingAction, new CommandResults(_doingAction.ToString(), CommandResult.Completed));
                    //_doingAction = LoadPortCommands.Idle;
                }

            }
            else
            {
                if (receivedMessage.StartsWith(AbnormalCompleteMessage) || receivedMessage.StartsWith(RetransmissionABS))
                {
                    // Error
                    if (false == _doingAction.Equals(LoadPortCommands.Idle))
                    {
                        _commandResults.TryAdd(_doingAction, new CommandResults(_doingAction.ToString(), CommandResult.Error, receivedMessage));
                        _doingAction = LoadPortCommands.Idle;
                    }
                    else
                    {
                        _triggeredAlarm = receivedMessage;
                    }
                }
            }
        }
        #endregion </Thread>

        #region <Internals>
        private CommandResults ExecuteFindingLoadingMode(LoadPortCommands command)
        {
            switch (_actionStep)
            {
                case 0:
                    {
                        // Reset
                        SetTimeOverByCommand(command);
                        _result.CommandResult = CommandResult.Proceed;
                        _result.Description = string.Empty;
                        if (SendMessage(LoadPortCommands.Reset))
                        {
                            ++_actionStep;
                        }
                    }
                    break;

                case 1:
                    {
                        if (IsTimeOver())
                        {
                            _result.CommandResult = CommandResult.Timeout;
                            break;
                        }
                        else
                        {
                            if (false == _commandResults.ContainsKey(LoadPortCommands.Reset))
                                break;

                            _result = GetCommandResult(LoadPortCommands.Reset);
                            if (_result.CommandResult.Equals(CommandResult.Completed))
                            {
                                if (false == command.Equals(LoadPortCommands.Reset))
                                {
                                    _result.CommandResult = CommandResult.Proceed;
                                    _result.Description = string.Empty;
                                    _actionStep = 10;
                                }
                            }
                        }
                    }
                    break;

                case 10:
                    {
                        // Command
                        if (SendMessage(command))
                        {
                            ++_actionStep;
                        }
                    }
                    break;

                case 11:
                    {
                        if (IsTimeOver())
                        {
                            _result.CommandResult = CommandResult.Timeout;
                            break;
                        }
                        else
                        {
                            if (false == _commandResults.ContainsKey(command))
                                break;

                            _result = GetCommandResult(command);
                        }
                    }
                    break;

                default:
                    _result.CommandResult = CommandResult.Invalid;
                    _result.Description = string.Format("Invalid Seq Num : {0}", _actionStep);
                    break;
            }

            if (false == _result.CommandResult.Equals(CommandResult.Proceed))
            {
                _doingAction = LoadPortCommands.Idle;
                _actionStep = 0;
            }

            return _result;
        }
        private CommandResults ExecuteChangingMode(LoadPortLoadingMode mode)
        {
            LoadPortCommands command = mode.Equals(LoadPortLoadingMode.Cassette) ?
                LoadPortCommands.ChangeToCassette : LoadPortCommands.ChangeToFoup;

            if (false == LoadPortModeChanger.ContainsKey(mode))
                return new CommandResults(command.ToString(), CommandResult.Error);


            var result = ExecuteLEDCommand(command);
            if (result.CommandResult.Equals(CommandResult.Completed))
            {
                _currentCarrierMode = mode;
                ChangeLoadingTypeState(mode);
            }

            return result;
        }
        // 2024.11.13. by dwlim [MOD] ACCESS MODE 저장
        private CommandResults ExecuteChangingAccessMode(LoadPortAccessMode mode)
        {
            LoadPortCommands command = mode.Equals(LoadPortAccessMode.Auto) ?
                LoadPortCommands.ChangeAccessModeToAuto : LoadPortCommands.ChangeAccessModeToManual;

            var result = ExecuteOnlyLEDCommand(command);
            if (_currentAccessMode != mode && result.CommandResult == CommandResult.Completed)
            {
                WriteAccessModeFile(mode);
            }

            return result;
        }

        private CommandResults ExecuteOnlyLEDCommand(LoadPortCommands command)
        {
            switch (_actionStep)
            {
                case 0:
                    {
                        // Reset
                        SetTimeOverByCommand(command);
                        _result.CommandResult = CommandResult.Proceed;
                        _result.Description = string.Empty;
                        if (SendMessage(LoadPortCommands.Reset))
                        {
                            ++_actionStep;
                        }
                    }
                    break;
                case 1:
                    {
                        if (IsTimeOver())
                        {
                            _result.CommandResult = CommandResult.Timeout;
                            break;
                        }
                        else
                        {
                            if (false == _commandResults.ContainsKey(LoadPortCommands.Reset))
                                break;

                            _result = GetCommandResult(LoadPortCommands.Reset);
                            if (_result.CommandResult.Equals(CommandResult.Completed))
                            {
                                if (false == command.Equals(LoadPortCommands.Reset))
                                {
                                    _result.CommandResult = CommandResult.Proceed;
                                    _result.Description = string.Empty;
                                    _actionStep = 10;
                                }
                            }
                        }
                    }
                    break;
                case 10:
                    {
                        // Led Off
                        GetConflictingLEDIndex(command);
                        if (SendMessage(LoadPortCommands.LedOff))
                        {
                            ++_actionStep;
                        }
                    }
                    break;
                case 11:
                    {
                        if (IsTimeOver())
                        {
                            _result.CommandResult = CommandResult.Timeout;
                            break;
                        }
                        else
                        {
                            if (false == _commandResults.ContainsKey(LoadPortCommands.LedOff))
                                break;

                            _result = GetCommandResult(LoadPortCommands.LedOff);
                            if (_result.CommandResult.Equals(CommandResult.Completed))
                            {
                                _result.CommandResult = CommandResult.Proceed;
                                _result.Description = string.Empty;
                                _actionStep = 20;
                            }
                        }
                    }
                    break;
                case 20:
                    {
                        // Led On
                        GetLEDIndex(command);
                        if (SendMessage(LoadPortCommands.LedOn))
                        {
                            ++_actionStep;
                        }
                    }
                    break;
                case 21:
                    {
                        if (IsTimeOver())
                        {
                            _result.CommandResult = CommandResult.Timeout;
                            break;
                        }
                        else
                        {
                            if (false == _commandResults.ContainsKey(LoadPortCommands.LedOn))
                                break;

                            _result = GetCommandResult(LoadPortCommands.LedOn);
                            if (_result.CommandResult.Equals(CommandResult.Completed))
                            {
                                _result.CommandResult = CommandResult.Proceed;
                                _result.Description = string.Empty;
                                _actionStep = 30;
                            }
                        }
                    }
                    break;
                case 30:
                    {
                        // GetState
                        if (SendMessage(LoadPortCommands.GetState))
                        {
                            ++_actionStep;
                        }
                    }
                    break;
                case 31:
                    if (IsTimeOver())
                    {
                        _result.CommandResult = CommandResult.Timeout;
                        break;
                    }

                    if (false == _commandResults.ContainsKey(LoadPortCommands.GetState))
                        break;

                    _result = GetCommandResult(LoadPortCommands.GetState);
                    break;

                default:
                    _result.CommandResult = CommandResult.Invalid;
                    _result.Description = string.Format("Invalid Seq Num : {0}", _actionStep);
                    break;
            }

            if (false == _result.CommandResult.Equals(CommandResult.Proceed))
            {
                _doingAction = LoadPortCommands.Idle;
                _actionStep = 0;
            }

            return _result;
        }

        private CommandResults ExecuteLEDCommand(LoadPortCommands command)
        {
            switch (_actionStep)
            {
                case 0:
                    {
                        // Reset
                        SetTimeOverByCommand(command);
                        _result.CommandResult = CommandResult.Proceed;
                        _result.Description = string.Empty;
                        if (SendMessage(LoadPortCommands.Reset))
                        {
                            ++_actionStep;
                        }
                    }
                    break;
                case 1:
                    {
                        if (IsTimeOver())
                        {
                            _result.CommandResult = CommandResult.Timeout;
                            break;
                        }
                        else
                        {
                            if (false == _commandResults.ContainsKey(LoadPortCommands.Reset))
                                break;

                            _result = GetCommandResult(LoadPortCommands.Reset);
                            if (_result.CommandResult.Equals(CommandResult.Completed))
                            {
                                if (false == command.Equals(LoadPortCommands.Reset))
                                {
                                    _result.CommandResult = CommandResult.Proceed;
                                    _result.Description = string.Empty;
                                    _actionStep = 10;
                                }
                            }
                        }
                    }
                    break;
                case 10:
                    {
                        // Led Off
                        GetLEDIndex(command);
                        if (SendMessage(LoadPortCommands.LedBlink))
                        {
                            ++_actionStep;
                        }
                    }
                    break;
                case 11:
                    {
                        if (IsTimeOver())
                        {
                            _result.CommandResult = CommandResult.Timeout;
                            break;
                        }
                        else
                        {
                            if (false == _commandResults.ContainsKey(LoadPortCommands.LedBlink))
                                break;

                            _result = GetCommandResult(LoadPortCommands.LedBlink);
                            if (_result.CommandResult.Equals(CommandResult.Completed))
                            {

                                //if (command.Equals(LoadPortCommands.Initialize))
                                //{
                                //    _initialized = true;
                                //    ChangeInitializationState(_initialized);
                                //}

                                _result.CommandResult = CommandResult.Proceed;
                                _result.Description = string.Empty;
                                _actionStep = 20;
                            }
                        }
                    }
                    break;
                case 20:
                    {
                        // Command
                        if (SendMessage(command))
                        {
                            ++_actionStep;
                        }
                    }
                    break;
                case 21:
                    {
                        if (IsTimeOver())
                        {
                            _result.CommandResult = CommandResult.Timeout;
                            break;
                        }
                        else
                        {
                            if (false == _commandResults.ContainsKey(command))
                                break;

                            _result = GetCommandResult(command);
                            if (_result.CommandResult.Equals(CommandResult.Completed))
                            {

                                //if (command.Equals(LoadPortCommands.Initialize))
                                //{
                                //    _initialized = true;
                                //    ChangeInitializationState(_initialized);
                                //}

                                _result.CommandResult = CommandResult.Proceed;
                                _result.Description = string.Empty;
                                _actionStep = 30;
                            }
                        }
                    }
                    break;
                case 30:
                    {
                        // Led On
                        if (SendMessage(LoadPortCommands.LedOff))
                        {
                            ++_actionStep;
                        }
                    }
                    break;
                case 31:
                    {
                        if (IsTimeOver())
                        {
                            _result.CommandResult = CommandResult.Timeout;
                            break;
                        }
                        else
                        {
                            if (false == _commandResults.ContainsKey(LoadPortCommands.LedOff))
                                break;

                            _result = GetCommandResult(LoadPortCommands.LedOff);
                            if (_result.CommandResult.Equals(CommandResult.Completed))
                            {

                                //if (command.Equals(LoadPortCommands.Initialize))
                                //{
                                //    _initialized = true;
                                //    ChangeInitializationState(_initialized);
                                //}

                                _result.CommandResult = CommandResult.Proceed;
                                _result.Description = string.Empty;
                                _actionStep = 40;
                            }
                        }
                    }
                    break;
                case 40:
                    {
                        // GetState
                        if (SendMessage(LoadPortCommands.GetState))
                        {
                            ++_actionStep;
                        }
                    }
                    break;
                case 41:
                    if (IsTimeOver())
                    {
                        _result.CommandResult = CommandResult.Timeout;
                        break;
                    }

                    if (false == _commandResults.ContainsKey(LoadPortCommands.GetState))
                        break;

                    _result = GetCommandResult(LoadPortCommands.GetState);
                    break;

                default:
                    _result.CommandResult = CommandResult.Invalid;
                    _result.Description = string.Format("Invalid Seq Num : {0}", _actionStep);
                    break;
            }

            if (false == _result.CommandResult.Equals(CommandResult.Proceed))
            {
                _doingAction = LoadPortCommands.Idle;
                _actionStep = 0;
            }

            return _result;
        }

        private CommandResults ExecuteLEDCommandWithMap(LoadPortCommands command)
        {
            switch (_actionStep)
            {
                case 0:
                    {
                        // Reset
                        SetTimeOverByCommand(command);
                        _result.CommandResult = CommandResult.Proceed;
                        _result.Description = string.Empty;
                        if (SendMessage(LoadPortCommands.Reset))
                        {
                            ++_actionStep;
                        }
                    }
                    break;

                case 1:
                    {
                        if (IsTimeOver())
                        {
                            _result.CommandResult = CommandResult.Timeout;
                            break;
                        }
                        else
                        {
                            if (false == _commandResults.ContainsKey(LoadPortCommands.Reset))
                                break;

                            _result = GetCommandResult(LoadPortCommands.Reset);
                            if (_result.CommandResult.Equals(CommandResult.Completed))
                            {
                                _result.CommandResult = CommandResult.Proceed;
                                _result.Description = string.Empty;
                                ++_actionStep;
                            }
                        }
                    }
                    break;

                case 2:
                    {
                        // Get Capacity
                        if (SendMessage(LoadPortCommands.GetCapacity))
                        {
                            ++_actionStep;
                        }
                    }
                    break;
                case 3:
                    {
                        if (IsTimeOver())
                        {
                            _result.CommandResult = CommandResult.Timeout;
                            break;
                        }
                        else
                        {
                            if (false == _commandResults.ContainsKey(LoadPortCommands.GetCapacity))
                                break;

                            _result = GetCommandResult(LoadPortCommands.GetCapacity);
                            if (_result.CommandResult.Equals(CommandResult.Completed))
                            {
                                _result.CommandResult = CommandResult.Proceed;
                                _result.Description = string.Empty;
                                _actionStep = 10;
                            }
                        }
                    }
                    break;
                case 10:
                    {
                        // Led Off
                        GetLEDIndex(command);
                        if (SendMessage(LoadPortCommands.LedBlink))
                        {
                            ++_actionStep;
                        }
                    }
                    break;
                case 11:
                    {
                        if (IsTimeOver())
                        {
                            _result.CommandResult = CommandResult.Timeout;
                            break;
                        }
                        else
                        {
                            if (false == _commandResults.ContainsKey(LoadPortCommands.LedBlink))
                                break;

                            _result = GetCommandResult(LoadPortCommands.LedBlink);
                            if (_result.CommandResult.Equals(CommandResult.Completed))
                            {

                                //if (command.Equals(LoadPortCommands.Initialize))
                                //{
                                //    _initialized = true;
                                //    ChangeInitializationState(_initialized);
                                //}

                                _result.CommandResult = CommandResult.Proceed;
                                _result.Description = string.Empty;
                                _actionStep = 20;
                            }
                        }
                    }
                    break;
                case 20:
                    {
                        // Command
                        if (SendMessage(command))
                        {
                            ++_actionStep;
                        }
                    }
                    break;

                case 21:
                    {
                        if (IsTimeOver())
                        {
                            _result.CommandResult = CommandResult.Timeout;
                            break;
                        }
                        else
                        {
                          if (false == _commandResults.ContainsKey(command))
                                break;

                            _result = GetCommandResult(command);
                            if (_result.CommandResult.Equals(CommandResult.Completed))
                            {
                                _result.CommandResult = CommandResult.Proceed;
                                _result.Description = string.Empty;
                                _actionStep = 30;
                            }
                        }
                    }
                    break;

                case 30:
                    {
                        // Led On
                        if (SendMessage(LoadPortCommands.LedOff))
                        {
                            ++_actionStep;
                        }
                    }
                    break;
                case 31:
                    {
                        if (IsTimeOver())
                        {
                            _result.CommandResult = CommandResult.Timeout;
                            break;
                        }
                        else
                        {
                            if (false == _commandResults.ContainsKey(LoadPortCommands.LedOff))
                                break;

                            _result = GetCommandResult(LoadPortCommands.LedOff);
                            if (_result.CommandResult.Equals(CommandResult.Completed))
                            {

                                //if (command.Equals(LoadPortCommands.Initialize))
                                //{
                                //    _initialized = true;
                                //    ChangeInitializationState(_initialized);
                                //}

                                _result.CommandResult = CommandResult.Proceed;
                                _result.Description = string.Empty;
                                _actionStep = 40;
                            }
                        }
                    }
                    break;

                case 40:
                    {
                        // GetState
                        if (SendMessage(LoadPortCommands.GetState))
                        {
                            ++_actionStep;
                        }
                    }
                    break;

                case 41:
                    if (IsTimeOver())
                    {
                        _result.CommandResult = CommandResult.Timeout;
                        break;
                    }

                    if (false == _commandResults.ContainsKey(LoadPortCommands.GetState))
                        break;

                    //if (command.Equals(LoadPortCommands.Initialize))
                    //{
                    //    _initialized = true;
                    //    ChangeInitializationState(_initialized);
                    //}
                    //_result = GetCommandResult(LoadPortCommands.GetState);
                    _result = GetCommandResult(LoadPortCommands.GetState);
                    if (_result.CommandResult.Equals(CommandResult.Completed))
                    {
                        _result.CommandResult = CommandResult.Proceed;
                        _result.Description = string.Empty;
                        _actionStep = 50;
                    }
                    break;

                case 50:
                    if (SendMessage(LoadPortCommands.GetMap))
                    {
                        ++_actionStep;
                    }
                    break;

                case 51:
                    {
                        if (IsTimeOver())
                        {
                            _result.CommandResult = CommandResult.Timeout;
                            break;
                        }
                        else
                        {
                            if (false == _commandResults.ContainsKey(LoadPortCommands.GetMap))
                                break;

                            _result = GetCommandResult(LoadPortCommands.GetMap);
                        }
                    }
                    break;

                default:
                    _result.CommandResult = CommandResult.Invalid;
                    _result.Description = string.Format("Invalid Seq Num : {0}", _actionStep);
                    break;
            }

            if (false == _result.CommandResult.Equals(CommandResult.Proceed))
            {
                _doingAction = LoadPortCommands.Idle;
                _actionStep = 0;
            }

            return _result;
        }

        private CommandResults ExecuteLEDInitialization(LoadPortCommands command)
        {
            switch (_actionStep)
            {
                case 0:
                    {
                        // Reset
                        SetTimeOverByCommand(command);
                        _result.CommandResult = CommandResult.Proceed;
                        _result.Description = string.Empty;
                        if (SendMessage(LoadPortCommands.Reset))
                        {
                            ++_actionStep;
                        }
                    }
                    break;

                case 1:
                    {
                        if (IsTimeOver())
                        {
                            _result.CommandResult = CommandResult.Timeout;
                            break;
                        }
                        else
                        {
                            if (false == _commandResults.ContainsKey(LoadPortCommands.Reset))
                                break;

                            _result = GetCommandResult(LoadPortCommands.Reset);
                            if (_result.CommandResult.Equals(CommandResult.Completed))
                            {
                                _result.CommandResult = CommandResult.Proceed;
                                _result.Description = string.Empty;
                                _actionStep = 10;
                            }
                        }
                    }
                    break;

                case 10:
                    {
                        // Command
                        if (SendMessage(command))
                        {
                            ++_actionStep;
                        }
                    }
                    break;
                case 11:
                    {
                        if (IsTimeOver())
                        {
                            _result.CommandResult = CommandResult.Timeout;
                            break;
                        }
                        else
                        {
                            if (false == _commandResults.ContainsKey(command))
                                break;

                            _result = GetCommandResult(command);
                            if (_result.CommandResult.Equals(CommandResult.Completed))
                            {
                                ChangeInitializationState(true);

                                _result.CommandResult = CommandResult.Proceed;
                                _result.Description = string.Empty;
                                _actionStep = 20;
                            }
                        }
                    }
                    break;
                case 20:
                    {
                        switch (AccessMode)
                        {
                            case LoadPortAccessMode.Auto:
                                GetConflictingLEDIndex(LoadPortCommands.ChangeAccessModeToAuto);
                                break;
                            case LoadPortAccessMode.Manual:
                                GetConflictingLEDIndex(LoadPortCommands.ChangeAccessModeToManual);
                                break;
                        }
                        if (SendMessage(LoadPortCommands.LedOff))
                        {
                            ++_actionStep;
                        }
                    }
                    break;
                case 21:
                    {
                        if (IsTimeOver())
                        {
                            _result.CommandResult = CommandResult.Timeout;
                            break;
                        }
                        else
                        {
                            if (false == _commandResults.ContainsKey(LoadPortCommands.LedOff))
                                break;

                            _result = GetCommandResult(LoadPortCommands.LedOff);
                            if (_result.CommandResult.Equals(CommandResult.Completed))
                            {
                                _result.CommandResult = CommandResult.Proceed;
                                _result.Description = string.Empty;
                                _actionStep = 30;
                            }
                        }
                    }
                    break;
                case 30:
                    {
                        switch (AccessMode)
                        {
                            case LoadPortAccessMode.Auto:
                                GetLEDIndex(LoadPortCommands.ChangeAccessModeToAuto);
                                break;
                            case LoadPortAccessMode.Manual:
                                GetLEDIndex(LoadPortCommands.ChangeAccessModeToManual);
                                break;
                        }
                        if (SendMessage(LoadPortCommands.LedOn))
                        {
                            ++_actionStep;
                        }
                    }
                    break;
                case 31:
                    {
                        if (IsTimeOver())
                        {
                            _result.CommandResult = CommandResult.Timeout;
                            break;
                        }
                        else
                        {
                            if (false == _commandResults.ContainsKey(LoadPortCommands.LedOn))
                                break;

                            _result = GetCommandResult(LoadPortCommands.LedOn);
                            if (_result.CommandResult.Equals(CommandResult.Completed))
                            {
                                _result.CommandResult = CommandResult.Proceed;
                                _result.Description = string.Empty;
                                _actionStep = 40;
                            }
                        }
                    }
                    break;
                case 40:
                    {
                        // GetState
                        if (SendMessage(LoadPortCommands.GetState))
                        {
                            ++_actionStep;
                        }
                    }
                    break;
                case 41:
                    {
                        if (IsTimeOver())
                        {
                            _result.CommandResult = CommandResult.Timeout;
                            break;
                        }

                        if (false == _commandResults.ContainsKey(LoadPortCommands.GetState))
                            break;

                        return GetCommandResult(LoadPortCommands.GetState);
                    }

                default:
                    _result.CommandResult = CommandResult.Invalid;
                    _result.Description = string.Format("Invalid Seq Num : {0}", _actionStep);
                    break;
            }

            if (false == _result.CommandResult.Equals(CommandResult.Proceed))
            {
                _doingAction = LoadPortCommands.Idle;
                _actionStep = 0;
            }

            return _result;
        }
        // 2024.11.13. by dwlim [END]

        private CommandResults ExecuteCommandWithMap(LoadPortCommands command)
        {
            switch (_actionStep)
            {
                case 0:
                    {
                        // Reset
                        SetTimeOverByCommand(command);
                        _result.CommandResult = CommandResult.Proceed;
                        _result.Description = string.Empty;
                        if (SendMessage(LoadPortCommands.Reset))
                        {
                            ++_actionStep;
                        }
                    }
                    break;

                case 1:
                    {
                        if (IsTimeOver())
                        {
                            _result.CommandResult = CommandResult.Timeout;
                            break;
                        }
                        else
                        {
                            if (false == _commandResults.ContainsKey(LoadPortCommands.Reset))
                                break;

                            _result = GetCommandResult(LoadPortCommands.Reset);
                            if (_result.CommandResult.Equals(CommandResult.Completed))
                            {
                                _result.CommandResult = CommandResult.Proceed;
                                _result.Description = string.Empty;
                                ++_actionStep;
                            }
                        }
                    }
                    break;

                case 2:
                    {
                        // Get Capacity
                        if (SendMessage(LoadPortCommands.GetCapacity))
                        {
                            ++_actionStep;
                        }
                    }
                    break;
                case 3:
                    {
                        if (IsTimeOver())
                        {
                            _result.CommandResult = CommandResult.Timeout;
                            break;
                        }
                        else
                        {
                            if (false == _commandResults.ContainsKey(LoadPortCommands.GetCapacity))
                                break;

                            _result = GetCommandResult(LoadPortCommands.GetCapacity);
                            if (_result.CommandResult.Equals(CommandResult.Completed))
                            {
                                _result.CommandResult = CommandResult.Proceed;
                                _result.Description = string.Empty;
                                _actionStep = 10;
                            }
                        }
                    }
                    break;

                case 10:
                    {
                        // Command
                        if (SendMessage(command))
                        {
                            ++_actionStep;
                        }
                    }
                    break;

                case 11:
                    {
                        if (IsTimeOver())
                        {
                            _result.CommandResult = CommandResult.Timeout;
                            break;
                        }
                        else
                        {
                            if (false == _commandResults.ContainsKey(command))
                                break;

                            _result = GetCommandResult(command);
                            if (_result.CommandResult.Equals(CommandResult.Completed))
                            {
                                _result.CommandResult = CommandResult.Proceed;
                                _result.Description = string.Empty;
                                _actionStep = 20;
                            }
                        }
                    }
                    break;

                case 20:
                    {
                        // GetState
                        if (SendMessage(LoadPortCommands.GetState))
                        {
                            ++_actionStep;
                        }
                    }
                    break;
                case 21:
                    if (IsTimeOver())
                    {
                        _result.CommandResult = CommandResult.Timeout;
                        break;
                    }

                    if (false == _commandResults.ContainsKey(LoadPortCommands.GetState))
                        break;

                    //if (command.Equals(LoadPortCommands.Initialize))
                    //{
                    //    _initialized = true;
                    //    ChangeInitializationState(_initialized);
                    //}
                    //_result = GetCommandResult(LoadPortCommands.GetState);
                    _result = GetCommandResult(LoadPortCommands.GetState);
                    if (_result.CommandResult.Equals(CommandResult.Completed))
                    {
                        _result.CommandResult = CommandResult.Proceed;
                        _result.Description = string.Empty;
                        _actionStep = 30;
                    }
                    break;

                case 30:
                    if (SendMessage(LoadPortCommands.GetMap))
                    {
                        ++_actionStep;
                    }
                    break;

                case 31:
                    {
                        if (IsTimeOver())
                        {
                            _result.CommandResult = CommandResult.Timeout;
                            break;
                        }
                        else
                        {
                            if (false == _commandResults.ContainsKey(LoadPortCommands.GetMap))
                                break;

                            _result = GetCommandResult(LoadPortCommands.GetMap);
                        }
                    }
                    break;

                default:
                    _result.CommandResult = CommandResult.Invalid;
                    _result.Description = string.Format("Invalid Seq Num : {0}", _actionStep);
                    break;
            }

            if (false == _result.CommandResult.Equals(CommandResult.Proceed))
            {
                _doingAction = LoadPortCommands.Idle;
                _actionStep = 0;
            }

            return _result;
        }

        private CommandResults ExecuteInitialization(LoadPortCommands command)
        {
            switch (_actionStep)
            {
                case 0:
                    {
                        // Reset
                        SetTimeOverByCommand(command);
                        _result.CommandResult = CommandResult.Proceed;
                        _result.Description = string.Empty;
                        if (SendMessage(LoadPortCommands.Reset))
                        {
                            ++_actionStep;
                        }
                    }
                    break;

                case 1:
                    {
                        if (IsTimeOver())
                        {
                            _result.CommandResult = CommandResult.Timeout;
                            break;
                        }
                        else
                        {
                            if (false == _commandResults.ContainsKey(LoadPortCommands.Reset))
                                break;

                            _result = GetCommandResult(LoadPortCommands.Reset);
                            if (_result.CommandResult.Equals(CommandResult.Completed))
                            {
                                _result.CommandResult = CommandResult.Proceed;
                                _result.Description = string.Empty;
                                _actionStep = 10;
                            }
                        }
                    }
                    break;

                case 10:
                    {
                        // Command
                        if (SendMessage(command))
                        {
                            ++_actionStep;
                        }
                    }
                    break;
                case 11:
                    {
                        if (IsTimeOver())
                        {
                            _result.CommandResult = CommandResult.Timeout;
                            break;
                        }
                        else
                        {
                            if (false == _commandResults.ContainsKey(command))
                                break;

                            _result = GetCommandResult(command);
                            if (_result.CommandResult.Equals(CommandResult.Completed))
                            {
                                ChangeInitializationState(true);

                                _result.CommandResult = CommandResult.Proceed;
                                _result.Description = string.Empty;
                                _actionStep = 20;
                            }
                        }
                    }
                    break;
                case 20:
                    {
                        // GetState
                        if (SendMessage(LoadPortCommands.GetState))
                        {
                            ++_actionStep;
                        }
                    }
                    break;
                case 21:
                    {
                        if (IsTimeOver())
                        {
                            _result.CommandResult = CommandResult.Timeout;
                            break;
                        }

                        if (false == _commandResults.ContainsKey(LoadPortCommands.GetState))
                            break;

                        _result = GetCommandResult(LoadPortCommands.GetState);
                        if (_result.CommandResult.Equals(CommandResult.Completed))
                        {
                            _result.CommandResult = CommandResult.Proceed;
                            _result.Description = string.Empty;
                            _actionStep = 30;
                        }
                    }
                    break;

                case 30:
                    {
                        // GetState
                        if (SendMessage(LoadPortCommands.GetAcceessingMode))
                        {
                            ++_actionStep;
                        }
                    }
                    break;
                case 31:
                    {
                        if (IsTimeOver())
                        {
                            _result.CommandResult = CommandResult.Timeout;
                            break;
                        }

                        if (false == _commandResults.ContainsKey(LoadPortCommands.GetAcceessingMode))
                            break;

                        return GetCommandResult(LoadPortCommands.GetAcceessingMode);
                    }

                default:
                    _result.CommandResult = CommandResult.Invalid;
                    _result.Description = string.Format("Invalid Seq Num : {0}", _actionStep);
                    break;
            }

            if (false == _result.CommandResult.Equals(CommandResult.Proceed))
            {
                _doingAction = LoadPortCommands.Idle;
                _actionStep = 0;
            }

            return _result;
        }

        private CommandResults ExecuteCommand(LoadPortCommands command)
        {
            switch (_actionStep)
            {
                case 0:
                    {
                        // Reset
                        SetTimeOverByCommand(command);
                        _result.CommandResult = CommandResult.Proceed;
                        _result.Description = string.Empty;
                        if (SendMessage(LoadPortCommands.Reset))
                        {
                            ++_actionStep;
                        }
                    }
                    break;

                case 1:
                    {
                        if (IsTimeOver())
                        {
                            _result.CommandResult = CommandResult.Timeout;
                            break;
                        }
                        else
                        {
                            if (false == _commandResults.ContainsKey(LoadPortCommands.Reset))
                                break;

                            _result = GetCommandResult(LoadPortCommands.Reset);
                            if (_result.CommandResult.Equals(CommandResult.Completed))
                            {
                                if (false == command.Equals(LoadPortCommands.Reset))
                                {
                                    _result.CommandResult = CommandResult.Proceed;
                                    _result.Description = string.Empty;
                                    _actionStep = 10;
                                }
                            }
                        }
                    }
                    break;

                case 10:
                    {
                        // Command
                        if (SendMessage(command))
                        {
                            ++_actionStep;
                        }
                    }
                    break;
                case 11:
                    {
                        if (IsTimeOver())
                        {
                            _result.CommandResult = CommandResult.Timeout;
                            break;
                        }
                        else
                        {
                            if (false == _commandResults.ContainsKey(command))
                                break;

                            _result = GetCommandResult(command);
                            if (_result.CommandResult.Equals(CommandResult.Completed))
                            {

                                //if (command.Equals(LoadPortCommands.Initialize))
                                //{
                                //    _initialized = true;
                                //    ChangeInitializationState(_initialized);
                                //}

                                _result.CommandResult = CommandResult.Proceed;
                                _result.Description = string.Empty;
                                _actionStep = 20;
                            }
                        }
                    }
                    break;
                case 20:
                    {
                        // GetState
                        if (SendMessage(LoadPortCommands.GetState))
                        {
                            ++_actionStep;
                        }
                    }
                    break;
                case 21:
                    if (IsTimeOver())
                    {
                        _result.CommandResult = CommandResult.Timeout;
                        break;
                    }

                    if (false == _commandResults.ContainsKey(LoadPortCommands.GetState))
                        break;

                    _result = GetCommandResult(LoadPortCommands.GetState);
                    break;

                default:
                    _result.CommandResult = CommandResult.Invalid;
                    _result.Description = string.Format("Invalid Seq Num : {0}", _actionStep);
                    break;
            }

            if (false == _result.CommandResult.Equals(CommandResult.Proceed))
            {
                _doingAction = LoadPortCommands.Idle;
                _actionStep = 0;
            }

            return _result;
        }


        private void SetTimeOverByCommand(LoadPortCommands command)
        {
            uint time;
            switch (command)
            {
                case LoadPortCommands.Load:
                case LoadPortCommands.Unload:
                case LoadPortCommands.DoorOpen:
                case LoadPortCommands.DoorClose:
                case LoadPortCommands.Initialize:
                case LoadPortCommands.Scan:
                    time = TimeLong;
                    break;

                case LoadPortCommands.Clamp:
                case LoadPortCommands.Unclamp:
                case LoadPortCommands.Reset:
                case LoadPortCommands.AmpOn:
                case LoadPortCommands.AmpOff:
                case LoadPortCommands.GetState:
                case LoadPortCommands.GetMap:
                case LoadPortCommands.GetCapacity:
                case LoadPortCommands.LedOn:
                case LoadPortCommands.LedOff:
                case LoadPortCommands.LedBlink:
                    time = TimeShort;
                    break;

                case LoadPortCommands.Dock:
                case LoadPortCommands.Undock:
                    time = TimeMiddle;
                    break;

                case LoadPortCommands.ChangeToCassette:
                case LoadPortCommands.ChangeToFoup:
                    time = TimeMiddle;
                    break;

                default:
                    time = TimeShort;
                    break;
            }

            _timeChecker.SetTickCount(time);
        }

        private bool SendMessage(LoadPortCommands command)
        {
            // SET : [장비 설정] 오류 재설정, 초기화 등의 설정 처리를 요청하는 명령
            // GET : [상태 획득 요청] 상태 또는 매핑 결과 보고를 요청하는 명령
            // MOV : [동작 요청] 동작 실행 및 동작 제어를 요청하는 명령
            // MOD : [모드 설정] SELOP 통신 모드 전환을 요청하는 명령
            // EVT : [이벤트 설정] 이벤트 활성화/비활성화를 요청하는 명령

            if (_commandResults.ContainsKey(command))
                _commandResults.TryRemove(command, out _);

            string commandMessage = string.Empty;
            string messageToSend = string.Empty;
            switch (command)
            {
                case LoadPortCommands.Load:
                    if (_currentCarrierMode == LoadPortLoadingMode.Cassette)
                    {
                        commandMessage = "MOV:CLDMP";
                    }
                    else
                    {
                        commandMessage = "MOV:CLDMP";
                    }
                    break;
                case LoadPortCommands.Unload:
                    if (_currentCarrierMode == LoadPortLoadingMode.Cassette)
                    {
                        commandMessage = "MOV:CULOD";
                    }
                    else
                    {
                        commandMessage = "MOV:CULOD";    // 확인 필요
                    }
                    break;
                case LoadPortCommands.Clamp:
                    commandMessage = "MOV:PODCL";
                    break;
                case LoadPortCommands.Unclamp:
                    commandMessage = "MOV:PODOP";
                    break;
                case LoadPortCommands.Dock:
                    commandMessage = "MOV:YDDOR";
                    break;
                case LoadPortCommands.Undock:
                    commandMessage = "MOV:YWAIT";
                    break;
                case LoadPortCommands.DoorOpen:
                    commandMessage = "MOV:ZMPDW";
                    break;
                case LoadPortCommands.DoorClose:
                    commandMessage = "MOV:ZMPUP";
                    break;
                case LoadPortCommands.Hello:
                    //messageToSend = "LOAD";
                    commandMessage = "";
                    break;
                case LoadPortCommands.Initialize:
                    commandMessage = "MOV:ABORG";
                    break;
                case LoadPortCommands.Scan:
                    //messageToSend = "SCAN UP";
                    commandMessage = "";
                    break;
                case LoadPortCommands.ScanDown:
                    commandMessage = "MOV:MAPD1";
                    break;
                case LoadPortCommands.GetMap:
                    //messageToSend = "GET:MAPDT";
                    commandMessage = "GET:MAPRD";
                    break;
                case LoadPortCommands.GetCapacity:
                    commandMessage = "GET:PARAM/SLN";
                    break;
                case LoadPortCommands.FindLoadingMode:
                    commandMessage = "GET:STATE";
                    break;
                //case LoadPortCommands.SetCapacity:      // +4 ~ +30
                //    messageToSend = string.Format("SET:PARAM/SLN=+0000{0}", _setCapacity);
                //    break;
                case LoadPortCommands.GetState:
                    commandMessage = "GET:STATE";
                    break;
                case LoadPortCommands.Reset:
                    commandMessage = "SET:RESET";
                    break;
                case LoadPortCommands.ChangeToCassette:
                    commandMessage = "MOV:PINUP";
                    break;
                case LoadPortCommands.ChangeToFoup:
                    commandMessage = "MOV:PINDW";
                    break;
                //case LoadPortCommands.GetAcceessingMode:
                //    //messageToSend = "AUTO_MODE";
                //    messageToSend = "";
                //    break;
                case LoadPortCommands.ChangeAccessModeToAuto:
                    commandMessage = "";
                    break;
                case LoadPortCommands.ChangeAccessModeToManual:
                    commandMessage = "";
                    break;
                // 2024.11.14. by dwlim [ADD] SELOP8 LED I/F 추가
                case LoadPortCommands.LedOn:
                    commandMessage = string.Format("SET:{0}{1}", LightOn, ((int)_LEDType).ToString("D2"));
                    break;
                case LoadPortCommands.LedOff:
                    commandMessage = string.Format("SET:{0}{1}", LightOff, ((int)_LEDType).ToString("D2"));
                    break;
                case LoadPortCommands.LedBlink:
                    commandMessage = string.Format("SET:{0}{1}", LightBlink, ((int)_LEDType).ToString("D2"));
                    break;
                // 2024.11.14. by dwlim [END]

                // 2024.11.18. by dwlim [ADD] SELOP8 LED Status 추가
                case LoadPortCommands.LedStatus:
                    commandMessage = "GET:LEDST";
                    break;
                default:
                    commandMessage = string.Empty;
                    break;
            }
            messageToSend = string.Format("{0}{1}{2}{3}", SendStartToken, LoadPortADR, commandMessage, EndToken);

            return DoAction(command, messageToSend);
        }

        private CommandResults GetCommandResult(LoadPortCommands command)
        {
            if (false == _commandResults.TryRemove(command, out CommandResults commandResult))
                return new CommandResults(command.ToString(), CommandResult.Proceed);

            return commandResult;
        }

        private bool IsTimeOver()
        {
            if (_timeChecker.IsTickOver(true))
            {
                _actionStep = 0;

                return true;
            }

            return false;
        }

        private void UpdateLogicalStates(string receivedMessage)
        {
            if (receivedMessage.Length != StatusMessageLength)
                return;

            // Place & Present
            ChangePlacedState(receivedMessage[6] == '1' ? true : false);
            ChangePresentState(receivedMessage[6] == '1' ? true : false);   // 2024.12.19. by dwlim [ADD] 처음에 Present정보가 없어서 추가

            // Clamp
            ChangeClampState(receivedMessage[7] == '1' ? true : false);

            // Dock
            ChangeDockState(receivedMessage[13] == '1' ? true : false);

            // Door Open
            ChangeDoorState(receivedMessage[15] == '1' ? true : false);

            // Loading Mode
            switch (receivedMessage[16])
            {
                case '0':
                    ChangeLoadingTypeState(LoadPortLoadingMode.Foup);
                    break;
                case '1':
                    ChangeLoadingTypeState(LoadPortLoadingMode.Cassette);
                    break;
                default:
                    ChangeLoadingTypeState(LoadPortLoadingMode.Unknown);
                    break;
            }

            // Mode

            // MappedStatus
        }

        private void UpdateMechanicalStates(string receivedMessage)
        {
            int states = int.Parse(receivedMessage, System.Globalization.NumberStyles.HexNumber);

            // Present
            _temporaryPresent = (states & FoupDetectSensorMask1) != 0 ? true : false;
            ChangePresentState(_temporaryPresent);

            // Placement
            _temporaryPlaced = ((states & FoupStabilizedSensorMask1) != 0) && ((states & FoupStabilizedSensorMask2) != 0)
                                            && ((states & FoupStabilizedSensorMask3) != 0) ? true : false;
            ChangePlacedState(_temporaryPlaced);

            // FoupClampStatus
            _temporaryClamped = (states & ClampPositionSensorMask) != 0 ? true : false;
            ChangeClampState(_temporaryClamped);
        }

        private void UpdateSlotMap(string receivedMessage)
        {
            CarrierSlotMapStates[] slotstate = new CarrierSlotMapStates[_capacity];

            if (_capacity == 0)
            {
                for (int i = SlotMaxCount - 1; i >= SlotMinCount - 1; i--)
                {
                    if (receivedMessage[i] != '0')
                    {
                        _capacity = i + 1;
                        break;
                    }
                }
            }
            
            for (int i = 0; i < _capacity; ++i)
            {
                if (receivedMessage[i] == SlotStatusEmpty)
                {
                    slotstate[i] = CarrierSlotMapStates.Empty;
                }
                else if (receivedMessage[i] == SlotStatusCorrectlyOccupied)
                {
                    slotstate[i] = CarrierSlotMapStates.CorrectlyOccupied;
                }
                else if (receivedMessage[i] == SlotStatusCross)
                {
                    slotstate[i] = CarrierSlotMapStates.CrossSlotted;
                }
                else if (receivedMessage[i] == SlotStatusUpDownTilted)
                {
                    slotstate[i] = CarrierSlotMapStates.NotEmpty;
                }
                else if (receivedMessage[i] == SlotStatusThicknessErrorThick)
                {
                    slotstate[i] = CarrierSlotMapStates.DoubleSlotted;
                }
                else if (receivedMessage[i] == SlotStatusThicknessErrorThin)
                {
                    slotstate[i] = CarrierSlotMapStates.NotEmpty;
                }
                //else if (receivedMessage[i] == '4')
                //{
                //    slotstate[i] = Defines.LoadPort.CarrierSlotMapStates.UpDownTilte;
                //}
                //else if (receivedMessage[i] == 'W')
                //{
                //    slotstate[i] = Defines.LoadPort.CarrierSlotMapStates.WaferThicknessError_Thick;
                //}
                //else if (receivedMessage[i] == 'T')
                //{
                //    slotstate[i] = Defines.LoadPort.CarrierSlotMapStates.WaferThicknessError_Thin;
                //}
                // 2024.10.04. jhlim [END]
                else
                {
                    slotstate[i] = CarrierSlotMapStates.Undefined;
                }
            }
            ChangeSlotMap(slotstate);
        }

        // 2024.11.18. by dwlim [TODO] ACCESS MODE 저장 만들어야함 (LSTEC 박찬율 책임 수정완료 이후에)
        private void UpdateLEDStates(string receivedMessage)
        { }

        private bool ParseDatas(string receivedMessage)
        {
            //int index = receivedMessage.IndexOf(":");
            //if (index < 0)
            //    return false;
            _doingActionData = null;

            if (string.IsNullOrEmpty(receivedMessage))
        {
                return false;
            }

            if (receivedMessage.Contains(":"))
            {
                if (receivedMessage.Contains("/"))
                {
                    _doingActionData = receivedMessage.Split(':', '/');
                    return true;
                }
                _doingActionData = receivedMessage.Split(':');
                return true;
            }

            return false;
        }
        private void GetLEDIndex(LoadPortCommands command)
        {
            switch (command)
            {
                // LOAD, UNLOAD, ALARM 은 Parameter 변경으로 HOST에서 컨트롤하도록 변경 가능.
                case LoadPortCommands.Load:
                    _LEDType = LoadPortLEDTypes.Load_Middle;    // Load_Top은 HOST에서 컨트롤
                    break;
                case LoadPortCommands.Unload:
                    _LEDType = LoadPortLEDTypes.Unload_Middle;  // Unload_Top는 HOST에서 컨트롤
                    break;
                case LoadPortCommands.ChangeToCassette:
                    _LEDType = LoadPortLEDTypes.Metal;
                    break;
                case LoadPortCommands.ChangeToFoup:
                    _LEDType = LoadPortLEDTypes.MAC;
                    break;
                case LoadPortCommands.ChangeAccessModeToAuto:
                    _LEDType = LoadPortLEDTypes.Auto;
                    break;
                case LoadPortCommands.ChangeAccessModeToManual:
                    _LEDType = LoadPortLEDTypes.Manual;
                    break;
                default:
                    break;
            }
        }
        // 2024.12.18. by dwlim [ADD] 상반되는 상태의 LED Index를 구한다.
        private void GetConflictingLEDIndex(LoadPortCommands command)
        {
            switch (command)
            {
                // LOAD, UNLOAD, ALARM 은 Parameter 변경으로 HOST에서 컨트롤하도록 변경 가능.
                case LoadPortCommands.Load:
                    _LEDType = LoadPortLEDTypes.Unload_Middle;    // Load_Top은 HOST에서 컨트롤
                    break;
                case LoadPortCommands.Unload:
                    _LEDType = LoadPortLEDTypes.Load_Middle;  // Unload_Top는 HOST에서 컨트롤
                    break;
                case LoadPortCommands.ChangeToCassette:
                    _LEDType = LoadPortLEDTypes.MAC;
                    break;
                case LoadPortCommands.ChangeToFoup:
                    _LEDType = LoadPortLEDTypes.Metal;
                    break;
                case LoadPortCommands.ChangeAccessModeToAuto:
                    _LEDType = LoadPortLEDTypes.Manual;
                    break;
                case LoadPortCommands.ChangeAccessModeToManual:
                    _LEDType = LoadPortLEDTypes.Auto;
                    break;
                default:
                    break;
            }
        }
        // 2024.12.18. by dwlim [END]
        // 2024.11.13. by dwlim [ADD] ACCESS MODE 저장
        private void ReadAccessModeFile()
        {
            string path = string.Format(@"{0}\AccessMode", Define.DefineConstant.FilePath.FILEPATH_EXE);
            if (false == Directory.Exists(path))
                Directory.CreateDirectory(path);

            string fullName = string.Format(@"{0}\LoadPort{1}.ini", path, _PortIndex);
            if (false == File.Exists(fullName))
            {
                WriteAccessModeFile(_currentAccessMode);
                return;
            }

            IniControl ini = new IniControl(fullName);

            _accessMode = ini.GetInt(SECTION_NAME, KEY_ACCESS_MODE, -1);
            if (_accessMode > -1)
            {
                _currentAccessMode = _accessMode == 0 ? LoadPortAccessMode.Auto : LoadPortAccessMode.Manual;
                ChangeAccessingState(_currentAccessMode);
            }
        }
        private void WriteAccessModeFile(LoadPortAccessMode mode)
        {
            string path = string.Format(@"{0}\AccessMode", Define.DefineConstant.FilePath.FILEPATH_EXE);
            if (false == Directory.Exists(path))
                Directory.CreateDirectory(path);

            string fullName = string.Format(@"{0}\LoadPort{1}.ini", path, _PortIndex);
            IniControl ini = new IniControl(fullName);

            if (_currentAccessMode != mode)
            {
                _currentAccessMode = mode;
                ChangeAccessingState(_currentAccessMode);
            }

            _accessMode = mode == LoadPortAccessMode.Auto ? 0 : 1;
            ini.WriteInt(SECTION_NAME, KEY_ACCESS_MODE, _accessMode);
        }
        // 2024.11.13. by dwlim [END]

        #endregion </Internals>

        #endregion </Methods>
    }
}


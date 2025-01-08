﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Define.DefineEnumBase.Common;
using EFEM.Defines.Common;
using EFEM.Defines.LoadPort;

namespace EFEM.Modules.LoadPort.LoadPortControllers
{
    class DuraportController : LoadPortController
    {
        #region <Constructors>
        public DuraportController(int portId, string name, EN_CONNECTION_TYPE interfaceType, int commIndex) : base(portId, name, interfaceType, commIndex) { }
        #endregion </Constructors>

        #region Status Types
        //private const int StatusLength = 9;
        private const int SlotMaxCount = 26;
        private const int SlotMinCount = 1;

        //private const int WaferSlideOutMask = 1 << 30;
        private const int PresentMask = 1 << 29;
        private const int PlacedMask = 1 << 28;
        //private const int UndockedMask = 1 << 12;
        private const int DockedMask = 1 << 11;
        //private const int UnclampedMask = 1 << 10;
        private const int ClampedMask = 1 << 9;
        //private const int DoorClosedMask = 1 << 3;
        private const int DoorOpenMask = 1 << 2;
        
        private const int InitializedMask = 1 << 0;
        private const int FoupModeMask = 1 << 7;
        private const int CassetteModeMask = 1 << 8;
        private const int AutoModeMask = 1 << 23;
        #endregion

        #region Event Types
        private const string LoadButtonPushed = "00000004";
        private const string UnloadButtonPushed = "00000008";
        private const string CarrierPlaced = "00000010";
        private const string CarrierRemoved = "00000020";
        private const string EmergencyPushed = "01000000";
        //public const int TP_Connected = 1 << 0;
        //public const int TP_Disconnected = 1 << 1;
        //public const int Load_Unload_Button_Pushed = 1 << 2;
        //public const int Pod_In = 1 << 4;
        //public const int Pod_Out = 1 << 5;
        //public const int Start_Reset_All = 1 << 8;
        //public const int End_Reset_All = 1 << 9;
        //public const int Foup_Started = 1 << 16;
        //public const int Foup_Incorrect_Pos = 1 << 17;
        //public const int Estop_Pushed = 1 << 24;
        #endregion

        #region <Fields>
        private const char LineFeedToken = '\n';
        //private const string AckMessage = "A";
        private const string NakMessage = "N";
        private const string CompleteMessage = "O";

        private const string ErrorMessage = "E";        // 시작 문자열
        private const string StatusMessage = "S";       // 시작 문자열
        private const string EventMessage = "C";        // 시작 문자열
        private const string MapStatusMessage = "M";    // 시작 문자열

        private const string VerMessage = "VER";        // 시작 문자열

        private const string On = "ON";
        private const string Off = "OFF";

        //private string _firmwareVersion = string.Empty;

        private readonly ConcurrentDictionary<LoadPortCommands, CommandResults> _commandResults
            = new ConcurrentDictionary<LoadPortCommands, CommandResults>();

        //private readonly Dictionary<int, bool> _statusResults = new Dictionary<int, bool>();

        //public DURAPORT_STATES[] _duraportStates = new DURAPORT_STATES[32];
        private LoadPortLoadingMode _currentCarrierMode;
        readonly StringBuilder StringBuilder = new StringBuilder();

        private bool _initialized = false;
        private int _capacity = 0;

        private const uint TimeLong = 30000;
        private const uint TimeMiddle = 10000;
        private const uint TimeShort = 5000;
        //private const string CommandAmpOn = "AMPON\n";

        #region <Status Fields>
        private bool _temporaryPresent = false;
        private bool _temporaryPlaced = false;
        private bool _temporaryClamped = false;
        private bool _temporaryDocked = false;
        private bool _temporaryDoorState = false;

        private bool _temporaryInitialized = false;
        //private bool _temporaryCassetteMode = false;
        //private bool _temporaryFoupMode = false;
        private bool _temporaryAutoMode = false;
        //private bool _waferSlideOut = false;
        //private bool _hasAllValues = false;
        #endregion </Status Fields>

        #region <Event Fields>
        //private bool _tpConnected = false;
        //private bool _tpDisconnected = false;
        //private bool _loadUnloadButtonPushed = false;
        //private bool _podIn = false;
        //private bool _podOut = false;
        //private bool _startResetAll = false;
        //private bool _endResetAll = false;
        //private bool _foupStarted = false;
        //private bool _foupIncorrectPos = false;
        //private bool _estopPushed = false;
        #endregion </Event Fields>

        #endregion </Fields>

        #region <Enumerations>
        public enum DURAPORT_STATES
        {
            HOMING_COMPLETES = 0,
            MOTOR_DRIVER_ON,
            OPEN_CONDITION,
            CLOSE_CONDITION,
            CONDITION_OF_ACTING,
            BACKUP_DATAS_CRASH,
            MAINTENANCE_MODE_CONDITION,
            RESERVED_1,
            RESERVED_2,
            POD_temporaryClamped = 9,
            POD_UNCLAMPED,
            POD_temporaryDocked,
            POD_UNDOCKED,
            VACUUM_CONDITION,
            LATCH_CONDITION,
            UNLATCH_CONDITION,
            ERROR_OCCURRENCE_CONDITION,
            RESERVED_3,
            RESERVED_4,
            RESERVED_5,
            AMHS_MODE = 20,
            BCR_USAGE,
            MAPPING_FUNCTION_USAGE,
            AUTO_MODE_CONDITION,
            LOAD_UNLOAD_ID_SWITCH_USAGE,
            OPEN_CASSETTE_USAGE,
            RESERVE_CONDITION_OF_LOADPORT,
            RESERVED_6,
            PLACEMENT_SENSOR_CONDITION = 28,
            PRESENCE_SENSOR_CONDITION,
            WAFER_SLIDE_OUT_PROTRUSION,
            FAN_IO_CONDITION,
        }
        public enum DURAPORT_EVENTS
        {
            TP_CONNECTED,
            TP_DISCONNECTED,
            LOAD_UNLOAD_BUTTON_PUSHED,
            POD_IN,
            POD_OUT,
            START_RESET_ALL,
            END_RESET_ALL,
            FOUP_STARTED,
            FOUP_INCORRECT_POS,
            ESTOP_PUSHED,
        }
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
            return ExecuteCommand(LoadPortCommands.Initialize);
        }

        public override CommandResults DoLoad()
        {
            return ExecuteCommandWithMap(LoadPortCommands.Load);
        }

        public override CommandResults DoUnload()
        {
            return ExecuteCommandWithMap(LoadPortCommands.Unload);
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
        public override CommandResults DoFindLoadingMode()
        {
            return ExecuteCommand(LoadPortCommands.FindLoadingMode);
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
            int index = receivedMessage.IndexOf(LineFeedToken);
            if (index < 0)
                return false;

            newString = receivedMessage.Remove(index);

            return true;
        }

        // 받은 메시지를 파싱한다.
        protected override void ParseMessages(string receivedMessage)
        {
            // 1. 토큰 제거
            //if (false == RemoveTokens(receivedMessage, ref receivedMessage))
            //    return;
            
            // 2. 받은 메시지를 이용하여 파싱
            // 일단 Ack는 파싱할 필요가 없을 것 같다.
            if (/*receivedMessage.Equals(AckMessage) || */receivedMessage.Equals(CompleteMessage))
            {
                //if (receivedMessage.Equals(AckMessage))
                //{
                //    _commandResults.TryAdd(_doingAction, COMMAND_RESULT.PROCEED);
                //}
                //else
                {
                    _commandResults.TryAdd(_doingAction, new CommandResults(_doingAction.ToString(), CommandResult.Completed));
                    _doingAction = LoadPortCommands.Idle;
                }
            }
            else if (receivedMessage.Equals(NakMessage))
            {
                _commandResults.TryAdd(_doingAction, new CommandResults(_doingAction.ToString(), CommandResult.Error));
                _doingAction = LoadPortCommands.Idle;
            }
            else if(_doingAction.Equals(LoadPortCommands.GetCapacity) && false == receivedMessage.Equals("A"))
            {
                _capacity = 0;
                int.TryParse(receivedMessage, out _capacity);
                _commandResults.TryAdd(_doingAction, new CommandResults(_doingAction.ToString(), CommandResult.Completed));
                _doingAction = LoadPortCommands.Idle;
            }
            else
            {
                if (receivedMessage.StartsWith(StatusMessage) || receivedMessage.StartsWith(EventMessage))
                {
                    string message = receivedMessage.Substring(1);

                    if (receivedMessage.StartsWith(StatusMessage))
                    {
                        // Status
                        UpdateLogicalStates(message);

                        if (_doingAction.Equals(LoadPortCommands.GetState))
                        {
                            _commandResults.TryAdd(_doingAction, new CommandResults(_doingAction.ToString(), CommandResult.Completed));
                            _doingAction = LoadPortCommands.Idle;
                        }
                    }
                    else
                    {
                        // Event
                        UpdateMechanicalStates(message);
                    }
                }
                else if (receivedMessage.StartsWith(MapStatusMessage))
                {
                    //string message = receivedMessage.Substring(1);

                    // Map
                    UpdateSlotMap(receivedMessage);

                    if (_doingAction.Equals(LoadPortCommands.GetMap) ||
                        _doingAction.Equals(LoadPortCommands.Load) ||
                        _doingAction.Equals(LoadPortCommands.Unload))
                    {
                        _commandResults.TryAdd(_doingAction, new CommandResults(_doingAction.ToString(), CommandResult.Completed));
                        _doingAction = LoadPortCommands.Idle;
                    }
                }
                else if (receivedMessage.StartsWith(ErrorMessage))
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
                else if (receivedMessage.Contains(VerMessage))
                {
                    //_firmwareVersion = receivedMessage.Substring(VerMessage.Length);
                    _doingAction = LoadPortCommands.Idle;
                }
                else if (receivedMessage.Equals(On) || receivedMessage.Equals("OFF"))
                {
                    switch (_doingAction)
                    {
                        case LoadPortCommands.GetAcceessingMode:
                            {
                                if (receivedMessage.Equals(On))
                                {
                                    ChangeAccessingState(LoadPortAccessMode.Auto);
                                }
                                else
                                {
                                    ChangeAccessingState(LoadPortAccessMode.Manual);
                                }

                                _commandResults.TryAdd(_doingAction, new CommandResults(_doingAction.ToString(), CommandResult.Completed));
                            }
                            break;
                        case LoadPortCommands.FindLoadingMode:
                            {
                                if (receivedMessage.Equals(On))
                                {
                                    ChangeLoadingTypeState(LoadPortLoadingMode.Cassette);
                                }
                                else
                                {
                                    ChangeLoadingTypeState(LoadPortLoadingMode.Foup);
                                }
                                _commandResults.TryAdd(_doingAction, new CommandResults(_doingAction.ToString(), CommandResult.Completed));
                            }
                            break;

                        default:
                            break;
                    }

                    _doingAction = LoadPortCommands.Idle;
                }
            }

        }
        #endregion </Thread>

        #region <Internals>
        private CommandResults ExecuteChangingMode(LoadPortLoadingMode mode)
        {
            LoadPortCommands command = mode.Equals(LoadPortLoadingMode.Cassette) ?
                LoadPortCommands.ChangeToCassette : LoadPortCommands.ChangeToFoup;

            if (false == LoadPortModeChanger.ContainsKey(mode))
                return new CommandResults(command.ToString(), CommandResult.Error);


            var result = ExecuteCommand(command);
            if (result.CommandResult.Equals(CommandResult.Completed))
            {
                _currentCarrierMode = mode;
                ChangeLoadingTypeState(mode);
            }

            return result;
        }
        private CommandResults ExecuteChangingAccessMode(LoadPortAccessMode mode)
        {
            LoadPortCommands command = mode.Equals(LoadPortAccessMode.Auto) ?
                LoadPortCommands.ChangeAccessModeToAuto : LoadPortCommands.ChangeAccessModeToManual;

            return ExecuteCommand(command);
        }

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
                            //switch (command)
                            //{
                            //    case LoadPortCommands.Load:
                            //        {
                            //            if (DockState && DoorState)
                            //            {
                            //                _commandResults.TryAdd(command, new CommandResults(command.ToString(), CommandResult.Completed));
                            //                _doingAction = LoadPortCommands.Idle;
                            //            }
                            //        }
                            //        break;
                            //    case LoadPortCommands.Unload:
                            //        {
                            //            if (false == DockState && false == DoorState)
                            //            {
                            //                _commandResults.TryAdd(command, new CommandResults(command.ToString(), CommandResult.Completed));
                            //                _doingAction = LoadPortCommands.Idle;
                            //            }
                            //        }
                            //        break;
                            //    case LoadPortCommands.DoorOpen:
                            //        {
                            //            if (DoorState)
                            //            {
                            //                _commandResults.TryAdd(command, new CommandResults(command.ToString(), CommandResult.Completed));
                            //                _doingAction = LoadPortCommands.Idle;
                            //            }
                            //        }
                            //        break;
                            //    case LoadPortCommands.DoorClose:
                            //        {
                            //            if (false == DoorState)
                            //            {
                            //                _commandResults.TryAdd(command, new CommandResults(command.ToString(), CommandResult.Completed));
                            //                _doingAction = LoadPortCommands.Idle;
                            //            }
                            //        }
                            //        break;
                            //    case LoadPortCommands.ScanDown:
                            //        break;
                            //    case LoadPortCommands.GetMap:
                            //        break;
                            //}
                            if (false == _commandResults.ContainsKey(command))
                                break;

                            _result = GetCommandResult(command);
                            break;
                            //if (_result.CommandResult.Equals(CommandResult.Completed))
                            //{
                            //    _result.CommandResult = CommandResult.Proceed;
                            //    _result.Description = string.Empty;
                            //    _actionStep = 20;
                            //}
                        }
                    }
                    
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
            if (_commandResults.ContainsKey(command))
                _commandResults.TryRemove(command, out _);
            string messageToSend;
            switch (command)
            {
                case LoadPortCommands.Load:                  
                    if(_currentCarrierMode == LoadPortLoadingMode.Cassette)
                    {
                        messageToSend = "LOAD O";
                    }
                    else
                    {
                        messageToSend = "LOAD M";
                    }
                    break;
                case LoadPortCommands.Unload:
                    if (_currentCarrierMode == LoadPortLoadingMode.Cassette)
                    {
                        messageToSend = "UNLOAD O";
                    }
                    else
                    {
                        messageToSend = "UNLOAD M";
                    }
                    break;
                case LoadPortCommands.Clamp:
                    messageToSend = "POD_LOCK ON";
                    break;
                case LoadPortCommands.Unclamp:
                    messageToSend = "POD_LOCK OFF";
                    break;
                case LoadPortCommands.Dock:
                    messageToSend = "DOCK";
                    break;
                case LoadPortCommands.Undock:
                    messageToSend = "UNDOCK";
                    break;
                case LoadPortCommands.DoorOpen:
                    messageToSend = "TOPEN";
                    break;
                case LoadPortCommands.DoorClose:
                    messageToSend = "TCLOSE";
                    break;
                case LoadPortCommands.Hello:
                    messageToSend = "LOAD";
                    break;
                case LoadPortCommands.Initialize:
                    messageToSend = "HOM";
                    break;
                case LoadPortCommands.Scan:
                    messageToSend = "SCAN UP";
                    break;
                case LoadPortCommands.ScanDown:
                    messageToSend = "SCAN DN";
                    break;
                case LoadPortCommands.GetMap:
                    //messageToSend = "CFG_SLOTS";
                    messageToSend = "GETMAP";
                    break;
                case LoadPortCommands.GetCapacity:
                    messageToSend = "CFG_SLOTS";
                    break;
                case LoadPortCommands.GetState:
                    messageToSend = "STATUS";
                    break;
                case LoadPortCommands.Reset:
                    messageToSend = "RESET";
                    break;
                case LoadPortCommands.FindLoadingMode:
                    messageToSend = "OC_MODE";
                    break;
                case LoadPortCommands.ChangeToCassette:
                    messageToSend = "OC_MODE ON";
                    break;
                case LoadPortCommands.ChangeToFoup:
                    messageToSend = "OC_MODE OFF";
                    break;
                case LoadPortCommands.GetAcceessingMode:
                    messageToSend = "AUTO_MODE";
                    break;
                case LoadPortCommands.ChangeAccessModeToAuto:
                    messageToSend = "AUTO_MODE ON";
                    break;
                case LoadPortCommands.ChangeAccessModeToManual:
                    messageToSend = "AUTO_MODE OFF";
                    break;
                default:
                    return false;
            }
            
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
            int states = 0;
            try
            {
                states = int.Parse(receivedMessage, System.Globalization.NumberStyles.HexNumber);
            }
            catch (Exception)
            {
                return;
            }

            // Present
            _temporaryPresent = (states & PresentMask) != 0;
            ChangePresentState(_temporaryPresent);

            // Placed
            _temporaryPlaced = (states & PlacedMask) != 0;
            ChangePlacedState(_temporaryPlaced);

            // Clamped
            _temporaryClamped = (states & ClampedMask) != 0;
            ChangeClampState(_temporaryClamped);

            // Docked
            _temporaryDocked = (states & DockedMask) != 0;
            ChangeDockState(_temporaryDocked);

            // Door
            _temporaryDoorState = (states & DoorOpenMask) != 0;
            ChangeDoorState(_temporaryDoorState);

            // Initialized
            _temporaryInitialized = (states & InitializedMask) != 0;
            ChangeInitializationState(_temporaryInitialized);

            // Loading Mode
            //_temporaryCassetteMode = (states & CassetteModeMask) != 0;
            //_temporaryFoupMode = (states & FoupModeMask) != 0;
            //if (_temporaryCassetteMode && false == _temporaryFoupMode)
            //    ChangeLoadingTypeState(LoadPortLoadingMode.Cassette);
            //else if (_temporaryFoupMode && false == _temporaryCassetteMode)
            //    ChangeLoadingTypeState(LoadPortLoadingMode.Foup);

            // Auto Mode
            _temporaryAutoMode = (states & AutoModeMask) != 0;
            LoadPortAccessMode mode = _temporaryAutoMode ? LoadPortAccessMode.Auto : LoadPortAccessMode.Manual;
            ChangeAccessingState(mode);
        }

        private void UpdateMechanicalStates(string receivedMessage)
        {
            //uint states = uint.Parse(receivedMessage, System.Globalization.NumberStyles.HexNumber);

            switch (receivedMessage)
            {
                case LoadButtonPushed:
                    OnButtonPressed(LoadPortButtonTypes.Load);
                    break;
                case UnloadButtonPushed:
                    OnButtonPressed(LoadPortButtonTypes.Unload);
                    break;
                case CarrierPlaced:
                    ChangePresentState(true);
                    ChangePlacedState(true);
                    //_carrier = new EFEM.CarrierManagement.Carrier(PortId);
                    break;
                case CarrierRemoved:
                    ChangePresentState(false);
                    ChangePlacedState(false);
                    //_carrier = null;
                    break;
                case EmergencyPushed:
                    _initialized = false;
                    ChangeInitializationState(_initialized);
                    break;
                default:
                    break;
            }
        }

        private void UpdateSlotMap(string receivedMessage)
        {
            string[] parts = receivedMessage.Split(new[] { 'M', ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3)
                return;

            int available = int.Parse(parts[0], System.Globalization.NumberStyles.HexNumber);
            if (available < 0)
                return;

            int crossSlotted = int.Parse(parts[1], System.Globalization.NumberStyles.HexNumber);
            int doubleSlotted = int.Parse(parts[2].Substring(0, 8), System.Globalization.NumberStyles.HexNumber);

            if (_capacity == 0)
            {
                for (int i = SlotMaxCount - 1; i >= SlotMinCount - 1; i--)
                {
                    bool slotMaskCheck;
                    int slotMask = 0x1 << i;
                    slotMaskCheck = (available & slotMask) != 0;
                    if (slotMaskCheck)
                    {
                        _capacity = i + 1;
                        //i = -1;
                        break;
                    }
                }
            }

            CarrierSlotMapStates[] slotstate = new CarrierSlotMapStates[_capacity];
            for (int i = 0; i < _capacity; ++i)
            {
                int slotMask = 0x1 << i;
                slotstate[i] = (available & slotMask) != 0 ? Defines.LoadPort.CarrierSlotMapStates.CorrectlyOccupied : Defines.LoadPort.CarrierSlotMapStates.Empty;
                if ((crossSlotted & slotMask) != 0)
                {
                    slotstate[i] = Defines.LoadPort.CarrierSlotMapStates.CrossSlotted;
                }

                if ((doubleSlotted & slotMask) != 0)
                {
                    slotstate[i] = Defines.LoadPort.CarrierSlotMapStates.DoubleSlotted;
                }
            }
            ChangeSlotMap(slotstate);
        }
        #endregion </Internals>

        #endregion </Methods>
    }
}

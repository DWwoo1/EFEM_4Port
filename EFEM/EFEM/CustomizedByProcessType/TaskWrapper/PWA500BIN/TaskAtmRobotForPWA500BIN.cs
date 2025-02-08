using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TickCounter_;

using EFEM.Defines.Common;
using EFEM.Defines.AtmRobot;
using EFEM.Defines.LoadPort;
using EFEM.MaterialTracking.LocationServer;
using EFEM.MaterialTracking;
using EFEM.Defines.MaterialTracking;
using EFEM.CustomizedByProcessType.PWA500BIN;

using FrameOfSystem3.Recipe;
using FrameOfSystem3.SECSGEM.Scenario;

using Define.DefineEnumProject.Task.AtmRobot;
using FrameOfSystem3.SECSGEM.DefineSecsGem;

// ConfigTask에서 이 namespace를 가지고 클래스 타입을 가져오기 때문에 변경 불가
namespace FrameOfSystem3.Task
{
    class TaskAtmRobotForPWA500BIN : TaskAtmRobot
    {
        #region <Constructors>
        public TaskAtmRobotForPWA500BIN(int nIndexOfTask, string strTaskName)
            : base(nIndexOfTask, strTaskName, new TaskAtmRobotRecovery500BIN(strTaskName, nIndexOfTask))
        {
            Ticks = new TickCounter();
            _scenarioManager = ScenarioManagerForPWA500BIN_TP.Instance;
            ProcessModuleName = _processGroup.GetProcessModuleName(ProcessModuleIndex);
            _lotHistoryLog = LotHistoryLog.Instance;
        }
        #endregion </Constructors>

        #region <Fields>
        private CommandResults _commandResult = new CommandResults("", CommandResult.Invalid);
        private const ScenarioListTypes ScenarioRecipeDownload = ScenarioListTypes.SCENARIO_REQ_RECIPE_DOWNLOAD;
        private const ScenarioListTypes ScenarioCoreTrackIn = ScenarioListTypes.SCENARIO_REQ_TRACK_IN;
        private const ScenarioListTypes ScenarioLotMatch = ScenarioListTypes.SCENARIO_REQ_LOT_MATCH;
        private const ScenarioListTypes ScenarioBinWaferIdAssign = ScenarioListTypes.SCENARIO_REQ_BIN_WAFER_ID_ASSIGN;
        private const ScenarioListTypes ScenarioSendClientToBinWaferIdAssign = ScenarioListTypes.SCENARIO_ASSIGN_SUBSTRATE_ID;
        private const ScenarioListTypes ScenarioBinWorkEnd = ScenarioListTypes.SCENARIO_BIN_WORK_END;
        private const ScenarioListTypes ScnearioBinTrackOut = ScenarioListTypes.SCENARIO_REQ_BIN_WAFER_TRACK_OUT;
        private const ScenarioListTypes ScenarioReqBinPartId = ScenarioListTypes.SCENARIO_BIN_PART_ID_INFO_REQ;
        private const ScenarioListTypes ScenarioSendClientUploadBinFile = ScenarioListTypes.SCENARIO_REQ_UPLOAD_BINFILE;
        private const ScenarioListTypes ScenarioUploadBinData = ScenarioListTypes.SCENARIO_BIN_DATA_UPLOAD;
        private const ScenarioListTypes ScenarioProcessStart = ScenarioListTypes.SCENARIO_PROCESS_START;
        private const ScenarioListTypes ScenarioProcessEnd = ScenarioListTypes.SCENARIO_PROCESS_END;
        private QueuedScenarioInfo _executingScenario = new QueuedScenarioInfo();
        private readonly Queue<ScenarioListTypes> QueuedScenarioForBinSubstrate = new Queue<ScenarioListTypes>();
        private readonly Queue<ScenarioListTypes> QueuedScenarioForCoreSubstrate = new Queue<ScenarioListTypes>();
        private string _temporaryDescription = string.Empty;

        private readonly TickCounter Ticks = null;
        private const int ProcessModuleIndex = 0;
        private readonly string ProcessModuleName = string.Empty;

        // 시간 파라메터화가 필요한가?
        private const uint TimeoutShort = 30000;
        private const uint TimeoutLong = 60000;
        private CommandResults _result = new CommandResults("", CommandResult.Error);
        private int _subStepInterface;

        private const string ErrorDescriptionForInvalidSubstratePortInfo = "Invalid Substrate Port Info";
        private const string ErrorDescriptionForDoesntHaveCarrier = "Doesn't have carrier at loadport";
        private const string ErrorDescriptionForLoadPortNotEnabled = "Loadport is not enabled";
        private const string ErrorDescriptionForDoorIsNotOpened = "Carrier's door is not opened";
        private const string ErrorDescriptionForSlotIsFull = "All of the slot is full";

        private const string ErrorDescriptionForAssignSubstrateId = "Cannot getting a assigned substrate Id";
        private const string ErrorDescriptionForRequestPartId = "Cannot getting a assigned part Id";
        private static ScenarioManagerForPWA500BIN_TP _scenarioManager = null;
        private static LotHistoryLog _lotHistoryLog = null;

        private string _newSubstrateId;
        private string _newPartId;

        private BinDataToUploadFromPWA500BIN _binDataToUpload;
        #endregion </Fields>

        #region <Properties>
        #endregion </Properties>

        #region <Type>
        private enum UnloadingStepTypes
        {
            Init = 0,
            AfterIdAssignment,
            AfterRequestPartId,
        }
        #endregion </Type>

        #region <Methods>

        #region <Overrids>

        #region <Input/Output>
        protected override bool GetBusySignalIndex(int index, ref int indexOfDigital)
        {
            indexOfDigital = (int)Define.DefineEnumProject.DigitalIO.PWA500BIN.EN_DIGITAL_IN.ROBOT_BUSY_STATUS;
            return true;
        }
        protected override bool GetAlarmSignalIndex(int index, ref int indexOfDigital)
        {
            indexOfDigital = (int)Define.DefineEnumProject.DigitalIO.PWA500BIN.EN_DIGITAL_IN.ROBOT_ALARM_STATUS;
            return true;
        }
        protected override bool GetServoSignalIndex(int index, ref int indexOfDigital)
        {
            indexOfDigital = (int)Define.DefineEnumProject.DigitalIO.PWA500BIN.EN_DIGITAL_IN.ROBOT_SERVO_ON_OFF_STATUS;
            return true;
        }
        #endregion </Input/Output>

        #region <Scenario>
        protected override void InitScenarioInfoPick()
        {
            QueuedScenarioForCoreSubstrate.Clear();
            QueuedScenarioForBinSubstrate.Clear();

            if (false == _scenarioOperator.UseScenario)
                return;

            // 2024.10.30. jhlim [MOD] 트랙인보다 레시피 다운로드가 선행되어야 해서 변경
            bool isManual = (false == EquipmentState_.EquipmentState.GetInstance().GetState().Equals(EquipmentState_.EQUIPMENT_STATE.EXECUTING));
            if (false == GetWorkingInformation(isManual, ref _workingInfo, ref _temporaryDescription))
                return;

            var location = _workingInfo.Location;
            if (!(location is LoadPortLocation lpLocation))
                return;

            #region <Track in or lot match>
            int portId = lpLocation.PortId;
            Substrate substrate = new Substrate("");
            if (false == _substrateManager.GetSubstrate(location, "", ref substrate))
                return;

            string substrateName = substrate.GetName();
            string subType = substrate.GetAttribute(PWA500BINSubstrateAttributes.SubstrateType);

            SubstrateType substrateType = SubstrateType.Bin1;
            if (false == GetSubstrateTypeByAttribute(subType, ref substrateType))
                return;

            string sourceCarrierId = _carrierServer.GetCarrierId(portId);
            switch (substrateType)
            {
                case SubstrateType.Core:
                    {
                        if (_substrateManager.IsFirstSubstrateAtLoadPort(sourceCarrierId, portId, substrateName))
                        {
                            bool useDownloadingRecipe = GetParameter(PARAM_EQUIPMENT.UseRecipeDownload, true);
                            if (useDownloadingRecipe)
                            {
                                QueuedScenarioForCoreSubstrate.Enqueue(ScenarioRecipeDownload);
                            }
                            QueuedScenarioForCoreSubstrate.Enqueue(ScenarioCoreTrackIn);
                        }
                        
                        bool isLast = _substrateManager.IsLastSubstrateAtLoadPort(portId, substrateName);
                        substrate.SetAttribute(PWA500BINSubstrateAttributes.IsLastSubstrate, isLast.ToString());
                    }
                    break;
                case SubstrateType.Empty:
                    {
                        if (_substrateManager.IsFirstSubstrateAtLoadPort(sourceCarrierId, portId, substrateName))
                        {
                            QueuedScenarioForBinSubstrate.Enqueue(ScenarioLotMatch);
                        }
                    }
                    break;
            }
            #endregion </Track in or lot match>
        }
        protected override bool UpdateParamToBeforePick()
        {
            if (false == _scenarioOperator.UseScenario)
                return false;

            // 2024.10.30. jhlim [MOD] 트랙인보다 레시피 다운로드가 선행되어야 해서 변경
            bool isManual = (false == EquipmentState_.EquipmentState.GetInstance().GetState().Equals(EquipmentState_.EQUIPMENT_STATE.EXECUTING));
            if (false == GetWorkingInformation(isManual, ref _workingInfo, ref _temporaryDescription))
                return false;

            var location = _workingInfo.Location;
            if (!(location is LoadPortLocation lpLocation))
                return false;

            #region <Track in or lot match>
            int portId = lpLocation.PortId;
            Substrate substrate = new Substrate("");
            if (false == _substrateManager.GetSubstrate(location, "", ref substrate))
                return false;

            //string substrateName = substrate.GetName();
            string subType = substrate.GetAttribute(PWA500BINSubstrateAttributes.SubstrateType);

            SubstrateType substrateType = SubstrateType.Bin1;
            if (false == GetSubstrateTypeByAttribute(subType, ref substrateType))
                return false;

            //string sourceCarrierId = _carrierServer.GetCarrierId(portId);
            switch (substrateType)
            {
                case SubstrateType.Core:
                    {
                        if (QueuedScenarioForCoreSubstrate.Count <= 0)
                            return false;

                        var scenario = QueuedScenarioForCoreSubstrate.Dequeue();
                        InitResult(scenario);
                        switch (scenario)
                        {
                            case ScenarioRecipeDownload:
                                {
                                    Dictionary<string, string> scenarioParam = _scenarioManager.MakeScenarioParamToRecipeDownload(substrate);
                                    if (scenarioParam == null)
                                        return false;

                                    _executingScenario = new QueuedScenarioInfo
                                    {
                                        Scenario = scenario,
                                        ScenarioParams = scenarioParam,
                                        AdditionalParams = new Dictionary<string, string>()
                                        {
                                            [AdditionalParamKeys.KeySubstrateType] = subType
                                        }
                                    };

                                    return _scenarioOperator.UpdateScenarioParam(scenario, scenarioParam);
                                }

                            case ScenarioCoreTrackIn:
                                {
                                    Dictionary<string, string> scenarioParam = _scenarioManager.MakeScenarioParamToCoreTrackIn(portId, substrate);
                                    if (scenarioParam == null)
                                        return false;

                                    _executingScenario = new QueuedScenarioInfo
                                    {
                                        Scenario = scenario,
                                        ScenarioParams = scenarioParam,
                                        AdditionalParams = new Dictionary<string, string>()
                                        {
                                            [AdditionalParamKeys.KeySubstrateType] = subType
                                        }
                                    };

                                    return _scenarioOperator.UpdateScenarioParam(scenario, scenarioParam);
                                }
                            default:
                                return false;
                        }
                    }
                case SubstrateType.Empty:
                    {
                        if (QueuedScenarioForBinSubstrate.Count <= 0)
                            return false;

                        var scenario = QueuedScenarioForBinSubstrate.Dequeue();
                        InitResult(ScenarioLotMatch);
                        
                        if (false == scenario.Equals(ScenarioLotMatch))
                            return false;

                        // 매 장 LotMatch 발생한다.
                        string lotId = _carrierServer.GetCarrierLotId(portId);
                        string carrierId = _carrierServer.GetCarrierId(portId);
                        Dictionary<string, string> scenarioParam = _scenarioManager.MakeScenarioParamToLotMatch(portId, lotId, carrierId);
                        if (scenarioParam == null)
                            return false;

                        _executingScenario = new QueuedScenarioInfo
                        {
                            Scenario = scenario,
                            ScenarioParams = scenarioParam
                        };

                        return _scenarioOperator.UpdateScenarioParam(scenario, scenarioParam);
                    }

                default:
                    return false;
            }
            #endregion </Track in or lot match>

            #region <Original #2>
            #region <Track in or lot match>
            //int portId = lpLocation.PortId;
            //Substrate substrate = new Substrate("");
            //if (false == _substrateManager.GetSubstrate(location, "", ref substrate))
            //    return false;

            //string substrateName = substrate.GetName();
            //string subType = substrate.GetAttribute(PWA500BINSubstrateAttributes.SubstrateType);

            //SubstrateType substrateType = SubstrateType.Bin1;
            //if (false == GetSubstrateTypeByAttribute(subType, ref substrateType))
            //    return false;

            //string sourceCarrierId = _carrierServer.GetCarrierId(portId);
            //switch (substrateType)
            //{
            //    case SubstrateType.Core:
            //        {
            //            if (_substrateManager.IsFirstSubstrateAtLoadPort(sourceCarrierId, portId, substrateName))
            //            {
            //                // 첫 자재, 시나리오 최초
            //                if (_executingScenario == null || _executingScenario.Scenario == null)
            //                {
            //                    bool useDownloadingRecipe = GetParameter(PARAM_EQUIPMENT.UseRecipeDownload, true);
                                
            //                    if (false == useDownloadingRecipe)
            //                    {
            //                        // 1. 레시피 다운로드를 하지 않는 경우 TrackIn을 실행한다.
            //                        Dictionary<string, string> scenarioParam = _scenarioManager.MakeScenarioParamToCoreTrackIn(portId, substrate);
            //                        if (scenarioParam == null)
            //                            return false;

            //                        InitResult(ScenarioCoreTrackIn);
            //                        _executingScenario = new QueuedScenarioInfo();
            //                        _executingScenario.Scenario = ScenarioCoreTrackIn;
            //                        _executingScenario.ScenarioParams = scenarioParam;
            //                        _executingScenario.AdditionalParams = new Dictionary<string, string>()
            //                        {
            //                            [AdditionalParamKeys.KeySubstrateType] = subType
            //                        };

            //                        return _scenarioOperator.UpdateScenarioParam(ScenarioCoreTrackIn, scenarioParam);
            //                    }
            //                    else
            //                    {
            //                        // 2. 레시피 다운로드를 하는 경우
            //                        Dictionary<string, string> scenarioParam = _scenarioManager.MakeScenarioParamToRecipeDownload(substrate);
            //                        if (scenarioParam == null)
            //                            return false;

            //                        InitResult(ScenarioRecipeDownload);
            //                        _executingScenario = new QueuedScenarioInfo();
            //                        _executingScenario.Scenario = ScenarioRecipeDownload;
            //                        _executingScenario.ScenarioParams = scenarioParam;
            //                        _executingScenario.AdditionalParams = new Dictionary<string, string>()
            //                        {
            //                            [AdditionalParamKeys.KeySubstrateType] = subType
            //                        };

            //                        return _scenarioOperator.UpdateScenarioParam(ScenarioCoreTrackIn, scenarioParam);
            //                    }
            //                }
            //                else
            //                {
            //                    // Recipe Download가 실행되었으니 TrackIn을 실행한다.
            //                    ScenarioListTypes typeOfScenario = (ScenarioListTypes)_executingScenario.Scenario;
            //                    if (typeOfScenario == ScenarioListTypes.SCENARIO_REQ_RECIPE_DOWNLOAD)
            //                    {
            //                        Dictionary<string, string> scenarioParam = _scenarioManager.MakeScenarioParamToCoreTrackIn(portId, substrate);
            //                        if (scenarioParam == null)
            //                            return false;

            //                        InitResult(ScenarioCoreTrackIn);
            //                        _executingScenario = new QueuedScenarioInfo();
            //                        _executingScenario.Scenario = ScenarioCoreTrackIn;
            //                        _executingScenario.ScenarioParams = scenarioParam;
            //                        _executingScenario.AdditionalParams = new Dictionary<string, string>()
            //                        {
            //                            [AdditionalParamKeys.KeySubstrateType] = subType
            //                        };

            //                        return _scenarioOperator.UpdateScenarioParam(ScenarioCoreTrackIn, scenarioParam);
            //                    }
            //                    else
            //                    {
            //                        // Recipe Download -> Trackin -> Pick Scenario 순서
            //                        _executingScenario = null;
            //                        return false;
            //                    }
            //                }       
            //            }
            //            else
            //            {
            //                bool isLast = _substrateManager.IsLastSubstrateAtLoadPort(portId, substrateName);
            //                substrate.SetAttribute(PWA500BINSubstrateAttributes.IsLastSubstrate, isLast.ToString());

            //                return false;
            //            }
            //        }
            //    case SubstrateType.Empty:
            //        {
            //            if (_executingScenario != null &&
            //                _executingScenario.Scenario != null)
            //            {
            //                // 이미 1회 실행 되고 온거다.
            //                _executingScenario = null;
            //                return false;
            //            }

            //            // 매 장 LotMatch 발생한다.
            //            string lotId = _carrierServer.GetCarrierLotId(portId);
            //            string carrierId = _carrierServer.GetCarrierId(portId);
            //            Dictionary<string, string> scenarioParam = _scenarioManager.MakeScenarioParamToLotMatch(portId, lotId, carrierId);
            //            if (scenarioParam == null)
            //                return false;

            //            InitResult(ScenarioLotMatch);

            //            _executingScenario = new QueuedScenarioInfo();
            //            _executingScenario.Scenario = ScenarioLotMatch;
            //            _executingScenario.ScenarioParams = scenarioParam;

            //            return _scenarioOperator.UpdateScenarioParam(ScenarioLotMatch, scenarioParam);
            //        }

            //    default:
            //        return false;
            //}
            #endregion </Track in or lot match>
            #endregion </Original #2>

            #region <Original>
            //if (_executingScenario != null &&
            //    _executingScenario.Scenario != null)
            //{
            //    // 이미 1회 실행 되고 온거다.
            //    _executingScenario = null;
            //    return false;
            //}

            //bool isManual = (false == EquipmentState_.EquipmentState.GetInstance().GetState().Equals(EquipmentState_.EQUIPMENT_STATE.EXECUTING));
            //if (false == GetWorkingInformation(isManual, ref _workingInfo, ref _temporaryDescription))
            //    return false;

            //var location = _workingInfo.Location;
            //if (!(location is LoadPortLocation lpLocation))
            //    return false;

            //#region <Track in or lot match>
            //int portId = lpLocation.PortId;
            //Substrate substrate = new Substrate("");
            //if (false == _substrateManager.GetSubstrate(location, "", ref substrate))
            //    return false;

            //string substrateName = substrate.GetName();
            //string subType = substrate.GetAttribute(PWA500BINSubstrateAttributes.SubstrateType);

            //SubstrateType substrateType = SubstrateType.Bin1;
            //if (false == GetSubstrateTypeByAttribute(subType, ref substrateType))
            //    return false;

            //string sourceCarrierId = _carrierServer.GetCarrierId(portId);
            //switch (substrateType)
            //{
            //    case SubstrateType.Core:
            //        {
            //            if (_substrateManager.IsFirstSubstrateAtLoadPort(sourceCarrierId, portId, substrateName))
            //            {
            //                // 시나리오상 최초에 한 번만 발생한다...
            //                Dictionary<string, string> scenarioParam = new Dictionary<string, string>();
            //                if (false == MakeScenarioParamToCoreTrackIn(portId, substrate, ref scenarioParam))
            //                    return false;

            //                InitResult(ScenarioCoreTrackIn);
            //                _executingScenario = new QueuedScenarioInfo();
            //                _executingScenario.Scenario = ScenarioCoreTrackIn;
            //                _executingScenario.ScenarioParams = scenarioParam;
            //                _executingScenario.AdditionalParams = new Dictionary<string, string>()
            //                {
            //                    [AdditionalParamKeys.KeySubstrateType] = subType
            //                };

            //                return _scenarioOperator.UpdateScenarioParam(ScenarioCoreTrackIn, scenarioParam);
            //            }
            //            else
            //            {
            //                bool isLast = _substrateManager.IsLastSubstrateAtLoadPort(portId, substrateName);
            //                substrate.SetAttribute(PWA500BINSubstrateAttributes.IsLastSubstrate, isLast.ToString());

            //                return false;
            //            }
            //        }
            //    case SubstrateType.Empty:
            //        {
            //            // 매 장 LotMatch 발생한다.
            //            Dictionary<string, string> scenarioParam = new Dictionary<string, string>();
            //            string lotId = _carrierServer.GetCarrierLotId(portId);
            //            string carrierId = _carrierServer.GetCarrierId(portId);
            //            if (false == MakeScenarioParamToLotMatch(portId, lotId, carrierId, ref scenarioParam))
            //                return false;

            //            InitResult(ScenarioLotMatch);

            //            _executingScenario = new QueuedScenarioInfo();
            //            _executingScenario.Scenario = ScenarioLotMatch;
            //            _executingScenario.ScenarioParams = scenarioParam;

            //            return _scenarioOperator.UpdateScenarioParam(ScenarioLotMatch, scenarioParam);
            //        }

            //    default:
            //        return false;
            //}
            //#endregion </Track in or lot match>
            #endregion </Original>

            // 2024.10.30. jhlim [END]
        }
        protected override CommandResults ExecuteScenarioToBeforePick()
        {
            return RunScenario(_executingScenario.Scenario);
        }
        protected override bool UpdateParamToAfterPick()
        {
            return false;
        }
        protected override CommandResults ExecuteScenarioToAfterPick()
        {
            return RunScenario(_executingScenario.Scenario);
        }
        protected override void InitScenarioInfoPlace()
        {
            QueuedScenarioForCoreSubstrate.Clear();
            QueuedScenarioForBinSubstrate.Clear();

            if (false == _scenarioOperator.UseScenario)
                return;

            bool isManual = (false == EquipmentState_.EquipmentState.GetInstance().GetState().Equals(EquipmentState_.EQUIPMENT_STATE.EXECUTING));
            if (false == GetWorkingInformation(isManual, ref _workingInfo, ref _temporaryDescription))
                return;

            var location = _workingInfo.Location;
            Substrate substrate = new Substrate("");
            if (false == _substrateManager.GetSubstrateAtRobot(RobotName, _workingInfo.ActionArm, ref substrate))
                return;

            string subType = substrate.GetAttribute(PWA500BINSubstrateAttributes.SubstrateType);

            SubstrateType substrateType = SubstrateType.Core;
            if (false == GetSubstrateTypeByAttribute(subType, ref substrateType))
                return;

            switch (substrateType)
            {
                case SubstrateType.Core:
                    {
                        if (location is ProcessModuleLocation)
                        {
                            QueuedScenarioForCoreSubstrate.Enqueue(ScenarioProcessStart);
                        }
                    }
                    break;

                case SubstrateType.Bin1:
                case SubstrateType.Bin2:
                case SubstrateType.Bin3:
                    {
                        if (location is LoadPortLocation)
                        {
                            // Initialize Flags
                            _newSubstrateId = string.Empty;
                            _newPartId = string.Empty;

                            // 스텝을 가져온다.
                            int unloadingStep = GetUnloadingStep(ref substrate);
                            switch (unloadingStep)
                            {
                                case (int)UnloadingStepTypes.Init:
                                    {
                                        // 1. Id Assignment Event
                                        QueuedScenarioForBinSubstrate.Enqueue(ScenarioBinWaferIdAssign);
                                        // 2. Send to PM Assigned Id
                                        QueuedScenarioForBinSubstrate.Enqueue(ScenarioSendClientToBinWaferIdAssign);
                                        // 3. Bin Work End Event
                                        QueuedScenarioForBinSubstrate.Enqueue(ScenarioBinWorkEnd);
                                        // 4. Bin Track Out Event
                                        QueuedScenarioForBinSubstrate.Enqueue(ScnearioBinTrackOut);
                                        // 5. Request Part Id Info
                                        QueuedScenarioForBinSubstrate.Enqueue(ScenarioReqBinPartId);
                                        // 6. Request Bin data to PWA500BIN
                                        QueuedScenarioForBinSubstrate.Enqueue(ScenarioSendClientUploadBinFile);
                                        // 7. Upload Bin Data Event
                                        QueuedScenarioForBinSubstrate.Enqueue(ScenarioUploadBinData);
                                    }
                                    break;
                                case (int)UnloadingStepTypes.AfterIdAssignment:
                                    {
                                        // AssignWaferId 이후
                                        // 2. Send to PM Assigned Id
                                        QueuedScenarioForBinSubstrate.Enqueue(ScenarioSendClientToBinWaferIdAssign);
                                        // 3. Bin Work End Event
                                        QueuedScenarioForBinSubstrate.Enqueue(ScenarioBinWorkEnd);
                                        // 4. Bin Track Out Event
                                        QueuedScenarioForBinSubstrate.Enqueue(ScnearioBinTrackOut);
                                        // 5. Request Part Id Info
                                        QueuedScenarioForBinSubstrate.Enqueue(ScenarioReqBinPartId);
                                        // 6. Request Bin data to PWA500BIN
                                        QueuedScenarioForBinSubstrate.Enqueue(ScenarioSendClientUploadBinFile);
                                        // 7. Upload Bin Data Event
                                        QueuedScenarioForBinSubstrate.Enqueue(ScenarioUploadBinData);
                                    }
                                    break;
                                case (int)UnloadingStepTypes.AfterRequestPartId:
                                    {
                                        // RequestPartId 이후
                                        // 6. Request Bin data to PWA500BIN
                                        QueuedScenarioForBinSubstrate.Enqueue(ScenarioSendClientUploadBinFile);
                                        // 7. Upload Bin Data Event
                                        QueuedScenarioForBinSubstrate.Enqueue(ScenarioUploadBinData);
                                    }
                                    break;

                                default:
                                    break;
                            }

                            #region <원본>
                            //// 1. Id Assignment Event
                            //QueuedScenarioForBinSubstrate.Enqueue(ScenarioBinWaferIdAssign);
                            //// 2. Send to PM Assigned Id
                            //QueuedScenarioForBinSubstrate.Enqueue(ScenarioSendClientToBinWaferIdAssign);
                            //// 3. Bin Work End Event
                            //QueuedScenarioForBinSubstrate.Enqueue(ScenarioBinWorkEnd);
                            //// 4. Bin Track Out Event
                            //QueuedScenarioForBinSubstrate.Enqueue(ScnearioBinTrackOut);
                            //// 5. Request Part Id Info
                            //QueuedScenarioForBinSubstrate.Enqueue(ScenarioReqBinPartId);
                            //// 6. Request Bin data to PWA500BIN
                            //QueuedScenarioForBinSubstrate.Enqueue(ScenarioSendClientUploadBinFile);
                            //// 7. Upload Bin Data Event
                            //QueuedScenarioForBinSubstrate.Enqueue(ScenarioUploadBinData);
                            #endregion </원본>
                        }
                    }
                    break;
                default:
                    break;
            }
        }
        protected override bool UpdateParamToBeforePlace()
        {
            if (false == _scenarioOperator.UseScenario)
                return false;

            bool isManual = (false == EquipmentState_.EquipmentState.GetInstance().GetState().Equals(EquipmentState_.EQUIPMENT_STATE.EXECUTING));
            if (false == GetWorkingInformation(isManual, ref _workingInfo, ref _temporaryDescription))
                return false;

            var location = _workingInfo.Location;
            if (location is LoadPortLocation)
            {
                var lpLocation = location as LoadPortLocation;
                int portId = lpLocation.PortId;
                int slot = lpLocation.Slot;
                Substrate substrate = new Substrate("");
                if (false == _substrateManager.GetSubstrateAtRobot(RobotName, _workingInfo.ActionArm, ref substrate))
                    return false;

                string subType = substrate.GetAttribute(PWA500BINSubstrateAttributes.SubstrateType);

                SubstrateType substrateType = SubstrateType.Core;
                if (false == GetSubstrateTypeByAttribute(subType, ref substrateType))
                    return false;

                switch (substrateType)
                {
                    case SubstrateType.Bin1:
                    case SubstrateType.Bin2:
                    case SubstrateType.Bin3:
                        {
                            if (QueuedScenarioForBinSubstrate.Count <= 0)
                                return false;

                            // Id Assignment Event -> Send to PM Assigned Id -> Bin Work End Event -> Bin Track Out Event -> Request Part Id Info -> Uploading Bin File Event..
                            var scenario = QueuedScenarioForBinSubstrate.Dequeue();
                            InitResult(scenario);
                            switch (scenario)
                            {
                                case ScenarioBinWaferIdAssign:
                                    {
                                        #region <1. Id Assignment Event>
                                        Dictionary<string, string> scenarioParam
                                            = _scenarioManager.MakeScenarioParamToAssignSubstrateId(portId, slot, substrateType, substrate);
                                        if (scenarioParam == null)
                                            return false;

                                        _executingScenario = new QueuedScenarioInfo
                                        {
                                            Scenario = ScenarioBinWaferIdAssign,
                                            ScenarioParams = scenarioParam
                                        };

                                        return _scenarioOperator.UpdateScenarioParam(ScenarioBinWaferIdAssign, scenarioParam);
                                        #endregion </1. Id Assignment Event>        
                                    }
                                    
                                case ScenarioSendClientToBinWaferIdAssign:
                                    {
                                        int currentStep = GetUnloadingStep(ref substrate);
                                        if (currentStep == (int)UnloadingStepTypes.Init)
                                        {
                                            // Step 증가
                                            int nextStep = (int)UnloadingStepTypes.AfterIdAssignment;
                                            substrate.SetAttribute(PWA500BINSubstrateAttributes.BinUnloadingStep, nextStep.ToString());
                                        }
                                        else
                                        {
                                            // 재시도일 것이다.
                                            _newSubstrateId = substrate.GetName();
                                        }

                                        #region <2. Send to PM Assigned Id : 공정설비에 할당받은 결과를 전달한다.>
                                        string ringId = substrate.GetAttribute(PWA500BINSubstrateAttributes.RingId);

                                        _lotHistoryLog.WriteSubstrateHistoryForAssignSubstrateId(portId, ringId, _newSubstrateId);

                                        // 서버에서 받은 이름을 이 웨이퍼의 이름으로 설정한다.
                                        substrate.SetName(_newSubstrateId);
                                        //substrate.SetAttribute(PWA500BINSubstrateAttributes.RingId, ringId);

                                        var scenarioParam = _scenarioManager.MakeScenarioParamToSendingAssignId(_newSubstrateId, ringId);
                                        if (scenarioParam == null)
                                            return false;

                                        _executingScenario = new QueuedScenarioInfo
                                        {
                                            Scenario = ScenarioSendClientToBinWaferIdAssign,
                                            ScenarioParams = scenarioParam
                                        };
                                        return _scenarioOperator.UpdateScenarioParam(_executingScenario.Scenario, scenarioParam);
                                        #endregion </2. Send to PM Assigned Id : 공정설비에 할당받은 결과를 전달한다.>                                            
                                    }
                                    
                                case ScenarioBinWorkEnd:
                                    {
                                        #region <3. Bin Work End Event : Bin Work End Event를 발생시킨다.>
                                        string substrateId = substrate.GetName();
                                        string ringId = substrate.GetAttribute(PWA500BINSubstrateAttributes.RingId);

                                        var scenarioParam = _scenarioManager.MakeScenarioParamToBinWorkEnd(substrateId, ringId, true);
                                        if (scenarioParam == null)
                                            return false;

                                        _executingScenario = new QueuedScenarioInfo
                                        {
                                            Scenario = ScenarioBinWorkEnd,
                                            ScenarioParams = scenarioParam
                                        };

                                        return _scenarioOperator.UpdateScenarioParam(_executingScenario.Scenario, scenarioParam);
                                        #endregion </3. Bin Work End Event : Bin Work End Event를 발생시킨다.>
                                    }

                                case ScnearioBinTrackOut:
                                    {
                                        #region <4. Bin Track Out Event : Bin Track Out Event를 발생시킨다.>
                                        string substrateId = substrate.GetName();
                                        var scenarioParam = _scenarioManager.MakeScenarioParamToTrackOut(substrateId, "AUTO", false);
                                        if (scenarioParam == null)
                                            return false;

                                        _executingScenario = new QueuedScenarioInfo
                                        {
                                            Scenario = ScnearioBinTrackOut,
                                            ScenarioParams = scenarioParam
                                        };
                                        return _scenarioOperator.UpdateScenarioParam(_executingScenario.Scenario, scenarioParam);
                                        #endregion </4. Bin Track Out Event : Bin Track Out Event를 발생시킨다.>
                                    }
                                    
                                case ScenarioReqBinPartId:
                                    {
                                        #region <5. Request Part Id Info : Track Out 이후 변경된 PartId 를 받기위한 이벤트를 발생시킨다.>
                                        string lotId = substrate.GetLotId();
                                        if (false == _carrierServer.HasCarrier(portId))
                                            return false;
                                        string carrierId = _carrierServer.GetCarrierId(portId);

                                        var scenarioParam = _scenarioManager.MakeScenarioParamToRequestBinPartId(lotId, carrierId);
                                        if (scenarioParam == null)
                                            return false;

                                        _executingScenario = new QueuedScenarioInfo
                                        {
                                            Scenario = ScenarioReqBinPartId,
                                            ScenarioParams = scenarioParam
                                        };

                                        return _scenarioOperator.UpdateScenarioParam(_executingScenario.Scenario, scenarioParam);
                                        #endregion </5. Request Part Id Info : Track Out 이후 변경된 PartId 를 받기위한 이벤트를 발생시킨다.>
                                    }

                                case ScenarioSendClientUploadBinFile:
                                    {
                                        int currentStep = GetUnloadingStep(ref substrate);
                                        if (currentStep == (int)UnloadingStepTypes.AfterIdAssignment)
                                        {
                                            // Step 증가
                                            int nextStep = (int)UnloadingStepTypes.AfterRequestPartId;
                                            substrate.SetAttribute(PWA500BINSubstrateAttributes.BinUnloadingStep, nextStep.ToString());


                                            #region <받은 PartId를 적용한다.>
                                            substrate.SetAttribute(PWA500BINSubstrateAttributes.PartId, _newPartId);
                                            #endregion </받은 PartId를 적용한다.>
                                        }
                                        #region <6. Uploading Bin File Event>

                                        // 매 장 BinFile Upload 발생이 필요하다.
                                        Dictionary<string, string> scenarioParam = _scenarioManager.MakeScenarioParamToUploadBinFile(portId, slot, Work.AppConfigManager.Instance.MachineName, substrate);
                                        if (scenarioParam == null)
                                            return false;

                                        _executingScenario = new QueuedScenarioInfo
                                        {
                                            Scenario = ScenarioSendClientUploadBinFile,
                                            ScenarioParams = scenarioParam
                                        };

                                        // 공정설비에 맵 데이터 요청 메시지 전달
                                        // TODO : 시나리오 결과를 받아 섭에 적용하기 구현 필요
                                        return _scenarioOperator.UpdateScenarioParam(_executingScenario.Scenario, scenarioParam);
                                        #endregion </6. Uploading Bin File Event>
                                    }

                                case ScenarioUploadBinData:
                                    {
                                        if (_scenarioManager.GetBinDataToUpload(ref _binDataToUpload) || IsSimulation())
                                        {
                                            if (IsSimulation())
                                            {
                                                string name = substrate.GetName();
                                                string ringId = substrate.GetAttribute(PWA500BINSubstrateAttributes.RingId);
                                                string qty = substrate.GetAttribute(PWA500BINSubstrateAttributes.ChipQty);
                                                int.TryParse(qty, out int chipQty);
                                                string angle = substrate.GetAttribute(PWA500BINSubstrateAttributes.Angle);
                                                double.TryParse(angle, out double waferAngle);
                                                string row = substrate.GetAttribute(PWA500BINSubstrateAttributes.CountY);
                                                int.TryParse(row, out int countRow);
                                                string col = substrate.GetAttribute(PWA500BINSubstrateAttributes.CountX);
                                                int.TryParse(col, out int countCol);
                                                string nullBinCode = " ";
                                                string mapData = "12345";
                                                string pmsFileBody = "TEST_PMS";
                                                _binDataToUpload = new BinDataToUploadFromPWA500BIN("MAIN",
                                                    name,
                                                    ringId,
                                                    chipQty,
                                                    waferAngle,
                                                    countRow,
                                                    countCol,
                                                    nullBinCode,
                                                    mapData,
                                                    pmsFileBody,
                                                    "AUTO",
                                                    true);
                                            }
                                            
                                            Dictionary<string, string> scenarioParam = _scenarioManager.MakeScenarioParamToUploadBinData(
                                                _binDataToUpload.NameOfEq,
                                                _binDataToUpload.SubstrateId,
                                                _binDataToUpload.RingId,
                                                _binDataToUpload.ChipQty,
                                                _binDataToUpload.Angle,
                                                _binDataToUpload.CountRow,
                                                _binDataToUpload.CountCol,
                                                _binDataToUpload.NullBinCode,
                                                _binDataToUpload.MapData,
                                                _binDataToUpload.PmsFileBody,
                                                _binDataToUpload.UserId,
                                                _binDataToUpload.UseEventHandling);

                                            if (scenarioParam == null)
                                                return false;

                                            _executingScenario = new QueuedScenarioInfo
                                            {
                                                Scenario = ScenarioUploadBinData,
                                                ScenarioParams = scenarioParam
                                            };

                                            // 공정설비에 맵 데이터 요청 메시지 전달 -> 콜백에서 UploadBinData Event 발생
                                            return _scenarioOperator.UpdateScenarioParam(_executingScenario.Scenario, scenarioParam);
                                        }

                                        return false;
                                    }

                                default:
                                    return false;
                            }

                            #region <Original Code>
                            //if (_executingScenario == null || _executingScenario.Scenario == null)
                            //{
                            //    #region <1. Id Assignment Event>
                            //    Dictionary<string, string> scenarioParam
                            //        = _scenarioManager.MakeScenarioParamToAssignSubstrateId(portId, slot, substrateType, substrate);
                            //    if (scenarioParam == null)
                            //        return false;

                            //    InitResult(ScenarioBinWaferIdAssign);

                            //    _executingScenario = new QueuedScenarioInfo();
                            //    _executingScenario.Scenario = ScenarioBinWaferIdAssign;
                            //    _executingScenario.ScenarioParams = scenarioParam;

                            //    return _scenarioOperator.UpdateScenarioParam(ScenarioBinWaferIdAssign, scenarioParam);
                            //    #endregion </1. Id Assignment Event>
                            //}
                            //else
                            //{
                            //    if (_executingScenario.Scenario == null)
                            //        return false;

                            //    if (false == _executingScenario.Scenario.GetType().Equals(typeof(ScenarioListTypes)))
                            //        return false;

                            //    ScenarioListTypes typeOfScenario = (ScenarioListTypes)_executingScenario.Scenario;
                            //    switch (typeOfScenario)
                            //    {
                            //        case ScenarioListTypes.SCENARIO_REQ_BIN_WAFER_ID_ASSIGN:
                            //            {
                            //                #region <2. Send to PM Assigned Id : 공정설비에 할당받은 결과를 전달한다.>
                            //                // Substrate Id 할당된 것을 전달
                            //                var resultOfScenario = _scenarioOperator.GetScenarioResultData(typeOfScenario);
                            //                if (false == resultOfScenario.TryGetValue(AssignSubstrateIdKeys.KeyResultSubstrateId,
                            //                    out string newSubstrateId))
                            //                {
                            //                    return false;
                            //                }

                            //                // 이전 이름이 링아이디다.
                            //                string ringId = substrate.GetName();
                            //                substrate.SetName(newSubstrateId);
                            //                substrate.SetAttribute(PWA500BINSubstrateAttributes.RingId, ringId);

                            //                InitResult(ScenarioSendClientToBinWaferIdAssign);
                            //                var scenarioParam = _scenarioManager.MakeScenarioParamToSendingAssignId(newSubstrateId, ringId);
                            //                if (scenarioParam == null)
                            //                    return false;

                            //                _executingScenario = new QueuedScenarioInfo
                            //                {
                            //                    Scenario = ScenarioSendClientToBinWaferIdAssign,
                            //                    ScenarioParams = scenarioParam
                            //                };
                            //                InitResult(ScenarioSendClientToBinWaferIdAssign);
                            //                return _scenarioOperator.UpdateScenarioParam(_executingScenario.Scenario, scenarioParam);
                            //                #endregion </2. Send to PM Assigned Id : 공정설비에 할당받은 결과를 전달한다.>                                            
                            //            }
                            //        case ScenarioListTypes.SCENARIO_ASSIGN_SUBSTRATE_ID:
                            //            {
                            //                #region <3. Bin Work End Event : Bin Work End Event를 발생시킨다.>
                            //                string substrateId = substrate.GetName();
                            //                string ringId = substrate.GetAttribute(PWA500BINSubstrateAttributes.RingId);

                            //                var scenarioParam = _scenarioManager.MakeScenarioParamToBinWorkEnd(substrateId, ringId, true);
                            //                if (scenarioParam == null)
                            //                    return false;

                            //                _executingScenario = new QueuedScenarioInfo
                            //                {
                            //                    Scenario = ScenarioBinWorkEnd,
                            //                    ScenarioParams = scenarioParam
                            //                };
                            //                InitResult(ScenarioBinWorkEnd);
                            //                return _scenarioOperator.UpdateScenarioParam(_executingScenario.Scenario, scenarioParam);
                            //                #endregion </3. Bin Work End Event : Bin Work End Event를 발생시킨다.>
                            //            }

                            //        case ScenarioListTypes.SCENARIO_BIN_WORK_END:
                            //            {
                            //                #region <4. Bin Track Out Event : Bin Track Out Event를 발생시킨다.>
                            //                string substrateId = substrate.GetName();
                            //                var scenarioParam = _scenarioManager.MakeScenarioParamToTrackOut(substrateId, "AUTO", true);
                            //                if (scenarioParam == null)
                            //                    return false;

                            //                _executingScenario = new QueuedScenarioInfo
                            //                {
                            //                    Scenario = ScnearioBinTrackOut,
                            //                    ScenarioParams = scenarioParam
                            //                };
                            //                InitResult(ScnearioBinTrackOut);
                            //                return _scenarioOperator.UpdateScenarioParam(_executingScenario.Scenario, scenarioParam);
                            //                #endregion </4. Bin Track Out Event : Bin Track Out Event를 발생시킨다.>
                            //            }

                            //        case ScenarioListTypes.SCENARIO_REQ_BIN_WAFER_TRACK_OUT:
                            //            {
                            //                #region <5. Request Part Id Info : Track Out 이후 변경된 PartId 를 받기위한 이벤트를 발생시킨다.>
                            //                string lotId = substrate.GetLotId();
                            //                if (false == _carrierServer.HasCarrier(portId))
                            //                    return false;
                            //                string carrierId = _carrierServer.GetCarrierId(portId);

                            //                var scenarioParam = _scenarioManager.MakeScenarioParamToRequestBinPartId(lotId, carrierId);
                            //                if (scenarioParam == null)
                            //                    return false;

                            //                _executingScenario = new QueuedScenarioInfo
                            //                {
                            //                    Scenario = ScenarioReqBinPartId,
                            //                    ScenarioParams = scenarioParam
                            //                };

                            //                InitResult(ScenarioReqBinPartId);
                            //                return _scenarioOperator.UpdateScenarioParam(_executingScenario.Scenario, scenarioParam);
                            //                #endregion </5. Request Part Id Info : Track Out 이후 변경된 PartId 를 받기위한 이벤트를 발생시킨다.>
                            //            }

                            //        case ScenarioListTypes.SCENARIO_BIN_PART_ID_INFO_REQ:
                            //            {
                            //                #region <받은 PartId를 적용한다.>
                            //                var scenarioResult = _scenarioOperator.GetScenarioResultData(typeOfScenario);
                            //                if (scenarioResult.TryGetValue(LotInfoKeys.KeyResultPartId, out string partId))
                            //                {
                            //                    substrate.SetAttribute(PWA500BINSubstrateAttributes.PartId, partId);
                            //                }
                            //                #endregion </받은 PartId를 적용한다.>

                            //                #region <6. Uploading Bin File Event>

                            //                // 매 장 BinFile Upload 발생이 필요하다.
                            //                Dictionary<string, string> scenarioParam = _scenarioManager.MakeScenarioParamToUploadBinFile(portId, slot, Work.AppConfigManager.Instance.MachineName, substrate);
                            //                if (scenarioParam == null)
                            //                    return false;

                            //                InitResult(ScenarioUploadBinFile);

                            //                _executingScenario = new QueuedScenarioInfo
                            //                {
                            //                    Scenario = ScenarioUploadBinFile,
                            //                    ScenarioParams = scenarioParam
                            //                };

                            //                // 공정설비에 맵 데이터 요청 메시지 전달 -> 콜백에서 UploadBinData Event 발생
                            //                return _scenarioOperator.UpdateScenarioParam(_executingScenario.Scenario, scenarioParam);
                            //                #endregion </6. Uploading Bin File Event>
                            //            }

                            //        //case ScenarioListTypes.SCENARIO_ASSIGN_SUBSTRATE_ID:
                            //        //{
                            //        //    #region <3. BinFileUpload>
                            //        //    // 매 장 BinFile Upload 발생이 필요하다.
                            //        //    string lotId = _carrierServer.GetCarrierLotId(portId);
                            //        //    string carrierId = _carrierServer.GetCarrierId(portId);
                            //        //    Dictionary<string, string> scenarioParam = _scenarioManager.MakeScenarioParamToUploadBinFile(portId, slot, Work.AppConfigManager.Instance.MachineName, substrate);
                            //        //    if (scenarioParam == null)    
                            //        //        return false;

                            //        //    InitResult(ScenarioUploadBinFile);

                            //        //    _executingScenario = new QueuedScenarioInfo();
                            //        //    _executingScenario.Scenario = ScenarioUploadBinFile;
                            //        //    _executingScenario.ScenarioParams = scenarioParam;

                            //        //    return _scenarioOperator.UpdateScenarioParam(ScenarioUploadBinFile, scenarioParam);
                            //        //    #endregion </3. BinFileUpload>
                            //        //}

                            //        case ScenarioListTypes.SCENARIO_REQ_UPLOAD_BINFILE:
                            //            {
                            //                // 완료되었으니 초기화
                            //                _executingScenario = null;
                            //            }
                            //            return false;

                            //        default:
                            //            return false;
                            //    }
                            //}
                            #endregion </Original Code>
                        }

                    default:
                        return false;
                }
            }
            else
            {
                return false;
            }            
        }
        protected override CommandResults ExecuteScenarioToBeforePlace()
        {
            return RunScenario(_executingScenario.Scenario);
        }
        protected override bool UpdateParamToAfterPlace()
        {
            if (false == _scenarioOperator.UseScenario)
                return false;

            if (QueuedScenarioForCoreSubstrate.Count <= 0)
                return false;

            var scenario = QueuedScenarioForCoreSubstrate.Dequeue();

            //if (_executingScenario != null &&
            //    _executingScenario.Scenario != null)
            //{
            //    // 이미 1회 실행 되고 온거다.
            //    _executingScenario = null;
            //    return false;
            //}

            bool isManual = (false == EquipmentState_.EquipmentState.GetInstance().GetState().Equals(EquipmentState_.EQUIPMENT_STATE.EXECUTING));
            if (false == GetWorkingInformation(isManual, ref _workingInfo, ref _temporaryDescription))
                return false;

            if (!(_workingInfo.Location is ProcessModuleLocation pmLocation))
                return false;

            Substrate substrate = new Substrate("");
            if (false == _substrateManager.GetSubstrateAtProcessModule(_workingInfo.SubstrateName, pmLocation, ref substrate))
                return false;

            string subType = substrate.GetAttribute(PWA500BINSubstrateAttributes.SubstrateType);

            SubstrateType substrateType = SubstrateType.Core;
            if (false == GetSubstrateTypeByAttribute(subType, ref substrateType))
                return false;

            if (false == substrateType.Equals(SubstrateType.Core))
                return false;

            int portId = substrate.GetSourcePortId();
            string sourceCarrierId = _carrierServer.GetCarrierId(portId);
            if (_substrateManager.IsFirstSubstrateAtLoadPort(sourceCarrierId, portId, substrate.GetName()))
            {
                #region <Process Start>
                var scenarioParam = _scenarioManager.MakeParamToProcessing(portId, substrate);

                InitResult(scenario);
                _executingScenario = new QueuedScenarioInfo
                {
                    Scenario = scenario,
                    ScenarioParams = scenarioParam
                };

                return _scenarioOperator.UpdateScenarioParam(scenario, scenarioParam);
                #endregion </Process Start>
            }

            return false;
        }
        protected override CommandResults ExecuteScenarioToAfterPlace()
        {
            if (false == _scenarioOperator.UseScenario)
            {
                _commandResult.ActionName = "Idle";
                _commandResult.Description = string.Empty;
                _commandResult.CommandResult = CommandResult.Skipped;
            }
            else
            {
                if (_executingScenario != null &&
                    _executingScenario.Scenario != null)
                {
                    return RunScenario(_executingScenario.Scenario);
                }
                else
                {
                    _commandResult.ActionName = "Idle";
                    _commandResult.Description = string.Empty;
                    _commandResult.CommandResult = CommandResult.Skipped;
                }
            }

            return _commandResult;
        }
        #endregion </Scenario>

        #region <Material Handling With Process Module>

        private bool IsTickOver()
        {
            //return false;

            return Ticks.IsTickOver(false);
        }

        #region <Loading>
        protected override void InitMaterialHandlingInterface()
        {            
            _subStepInterface = 0;
        }

        protected override CommandResults IsApproachLoadingPrepared()
        {
            const string MethodName = "IsApproachLoadingPrepared";

            if (false == IsLoadingSignalStillActive(_workingInfo.Location.Name))
                return ReturnSkipped(MethodName);

            switch (_subStepInterface)
            {
                case 0:
                    {
                        //  1. Foup이 준비 되었는지 확인 후 준비 되지 않았으면 Skipped 리턴
                        //     준비되면 스메마 켜고 진행
                        bool prepared;
                        if (_workingInfo.Location is LoadPortLocation)
                        {
                            var lpLocation = _workingInfo.Location as LoadPortLocation;
                            prepared = _carrierServer.HasCarrier(lpLocation.PortId);
                            if (prepared)
                            {
                                // 스메마 켠다.
                                _processGroup.SetLoadingSignal(ProcessModuleIndex, _workingInfo.Location.Name, true);
                                ++_subStepInterface;
                            }
                            else
                            {
                                return ReturnSkipped(MethodName);
                            }
                        }
                        else if (_workingInfo.Location is ProcessModuleLocation)
                        {
                            // 스메마 켠다.
                            _processGroup.SetLoadingSignal(ProcessModuleIndex, _workingInfo.Location.Name, true);
                            ++_subStepInterface;
                        }
                        else
                        {
                            return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_BEFORE_LOADING_DATA_INVALID, MethodName, _subStepInterface, "Location is invalid");
                        }
                    }
                    break;
                case 1:
                    {
                        // 2. AppreachLoading을 전송
                        if (false == _processGroup.SendMessage(ProcessModuleIndex, _workingInfo.Location,
                            RequestMessages.RequestApproachLoading.ToString(), _workingInfo.SubstrateName))
                        {
                            return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_BEFORE_LOADING_SENDING_FAILED, MethodName,
                                _subStepInterface, RequestMessages.RequestApproachLoading.ToString());
                        }

                        Ticks.SetTickCount(TimeoutShort);
                        ++_subStepInterface;
                    }
                    break;
                case 2:
                    {
                        if (IsTickOver())
                        {
                            return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_BEFORE_LOADING_SENDING_COMPLETED_TIMEOUT_ACK, MethodName,
                                _subStepInterface, RequestMessages.RequestApproachLoading.ToString());
                        }

                        //  3. Ack 확인
                        var result = _processGroup.IsSendingCompleted(ProcessModuleIndex, _workingInfo.Location,
                            RequestMessages.RequestApproachLoading.ToString());
                        switch (result)
                        {
                            case CommunicationResult.Ack:
                                Ticks.SetTickCount(TimeoutLong);
                                ++_subStepInterface;
                                break;
                            case CommunicationResult.Nack:
                            case CommunicationResult.Error:
                                return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_BEFORE_LOADING_SENDING_COMPLETED_BUT_NACK, MethodName,
                                    _subStepInterface, RequestMessages.RequestApproachLoading.ToString());

                            default:
                                break;
                        }

                    }
                    break;
                case 3:
                    {
                        if (IsTickOver())
                        {
                            return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_BEFORE_LOADING_RECEIVING_RESPONSE_MESSAGE_TIMEOUT, MethodName,
                                _subStepInterface, ResponseMessages.ResponseApproachLoading.ToString());
                        }

                        // 4. Response 확인
                        var result = _processGroup.IsMessageReceived(ProcessModuleIndex, _workingInfo.Location,
                            ResponseMessages.ResponseApproachLoading.ToString());
                        switch (result)
                        {
                            case CommunicationResult.Ack:
                                Ticks.SetTickCount(TimeoutShort);
                                ++_subStepInterface;
                                break;

                            case CommunicationResult.Nack:
                            case CommunicationResult.Error:
                                {
                                    return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_BEFORE_LOADING_RECEIVING_COMPLETED_BUT_ERROR, MethodName,
                                        _subStepInterface, ResponseMessages.ResponseApproachLoading.ToString());
                                }

                            default:
                                break;
                        }
                    }
                    break;

                case 4:
                    {
                        if (IsTickOver())
                        {
                            return ReturnToError(CommandResult.Timeout, EN_ALARM.INTERFACE_BEFORE_LOADING_RECEIVING_RESPONSE_DATA_TIMEOUT, MethodName,
                                _subStepInterface, ResponseMessages.ResponseApproachLoading.ToString());
                        }

                        // 5. 데이터 확인
                        Dictionary<string, string> receivedData = new Dictionary<string, string>();
                        if (false == _processGroup.GetReceivedData(ProcessModuleIndex, _workingInfo.Location,
                            ResponseMessages.ResponseApproachLoading.ToString(), ref receivedData))
                            break;

                        // 6. Ack 전송 : 콜백에서 자동 Ack 나가니 현재 미구현
                        //if (false == _processGroup.SetAckReceivedMessage(ProcessModuleIndex, _workingInfo.Location,
                        //    ResponseMessages.ResponseApproachLoading.ToString(), CommunicationResult.Ack, string.Empty))
                        //{

                        //}

                        return ReturnCompleted();
                    }

                default:
                    break;
            }

            return ReturnProceed();
        }
        protected override CommandResults IsApproachLoadingCompleted()
        {
            const string MethodName = "IsApproachLoadingCompleted";

            // 상황이 바뀌었을 수 있다..
            if (false == IsLoadingSignalStillActive(_workingInfo.Location.Name))
                return ReturnSkipped(MethodName);

            return ReturnCompleted();
        }
        protected override CommandResults IsLoadingPrepared()
        {
            const string MethodName = "IsLoadingPrepared";

            // 상황이 바뀌었을 수 있다..
            if (false == IsLoadingSignalStillActive(_workingInfo.Location.Name))
                return ReturnSkipped(MethodName);

            switch (_subStepInterface)
            {
                case 0:
                    {
                        // 1. ActionLoading을 전송
                        if (false == _processGroup.SendMessage(ProcessModuleIndex, _workingInfo.Location,
                            RequestMessages.RequestActionLoading.ToString(), _workingInfo.SubstrateName))
                        {
                            return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_ACTION_LOADING_SENDING_FAILED, MethodName,
                                _subStepInterface, RequestMessages.RequestActionLoading.ToString());
                        }

                        Ticks.SetTickCount(TimeoutShort);
                        ++_subStepInterface;
                    }
                    break;
                case 1:
                    {
                        if (IsTickOver())
                        {
                            return ReturnToError(CommandResult.Timeout, EN_ALARM.INTERFACE_ACTION_LOADING_SENDING_COMPLETED_TIMEOUT_ACK, MethodName,
                                _subStepInterface, RequestMessages.RequestActionLoading.ToString());
                        }

                        //  2. Ack 확인
                        var result = _processGroup.IsSendingCompleted(ProcessModuleIndex, _workingInfo.Location,
                            RequestMessages.RequestActionLoading.ToString());
                        switch (result)
                        {
                            case CommunicationResult.Ack:
                                Ticks.SetTickCount(TimeoutLong);
                                ++_subStepInterface;
                                break;
                            case CommunicationResult.Nack:
                            case CommunicationResult.Error:
                                {
                                    return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_ACTION_LOADING_SENDING_COMPLETED_BUT_NACK, MethodName,
                                        _subStepInterface, RequestMessages.RequestActionLoading.ToString());
                                }
                            default:
                                break;
                        }

                    }
                    break;
                case 2:
                    {
                        if (IsTickOver())
                        {
                            return ReturnToError(CommandResult.Timeout, EN_ALARM.INTERFACE_ACTION_LOADING_RECEIVING_RESPONSE_MESSAGE_TIMEOUT, MethodName,
                                _subStepInterface, ResponseMessages.ResponseActionLoading.ToString());
                        }

                        // 3. Response 확인
                        var result = _processGroup.IsMessageReceived(ProcessModuleIndex, _workingInfo.Location,
                            ResponseMessages.ResponseActionLoading.ToString());
                        switch (result)
                        {
                            case CommunicationResult.Ack:
                                Ticks.SetTickCount(TimeoutShort);
                                ++_subStepInterface;
                                break;

                            case CommunicationResult.Nack:
                            case CommunicationResult.Error:
                                {
                                    return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_ACTION_LOADING_RECEIVING_COMPLETED_BUT_ERROR, MethodName,
                                        _subStepInterface, ResponseMessages.ResponseActionLoading.ToString());
                                }

                            default:
                                break;
                        }
                    }
                    break;

                case 3:
                    {
                        if (IsTickOver())
                        {
                            return ReturnToError(CommandResult.Timeout, EN_ALARM.INTERFACE_ACTION_LOADING_RECEIVING_RESPONSE_DATA_TIMEOUT, MethodName,
                                _subStepInterface, ResponseMessages.ResponseActionLoading.ToString());                            
                        }

                        // 4. 데이터 확인
                        Dictionary<string, string> receivedData = new Dictionary<string, string>();
                        if (false == _processGroup.GetReceivedData(ProcessModuleIndex, _workingInfo.Location,
                            ResponseMessages.ResponseActionLoading.ToString(), ref receivedData))
                            break;

                        // 6. Ack 전송 : 콜백에서 자동 Ack 나가니 현재 미구현
                        //if (false == _processGroup.SetAckReceivedMessage(ProcessModuleIndex, _workingInfo.Location,
                        //    ResponseMessages.ResponseActionLoading.ToString(), CommunicationResult.Ack, string.Empty))
                        //{

                        //}

                        return ReturnCompleted();
                    }

                default:
                    break;
            }

            return ReturnProceed();
        }
        protected override CommandResults IsLoadingCompleted()
        {
            const string MethodName = "IsLoadingCompleted";

            // 상황이 바뀌었을 수 있다..
            if (false == IsLoadingSignalStillActive(_workingInfo.Location.Name))
            {
                return ReturnSkipped(MethodName);
            }

            switch (_subStepInterface)
            {
                case 0:
                    {
                        // 1. ConfirmLoading 전송
                        if (false == _processGroup.SendMessage(ProcessModuleIndex, _workingInfo.Location,
                            RequestMessages.RequestConfirmLoading.ToString(), _workingInfo.SubstrateName))
                        {
                            return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_AFTER_LOADING_SENDING_FAILED, MethodName,
                                _subStepInterface, RequestMessages.RequestConfirmLoading.ToString());
                        }

                            Ticks.SetTickCount(TimeoutShort);
                        ++_subStepInterface;
                    }
                    break;
                case 1:
                    {
                        if (IsTickOver())
                        {
                            return ReturnToError(CommandResult.Timeout, EN_ALARM.INTERFACE_AFTER_LOADING_SENDING_COMPLETED_TIMEOUT_ACK, MethodName,
                                _subStepInterface, RequestMessages.RequestConfirmLoading.ToString());
                        }

                        //  2. Ack 확인
                        var result = _processGroup.IsSendingCompleted(ProcessModuleIndex, _workingInfo.Location,
                            RequestMessages.RequestConfirmLoading.ToString());
                        switch (result)
                        {
                            case CommunicationResult.Ack:
                                Ticks.SetTickCount(TimeoutLong);
                                ++_subStepInterface;
                                break;
                            case CommunicationResult.Nack:
                            case CommunicationResult.Error:
                                {
                                    return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_AFTER_LOADING_SENDING_COMPLETED_BUT_NACK, MethodName,
                                        _subStepInterface, RequestMessages.RequestConfirmLoading.ToString());
                                }
                            default:
                                break;
                        }

                    }
                    break;
                case 2:
                    {
                        if (IsTickOver())
                        {
                            return ReturnToError(CommandResult.Timeout, EN_ALARM.INTERFACE_AFTER_LOADING_RECEIVING_RESPONSE_MESSAGE_TIMEOUT, MethodName,
                                _subStepInterface, ResponseMessages.ResponseConfirmLoading.ToString());
                        }

                        // 3. Response 확인
                        var result = _processGroup.IsMessageReceived(ProcessModuleIndex, _workingInfo.Location,
                            ResponseMessages.ResponseConfirmLoading.ToString());
                        switch (result)
                        {
                            case CommunicationResult.Ack:
                                Ticks.SetTickCount(TimeoutShort);
                                ++_subStepInterface;
                                break;

                            case CommunicationResult.Nack:
                            case CommunicationResult.Error:
                                return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_AFTER_LOADING_RECEIVING_COMPLETED_BUT_ERROR, MethodName,
                                    _subStepInterface, ResponseMessages.ResponseConfirmLoading.ToString());

                            default:
                                break;
                        }
                    }
                    break;

                case 3:
                    {
                        if (IsTickOver())
                        {
                            return ReturnToError(CommandResult.Timeout, EN_ALARM.INTERFACE_AFTER_LOADING_RECEIVING_RESPONSE_DATA_TIMEOUT, MethodName
                                , _subStepInterface, ResponseMessages.ResponseConfirmLoading.ToString());
                        }

                        // 4. 데이터 확인
                        Dictionary<string, string> receivedData = new Dictionary<string, string>();
                        if (false == _processGroup.GetReceivedData(ProcessModuleIndex, _workingInfo.Location,
                            ResponseMessages.ResponseConfirmLoading.ToString(), ref receivedData))
                            break;

                        // 5. Ack 전송 : 콜백에서 자동 Ack 나가니 현재 미구현
                        //if (false == _processGroup.SetAckReceivedMessage(ProcessModuleIndex, _workingInfo.Location,
                        //    ResponseMessages.ResponseConfirmLoading.ToString(), CommunicationResult.Ack, string.Empty))
                        //    return ReturnError(CommandResult.Error, MethodName,
                        //        _subStepInterface, string.Format("Response has failed : {0}", ResponseMessages.ResponseConfirmLoading.ToString()));

                        Ticks.SetTickCount(100);
                        ++_subStepInterface;
                    }
                    break;

                case 4:
                    {
                        if (false == IsTickOver())
                            break;

                        // 6. SMEMA OFF
                        _processGroup.SetLoadingSignal(ProcessModuleIndex, _workingInfo.Location.Name, false);

                        return ReturnCompleted();
                    }

                default:
                    break;
            }

            return ReturnProceed();
        }
        #endregion </Loading>

        #region <Unloading>
        private bool IsUnloadingSignalStillActive(string location)
        {
            //  1) PM의 스메마 확인 후 Off면 Skipped 리턴
            List<string> requestedLocation = new List<string>();
            if (false == _processGroup.IsUnloadingRequested(ProcessModuleIndex, ref requestedLocation))
                return false;

            if (false == requestedLocation.Contains(location))
                return false;

            return true;
        }

        private bool FindSubstrateByAttribute(string substrateName, string portId, string slot, ref Substrate substrate)
        {
            // 1. 해당 Substrate의 정보가 공정 설비에 존재하는 경우(정상)
            if (_substrateManager.GetSubstrateAtProcessModule(ProcessModuleName, substrateName, ref substrate))
                return true;

            // 2. 공정 설비에 있는 Substrate와 Source 정보들을 바탕으로 자재를 매칭(이름이 없고 포트번호, 슬롯번호가 존재하는 경우)
            #region <Find substrate by source info>
            List<Substrate> substrates = new List<Substrate>();
            if (false == _substrateManager.GetSubstratesAtProcessModule(ProcessModuleName, ref substrates))
                return false;

            substrate = null;
            for (int i = 0; i < substrates.Count; ++i)
            {
                if (substrates[i].GetSourcePortId().ToString().Equals(portId) && substrates[i].GetSourceSlot().ToString().Equals(slot))
                {
                    substrate = substrates[i];
                    break;
                }
                if (substrates[i].Equals(substrateName))
                {
                    substrate = substrates[i];
                    break;
                }
            }
            #endregion </Find substrate by source info>

            return substrate != null;
        }

        private bool FindPortInfo(int sourcePortId, SubstrateType substrateType, ref int targetPortId)
        {
            for (int i = 0; i < _loadPortManager.Count; ++i)
            {
                // 2024.09.03. jhlim [MOD] SubType을 UI에는 Center/Left/Right로 지정되도록 변경
                //FrameOfSystem3.Recipe.PARAM_EQUIPMENT paramName;
                //paramName = FrameOfSystem3.Recipe.PARAM_EQUIPMENT.LoadPortType1 + i;
                //string subTypeByRecipe = FrameOfSystem3.Recipe.Recipe.GetInstance().GetValue(FrameOfSystem3.Recipe.EN_RECIPE_TYPE.EQUIPMENT,
                //    paramName.ToString(),
                //    SubstrateType.Empty.ToString());

                //if (false == Enum.TryParse(subTypeByRecipe, out SubstrateType convertedSubType))
                //    continue;
                SubstrateType convertedSubType = _scenarioManager.GetSubstrateTypeByLoadPortIndex(i);
                // 2024.09.03. jhlim [END]

                if (false == substrateType.Equals(convertedSubType))
                    continue;
                if (substrateType == SubstrateType.Bin1 ||
                    substrateType == SubstrateType.Bin2 ||
                    substrateType == SubstrateType.Bin3)
                {
                    if (FrameOfSystem3.Recipe.Recipe.GetInstance().GetValue(FrameOfSystem3.Recipe.EN_RECIPE_TYPE.COMMON, FrameOfSystem3.Recipe.PARAM_COMMON.UseCycleMode.ToString(), false))
                        substrateType = SubstrateType.Empty;
                }

                if (substrateType == SubstrateType.Core ||
                    substrateType == SubstrateType.Empty)
                {
                    if (sourcePortId > 0)
                    {
                        targetPortId = sourcePortId;
                        return true;
                    }
                }

                // 캐리어가 있고, Accessing 된 거만 찾는다.
                int temporaryPortId = _loadPortManager.GetLoadPortPortId(i);
                if (_carrierServer.HasCarrier(temporaryPortId))
                {
                    targetPortId = temporaryPortId;
                    return true;
                }
            }

            return false;
        }

        private bool CheckCarrierExistanceBySubstrateType(int sourcePortId, int sourceSlot, string substrateType, ref int targetPortId, ref int targetSlot, ref SubstrateType targetType)
        {
            if (false == Enum.TryParse(substrateType, out targetType))
                return false;

            bool result = FindPortInfo(sourcePortId, targetType, ref targetPortId);
            if (result)
            {
                switch (targetType)
                {
                    case SubstrateType.Core:
                    case SubstrateType.Empty:
                        {
                            if (sourceSlot < 0)
                            {
                                targetSlot = -1;
                                int capacity = _carrierServer.GetCapacity(targetPortId);
                                for (int i = 0; i < capacity; ++i)
                                {
                                    if (false == _substrateManager.HasSubstrateAtLoadPort(targetPortId, i))
                                    {
                                        targetSlot = i;
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                targetSlot = sourceSlot;
                            }

                            if (targetSlot < 0)
                                return false;

                            return _carrierServer.HasCarrier(targetPortId);
                        }

                    case SubstrateType.Bin1:
                    case SubstrateType.Bin2:
                    case SubstrateType.Bin3:
                        {
                            // TODO : 하드코딩
                            if (Recipe.Recipe.GetInstance().GetValue(EN_RECIPE_TYPE.COMMON, PARAM_COMMON.UseCycleMode.ToString(),
                                false))
                            {
                                //targetPortId = 4;
                                targetType = SubstrateType.Empty;
                            }

                            if (sourceSlot < 0)
                            {
                                targetSlot = -1;
                                int capacity = _carrierServer.GetCapacity(targetPortId);
                                for (int i = 0; i < capacity; ++i)
                                {
                                    if (false == _substrateManager.HasSubstrateAtLoadPort(targetPortId, i))
                                    {
                                        targetSlot = i;
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                targetSlot = sourceSlot;
                            }

                            if (targetSlot < 0)
                                return false;

                            return _carrierServer.HasCarrier(targetPortId);
                        }

                    default:
                        return false;
                }
            }

            return false;
        }
        private bool CheckCarrierExistanceBySubstrateType2(int sourcePortId, string substrateType, ref int portId, ref SubstrateType targetType)
        {
            if (false == Enum.TryParse(substrateType, out targetType))
                return false;

            switch (targetType)
            {
                case SubstrateType.Core:
                case SubstrateType.Empty:
                    portId = sourcePortId;
                    return _carrierServer.HasCarrier(sourcePortId);

                case SubstrateType.Bin1:
                case SubstrateType.Bin2:
                case SubstrateType.Bin3:
                    {
                        // TODO : 하드코딩
                        if (Recipe.Recipe.GetInstance().GetValue(EN_RECIPE_TYPE.COMMON, PARAM_COMMON.UseCycleMode.ToString(),
                            false))
                        {
                            portId = 4;
                            targetType = SubstrateType.Empty;
                            return _carrierServer.HasCarrier(sourcePortId);
                        }
                        else
                        {
                            for (int i = 0; i < _loadPortManager.Count; ++i)
                            {
                                // 2024.09.03. jhlim [MOD] SubType을 UI에는 Center/Left/Right로 지정되도록 변경
                                //FrameOfSystem3.Recipe.PARAM_EQUIPMENT paramName;
                                //paramName = FrameOfSystem3.Recipe.PARAM_EQUIPMENT.LoadPortType1 + i;
                                //string subTypeByRecipe = FrameOfSystem3.Recipe.Recipe.GetInstance().GetValue(FrameOfSystem3.Recipe.EN_RECIPE_TYPE.EQUIPMENT,
                                //    paramName.ToString(),
                                //    SubstrateType.Empty.ToString());

                                //if (false == Enum.TryParse(subTypeByRecipe, out SubstrateType convertedSubType))
                                //    continue;
                                SubstrateType convertedSubType = _scenarioManager.GetSubstrateTypeByLoadPortIndex(i);
                                // 2024.09.03. jhlim [END]

                                if (false == targetType.Equals(convertedSubType))
                                    continue;

                                portId = _loadPortManager.GetLoadPortPortId(i);
                                if (_carrierServer.HasCarrier(portId))
                                    return true;
                            }
                        }

                        return false;
                    }

                default:
                    return false;
            }
        }
        private int FindPortIdBySubstrateType(int sourcePortId, string substrateType)
        {
            if (false == Enum.TryParse(substrateType, out SubstrateType targetType))
                return -1;

            switch (targetType)
            {
                case SubstrateType.Core:
                case SubstrateType.Empty:
                    return sourcePortId;

                case SubstrateType.Bin1:
                case SubstrateType.Bin2:
                case SubstrateType.Bin3:
                    {
                        for (int i = 0; i < _loadPortManager.Count; ++i)
                        {
                            // 2024.09.03. jhlim [MOD] SubType을 UI에는 Center/Left/Right로 지정되도록 변경
                            //FrameOfSystem3.Recipe.PARAM_EQUIPMENT paramName;
                            //paramName = FrameOfSystem3.Recipe.PARAM_EQUIPMENT.LoadPortType1 + i;
                            //string subTypeByRecipe = FrameOfSystem3.Recipe.Recipe.GetInstance().GetValue(FrameOfSystem3.Recipe.EN_RECIPE_TYPE.EQUIPMENT,
                            //    paramName.ToString(),
                            //    SubstrateType.Empty.ToString());

                            //if (false == Enum.TryParse(subTypeByRecipe, out SubstrateType convertedSubType))
                            //    continue;
                            SubstrateType convertedSubType = _scenarioManager.GetSubstrateTypeByLoadPortIndex(i);
                            // 2024.09.03. jhlim [END]

                            if (false == targetType.Equals(convertedSubType))
                                continue;

                            return _loadPortManager.GetLoadPortPortId(i);
                        }

                        return -1;
                    }

                default:
                    return -1;
            }
        }
        private bool FindWellknownProtInfoBySubstrateType(Substrate substrate, SubstrateType subType, ref int portId, ref int slot, ref string description)
        {
            description = string.Empty;
            portId = -1; slot = -1;
            switch (subType)
            {
                case SubstrateType.Bin1:
                case SubstrateType.Bin2:
                case SubstrateType.Bin3:
                    {
                        int lpIndex = -1;
                        for (int i = 0; i < _loadPortManager.Count; ++i)
                        {
                            // 2024.09.03. jhlim [MOD] SubType을 UI에는 Center/Left/Right로 지정되도록 변경
                            //FrameOfSystem3.Recipe.PARAM_EQUIPMENT paramName;
                            //paramName = FrameOfSystem3.Recipe.PARAM_EQUIPMENT.LoadPortType1 + i;
                            //string subTypeByRecipe = FrameOfSystem3.Recipe.Recipe.GetInstance().GetValue(FrameOfSystem3.Recipe.EN_RECIPE_TYPE.EQUIPMENT,
                            //    paramName.ToString(),
                            //    SubstrateType.Empty.ToString());

                            //if (false == Enum.TryParse(subTypeByRecipe, out SubstrateType convertedSubType))
                            //    continue;
                            SubstrateType convertedSubType = _scenarioManager.GetSubstrateTypeByLoadPortIndex(i);
                            // 2024.09.03. jhlim [END]

                            if (false == subType.Equals(convertedSubType))
                                continue;

                            lpIndex = i;
                            break;
                        }

                        if (lpIndex < 0)
                        {
                            description = ErrorDescriptionForInvalidSubstratePortInfo;
                            return false;
                        }

                        portId = _loadPortManager.GetLoadPortPortId(lpIndex);
                        if (portId <= 0)
                        {
                            description = ErrorDescriptionForInvalidSubstratePortInfo;
                            return false;
                        }

                        if (false == _carrierServer.HasCarrier(portId))
                        {
                            description = ErrorDescriptionForDoesntHaveCarrier;
                            return false;
                        }

                        if (false == _loadPortManager.IsLoadPortEnabled(lpIndex))
                        {
                            description = ErrorDescriptionForLoadPortNotEnabled;
                            return false;
                        }

                        if (false == _loadPortManager.GetDoorState(lpIndex))
                        {
                            description = ErrorDescriptionForDoorIsNotOpened;
                            return false;
                        }

                        int capacity = _carrierServer.GetCapacity(portId);
                        var substrates = _substrateManager.GetSubstratesAtLoadPort(portId);
                        var loadingMode = _loadPortManager.GetCarrierLoadingType(lpIndex);
                        for (int i = 0; i < capacity; ++i)
                        {
                            if (i == 0 && loadingMode.Equals(LoadPortLoadingMode.Cassette))
                                continue;

                            if (false == substrates.ContainsKey(i))
                            {
                                slot = i;
                                return true;
                            }
                        }

                        description = ErrorDescriptionForSlotIsFull;
                        return false;
                    }

                default:
                    portId = substrate.GetSourcePortId();
                    slot = substrate.GetSourceSlot();
                    return true;
            }
        }

        // Type과 Sub정보를 이용해서 Port 번호를 받아온다.
        private int FindUnknownPortInfoBySubstrateType(Substrate substrate, SubstrateType subType)
        {
            switch (subType)
            {
                case SubstrateType.Core:
                    {
                        // Core Port는 두갠데...
                        for (int i = 0; i < _loadPortManager.Count; ++i)
                        {
                            if (i.Equals((int)LoadPortType.Core_1) ||
                                i.Equals((int)LoadPortType.Core_2))
                            {
                                int portId = _loadPortManager.GetLoadPortPortId(i);

                                if (_carrierServer.GetCarrierAccessingStatus(portId).Equals(CarrierAccessStates.InAccessed))
                                {
                                    return portId;
                                }
                            }
                        }
                        return -1;
                    }

                case SubstrateType.Empty:
                    {
                        //
                        int lpIndex = (int)LoadPortType.EmptyTape;
                        return _loadPortManager.GetLoadPortPortId(lpIndex);
                    }

                case SubstrateType.Bin1:
                case SubstrateType.Bin2:
                case SubstrateType.Bin3:
                    {
                        return FindDestinationPortBySubstrateType(substrate, subType);
                    }

                default:
                    return -1;
            }
        }
        // Type과 Sub정보를 이용해서 Slot 번호를 받아온다.
        private int FindUnknownSlotInfoByPortId(int portId, Substrate substrate, SubstrateType subType)
        {
            int capacity = _carrierServer.GetCapacity(portId);
            int lpIndex = _loadPortManager.GetLoadPortIndexByPortId(portId);

            switch (subType)
            {
                case SubstrateType.Core:
                case SubstrateType.Empty:
                    {
                        bool notAvailableSlotFirst = (_loadPortManager.GetCarrierLoadingType(lpIndex).Equals(LoadPortLoadingMode.Cassette));
                        for (int i = 0; i < capacity; ++i)
                        {
                            if (notAvailableSlotFirst && i == 0)
                                continue;

                            if (_substrateManager.HasSubstrateAtLoadPort(portId, i))
                                continue;

                            if (false == _substrateManager.IsSourceSlotReserved(portId, i))
                            {
                                return i;
                            }
                        }

                        // 둘 데가 없다..
                        return -1;
                    }

                case SubstrateType.Bin1:
                case SubstrateType.Bin2:
                case SubstrateType.Bin3:
                    {
                        int slot = -1;
                        GetNextSlotInformationToPlace(lpIndex, ref slot);

                        return slot;
                    }

                default:
                    return -1;
            }
        }

        private int FindDestinationPortBySubstrateType(Substrate substrate, SubstrateType subType)
        {
            if (subType.Equals(SubstrateType.Core) ||
                subType.Equals(SubstrateType.Empty))
            {
                return substrate.GetSourcePortId();
            }

            //int portId = -1;
            for (int i = 0; i < _loadPortManager.Count; ++i)
            {
                // 2024.09.03. jhlim [MOD] SubType을 UI에는 Center/Left/Right로 지정되도록 변경
                //FrameOfSystem3.Recipe.PARAM_EQUIPMENT paramName;
                //paramName = FrameOfSystem3.Recipe.PARAM_EQUIPMENT.LoadPortType1 + i;
                //string subTypeByRecipe = FrameOfSystem3.Recipe.Recipe.GetInstance().GetValue(FrameOfSystem3.Recipe.EN_RECIPE_TYPE.EQUIPMENT,
                //    paramName.ToString(),
                //    SubstrateType.Empty.ToString());

                //if (false == Enum.TryParse(subTypeByRecipe, out SubstrateType convertedSubType))
                //    continue;
                SubstrateType convertedSubType = _scenarioManager.GetSubstrateTypeByLoadPortIndex(i);
                // 2024.09.03. jhlim [END]

                if (false == subType.Equals(convertedSubType))
                    continue;

                switch (convertedSubType)
                {
                    case SubstrateType.Bin1:
                    case SubstrateType.Bin2:
                    case SubstrateType.Bin3:
                        return _loadPortManager.GetLoadPortPortId(i);

                    default:
                        break;
                }
            }

            return -1;
        }

        protected override CommandResults IsApproachUnloadingPrepared()
        {
            const string MethodName = "IsApproachUnloadingPrepared";

            // 상황이 바뀌었을 수 있다..
            if (false == IsUnloadingSignalStillActive(_workingInfo.Location.Name))
                return ReturnSkipped(MethodName);

            switch (_subStepInterface)
            {
                case 0:
                    {
                        // 1. ActionUnloading을 전송
                        if (false == _processGroup.SendMessage(ProcessModuleIndex, _workingInfo.Location,
                            RequestMessages.RequestApproachUnloading.ToString(), _workingInfo.SubstrateName))
                        {
                            return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_BEFORE_UNLOADING_SENDING_FAILED, MethodName,
                                _subStepInterface, RequestMessages.RequestApproachUnloading.ToString());
                        }

                        Ticks.SetTickCount(TimeoutShort);
                        ++_subStepInterface;
                    }
                    break;
                case 1:
                    {
                        if (IsTickOver())
                        {
                            return ReturnToError(CommandResult.Timeout, EN_ALARM.INTERFACE_BEFORE_UNLOADING_SENDING_COMPLETED_TIMEOUT_ACK, MethodName,
                                _subStepInterface, RequestMessages.RequestApproachUnloading.ToString());
                        }

                        //  2. Ack 확인
                        var result = _processGroup.IsSendingCompleted(ProcessModuleIndex, _workingInfo.Location,
                            RequestMessages.RequestApproachUnloading.ToString());

                        switch (result)
                        {
                            case CommunicationResult.Ack:
                                Ticks.SetTickCount(TimeoutLong);
                                ++_subStepInterface;
                                break;
                            case CommunicationResult.Nack:
                            case CommunicationResult.Error:
                                {
                                    return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_BEFORE_UNLOADING_SENDING_COMPLETED_BUT_NACK, MethodName,
                                        _subStepInterface, RequestMessages.RequestApproachUnloading.ToString());
                                }
                            default:
                                break;
                        }

                    }
                    break;
                case 2:
                    {
                        if (IsTickOver())
                        {
                            return ReturnToError(CommandResult.Timeout, EN_ALARM.INTERFACE_BEFORE_UNLOADING_RECEIVING_RESPONSE_MESSAGE_TIMEOUT, MethodName,
                                _subStepInterface, ResponseMessages.ResponseApproachUnloading.ToString());
                        }

                        // 3. Response 확인
                        var result = _processGroup.IsMessageReceived(ProcessModuleIndex, _workingInfo.Location,
                            ResponseMessages.ResponseApproachUnloading.ToString());
                        switch (result)
                        {
                            case CommunicationResult.Ack:
                                Ticks.SetTickCount(TimeoutShort);
                                ++_subStepInterface;
                                break;

                            case CommunicationResult.Nack:
                            case CommunicationResult.Error:
                                return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_BEFORE_UNLOADING_RECEIVING_COMPLETED_BUT_ERROR, MethodName,
                                    _subStepInterface, ResponseMessages.ResponseApproachUnloading.ToString());

                            default:
                                break;
                        }
                    }
                    break;

                case 3:
                    {
                        if (IsTickOver())
                        {
                            return ReturnToError(CommandResult.Timeout, EN_ALARM.INTERFACE_BEFORE_UNLOADING_RECEIVING_RESPONSE_DATA_TIMEOUT, MethodName,
                                _subStepInterface, ResponseMessages.ResponseApproachUnloading.ToString());
                        }

                        // 4. 데이터 확인
                        Dictionary<string, string> receivedData = new Dictionary<string, string>();
                        if (false == _processGroup.GetReceivedData(ProcessModuleIndex, _workingInfo.Location,
                            ResponseMessages.ResponseApproachUnloading.ToString(), ref receivedData))
                            break;

                        #region <Data 확인>
                        if (false == receivedData.TryGetValue(PWA500BINSubstrateAttributes.SubstrateName, out string substrateName))
                        {
                            return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_BEFORE_UNLOADING_RECEIVING_COMPLETED_BUT_DATA_INVALID, MethodName,
                                _subStepInterface, string.Format("{0} : {1}",
                                ResponseMessages.ResponseApproachUnloading.ToString(), PWA500BINSubstrateAttributes.SubstrateName));
                        }

                        if (false == receivedData.TryGetValue(PWA500BINSubstrateAttributes.LotId, out string lotId))
                        {
                            return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_BEFORE_UNLOADING_RECEIVING_COMPLETED_BUT_DATA_INVALID, MethodName,
                                _subStepInterface, string.Format("{0} : {1}",
                                ResponseMessages.ResponseApproachUnloading.ToString(), PWA500BINSubstrateAttributes.LotId));
                        }

                        if (false == receivedData.TryGetValue(PWA500BINSubstrateAttributes.RecipeId, out string recipeId))
                        {
                            return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_BEFORE_UNLOADING_RECEIVING_COMPLETED_BUT_DATA_INVALID, MethodName,
                                _subStepInterface, string.Format("{0} : {1}",
                                ResponseMessages.ResponseApproachUnloading.ToString(), PWA500BINSubstrateAttributes.RecipeId));
                        }

                        if (false == receivedData.TryGetValue(PWA500BINSubstrateAttributes.RingId, out string ringId))
                        {
                            return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_BEFORE_UNLOADING_RECEIVING_COMPLETED_BUT_DATA_INVALID, MethodName,
                                _subStepInterface, string.Format("{0} : {1}",
                                ResponseMessages.ResponseApproachUnloading.ToString(), PWA500BINSubstrateAttributes.RingId));
                        }

                        if (false == receivedData.TryGetValue(PWA500BINSubstrateAttributes.PortId, out string portId))
                        {
                            return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_BEFORE_UNLOADING_RECEIVING_COMPLETED_BUT_DATA_INVALID, MethodName,
                                _subStepInterface, string.Format("{0} : {1}",
                                ResponseMessages.ResponseApproachUnloading.ToString(), PWA500BINSubstrateAttributes.PortId));
                        }

                        if (false == receivedData.TryGetValue(PWA500BINSubstrateAttributes.SlotId, out string slot))
                        {
                            return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_BEFORE_UNLOADING_RECEIVING_COMPLETED_BUT_DATA_INVALID, MethodName,
                                _subStepInterface, string.Format("{0} : {1}",
                                ResponseMessages.ResponseApproachUnloading.ToString(), PWA500BINSubstrateAttributes.SlotId));
                        }

                        if (false == receivedData.TryGetValue(PWA500BINSubstrateAttributes.SubstrateType, out string subType) ||
                            false == Enum.TryParse(subType, out SubstrateType convertedType))
                        {
                            return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_BEFORE_UNLOADING_RECEIVING_COMPLETED_BUT_DATA_INVALID, MethodName,
                                _subStepInterface, string.Format("{0} : {1}",
                                ResponseMessages.ResponseApproachUnloading.ToString(), PWA500BINSubstrateAttributes.SubstrateType));
                        }

                        bool hasCarrier = false;
                        Substrate substrate = new Substrate("");

                        if (false == FindSubstrateByAttribute(substrateName, portId, slot, ref substrate))
                        {
                            string substrateTemporaryName;
                            if (string.IsNullOrEmpty(ringId) && string.IsNullOrEmpty(substrateName))
                                substrateTemporaryName = "Unknown";
                            else if (string.IsNullOrEmpty(substrateName))
                                substrateTemporaryName = ringId;
                            else
                                substrateTemporaryName = substrateName;

                            _substrateManager.CreateSubstrate(substrateTemporaryName, _workingInfo.Location);

                            substrate = new Substrate("");
                            bool result = false;

                            if (_workingInfo.Location is ProcessModuleLocation pmLocation)
                            {
                                result = _substrateManager.GetSubstrateAtProcessModule(substrateTemporaryName, pmLocation, ref substrate);
                            }

                            if (result)
                            {
                                var attr = substrate.GetAttributesAll();
                                string[] keys = attr.Keys.ToArray();
                                for (int i = 0; i < keys.Length; ++i)
                                {
                                    if (attr[keys[i]] == null)
                                    {
                                        substrate.SetAttribute(keys[i], string.Empty);
                                    }
                                }

                                substrate.SetAttribute(BaseSubstrateAttributeKeys.Name, substrateTemporaryName);
                                substrate.SetAttribute(PWA500BINSubstrateAttributes.RingId, substrateTemporaryName);
                                substrate.SetAttribute(BaseSubstrateAttributeKeys.ProcessingState, ProcessingStates.Processed.ToString());
                                substrate.SetAttribute(BaseSubstrateAttributeKeys.TransPortState, SubstrateTransferStates.AtWork.ToString());
                                substrate.SetAttribute(PWA500BINSubstrateAttributes.SubstrateType, subType);
                                substrate.SetAttribute(BaseSubstrateAttributeKeys.LotId, lotId);
                                substrate.SetAttribute(BaseSubstrateAttributeKeys.RecipeId, recipeId);

                                int port = FindUnknownPortInfoBySubstrateType(substrate, convertedType);
                                int slotIndex = FindUnknownSlotInfoByPortId(port, substrate, convertedType);

                                substrate.SetAttribute(BaseSubstrateAttributeKeys.SourcePortId, port.ToString());
                                substrate.SetAttribute(BaseSubstrateAttributeKeys.SourceSlot, slotIndex.ToString());
                                substrate.SetAttribute(BaseSubstrateAttributeKeys.DestinationPortId, port.ToString());
                                substrate.SetAttribute(BaseSubstrateAttributeKeys.DestinationSlot, slotIndex.ToString());
                                substrateName = substrateTemporaryName;

                                _processGroup.AssignSubstrate(_workingInfo.Location.Name, substrate);

                            }
                            else
                            {
                                return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_BEFORE_UNLOADING_RECEIVING_COMPLETED_BUT_DATA_INVALID, MethodName,
                                    _subStepInterface, string.Format("Find substrate has failed by Receiving data : {0}, [{1},{2},{3}]", ResponseMessages.ResponseApproachUnloading.ToString(),
                                    ringId, portId, slot));
                            }
                        }

                        // 찾았으면 Set
                        substrate.SetName(substrateName);
                        //substrate.SetLotId(lotId);
                        substrate.SetRecipeId(recipeId);

                        if (false == int.TryParse(portId, out int sourcePortId))
                            sourcePortId = -1;
                        substrate.SetSourcePortId(sourcePortId);

                        if (false == int.TryParse(slot, out int sourceSlot))
                            sourceSlot = -1;
                        substrate.SetSourceSlot(sourceSlot);

                        substrate.SetProcessingStatus(ProcessingStates.Processed);
                        substrate.SetAttribute(PWA500BINSubstrateAttributes.SubstrateType, convertedType.ToString());

                        //if (sourcePortId > 0)
                        //{
                            int targetPortId = 0, targetSlot = 0;
                            string description = string.Empty;
                            // 2024.08.22. jhlim [MOD] 공정설비에서 자재를 찾도록 수정
                            hasCarrier = FindWellknownProtInfoBySubstrateType(substrate, convertedType, ref targetPortId, ref targetSlot, ref description);
                            if (hasCarrier)
                            {
                                substrate.SetDestinationPortId(targetPortId);
                                substrate.SetDestinationSlot(targetSlot);
                            }

                            // 2024.08.22. jhlim [END]
                            //hasCarrier = CheckCarrierExistanceBySubstrateType(_substrateToPick.GetSourcePortId(), _substrateToPick.GetSourceSlot(), subType, ref targetPortId, ref targetSlot, ref convertedType);
                            //if (targetPortId > 0)
                            //{
                            //    _substrateToPick.SetProcessingStatus(ProcessingStates.Processed);
                            //    _substrateToPick.SetDestinationPortId(targetPortId);
                            //    _substrateToPick.SetDestinationSlot(targetSlot);
                            //    switch (convertedType)
                            //    {
                            //        case SubstrateType.Bin1:
                            //        case SubstrateType.Bin2:
                            //        case SubstrateType.Bin3:
                            //            {
                            //                _substrateToPick.SetDestinationPortId(targetPortId);
                            //                _substrateToPick.SetDestinationSlot(targetSlot);
                            //                _substrateToPick.SetAttribute(PWA500BINSubstrateAttributes.SubstrateType, convertedType.ToString());
                            //            }
                            //            break;

                            //        default:
                            //            break;
                            //    }
                            //}
                        //}

                        _workingInfo.SubstrateName = substrate.GetName();
                        //if (ManualWorkingInfo.Count > 0)
                        //{
                        //    ManualWorkingInfo.First().Value.SubstrateName = substrate.GetName();
                        //}
                        #endregion </Data 확인>

                        // 5. Ack 전송 : 콜백에서 자동 Ack 나가니 현재 미구현
                        //if (false == _processGroup.SetAckReceivedMessage(ProcessModuleIndex, _workingInfo.Location,
                        //    ResponseMessages.ResponseApproachUnloading.ToString(), CommunicationResult.Ack, string.Empty))
                        //{

                        //}

                        string handlingResult = hasCarrier ? PWA500BINSubstrateAttributes.HandlingResultOk : PWA500BINSubstrateAttributes.HandlingResultNg;
                        receivedData[PWA500BINSubstrateAttributes.HandlingResult] = handlingResult;

                        if (false == _processGroup.SendMessage(ProcessModuleIndex, _workingInfo.Location,
                            RequestMessages.RequestStartUnloading.ToString(), receivedData))
                        {
                            return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_BEFORE_UNLOADING_SENDING_FAILED, MethodName,
                                _subStepInterface, RequestMessages.RequestStartUnloading.ToString());
                        }

                        if (sourcePortId > 0 && false == hasCarrier)
                        {
                            int lpIndex = _loadPortManager.GetLoadPortIndexByPortId(sourcePortId);
                            if (description.Equals(ErrorDescriptionForDoesntHaveCarrier))
                            {
                                // Carrier가 없고, Auto면 올 때까지 대기해야 하므로 스킵
                                if (_loadPortManager.GetAccessMode(lpIndex).Equals(LoadPortAccessMode.Auto))
                                {
                                    return ReturnSkipped(MethodName);
                                }
                                else
                                {
                                    return ReturnToError(CommandResult.Error, EN_ALARM.ATM_ROBOT_DOES_NOT_HAVE_CARRIER, MethodName, _subStepInterface, description);
                                }
                            }
                            else
                            {
                                return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_BEFORE_UNLOADING_RECEIVING_COMPLETED_BUT_DATA_INVALID, MethodName,
                                    _subStepInterface, description);
                            }                            
                            //return ReturnSkipped(MethodName, description);
                        }

                        Ticks.SetTickCount(TimeoutShort);
                        ++_subStepInterface;
                    }
                    break;

                case 4:
                    {
                        if (IsTickOver())
                        {
                            return ReturnToError(CommandResult.Timeout, EN_ALARM.INTERFACE_BEFORE_UNLOADING_SENDING_COMPLETED_TIMEOUT_ACK, MethodName,
                                _subStepInterface, RequestMessages.RequestStartUnloading.ToString());
                        }

                        //  2. Ack 확인
                        var result = _processGroup.IsSendingCompleted(ProcessModuleIndex, _workingInfo.Location,
                            RequestMessages.RequestStartUnloading.ToString());

                        switch (result)
                        {
                            case CommunicationResult.Ack:
                                Ticks.SetTickCount(TimeoutLong);
                                ++_subStepInterface;
                                break;
                            case CommunicationResult.Nack:
                            case CommunicationResult.Error:
                                {
                                    return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_BEFORE_UNLOADING_SENDING_COMPLETED_BUT_NACK, MethodName,
                                        _subStepInterface, RequestMessages.RequestStartUnloading.ToString());
                                }
                            default:
                                break;
                        }
                    }
                    break;

                case 5:
                    {
                        if (IsTickOver())
                        {
                            return ReturnToError(CommandResult.Timeout, EN_ALARM.INTERFACE_BEFORE_UNLOADING_RECEIVING_RESPONSE_DATA_TIMEOUT, MethodName,
                                _subStepInterface, ResponseMessages.ResponseStartUnloading.ToString());
                        }

                        // TODO : 메시지를 왜 확인 안 했더라??
                        // 3. Response 확인 
                        //var result = _processGroup.IsMessageReceived(ProcessModuleIndex, _workingInfo.Location,
                        //    ResponseMessages.ResponseStartUnloading.ToString());
                        //switch (result)
                        //{
                        //    case CommunicationResult.Ack:
                        //        _processGroup.SetUnloadingSignal(ProcessModuleIndex, _workingInfo.Location, true);
                        //        return ReturnCompleted();

                        //    case CommunicationResult.Nack:
                        //    case CommunicationResult.Error:
                        //        return CommandResult.Error;

                        //    default:
                        //        break;
                        //}
                        Dictionary<string, string> resp = new Dictionary<string, string>();
                        if (false == _processGroup.GetReceivedData(ProcessModuleIndex, _workingInfo.Location, ResponseMessages.ResponseStartUnloading.ToString(), ref resp))
                            break;

                        _processGroup.SetUnloadingSignal(ProcessModuleIndex, _workingInfo.Location.Name, true);

                        return ReturnCompleted();
                    }

                default:
                    break;
            }

            return ReturnProceed();
        }
        protected override CommandResults IsApproachUnloadingCompleted()
        {
            const string MethodName = "IsApproachUnloadingCompleted";

            // 상황이 바뀌었을 수 있다..
            if (false == IsUnloadingSignalStillActive(_workingInfo.Location.Name))
                return ReturnSkipped(MethodName);

            return ReturnCompleted();
        }
        protected override CommandResults IsUnloadingPrepared()
        {
            const string MethodName = "IsUnloadingPrepared";

            // 상황이 바뀌었을 수 있다..
            if (false == IsUnloadingSignalStillActive(_workingInfo.Location.Name))
                return ReturnSkipped(MethodName);

            switch (_subStepInterface)
            {
                case 0:
                    {
                        // 1. ActionUnloading을 전송
                        if (false == _processGroup.SendMessage(ProcessModuleIndex, _workingInfo.Location,
                            RequestMessages.RequestActionUnloading.ToString(), _workingInfo.SubstrateName))
                        {
                            return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_ACTION_UNLOADING_SENDING_FAILED, MethodName,
                                _subStepInterface, RequestMessages.RequestActionUnloading.ToString());
                        }

                        Ticks.SetTickCount(TimeoutShort);
                        ++_subStepInterface;
                    }
                    break;
                case 1:
                    {
                        if (IsTickOver())
                        {
                            return ReturnToError(CommandResult.Timeout, EN_ALARM.INTERFACE_ACTION_UNLOADING_SENDING_COMPLETED_TIMEOUT_ACK, MethodName,
                                _subStepInterface, RequestMessages.RequestActionUnloading.ToString());
                        }

                        //  2. Ack 확인
                        var result = _processGroup.IsSendingCompleted(ProcessModuleIndex, _workingInfo.Location,
                            RequestMessages.RequestActionUnloading.ToString());

                        switch (result)
                        {
                            case CommunicationResult.Ack:
                                Ticks.SetTickCount(TimeoutLong);
                                ++_subStepInterface;
                                break;
                            case CommunicationResult.Nack:
                            case CommunicationResult.Error:
                                {
                                    return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_ACTION_UNLOADING_SENDING_COMPLETED_BUT_NACK, MethodName,
                                        _subStepInterface, RequestMessages.RequestActionUnloading.ToString());
                                }
                            default:
                                break;
                        }

                    }
                    break;
                case 2:
                    {
                        if (IsTickOver())
                        {
                            return ReturnToError(CommandResult.Timeout, EN_ALARM.INTERFACE_ACTION_UNLOADING_RECEIVING_RESPONSE_MESSAGE_TIMEOUT, MethodName,
                                _subStepInterface, ResponseMessages.ResponseActionUnloading.ToString());
                        }

                        // 3. Response 확인
                        var result = _processGroup.IsMessageReceived(ProcessModuleIndex, _workingInfo.Location,
                            ResponseMessages.ResponseActionUnloading.ToString());
                        switch (result)
                        {
                            case CommunicationResult.Ack:
                                Ticks.SetTickCount(TimeoutShort);
                                ++_subStepInterface;
                                break;

                            case CommunicationResult.Nack:
                            case CommunicationResult.Error:
                                {
                                    return ReturnToError(CommandResult.Timeout, EN_ALARM.INTERFACE_ACTION_UNLOADING_RECEIVING_COMPLETED_BUT_ERROR, MethodName,
                                        _subStepInterface, ResponseMessages.ResponseActionUnloading.ToString());
                                }

                            default:
                                break;
                        }
                    }
                    break;

                case 3:
                    {
                        if (IsTickOver())
                        {
                            return ReturnToError(CommandResult.Timeout, EN_ALARM.INTERFACE_ACTION_UNLOADING_RECEIVING_RESPONSE_DATA_TIMEOUT, MethodName,
                                _subStepInterface, ResponseMessages.ResponseActionUnloading.ToString());
                        }

                        // 4. 데이터 확인
                        Dictionary<string, string> receivedData = new Dictionary<string, string>();
                        if (false == _processGroup.GetReceivedData(ProcessModuleIndex, _workingInfo.Location,
                            ResponseMessages.ResponseActionUnloading.ToString(), ref receivedData))
                            break;

                        // 5. Ack 전송 : 콜백에서 자동 Ack 나가니 현재 미구현
                        //if (false == _processGroup.SetAckReceivedMessage(ProcessModuleIndex, _workingInfo.Location,
                        //    ResponseMessages.ResponseActionUnloading.ToString(), CommunicationResult.Ack, string.Empty))
                        //{

                        //}

                        return ReturnCompleted();
                    }

                default:
                    break;
            }

            return ReturnProceed();
        }
        protected override CommandResults IsUnloadingCompleted()
        {
            const string MethodName = "IsUnloadingCompleted";

            // 상황이 바뀌었을 수 있다..
            if (false == IsUnloadingSignalStillActive(_workingInfo.Location.Name))
            {
                return ReturnSkipped(MethodName);
            }

            switch (_subStepInterface)
            {
                case 0:
                    {
                        // 1. ConfirmUnloading 전송
                        if (false == _processGroup.SendMessage(ProcessModuleIndex, _workingInfo.Location,
                            RequestMessages.RequestConfirmUnloading.ToString(), _workingInfo.SubstrateName))
                        {
                            return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_AFTER_UNLOADING_SENDING_FAILED, MethodName,
                                _subStepInterface, RequestMessages.RequestConfirmUnloading.ToString());
                        }

                        Ticks.SetTickCount(TimeoutShort);
                        ++_subStepInterface;
                    }
                    break;
                case 1:
                    {
                        if (IsTickOver())
                        {
                            return ReturnToError(CommandResult.Timeout, EN_ALARM.INTERFACE_AFTER_UNLOADING_SENDING_COMPLETED_TIMEOUT_ACK, MethodName,
                                _subStepInterface, RequestMessages.RequestConfirmUnloading.ToString());
                        }

                        //  2. Ack 확인
                        var result = _processGroup.IsSendingCompleted(ProcessModuleIndex, _workingInfo.Location,
                            RequestMessages.RequestConfirmUnloading.ToString());

                        switch (result)
                        {
                            case CommunicationResult.Ack:
                                Ticks.SetTickCount(TimeoutLong);
                                ++_subStepInterface;
                                break;
                            case CommunicationResult.Nack:
                            case CommunicationResult.Error:
                                {
                                    return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_AFTER_UNLOADING_SENDING_COMPLETED_BUT_NACK, MethodName,
                                        _subStepInterface, RequestMessages.RequestConfirmUnloading.ToString());
                                }
                            default:
                                break;
                        }

                    }
                    break;
                case 2:
                    {
                        if (IsTickOver())
                        {
                            return ReturnToError(CommandResult.Timeout, EN_ALARM.INTERFACE_AFTER_UNLOADING_RECEIVING_RESPONSE_MESSAGE_TIMEOUT, MethodName,
                                _subStepInterface, ResponseMessages.ResponseConfirmUnloading.ToString());
                        }

                        // 3. Response 확인
                        var result = _processGroup.IsMessageReceived(ProcessModuleIndex, _workingInfo.Location,
                            ResponseMessages.ResponseConfirmUnloading.ToString());
                        switch (result)
                        {
                            case CommunicationResult.Ack:
                                Ticks.SetTickCount(TimeoutShort);
                                ++_subStepInterface;
                                break;

                            case CommunicationResult.Nack:
                            case CommunicationResult.Error:
                                {
                                    return ReturnToError(CommandResult.Error, EN_ALARM.INTERFACE_AFTER_UNLOADING_RECEIVING_COMPLETED_BUT_ERROR, MethodName,
                                        _subStepInterface, ResponseMessages.ResponseConfirmUnloading.ToString());
                                }

                            default:
                                break;
                        }
                    }
                    break;

                case 3:
                    {
                        if (IsTickOver())
                        {
                            return ReturnToError(CommandResult.Timeout, EN_ALARM.INTERFACE_AFTER_UNLOADING_RECEIVING_RESPONSE_DATA_TIMEOUT, MethodName,
                                _subStepInterface, ResponseMessages.ResponseConfirmUnloading.ToString());
                        }

                        // 4. 데이터 확인
                        //Dictionary<string, string> receivedData = new Dictionary<string, string>();
                        //if (false == _processGroup.GetReceivedData(ProcessModuleIndex, _workingInfo.Location,
                        //    ResponseMessages.ResponseConfirmUnloading.ToString(), ref receivedData))
                        //    break;

                        //// 5. Ack 전송 : 콜백에서 자동 Ack 나가니 현재 미구현
                        //if (false == _processGroup.SetAckReceivedMessage(ProcessModuleIndex, _workingInfo.Location,
                        //    ResponseMessages.ResponseConfirmUnloading.ToString(), CommunicationResult.Ack))
                        //    return CommandResult.Error;

                        Ticks.SetTickCount(100);
                        ++_subStepInterface;
                    }
                    break;
                case 4:
                    {
                        if (false == IsTickOver())
                            break;

                        // 6. SMEMA OFF
                        _processGroup.SetUnloadingSignal(ProcessModuleIndex, _workingInfo.Location.Name, false);

                        return ReturnCompleted();
                    }

                default:
                    break;
            }

            return ReturnProceed();
        }
        #endregion </Unloading>

        #endregion </Material Handling With Process Module>

        #region <Recovery Data>
        protected override void UpdateRecoveryDataBeforePick()
        {
        }

        protected override void UpdateRecoveryDataAfterPick()
        {
        }
        protected override void UpdateRecoveryDataBeforePlace()
        {
        }
        protected override void UpdateRecoveryDataAfterPlace()
        {
        }
        #endregion </Recovery Data>

        #endregion </Overrids>

        #region <Internals>
        #region <Scenario>
        private bool GetSubstrateTypeByAttribute(string attributeValue, ref SubstrateType substrateType)
        {
            return Enum.TryParse(attributeValue, out substrateType);
        }
        private void InitResult(ScenarioListTypes scenario)
        {
            _commandResult.ActionName = scenario.ToString();
            _commandResult.CommandResult = CommandResult.Proceed;
            _commandResult.Description = string.Empty;
        }
        private CommandResults RunScenario(Enum scenario)
        {
            var result = _scenarioOperator.ExecuteScenario(scenario);
            _commandResult.ActionName = scenario.ToString();
            switch (result)
            {
                case EN_SCENARIO_RESULT.PROCEED:
                    _commandResult.CommandResult = CommandResult.Proceed;
                    break;
                case EN_SCENARIO_RESULT.COMPLETED:
                    {
                        _commandResult.CommandResult = CommandResult.Completed;

                        ScenarioListTypes typeOfScenario = (ScenarioListTypes)scenario;
                        switch (typeOfScenario)
                        {
                            case ScenarioCoreTrackIn:
                            case ScenarioLotMatch:
                                {
                                    bool isManual = (false == EquipmentState_.EquipmentState.GetInstance().GetState().Equals(EquipmentState_.EQUIPMENT_STATE.EXECUTING));
                                    if (false == GetWorkingInformation(isManual, ref _workingInfo, ref _temporaryDescription))
                                        break;

                                    var location = _workingInfo.Location;
                                    if (!(location is LoadPortLocation lpLocation))
                                        break;

                                    #region <Track in or lot match>
                                    int portId = lpLocation.PortId;
                                    Substrate substrate = new Substrate("");
                                    if (false == _substrateManager.GetSubstrate(location, "", ref substrate))
                                        break;

                                    string lotId = substrate.GetLotId();
                                    string sourceCarrierId = substrate.GetSourceCarrierId();
                                    _lotHistoryLog.WriteHistoryForTrackIn(portId, sourceCarrierId, typeOfScenario.ToString(), lotId);
                                    #endregion </Track in or lot match>
                                }
                                break;

                            case ScenarioBinWaferIdAssign:
                                {
                                    // Substrate Id 할당된 것을 전달
                                    var resultOfScenario = _scenarioOperator.GetScenarioResultData(ScenarioBinWaferIdAssign);
                                    if (false == resultOfScenario.TryGetValue(AssignSubstrateIdKeys.KeyResultSubstrateId,
                                        out _newSubstrateId))
                                    {
                                        _commandResult.CommandResult = CommandResult.Error;
                                        _commandResult.Description = ErrorDescriptionForAssignSubstrateId;
                                    }
                                }
                                break;

                            case ScenarioBinWorkEnd:
                                {
                                    bool isManual = (false == EquipmentState_.EquipmentState.GetInstance().GetState().Equals(EquipmentState_.EQUIPMENT_STATE.EXECUTING));
                                    if (false == GetWorkingInformation(isManual, ref _workingInfo, ref _temporaryDescription))
                                        break;

                                    Substrate substrate = new Substrate("");
                                    if (false == _substrateManager.GetSubstrateAtRobot(RobotName, _workingInfo.ActionArm, ref substrate))
                                        break;

                                    int portId = substrate.GetDestinationPortId();
                                    string substrateName = substrate.GetName();
                                    string remainingChips = substrate.GetAttribute(PWA500BINSubstrateAttributes.ChipQty);
                                    string binCode = substrate.GetAttribute(PWA500BINSubstrateAttributes.BinCode);

                                    _lotHistoryLog.WriteSubstrateHistoryForBinWorkEnd(portId, substrateName, binCode, remainingChips);
                                }
                                break;

                            case ScnearioBinTrackOut:
                                {
                                    bool isManual = (false == EquipmentState_.EquipmentState.GetInstance().GetState().Equals(EquipmentState_.EQUIPMENT_STATE.EXECUTING));
                                    if (false == GetWorkingInformation(isManual, ref _workingInfo, ref _temporaryDescription))
                                        break;

                                    Substrate substrate = new Substrate("");
                                    if (false == _substrateManager.GetSubstrateAtRobot(RobotName, _workingInfo.ActionArm, ref substrate))
                                        break;

                                    int portId = substrate.GetDestinationPortId();
                                    string lotId = substrate.GetLotId();
                                    string substrateName = substrate.GetName();
                                    string remainingChips = substrate.GetAttribute(PWA500BINSubstrateAttributes.ChipQty);
                                    string binCode = substrate.GetAttribute(PWA500BINSubstrateAttributes.BinCode);

                                    _lotHistoryLog.WriteSubstrateHistoryForBinTrackOut(portId, substrateName, lotId, binCode, remainingChips);
                                }
                                break;

                            case ScenarioReqBinPartId:
                                {
                                    var scenarioResult = _scenarioOperator.GetScenarioResultData(ScenarioReqBinPartId);
                                    if (false == scenarioResult.TryGetValue(LotInfoKeys.KeyResultPartId,
                                        out _newPartId))
                                    {
                                        _commandResult.CommandResult = CommandResult.Error;
                                        _commandResult.Description = ErrorDescriptionForRequestPartId;
                                    }

                                    bool isManual = (false == EquipmentState_.EquipmentState.GetInstance().GetState().Equals(EquipmentState_.EQUIPMENT_STATE.EXECUTING));
                                    if (false == GetWorkingInformation(isManual, ref _workingInfo, ref _temporaryDescription))
                                        break;

                                    Substrate substrate = new Substrate("");
                                    if (false == _substrateManager.GetSubstrateAtRobot(RobotName, _workingInfo.ActionArm, ref substrate))
                                        break;

                                    int portId = substrate.GetDestinationPortId();
                                    string substrateName = substrate.GetName();
                                    string binCode = substrate.GetAttribute(PWA500BINSubstrateAttributes.BinCode);
                                    string partId = substrate.GetAttribute(PWA500BINSubstrateAttributes.PartId);
                                    _lotHistoryLog.WriteSubstrateHistoryForReqBinPartId(portId, substrateName, binCode, partId, _newPartId);
                                }
                                break;
                            case ScenarioUploadBinData:
                                {
                                    _binDataToUpload = null;
                                    _scenarioManager.ClearBinDataToUpload();

                                    bool isManual = (false == EquipmentState_.EquipmentState.GetInstance().GetState().Equals(EquipmentState_.EQUIPMENT_STATE.EXECUTING));
                                    if (false == GetWorkingInformation(isManual, ref _workingInfo, ref _temporaryDescription))
                                        break;

                                    Substrate substrate = new Substrate("");
                                    if (false == _substrateManager.GetSubstrateAtRobot(RobotName, _workingInfo.ActionArm, ref substrate))
                                        break;

                                    int portId = substrate.GetDestinationPortId();
                                    string substrateName = substrate.GetName();

                                    _lotHistoryLog.WriteSubstrateHistoryForUploadBinData(portId, substrateName, _scenarioManager.PmsFullPath);
                                }
                                break; ;

                            default:
                                break;
                        }
                    }
                    break;
                case EN_SCENARIO_RESULT.ERROR:
                    {
                        // TODO : 시뮬레이션인 경우 디버깅을 위함 : 공정설비와 통신 안 하는 경우
                        if (IsSimulation())
                        {
                            ScenarioListTypes typeOfScenario = (ScenarioListTypes)scenario;
                            switch (typeOfScenario)
                            {
                                case ScenarioSendClientToBinWaferIdAssign:
                                    {                                        
                                        _commandResult.CommandResult = CommandResult.Completed;
                                    }
                                    return _commandResult;

                                case ScenarioSendClientUploadBinFile:
                                    {
                                        _newPartId = "TEST_PART_ID";
                                        _commandResult.CommandResult = CommandResult.Completed;
                                    }
                                    return _commandResult;                                    
                            }
                        }
                        _commandResult.CommandResult = CommandResult.Error;
                        _commandResult.Description = _commandResult.ActionName;
                    }
                    break;
                case EN_SCENARIO_RESULT.TIMEOUT_ERROR:
                    _commandResult.CommandResult = CommandResult.Timeout;
                    _commandResult.Description = _commandResult.ActionName;
                    break;
                default:
                    break;
            }

            return _commandResult;
        }
        private int GetUnloadingStep(ref Substrate substrate)
        {
            var step = substrate.GetAttribute(PWA500BINSubstrateAttributes.BinUnloadingStep);
            if (false == int.TryParse(step, out int unloadingStep))
            {
                // 파싱 불가면 0으로 고정
                unloadingStep = (int)UnloadingStepTypes.Init;
                substrate.SetAttribute(PWA500BINSubstrateAttributes.BinUnloadingStep, unloadingStep.ToString());
            }

            return unloadingStep;
        }
        #endregion </Scenario>

        #region <Material Handling Interface>
        private bool GetNextSlotInformationToPlace(int lpIndex, ref int slot)
        {
            int portId = _loadPortManager.GetLoadPortPortId(lpIndex);
            if (false == _carrierServer.HasCarrier(portId))
                return false;

            //if (false == _substrateManager.HasAnySubstrateInLoadPort(portId))
            //    return false;

            slot = -1;
            bool notAvailableSlotFirst = (_loadPortManager.GetCarrierLoadingType(lpIndex).Equals(LoadPortLoadingMode.Cassette));
            int capacity = _carrierServer.GetCapacity(portId);
            for (int i = 0; i < capacity; ++i)
            {
                if (notAvailableSlotFirst && i == 0)
                    continue;

                if (false == _substrateManager.HasSubstrateAtLoadPort(portId, i))
                {
                    slot = i;
                    break;
                }
            }

            return (slot >= 0);
        }
        private void InitSubStepFlag()
        {
            _subStepInterface = 0;
        }
        private CommandResults ReturnToError(CommandResult result, EN_ALARM alarmCode,string methodName, int step, string description)
        {
            InitSubStepFlag();

            _result.ActionName = methodName;
            _result.AlarmCode = (int)alarmCode;
            _result.CommandResult = CommandResult.Error;
            _result.Description = description;

            return _result;
        }
        private CommandResults ReturnSkipped(string methodName)
        {
            InitSubStepFlag();

            _result.ActionName = methodName;
            _result.CommandResult = CommandResult.Skipped;
            _result.Description = string.Empty;

            return _result;
        }
        private CommandResults ReturnProceed()
        {
            //InitSubStepFlag();

            _result.ActionName = string.Empty;
            _result.CommandResult = CommandResult.Proceed;
            _result.Description = string.Empty;

            return _result;
        }
        private CommandResults ReturnCompleted()
        {
            InitSubStepFlag();

            _result.ActionName = string.Empty;
            _result.CommandResult = CommandResult.Completed;
            _result.Description = string.Empty;

            return _result;
        }
        private bool IsLoadingSignalStillActive(string location)
        {
            //  1) PM의 스메마 확인 후 Off면 Skipped 리턴
            List<string> requestedLocation = new List<string>();
            if (false == _processGroup.IsLoadingRequested(ProcessModuleIndex, ref requestedLocation))
                return false;

            if (false == requestedLocation.Contains(location))
                return false;

            return true;
        }

        #endregion </Material Handling Interface>
        #endregion </Internals>

        #endregion </Methods>
    }

    class TaskAtmRobotRecovery500BIN : Work.RecoveryData
    {
        public TaskAtmRobotRecovery500BIN(string taskName, int nPortCount)
            : base(taskName, nPortCount)
        {
        }

        protected override void LoadData(ref FileComposite_.FileComposite fComp, string sRootName)
        {
        }
        protected override void SaveData(ref FileComposite_.FileComposite fComp, string sRootName)
        {
        }
    }
}

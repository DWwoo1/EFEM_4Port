using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TickCounter_;

using FrameOfSystem3.SECSGEM.Scenario;
using FrameOfSystem3.SECSGEM.DefineSecsGem;

using EFEM.Defines.Common;
using EFEM.Defines.LoadPort;
using EFEM.MaterialTracking;
using EFEM.CustomizedByProcessType.PWA500BIN;

// ConfigTask에서 이 namespace를 가지고 클래스 타입을 가져오기 때문에 변경 불가
namespace FrameOfSystem3.Task
{
    class TaskLoadPortForPWA500BIN : TaskLoadPort
    {
        #region <Constructors>
        public TaskLoadPortForPWA500BIN(int nIndexOfTask, string strTaskName)
            : base(nIndexOfTask, strTaskName, new TaskLoadPortRecovery500BIN(strTaskName, nIndexOfTask))
        {
            if (PortId > 3)
            {
                int coreIndex = _loadPortManager.Count - PortId;
                ScenarioTypeToIdRead = ScenarioListTypes.SCENARIO_RFID_READ_CORE_1 + coreIndex;
                ScenarioTypeToRequestLotInfo = ScenarioListTypes.SCENARIO_REQ_LOT_INFO_CORE_1 + coreIndex;
                ScenarioTypeToSlotVerification = ScenarioListTypes.SCENARIO_REQ_SLOT_INFO_CORE_1 + coreIndex;

                ScenarioTypeToSlotMapping = ScenarioListTypes.SCENARIO_SLOT_WAFER_MAPPING_CORE_1 + coreIndex;
                ScenarioTypeToLotMerge = ScenarioListTypes.SCENARIO_REQ_LOT_MERGE_CORE_1 + coreIndex;
            }
            else
            {
                ScenarioTypeToIdRead = ScenarioListTypes.SCENARIO_RFID_READ_BIN_1 + LoadPortIndex;

                // Bin은 없음..
                //ScenarioTypeToRequestLotInfo = ScenarioListTypes.SCENARIO_REQ_LOT_INFO_CORE_1 + LoadPortIndex;
                //ScenarioTypeToSlotVerification = ScenarioListTypes.SCENARIO_REQ_SLOT_INFO_CORE_1 + LoadPortIndex;

                ScenarioTypeToSlotMapping = ScenarioListTypes.SCENARIO_SLOT_WAFER_MAPPING_BIN_1 + LoadPortIndex;
                ScenarioTypeToLotMerge = ScenarioListTypes.SCENARIO_REQ_LOT_ID_MERGE_AND_CHANGE_BIN_1 + LoadPortIndex;
            }

            ScenarioTypeToCarrierLoad = ScenarioListTypes.SCENARIO_PORT_STATUS_LOAD_1 + LoadPortIndex;
            ScenarioTypeToCarrierUnload = ScenarioListTypes.SCENARIO_PORT_STATUS_UNLOAD_1 + LoadPortIndex;

            _scenarioManager = ScenarioManagerForPWA500BIN_TP.Instance;

            _recovery = _recoveryData as TaskLoadPortRecovery500BIN;

            #region <Assign Digital IO>
            int relIndexOutput = LoadPortIndex * 2;
            int indexCassetteOutput = (int)Define.DefineEnumProject.DigitalIO.PWA500BIN.EN_DIGITAL_OUT.LP1_MANUAL_CASSETTE + relIndexOutput;
            int indexFoupOutput = (int)Define.DefineEnumProject.DigitalIO.PWA500BIN.EN_DIGITAL_OUT.LP1_MANUAL_MAC_FOUP + relIndexOutput;

            _loadPortManager.AttachModeChangerEventHandler(LoadPortIndex, LoadPortLoadingMode.Cassette,
                (trigger) => { DigitalIO_.DigitalIO.GetInstance().WriteOutput(indexCassetteOutput, trigger); }
                );

            _loadPortManager.AttachModeChangerEventHandler(LoadPortIndex, LoadPortLoadingMode.Foup,
                (trigger) => { DigitalIO_.DigitalIO.GetInstance().WriteOutput(indexFoupOutput, trigger); }
                );

            int relIndex = LoadPortIndex * 8;
            int indexInputCassetteChanger = (int)Define.DefineEnumProject.DigitalIO.PWA500BIN.EN_DIGITAL_IN.LP1_MANUAL_BUTTON_CASSETTE_STATUS + relIndex;
            int indexInputFoupChanger = (int)Define.DefineEnumProject.DigitalIO.PWA500BIN.EN_DIGITAL_IN.LP1_MANUAL_BUTTON_MAC_FOUP_STATUS + relIndex;

            TriggerChangingMode = new ConcurrentDictionary<LoadPortMode, bool>();
            TriggerChangingMode.TryAdd(new LoadPortMode(LoadPortLoadingMode.Cassette, indexInputCassetteChanger), false);
            TriggerChangingMode.TryAdd(new LoadPortMode(LoadPortLoadingMode.Foup, indexInputFoupChanger), false);

            int indexInputCassetteMode = (int)Define.DefineEnumProject.DigitalIO.PWA500BIN.EN_DIGITAL_IN.LP1_PLACEMENT_CASSETTE_STATUS + relIndex;
            int indexInputFoupMode = (int)Define.DefineEnumProject.DigitalIO.PWA500BIN.EN_DIGITAL_IN.LP1_PLACEMENT_MAC_FOUP_STATUS + relIndex;
            //LoadPortModeSignals = new Dictionary<LoadPortLoadingMode, int>
            //{
            //    { LoadPortLoadingMode.Cassette, indexInputCassetteMode },
            //    { LoadPortLoadingMode.Foup, indexInputFoupMode }
            //};

            //CarrierPresenceIndex = (int)Define.DefineEnumProject.DigitalIO.PWA500BIN.EN_DIGITAL_IN.LP1_PRESENT_STATUS + relIndexInput;
            #endregion </Assign Digital IO>

            _lotHistoryLog = LotHistoryLog.Instance;
            string name = _loadPortManager.GetLoadPortName(LoadPortIndex);
            _lotHistoryLog.AddLogInfo(PortId, name);
        }
        #endregion </Constructors>

        #region <Fields>
        private readonly ScenarioListTypes ScenarioTypeToIdRead;            // 1~6
        private readonly ScenarioListTypes ScenarioTypeToRequestLotInfo;    // 4~6
        private readonly ScenarioListTypes ScenarioTypeToSlotVerification;  // 4~6
        private readonly ScenarioListTypes ScenarioTypeToSlotMapping;       // 1~6
        private readonly ScenarioListTypes ScenarioTypeToLotMerge;          // 1~3, 5~6(1~3은 Change 포함)

        private readonly Enum ScenarioTypeToCarrierLoad;
        private readonly Enum ScenarioTypeToCarrierUnload;

        private const int CarrierMaxCapacity = 25;
        private const int DelayBeforeIdReadScenario = 3000;

        private CommandResults _commandResult = new CommandResults("", CommandResult.Invalid);
        private static TaskLoadPortRecovery500BIN _recovery;
        string _lotId = string.Empty;
        string _partId = string.Empty;
        string _stepSeq = string.Empty;
        string _lotType = string.Empty;
        string _recipeId = string.Empty;

        private string _carrierIdToWrite = string.Empty;

        private static ScenarioManagerForPWA500BIN_TP _scenarioManager = null;
        private static LotHistoryLog _lotHistoryLog = null;
        private readonly ConcurrentDictionary<LoadPortMode, bool> TriggerChangingMode;
        #endregion </Fields>

        #region <Properties>
        bool NeedExecuteToScenario
        {
            get
            {
                return (_carrierServer.GetCarrierAccessingStatus(PortId).Equals(CarrierAccessStates.NotAccessed) ||
                    _carrierServer.GetCarrierAccessingStatus(PortId).Equals(CarrierAccessStates.Unknown));
            }
        }
        SubstrateType MySubstrateType
        {
            get
            {
                return _scenarioManager.GetSubstrateTypeByLoadPortIndex(LoadPortIndex);
            }
        }
        #endregion </Properties>

        #region <Methods>

        #region <Overrides>
        protected override bool GetBusyIndex(int lpIndex, ref int indexOfDigital)
        {
            int relIndex = lpIndex * 8;
            indexOfDigital = (int)Define.DefineEnumProject.DigitalIO.PWA500BIN.EN_DIGITAL_IN.LP1_RUN + relIndex;

            return true;
        }
        protected override void ExecuteAtAlways()
        {
            if (EquipmentState_.EquipmentState.GetInstance().GetState().Equals(EquipmentState_.EQUIPMENT_STATE.IDLE) || EquipmentState_.EquipmentState.GetInstance().GetState().Equals(EquipmentState_.EQUIPMENT_STATE.PAUSE))
            {
                foreach (var item in TriggerChangingMode)
                {
                    if (item.Value != DigitalIO_.DigitalIO.GetInstance().ReadInput(item.Key.DigitalInputIndex))
                    {
                        TriggerChangingMode[item.Key] = !item.Value;
                        if (TriggerChangingMode[item.Key])
                        {
                            ChangingModeButtonClicked(item.Key.LoadingType);
                        }
                        //_loadPortManager.ChangeLoadPortMode(MyLoadPortIndex, item.Key.LoadingType);
                    }
                }
            }
        }
        protected override void GetAtmRobotTaskName(out List<string> taskNames)
        {
            taskNames = new List<string>();
            taskNames.Add("AtmRobot");
        }
        protected override void ExecuteOnCarrierPlaced()
        {
        }
        protected override void ExecuteOnCarrierRemoved()
        {
        }
        protected override void OnCarrierAccessStatusChanged(CarrierAccessStates newAccessStatus)
        {            
        }
        protected override bool UpdateParamToCarrierIdRead()
        {
            if (false == _scenarioOperator.UseScenario)
                return false;

            if (false == _carrierServer.HasCarrier(PortId))
                return false;

            if (false == NeedExecuteToScenario)
                return false;

            InitResult(ScenarioTypeToIdRead);

            var param = new Dictionary<string, string>
            {
                [RFIDReadKeys.KeyParamLotId] = _carrierServer.GetCarrierLotId(PortId),
                [RFIDReadKeys.KeyParamCarrierId] = _carrierServer.GetCarrierId(PortId),
                [RFIDReadKeys.KeyParamPortId] = _scenarioManager.GetPortName(PortId),
                [RFIDReadKeys.KeyParamOperatorId] = "AUTO"
            };

            // 2024.12.29. jhlim [ADD] 고객사 요청으로 id read 딜레이 추가
            SetDelayForSequence(DelayBeforeIdReadScenario);

            return _scenarioOperator.UpdateScenarioParam(ScenarioTypeToIdRead, param);
        }
        protected override CommandResults ExecuteScenarioToCarrierIdRead()
        {
            return RunScenario(ScenarioTypeToIdRead);
        }
        protected override bool UpdateParamToIdVarification()
        {
            if (PortId < 4)
                return false;

            if (false == _scenarioOperator.UseScenario)
                return false;

            if (false == _carrierServer.HasCarrier(PortId))
                return false;

            if (false == NeedExecuteToScenario)
                return false;

            InitResult(ScenarioTypeToRequestLotInfo);

            var param = new Dictionary<string, string>
            {
                [LotInfoKeys.KeyParamLotId] = _carrierServer.GetCarrierLotId(PortId),
                [LotInfoKeys.KeyParamCarrierId] = _carrierServer.GetCarrierId(PortId),
            };
            return _scenarioOperator.UpdateScenarioParam(ScenarioTypeToRequestLotInfo, param);
        }
        protected override CommandResults ExecuteScenarioToIdVarification()
        {
            if (PortId < 4)
            {
                _commandResult.ActionName = "Idle";
                _commandResult.CommandResult = CommandResult.Completed;
                _commandResult.Description = string.Empty;
                return _commandResult;
            }

            if (false == _carrierServer.HasCarrier(PortId))
            {
                _commandResult.CommandResult = CommandResult.Error;
                _commandResult.Description = "Doesn't have carrier";
                return _commandResult;
            }

            _commandResult = RunScenario(ScenarioTypeToRequestLotInfo);

            return _commandResult;
        }
        protected override bool UpdateParamToSlotMapVarification()
        {
            if (PortId < 4)
                return false;

            if (false == _carrierServer.HasCarrier(PortId))
                return false;

            if (false == NeedExecuteToScenario)
                return false;

            if (false == _scenarioOperator.UseScenario)
            {
                string lotId = _carrierServer.GetCarrierLotId(PortId);
                AssignSubstrateInfoByCarrierRFIDInfo(lotId);

                return false;
            }

            InitResult(ScenarioTypeToSlotVerification);

            var param = new Dictionary<string, string>
            {
                [LotInfoKeys.KeyParamLotId] = _carrierServer.GetCarrierLotId(PortId),
                [LotInfoKeys.KeyParamCarrierId] = _carrierServer.GetCarrierId(PortId),
            };

            return _scenarioOperator.UpdateScenarioParam(ScenarioTypeToSlotVerification, param);
        }
        protected override CommandResults ExecuteToSlotMapVarification()
        {
            if (PortId < 4)
            {
                _commandResult.ActionName = "Idle";
                _commandResult.CommandResult = CommandResult.Completed;
                _commandResult.Description = string.Empty;
                return _commandResult;
            }

            if (false == _carrierServer.HasCarrier(PortId))
            {
                _commandResult.CommandResult = CommandResult.Error;
                _commandResult.Description = "Doesn't have carrier";
                return _commandResult;
            }

            _commandResult = RunScenario(ScenarioTypeToSlotVerification);
            return _commandResult;          
        }
        protected override bool EnqueueScenraioBeforeActionCompletion(out QueuedScenarioInfo scenarioInfo)
        {
            scenarioInfo = new QueuedScenarioInfo();

            if (false == _scenarioOperator.UseScenario)
                return false;

            if (false == _carrierServer.GetCarrierAccessingStatus(PortId).Equals(CarrierAccessStates.CarrierCompleted))
                return false;

            Dictionary<string, string> scenarioParam = new Dictionary<string, string>();

            bool needExecuteMerge;
            // 2024.10.23. jhlim [MOD] 고객사 요청으로 머지 실행 여부 추가
            switch (MySubstrateType)
            {
                case SubstrateType.Core:
                    {
                        needExecuteMerge = GetSubstrateToMerge(out _);
                    }
                    break;
                case SubstrateType.Empty:
                    needExecuteMerge = false;
                    break;
                default:
                    needExecuteMerge = true;
                    break;
            }
            if (false == needExecuteMerge)
            {
                if (false == MakeScenarioParamForSlotMapping(ref scenarioParam))
                    return false;

                scenarioInfo.Scenario = ScenarioTypeToSlotMapping;
            }
            else
            {
                if (false == MakeScenarioParamForMergeLot(ref scenarioParam))
                    return false;

                scenarioInfo.Scenario = ScenarioTypeToLotMerge;
            }
            // 2024.10.23. jhlim [END]

            #region <Old>
            //// 2024.09.29. jhlim [MOD] 고객사 요청으로 순서 변경
            //// 코어는 머지 -> 슬롯매핑, 공테이프는 슬롯매핑, 빈은 머지&체인지 -> 슬롯매핑 순으로 진행한다.
            ////if (false == MakeScenarioParamForSlotMapping(ref scenarioParam))
            ////    return false;
            ////scenarioInfo.Scenario = ScenarioTypeToSlotMapping;
            //if (MySubstrateType.Equals(SubstrateType.Empty))
            //{
            //    if (false == MakeScenarioParamForSlotMapping(ref scenarioParam))
            //        return false;

            //    scenarioInfo.Scenario = ScenarioTypeToSlotMapping;
            //}
            //else
            //{
            //    if (false == MakeScenarioParamForMergeLot(ref scenarioParam))
            //        return false;

            //    scenarioInfo.Scenario = ScenarioTypeToLotMerge;
            //}
            //// 2024.09.29. jhlim [END]
            #endregion </Old>

            scenarioInfo.ScenarioParams = scenarioParam;

            return true;
        }
        protected override void ExecuteAfterScenarioCompletion(Enum scenario, EN_SCENARIO_RESULT result, Dictionary<string, string> scenarioParam, Dictionary<string, string> additionalParams)
        {
            if (scenario.GetType().Equals(typeof(ScenarioListTypes)))
            {
                ScenarioListTypes typeOfScenario = (ScenarioListTypes)scenario;
                switch (typeOfScenario)
                {
                    case ScenarioListTypes.SCENARIO_REQ_LOT_MERGE_CORE_1:
                    case ScenarioListTypes.SCENARIO_REQ_LOT_MERGE_CORE_2:
                    case ScenarioListTypes.SCENARIO_REQ_LOT_ID_MERGE_AND_CHANGE_BIN_1:
                    case ScenarioListTypes.SCENARIO_REQ_LOT_ID_MERGE_AND_CHANGE_BIN_2:
                    case ScenarioListTypes.SCENARIO_REQ_LOT_ID_MERGE_AND_CHANGE_BIN_3:
                        {
                            if (false == result.Equals(EN_SCENARIO_RESULT.COMPLETED))
                            {
                                return;
                            }

                            if (false == MySubstrateType.Equals(SubstrateType.Empty))
                            {
                                var scenarioResult = _scenarioOperator.GetScenarioResultData(typeOfScenario);
                                if (false == ApplyResultOfMergingLot(scenarioResult))
                                    return;

                                // 2024.09.29. jhlim [MOD] 고객사 요청으로 순서 변경(랏 머지&체인지 후 매핑 진행)
                                Dictionary<string, string> param = new Dictionary<string, string>();
                                if (false == MakeScenarioParamForSlotMapping(ref param))
                                    return;

                                EnqueueScenario(ScenarioTypeToSlotMapping, param, null);
                                // 2024.09.29. jhlim [END]

                                // 2024.09.29. jhlim [DEL] LotId Change는 Merge에 병합됨
                                //Dictionary<string, string> param = new Dictionary<string, string>();
                                //if (MakeScenarioParamForChangeToLotId(ref param))
                                //{
                                //    EnqueueScenario(ScenarioTypeToLotIdChange, param, null);
                                //}
                            }
                        }
                        break;

                    case ScenarioListTypes.SCENARIO_SLOT_WAFER_MAPPING_CORE_1:
                    case ScenarioListTypes.SCENARIO_SLOT_WAFER_MAPPING_CORE_2:
                    case ScenarioListTypes.SCENARIO_SLOT_WAFER_MAPPING_EMPTY_TAPE:
                    case ScenarioListTypes.SCENARIO_SLOT_WAFER_MAPPING_BIN_1:
                    case ScenarioListTypes.SCENARIO_SLOT_WAFER_MAPPING_BIN_2:
                    case ScenarioListTypes.SCENARIO_SLOT_WAFER_MAPPING_BIN_3:
                        {
                            if (false == result.Equals(EN_SCENARIO_RESULT.COMPLETED))
                                return;

                            _recovery.LotCompleted = true;
                            string carrierId = _carrierServer.GetCarrierId(PortId);
                            List<string> substrates = null;
                            if (PortId != 4)
                            {
                                var temporarySubstrates = _substrateManager.GetSubstratesAtLoadPort(PortId);
                                if(temporarySubstrates != null)
                                {
                                    substrates = new List<string>();
                                    foreach (var item in temporarySubstrates)
                                    {
                                        substrates.Add(item.Value.GetName());
                                    }
                                }
                            }

                            bool isCore = typeOfScenario.Equals(ScenarioListTypes.SCENARIO_SLOT_WAFER_MAPPING_CORE_1) ||
                                typeOfScenario.Equals(ScenarioListTypes.SCENARIO_SLOT_WAFER_MAPPING_CORE_2);

                            string lotId = _carrierServer.GetCarrierLotId(PortId);
                            if (false == isCore)
                            {
                                lotId = _carrierIdToWrite;
                            }
                            _lotHistoryLog.BackupCarrierHistory(PortId, carrierId, lotId, substrates, isCore);
                            //if (PortId == 4)
                            //{
                            //    // Empty는 Merge 를 진행하지 않는다. -> 끝나네.. 할게없다.
                            //    _recovery.LotCompleted = true;
                            //}
                            //else
                            //{
                            //    Dictionary<string, string> param = new Dictionary<string, string>();
                            //    if (MakeScenarioParamForMergeLot(ref param))
                            //    {
                            //        EnqueueScenario(ScenarioTypeToLotMerge, param, null);
                            //    }
                            //}
                        }
                        break;

                    default:
                        break;
                }
            }
        }
        protected override bool UpdateParamToLoadCarrier()
        {
            switch (MySubstrateType)
            {
                case SubstrateType.Core:
                case SubstrateType.Empty:
                    {
                        _commandResult.ActionName = ScenarioTypeToCarrierLoad.ToString();
                        _loadingMode = _loadPortManager.GetCarrierLoadingType(LoadPortIndex);
                        ScenarioListTypes typeOfScenario = (ScenarioListTypes)ScenarioTypeToCarrierLoad;

                        _scenarioManager.EnqueueScenarioCarrierHandlingAsync(PortId, _loadingMode, string.Empty, typeOfScenario);
                        return true;
                        //var param = _scenarioManager.MakeParamToOHTHandling(PortId, _loadingMode, string.Empty, typeOfScenario);
                        //return _scenarioOperator.UpdateScenarioParam(ScenarioTypeToCarrierLoad, param);
                    }

                case SubstrateType.Bin1:
                case SubstrateType.Bin2:
                case SubstrateType.Bin3:
                    {
                        LoadPortLoadingMode loadingMode = LoadPortLoadingMode.Unknown;
                        for (int i = 0; i < _loadPortManager.Count; ++i)
                        {
                            var substrateType = _scenarioManager.GetSubstrateTypeByLoadPortIndex(i);
                            if (false == substrateType.Equals(SubstrateType.Core))
                                continue;

                            int portId = _loadPortManager.GetLoadPortPortId(i);
                            if (false == _carrierServer.HasCarrier(portId))
                                continue;

                            loadingMode = _loadPortManager.GetCarrierLoadingType(i);
                            break;
                        }

                        // 아직 Core 캐리어가 도착하지 않은 거다..
                        if (loadingMode.Equals(LoadPortLoadingMode.Unknown))
                            return false;

                        string binLotId = string.Empty;
                        switch (loadingMode)
                        {
                            case LoadPortLoadingMode.Cassette:
                                {
                                    binLotId = CarrierLotIdType.ECASSETTE.ToString();
                                }
                                break;
                            case LoadPortLoadingMode.Foup:
                                {
                                    binLotId = CarrierLotIdType.PEMAC.ToString();
                                }
                                break;
                        }

                        _commandResult.ActionName = ScenarioTypeToCarrierLoad.ToString();
                        _loadingMode = _loadPortManager.GetCarrierLoadingType(LoadPortIndex);
                        ScenarioListTypes typeOfScenario = (ScenarioListTypes)ScenarioTypeToCarrierLoad;

                        _scenarioManager.EnqueueScenarioCarrierHandlingAsync(PortId, _loadingMode, binLotId, typeOfScenario);
                        return true;
                        //var param = _scenarioManager.MakeParamToOHTHandling(PortId, _loadingMode, binLotId, typeOfScenario);
                        //return _scenarioOperator.UpdateScenarioParam(ScenarioTypeToCarrierLoad, param);
                    }

                default:
                    break;
            }

            return false;
        }
        protected override CommandResults ExecuteScenarioToLoadCarrier()
        {
            _commandResult.CommandResult = CommandResult.Completed;
            return _commandResult;

            //var result = _scenarioOperator.ExecuteScenario(ScenarioTypeToCarrierLoad);
            //switch (result)
            //{
            //    case EN_SCENARIO_RESULT.PROCEED:
            //        _commandResult.CommandResult = CommandResult.Proceed;
            //        break;
            //    case EN_SCENARIO_RESULT.COMPLETED:
            //        _commandResult.CommandResult = CommandResult.Completed;
            //        break;
            //    case EN_SCENARIO_RESULT.ERROR:
            //        _commandResult.CommandResult = CommandResult.Error;
            //        _commandResult.Description = "Scenario Error";
            //        break;
            //    case EN_SCENARIO_RESULT.TIMEOUT_ERROR:
            //        _commandResult.CommandResult = CommandResult.Timeout;
            //        _commandResult.Description = "Scenario Timeout";
            //        break;
            //    default:
            //        break;
            //}

            //return _commandResult;
        }
        protected override bool UpdateParamToUnloadCarrier()
        {
            _commandResult.ActionName = ScenarioTypeToCarrierUnload.ToString();
            _loadingMode = _loadPortManager.GetCarrierLoadingType(LoadPortIndex);
            ScenarioListTypes typeOfScenario = (ScenarioListTypes)ScenarioTypeToCarrierUnload;
            string carrierLotId = _carrierServer.GetCarrierLotId(PortId);

            var param = _scenarioManager.MakeParamToOHTHandling(PortId, _loadingMode, carrierLotId, typeOfScenario);

            return _scenarioOperator.UpdateScenarioParam(ScenarioTypeToCarrierUnload, param);
        }
        protected override CommandResults ExecuteScenarioToUnloadCarrier()
        {
            var result = _scenarioOperator.ExecuteScenario(ScenarioTypeToCarrierUnload);
            switch (result)
            {
                case EN_SCENARIO_RESULT.PROCEED:
                    _commandResult.CommandResult = CommandResult.Proceed;
                    break;
                case EN_SCENARIO_RESULT.COMPLETED:
                    _commandResult.CommandResult = CommandResult.Completed;
                    break;
                case EN_SCENARIO_RESULT.ERROR:
                    _commandResult.CommandResult = CommandResult.Error;
                    _commandResult.Description = "Scenario Error";
                    break;
                case EN_SCENARIO_RESULT.TIMEOUT_ERROR:
                    _commandResult.CommandResult = CommandResult.Timeout;
                    _commandResult.Description = "Scenario Timeout";
                    break;
                default:
                    break;
            }

            return _commandResult;
        }
        protected override CommandResults WriteCarrierId()
        {
            _rfidManager.InitAction(LoadPortIndex, _loadingMode);
            return new CommandResults(string.Empty, CommandResult.Completed);
        }
        protected override CommandResults WriteLotId()
        {
            if (false == _scenarioOperator.UseScenario)
            {
                return new CommandResults(string.Empty, CommandResult.Skipped);
            }
            else
            {
                switch (MySubstrateType)
                {
                    case SubstrateType.Core:
                        {
                            bool needToMerge = GetSubstrateToMerge(out Dictionary<int, Substrate> substrates);
                            if (substrates.Count > 0 || needToMerge)
                            {
                                return new CommandResults(string.Empty, CommandResult.Skipped);
                            }
                            //bool needMerge = _recipe.GetValue(GetTaskName(), Define.DefineEnumProject.Task.LoadPort.PARAM_PROCESS.USE_TRACKOUT.ToString(), true);
                            //var substrates = _substrateManager.GetSubstratesAtLoadPort(PortId);
                            //foreach (var item in substrates)
                            //{
                            //    string qtyString = item.Value.GetAttribute(PWA500BINSubstrateAttributes.ChipQty);
                            //    if (false == int.TryParse(qtyString, out int qty))
                            //        continue;

                            //    if (qty > 0 || needMerge)
                            //        return new CommandResults(string.Empty, CommandResult.Skipped);
                            //}
                            _loadingMode = _loadPortManager.GetCarrierLoadingType(LoadPortIndex);
                            switch (_loadingMode)
                            {
                                case LoadPortLoadingMode.Cassette:
                                    _carrierIdToWrite = CarrierLotIdType.RCASSETTE.ToString();
                                    break;
                                case LoadPortLoadingMode.Foup:
                                    // 2024.11.27. jhlim [MOD] 고객사 요청으로 명칭 변경
                                    _carrierIdToWrite = CarrierLotIdType.PRMAC.ToString();
                                    // 2024.11.27. jhlim [END]
                                    break;
                                default:
                                    break;
                            }
                        }
                        break;

                    case SubstrateType.Empty:
                        {
                            var substrates = _substrateManager.GetSubstratesAtLoadPort(PortId);
                            if (substrates.Count > 0)
                                return new CommandResults(string.Empty, CommandResult.Skipped);
                            else
                            {
                                _loadingMode = _loadPortManager.GetCarrierLoadingType(LoadPortIndex);
                                switch (_loadingMode)
                                {
                                    case LoadPortLoadingMode.Cassette:
                                        _carrierIdToWrite = CarrierLotIdType.ECASSETTE.ToString();
                                        break;
                                    case LoadPortLoadingMode.Foup:
                                        // 2024.11.27. jhlim [MOD] 고객사 요청으로 명칭 변경
                                        _carrierIdToWrite = CarrierLotIdType.PRMAC.ToString();
                                        // 2024.11.27. jhlim [END]
                                        break;
                                    default:
                                        break;
                                }
                            }
                        }
                        break;

                    default:
                        break;
                }
                _loadingMode = _loadPortManager.GetCarrierLoadingType(LoadPortIndex);

                var result = _rfidManager.WriteLotId(LoadPortIndex, _loadingMode, _carrierIdToWrite);
                if (result.CommandResult == CommandResult.Completed)
                {
                    _carrierServer.SetCarrierLotId(PortId, _carrierIdToWrite);
                    _rfidManager.InitAction(LoadPortIndex, _loadingMode);
                }

                return result;
            }
        }
        protected override CommandResults ExecuteAfterWriting()
        {
            if (false == _scenarioOperator.UseScenario)
            {
                return new CommandResults(string.Empty, CommandResult.Skipped);
            }
            else
            {
                switch (MySubstrateType)
                {
                    case SubstrateType.Core:
                    case SubstrateType.Empty:
                        return new CommandResults(string.Empty, CommandResult.Skipped);

                    default:
                        break;
                }

                _loadingMode = _loadPortManager.GetCarrierLoadingType(LoadPortIndex);
                string readLotId = string.Empty;
                _commandResult = _rfidManager.ReadLotId(LoadPortIndex, _loadingMode, ref readLotId);
                switch (_commandResult.CommandResult)
                {
                    case CommandResult.Completed:
                    case CommandResult.Skipped:
                        {
                            if (false == readLotId.Equals(_carrierIdToWrite))
                            {
                                _commandResult = new CommandResults(_commandResult.ActionName, CommandResult.Error, string.Format("Lot Id does not match"));
                            }
                        }
                        break;

                    default:
                        break;
                }

                return _commandResult;
            }
        }
        #endregion </Overrides>

        #region <Internal Interfaces>
        private void InitResult(ScenarioListTypes scenario)
        {
            _commandResult.ActionName = scenario.ToString();
            _commandResult.CommandResult = CommandResult.Proceed;
            _commandResult.Description = string.Empty;
        }
        private CommandResults RunScenario(Enum scenario)
        {
            if (false == _carrierServer.HasCarrier(PortId))
            {
                _commandResult.CommandResult = CommandResult.Error;
                _commandResult.Description = "Doesn't have carrier";
                return _commandResult;
            }

            var result = _scenarioOperator.ExecuteScenario(scenario);
            _commandResult.ActionName = scenario.ToString();
            switch (result)
            {
                case EN_SCENARIO_RESULT.PROCEED:
                    _commandResult.CommandResult = CommandResult.Proceed;
                    break;
                case EN_SCENARIO_RESULT.COMPLETED:
                    _commandResult.CommandResult = CommandResult.Completed;
                    ExecuteAfterScenarioCompletedForHistory(scenario);
                    break;
                case EN_SCENARIO_RESULT.ERROR:                    
                    _commandResult.CommandResult = CommandResult.Error;
                    _commandResult.Description = "Scenario Error";
                    break;
                case EN_SCENARIO_RESULT.TIMEOUT_ERROR:
                    _commandResult.CommandResult = CommandResult.Timeout;
                    _commandResult.Description = "Scenario Timeout";
                    break;

                default:
                    break;
            }
            return _commandResult;
        }

        // 1. 모든 코어가 전량 소진된 경우 머지 진행하지 않음
        // 2. 전량 소진된 자재가 없는 경우 머지 진행
        // 3. 공존하는 경우
        //  3-1. 1개면 머지진행하지 않음
        //  3-2. 2개 이상이면 전량 소진되지 않은 것만 머지 진행
        private bool GetSubstrateToMerge(out Dictionary<int, Substrate> substrates)
        {
            substrates = new Dictionary<int, Substrate>();
            var temporarySubstrates = _substrateManager.GetSubstratesAtLoadPort(PortId);

            bool hasTerminatedSubstrate = false;
            foreach (var item in temporarySubstrates)
            {
                string qtyString = item.Value.GetAttribute(PWA500BINSubstrateAttributes.ChipQty);
                if (false == int.TryParse(qtyString, out int qty))
                    continue;

                if (qty > 0)
                {
                    substrates[item.Key] = item.Value;
                }
                else
                {
                    hasTerminatedSubstrate = true;
                }
            }

            if (false == hasTerminatedSubstrate)
            {
                return true;
            }
            else
            {
                return substrates.Count > 1;
            }
        }
        private bool MakeScenarioParamForSlotMapping(ref Dictionary<string, string> scenarioParam)
        {
            if (scenarioParam == null)
                scenarioParam = new Dictionary<string, string>();

            scenarioParam.Clear();

            // 1~6 포트 모두 진행
            var substrates = _substrateManager.GetSubstratesAtLoadPort(PortId);
            string lotId = string.Empty;
            switch (MySubstrateType)
            {
                case SubstrateType.Bin1:
                case SubstrateType.Bin2:
                case SubstrateType.Bin3:
                    {
                        if (substrates.Count <= 0)
                            return false;
                        var substrateFirst = substrates.First();
                        lotId = substrateFirst.Value.GetLotId();
                    }
                    break;
                default:
                    {
                        lotId = _carrierServer.GetCarrierLotId(PortId);
                    }
                    break;
            }

            string carrierId = _carrierServer.GetCarrierId(PortId);
            scenarioParam[SlotMappingKeys.KeyParamLotId] = lotId;
            scenarioParam[SlotMappingKeys.KeyParamCarrierId] = carrierId;

            Dictionary<int, Tuple<string, string>> substratesToMapping = new Dictionary<int, Tuple<string, string>>();

            string[] substrateIds = new string[CarrierMaxCapacity];
            string[] substrateQtys = new string[CarrierMaxCapacity];
            for (int i = 0; i < CarrierMaxCapacity; ++i)
            {
                string id = string.Empty;
                string qty = "";
                if (substrates.TryGetValue(i, out Substrate substrate))
                {
                    id = substrate.GetName();
                    qty = substrate.GetAttribute(PWA500BINSubstrateAttributes.ChipQty);
                    if (qty.Equals("0"))
                        qty = string.Empty;

                    substratesToMapping[i] = Tuple.Create(id, qty);
                }
                substrateIds[i] = id;
                substrateQtys[i] = qty;
            }

            for (int i = 0; i < CarrierMaxCapacity; ++i)
            {
                string keyForId = string.Format("{0}{1}_{2}", SlotMappingKeys.KeyParamSlotNamePre, i + 1, SlotMappingKeys.KeyParamSlotNamePost);
                scenarioParam[keyForId] = substrateIds[i];
            }

            for (int i = 0; i < CarrierMaxCapacity; ++i)
            {
                string keyForQty = string.Format("{0}{1}_{2}", SlotMappingKeys.KeyParamSlotQtyPre, i + 1, SlotMappingKeys.KeyParamSlotQtyPost);
                scenarioParam[keyForQty] = substrateQtys[i];
            }

            _lotHistoryLog.WriteHistoryForSlotMapping(PortId, carrierId, substratesToMapping);

            return true;
        }
        private bool MakeScenarioParamForChangeToLotId(ref Dictionary<string, string> scenarioParam)
        {
            if (scenarioParam == null)
                scenarioParam = new Dictionary<string, string>();

            scenarioParam.Clear();

            // 1~6 포트 모두 진행
            string lotId = _carrierServer.GetCarrierLotId(PortId);
            string carrierId = _carrierServer.GetCarrierId(PortId);

            scenarioParam[ChangeToLotIdKeys.KeyParamLotId] = lotId;
            scenarioParam[ChangeToLotIdKeys.KeyParamCarrierId] = carrierId;

            return true;
        }
        private bool MakeScenarioParamForMergeLot(ref Dictionary<string, string> scenarioParam)
        {
            if (scenarioParam == null)
                scenarioParam = new Dictionary<string, string>();

            scenarioParam.Clear();

            // 1~6 포트 모두 진행
            string lotId = _carrierServer.GetCarrierLotId(PortId);
            string carrierId = _carrierServer.GetCarrierId(PortId);

            // 2024.10.23. jhlim [MOD] 코어는 머지할 자재를 모두 가져오는 것이 아닌, 자재 정보를 통해 선별된 것만 가져온다.
            Dictionary<int, Substrate> substrates;
            if (MySubstrateType.Equals(SubstrateType.Core))
            {
                GetSubstrateToMerge(out substrates);
            }
            else
            {
                substrates = _substrateManager.GetSubstratesAtLoadPort(PortId);
            }
            // 2024.10.23. jhlim [END]

            if (substrates.Count <= 0)
                return false;

            var firstSubstrate = substrates.First();
            switch (MySubstrateType)
            {
                case SubstrateType.Core:
                    {
                        bool hasParentLotId = false;
                        foreach (var item in substrates)
                        {
                            if (item.Value.GetLotId().Equals(lotId))
                            {
                                hasParentLotId = true;
                                break;
                            }
                        }

                        if (false == hasParentLotId)
                        {
                            lotId = firstSubstrate.Value.GetLotId();
                        }
                    }
                    break;
                case SubstrateType.Bin1:
                case SubstrateType.Bin2:
                case SubstrateType.Bin3:
                    // Bin의 경우 첫 번째 Lot 이름을 대표이름으로 병합한다. -> LotId Change 이후 새로운 Lot Id 부여받음
                    lotId = firstSubstrate.Value.GetLotId();
                    break;
                default:
                    break;
            }

            scenarioParam[LotMergeKeys.KeyParamLotId] = lotId;
            scenarioParam[LotMergeKeys.KeyParamCarrierId] = carrierId;


            string partId = firstSubstrate.Value.GetAttribute(PWA500BINSubstrateAttributes.PartId);
            string recipeId = _scenarioManager.GetRecipeId();

            scenarioParam[LotMergeKeys.KeyParamPartId] = partId;
            scenarioParam[LotMergeKeys.KeyParamRecipeId] = recipeId;
            scenarioParam[LotMergeKeys.KeyOperatorId] = "AUTO";

            for (int i = 0; i < CarrierMaxCapacity; ++i)
            {
                string substrateLotId = string.Empty;
                if (substrates.TryGetValue(i, out Substrate substrate))
                {
                    substrateLotId = substrate.GetLotId();
                }

                string keyForQty = string.Format("{0}{1}_{2}", LotMergeKeys.KeyParamSlotLotIdPre, i + 1, LotMergeKeys.KeyParamSlotLotIdPost);
                scenarioParam[keyForQty] = substrateLotId;
            }

            // Change를 위함
            if (PortId < 4)
            {
                string[] substrateIds = new string[CarrierMaxCapacity];
                string[] substrateQtys = new string[CarrierMaxCapacity];

                for (int i = 0; i < CarrierMaxCapacity; ++i)
                {
                    string id = string.Empty;
                    string qty = "0";
                    if (substrates.TryGetValue(i, out Substrate substrate))
                    {
                        id = substrate.GetName();
                        qty = substrate.GetAttribute(PWA500BINSubstrateAttributes.ChipQty);
                    }
                    substrateIds[i] = id;
                    substrateQtys[i] = qty;
                }

                for (int i = 0; i < CarrierMaxCapacity; ++i)
                {
                    string keyForId = string.Format("{0}{1}_{2}", SlotMappingKeys.KeyParamSlotNamePre, i + 1, SlotMappingKeys.KeyParamSlotNamePost);
                    scenarioParam[keyForId] = substrateIds[i];
                }

                for (int i = 0; i < CarrierMaxCapacity; ++i)
                {
                    string keyForQty = string.Format("{0}{1}_{2}", SlotMappingKeys.KeyParamSlotQtyPre, i + 1, SlotMappingKeys.KeyParamSlotQtyPost);
                    scenarioParam[keyForQty] = substrateQtys[i];
                }
            }
            return true;
        }
        private bool ApplyResultOfMergingLot(Dictionary<string, string> resultOfMergingLot)
        {
            if (false == resultOfMergingLot.TryGetValue(LotMergeKeys.KeyResultLotId, out string lotId))
                return false;

            _carrierIdToWrite = lotId;

            Dictionary<int, string> lotIdToMerge = new Dictionary<int, string>();

            // 새로 부여 받은 LotId로 모든 자재의 LotId를 갱신한다.
            var substrates = _substrateManager.GetSubstratesAtLoadPort(PortId);
            foreach (var item in substrates)
            {
                lotIdToMerge[item.Key] = item.Value.GetLotId();

                item.Value.SetLotId(lotId);
            }

            string carrierId = _carrierServer.GetCarrierId(PortId);
            _lotHistoryLog.WriteHistoryForMerge(PortId, carrierId, lotId, lotIdToMerge);

            return true;
        }
        private void AssignSubstrateInfoByCarrierRFIDInfo(string lotId)
        {
            var substrates = _substrateManager.GetSubstratesAtLoadPort(PortId);
            foreach (var item in substrates)
            {
                string prevLotId = item.Value.GetLotId();
                if (string.IsNullOrEmpty(prevLotId)/* || false == item.Value.GetLotId().Equals(lotId)*/)
                {
                    item.Value.SetLotId(lotId);
                    item.Value.SetAttribute(PWA500BINSubstrateAttributes.RingId, item.Value.GetName());
                }

                string prevParentLotId = item.Value.GetAttribute(PWA500BINSubstrateAttributes.ParentLotId);
                if (string.IsNullOrEmpty(prevParentLotId))
                {
                    item.Value.SetAttribute(PWA500BINSubstrateAttributes.ParentLotId, lotId);
                }
            }
        }
        private void ExecuteAfterScenarioCompletedForHistory(Enum scenario)
        {
            if (scenario.Equals(ScenarioTypeToIdRead))
            {
                _lotHistoryLog.WriteHistoryForIdRead(PortId, 
                    _carrierServer.GetCarrierId(PortId),
                    _carrierServer.GetCarrierLotId(PortId));
            }
            else
            {
                if (PortId > 3)
                {
                    if (scenario.Equals(ScenarioTypeToRequestLotInfo))
                    {
                        #region <Lot Info 갱신>
                        var scenarioResult = _scenarioOperator.GetScenarioResultData(ScenarioTypeToRequestLotInfo);
                        _lotId = scenarioResult[LotInfoKeys.KeyResultLotId];
                        _partId = scenarioResult[LotInfoKeys.KeyResultPartId];
                        _stepSeq = scenarioResult[LotInfoKeys.KeyResultStepSeq];
                        _lotType = scenarioResult[LotInfoKeys.KeyResultLotType];
                        _recipeId = scenarioResult[LotInfoKeys.KeyResultRecipeId];

                        if (string.IsNullOrEmpty(_lotId) ||
                            string.IsNullOrEmpty(_partId) ||
                            string.IsNullOrEmpty(_stepSeq) ||
                            string.IsNullOrEmpty(_lotType))
                        {
                            _commandResult.CommandResult = CommandResult.Error;
                            _commandResult.Description = "Scenario Result Error";
                            return;
                        }

                        if (MySubstrateType.Equals(SubstrateType.Core) ||
                            MySubstrateType.Equals(SubstrateType.Empty))
                        {
                            _carrierServer.SetCarrierLotId(PortId, _lotId);
                            _carrierServer.SetAttribute(PortId, PWA500BINCarrierAttributeKeys.KeyPartId, _partId);
                            _carrierServer.SetAttribute(PortId, PWA500BINCarrierAttributeKeys.KeyStepSeq, _stepSeq);
                        }

                        string carrierId = _carrierServer.GetCarrierId(PortId);
                        _lotHistoryLog.WriteHistoryForLotInfo(PortId, carrierId, _lotId, _partId, _stepSeq, _lotType);
                        #endregion </Lot Info 갱신>
                    }
                    else if (scenario.Equals(ScenarioTypeToSlotVerification))
                    {
                        #region <Slot Info 갱신>
                        var scenarioResult = _scenarioOperator.GetScenarioResultData(ScenarioTypeToSlotVerification);
                        if (false == scenarioResult.TryGetValue(SlotMapVefiricationKeys.KeyIsCancelCarrier, out string isCancelCarrier))
                            return;

                        bool.TryParse(isCancelCarrier, out _receivedCancelCarrier);
                        if (_receivedCancelCarrier)
                        {
                            // TODO : Cancel Carrier Logging 필요
                            _commandResult.CommandResult = CommandResult.Skipped;
                        }
                        else
                        {
                            Dictionary<int, string> scenarioResultForStatus = new Dictionary<int, string>();

                            var status = _carrierServer.GetCarrierSlotMap(PortId);
                            var substrates = _substrateManager.GetSubstratesAtLoadPort(PortId);
                            string statusKeyForSplit = string.Format("{0}_", SlotMapVefiricationKeys.KeyResultStatus);
                            foreach (var item in scenarioResult)
                            {
                                if (item.Key.Contains(statusKeyForSplit))
                                {
                                    string[] statusKey = item.Key.Split('_');
                                    if (statusKey.Length != 2)
                                        continue;

                                    if (false == int.TryParse(statusKey[1], out int index))
                                        continue;

                                    if (index < 0 || index > status.Length - 1)
                                        continue;

                                    if (item.Value == "4")       // Exist
                                    {
                                        scenarioResultForStatus[index] = item.Value;

                                        if (false == status[index].Equals(CarrierSlotMapStates.CorrectlyOccupied))
                                        {
                                            continue;
                                        }
                                        else
                                        {
                                            string keyForLotId = string.Format("{0}_{1}", SlotMapVefiricationKeys.KeyResultLotId, index);
                                            string keyForSubstrateId = string.Format("{0}_{1}", SlotMapVefiricationKeys.KeyResultName, index);
                                            if (substrates.ContainsKey(index))
                                            {
                                                if (scenarioResult.ContainsKey(keyForLotId))
                                                {
                                                    substrates[index].SetLotId(scenarioResult[keyForLotId]);

                                                    if (string.IsNullOrEmpty(substrates[index].GetAttribute(PWA500BINSubstrateAttributes.ParentLotId)))
                                                    {
                                                        substrates[index].SetAttribute(PWA500BINSubstrateAttributes.ParentLotId, scenarioResult[keyForLotId]);
                                                    }
                                                }
                                                if (scenarioResult.ContainsKey(keyForSubstrateId))
                                                {
                                                    // 2024.12.29. jhlim [MOD] Ring Id를 고유하게 만들기 위함 : CarrierId_LP{포트번호}.{슬롯번호} 형식
                                                    substrates[index].SetAttribute(PWA500BINSubstrateAttributes.RingId, substrates[index].GetName());
                                                    //substrates[index].SetName(scenarioResult[keyForSubstrateId]);
                                                    //substrates[index].SetAttribute(PWA500BINSubstrateAttributes.RingId, scenarioResult[keyForSubstrateId]);
                                                    // 2024.12.29. jhlim [END]
                                                }

                                                #region <Lot Info 갱신>
                                                substrates[index].SetRecipeId(_recipeId);
                                                substrates[index].SetAttribute(PWA500BINSubstrateAttributes.PartId, _partId);
                                                substrates[index].SetAttribute(PWA500BINSubstrateAttributes.StepSeq, _stepSeq);
                                                substrates[index].SetAttribute(PWA500BINSubstrateAttributes.LotType, _lotType);
                                                #endregion </Lot Info 갱신>
                                            }
                                        }
                                    }
                                    else
                                    {
                                        // 서버에서는 없다고 하는데 실제로 있는 경우(이미 나가있는 자재 포함)
                                        if (status[index].Equals(EFEM.Defines.LoadPort.CarrierSlotMapStates.CorrectlyOccupied))
                                        {
                                            Substrate temporarySubstrate;
                                            if (substrates.ContainsKey(index))
                                            {
                                                temporarySubstrate = substrates[index];
                                            }
                                            else
                                            {
                                                string carrierId = _carrierServer.GetCarrierId(PortId);
                                                temporarySubstrate = new Substrate("");
                                                if (false == _substrateManager.GetSubstrateBySourceCarrierInfo(PortId, index, carrierId, ref temporarySubstrate))
                                                    continue;
                                            }

                                            #region <Lot Info 갱신>
                                            temporarySubstrate.SetRecipeId(_recipeId);
                                            // 2024.12.29. jhlim [DEL] Ring Id를 고유하게 만들기 위함 : CarrierId_LP{포트번호}.{슬롯번호} 형식
                                            if (string.IsNullOrEmpty(temporarySubstrate.GetAttribute(PWA500BINSubstrateAttributes.RingId)))
                                            {
                                                temporarySubstrate.SetAttribute(PWA500BINSubstrateAttributes.RingId, temporarySubstrate.GetName());
                                            }
                                            // 2024.12.29. jhlim [END]

                                            temporarySubstrate.SetAttribute(PWA500BINSubstrateAttributes.PartId, _partId);
                                            temporarySubstrate.SetAttribute(PWA500BINSubstrateAttributes.StepSeq, _stepSeq);
                                            temporarySubstrate.SetAttribute(PWA500BINSubstrateAttributes.LotType, _lotType);
                                            if (string.IsNullOrEmpty(temporarySubstrate.GetAttribute(PWA500BINSubstrateAttributes.ParentLotId)))
                                            {
                                                string parentLotId = _carrierServer.GetCarrierLotId(PortId);
                                                temporarySubstrate.SetAttribute(PWA500BINSubstrateAttributes.ParentLotId, parentLotId);
                                            }

                                            #endregion </Lot Info 갱신>

                                            continue;
                                        }
                                        else
                                        {
                                            // 정상
                                        }
                                    }
                                }
                            }

                            string currentCarrierId = _carrierServer.GetCarrierId(PortId);
                            _lotHistoryLog.WriteHistoryForSlotMap(PortId, currentCarrierId, scenarioResultForStatus);
                        }
                        #endregion </Slot Info 갱신>
                    }

                }
            }
        }
        #endregion </Internal Interfaces>

        #endregion </Methods>
    }

    class TaskLoadPortRecovery500BIN : Work.RecoveryData
    {
        public TaskLoadPortRecovery500BIN(string taskName, int nPortCount)
            : base(taskName, nPortCount)
        {
        }

        #region <Fields>
        private const string KeyAccessStatus = "AccessStatus";
        private CarrierAccessStates _accessStatus;

        private bool _lotCompletionFlag;
        #endregion </Fields>

        #region <Properties>
        public CarrierAccessStates AccessStatus
        {
            get
            {
                return _accessStatus;
            }
            set
            {
                if (false == _accessStatus.Equals(value))
                {
                    _accessStatus = value;
                    //Save();
                }
            }
        }
        public bool LotCompleted
        {
            get
            {
                return _lotCompletionFlag;
            }
            set
            {
                if (false == _lotCompletionFlag.Equals(value))
                {
                    _lotCompletionFlag = value;
                    //Save();
                }
            }
        }
        #endregion </Properties>

        protected override void LoadData(ref FileComposite_.FileComposite fComp, string sRootName)
        {
            string value = string.Empty;
            fComp.GetValue(sRootName, KeyAccessStatus, ref value);
            if (false == Enum.TryParse(value, out _accessStatus))
            {
                AccessStatus = CarrierAccessStates.Unknown;
            }
            else
            {
                AccessStatus = _accessStatus;
            }
        }
        protected override void SaveData(ref FileComposite_.FileComposite fComp, string sRootName)
        {
            fComp.AddItem(sRootName, KeyAccessStatus, AccessStatus.ToString());
        }
    }
}
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;

using TickCounter_;

using FrameOfSystem3.Recipe;
using FrameOfSystem3.SECSGEM.DefineSecsGem;

using EFEM.MaterialTracking;
using EFEM.Defines.LoadPort;
using EFEM.Modules;
using EFEM.CustomizedByProcessType.PWA500W;

namespace FrameOfSystem3.SECSGEM.Scenario
{
    class ScenarioManagerForPWA500W_NRD
    {
        #region <Constructors>
        private ScenarioManagerForPWA500W_NRD()
        {
            _substrateManager = SubstrateManager.Instance;
            _carrierServer = CarrierManagementServer.Instance;
            _loadPortManager = LoadPortManager.Instance;
            _processGroup = ProcessModuleGroup.Instance;
            _recipe = Recipe.Recipe.GetInstance();

        }
        #endregion </Constructors>

        #region <Fields>
        private static ScenarioManagerForPWA500W_NRD _instance = null;

        private static SubstrateManager _substrateManager = null;
        private static CarrierManagementServer _carrierServer = null;
        private static ProcessModuleGroup _processGroup = null;
        private static LoadPortManager _loadPortManager = null;

        private static Recipe.Recipe _recipe = null;
        private Func<string, string, string, string, string[], string[], EN_MESSAGE_RESULT, bool, bool> _funcToSendClientMessage = null;
        private Action<Enum, Dictionary<string, string>, Dictionary<string, string>> _actionToEnqueueScenarioAsync = null;
        private const int ProcessModuleIndex = 0;

        private Func<string, Dictionary<string, string>, bool> _funcToUpdateScenarioParam = null;
        private Func<Enum, EN_SCENARIO_RESULT> _funcToExecuteScenario = null;

        private readonly TickCounter TicksForCarrierLoad = new TickCounter();
        private QueuedScenarioInfo _dequeuedScenarioToCarrierLoad = null;
        private readonly ConcurrentQueue<QueuedScenarioInfo> CarrierLoadingReservation = new ConcurrentQueue<QueuedScenarioInfo>();
        //private readonly TickCounter TicksForCarrierUnload = new TickCounter();
        //private QueuedScenarioInfo _dequeuedScenarioToCarrierUnload = null;
        //private readonly ConcurrentQueue<QueuedScenarioInfo> CarrierUnloadingReservation = new ConcurrentQueue<QueuedScenarioInfo>();
        #endregion </Fields>

        #region <Properties>
        public static ScenarioManagerForPWA500W_NRD Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ScenarioManagerForPWA500W_NRD();

                return _instance;
            }
        }
        private bool UseCoreMapHandlingOnly
        {
            get
            {
                return (false == _recipe.GetValue(EN_RECIPE_TYPE.COMMON, PARAM_COMMON.UseSecsGem.ToString(), false));
            }
        }
        #endregion </Properties>

        #region <Methods>

        #region <Assign Functions>
        public void AssignFunctionToSendClientMessage(Func<string, string, string, string, string[], string[], EN_MESSAGE_RESULT, bool, bool> func)
        {
            _funcToSendClientMessage = func;
        }
        public void AssignActionToEnqueueScenarioAsync(Action<Enum, Dictionary<string, string>, Dictionary<string, string>> action)
        {
            _actionToEnqueueScenarioAsync = action;
        }
        public void AssignFunctionToUpdateParam(Func<string, Dictionary<string, string>, bool> func)
        {
            _funcToUpdateScenarioParam = func;
        }
        public void AssignFunctionToExecuteScenario(Func<Enum, EN_SCENARIO_RESULT> func)
        {
            _funcToExecuteScenario = func;
        }
        #endregion </Assign Functions>

        #region <OHT Handling>
        // 
        public void EnqueueScenarioCarrierHandlingAsync(int portId, LoadPortLoadingMode loadingType, string lotId, ScenarioListTypes scenario)
        {
            var param = MakeParamToOHTHandling(portId, loadingType, lotId, scenario);
            var queuedScenario = new QueuedScenarioInfo
            {
                Scenario = scenario,
                ScenarioParams = param
            };
            if (scenario.Equals(ScenarioListTypes.SCENARIO_PORT_STATUS_LOAD_1) ||
                scenario.Equals(ScenarioListTypes.SCENARIO_PORT_STATUS_LOAD_2) ||
                scenario.Equals(ScenarioListTypes.SCENARIO_PORT_STATUS_LOAD_3) ||
                scenario.Equals(ScenarioListTypes.SCENARIO_PORT_STATUS_LOAD_4) ||
                scenario.Equals(ScenarioListTypes.SCENARIO_PORT_STATUS_LOAD_5) ||
                scenario.Equals(ScenarioListTypes.SCENARIO_PORT_STATUS_LOAD_6))
            {
                CarrierLoadingReservation.Enqueue(queuedScenario);
            }
            else if (scenario.Equals(ScenarioListTypes.SCENARIO_PORT_STATUS_UNLOAD_1) ||
                    scenario.Equals(ScenarioListTypes.SCENARIO_PORT_STATUS_UNLOAD_2) ||
                    scenario.Equals(ScenarioListTypes.SCENARIO_PORT_STATUS_UNLOAD_3) ||
                    scenario.Equals(ScenarioListTypes.SCENARIO_PORT_STATUS_UNLOAD_4) ||
                    scenario.Equals(ScenarioListTypes.SCENARIO_PORT_STATUS_UNLOAD_5) ||
                    scenario.Equals(ScenarioListTypes.SCENARIO_PORT_STATUS_UNLOAD_6))
            {
                //CarrierUnloadingReservation.Enqueue(queuedScenario);
            }

            string message = string.Format("[{0:d2}/{1:d2}-{2:d2}:{3:d2}:{4:d2}.{5:d3}] Scenario : {6} Enqueued !! ",
                DateTime.Now.Month,
                DateTime.Now.Day,
                DateTime.Now.Hour,
                DateTime.Now.Minute,
                DateTime.Now.Second,
                DateTime.Now.Millisecond,
                scenario.ToString());
            Console.WriteLine(message);
        }

        public void ExecuteScanrioToCarrierLoadAsync()
        {
            if (_funcToUpdateScenarioParam == null || _funcToExecuteScenario == null)
                return;

            if (UseCoreMapHandlingOnly)
            {
                while (CarrierLoadingReservation.Count > 0)
                {
                    CarrierLoadingReservation.TryDequeue(out _);
                }

                return;
            }    

            if (_dequeuedScenarioToCarrierLoad != null)
            {
                var result = _funcToExecuteScenario(_dequeuedScenarioToCarrierLoad.Scenario);
                switch (result)
                {
                    case EN_SCENARIO_RESULT.PROCEED:
                        return;

                    case EN_SCENARIO_RESULT.COMPLETED:
                    case EN_SCENARIO_RESULT.ERROR:
                    case EN_SCENARIO_RESULT.TIMEOUT_ERROR:
                        {
                            string message = string.Format("[{0:d2}/{1:d2}-{2:d2}:{3:d2}:{4:d2}.{5:d3}] Scenario : {6} Finished !! ",
                                                            DateTime.Now.Month,
                                                            DateTime.Now.Day,
                                                            DateTime.Now.Hour,
                                                            DateTime.Now.Minute,
                                                            DateTime.Now.Second,
                                                            DateTime.Now.Millisecond,
                                                            _dequeuedScenarioToCarrierLoad.Scenario.ToString());
                            Console.WriteLine(message);

                            TicksForCarrierLoad.SetTickCount(3000);
                            _dequeuedScenarioToCarrierLoad = null;

                            // 종료 중이면 비운다.
                            if (false == EquipmentState_.EquipmentState.GetInstance().GetState().Equals(EquipmentState_.EQUIPMENT_STATE.SETUP) &&
                                false == EquipmentState_.EquipmentState.GetInstance().GetState().Equals(EquipmentState_.EQUIPMENT_STATE.FINISHING) &&
                                false == EquipmentState_.EquipmentState.GetInstance().GetState().Equals(EquipmentState_.EQUIPMENT_STATE.EXECUTING))
                            {
                                while (CarrierLoadingReservation.Count > 0)
                                {
                                    CarrierLoadingReservation.TryDequeue(out _);
                                }
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
            else
            {
                if (CarrierLoadingReservation.Count <= 0)
                    return;

                // 셋 된 상태에서 Tick이 넘어가지 않았으면 리턴
                if (false == TicksForCarrierLoad.IsTickOver(false) &&
                    TicksForCarrierLoad.IsSet())
                    return;

                CarrierLoadingReservation.TryDequeue(out _dequeuedScenarioToCarrierLoad);
                // 파라메터 갱신
                Enum scenario = _dequeuedScenarioToCarrierLoad.Scenario;
                var scenarioParams = _dequeuedScenarioToCarrierLoad.ScenarioParams;
                _funcToUpdateScenarioParam(scenario.ToString(), scenarioParams);

                string message = string.Format("[{0:d2}/{1:d2}-{2:d2}:{3:d2}:{4:d2}.{5:d3}] Scenario : {6} Dequeued !! ",
                                DateTime.Now.Month,
                                DateTime.Now.Day,
                                DateTime.Now.Hour,
                                DateTime.Now.Minute,
                                DateTime.Now.Second,
                                DateTime.Now.Millisecond,
                                _dequeuedScenarioToCarrierLoad.Scenario.ToString());
                Console.WriteLine(message);
            }
        }
        #endregion </OHT Handling>

        public void ExecuteAfterScenarioCompletion(ScenarioListTypes typeOfScenario,
            Dictionary<string, string> scenarioParams,
            Dictionary<string, string> resultOfScenario,
            Dictionary<string, string> additionalParams,
            EN_MESSAGE_RESULT result,
            bool isManual = false)
        {
            // 완료된 시나리오 타입에 따라 실행되어야할 액션을 여기서 선택한다.
            switch (typeOfScenario)
            {
                case ScenarioListTypes.SCENARIO_WORK_START:
                    {
                        #region
                        Dictionary<string, string> messageContentToSend = new Dictionary<string, string>();
                        messageContentToSend[ResultKeys.KeyResult] = result.ToString();
                        if (result.Equals(EN_MESSAGE_RESULT.OK))
                        {
                            messageContentToSend[ResultKeys.KeyDescription] = string.Empty;
                        }
                        else
                        {
                            messageContentToSend[ResultKeys.KeyDescription] = "Gem Error";
                        }

                        if (false == resultOfScenario.TryGetValue(RequestDownloadMapFileKeys.KeyResultSubstrateId, out string resultSubstrateId))
                        {
                            result = EN_MESSAGE_RESULT.NG;
                            messageContentToSend[ResultKeys.KeyResult] = result.ToString();
                        }

                        if (false == resultOfScenario.TryGetValue(RequestDownloadMapFileKeys.KeyResultCountRow, out string resultCountRow))
                        {
                            result = EN_MESSAGE_RESULT.NG;
                            messageContentToSend[ResultKeys.KeyResult] = result.ToString();
                        }

                        if (false == resultOfScenario.TryGetValue(RequestDownloadMapFileKeys.KeyResultCountCol, out string resultCountCol))
                        {
                            result = EN_MESSAGE_RESULT.NG;
                            messageContentToSend[ResultKeys.KeyResult] = result.ToString();
                        }

                        if (false == resultOfScenario.TryGetValue(RequestDownloadMapFileKeys.KeyResultAngle, out string resultAngle))
                        {
                            result = EN_MESSAGE_RESULT.NG;
                            messageContentToSend[ResultKeys.KeyResult] = result.ToString();
                        }

                        if (false == resultOfScenario.TryGetValue(RequestDownloadMapFileKeys.KeyResultQty, out string resultQty))
                        {
                            result = EN_MESSAGE_RESULT.NG;
                            messageContentToSend[ResultKeys.KeyResult] = result.ToString();
                        }

                        if (false == resultOfScenario.TryGetValue(RequestDownloadMapFileKeys.KeyResultReferenceX, out string resultRefX))
                        {
                            result = EN_MESSAGE_RESULT.NG;
                            messageContentToSend[ResultKeys.KeyResult] = result.ToString();
                        }
                        if (false == resultOfScenario.TryGetValue(RequestDownloadMapFileKeys.KeyResultReferenceY, out string resultRefY))
                        {
                            result = EN_MESSAGE_RESULT.NG;
                            messageContentToSend[ResultKeys.KeyResult] = result.ToString();
                        }
                        if (false == resultOfScenario.TryGetValue(RequestDownloadMapFileKeys.KeyResultStartingX, out string resultStartX))
                        {
                            result = EN_MESSAGE_RESULT.NG;
                            messageContentToSend[ResultKeys.KeyResult] = result.ToString();
                        }
                        if (false == resultOfScenario.TryGetValue(RequestDownloadMapFileKeys.KeyResultStartingY, out string resultStartY))
                        {
                            result = EN_MESSAGE_RESULT.NG;
                            messageContentToSend[ResultKeys.KeyResult] = result.ToString();
                        }


                        if (false == resultOfScenario.TryGetValue(RequestDownloadMapFileKeys.KeyResultMapData, out string resultMapData))
                        {
                            result = EN_MESSAGE_RESULT.NG;
                            messageContentToSend[ResultKeys.KeyResult] = result.ToString();
                        }

                        Substrate substrate = new Substrate("");
                        if (isManual)
                        {
                            if (false == _substrateManager.GetSubstrateByName(resultSubstrateId, ref substrate))
                                return;

                            SetSubstrateAttributes(substrate,
                                resultSubstrateId,
                                resultAngle,
                                resultCountRow,
                                resultCountCol,
                                resultQty,
                                resultRefX,
                                resultRefY,
                                resultStartX,
                                resultStartY,
                                resultMapData);
                        }
                        else
                        {
                            if (additionalParams == null)
                                return;

                            if (false == additionalParams.TryGetValue(AdditionalParamKeys.KeySubstrateId, out string substrateId))
                            {
                                result = EN_MESSAGE_RESULT.NG;
                                messageContentToSend[ResultKeys.KeyResult] = result.ToString();
                            }

                            if (false == additionalParams.TryGetValue(AdditionalParamKeys.KeyNameOfEq, out string nameOfEq))
                            {
                                result = EN_MESSAGE_RESULT.NG;
                                messageContentToSend[ResultKeys.KeyResult] = result.ToString();
                            }

                            messageContentToSend[RequestDownloadMapFileKeys.KeySubstrateName] = resultSubstrateId;
                            messageContentToSend[RequestDownloadMapFileKeys.KeyCountRow] = resultCountRow;
                            messageContentToSend[RequestDownloadMapFileKeys.KeyCountCol] = resultCountCol;
                            messageContentToSend[RequestDownloadMapFileKeys.KeyWaferAngle] = resultAngle;
                            messageContentToSend[RequestDownloadMapFileKeys.KeyChipQty] = resultQty;
                            messageContentToSend[RequestDownloadMapFileKeys.KeyMapData] = resultMapData;

                            if (_substrateManager.GetSubstrateByName(substrateId, ref substrate) ||
                                _substrateManager.GetSubstrateByName(resultOfScenario[RequestDownloadMapFileKeys.KeyResultSubstrateId], ref substrate))
                            {
                                SetSubstrateAttributes(substrate,
                                    resultSubstrateId,
                                    resultAngle,
                                    resultCountRow,
                                    resultCountCol,
                                    resultQty,
                                    resultRefX,
                                    resultRefY,
                                    resultStartX,
                                    resultStartY,
                                    resultMapData);

                                _funcToSendClientMessage(nameOfEq, MessagesToSend.ResponseDownloadMapFile.ToString(),
                                         string.Empty, string.Empty,
                                         messageContentToSend.Keys.ToArray(), messageContentToSend.Values.ToArray(),
                                         result, true);
                            }
                            else
                            {
                                if (false == UseCoreMapHandlingOnly)
                                {
                                    _funcToSendClientMessage(nameOfEq, MessagesToSend.ResponseDownloadMapFile.ToString(),
                                        string.Empty, string.Empty,
                                        messageContentToSend.Keys.ToArray(), messageContentToSend.Values.ToArray(),
                                        EN_MESSAGE_RESULT.NG, true);

                                }
                                else
                                {
                                    _funcToSendClientMessage(nameOfEq, MessagesToSend.ResponseDownloadMapFile.ToString(),
                                            string.Empty, string.Empty,
                                            messageContentToSend.Keys.ToArray(), messageContentToSend.Values.ToArray(),
                                            result, true);
                                }
                            }

                            #region
                            if (false == additionalParams.TryGetValue(AdditionalParamKeys.KeyRingId, out string ringId))
                                return;

                            if (false == additionalParams.TryGetValue(AdditionalParamKeys.KeyUserId, out string userId))
                                return;

                            // Work_start 이후 발생하도록 수정 필요 -> ResponseDownloadMapFile 후 WaferSplitEvent 발생하도록 수정 필요
                            int portId = substrate.GetSourcePortId();
                            if (false == _carrierServer.HasCarrier(portId))
                                return;

                            string isLastString = substrate.GetAttribute(PWA500WSubstrateAttributes.IsLastSubstrate);
                            bool.TryParse(isLastString, out bool isLast);
                            //bool isLast = _substrateManager.IsLastSubstrateAtLoadPort(portId, substrateId);
                            ExecuteScenarioToSplitWafer(nameOfEq, substrate.GetName(), ringId, userId, isLast);
                            #endregion
                        }

                        #endregion
                    }
                    break;
                case ScenarioListTypes.SCENARIO_WORK_END:
                    {
                        if (isManual)
                            return;

                        #region
                        Dictionary<string, string> messageContentToSend = new Dictionary<string, string>();
                        messageContentToSend[ResultKeys.KeyResult] = result.ToString();
                        if (result.Equals(EN_MESSAGE_RESULT.OK))
                        {
                            messageContentToSend[ResultKeys.KeyDescription] = string.Empty;
                        }
                        else
                        {
                            messageContentToSend[ResultKeys.KeyDescription] = "Gem Error";
                        }

                        if (additionalParams.TryGetValue(AdditionalParamKeys.KeyNameOfEq, out string nameOfEq))
                        {
                            _funcToSendClientMessage(nameOfEq, MessagesToSend.ResponseUploadCoreFile.ToString(),
                                string.Empty, string.Empty,
                                messageContentToSend.Keys.ToArray(), messageContentToSend.Values.ToArray(),
                                result, true);
                        }
                        if (result.Equals(EN_MESSAGE_RESULT.NG))
                            return;
                        #endregion

                        // 2024.08.18 : [START] 코어맵 핸들링만 사용하는 경우 이후 시나리오를 무시한다.
                        if (UseCoreMapHandlingOnly)
                            return;
                        // [END]

                        #region
                        // Track Out
                        if (false == additionalParams.TryGetValue(AdditionalParamKeys.KeySubstrateId, out string substrateId))
                            return;
                        if (false == additionalParams.TryGetValue(AdditionalParamKeys.KeyChipQty, out string qty))
                            return;
                        if (false == int.TryParse(qty, out int chipQty))
                            return;
                        if (false == additionalParams.TryGetValue(AdditionalParamKeys.KeyUserId, out string userId))
                            return;

                        #region

                        // Process End
                        Substrate substrate = new Substrate("");
                        if (FindSubstrateByNameOrRingId(substrateId, substrateId, ref substrate))
                        {
                            int portId = substrate.GetSourcePortId();
                            string isLastString = substrate.GetAttribute(PWA500WSubstrateAttributes.IsLastSubstrate);
                            bool.TryParse(isLastString, out bool isLast);
                            if (isLast)
                            {
                                var scenarioParam = new Dictionary<string, string>
                                {
                                    [EESKeys.KeyCarrierId] = _carrierServer.GetCarrierId(portId),
                                    [EESKeys.KeyPortId] = GetPortName(portId),
                                    [EESKeys.KeyLotId] = substrate.GetLotId(),
                                    [EESKeys.KeyPartId] = substrate.GetAttribute(PWA500WSubstrateAttributes.PartId),
                                    [EESKeys.KeyParamRecipeId] = GetRecipeId(),
                                    [EESKeys.KeyOperatorId] = "AUTO"
                                };

                                _actionToEnqueueScenarioAsync(ScenarioListTypes.SCENARIO_PROCESS_END, scenarioParam, null);
                            }
                        }
                        #endregion

                        // 2024.10.23. jhlim [MOD] 고객사 요청으로 트랙아웃 기준 변경
                        if (chipQty <= 0)
                            return;
                        //bool useFullSort = _recipe.GetValue(Define.DefineEnumProject.Task.EN_TASK_LIST.Global.ToString(),
                        //    Define.DefineEnumProject.Task.Global.PARAM_GLOBAL.UseFullSortMode.ToString(), true);
                        //if (useFullSort)
                        //    return;
                        // 2024.10.23. jhlim [END]

                        ExecuteScenarioToTrackOut(substrateId, chipQty, userId, true);
                        #endregion
                    }
                    break;

                case ScenarioListTypes.SCENARIO_REQ_CORE_WAFER_SPLIT:
                case ScenarioListTypes.SCENARIO_REQ_CORE_WAFER_SPLIT_LAST:
                    {
                        if (false == scenarioParams.TryGetValue(AssignSubstrateLotIdKeys.KeyParamWaferId, out string substrateId))
                            return;

                        Substrate substrate = new Substrate("");
                        if (false == _substrateManager.GetSubstrateByName(substrateId, ref substrate))
                            return;

                        string targetLotId;
                        if (typeOfScenario.Equals(ScenarioListTypes.SCENARIO_REQ_CORE_WAFER_SPLIT))
                        {
                            if (false == resultOfScenario.TryGetValue(AssignSubstrateLotIdKeys.KeyResultLotId, out targetLotId))
                                return;
                        }
                        else
                        {
                            int portId = substrate.GetSourcePortId();
                            if (false == _carrierServer.HasCarrier(portId))
                                return;

                            targetLotId = _carrierServer.GetCarrierLotId(portId);
                        }
                        substrate.SetLotId(targetLotId);

                        if (false == isManual && additionalParams != null)
                        {
                            if (additionalParams.TryGetValue(AdditionalParamKeys.KeyNameOfEq, out string nameOfEq))
                            {
                                var messageContentToSend = new Dictionary<string, string>();
                                messageContentToSend[AssignSubstrateLotIdKeys.KeySubstrateName] = substrateId;
                                messageContentToSend[AssignSubstrateLotIdKeys.KeyLotId] = targetLotId;

                                _funcToSendClientMessage(nameOfEq, MessagesToSend.RequestAssignLotId.ToString(),
                                    string.Empty, string.Empty,
                                    messageContentToSend.Keys.ToArray(), messageContentToSend.Values.ToArray(),
                                    result, true);
                            }
                        }
                        // 2024.09.29. jhlim : 이거 필요 없다.
                        // 2024.09.25. jhlim 여기서 터짐
                        //substrate.SetAttribute(PWA500BINSubstrateAttributes.ChipQty, resultOfScenario[AssignSubstrateLotIdKeys.KeyResultQty]);                        
                    }
                    break;

                case ScenarioListTypes.SCENARIO_CORE_WAFER_DETACH_START:
                case ScenarioListTypes.SCENARIO_CORE_WAFER_DETACH_END:
                case ScenarioListTypes.SCENARIO_BIN_SORTING_START_1:
                case ScenarioListTypes.SCENARIO_BIN_SORTING_START_2:
                case ScenarioListTypes.SCENARIO_BIN_SORTING_START_3:
                    {
                        #region
                        if (false == additionalParams.TryGetValue(AdditionalParamKeys.KeyNameOfEq, out string nameOfEq))
                            return;

                        if (false == additionalParams.TryGetValue(AdditionalParamKeys.KeyMessageNameToSend, out string messageNameToSend))
                            return;

                        ExecuteToSendSimpleResultToClient(result, messageNameToSend, nameOfEq);
                        #endregion
                    }
                    break;

                case ScenarioListTypes.SCENARIO_REQ_CORE_CHIP_SPLIT_FIRST:
                case ScenarioListTypes.SCENARIO_REQ_CORE_CHIP_SPLIT:
                case ScenarioListTypes.SCENARIO_REQ_CORE_CHIP_FULL_SPLIT_FIRST:
                case ScenarioListTypes.SCENARIO_REQ_CORE_CHIP_FULL_SPLIT:
                    {
                        if (false == isManual)
                        {
                            #region
                            // 스플릿 이벤트 전송 후 리스폰스 전송
                            Dictionary<string, string> messageContentToSend = new Dictionary<string, string>();
                            messageContentToSend[ResultKeys.KeyResult] = result.ToString();
                            if (result.Equals(EN_MESSAGE_RESULT.OK))
                            {
                                messageContentToSend[ResultKeys.KeyDescription] = string.Empty;
                            }
                            else
                            {
                                messageContentToSend[ResultKeys.KeyDescription] = "Gem Error";
                            }

                            if (additionalParams.TryGetValue(AdditionalParamKeys.KeyNameOfEq, out string nameOfEq))
                            {
                                ExecuteToSendSimpleResultToClient(result, MessagesToSend.ResponseSplitCoreChip.ToString(), nameOfEq);
                            }
                            else
                                return;

                            if (result.Equals(EN_MESSAGE_RESULT.NG))
                                return;
                            #endregion
                        }

                        #region
                        // LotId 할당된 것을 설정
                        if (false == resultOfScenario.TryGetValue(SplitCoreChipKeys.KeyResultLotId, out string lotId))
                        {
                            return;
                        }
                        if (false == scenarioParams.TryGetValue(SplitCoreChipKeys.KeyParamSplitWaferId, out string coreSubstrateId))
                        {
                            return;
                        }
                        if (false == scenarioParams.TryGetValue(SplitCoreChipKeys.KeyParamRingFrameId, out string substrateId))
                        {
                            return;
                        }
                        if (false == scenarioParams.TryGetValue(SplitCoreChipKeys.KeyParamBinType, out string binType))
                        {
                            return;
                        }
                        Substrate binSubstrate = new Substrate("");
                        if (_substrateManager.GetSubstrateByName(substrateId, ref binSubstrate))
                        {
                            string chipQtyToIncreaseByString;
                            if (false == scenarioParams.TryGetValue(SplitCoreChipKeys.KeyParamSplitChipQty, out chipQtyToIncreaseByString))
                                chipQtyToIncreaseByString = "0";

                            if (typeOfScenario.Equals(ScenarioListTypes.SCENARIO_REQ_CORE_CHIP_SPLIT_FIRST) ||
                                typeOfScenario.Equals(ScenarioListTypes.SCENARIO_REQ_CORE_CHIP_FULL_SPLIT_FIRST))
                            {
                                binSubstrate.SetLotId(lotId);
                                binSubstrate.SetAttribute(PWA500WSubstrateAttributes.ChipQty, chipQtyToIncreaseByString);
                            }
                            else
                            {
                                binSubstrate.SetAttribute(PWA500WSubstrateAttributes.SplittedLotId, lotId);

                                // 기존 값을 읽어와 받은 데이터를 더한다.
                                //string qtyByString = binSubstrate.GetAttribute(PWA500BINSubstrateAttributes.ChipQty);
                                //if (false == int.TryParse(qtyByString, out int chipQty))
                                //    chipQty = 0;
                                //if (false == int.TryParse(chipQtyToIncreaseByString, out int chipQtyToIncrease))
                                //    chipQtyToIncrease = 0;

                                //int totalQty = chipQty + chipQtyToIncrease;
                                //binSubstrate.SetAttribute(PWA500BINSubstrateAttributes.ChipQty, totalQty.ToString());

                                string lotIdForParent = binSubstrate.GetLotId();
                                // 토탈이 아닌 증가되는 양만 머지한다. 여기서 수량이 계속 증가되는듯..
                                ExecuteScenarioToChipMerge(lotIdForParent, lotId, coreSubstrateId, substrateId, binType, chipQtyToIncreaseByString/*totalQty.ToString()*/);
                            }
                        }
                        #endregion
                    }
                    break;

                case ScenarioListTypes.SCENARIO_BIN_WAFER_ID_READ:
                    {
                        #region
                        if (false == isManual)
                        {
                            if (false == additionalParams.TryGetValue(AdditionalParamKeys.KeyNameOfEq, out string nameOfEq))
                                return;

                            if (false == additionalParams.TryGetValue(AdditionalParamKeys.KeyMessageNameToSend, out string messageNameToSend))
                                return;

                            ExecuteToSendSimpleResultToClient(result, messageNameToSend, nameOfEq);
                        }
                        #endregion
                    }
                    break;
                case ScenarioListTypes.SCENARIO_BIN_WORK_END:
                    {
                        #region
                        // TODO : 자동 발생하던 부분을 Bin Work End가 발생하는 Robot 쪽에서 발생시키도록 변경 -> Fail 날 경우 에러 발생시키기 위함
                        // Track Out
                        //if (false == additionalParams.TryGetValue(AdditionalParamKeys.KeySubstrateId, out string substrateId))
                        //    return;
                        //if (false == additionalParams.TryGetValue(AdditionalParamKeys.KeyChipQty, out string qty))
                        //    return;
                        //if (false == int.TryParse(qty, out int chipQty))
                        //    return;

                        //ExecuteScenarioToTrackOut(substrateId, chipQty, "AUTO", false);
                        #endregion
                    }
                    break;
                case ScenarioListTypes.SCENARIO_REQ_BIN_WAFER_ID_ASSIGN:
                    {
                        // Robot에서 발생시키도록 시나리오 변경됨
                        if (false == isManual)
                            return;

                        #region
                        if (false == scenarioParams.TryGetValue(AssignSubstrateIdKeys.KeyParamRingFrameId, out string ringId))
                            return;

                        Substrate binSubstrate = new Substrate("");
                        if (false == _substrateManager.GetSubstrateByName(ringId, ref binSubstrate))
                            return;

                        if (false == resultOfScenario.TryGetValue(AssignSubstrateIdKeys.KeyResultSubstrateId, out string newSubstrateId))
                        {
                            return;
                        }
                        binSubstrate.SetName(newSubstrateId);
                        #endregion
                    }
                    break;
                case ScenarioListTypes.SCENARIO_BIN_SORTING_END_1:
                case ScenarioListTypes.SCENARIO_BIN_SORTING_END_2:
                case ScenarioListTypes.SCENARIO_BIN_SORTING_END_3:
                    {
                        #region
                        if (false == additionalParams.TryGetValue(AdditionalParamKeys.KeyRingId, out string ringId))
                        {
                            result = EN_MESSAGE_RESULT.NG;
                        }

                        if (false == additionalParams.TryGetValue(AdditionalParamKeys.KeySubstrateType, out string subType))
                        {
                            result = EN_MESSAGE_RESULT.NG;
                        }
                        if (false == Enum.TryParse(subType, out SubstrateType substrateType))
                        {
                            result = EN_MESSAGE_RESULT.NG;
                        }

                        if (false == additionalParams.TryGetValue(AdditionalParamKeys.KeyNameOfEq, out string nameOfEq))
                        {
                            result = EN_MESSAGE_RESULT.NG;
                        }

                        int chipQty = 0;
                        if (false == additionalParams.TryGetValue(AdditionalParamKeys.KeyChipQty, out string qty) ||
                            false == int.TryParse(qty, out chipQty))
                        {
                            result = EN_MESSAGE_RESULT.NG;
                        }

                        string description = string.Empty;
                        if (result == EN_MESSAGE_RESULT.NG)
                        {
                            description = "Gem Error";
                        }

                        Dictionary<string, string> messageContentToSend = new Dictionary<string, string>
                        {
                            [ResultKeys.KeyResult] = result.ToString(),
                            [ResultKeys.KeyDescription] = description,
                        };

                        _funcToSendClientMessage(nameOfEq, MessagesToSend.ResponseFinishSorting.ToString(),
                            string.Empty, string.Empty,
                            messageContentToSend.Keys.ToArray(), messageContentToSend.Values.ToArray(),
                            result, true);
                        //if (result.Equals(EN_MESSAGE_RESULT.NG))
                        //{

                        //    Dictionary<string, string> messageContentToSend = new Dictionary<string, string>
                        //    {
                        //        [ResultKeys.KeyResult] = EN_MESSAGE_RESULT.NG.ToString(),
                        //        [ResultKeys.KeyDescription] = "Gem Error",
                        //    };

                        //    SendClientToClientMessage(nameOfEq, MessagesToSend.ResponseFinishSorting.ToString(),
                        //        string.Empty, string.Empty,
                        //        messageContentToSend.Keys.ToArray(), messageContentToSend.Values.ToArray(),
                        //        EN_MESSAGE_RESULT.NG, true);
                        //}
                        //else
                        //{
                        //    ExecuteScenarioToAssignSubstrateId(nameOfEq, ringId, substrateType);
                        //}
                        #endregion
                    }
                    break;

                default:
                    break;
            }
        }
        private bool ExecuteToSendSimpleResultToClient(EN_MESSAGE_RESULT result, string messageNameToSend, string nameOfEq)
        {
            if (_funcToSendClientMessage == null)
                return false;

            if (messageNameToSend == null || string.IsNullOrEmpty(messageNameToSend))
                return true;

            Dictionary<string, string> messageContentToSend = new Dictionary<string, string>
            {
                [ResultKeys.KeyResult] = result.ToString(),
                [ResultKeys.KeyDescription] = string.Empty,
            };

            return _funcToSendClientMessage(nameOfEq, messageNameToSend.ToString(),
                        string.Empty, string.Empty,
                        messageContentToSend.Keys.ToArray(),
                        messageContentToSend.Values.ToArray(),
                        result, true);
        }
        private bool ExecuteScenarioToChipMerge(string lotId, string lotIdToMerge, string coreWaferId, string binRingId, string binType, string chipQty)
        {
            if (_actionToEnqueueScenarioAsync == null)
                return false;

            Dictionary<string, string> scenarioParam = new Dictionary<string, string>();
            scenarioParam[SplitCoreChipKeys.KeyParamLotId] = lotId;
            scenarioParam[SplitCoreChipKeys.KeyParamSplitLotId] = lotIdToMerge;
            scenarioParam[SplitCoreChipKeys.KeyParamSplitWaferId] = coreWaferId;
            scenarioParam[SplitCoreChipKeys.KeyParamRingFrameId] = binRingId;
            scenarioParam[SplitCoreChipKeys.KeyParamBinType] = binType;
            scenarioParam[SplitCoreChipKeys.KeyParamSplitChipQty] = chipQty;

            _actionToEnqueueScenarioAsync(ScenarioListTypes.SCENARIO_REQ_CORE_CHIP_MERGE, scenarioParam, null);

            return true;
        }
        private bool ExecuteScenarioToSplitWafer(string nameOfEq, string substrateId, string ringId, string userId, bool isLast)
        {
            if (_actionToEnqueueScenarioAsync == null)
                return false;

            Substrate substrate = new Substrate("");
            if (false == _substrateManager.GetSubstrateByName(substrateId, ref substrate))
                return false;

            var scenarioParam = new Dictionary<string, string>
            {
                [AssignSubstrateLotIdKeys.KeyParamLotId] = substrate.GetLotId(),
                [AssignSubstrateLotIdKeys.KeyParamWaferId] = substrateId,
                [AssignSubstrateLotIdKeys.KeyParamPartId] = substrate.GetAttribute(PWA500WSubstrateAttributes.PartId),
                [AssignSubstrateLotIdKeys.KeyParamRecipeId] = GetRecipeId(),
                // TODO : 슬롯을 1부터 매기도록 바꿔야하나?
                [AssignSubstrateLotIdKeys.KeyParamSlotId] = (substrate.GetSourceSlot() + 1).ToString(),
                [AssignSubstrateLotIdKeys.KeyParamOperatorId] = userId
            };

            Dictionary<string, string> additionalParams = new Dictionary<string, string>
            {
                [AdditionalParamKeys.KeyNameOfEq] = nameOfEq,
                [AdditionalParamKeys.KeySubstrateId] = substrateId,
                [AdditionalParamKeys.KeyRingId] = ringId
            };

            ScenarioListTypes scenario;
            if (false == isLast)
            {
                scenario = ScenarioListTypes.SCENARIO_REQ_CORE_WAFER_SPLIT;
            }
            else
            {
                scenario = ScenarioListTypes.SCENARIO_REQ_CORE_WAFER_SPLIT_LAST;
            }

            _actionToEnqueueScenarioAsync(scenario, scenarioParam, additionalParams);
            return true;
        }
        private bool ExecuteScenarioToTrackOut(string substrateId, int chipQty, string userId, bool isCore)
        {
            if (_actionToEnqueueScenarioAsync == null)
                return false;

            ScenarioListTypes scenario;
            if (false == isCore)
            {
                scenario = ScenarioListTypes.SCENARIO_REQ_BIN_WAFER_TRACK_OUT;
            }
            else
            {
                scenario = ScenarioListTypes.SCENARIO_REQ_CORE_WAFER_TRACK_OUT;
            }
            var scenarioParams = MakeScenarioParamToTrackOut(substrateId, userId, isCore);

            _actionToEnqueueScenarioAsync(scenario, scenarioParams, null);

            return true;
        }
        private bool FindSubstrateByNameOrRingId(string substrateName, string ringId, ref Substrate substrate)
        {
            if (_substrateManager.GetSubstrateByName(substrateName, ref substrate) ||
                _substrateManager.GetSubstrateByName(ringId, ref substrate))
                return true;

            return false;
        }
        private void SetSubstrateAttributes(Substrate substrate, string substrateId, string angle, string countRow, string countCol, string qty, string referenceX, string referenceY, string startingX, string startingY, string mapData)
        {
            substrate.SetName(substrateId);
            substrate.SetAttribute(PWA500WSubstrateAttributes.Angle, angle);
            substrate.SetAttribute(PWA500WSubstrateAttributes.CountX, countRow);
            substrate.SetAttribute(PWA500WSubstrateAttributes.CountY, countCol);
            substrate.SetAttribute(PWA500WSubstrateAttributes.ChipQty, qty);
            substrate.SetAttribute(PWA500WSubstrateAttributes.RefPositionX, referenceX);
            substrate.SetAttribute(PWA500WSubstrateAttributes.RefPositionY, referenceY);
            substrate.SetAttribute(PWA500WSubstrateAttributes.StartingPositionX, startingX);
            substrate.SetAttribute(PWA500WSubstrateAttributes.StartingPositionY, startingY);
            substrate.SetAttribute(PWA500WSubstrateAttributes.MapData, mapData);
        }
        public string GetSubstrateTypeForUILoadPortIndex(int lpIndex)
        {
            var paramName = PARAM_EQUIPMENT.LoadPortType1 + lpIndex;
            string subTypeByRecipe = Recipe.Recipe.GetInstance().GetValue(EN_RECIPE_TYPE.EQUIPMENT,
                paramName.ToString(),
                SubstrateTypeForUI.Core_8.ToString());

            return subTypeByRecipe;
        }
        public SubstrateType GetSubstrateTypeByLoadPortIndex(int lpIndex)
        {
            var paramName = PARAM_EQUIPMENT.LoadPortType1 + lpIndex;
            string subTypeByRecipe = Recipe.Recipe.GetInstance().GetValue(EN_RECIPE_TYPE.EQUIPMENT,
                paramName.ToString(),
                SubstrateTypeForUI.Core_8.ToString());

            if (false == Enum.TryParse(subTypeByRecipe, out SubstrateTypeForUI substrateType))
                return SubstrateType.Core_8;

            switch (substrateType)
            {
                case SubstrateTypeForUI.Core_8:
                    return SubstrateType.Core_8;

                case SubstrateTypeForUI.Core_12:
                    return SubstrateType.Core_12;

                case SubstrateTypeForUI.Sort_12:
                    return SubstrateType.Bin_12;

                default:
                    return SubstrateType.Core_8;
            }
        }
        public string GetRecipeId()
        {
            return _processGroup.GetRecipeId(ProcessModuleIndex);
        }
        public string GetPortName(int portId)
        {
            return string.Format("B{0}", portId);

            //return string.Format("{0}_B{1}", Work.AppConfigManager.Instance.MachineName, portId);
        }
        public string GetStepIdForBinWafer()
        {
            return _recipe.GetValue(EN_RECIPE_TYPE.EQUIPMENT, PARAM_EQUIPMENT.BinWaferStepId.ToString(), "P420");
        }
        public Dictionary<string, string> MakeParamToProcessing(int portId, Substrate substrate)
        {
            var scenarioParam = new Dictionary<string, string>();
            scenarioParam[EESKeys.KeyCarrierId] = _carrierServer.GetCarrierId(portId);
            scenarioParam[EESKeys.KeyPortId] = GetPortName(portId);
            scenarioParam[EESKeys.KeyLotId] = substrate.GetLotId();
            scenarioParam[EESKeys.KeyPartId] = substrate.GetAttribute(PWA500WSubstrateAttributes.PartId);
            scenarioParam[EESKeys.KeyParamRecipeId] = GetRecipeId();
            scenarioParam[EESKeys.KeyOperatorId] = "AUTO";

            return scenarioParam;
        }
        public Dictionary<string, string> MakeParamToEquipmentStatus()
        {
            var scenarioParams = new Dictionary<string, string>();

            int currentPort = -1;
            List<int> portIdForCore = new List<int>();
            // Core 기준으로 전송한다.
            for (int i = 0; i < _loadPortManager.Count; ++i)
            {
                if (false == _loadPortManager.IsLoadPortEnabled(i))
                    continue;

                var substrateType = GetSubstrateTypeByLoadPortIndex(i);
                switch (substrateType)
                {
                    case SubstrateType.Core_8:
                    case SubstrateType.Core_12:
                        {
                            int portId = _loadPortManager.GetLoadPortPortId(i);
                            if (_carrierServer.HasCarrier(portId))
                            {
                                portIdForCore.Add(portId);

                                var status = _carrierServer.GetCarrierAccessingStatus(portId);
                                switch (status)
                                {
                                    case CarrierAccessStates.InAccessed:
                                        {
                                            currentPort = portId;
                                        }
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

                if (currentPort > 0)
                {
                    break;
                }
            }

            string lotId = string.Empty, partId = string.Empty, stepSeq = string.Empty;
            if (currentPort < 0)
            {
                if (portIdForCore.Count > 0)
                {
                    currentPort = portIdForCore[0];
                }
            }

            if (currentPort > 0)
            {
                lotId = _carrierServer.GetCarrierLotId(currentPort);
                partId = _carrierServer.GetAttribute(currentPort, PWA500WCarrierAttributeKeys.KeyPartId);
                stepSeq = _carrierServer.GetAttribute(currentPort, PWA500WCarrierAttributeKeys.KeyStepSeq);
            }

            scenarioParams[ProcessModuleStatusChangedKeys.KeyParamLotId] = lotId;
            scenarioParams[ProcessModuleStatusChangedKeys.KeyParamPartId] = partId;
            scenarioParams[ProcessModuleStatusChangedKeys.KeyParamStepSeq] = stepSeq;

            return scenarioParams;
        }
        public Dictionary<string, string> MakeParamToOHTHandling(int portId, LoadPortLoadingMode loadingType, string lotId, ScenarioListTypes scenario)
        {
            var scenarioParams = new Dictionary<string, string>();
            string carrierType = loadingType.Equals(LoadPortLoadingMode.Cassette) ?
                OHTHandlingCarrierType.CASSETTE.ToString() :
                OHTHandlingCarrierType.MAC.ToString();

            scenarioParams[AMHSHandlingKeys.KeyParamPortId] = GetPortName(portId);
            // 2024.12.24. jhlim [MOD]
            scenarioParams[AMHSHandlingKeys.KeyParamLotId] = lotId;
            scenarioParams[AMHSHandlingKeys.KeyParamCarrierId] = _carrierServer.GetCarrierId(portId);
            // 2024.12.24. jhlim [END]
            scenarioParams[AMHSHandlingKeys.KeyParamCarrierType] = carrierType;

            if (scenario.Equals(ScenarioListTypes.SCENARIO_PORT_STATUS_LOAD_1) ||
                scenario.Equals(ScenarioListTypes.SCENARIO_PORT_STATUS_LOAD_2) ||
                scenario.Equals(ScenarioListTypes.SCENARIO_PORT_STATUS_LOAD_3) ||
                scenario.Equals(ScenarioListTypes.SCENARIO_PORT_STATUS_LOAD_4) ||
                scenario.Equals(ScenarioListTypes.SCENARIO_PORT_STATUS_LOAD_5) ||
                scenario.Equals(ScenarioListTypes.SCENARIO_PORT_STATUS_LOAD_6))
            {
                scenarioParams[AMHSHandlingKeys.KeyParamStatus] = OHTHandlingStatus.UNLOAD.ToString();
            }
            else if (scenario.Equals(ScenarioListTypes.SCENARIO_PORT_STATUS_UNLOAD_1) ||
                scenario.Equals(ScenarioListTypes.SCENARIO_PORT_STATUS_UNLOAD_2) ||
                scenario.Equals(ScenarioListTypes.SCENARIO_PORT_STATUS_UNLOAD_3) ||
                scenario.Equals(ScenarioListTypes.SCENARIO_PORT_STATUS_UNLOAD_4) ||
                scenario.Equals(ScenarioListTypes.SCENARIO_PORT_STATUS_UNLOAD_5) ||
                scenario.Equals(ScenarioListTypes.SCENARIO_PORT_STATUS_UNLOAD_6))
            {
                scenarioParams[AMHSHandlingKeys.KeyParamStatus] = OHTHandlingStatus.LOAD.ToString();
            }

            scenarioParams[AMHSHandlingKeys.KeyParamOperId] = "AUTO";

            return scenarioParams;
        }
        public Dictionary<string, string> MakeScenarioParamToRecipeDownload(Substrate substrate)
        {
            string lotId = substrate.GetLotId();
            string partId = substrate.GetAttribute(PWA500WSubstrateAttributes.PartId);
            string stepSeq = substrate.GetAttribute(PWA500WSubstrateAttributes.StepSeq);
            string recipeId = substrate.GetAttribute(PWA500WSubstrateAttributes.RecipeId);
            string lotType = substrate.GetAttribute(PWA500WSubstrateAttributes.LotType);

            var scenarioParam = new Dictionary<string, string>
            {
                [RecipeHandlingKeys.KeyParamLotId] = lotId,
                [RecipeHandlingKeys.KeyParamRecipeId] = recipeId,
                [RecipeHandlingKeys.KeyParamPartId] = partId,
                [RecipeHandlingKeys.KeyParamStepSeq] = stepSeq,
                [RecipeHandlingKeys.KeyParamLotType] = lotType,
                [RecipeHandlingKeys.KeyUseCommunicationToPM] = "True",
            };

            return scenarioParam;
        }
        public Dictionary<string, string> MakeScenarioParamToSendingAssignId(string newSubstrateId, string ringId)
        {
            Dictionary<string, string> scenarioParam = new Dictionary<string, string>
            {
                [AssignSubstrateIdKeys.KeySubstrateName] = newSubstrateId,
                [AssignSubstrateIdKeys.KeyRingId] = ringId
            };

            return scenarioParam;
        }
        public Dictionary<string, string> MakeScenarioParamToBinWorkEnd(string substrateId, string ringId, bool useEventHandling)
        {
            Substrate substrate = new Substrate("");
            Dictionary<string, string> scenarioParams = new Dictionary<string, string>();
            string userId = "AUTO";
            if (_substrateManager.GetSubstrateByName(substrateId, ref substrate) ||
                _substrateManager.GetSubstrateByName(ringId, ref substrate))
            {
                int portId = substrate.GetDestinationPortId();
                int slot = substrate.GetDestinationSlot();
                if (portId <= 0 || slot < 0)
                    return null;

                string lotId = substrate.GetLotId();
                string partId = substrate.GetAttribute(PWA500WSubstrateAttributes.PartId);
                string recipeId = GetRecipeId();

                string chipQty = substrate.GetAttribute(PWA500WSubstrateAttributes.ChipQty);
                string carrierId = _carrierServer.GetCarrierId(portId);

                scenarioParams[UploadCoreOrBinFileKeys.KeyParamCarrierId] = carrierId;
                scenarioParams[UploadCoreOrBinFileKeys.KeyParamPortId] = GetPortName(portId);
                scenarioParams[UploadCoreOrBinFileKeys.KeyParamLotId] = lotId;
                scenarioParams[UploadCoreOrBinFileKeys.KeyParamPartId] = partId;
                scenarioParams[UploadCoreOrBinFileKeys.KeyParamRecipeId] = recipeId;

                scenarioParams[UploadCoreOrBinFileKeys.KeyParamRecipeId] = recipeId;
                scenarioParams[UploadCoreOrBinFileKeys.KeyParamOperatorId] = userId;
                scenarioParams[UploadCoreOrBinFileKeys.KeyChipQty] = chipQty;
                scenarioParams[UploadCoreOrBinFileKeys.KeyUseEventHandling] = useEventHandling.ToString();

                return scenarioParams;
            }
            else
            {
                if (false == useEventHandling)
                {
                    scenarioParams[UploadCoreOrBinFileKeys.KeyParamCarrierId] = string.Empty;
                    scenarioParams[UploadCoreOrBinFileKeys.KeyParamPortId] = string.Empty;
                    scenarioParams[UploadCoreOrBinFileKeys.KeyParamLotId] = string.Empty;
                    scenarioParams[UploadCoreOrBinFileKeys.KeyParamPartId] = string.Empty;
                    scenarioParams[UploadCoreOrBinFileKeys.KeyParamRecipeId] = GetRecipeId();
                    scenarioParams[UploadCoreOrBinFileKeys.KeyParamOperatorId] = userId;
                    scenarioParams[UploadCoreOrBinFileKeys.KeyChipQty] = string.Empty;
                    scenarioParams[UploadCoreOrBinFileKeys.KeyUseEventHandling] = useEventHandling.ToString();

                    return scenarioParams;
                }
            }

            return null;
        }
        public Dictionary<string, string> MakeScenarioParamToCoreTrackIn(int portId, Substrate substrate)
        {
            if (false == _carrierServer.HasCarrier(portId))
                return null;


            string carrierId = _carrierServer.GetCarrierId(portId);
            string lotId = _carrierServer.GetCarrierLotId(portId);
            string partId = substrate.GetAttribute(PWA500WSubstrateAttributes.PartId);
            string stepSeq = substrate.GetAttribute(PWA500WSubstrateAttributes.StepSeq);
            string recipeId = GetRecipeId();
            string chipQty = substrate.GetAttribute(PWA500WSubstrateAttributes.ChipQty);

            var scenarioParam = new Dictionary<string, string>
            {
                [TrackInOrOut.KeyParamCarrierId] = carrierId,
                [TrackInOrOut.KeyParamPortId] = GetPortName(portId),
                [TrackInOrOut.KeyParamLotId] = lotId,
                [TrackInOrOut.KeyParamPartId] = partId,
                [TrackInOrOut.KeyParamStepSeq] = stepSeq,
                [TrackInOrOut.KeyParamRecipeId] = recipeId,
                [TrackInOrOut.KeyParamChipQty] = chipQty,
                [TrackInOrOut.KeyParamOperatorId] = "AUTO"
            };

            return scenarioParam;
        }
        public Dictionary<string, string> MakeScenarioParamToLotMatch(int portId, string lotId, string carrierId)
        {
            if (false == _carrierServer.HasCarrier(portId))
                return null;

            var scenarioParam = new Dictionary<string, string>
            {
                [TrackInOrOut.KeyParamLotId] = lotId,
                [TrackInOrOut.KeyParamCarrierId] = carrierId,

                // 2024.09.03. jhlim [MOD]
                // MATERIAL_TYPE : TM_TAPE
                // CHANGE_REASON : 전량 소진 후 교체-FINISH_CHAGNE, 품종교체 - PACKAGE_CHAGNE
                // 추후 품종 교체 기준이 생기면 구분이 필요할 수 있다. 현재는 HARDCODING
                [TrackInOrOut.KeyParamChangeReason] = Constants.EmptyWaferChangeReason,
                [TrackInOrOut.KeyParamMaterialType] = Constants.EmptyWaferMaterialType
            };
            // 2024.09.03. jhlim [END]

            return scenarioParam;
        }
        public Dictionary<string, string> MakeScenarioParamToTrackOut(string substrateId, string userId, bool isCore)
        {
            Substrate substrate = new Substrate("");
            if (false == _substrateManager.GetSubstrateByName(substrateId, ref substrate))
                return null;

            string lotId = substrate.GetLotId();
            string partId = substrate.GetAttribute(PWA500WSubstrateAttributes.PartId);
            string stepSeq = substrate.GetAttribute(PWA500WSubstrateAttributes.StepSeq);
            string recipeId = GetRecipeId();
            string chipQty = substrate.GetAttribute(PWA500WSubstrateAttributes.ChipQty);

            int portId;
            if (false == isCore)
            {
                portId = substrate.GetDestinationPortId();
            }
            else
            {
                portId = substrate.GetSourcePortId();
            }

            if (portId <= 0 || false == _carrierServer.HasCarrier(portId))
                return null;

            string carrierId = _carrierServer.GetCarrierId(portId);

            Dictionary<string, string> scenarioParams = new Dictionary<string, string>();
            scenarioParams[TrackInOrOut.KeyParamCarrierId] = carrierId;
            scenarioParams[TrackInOrOut.KeyParamPortId] = GetPortName(portId);
            scenarioParams[TrackInOrOut.KeyParamLotId] = lotId;
            scenarioParams[TrackInOrOut.KeyParamPartId] = partId;
            scenarioParams[TrackInOrOut.KeyParamStepSeq] = stepSeq;
            scenarioParams[TrackInOrOut.KeyParamRecipeId] = recipeId;
            scenarioParams[TrackInOrOut.KeyParamChipQty] = chipQty;

            if (false == isCore)
            {
                scenarioParams[TrackInOrOut.KeyParamBinType] = substrate.GetAttribute(PWA500WSubstrateAttributes.BinCode);
            }

            scenarioParams[TrackInOrOut.KeyParamOperatorId] = userId;

            return scenarioParams;
        }
        public Dictionary<string, string> MakeScenarioParamToRequestBinPartId(string lotId, string carrierId)
        {
            Dictionary<string, string> scenarioParams = new Dictionary<string, string>
            {
                [LotInfoKeys.KeyParamLotId] = lotId,
                [LotInfoKeys.KeyParamCarrierId] = carrierId
            };

            return scenarioParams;
        }
        public Dictionary<string, string> MakeScenarioParamToAssignSubstrateId(int portId, int slot, SubstrateType substrateType, Substrate substrate)
        {
            if (false == _carrierServer.HasCarrier(portId))
                return null;

            string lotId = substrate.GetLotId();
            string substrateId = substrate.GetName();
            string chipQty = substrate.GetAttribute(PWA500WSubstrateAttributes.ChipQty);
            string binCode = substrate.GetAttribute(PWA500WSubstrateAttributes.BinCode);

            var scenarioParam = new Dictionary<string, string>
            {
                [AssignSubstrateIdKeys.KeyParamLotId] = lotId,
                [AssignSubstrateIdKeys.KeyParamBinType] = binCode,
                [AssignSubstrateIdKeys.KeyParamRingFrameId] = substrateId,

                // TODO : 슬롯을 1부터 매기도록 바꿔야하나?
                [AssignSubstrateIdKeys.KeyParamSlotId] = (slot + 1).ToString(),
                [AssignSubstrateIdKeys.KeyChipQty] = chipQty
            };

            return scenarioParam;
        }
        public Dictionary<string, string> MakeScenarioParamToUploadBinFile(int portId, int slot, string equipId, Substrate substrate)
        {
            if (false == _carrierServer.HasCarrier(portId))
                return null;

            string substrateName = substrate.GetName();
            string ringId = substrate.GetAttribute(PWA500WSubstrateAttributes.RingId);
            string recipeId = substrate.GetRecipeId();
            string substrateType = substrate.GetAttribute(PWA500WSubstrateAttributes.SubstrateType);
            string stepId = substrate.GetAttribute(PWA500WSubstrateAttributes.StepSeq);

            // 2024.10.29. jhlim [MOD] StepSeq가 설정값과 다르면 값을 셋한다.
            string stepSeqFromParam = GetStepIdForBinWafer();
            if (stepId.Equals(stepSeqFromParam))
            {
                substrate.SetAttribute(PWA500WSubstrateAttributes.StepSeq, stepSeqFromParam);
            }

            stepId = stepSeqFromParam;
            // 2024.10.29. jhlim [END]

            string partId = substrate.GetAttribute(PWA500WSubstrateAttributes.PartId);
            string lotId = substrate.GetLotId();

            var scenarioParam = new Dictionary<string, string>
            {
                [UploadCoreOrBinFileKeys.KeySubstrateName] = substrateName,
                [UploadCoreOrBinFileKeys.KeyRingId] = ringId,
                [UploadCoreOrBinFileKeys.KeyRecipeId] = recipeId,
                [UploadCoreOrBinFileKeys.KeySubstrateType] = substrateType,
                [UploadCoreOrBinFileKeys.KeyStepId] = stepId,
                [UploadCoreOrBinFileKeys.KeyEquipId] = equipId,
                [UploadCoreOrBinFileKeys.KeyPartId] = partId,
                [UploadCoreOrBinFileKeys.KeySlot] = (slot + 1).ToString(),
                [UploadCoreOrBinFileKeys.KeyLotId] = lotId
            };

            return scenarioParam;
        }

        #endregion </Methods>
    }

    public class SharedFolderAccess
    {
        [StructLayout(LayoutKind.Sequential)]
        private class NETRESOURCE
        {
            public ResourceScope dwScope = 0;
            public ResourceType dwType = ResourceType.Disk;
            public ResourceDisplayType dwDisplayType = 0;
            public ResourceUsage dwUsage = 0;
            public string lpLocalName = null;
            public string lpRemoteName = null;
            public string lpComment = null;
            public string lpProvider = null;
        }

        [DllImport("mpr.dll")]
        private static extern int WNetGetConnection(string lpLocalName, StringBuilder lpRemoteName, ref int lpnLength);

        [DllImport("mpr.dll")]
        private static extern int WNetUseConnection(IntPtr hwndOwner, NETRESOURCE lpNetResource,
            string lpPassword, string lpUserName, int dwFlags, string lpAccessName,
            string lpBufferSize, string lpResult);

        [DllImport("mpr.dll")]
        private static extern int WNetCancelConnection2(string lpName, int dwFlags, bool fForce);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint FormatMessage(
        uint dwFlags,
        IntPtr lpSource,
        uint dwMessageId,
        uint dwLanguageId,
        System.Text.StringBuilder lpBuffer,
        uint nSize,
        IntPtr Arguments);

        private const uint FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;
        private const uint FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000001;

        private enum ResourceScope : int
        {
            Connected = 1,
            GlobalNetwork,
            Remembered,
            Recent,
            Context
        }

        private enum ResourceType : int
        {
            Any = 0,
            Disk = 1,
            Print = 2,
            Reserved = 8
        }

        private enum ResourceDisplayType : int
        {
            Generic = 0,
            Domain,
            Server,
            Share,
            File,
            Group,
            Network,
            Root,
            Shareadmin,
            Directory,
            Tree,
            Ndscontainer
        }

        private enum ResourceUsage : int
        {
            Connectable = 1,
            Container = 2
        }
        public string AccessPath { get; private set; }

        public bool ConnectToSharedFolder(string networkPath, string username, string password)
        {
            // 2024.12.21. jhlim [DEL] 당분간 공유폴더를 사용할 일이 없으니 나중에 검증하자.
            //AccessPath = networkPath;
            //NETRESOURCE netResource = new NETRESOURCE
            //{
            //    lpRemoteName = AccessPath,
            //    dwType = ResourceType.Disk
            //};

            //DisconnectFromSharedFolder();

            //int result = WNetUseConnection(IntPtr.Zero, netResource, password, username, 0, null, null, null);
            //if (result != 0)
            //{
            //    var message = GetErrorMessage(result);
            //    //message = message.Replace('\n', ' ');
            //    Console.WriteLine(message);
            //}
            //return result == 0;
            // 2024.12.21. jhlim [END]

            return true;
        }

        public bool DisconnectFromSharedFolder()
        {
            // 2024.12.21. jhlim [DEL] 당분간 공유폴더를 사용할 일이 없으니 나중에 검증하자.
            ////if (AccessPath == null || string.IsNullOrEmpty(AccessPath))
            ////    return true;

            ////System.Diagnostics.Process process = new System.Diagnostics.Process();
            ////System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo
            ////{
            ////    FileName = "cmd.exe",
            ////    Arguments = $"/c net use /delete {AccessPath}",
            ////    UseShellExecute = true,
            ////    Verb = "runas" // 관리자 권한으로 실행
            ////};
            ////process.StartInfo = startInfo;
            ////process.Start();
            ////return true;

            //int result = WNetCancelConnection2(AccessPath, 0, true);
            //if (result != 0)
            //{
            //    var message = GetErrorMessage(result);
            //    //message = message.Replace('\n', ' ');
            //    Console.WriteLine(message);
            //}
            //return result == 0;
            // 2024.12.21. jhlim [END]

            return true;
        }

        private string GetErrorMessage(int errorCode)
        {
            StringBuilder messageBuffer = new StringBuilder(512);
            uint result = FormatMessage(
                FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
                IntPtr.Zero,
                (uint)errorCode,
                0,
                messageBuffer,
                (uint)messageBuffer.Capacity,
                IntPtr.Zero);

            if (result > 0)
            {
                // 문자열로 변환하고 줄 바꿈 문자를 제거
                string message = messageBuffer.ToString().Trim();
                return message.Replace("\r\n", " "); // 또는 "\n"을 사용하여 필요에 따라 줄 바꿈을 제거
            }
            else
            {
                return $"Unknown error (Code: {errorCode})";
            }
        }
    }
}

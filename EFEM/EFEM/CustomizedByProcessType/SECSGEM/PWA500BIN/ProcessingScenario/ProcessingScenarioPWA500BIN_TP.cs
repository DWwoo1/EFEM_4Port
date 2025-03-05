using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

using FrameOfSystem3.Recipe;
using FrameOfSystem3.SECSGEM.DefineSecsGem;
using FrameOfSystem3.SECSGEM.Scenario.Common;
using FrameOfSystem3.Functional;
using Define.DefineEnumProject.AnalogIO.PWA500BIN;

using AnalogIO_;
using WCFManager_;

using Define.DefineEnumProject.Mail;
using Define.DefineConstant;

using EFEM.Modules;
using EFEM.MaterialTracking;
using EFEM.CustomizedByProcessType.PWA500BIN;
using FrameOfSystem3.ExternalDevice.Serial.FanFilterUnit;

namespace FrameOfSystem3.SECSGEM.Scenario
{
    public class ProcessingScenarioPWA500BIN_TP : ProcessingScenario
    {
        public ProcessingScenarioPWA500BIN_TP()
        {
            _postOffice = PostOffice.GetInstance();
            _postOffice.RequestSubscribe(EN_SUBSCRIBER.ProcessingScenario);

            _loadPortManager = LoadPortManager.Instance;
            _robotManager = AtmRobotManager.Instance;
            _processModuleGroup = ProcessModuleGroup.Instance;
            _carrierServer = CarrierManagementServer.Instance;
            _substrateManager = SubstrateManager.Instance;

            _wcfManager = WCFManager.GetInstance();
            _analogIO = AnalogIO.GetInstance();
            _ffuManager = FanFilterUnitManager.Instance;

            //RecipePath = new Dictionary<string, string>
            //{
            //    [NameOfPM] = RecipePathForPM
            //};

            //ClientInfos = new Dictionary<string, string>();
            //int[] indexOfClients = new int[1];
            //if (_wcfManager.GetListofClientItems(ref indexOfClients))
            //{
            //    for (int i = 0; i < indexOfClients.Length; ++i)
            //    {
            //        string deviceName = string.Empty;
            //        string ipAddress = string.Empty;
            //        int indexOfItem = indexOfClients[i];
            //        if (false == _wcfManager.GetParameter(indexOfItem, ParameterTypeForClient.Name, ref deviceName))
            //            continue;

            //        if (false == _wcfManager.GetParameter(indexOfItem, ParameterTypeForClient.TargetServiceIP, ref ipAddress))
            //            continue;

            //        ClientInfos[deviceName] = ipAddress;
            //    }
            //}           

            _scenarioManager = ScenarioManagerForPWA500BIN_TP.Instance;
            _scenarioManager.AssignFunctionToSendClientMessage(SendClientToClientMessage);
            _scenarioManager.AssignActionToEnqueueScenarioAsync(EnqueueScenarioAsync);
            _scenarioManager.AssignFunctionToUpdateParam(UpdateScenarioParams);
            _scenarioManager.AssignFunctionToExecuteScenario(ExecuteScenario);

            _lotHistoryLog = LotHistoryLog.Instance;

            //string path = @"\\192.168.201.116";
            //_sharedFolderForAccess = new SharedFolderAccess();
            //_sharedFolderForAccess.DisconnectFromSharedFolder(path);
            //_sharedFolderForAccess.ConnectToSharedFolder(path, "1", "");

            //sharedFolderAccess.AccessSharedFolder(path);

            //sharedFolderAccess.DisconnectFromSharedFolder(path);

            // Command 이용하여 실행하는 구문
            //Process process = new Process();
            //ProcessStartInfo startInfo = new ProcessStartInfo
            //{
            //    FileName = "cmd.exe",
            //    Arguments = $"/c net use {path} /persistent:yes /USER:1",
            //    UseShellExecute = true,
            //    Verb = "runas" // 관리자 권한으로 실행
            //};
            //process.StartInfo = startInfo;
            //process.Start();
        }

        WCFManager _wcfManager;
        PostOffice _postOffice = null;
        private const string IsResponseMessage = "Request";
        private const string NameOfClient = "MAIN";
        private const string RecipePathForPM = @"\\192.168.100.200\Recipe\RMS";
        //private const string RecipePathForPM = @"\\127.0.0.1\bp5000ld\RMS";
        private readonly int ProcessModuleIndex = 0;
        //private readonly int RobotIndex = 0;

        private static LoadPortManager _loadPortManager;
        private static AtmRobotManager _robotManager;
        private static SubstrateManager _substrateManager;
        private static CarrierManagementServer _carrierServer;
        private static ProcessModuleGroup _processModuleGroup;
        private static AnalogIO _analogIO = null;
        private static FanFilterUnitManager _ffuManager = null;

        private static ScenarioManagerForPWA500BIN_TP _scenarioManager = null;

        //private readonly Dictionary<string, string> ClientInfos;
        private QueuedScenarioInfo _executingScenarioInfo = null;
        private readonly ConcurrentQueue<QueuedScenarioInfo> QueuedScenario = new ConcurrentQueue<QueuedScenarioInfo>();
        private const string NameOfPM = "PWA500BIN";

        // TODO : 다운로드시 체크
        //private const string _recipeBasePathToDownload = @"\\192.168.100.200\Shared\Download";
        private const string _recipeBasePathToDownload = @"\\192.168.100.150\EFEM\RMS\PWA500BIN\Download";

        private string _recipePathToUploadForPM = string.Empty;
        //private readonly Dictionary<string, string> RecipePath = null;
        private Dictionary<string, string> _traceVariables = new Dictionary<string, string>();
        private ConcurrentDictionary<long, string> _variablesToUpdate = new ConcurrentDictionary<long, string>();
        //private SharedFolderAccess _sharedFolderForAccess = null;
        private const string AccessAccount = "protec";
        private const string AccessPassword = "1";
        private string _accessIpAddress = string.Empty;
        private const int AlarmOffset = 2000000;

        private readonly ConcurrentQueue<int> _queuedAlarmsFromPM = new ConcurrentQueue<int>();
        private Dictionary<long, int> _analogInfo = null;
        private Dictionary<long, string> _traceDataForEFEM = null;
        private ConcurrentDictionary<string, string> _traceDataAtDetaching = null;
        private ConcurrentDictionary<string, string> _traceDataForPWA500BIN = null;   // Key : Vid(VidName), Value : 값
        private Dictionary<string, long> _traceDataForPWA500BINByName = null; // Key : VidName, Value : Vid(String)
        private UnitItem _unitItem = new UnitItem();
        
        private const uint TraceDataInterval = 2000;
        private readonly TickCounter_.TickCounter TicksForTraceData = new TickCounter_.TickCounter();
        // TODO : 나중에 올려야함
        Dictionary<string, string> _ecidToUpdate = new Dictionary<string, string>();
        private static LotHistoryLog _lotHistoryLog = null; 

        #region interface

        #region <Init, Exit>
        public override bool Init(string recipePath, string configPath, Dictionary<string, StatusVariable> statusVariableList, Dictionary<long, List<StatusVariable>> reportList, Dictionary<string, CollectionEvent> collectionEventList)
        {
            var result = base.Init(recipePath, configPath, statusVariableList, reportList, collectionEventList);

            int clientIndex = 4;
            if (Work.AppConfigManager.Instance.ProcessModuleSimulation)
            {
                clientIndex = 9;
            }

            int[] indexOfClients = new int[1];
            if (_wcfManager.GetListofClientItems(ref indexOfClients))
            {
                for (int i = 0; i < indexOfClients.Length; ++i)
                {
                    if (false == i.Equals(clientIndex))
                        continue;

                    string deviceName = string.Empty;
                    int indexOfItem = indexOfClients[i];
                    if (false == _wcfManager.GetParameter(indexOfItem, ParameterTypeForClient.Name, ref deviceName))
                        continue;

                    string accessIp = string.Empty;
                    if (false == _wcfManager.GetParameter(indexOfItem, ParameterTypeForClient.TargetServiceIP, ref accessIp))
                        continue;

                    _accessIpAddress = string.Format(@"\\{0}", accessIp);
                }
            }

            //if (false == string.IsNullOrEmpty(_accessIpAddress))
            //{
            //    if (_sharedFolderForAccess == null)
            //    {
            //        _sharedFolderForAccess = new SharedFolderAccess();
            //    }
            //    else
            //    {
            //        _sharedFolderForAccess.DisconnectFromSharedFolder();
            //    }

            //    System.Threading.Tasks.Task.Run(() => _sharedFolderForAccess.ConnectToSharedFolder(_accessIpAddress, AccessAccount, AccessPassword));

            //    // Win10에서 공용 -> 개인네트워크 설정방법
            //    // Windows 로고키 +R키를 누르고 실행창이 나타나면 gpedit.msc 입력 후 확인 버튼을 클릭합니다.
            //    // 로컬 그룹 정책 편집기가 실행되면[컴퓨터 구성 - Windows 설정 - 보안 설정 - 네트워크 목록 관리자 정책]을 클릭한 후 오른쪽에서 네트워크를 더블클릭합니다.
            //    // 속성 창이 나타나면 네트워크 위치 탭으로 이동한 후 위치 유형에서 개인 / 공용을 선택하고 적용 버튼을 클릭합니다.
            //}

            return result;
        }
        public override void Exit()
        {
            // 2025.01.06. jhlim [ADD] 마지막 데이터를 리커버리를 위해 저장한다. 
            #region <Write Initial Variable Values to file>
            Dictionary<long, string> valuesFromFile = new Dictionary<long, string>(_variablesToUpdate);
            _scenarioManager.WriteVariableValuesToFile(valuesFromFile);
            #endregion <'Write Initial Variable Values to file>
            // 2025.01.06. jhlim [END] 

            //System.Threading.Tasks.Task.Run(() => _sharedFolderForAccess.DisconnectFromSharedFolder());
            base.Exit();
        }
        #endregion </Init, Exit>

        #region <Properties>
        private bool UseCoreMapHandlingOnly
        {
            get
            {
                return false;// (false == _recipe.GetValue(EN_RECIPE_TYPE.COMMON, PARAM_COMMON.UseSecsGem.ToString(), false));
            }
        }
        #endregion </Properties>

        #region config
        public override Enum ConvertScenarioByName(string scenarioName)
        {
            if (false == Enum.TryParse(scenarioName, out ScenarioListTypes convertedScenario))
                return null;

            return convertedScenario;
        }

        protected override void MakeCustomScenario()
        {
            foreach (ScenarioListTypes scenario in Enum.GetValues(typeof(ScenarioListTypes)))
            {
                if (ScenarioList.ContainsKey(scenario))
                    continue;

                switch (scenario)
                {
                    case ScenarioListTypes.SCENARIO_REQ_LOT_INFO_CORE_1:
                        {
                            MakeScenario(scenario, new ScenarioReqLotInfo(
                                ObjectNames.LOTINFO.ToString(),
                                CollectionEventList[EN_EVENT_LIST.REQ_LOT_INFO_CORE_1.ToString()].Id,
                                CollectionEventList[EN_EVENT_LIST.REQ_LOT_INFO_CORE_1.ToString()].VariableIds,
                                14, 3, true
                                ));
                        }
                        break;
                    case ScenarioListTypes.SCENARIO_REQ_LOT_INFO_CORE_2:
                        {
                            MakeScenario(scenario, new ScenarioReqLotInfo(
                                ObjectNames.LOTINFO.ToString(),
                                CollectionEventList[EN_EVENT_LIST.REQ_LOT_INFO_CORE_2.ToString()].Id,
                                CollectionEventList[EN_EVENT_LIST.REQ_LOT_INFO_CORE_2.ToString()].VariableIds,
                                14, 3, true
                                ));
                        }
                        break;
                    case ScenarioListTypes.SCENARIO_REQ_LOT_INFO_EMPTY_TAPE:
                        {
                            MakeScenario(scenario, new ScenarioReqLotInfo(
                                ObjectNames.LOTINFO.ToString(),
                                CollectionEventList[EN_EVENT_LIST.REQ_LOT_INFO_EMPTY_TAPE.ToString()].Id,
                                CollectionEventList[EN_EVENT_LIST.REQ_LOT_INFO_EMPTY_TAPE.ToString()].VariableIds,
                                14, 3, true
                                ));
                        }
                        break;
                    case ScenarioListTypes.SCENARIO_BIN_PART_ID_INFO_REQ:
                        {
                            MakeScenario(scenario, new ScenarioReqPartIdInfo(
                                ObjectNames.BIN_PART_ID_INFO.ToString(),
                                CollectionEventList[EN_EVENT_LIST.BIN_PART_ID_INFO_REQ.ToString()].Id,
                                CollectionEventList[EN_EVENT_LIST.BIN_PART_ID_INFO_REQ.ToString()].VariableIds,
                                14, 3, true
                                ));
                        }
                        break;

                    case ScenarioListTypes.SCENARIO_REQ_LOT_MERGE_CORE_1:
                        {
                            MakeScenario(scenario, new ScenarioReqLotMergeAndChange(
                                ObjectNames.LOT_MERGE.ToString(),
                                new Dictionary<string, ObjectNames>() { [ObjectNames.LOT_MERGE.ToString()] = ObjectNames.LOT_MERGE },
                                CollectionEventList[EN_EVENT_LIST.REQ_LOT_MERGE_CORE_1.ToString()].Id,
                                CollectionEventList[EN_EVENT_LIST.REQ_LOT_MERGE_CORE_1.ToString()].VariableIds,
                                -1,
                                null));
                        }
                        break;
                    case ScenarioListTypes.SCENARIO_REQ_LOT_MERGE_CORE_2:
                        {
                            MakeScenario(scenario, new ScenarioReqLotMergeAndChange(
                                ObjectNames.LOT_MERGE.ToString(),
                                new Dictionary<string, ObjectNames>() { [ObjectNames.LOT_MERGE.ToString()] = ObjectNames.LOT_MERGE },
                                CollectionEventList[EN_EVENT_LIST.REQ_LOT_MERGE_CORE_2.ToString()].Id,
                                CollectionEventList[EN_EVENT_LIST.REQ_LOT_MERGE_CORE_2.ToString()].VariableIds,
                                -1,
                                null));
                        }
                        break;
                    case ScenarioListTypes.SCENARIO_REQ_LOT_ID_MERGE_AND_CHANGE_BIN_1:
                        {
                            MakeScenario(scenario, new ScenarioReqLotMergeAndChange(
                                ObjectNames.LOT_MERGE.ToString(),
                                new Dictionary<string, ObjectNames>()
                                {
                                    [ObjectNames.LOT_MERGE.ToString()] = ObjectNames.LOT_MERGE,
                                    [ObjectNames.LOT_ID_CHANGE.ToString()] = ObjectNames.LOT_ID_CHANGE
                                },
                                CollectionEventList[EN_EVENT_LIST.REQ_LOT_MERGE_BIN_1.ToString()].Id,
                                CollectionEventList[EN_EVENT_LIST.REQ_LOT_MERGE_BIN_1.ToString()].VariableIds,
                                CollectionEventList[EN_EVENT_LIST.REQ_LOT_ID_CHANGE_BIN_1.ToString()].Id,
                                CollectionEventList[EN_EVENT_LIST.REQ_LOT_ID_CHANGE_BIN_1.ToString()].VariableIds));
                        }
                        break;

                    case ScenarioListTypes.SCENARIO_REQ_LOT_ID_MERGE_AND_CHANGE_BIN_2:
                        {
                            MakeScenario(scenario, new ScenarioReqLotMergeAndChange(
                                ObjectNames.LOT_MERGE.ToString(),
                                new Dictionary<string, ObjectNames>()
                                {
                                    [ObjectNames.LOT_MERGE.ToString()] = ObjectNames.LOT_MERGE,
                                    [ObjectNames.LOT_ID_CHANGE.ToString()] = ObjectNames.LOT_ID_CHANGE
                                },
                                CollectionEventList[EN_EVENT_LIST.REQ_LOT_MERGE_BIN_2.ToString()].Id,
                                CollectionEventList[EN_EVENT_LIST.REQ_LOT_MERGE_BIN_2.ToString()].VariableIds,
                                CollectionEventList[EN_EVENT_LIST.REQ_LOT_ID_CHANGE_BIN_2.ToString()].Id,
                                CollectionEventList[EN_EVENT_LIST.REQ_LOT_ID_CHANGE_BIN_2.ToString()].VariableIds));
                        }
                        break;

                    case ScenarioListTypes.SCENARIO_REQ_LOT_ID_MERGE_AND_CHANGE_BIN_3:
                        {
                            MakeScenario(scenario, new ScenarioReqLotMergeAndChange(
                                ObjectNames.LOT_MERGE.ToString(),
                                new Dictionary<string, ObjectNames>()
                                {
                                    [ObjectNames.LOT_MERGE.ToString()] = ObjectNames.LOT_MERGE,
                                    [ObjectNames.LOT_ID_CHANGE.ToString()] = ObjectNames.LOT_ID_CHANGE
                                },
                                CollectionEventList[EN_EVENT_LIST.REQ_LOT_MERGE_BIN_3.ToString()].Id,
                                CollectionEventList[EN_EVENT_LIST.REQ_LOT_MERGE_BIN_3.ToString()].VariableIds,
                                CollectionEventList[EN_EVENT_LIST.REQ_LOT_ID_CHANGE_BIN_3.ToString()].Id,
                                CollectionEventList[EN_EVENT_LIST.REQ_LOT_ID_CHANGE_BIN_3.ToString()].VariableIds));
                        }
                        break;

                    case ScenarioListTypes.SCENARIO_REQ_CORE_WAFER_SPLIT:
                        {
                            MakeScenario(scenario, new ScenarioReqWaferSplitFromLot(
                                ObjectNames.CHANGELOTINFO.ToString(),
                                CollectionEventList[EN_EVENT_LIST.REQ_CORE_WAFER_SPLIT.ToString()].Id,
                                CollectionEventList[EN_EVENT_LIST.REQ_CORE_WAFER_SPLIT.ToString()].VariableIds,
                                14, 3, true
                                ));
                        }
                        break;
                    case ScenarioListTypes.SCENARIO_REQ_CORE_CHIP_SPLIT_FIRST:
                        {
                            MakeScenario(scenario, new ScenarioReqChipSplit(
                                ObjectNames.ASSIGN_WAFER_LOT_ID.ToString(),
                                CollectionEventList[EN_EVENT_LIST.REQ_CORE_CHIP_SPLIT_FIRST.ToString()].Id,
                                CollectionEventList[EN_EVENT_LIST.REQ_CORE_CHIP_SPLIT_FIRST.ToString()].VariableIds,
                                14, 3, AttributeNames.LOTID.ToString(),
                                true
                                ));
                        }
                        break;
                    case ScenarioListTypes.SCENARIO_REQ_CORE_CHIP_SPLIT:
                        {
                            MakeScenario(scenario, new ScenarioReqChipSplit(
                                ObjectNames.ASSIGN_SPLIT_LOT_ID.ToString(),
                                CollectionEventList[EN_EVENT_LIST.REQ_CORE_CHIP_SPLIT.ToString()].Id,
                                CollectionEventList[EN_EVENT_LIST.REQ_CORE_CHIP_SPLIT.ToString()].VariableIds,
                                14, 3, AttributeNames.SPLIT_LOTID.ToString(),
                                true
                                ));
                        }
                        break;
                    case ScenarioListTypes.SCENARIO_REQ_CORE_CHIP_FULL_SPLIT_FIRST:
                        {
                            MakeScenario(scenario, new ScenarioReqChipSplit(
                                ObjectNames.ASSIGN_WAFER_LOT_ID.ToString(),
                                CollectionEventList[EN_EVENT_LIST.REQ_CORE_CHIP_FULL_SPLIT_FIRST.ToString()].Id,
                                CollectionEventList[EN_EVENT_LIST.REQ_CORE_CHIP_FULL_SPLIT_FIRST.ToString()].VariableIds,
                                14, 3, AttributeNames.LOTID.ToString(),
                                true
                                ));
                        }
                        break;
                    case ScenarioListTypes.SCENARIO_REQ_CORE_CHIP_FULL_SPLIT:
                        {
                            MakeScenario(scenario, new ScenarioReqChipSplit(
                                ObjectNames.ASSIGN_SPLIT_LOT_ID.ToString(),
                                CollectionEventList[EN_EVENT_LIST.REQ_CORE_CHIP_FULL_SPLIT.ToString()].Id,
                                CollectionEventList[EN_EVENT_LIST.REQ_CORE_CHIP_FULL_SPLIT.ToString()].VariableIds,
                                14, 3, AttributeNames.SPLIT_LOTID.ToString(),
                                true
                                ));
                        }
                        break;
                    case ScenarioListTypes.SCENARIO_REQ_BIN_WAFER_ID_ASSIGN:
                        {
                            MakeScenario(scenario, new ScenarioReqWaferIdAssign(
                                ObjectNames.ASSIGN_WAFER_ID.ToString(),
                                CollectionEventList[EN_EVENT_LIST.REQ_BIN_WAFER_ID_ASSIGN.ToString()].Id,
                                CollectionEventList[EN_EVENT_LIST.REQ_BIN_WAFER_ID_ASSIGN.ToString()].VariableIds,
                                CollectionEventList[EN_EVENT_LIST.REQ_BIN_WAFER_ID_CONFIRM.ToString()].Id,
                                CollectionEventList[EN_EVENT_LIST.REQ_BIN_WAFER_ID_CONFIRM.ToString()].VariableIds,
                                14, 3, true
                                ));
                        }
                        break;

                    case ScenarioListTypes.SCENARIO_REQ_SLOT_INFO_CORE_1:
                        {
                            MakeScenario(scenario, new ScenarioProceedWithCarrier(
                                CollectionEventList[EN_EVENT_LIST.REQ_SLOT_INFO_CORE_1.ToString()].Id,
                                CollectionEventList[EN_EVENT_LIST.REQ_SLOT_INFO_CORE_1.ToString()].VariableIds, 3, 17, true));
                        }
                        break;
                    case ScenarioListTypes.SCENARIO_REQ_SLOT_INFO_CORE_2:
                        {
                            MakeScenario(scenario, new ScenarioProceedWithCarrier(
                                CollectionEventList[EN_EVENT_LIST.REQ_SLOT_INFO_CORE_2.ToString()].Id,
                                CollectionEventList[EN_EVENT_LIST.REQ_SLOT_INFO_CORE_2.ToString()].VariableIds, 3, 17, true));
                        }
                        break;
                    case ScenarioListTypes.SCENARIO_REQ_SLOT_INFO_EMPTY_TAPE:
                        {
                            MakeScenario(scenario, new ScenarioProceedWithCarrier(
                                CollectionEventList[EN_EVENT_LIST.REQ_SLOT_INFO_EMPTY_TAPE.ToString()].Id,
                                CollectionEventList[EN_EVENT_LIST.REQ_SLOT_INFO_EMPTY_TAPE.ToString()].VariableIds, 3, 17, true));
                        }
                        break;
                    case ScenarioListTypes.SCENARIO_REQ_RECIPE_DOWNLOAD:
                        {
                            MakeScenario(scenario, new EFEM.CustomizedByProcessType.PWA500BIN.ScenarioRecipeHandlingRequest(scenario.ToString(),
                                false,
                                CollectionEventList[EN_EVENT_LIST.REQ_RECIPE_DOWNLOAD.ToString()].Id,
                                CollectionEventList[EN_EVENT_LIST.REQ_RECIPE_DOWNLOAD.ToString()].VariableIds,
                                true));
                        }
                        break;
                    case ScenarioListTypes.SCENARIO_REQ_RECIPE_UPLOAD:
                        {
                            MakeScenario(scenario, new EFEM.CustomizedByProcessType.PWA500BIN.ScenarioRecipeHandlingRequest(scenario.ToString(),
                                true,
                                CollectionEventList[EN_EVENT_LIST.REQ_RECIPE_UPLOAD.ToString()].Id,
                                CollectionEventList[EN_EVENT_LIST.REQ_RECIPE_UPLOAD.ToString()].VariableIds,
                                true));
                        }
                        break;
                    case ScenarioListTypes.SCENARIO_WORK_START:
                        {
                            MakeScenario(scenario, new ScenarioReqWorkStart(
                                scenario.ToString(),
                                CollectionEventList[EN_EVENT_LIST.WORK_START.ToString()].Id,
                                CollectionEventList[EN_EVENT_LIST.WORK_START.ToString()].VariableIds,
                                12,
                                3,
                                15));
                        }
                        break;
                    case ScenarioListTypes.SCENARIO_WORK_END:
                        {
                            // Map Upload Only
                            MakeScenario(scenario, new ScenarioReqWorkEnd(
                                scenario.ToString(),
                                CollectionEventList[EN_EVENT_LIST.WORK_END.ToString()].Id,
                                CollectionEventList[EN_EVENT_LIST.WORK_END.ToString()].VariableIds,
                                -1,
                                12,
                                1,
                                5,
                                9));
                        }
                        break;
                    case ScenarioListTypes.SCENARIO_BIN_WORK_END:
                        {
                            MakeScenario(scenario, new ScenarioReqWorkEnd(
                                scenario.ToString(),
                                CollectionEventList[EN_EVENT_LIST.BIN_WORK_END.ToString()].Id,
                                CollectionEventList[EN_EVENT_LIST.BIN_WORK_END.ToString()].VariableIds,
                                -1,
                                -1,
                                -1,
                                -1,
                                -1));
                        }
                        break;
                    case ScenarioListTypes.SCENARIO_BIN_DATA_UPLOAD:
                        {
                            // Map and PMS Upload
                            MakeScenario(scenario, new ScenarioUploadBinData(
                                scenario.ToString(),
                                CollectionEventList[EN_EVENT_LIST.BIN_DATA_UPLOAD.ToString()].Id,
                                CollectionEventList[EN_EVENT_LIST.BIN_DATA_UPLOAD.ToString()].VariableIds,
                                StatusVariableList[EN_SVID_LIST.PMS_FILEBODY.ToString()].Id,
                                12,
                                1,
                                5,
                                9));
                        }
                        break;
                    case ScenarioListTypes.SCENARIO_REQ_UPLOAD_BINFILE:
                        {
                            MakeScenario(scenario, new ScenarioReqUploadBinFile(scenario.ToString()));
                        }
                        break;
                    case ScenarioListTypes.SCENARIO_ASSIGN_SUBSTRATE_ID:
                        {
                            MakeScenario(scenario, new ClientToClientCommunicationScenario(scenario.ToString()));
                        }
                        break;

                    default:
                        break;
                }
            }
        }

        protected override bool MakeScenarioToTraceData(ref uint interval, ref List<long> traceDataVariableIds)
        {
            _variablesToUpdate[1700] = string.Empty;
            _variablesToUpdate[1701] = string.Empty;
            _variablesToUpdate[1702] = string.Empty;
            _variablesToUpdate[1703] = string.Empty;
            _variablesToUpdate[1704] = string.Empty;
            _variablesToUpdate[1705] = string.Empty;
            _variablesToUpdate[1706] = string.Empty;
            _variablesToUpdate[1707] = string.Empty;
            _variablesToUpdate[1708] = string.Empty;
            _variablesToUpdate[1709] = string.Empty;
            _variablesToUpdate[1710] = string.Empty;

            _variablesToUpdate[1750] = string.Empty;
            _variablesToUpdate[1751] = string.Empty;
            _variablesToUpdate[1752] = string.Empty;
            _variablesToUpdate[1753] = string.Empty;
            _variablesToUpdate[1754] = string.Empty;
            _variablesToUpdate[1755] = string.Empty;
            _variablesToUpdate[1756] = string.Empty;
            _variablesToUpdate[1760] = string.Empty;
            _variablesToUpdate[1761] = string.Empty;
            _variablesToUpdate[1762] = string.Empty;

            _variablesToUpdate[1800] = string.Empty;
            _variablesToUpdate[1801] = string.Empty;
            _variablesToUpdate[1802] = string.Empty;
            _variablesToUpdate[1803] = string.Empty;

            _variablesToUpdate[2000] = string.Empty;
            _variablesToUpdate[2001] = string.Empty;
            _variablesToUpdate[2010] = string.Empty;
            _variablesToUpdate[2011] = string.Empty;
            _variablesToUpdate[2012] = string.Empty;
            _variablesToUpdate[2013] = string.Empty;
            _variablesToUpdate[2014] = string.Empty;
            _variablesToUpdate[2015] = string.Empty;
            _variablesToUpdate[2020] = string.Empty;
            _variablesToUpdate[2021] = string.Empty;
            _variablesToUpdate[2022] = string.Empty;
            _variablesToUpdate[2023] = string.Empty;
            _variablesToUpdate[2024] = string.Empty;
            _variablesToUpdate[2025] = string.Empty;

            interval = TraceDataInterval;
            traceDataVariableIds = new List<long>(_variablesToUpdate.Keys.ToList());

            #region <TraceData For EFEM>
            _traceDataForEFEM = new Dictionary<long, string>
            {
                [1700/*EN_SVID_LIST.EFEM_MAIN_CDA_PRESSURE.ToString()*/] = string.Empty,
                [1701/*EN_SVID_LIST.EFEM_MAIN_VAC_PRESSURE.ToString()*/] = string.Empty,
                [1702/*EN_SVID_LIST.ROBOT_CDA_PRESSURE.ToString()*/] = string.Empty,
                [1703/*EN_SVID_LIST.IONIZER_PRESSURE.ToString()*/] = string.Empty,
                [1704/*EN_SVID_LIST.IONIZER_FLOW_METER_1.ToString()*/] = string.Empty,
                [1705/*EN_SVID_LIST.IONIZER_FLOW_METER_2.ToString()*/] = string.Empty,
                [1706/*EN_SVID_LIST.IONIZER_FLOW_METER_3.ToString()*/] = string.Empty,
                [1707/*EN_SVID_LIST.IONIZER_FLOW_METER_4.ToString()*/] = string.Empty,
                [1708/*EN_SVID_LIST.EFEM_FFU_SPEED_1.ToString()*/] = string.Empty,
                [1709/*EN_SVID_LIST.EFEM_FFU_SPEED_2.ToString()*/] = string.Empty,
                [1710/*EN_SVID_LIST.EFEM_FFU_SPEED_3.ToString()*/] = string.Empty,
            };

            _analogInfo = new Dictionary<long, int>
            {
                [1700/*EN_SVID_LIST.EFEM_MAIN_CDA_PRESSURE.ToString()*/] = (int)EN_ANALOG_IN.EFEM_MAIN_CDA_PRESSURE_SWITCH,
                [1701/*EN_SVID_LIST.EFEM_MAIN_VAC_PRESSURE.ToString()*/] = (int)EN_ANALOG_IN.EFEM_MAIN_CDA_VACUUM_SWITCH,
                [1702/*EN_SVID_LIST.ROBOT_CDA_PRESSURE.ToString()*/] = (int)EN_ANALOG_IN.ROBOT_CDA_PRESSURE_SWITCH,
                [1703/*EN_SVID_LIST.IONIZER_PRESSURE.ToString()*/] = (int)EN_ANALOG_IN.IONIZER_PRESSURE_SWITCH,
                [1704/*EN_SVID_LIST.IONIZER_FLOW_METER_1.ToString()*/] = (int)EN_ANALOG_IN.IONIZER_1,
                [1705/*EN_SVID_LIST.IONIZER_FLOW_METER_2.ToString()*/] = (int)EN_ANALOG_IN.IONIZER_2,
                [1706/*EN_SVID_LIST.IONIZER_FLOW_METER_3.ToString()*/] = (int)EN_ANALOG_IN.IONIZER_3,
                [1707/*EN_SVID_LIST.IONIZER_FLOW_METER_4.ToString()*/] = (int)EN_ANALOG_IN.IONIZER_4
            };
            #endregion </TraceData For EFEM>

            #region <TraceData For PWA500BIN>
            _traceDataForPWA500BIN = new ConcurrentDictionary<string, string>
            {
                [EN_SVID_LIST.SUPPLY_BUFFER_IONIZER_FLOW.ToString()]    = string.Empty,
                [EN_SVID_LIST.SORTING_BUFFER_IONIZER_FLOW.ToString()]   = string.Empty,
                [EN_SVID_LIST.SUPPLY_STAGE_IONIZER_FLOW.ToString()]     = string.Empty,
                [EN_SVID_LIST.SORTING_STAGE_IONIZER_FLOW.ToString()]    = string.Empty,
                [EN_SVID_LIST.PM_FFU_SPEED_1.ToString()]                = string.Empty,
                [EN_SVID_LIST.PM_FFU_SPEED_2.ToString()]                = string.Empty,
                [EN_SVID_LIST.PM_FFU_SPEED_3.ToString()]                = string.Empty,
                [EN_SVID_LIST.EJECT_MEMBRANE_AIR_REGULATOR.ToString()]  = string.Empty,
                [EN_SVID_LIST.EJECT_MEMBRANE_VAC_PRESS.ToString()]      = string.Empty,
                [EN_SVID_LIST.EJECT_VAC_PRESS.ToString()]               = string.Empty,

                [EN_SVID_LIST.ESD_SENSOR_01.ToString()]                 = string.Empty,
                [EN_SVID_LIST.ESD_SENSOR_02.ToString()]                 = string.Empty,
                [EN_SVID_LIST.ESD_SENSOR_03.ToString()]                 = string.Empty,
                [EN_SVID_LIST.ESD_SENSOR_04.ToString()]                 = string.Empty,

                [EN_SVID_LIST.NEEDLE_HEIGHT.ToString()]                 = string.Empty,
                [EN_SVID_LIST.EXPENSION_HEIGHT.ToString()]              = string.Empty,
                [EN_SVID_LIST.PICK_SEARCH_LEVEL.ToString()]             = string.Empty,
                [EN_SVID_LIST.PICK_SEARCH_SPEED.ToString()]             = string.Empty,
                [EN_SVID_LIST.PICK_DELAY.ToString()]                    = string.Empty,
                [EN_SVID_LIST.PICK_FORCE.ToString()]                    = string.Empty,
                [EN_SVID_LIST.PICK_SLOWUP_LEVEL.ToString()]             = string.Empty,
                [EN_SVID_LIST.PICK_SLOWUP_SPEED.ToString()]             = string.Empty,
                [EN_SVID_LIST.PLACE_SEARCH_LEVEL.ToString()]            = string.Empty,
                [EN_SVID_LIST.PLACE_SEARCH_SPEED.ToString()]            = string.Empty,
                [EN_SVID_LIST.PLACE_DELAY.ToString()]                   = string.Empty,
                [EN_SVID_LIST.PLACE_FORCE.ToString()]                   = string.Empty,
                [EN_SVID_LIST.PLACE_SLOWUP_LEVEL.ToString()]            = string.Empty,
                [EN_SVID_LIST.PLACE_SLOWUP_SPEED.ToString()]            = string.Empty
            };

            _traceDataAtDetaching = new ConcurrentDictionary<string, string>
            {
                [EN_SVID_LIST.NEEDLE_HEIGHT.ToString()]                 = string.Empty,
                [EN_SVID_LIST.EXPENSION_HEIGHT.ToString()]              = string.Empty,
                [EN_SVID_LIST.PICK_SEARCH_LEVEL.ToString()]             = string.Empty,
                [EN_SVID_LIST.PICK_SEARCH_SPEED.ToString()]             = string.Empty,
                [EN_SVID_LIST.PICK_DELAY.ToString()]                    = string.Empty,
                [EN_SVID_LIST.PICK_FORCE.ToString()]                    = string.Empty,
                [EN_SVID_LIST.PICK_SLOWUP_LEVEL.ToString()]             = string.Empty,
                [EN_SVID_LIST.PICK_SLOWUP_SPEED.ToString()]             = string.Empty,
                [EN_SVID_LIST.PLACE_SEARCH_LEVEL.ToString()]            = string.Empty,
                [EN_SVID_LIST.PLACE_SEARCH_SPEED.ToString()]            = string.Empty,
                [EN_SVID_LIST.PLACE_DELAY.ToString()]                   = string.Empty,
                [EN_SVID_LIST.PLACE_FORCE.ToString()]                   = string.Empty,
                [EN_SVID_LIST.PLACE_SLOWUP_LEVEL.ToString()]            = string.Empty,
                [EN_SVID_LIST.PLACE_SLOWUP_SPEED.ToString()]            = string.Empty
            };

            _traceDataForPWA500BINByName = new Dictionary<string, long>
            {
                [EN_SVID_LIST.SUPPLY_BUFFER_IONIZER_FLOW.ToString()]    = 1750,
                [EN_SVID_LIST.SORTING_BUFFER_IONIZER_FLOW.ToString()]   = 1751,
                [EN_SVID_LIST.SUPPLY_STAGE_IONIZER_FLOW.ToString()]     = 1752,
                [EN_SVID_LIST.SORTING_STAGE_IONIZER_FLOW.ToString()]    = 1753,

                [EN_SVID_LIST.ESD_SENSOR_01.ToString()]                 = 1800,
                [EN_SVID_LIST.ESD_SENSOR_02.ToString()]                 = 1801,
                [EN_SVID_LIST.ESD_SENSOR_03.ToString()]                 = 1802,
                [EN_SVID_LIST.ESD_SENSOR_04.ToString()]                 = 1803,

                [EN_SVID_LIST.PM_FFU_SPEED_1.ToString()]                = 1754,
                [EN_SVID_LIST.PM_FFU_SPEED_2.ToString()]                = 1755,
                [EN_SVID_LIST.PM_FFU_SPEED_3.ToString()]                = 1756,
                [EN_SVID_LIST.EJECT_MEMBRANE_AIR_REGULATOR.ToString()]  = 1760,
                [EN_SVID_LIST.EJECT_MEMBRANE_VAC_PRESS.ToString()]      = 1761,
                [EN_SVID_LIST.EJECT_VAC_PRESS.ToString()]               = 1762,
                [EN_SVID_LIST.NEEDLE_HEIGHT.ToString()]                 = 2000,
                [EN_SVID_LIST.EXPENSION_HEIGHT.ToString()]              = 2001,
                [EN_SVID_LIST.PICK_SEARCH_LEVEL.ToString()]             = 2010,
                [EN_SVID_LIST.PICK_SEARCH_SPEED.ToString()]             = 2011,
                [EN_SVID_LIST.PICK_DELAY.ToString()]                    = 2012,
                [EN_SVID_LIST.PICK_FORCE.ToString()]                    = 2013,
                [EN_SVID_LIST.PICK_SLOWUP_LEVEL.ToString()]             = 2014,
                [EN_SVID_LIST.PICK_SLOWUP_SPEED.ToString()]             = 2015,
                [EN_SVID_LIST.PLACE_SEARCH_LEVEL.ToString()]            = 2020,
                [EN_SVID_LIST.PLACE_SEARCH_SPEED.ToString()]            = 2021,
                [EN_SVID_LIST.PLACE_DELAY.ToString()]                   = 2022,
                [EN_SVID_LIST.PLACE_FORCE.ToString()]                   = 2023,
                [EN_SVID_LIST.PLACE_SLOWUP_LEVEL.ToString()]            = 2024,
                [EN_SVID_LIST.PLACE_SLOWUP_SPEED.ToString()]            = 2025
            };
            #endregion </TraceData For PWA500BIN>

            // 2025.01.06. jhlim [ADD] EFEM 가동하는 순간, 값이 0이어서 FDC 에러가 발생한다.
            // 해결책은
            // 1. EquipmentState가 Idle 이상일 경우에만 Trace Data Update
            // 2. 마지막 업데이트된 값을 저장하고, 기동 시 로드하여 값을 Set
            #region <Set Initial Variable Values from file>
            Dictionary<long, string> valuesFromFile = new Dictionary<long, string>(_variablesToUpdate);
            if (_scenarioManager.ReadVariableValuesFromFile(ref valuesFromFile))
            {
                foreach (var item in valuesFromFile)
                {
                    if (_variablesToUpdate.ContainsKey(item.Key))
                    {
                        _variablesToUpdate[item.Key] = item.Value;
                    }
                }

                UpdateVariable(_variablesToUpdate.Keys.ToArray(), _variablesToUpdate.Values.ToArray());
            }
            #endregion </Set Initial Variable Values from file>
            // 2025.01.06. jhlim [END] 

            //var traceVariables = new Dictionary<string, long>
            //{
            //    #region <EFEM>
            //    [EN_SVID_LIST.EFEM_MAIN_CDA_PRESSURE.ToString()] = 1700,
            //    [EN_SVID_LIST.EFEM_MAIN_VAC_PRESSURE.ToString()] = 1701,
            //    [EN_SVID_LIST.ROBOT_CDA_PRESSURE.ToString()]     = 1702,
            //    [EN_SVID_LIST.IONIZER_PRESSURE.ToString()]       = 1703,
            //    [EN_SVID_LIST.IONIZER_FLOW_METER_1.ToString()]   = 1704,
            //    [EN_SVID_LIST.IONIZER_FLOW_METER_2.ToString()]   = 1705,
            //    [EN_SVID_LIST.IONIZER_FLOW_METER_3.ToString()]   = 1706,
            //    [EN_SVID_LIST.IONIZER_FLOW_METER_4.ToString()]   = 1707,
            //    [EN_SVID_LIST.EFEM_FFU_SPEED_1.ToString()]       = 1708,
            //    [EN_SVID_LIST.EFEM_FFU_SPEED_2.ToString()]       = 1709,
            //    [EN_SVID_LIST.EFEM_FFU_SPEED_3.ToString()]       = 1710,
            //    #endregion </EFEM>

            //    #region <PWA-500BIN>
            //    [EN_SVID_LIST.SUPPLY_BUFFER_IONIZER_FLOW.ToString()]    = 1750,
            //    [EN_SVID_LIST.SORTING_BUFFER_IONIZER_FLOW.ToString()]   = 1751,
            //    [EN_SVID_LIST.SUPPLY_STAGE_IONIZER_FLOW.ToString()]     = 1752,
            //    [EN_SVID_LIST.SORTING_STAGE_IONIZER_FLOW.ToString()]    = 1753,
            //    [EN_SVID_LIST.PM_FFU_SPEED_1.ToString()]                = 1754,
            //    [EN_SVID_LIST.PM_FFU_SPEED_2.ToString()]                = 1755,
            //    [EN_SVID_LIST.PM_FFU_SPEED_3.ToString()]                = 1756,
            //    [EN_SVID_LIST.EJECT_MEMBRANE_AIR_REGULATOR.ToString()]  = 1760,
            //    [EN_SVID_LIST.EJECT_MEMBRANE_VAC_PRESS.ToString()]      = 1761,
            //    [EN_SVID_LIST.EJECT_VAC_PRESS.ToString()]               = 1762,
            //    [EN_SVID_LIST.NEEDLE_HEIGHT.ToString()]                 = 2000,
            //    [EN_SVID_LIST.EXPENSION_HEIGHT.ToString()]              = 2001,
            //    [EN_SVID_LIST.PICK_SEARCH_LEVEL.ToString()]             = 2010,
            //    [EN_SVID_LIST.PICK_SEARCH_SPEED.ToString()]             = 2011,
            //    [EN_SVID_LIST.PICK_DELAY.ToString()]                    = 2012,
            //    [EN_SVID_LIST.PICK_FORCE.ToString()]                    = 2013,
            //    [EN_SVID_LIST.PICK_SLOWUP_LEVEL.ToString()]             = 2014,
            //    [EN_SVID_LIST.PICK_SLOWUP_SPEED.ToString()]             = 2015,
            //    [EN_SVID_LIST.PLACE_SEARCH_LEVEL.ToString()]            = 2020,
            //    [EN_SVID_LIST.PLACE_SEARCH_SPEED.ToString()]            = 2021,
            //    [EN_SVID_LIST.PLACE_DELAY.ToString()]                   = 2022,
            //    [EN_SVID_LIST.PLACE_FORCE.ToString()]                   = 2023,
            //    [EN_SVID_LIST.PLACE_SLOWUP_LEVEL.ToString()]            = 2024,
            //    [EN_SVID_LIST.PLACE_SLOWUP_SPEED.ToString()]            = 2025
            //    #endregion </PWA-500BIN>
            //};
            //traceDataInfo.AddDataTable(traceVariables, 1000);

            //foreach (var item in traceVariables)
            //{
            //    _variablesToUpdate[item.Value] = string.Empty;
            //}

            //traceDataVariableIds = new List<long>(_variablesToUpdate.Keys.ToList());

            return true;
        }
        protected override void MakeScenarioByConfigFiles(string configPath)
        {
            string path = string.Format(@"{0}\ScenarioListConfig.txt", configPath);
            if (false == File.Exists(path))
                return;

            StreamReader sr = null;
            try
            {
                sr = new StreamReader(path);
                string fileBody = sr.ReadToEnd();
                string[] lines = fileBody.Split('\n');
                for (int i = 1; i < lines.Length; ++i)
                {
                    string line = lines[i];
                    string[] fields = line.Split('\t');
                    if (fields.Length < 2)
                        continue;

                    string scenarioName = fields[0];
                    if (false == Enum.TryParse(scenarioName, out ScenarioListTypes scenario))
                        continue;

                    // 0 : name, 1 : type, 2 : permission
                    if (false == Enum.TryParse(fields[1], out ScenarioTypes scenarioType))
                        continue;

                    switch (scenarioType)
                    {
                        case ScenarioTypes.SendingEventScenario:
                            {
                                if (fields.Length < 4)
                                    break;

                                if (false == long.TryParse(fields[2], out long eventId))
                                    break;

                                if (false == bool.TryParse(fields[3], out bool useConfirmation))
                                    break;

                                string eventName = string.Empty;
                                foreach (var item in CollectionEventList)
                                {
                                    if (item.Value.Id.Equals(eventId))
                                    {
                                        eventName = item.Key;
                                        break;
                                    }
                                }
                                if (string.IsNullOrEmpty(eventName))
                                    break;

                                MakeScenario(scenario,
                                    new SendingEventScenario(scenarioName, eventId, CollectionEventList[eventName].Variables.Keys.ToList(), useConfirmation));
                            }
                            break;
                        case ScenarioTypes.ClientToClientCommunicationScenario:
                            {
                                MakeScenario(scenario, new ClientToClientCommunicationScenario(scenarioName));
                            }
                            break;
                        case ScenarioTypes.Custom:
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (Exception)
            {
            }

            if (sr != null)
                sr.Close();
        }

        public override List<string> GetScenarioParameterList(Enum scenario)
        {
            if (false == ScenarioList.TryGetValue(scenario, out ScenarioBaseClass scen))
                return null;

            List<string> parameterList = null;
            if (scen is SendingEventScenario)
            {
                var convertedScen = scen as SendingEventScenario;

                foreach (var item in CollectionEventList)
                {
                    if (item.Value.Id.Equals(convertedScen.EventId))
                    {
                        parameterList = new List<string>();
                        foreach (var kvp in item.Value.Variables)
                        {
                            parameterList.Add(kvp.Value.Name);
                        }
                        break;
                    }
                }
            }
            else if (scen is ScenarioReqLotInfo)
            {
                var convertedScen = scen as ScenarioReqLotInfo;
                foreach (var item in CollectionEventList)
                {
                    if (item.Value.Id.Equals(convertedScen.EventId))
                    {
                        parameterList = new List<string>();
                        foreach (var kvp in item.Value.Variables)
                        {
                            parameterList.Add(kvp.Value.Name);
                        }
                        break;
                    }
                }
            }
            else if (scen is ScenarioReqLotMergeAndChange)
            {
                var convertedScen = scen as ScenarioReqLotMergeAndChange;
                parameterList = new List<string>();
                foreach (var item in CollectionEventList)
                {
                    if (item.Value.Id.Equals(convertedScen.EventIdMerge) ||
                        item.Value.Id.Equals(convertedScen.EventIdChange))
                    {
                        foreach (var kvp in item.Value.Variables)
                        {
                            if (false == parameterList.Contains(kvp.Value.Name))
                            {
                                parameterList.Add(kvp.Value.Name);
                            }
                        }
                    }
                }
            }
            else if (scen is ScenarioProceedWithCarrier)
            {
                var convertedScen = scen as ScenarioProceedWithCarrier;

                foreach (var item in CollectionEventList)
                {
                    if (item.Value.Id.Equals(convertedScen.EventId))
                    {
                        parameterList = new List<string>();
                        foreach (var kvp in item.Value.Variables)
                        {
                            parameterList.Add(kvp.Value.Name);
                        }
                        break;
                    }
                }
            }
            else if (scen is EFEM.CustomizedByProcessType.PWA500BIN.ScenarioRecipeHandlingRequest)
            {
                parameterList = new List<string>();
                parameterList.Add(RecipeHandlingKeys.KeyParamLotId);
                parameterList.Add(RecipeHandlingKeys.KeyParamRecipeId);
                parameterList.Add(RecipeHandlingKeys.KeyParamPartId);
                parameterList.Add(RecipeHandlingKeys.KeyParamStepSeq);
                parameterList.Add(RecipeHandlingKeys.KeyParamLotType);
                parameterList.Add(RecipeHandlingKeys.KeyUseCommunicationToPM);
            }
            else if (scen is ScenarioReqWorkStart)
            {
                //var convertedScen = scen as ScenarioReqWorkStart;
                parameterList = new List<string>();
                parameterList.Add(RequestDownloadMapFileKeys.KeyParamCarrierId);
                parameterList.Add(RequestDownloadMapFileKeys.KeyParamPortId);
                parameterList.Add(RequestDownloadMapFileKeys.KeyParamLotId);
                parameterList.Add(RequestDownloadMapFileKeys.KeyParamPartId);
                parameterList.Add(RequestDownloadMapFileKeys.KeyParamRecipeId);
                parameterList.Add(RequestDownloadMapFileKeys.KeyParamOperatorId);
                parameterList.Add(RequestDownloadMapFileKeys.KeyParamWaferId);
                parameterList.Add(RequestDownloadMapFileKeys.KeyParamAngle);
                parameterList.Add(RequestDownloadMapFileKeys.KeyNullBinCode);
                parameterList.Add(RequestDownloadMapFileKeys.KeyUseEventHandling);
                //          foreach (var item in CollectionEventList)
                //          {
                //              if (item.Value.Id.Equals(convertedScen.EventId))
                //              {
                //                  parameterList = new List<string>();
                //                  foreach (var kvp in item.Value.Variables)
                //                  {
                //                      parameterList.Add(kvp.Value.Name);
                //                  }
                //var data = convertedScen.WaferData.GetDataAll();
                //if (data != null)
                //                  {
                //                      foreach (var kvp in data)
                //                      {
                //		parameterList.Add(kvp.Key);
                //	}
                //                  }
                //break;
                //              }
                //          }
            }
            else if (scen is ScenarioReqWorkEnd || scen is ScenarioUploadBinData)
            {
                //var convertedScen = scen as ScenarioReqWorkEnd;
                parameterList = new List<string>();
                parameterList.Add(UploadCoreOrBinFileKeys.KeyParamCarrierId);
                parameterList.Add(UploadCoreOrBinFileKeys.KeyParamPortId);
                parameterList.Add(UploadCoreOrBinFileKeys.KeyParamLotId);
                parameterList.Add(UploadCoreOrBinFileKeys.KeyParamRecipeId);
                parameterList.Add(UploadCoreOrBinFileKeys.KeyParamPartId);
                parameterList.Add(UploadCoreOrBinFileKeys.KeyParamOperatorId);
                parameterList.Add(UploadCoreOrBinFileKeys.KeyChipQty);
                parameterList.Add(UploadCoreOrBinFileKeys.KeyMapData);
                parameterList.Add(UploadCoreOrBinFileKeys.KeyPMSFileName);
                parameterList.Add(UploadCoreOrBinFileKeys.KeyPMSFileBody);

                // 여기부터 웨이퍼 데이터
                parameterList.Add(UploadCoreOrBinFileKeys.KeySubstrateName);
                parameterList.Add(UploadCoreOrBinFileKeys.KeyWaferAngle);
                parameterList.Add(UploadCoreOrBinFileKeys.KeyCountRow);
                parameterList.Add(UploadCoreOrBinFileKeys.KeyCountCol);
                parameterList.Add(UploadCoreOrBinFileKeys.KeyReferenceX);
                parameterList.Add(UploadCoreOrBinFileKeys.KeyReferenceY);
                parameterList.Add(UploadCoreOrBinFileKeys.KeyStartingPosX);
                parameterList.Add(UploadCoreOrBinFileKeys.KeyStartingPosY);
                parameterList.Add(UploadCoreOrBinFileKeys.KeyNullBinCode);
            }
            else if (scen is ScenarioReqWaferSplitFromLot)
            {
                var convertedScen = scen as ScenarioReqWaferSplitFromLot;
                foreach (var item in CollectionEventList)
                {
                    if (item.Value.Id.Equals(convertedScen.EventId))
                    {
                        parameterList = new List<string>();
                        foreach (var kvp in item.Value.Variables)
                        {
                            parameterList.Add(kvp.Value.Name);
                        }
                        break;
                    }
                }
            }
            else if (scen is ScenarioReqChipSplit)
            {
                var convertedScen = scen as ScenarioReqChipSplit;
                foreach (var item in CollectionEventList)
                {
                    if (item.Value.Id.Equals(convertedScen.EventId))
                    {
                        parameterList = new List<string>();
                        foreach (var kvp in item.Value.Variables)
                        {
                            parameterList.Add(kvp.Value.Name);
                        }
                        break;
                    }
                }
            }
            else if (scen is ScenarioReqUploadBinFile)
            {
                parameterList = new List<string>();
                parameterList.Add(UploadCoreOrBinFileKeys.KeySubstrateName);
                parameterList.Add(UploadCoreOrBinFileKeys.KeyRingId);
                parameterList.Add(UploadCoreOrBinFileKeys.KeyRecipeId);
                parameterList.Add(UploadCoreOrBinFileKeys.KeySubstrateType);
                parameterList.Add(UploadCoreOrBinFileKeys.KeyStepId);
                parameterList.Add(UploadCoreOrBinFileKeys.KeyEquipId);
                parameterList.Add(UploadCoreOrBinFileKeys.KeyPartId);
                parameterList.Add(UploadCoreOrBinFileKeys.KeySlot);
                parameterList.Add(UploadCoreOrBinFileKeys.KeyLotId);
            }
            else if (scen is ScenarioReqWaferIdAssign)
            {
                var convertedScen = scen as ScenarioReqWaferIdAssign;
                foreach (var item in CollectionEventList)
                {
                    if (item.Value.Id.Equals(convertedScen.EventId))
                    {
                        parameterList = new List<string>();
                        foreach (var kvp in item.Value.Variables)
                        {
                            parameterList.Add(kvp.Value.Name);
                        }
                        break;
                    }
                }
            }
            return parameterList;
        }

        public override Dictionary<string, string> GetScenarioResultData(Enum scenario)
        {
            if (false == ScenarioList.TryGetValue(scenario, out ScenarioBaseClass scen))
                return null;

            return scen.GetResultData();
        }

        public override bool UpdateScenarioParams(string scenarioName, Dictionary<string, string> param)
        {
            if (false == Enum.TryParse(scenarioName, out ScenarioListTypes scenario))
                return false;

            switch (scenario)
            {
                #region <Event Based Scenario>
                case ScenarioListTypes.SCENARIO_EQUIPMENT_START:
                case ScenarioListTypes.SCENARIO_EQUIPMENT_END:
                case ScenarioListTypes.SCENARIO_PROCESS_START:
                case ScenarioListTypes.SCENARIO_PROCESS_END:
                case ScenarioListTypes.SCENARIO_ERROR_START:
                case ScenarioListTypes.SCENARIO_ERROR_STOP:
                case ScenarioListTypes.SCENARIO_PORT_STATUS_LOAD_1:
                case ScenarioListTypes.SCENARIO_PORT_STATUS_LOAD_2:
                case ScenarioListTypes.SCENARIO_PORT_STATUS_LOAD_3:
                case ScenarioListTypes.SCENARIO_PORT_STATUS_LOAD_4:
                case ScenarioListTypes.SCENARIO_PORT_STATUS_LOAD_5:
                case ScenarioListTypes.SCENARIO_PORT_STATUS_LOAD_6:
                case ScenarioListTypes.SCENARIO_PORT_STATUS_UNLOAD_1:
                case ScenarioListTypes.SCENARIO_PORT_STATUS_UNLOAD_2:
                case ScenarioListTypes.SCENARIO_PORT_STATUS_UNLOAD_3:
                case ScenarioListTypes.SCENARIO_PORT_STATUS_UNLOAD_4:
                case ScenarioListTypes.SCENARIO_PORT_STATUS_UNLOAD_5:
                case ScenarioListTypes.SCENARIO_PORT_STATUS_UNLOAD_6:
                case ScenarioListTypes.SCENARIO_CARRIER_LOAD:
                case ScenarioListTypes.SCENARIO_CARRIER_UNLOAD:
                case ScenarioListTypes.SCENARIO_RFID_READ_CORE_1:
                case ScenarioListTypes.SCENARIO_RFID_READ_CORE_2:
                case ScenarioListTypes.SCENARIO_RFID_READ_EMPTY_TAPE:
                case ScenarioListTypes.SCENARIO_RFID_READ_BIN_1:
                case ScenarioListTypes.SCENARIO_RFID_READ_BIN_2:
                case ScenarioListTypes.SCENARIO_RFID_READ_BIN_3:
                case ScenarioListTypes.SCENARIO_REQ_TRACK_IN:
                case ScenarioListTypes.SCENARIO_REQ_CORE_WAFER_TRACK_OUT:
                case ScenarioListTypes.SCENARIO_REQ_LOT_MATCH:
                case ScenarioListTypes.SCENARIO_REQ_BIN_WAFER_TRACK_OUT:
                case ScenarioListTypes.SCENARIO_SLOT_WAFER_MAPPING_CORE_1:
                case ScenarioListTypes.SCENARIO_SLOT_WAFER_MAPPING_CORE_2:
                case ScenarioListTypes.SCENARIO_SLOT_WAFER_MAPPING_EMPTY_TAPE:
                case ScenarioListTypes.SCENARIO_SLOT_WAFER_MAPPING_BIN_1:
                case ScenarioListTypes.SCENARIO_SLOT_WAFER_MAPPING_BIN_2:
                case ScenarioListTypes.SCENARIO_SLOT_WAFER_MAPPING_BIN_3:
                case ScenarioListTypes.SCENARIO_REQ_CORE_WAFER_SPLIT_LAST:
                case ScenarioListTypes.SCENARIO_REQ_CORE_CHIP_MERGE:
                case ScenarioListTypes.SCENARIO_CORE_WAFER_DETACH_END:
                case ScenarioListTypes.SCENARIO_UPLOAD_SCRAP_DATA:
                case ScenarioListTypes.SCENARIO_BIN_WAFER_ID_READ:
                case ScenarioListTypes.SCENARIO_BIN_SORTING_START_1:
                case ScenarioListTypes.SCENARIO_BIN_SORTING_END_1:
                case ScenarioListTypes.SCENARIO_BIN_SORTING_START_2:
                case ScenarioListTypes.SCENARIO_BIN_SORTING_END_2:
                case ScenarioListTypes.SCENARIO_BIN_SORTING_START_3:
                case ScenarioListTypes.SCENARIO_BIN_SORTING_END_3:
                case ScenarioListTypes.SCENARIO_REQ_COLLET_CHANGE_1:
                case ScenarioListTypes.SCENARIO_REQ_COLLET_CHANGE_2:
                case ScenarioListTypes.SCENARIO_REQ_HOOD_CHANGE:
                    {
                        ScenarioList[scenario].UpdateParamValues(new SendingEventParamValues(param.Values.ToList()));
                    }
                    break;
                case ScenarioListTypes.SCENARIO_CORE_WAFER_DETACH_START:
                    {
                        List<string> variablesToUpdate = new List<string>();
                        string vidName;
                        #region <EES>
                        vidName = EN_SVID_LIST.CARRIERID.ToString();
                        if (param.ContainsKey(vidName))
                        {
                            variablesToUpdate.Add(param[vidName]);
                        }

                        vidName = EN_SVID_LIST.PORTID.ToString();
                        if (param.ContainsKey(vidName))
                        {
                            variablesToUpdate.Add(param[vidName]);
                        }

                        vidName = EN_SVID_LIST.LOTID.ToString();
                        if (param.ContainsKey(vidName))
                        {
                            variablesToUpdate.Add(param[vidName]);
                        }

                        vidName = EN_SVID_LIST.PARTID.ToString();
                        if (param.ContainsKey(vidName))
                        {
                            variablesToUpdate.Add(param[vidName]);
                        }

                        vidName = EN_SVID_LIST.RECIPEID.ToString();
                        if (param.ContainsKey(vidName))
                        {
                            variablesToUpdate.Add(param[vidName]);
                        }

                        vidName = EN_SVID_LIST.WAFERID.ToString();
                        if (param.ContainsKey(vidName))
                        {
                            variablesToUpdate.Add(param[vidName]);
                        }

                        vidName = EN_SVID_LIST.SLOTID.ToString();
                        if (param.ContainsKey(vidName))
                        {
                            variablesToUpdate.Add(param[vidName]);
                        }

                        vidName = EN_SVID_LIST.OPERID.ToString();
                        if (param.ContainsKey(vidName))
                        {
                            variablesToUpdate.Add(param[vidName]);
                        }
                        #endregion </EES>

                        #region <ERD>
                        vidName = EN_SVID_LIST.NEEDLE_HEIGHT.ToString();
                        if (param.ContainsKey(vidName))
                        {
                            variablesToUpdate.Add(param[vidName]);
                        }

                        vidName = EN_SVID_LIST.EXPENSION_HEIGHT.ToString();
                        if (param.ContainsKey(vidName))
                        {
                            variablesToUpdate.Add(param[vidName]);
                        }

                        vidName = EN_SVID_LIST.PICK_SEARCH_LEVEL.ToString();
                        if (param.ContainsKey(vidName))
                        {
                            variablesToUpdate.Add(param[vidName]);
                        }

                        vidName = EN_SVID_LIST.PICK_SEARCH_SPEED.ToString();
                        if (param.ContainsKey(vidName))
                        {
                            variablesToUpdate.Add(param[vidName]);
                        }

                        vidName = EN_SVID_LIST.PICK_DELAY.ToString();
                        if (param.ContainsKey(vidName))
                        {
                            variablesToUpdate.Add(param[vidName]);
                        }

                        vidName = EN_SVID_LIST.PICK_FORCE.ToString();
                        if (param.ContainsKey(vidName))
                        {
                            variablesToUpdate.Add(param[vidName]);
                        }

                        vidName = EN_SVID_LIST.PICK_SLOWUP_LEVEL.ToString();
                        if (param.ContainsKey(vidName))
                        {
                            variablesToUpdate.Add(param[vidName]);
                        }

                        vidName = EN_SVID_LIST.PICK_SLOWUP_SPEED.ToString();
                        if (param.ContainsKey(vidName))
                        {
                            variablesToUpdate.Add(param[vidName]);
                        }

                        vidName = EN_SVID_LIST.PLACE_SEARCH_LEVEL.ToString();
                        if (param.ContainsKey(vidName))
                        {
                            variablesToUpdate.Add(param[vidName]);
                        }

                        vidName = EN_SVID_LIST.PLACE_SEARCH_SPEED.ToString();
                        if (param.ContainsKey(vidName))
                        {
                            variablesToUpdate.Add(param[vidName]);
                        }

                        vidName = EN_SVID_LIST.PLACE_DELAY.ToString();
                        if (param.ContainsKey(vidName))
                        {
                            variablesToUpdate.Add(param[vidName]);
                        }

                        vidName = EN_SVID_LIST.PLACE_FORCE.ToString();
                        if (param.ContainsKey(vidName))
                        {
                            variablesToUpdate.Add(param[vidName]);
                        }

                        vidName = EN_SVID_LIST.PLACE_SLOWUP_LEVEL.ToString();
                        if (param.ContainsKey(vidName))
                        {
                            variablesToUpdate.Add(param[vidName]);
                        }

                        vidName = EN_SVID_LIST.PLACE_SLOWUP_SPEED.ToString();
                        if (param.ContainsKey(vidName))
                        {
                            variablesToUpdate.Add(param[vidName]);
                        }
                        #endregion </ERD>

                        ScenarioList[scenario].UpdateParamValues(new SendingEventParamValues(variablesToUpdate));
                    }
                    break;
                #endregion </Event Based Scenario>

                ////////////////////////////////////////////////////
                case ScenarioListTypes.SCENARIO_REQ_LOT_INFO_CORE_1:
                case ScenarioListTypes.SCENARIO_REQ_LOT_INFO_CORE_2:
                case ScenarioListTypes.SCENARIO_REQ_LOT_INFO_EMPTY_TAPE:
                    {
                        ScenarioList[scenario].UpdateParamValues(new ScenarioReqLotInfoParamValues(param.Values.ToList()));
                    }
                    break;
                case ScenarioListTypes.SCENARIO_BIN_PART_ID_INFO_REQ:
                    {
                        ScenarioList[scenario].UpdateParamValues(new ScenarioReqPartIdInfoParamValues(param.Values.ToList()));
                    }
                    break;

                case ScenarioListTypes.SCENARIO_REQ_SLOT_INFO_CORE_1:
                case ScenarioListTypes.SCENARIO_REQ_SLOT_INFO_CORE_2:
                case ScenarioListTypes.SCENARIO_REQ_SLOT_INFO_EMPTY_TAPE:
                    {
                        ScenarioList[scenario].UpdateParamValues(new ScenarioProceedWithCarrierParamValues(param.Values.ToList()));
                    }
                    break;

                case ScenarioListTypes.SCENARIO_REQ_RECIPE_DOWNLOAD:
                    {
                        if (false == param.TryGetValue(RecipeHandlingKeys.KeyParamLotId, out _))
                            return false;
                        if (false == param.TryGetValue(RecipeHandlingKeys.KeyParamRecipeId, out string recipeId))
                            return false;
                        if (false == param.TryGetValue(RecipeHandlingKeys.KeyParamPartId, out _))
                            return false;
                        if (false == param.TryGetValue(RecipeHandlingKeys.KeyParamStepSeq, out _))
                            return false;
                        if (false == param.TryGetValue(RecipeHandlingKeys.KeyParamLotType, out _))
                            return false;
                        if (false == param.TryGetValue(RecipeHandlingKeys.KeyUseCommunicationToPM, out string useComm))
                            return false;
                        if (false == bool.TryParse(useComm, out bool useCommunication))
                            return false;
                        //if (false == RecipePath.TryGetValue(NameOfPM, out string recipePathForPM))
                        //    return false;

                        // TODO : 파일 생성 후 바디로 읽어옴

                        string fullPath = string.Format(@"{0}\Download\{1}\{2}{3}", _recipePath, recipeId, recipeId, FileFormat.FILEFORMAT_RECIPE);
                        var paramToSend = new Dictionary<string, string>
                        {
                            [RecipeHandlingKeys.KeyRecipeId] = recipeId,
                            [RecipeHandlingKeys.KeyRecipeBody] = string.Empty,      // TODO : 레시피 바디를 읽어서 넣는다.
                        };

                        param.Remove(RecipeHandlingKeys.KeyUseCommunicationToPM);
                        ScenarioList[scenario].UpdateParamValues(new EFEM.CustomizedByProcessType.PWA500BIN.RecipeHandlingRequestParamValues(NameOfClient, MessagesToSend.RequestDownloadRecipe.ToString(),
                            paramToSend, useCommunication,
                            param.Values.ToList(), recipeId, fullPath));
                    }
                    break;
                case ScenarioListTypes.SCENARIO_REQ_RECIPE_UPLOAD:
                    {
                        if (false == param.TryGetValue(RecipeHandlingKeys.KeyParamLotId, out _))
                            return false;
                        if (false == param.TryGetValue(RecipeHandlingKeys.KeyParamRecipeId, out string recipeId))
                            return false;
                        if (false == param.TryGetValue(RecipeHandlingKeys.KeyParamPartId, out _))
                            return false;
                        if (false == param.TryGetValue(RecipeHandlingKeys.KeyParamStepSeq, out _))
                            return false;
                        if (false == param.TryGetValue(RecipeHandlingKeys.KeyParamLotType, out _))
                            return false;
                        if (false == param.TryGetValue(RecipeHandlingKeys.KeyUseCommunicationToPM, out string useComm))
                            return false;
                        if (false == bool.TryParse(useComm, out bool useCommunication))
                            return false;

                        var paramToSend = new Dictionary<string, string>
                        {
                            [RecipeHandlingKeys.KeyRecipeId] = recipeId
                        };
                        param.Remove(RecipeHandlingKeys.KeyUseCommunicationToPM);
                        ScenarioList[scenario].UpdateParamValues(new EFEM.CustomizedByProcessType.PWA500BIN.RecipeHandlingRequestParamValues(NameOfClient, MessagesToSend.RequestUploadRecipe.ToString(),
                            paramToSend, useCommunication,
                            param.Values.ToList(), recipeId, _recipePath));
                    }
                    break;
                case ScenarioListTypes.SCENARIO_REQ_LOT_MERGE_CORE_1:
                case ScenarioListTypes.SCENARIO_REQ_LOT_MERGE_CORE_2:
                case ScenarioListTypes.SCENARIO_REQ_LOT_ID_MERGE_AND_CHANGE_BIN_1:
                case ScenarioListTypes.SCENARIO_REQ_LOT_ID_MERGE_AND_CHANGE_BIN_2:
                case ScenarioListTypes.SCENARIO_REQ_LOT_ID_MERGE_AND_CHANGE_BIN_3:
                    {
                        if (false == param.TryGetValue(LotMergeKeys.KeyParamLotId, out string lotId))
                            return false;
                        if (false == param.TryGetValue(LotMergeKeys.KeyParamCarrierId, out string carrierId))
                            return false;

                        if (scenario.Equals(ScenarioListTypes.SCENARIO_REQ_LOT_MERGE_CORE_1) ||
                            scenario.Equals(ScenarioListTypes.SCENARIO_REQ_LOT_MERGE_CORE_2))
                        {
                            ScenarioList[scenario].UpdateParamValues(new ScenarioReqLotMergeAndChangeParamValues(lotId, carrierId, param.Values.ToList(), null));
                        }
                        else
                        {
                            List<string> mergeData = new List<string>();
                            List<string> changeData = new List<string>();

                            //
                            foreach (var item in param)
                            {
                                switch (item.Key)
                                {
                                    case LotMergeKeys.KeyParamLotId:
                                    case LotMergeKeys.KeyParamCarrierId:
                                        mergeData.Add(item.Value);
                                        changeData.Add(item.Value);
                                        break;
                                    case LotMergeKeys.KeyParamPartId:
                                    case LotMergeKeys.KeyParamRecipeId:
                                    case LotMergeKeys.KeyOperatorId:
                                        mergeData.Add(item.Value);
                                        break;

                                    default:
                                        {
                                            if (item.Key.Contains(LotMergeKeys.KeyParamSlotLotIdPost))
                                            {
                                                mergeData.Add(item.Value);
                                            }
                                            else if (item.Key.Contains(SlotMappingKeys.KeyParamSlotNamePost) ||
                                                item.Key.Contains(SlotMappingKeys.KeyParamSlotQtyPost))
                                            {
                                                changeData.Add(item.Value);
                                            }
                                        }
                                        break;
                                }
                            }

                            ScenarioList[scenario].UpdateParamValues(new ScenarioReqLotMergeAndChangeParamValues(lotId, carrierId, mergeData, changeData));
                        }


                    }
                    break;

                case ScenarioListTypes.SCENARIO_WORK_START:
                    {
                        List<string> vids = new List<string>();
                        vids.Add(param[RequestDownloadMapFileKeys.KeyParamCarrierId]);
                        vids.Add(param[RequestDownloadMapFileKeys.KeyParamPortId]);
                        vids.Add(param[RequestDownloadMapFileKeys.KeyParamLotId]);
                        vids.Add(param[RequestDownloadMapFileKeys.KeyParamPartId]);
                        vids.Add(param[RequestDownloadMapFileKeys.KeyParamRecipeId]);
                        vids.Add(param[RequestDownloadMapFileKeys.KeyParamOperatorId]);
                        vids.Add(param[RequestDownloadMapFileKeys.KeyParamWaferId]);

                        double.TryParse(param[RequestDownloadMapFileKeys.KeyParamAngle], out double angle);
                        bool.TryParse(param[UploadCoreOrBinFileKeys.KeyUseEventHandling], out bool useEventHandling);

                        var waferMapData = new WaferMapData
                        {
                            WaferId = param[RequestDownloadMapFileKeys.KeyParamWaferId],
                            Angle = angle,
                            NullBinCode = param[RequestDownloadMapFileKeys.KeyNullBinCode]
                        };

                        ScenarioList[scenario].UpdateParamValues(new ScenarioReqWorkStartParamValues(vids, useEventHandling, waferMapData));
                    }
                    break;
                case ScenarioListTypes.SCENARIO_WORK_END:
                    {
                        List<string> vids = new List<string>();
                        vids.Add(param[UploadCoreOrBinFileKeys.KeyParamCarrierId]);
                        vids.Add(param[UploadCoreOrBinFileKeys.KeyParamPortId]);
                        vids.Add(param[UploadCoreOrBinFileKeys.KeyParamLotId]);
                        vids.Add(param[UploadCoreOrBinFileKeys.KeyParamRecipeId]);
                        vids.Add(param[UploadCoreOrBinFileKeys.KeyParamPartId]);
                        vids.Add(param[UploadCoreOrBinFileKeys.KeyParamOperatorId]);
                        vids.Add(param[UploadCoreOrBinFileKeys.KeyChipQty]);

                        double.TryParse(param[UploadCoreOrBinFileKeys.KeyWaferAngle], out double angle);
                        int.TryParse(param[UploadCoreOrBinFileKeys.KeyCountRow], out int row);
                        int.TryParse(param[UploadCoreOrBinFileKeys.KeyCountCol], out int col);
                        int.TryParse(param[UploadCoreOrBinFileKeys.KeyReferenceX], out int refX);
                        int.TryParse(param[UploadCoreOrBinFileKeys.KeyReferenceY], out int refY);
                        int.TryParse(param[UploadCoreOrBinFileKeys.KeyStartingPosX], out int startingX);
                        int.TryParse(param[UploadCoreOrBinFileKeys.KeyStartingPosY], out int startingY);
                        bool.TryParse(param[UploadCoreOrBinFileKeys.KeyUseEventHandling], out bool useEventHandling);
                        int.TryParse(param[UploadCoreOrBinFileKeys.KeyChipQty], out int chipQty);
                        var waferMapData = new WaferMapData
                        {
                            WaferId = param[UploadCoreOrBinFileKeys.KeySubstrateName],
                            Angle = angle,
                            CountOfRow = row,
                            CountOfCol = col,
                            IndexOfRefX = refX,
                            IndexOfRefY = refY,
                            IndexOfStartingX = startingX,
                            IndexOfStartingY = startingY,
                            CountOfProcessDies = chipQty,
                            MapData = param[UploadCoreOrBinFileKeys.KeyMapData]
                        };

                        ScenarioList[scenario].UpdateParamValues(new ScenarioReqWorkEndParamValues(vids, useEventHandling, string.Empty, waferMapData));
                    }
                    break;
                case ScenarioListTypes.SCENARIO_BIN_WORK_END:
                    {
                        List<string> vids = new List<string>();
                        vids.Add(param[UploadCoreOrBinFileKeys.KeyParamCarrierId]);
                        vids.Add(param[UploadCoreOrBinFileKeys.KeyParamPortId]);
                        vids.Add(param[UploadCoreOrBinFileKeys.KeyParamLotId]);
                        vids.Add(param[UploadCoreOrBinFileKeys.KeyParamRecipeId]);
                        vids.Add(param[UploadCoreOrBinFileKeys.KeyParamPartId]);
                        vids.Add(param[UploadCoreOrBinFileKeys.KeyParamOperatorId]);
                        vids.Add(param[UploadCoreOrBinFileKeys.KeyChipQty]);

                        bool.TryParse(param[UploadCoreOrBinFileKeys.KeyUseEventHandling], out bool useEventHandling);

                        ScenarioList[scenario].UpdateParamValues(new ScenarioReqWorkEndParamValues(vids, useEventHandling, string.Empty, null));
                    }
                    break;
                case ScenarioListTypes.SCENARIO_BIN_DATA_UPLOAD:
                    {
                        List<string> vids = new List<string>();
                        vids.Add(param[UploadCoreOrBinFileKeys.KeyParamCarrierId]);
                        vids.Add(param[UploadCoreOrBinFileKeys.KeyParamPortId]);
                        vids.Add(param[UploadCoreOrBinFileKeys.KeyParamLotId]);
                        vids.Add(param[UploadCoreOrBinFileKeys.KeyParamRecipeId]);
                        vids.Add(param[UploadCoreOrBinFileKeys.KeyParamPartId]);
                        vids.Add(param[UploadCoreOrBinFileKeys.KeyParamOperatorId]);
                        vids.Add(param[UploadCoreOrBinFileKeys.KeyChipQty]);
                        vids.Add(param[UploadCoreOrBinFileKeys.KeyPMSFileName]);
                        vids.Add(param[UploadCoreOrBinFileKeys.KeyPMSFileBody]);

                        double.TryParse(param[UploadCoreOrBinFileKeys.KeyWaferAngle], out double angle);
                        int.TryParse(param[UploadCoreOrBinFileKeys.KeyCountRow], out int row);
                        int.TryParse(param[UploadCoreOrBinFileKeys.KeyCountCol], out int col);
                        int.TryParse(param[UploadCoreOrBinFileKeys.KeyReferenceX], out int refX);
                        int.TryParse(param[UploadCoreOrBinFileKeys.KeyReferenceY], out int refY);
                        int.TryParse(param[UploadCoreOrBinFileKeys.KeyStartingPosX], out int startingX);
                        int.TryParse(param[UploadCoreOrBinFileKeys.KeyStartingPosY], out int startingY);
                        //bool.TryParse(param[UploadCoreOrBinFileKeys.KeyUseEventHandling], out bool useEventHandling);
                        bool useEventHandling = true;
                        int.TryParse(param[UploadCoreOrBinFileKeys.KeyChipQty], out int chipQty);
                        var waferMapData = new WaferMapData
                        {
                            WaferId = param[UploadCoreOrBinFileKeys.KeySubstrateName],
                            Angle = angle,
                            CountOfRow = row,
                            CountOfCol = col,
                            IndexOfRefX = refX,
                            IndexOfRefY = refY,
                            IndexOfStartingX = startingX,
                            IndexOfStartingY = startingY,
                            CountOfProcessDies = chipQty,
                            MapData = param[UploadCoreOrBinFileKeys.KeyMapData]
                        };

                        ScenarioList[scenario].UpdateParamValues(new ScenarioUploadBinDataParamValues(vids, useEventHandling, param[UploadCoreOrBinFileKeys.KeyPMSFileBody], waferMapData));
                    }
                    break;

                case ScenarioListTypes.SCENARIO_REQ_CORE_WAFER_SPLIT:
                    {
                        ScenarioList[scenario].UpdateParamValues(new ScenarioReqWaferSplitFromLotParamValues(param.Values.ToList()));
                    }
                    break;
                case ScenarioListTypes.SCENARIO_REQ_CORE_CHIP_SPLIT_FIRST:
                case ScenarioListTypes.SCENARIO_REQ_CORE_CHIP_SPLIT:
                case ScenarioListTypes.SCENARIO_REQ_CORE_CHIP_FULL_SPLIT_FIRST:
                case ScenarioListTypes.SCENARIO_REQ_CORE_CHIP_FULL_SPLIT:
                    {
                        ScenarioList[scenario].UpdateParamValues(new ScenarioReqChipSplitParamValues(param.Values.ToList()));
                    }
                    break;
                case ScenarioListTypes.SCENARIO_REQ_BIN_WAFER_ID_ASSIGN:
                    {
                        ScenarioList[scenario].UpdateParamValues(new ScenarioReqWaferIdAssignParamValues(param.Values.ToList()));
                    }
                    break;
                case ScenarioListTypes.SCENARIO_REQ_UPLOAD_BINFILE:
                    {
                        ScenarioList[scenario].UpdateParamValues(new ScenarioReqUploadBinFileParamValues(
                            NameOfClient,
                            MessagesToSend.RequestUploadBinFile.ToString(),
                            param));
                    }
                    break;
                case ScenarioListTypes.SCENARIO_ASSIGN_SUBSTRATE_ID:
                    {
                        ScenarioList[scenario].UpdateParamValues(new ClientToClientCommunicationParamValues(
                            NameOfClient,
                            MessagesToSend.RequestAssignSubstrateId.ToString(),
                            param));
                    }
                    break;

                //////////////////////////////////////////////////
                default:
                    return false;
            }

            return true;
        }
        public override bool IsScenarioEnabled(Enum scenario)
        {
            return (false == (GetInstanceScenario(scenario) == null));
        }
        #endregion /config

        #region Delegate Functions
        public override bool RemoteCommandReceived(string rcmdName, string[] cpNames, string[] cpValues, ref long[] results)
        {
            // XEIC를 사용하는 경우에는 마스터 장비만 RCMD를 받도록 설정한다.
            // 마스터 장비의 RCMD 콜백 함수에서 받은 메시시 중 사전에 정의된 메시지 형식으로 (StrName을 SIGNAL 메시지 이름으로, CPVal을 메시지로) 변환한 뒤, 리턴시킨다.
            // SDK 내에서는 리턴된 메시지의 참조 인자를 통해 시그널 메시지를 브로드캐스팅한 뒤, 결과를 기다린다.
            // 즉 마스터가 아닌 클라이언트의 경우 RemoteCommand를 구현할 필요는 없고, RecvSignal만 구현하면 된다.
            WriteLog("Received RCMD : " + rcmdName);

            RemoteCommandTypes rcmd;
            if (false == Enum.TryParse(rcmdName, out rcmd))
            {
                WriteLog("> unknown rcmd name");
                return false;
            }
            if (false == GetControlState().Equals(EN_CONTROL_STATE.REMOTE))
            {
                rcmd = RemoteCommandTypes.NEXT_WORK_REQ;
            }
            bool permission = false;
            switch (rcmd)
            {
                case RemoteCommandTypes.STOP:
                case RemoteCommandTypes.STOP_WORK_REQ:
                    {
                        permission = false;
                        Task.TaskOperator.GetInstance().SetOperation(RunningMain_.OPERATION_EQUIPMENT.STOP);
                    }
                    break;
                case RemoteCommandTypes.NEXT_WORK_REQ:
                    {
                        permission = true;
                    }
                    break;
                default:
                    break;
            }

            if (rcmd.Equals(RemoteCommandTypes.STOP))
            {
                Task.TaskOperator.GetInstance().RemoteCommandError = true;
                SendClientToClientMessage(NameOfClient, MessagesToSend.RequestStop.ToString(),
                             string.Empty, string.Empty,
                             new string[] { }, new string[] { },
                             EN_MESSAGE_RESULT.OK, false);

                return true;
            }

            int index = -1;
            for (int i = 0; i < cpNames.Length; ++i)
            {
                if (cpNames[i].Equals("CUR_STEP_CEID"))
                {
                    index = i;
                    break;
                }
            }

            if (index < 0)
            {
                for (int i = 0; i < results.Length; ++i)
                {
                    results[i] = (int)EN_CPACK_TYPE.UNKNOWN_CPNAME;
                }

                return false;
            }
            else
            {
                for (int i = 0; i < results.Length; ++i)
                {
                    results[i] = (int)EN_CPACK_TYPE.OK;
                }

                if (false == int.TryParse(cpValues[index], out int ceid))
                {
                    for (int i = 0; i < results.Length; ++i)
                    {
                        results[i] = (int)EN_CPACK_TYPE.ILLEGAL_FORMAT_FOR_CPVAL;
                    }

                    return false;
                }

                foreach (var item in ScenarioList)
                {
                    if (item.Value is ScenarioReqLotInfo)
                    {
                        var lotInfoScenario = item.Value as ScenarioReqLotInfo;
                        if (lotInfoScenario.EventId.Equals(ceid))
                        {
                            item.Value.UpdatePermission(permission);
                            return true;
                        }
                    }
                    //else if (item.Value is ScenarioReqLotMergeAndChange)
                    //{
                    //    var lotInfoScenario = item.Value as ScenarioReqLotMergeAndChange;
                    //    if (lotInfoScenario.EventIdMerge.Equals(ceid) ||
                    //        lotInfoScenario.EventIdChange.Equals(ceid))
                    //    {
                    //        item.Value.UpdatePermission(permission);
                    //        return true;
                    //    }
                    //}
                    else if (item.Value is ScenarioProceedWithCarrier)
                    {
                        var convertedScen = item.Value as ScenarioProceedWithCarrier;
                        if (convertedScen.EventId.Equals(ceid))
                        {
                            item.Value.UpdatePermission(permission);
                            return true;
                        }
                    }
                    else if (item.Value is ScenarioReqWaferSplitFromLot)
                    {
                        var convertedScen = item.Value as ScenarioReqWaferSplitFromLot;
                        if (convertedScen.EventId.Equals(ceid))
                        {
                            item.Value.UpdatePermission(permission);
                            return true;
                        }
                    }
                    else if (item.Value is ScenarioReqChipSplit)
                    {
                        var convertedScen = item.Value as ScenarioReqChipSplit;
                        if (convertedScen.EventId.Equals(ceid))
                        {
                            item.Value.UpdatePermission(permission);
                            return true;
                        }
                    }
                    else if (item.Value is ScenarioReqWorkStart)
                    {
                        var convertedScen = item.Value as ScenarioReqWorkStart;
                        if (convertedScen.EventId.Equals(ceid))
                        {
                            item.Value.UpdatePermission(permission);
                            return true;
                        }
                    }
                    else if (item.Value is ScenarioReqWorkEnd)
                    {
                        var convertedScen = item.Value as ScenarioReqWorkEnd;
                        if (convertedScen.EventId.Equals(ceid))
                        {
                            item.Value.UpdatePermission(permission);
                            return true;
                        }
                    }
                    else if (item.Value is ScenarioUploadBinData)
                    {
                        var convertedScen = item.Value as ScenarioUploadBinData;
                        if (convertedScen.EventId.Equals(ceid))
                        {
                            item.Value.UpdatePermission(permission);
                            return true;
                        }
                    }
                    else if (item.Value is ScenarioReqWaferIdAssign)
                    {
                        var convertedScen = item.Value as ScenarioReqWaferIdAssign;
                        if (convertedScen.EventId.Equals(ceid) ||
                            convertedScen.EventIdConfirmation.Equals(ceid))
                        {
                            item.Value.UpdatePermission(permission);
                            return true;
                        }
                    }
                    else if (item.Value is SendingEventScenario)
                    {
                        var eventScenario = item.Value as SendingEventScenario;
                        if (eventScenario.EventId.Equals(ceid))
                        {
                            item.Value.UpdatePermission(permission);
                            return true;
                        }
                    }
                    else if (item.Value is ScenarioSendEventThenHandlingSecsMessage)
                    {
                        var convertedScen = item.Value as ScenarioSendEventThenHandlingSecsMessage;
                        if (convertedScen.EventId.Equals(ceid))
                        {
                            item.Value.UpdatePermission(permission);
                            return true;
                        }
                    }
                    else if (item.Value is EFEM.CustomizedByProcessType.PWA500BIN.ScenarioRecipeHandlingRequest)
                    {
                        var eventScenario = item.Value as EFEM.CustomizedByProcessType.PWA500BIN.ScenarioRecipeHandlingRequest;
                        if (eventScenario.EventId.Equals(ceid))
                        {
                            item.Value.UpdatePermission(permission);
                            return true;
                        }
                    }
                    else if (item.Value is ScenarioReqLotMergeAndChange)
                    {
                        var convertedScen = item.Value as ScenarioReqLotMergeAndChange;
                        if (convertedScen.EventIdMerge.Equals(ceid) ||
                            convertedScen.EventIdChange.Equals(ceid))
                        {
                            item.Value.UpdatePermission(permission);
                            return true;
                        }
                    }
                    //else if (item.Value is ScenarioRecipeHandlingRequest)
                    //{
                    //    var eventScenario = item.Value as ScenarioRecipeHandlingRequest;
                    //    if (eventScenario.EventId.Equals(ceid))
                    //    {
                    //        item.Value.UpdatePermission(permission);
                    //        return true;
                    //    }
                    //}
                }
            }

            WriteLog("> success");

            return true;
        }
        public override bool ClientToClientMessageReceived(string deviceName, string messageName, string sendingType, string scenarioName, string[] contentNames, string[] messages, EN_MESSAGE_RESULT result, ref bool useLogging)
        {
            if (false == Enum.TryParse(messageName, out MessagesToReceive messageTypeToReceive))
                return false;

            bool requestOnlyMessage = false;
            requestOnlyMessage |= (messageTypeToReceive.Equals(MessagesToReceive.RequestUpdateEquipmentData));
            requestOnlyMessage |= (messageTypeToReceive.Equals(MessagesToReceive.RequestUpdateTraceData));
            requestOnlyMessage |= (messageTypeToReceive.Equals(MessagesToReceive.RequestUpdateEquipmentState));
            requestOnlyMessage |= (messageTypeToReceive.Equals(MessagesToReceive.RequestNotifyAlarmStatus));

            Dictionary<string, string> messagePairs = new Dictionary<string, string>();
            for (int i = 0; i < contentNames.Length; ++i)
            {
                messagePairs[contentNames[i]] = messages[i];
            }

            if (messageName.StartsWith("Response") || requestOnlyMessage)
            {
                sendingType = DefinesForClientToClientMessage.VALUE_MESSAGE_TYPE_ACK;
                return ParseMessages(deviceName, messageTypeToReceive, scenarioName, messagePairs, result, ref useLogging);
            }
            else
            {
                sendingType = DefinesForClientToClientMessage.VALUE_MESSAGE_TYPE_SEND;
                return ParseMessagesAndAck(deviceName, messageTypeToReceive, scenarioName, messagePairs, result, ref useLogging);
            }
        }
        public override bool SecsMessageReceived(UserDefinedSecsMessage receivedSecsMessage, ref UserDefinedSecsMessage secsMessageToSend)
        {
            foreach (var kvp in ScenarioList)
            {
                if (false == IsScenarioRunning(kvp.Key))
                    continue;

                if (kvp.Value.ReceiveStream == receivedSecsMessage.Stream
                    && receivedSecsMessage.Stream == 12)
                {
                    if (kvp.Value is ScenarioReqWorkStart)
                    {
                        var scenario = kvp.Value as ScenarioReqWorkStart;
                        if (receivedSecsMessage.Function == scenario.FunctionToReceivedWaferMapSetup ||
                            receivedSecsMessage.Function == scenario.FunctionToReceivedWaferMapData)
                        {
                            if (scenario.UpdateReceivedSecsMessage(receivedSecsMessage.Function,
                                receivedSecsMessage.ListItemFormat))
                                break;
                        }
                    }
                    else if (kvp.Value is ScenarioReqWorkEnd)
                    {
                        var scenario = kvp.Value as ScenarioReqWorkEnd;
                        if (receivedSecsMessage.Function == scenario.FunctionToReceivedWaferMapDataSetup ||
                            receivedSecsMessage.Function == scenario.FunctionToReceivedWaferMapTransmitInquire ||
                            receivedSecsMessage.Function == scenario.FunctionToReceivedWaferMapData)
                        {
                            if (scenario.UpdateReceivedSecsMessage(receivedSecsMessage.Function,
                                receivedSecsMessage.ListItemFormat))
                                break;
                        }
                    }
                    else if (kvp.Value is ScenarioUploadBinData)
                    {
                        var scenario = kvp.Value as ScenarioUploadBinData;
                        if (receivedSecsMessage.Function == scenario.FunctionToReceivedWaferMapDataSetup ||
                            receivedSecsMessage.Function == scenario.FunctionToReceivedWaferMapTransmitInquire ||
                            receivedSecsMessage.Function == scenario.FunctionToReceivedWaferMapData)
                        {
                            if (scenario.UpdateReceivedSecsMessage(receivedSecsMessage.Function,
                                receivedSecsMessage.ListItemFormat))
                                break;
                        }
                    }
                }
                else
                {
                    if (kvp.Value.ReceiveStream == receivedSecsMessage.Stream
                        && kvp.Value.ReceiveFunction == receivedSecsMessage.Function
                        && kvp.Value.Receiving)
                    {
                        if (kvp.Value is ScenarioSendEventThenHandlingSecsMessage)
                        {
                            if (kvp.Value.UpdateReceiveMessage(receivedSecsMessage.ListItemFormat))
                            {
                                var targetScenario = kvp.Value as ScenarioSendEventThenHandlingSecsMessage;
                                secsMessageToSend = new UserDefinedSecsMessage(targetScenario.SendStream,
                                    targetScenario.SendFunction);

                                secsMessageToSend.SetStructure(targetScenario.MessageFormatToSend);
                                return true;
                            }
                        }
                        else if (kvp.Value is ScenarioReqLotMergeAndChange)
                        {
                            if (kvp.Value.UpdateReceiveMessage(receivedSecsMessage.ListItemFormat))
                            {
                                var targetScenario = kvp.Value as ScenarioReqLotMergeAndChange;
                                secsMessageToSend = new UserDefinedSecsMessage(targetScenario.StreamToSend,
                                    targetScenario.FunctionToSend);

                                secsMessageToSend.SetStructure(targetScenario.MessageFormatToSend);
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
            //throw new NotImplementedException();
        }
        public override EN_PPGRANT CheckingRecipeControlGrant(string recipeName)
        {
            var state = EquipmentState_.EquipmentState.GetInstance().GetState();
            switch (state)
            {
                case EquipmentState_.EQUIPMENT_STATE.EXECUTING:
                case EquipmentState_.EQUIPMENT_STATE.SETUP:
                    return Task.TaskOperator.GetInstance().IsMachineWait()
                        ? EN_PPGRANT.OK
                        : EN_PPGRANT.BUSY;
                // 경우에 따라서 레시피 조작이 가능한지 여부 판단 후 코드 리턴
                //
                // return EN_PPGRANT.OK; or return EN_PPGRANT.BUSY;

                case EquipmentState_.EQUIPMENT_STATE.FINISHING:
                case EquipmentState_.EQUIPMENT_STATE.INITIALIZE:
                case EquipmentState_.EQUIPMENT_STATE.READY:
                    if (IsScenarioRunning(ScenarioListTypes.SCENARIO_REQ_RECIPE_DOWNLOAD))
                    {
                        UpdateScenarioPermission(ScenarioListTypes.SCENARIO_REQ_RECIPE_DOWNLOAD, false);
                    }

                    return EN_PPGRANT.BUSY;


                case EquipmentState_.EQUIPMENT_STATE.IDLE:
                case EquipmentState_.EQUIPMENT_STATE.PAUSE:
                    return EN_PPGRANT.OK;

                default:
                    if (IsScenarioRunning(ScenarioListTypes.SCENARIO_REQ_RECIPE_DOWNLOAD))
                    {
                        UpdateScenarioPermission(ScenarioListTypes.SCENARIO_REQ_RECIPE_DOWNLOAD, false);
                    }
                    return EN_PPGRANT.BUSY;
            }
        }
        protected override void OnTerminalMessageReceived(string message)
        {
            SendClientToClientMessage(NameOfClient, MessagesToSend.RequestCallOperator.ToString(),
                                         string.Empty, string.Empty,
                                         new string[] { "Message" }, new string[] { message },
                                         EN_MESSAGE_RESULT.OK, false);
        }
        #endregion /Delegate Functions

        #region Variable Get/Set
        public override void UpdateVariablesAll()
        {
            var baseStatusVariablesToUpdate = new Dictionary<long, string>
            {
                [14] = _scenarioManager.GetModelName(),
                [15] = GetSoftwareVersion()
            };
            UpdateVariable(baseStatusVariablesToUpdate.Keys.ToArray(), baseStatusVariablesToUpdate.Values.ToArray());

            var baseEquipmentConstantsToUpdate = new Dictionary<long, string>
            {
                [121] = "PROTEC"
            };
            UpdateEquipmentConstants(baseEquipmentConstantsToUpdate.Keys.ToArray(), baseEquipmentConstantsToUpdate.Values.ToArray());

            UpdateECVAll();
        }
        private void UpdateECVAll()
        {
            // COMMON
            //         List<long> listEcids = new List<long>();
            //         List<string> listValues = new List<string>();

            //         int index = 0;
            //         var paramRange = PARAM_RANGE.GetInstance();
            //         int indexOfItem;
            //         string value = string.Empty;
            //foreach (var en in Enum.GetValues(typeof(PARAM_COMMON)))
            //         {
            //             string parameter = en.ToString();
            //             string[] parameters = parameter.Split('_');

            //             if (false == int.TryParse(parameters[parameters.Length - 1], out indexOfItem))
            //             {
            //                 value = _recipe.GetValue(EN_RECIPE_TYPE.COMMON, en.ToString(),
            //                     0, EN_RECIPE_PARAM_TYPE.VALUE, String.Empty);

            //                 listEcids.Add(paramRange.ECID_COMMON_START + index);
            //                 listValues.Add(value);

            //                 ++index;
            //             }
            //             else
            //             {
            //                 for (int i = 0; i < indexOfItem; i++)
            //                 {
            //                     value = _recipe.GetValue(EN_RECIPE_TYPE.COMMON, en.ToString(),
            //                         0, EN_RECIPE_PARAM_TYPE.VALUE, String.Empty);

            //                     listEcids.Add(paramRange.ECID_COMMON_START + index);
            //                     listValues.Add(value);

            //                     ++index;
            //                 }
            //             }
            //         }
            //         foreach (PARAM_EQUIPMENT en in Enum.GetValues(typeof(PARAM_EQUIPMENT)))
            //         {
            //             string parameter = en.ToString();
            //             string[] parameters = parameter.Split('_');

            //             if (false == int.TryParse(parameters[parameters.Length - 1], out indexOfItem))
            //             {
            //                 value = _recipe.GetValue(EN_RECIPE_TYPE.EQUIPMENT, parameter,
            //                     0, EN_RECIPE_PARAM_TYPE.VALUE, String.Empty);

            //                 listEcids.Add(paramRange.ECID_EQUIP_START + index);
            //                 listValues.Add(value);

            //                 ++index;
            //             }
            //             else
            //             {
            //                 for (int i = 0; i < indexOfItem; i++)
            //                 {
            //                     value = _recipe.GetValue(EN_RECIPE_TYPE.EQUIPMENT, parameter,
            //                         i, EN_RECIPE_PARAM_TYPE.VALUE, String.Empty);

            //                     listEcids.Add(paramRange.ECID_EQUIP_START + index);
            //                     listValues.Add(value);

            //                     ++index;
            //                 }
            //             }
            //         }

            //         long[] ecids = listEcids.ToArray();
            //         string[] values = listValues.ToArray();

            //         UpdateEquipmentConstants(ecids, values);        
        }
        public override bool UpdateECVParameter(string strECVName, string strValue)
        {
            throw new NotImplementedException();
        }
        public override void EquipmentParameterChangeRequested(string[] ecNameList, string[] valueList)
        {
            // 공정설비에 변경 요청 메시지를 보낸다. -> 현재는 서버 to PM 업데이트 시나리오가 없음
            UpdateEquipmentConstants(ecNameList, valueList);
        }
        #endregion /Variable Get/Set

        #region state changed
        public override void ControlStateChanged(string state)
        {

        }
        public override void EquipmentstateChanged(string state)
        {

        }
        #endregion /state changed

        #region <Recipe Management>
        public override void RecipeFileIsDeleted(string[] deletedFileList)
        {
            // 레시피 파일 제거 후 이벤트 발생 필요 시 구현
        }

        #region <UnFormatted Recipe>
        public override void UploadingUnFormattedRecipeAckReceived(string recipeName, EN_ACK7 recipeUploadAck)
        {
            throw new NotImplementedException();
        }
        public override bool UploadingUnFormattedRecipeReceived(string recipeName, ref string recipeFullPath)
        {
            // recipeName = A or A.rcp;
            // recipeFullPath = D:\Work\Recipe\A.rcp;
            WriteLog("Upload recipe (unformatted)");

            // 1. 파일 있는지 체크, 있다면 복사
            if (false == CheckRecipeFiles(recipeName))
            {
                WriteLog("> Check failed");
                return false;
            }

            // 2. 파일 압축
            if (false == CompressFiles(_recipePath, recipeName, out recipeFullPath))
            {
                WriteLog("> Compression failed");
                return false;
            }

            //         string source = string.Format("{0}\\{1}{2}", _recipePath, recipeName, Define.DefineConstant.FileFormat.FILEFORMAT_RECIPE);
            //if (false == FunctionsETC.FileExistCheck(source))
            //	return false;

            //string destination = string.Format("{0}\\upload\\{1}{2}", _recipePath, recipeName, Define.DefineConstant.FileFormat.FILEFORMAT_RECIPE);
            //if (false == FunctionsETC.FileCopy(source, destination))
            //	return false;

            //recipeFullPath = destination;

            WriteLog("> Success");
            return true;
        }
        public override EN_ACK7 DownloadingUnFormattedRecipeReceived(string recipeName, string recipeFullPath)
        {
            // recipeName = A or A.rcp;
            // recipeFullPath = D:\Work\SecsGem\XWork\Recipe\A.rcp;
            WriteLog("Download recipe (unformatted)");

            try
            {
                string newFullPath = recipeFullPath;
                if (false == Path.HasExtension(recipeFullPath))
                {
                    newFullPath = string.Format("{0}.zip", recipeFullPath);

                    if (File.Exists(newFullPath))
                        File.Delete(newFullPath);

                    File.Move(recipeFullPath, newFullPath);
                }


                ExtractFile(newFullPath, _recipePath, recipeName);
            }
            catch (Exception ex)
            {
                throw;
            }
            // 1. 파일이 정상인지 검사
            // 2. 파일 압축 해제

            //if (false == FrameOfSystem3.Task.TaskOperator.GetInstance().IsMachineWait())
            //{
            //	WriteLog("> machine is running");
            //	return EN_ACK7.UNSUPPORTED;
            //}

            //if (false == FunctionsETC.FileExistCheck(recipeFullPath))
            //{
            //	WriteLog("> source file not found : " + recipeFullPath);
            //	return EN_ACK7.NOT_FOUND;
            //}

            //         // 3. 각 설비에 파일 복사
            //string destination = AddExtensionToFileName(string.Format("{0}\\{1}", _recipePath, recipeName));

            //if (false == FunctionsETC.FileCopy(recipeFullPath, destination))
            //{
            //	WriteLog("> file copy failed");
            //	return EN_ACK7.PERMISSION;
            //}
            string recipePath = _recipePath;
            string recipeId = recipeName;
            if (false == Path.GetExtension(recipeId).Equals(FileFormat.FILEFORMAT_RECIPE))
            {
                recipeId = string.Format("{0}{1}", recipeId, FileFormat.FILEFORMAT_RECIPE);
            }
            string errorMessage = string.Empty;
            if (false == Recipe.Recipe.GetInstance().LoadProcessRecipe(ref recipePath, ref recipeId, ref errorMessage))
            {
                return EN_ACK7.PERMISSION;
            }

            WriteLog("> Success");

            return EN_ACK7.OK;
        }
        #endregion </UnFormatted Recipe>

        #region <Formatted Recipe Control>
        public override bool UploadingFormattedRecipeReceived(string recipeName, out Dictionary<string, SemiObject[]> recipeBodies)
        {
            recipeBodies = new Dictionary<string, SemiObject[]>();
            return false;
        }
        public override bool DownloadingFormattedRecipeReceived(string recipeName, Dictionary<string, string[]> recipeBodies)
        {
            return false;
        }
        #endregion </Formatted Recipe Control>

        #endregion </Recipe Management>

        #region Alarm
        public override void ExecuteReportAlarm(int alarmId, EN_GEM_ALARM_STATE state)
        {
            alarmId += 1000000;

            base.ExecuteReportAlarm(alarmId, state);
        }
        #endregion /Alarm

        public override void Execute()
        {
            if (false == EquipmentState_.EquipmentState.GetInstance().GetState().Equals(EquipmentState_.EQUIPMENT_STATE.UNDEFINED) &&
                false == EquipmentState_.EquipmentState.GetInstance().GetState().Equals(EquipmentState_.EQUIPMENT_STATE.PAUSE))
            {
                if (TicksForTraceData.IsTickOver(true))
                {
                    UpdateTraceDataValuesForEFEM();
                    UpdateTraceDataValuesForPWA500BIN();

                    TicksForTraceData.SetTickCount(TraceDataInterval);
                }
            }

            _lotHistoryLog.ExecuteWriteAsync();

            ExecuteQueuedScenario();
            _scenarioManager.ExecuteScanrioToCarrierLoadAsync();
        }

        #region <Trace Data>
        protected override bool GetTraceDataValue(ref Dictionary<long, string> dataToUpdate)
        {
            if (EquipmentState_.EquipmentState.GetInstance().GetState() == EquipmentState_.EQUIPMENT_STATE.UNDEFINED ||
                EquipmentState_.EquipmentState.GetInstance().GetState() == EquipmentState_.EQUIPMENT_STATE.PAUSE)
                return false;

            foreach (var item in _variablesToUpdate)
            {
                dataToUpdate[item.Key] = item.Value;
            }

            return true;
        }
        #endregion </Trace Data>
        #endregion /interface

        #region method
        private void UpdateTraceDataFromPWA500BIN(ref Dictionary<string, string> dataToUpdate)
        {
            foreach (var item in dataToUpdate)
            {
                _traceDataForPWA500BIN[item.Key] = item.Value;
                
                if (_traceDataAtDetaching.ContainsKey(item.Key))
                {
                    _traceDataAtDetaching[item.Key] = item.Value;
                }
            }
        }
        private void UpdateTraceDataValuesForPWA500BIN()
        {
            foreach (var item in _traceDataForPWA500BIN)
            {
                if (_traceDataForPWA500BINByName.ContainsKey(item.Key))
                {
                    _variablesToUpdate[_traceDataForPWA500BINByName[item.Key]] = item.Value;
                }
            }
        }
        private void UpdateTraceDataValuesForEFEM()
        {
            foreach (var item in _analogInfo)
            {                
                if (_variablesToUpdate.ContainsKey(item.Key))
                {
                    _variablesToUpdate[item.Key] = _analogIO.ReadInputValue(item.Value).ToString();
                }
            }

            for (int i = 0; i < _ffuManager.Count && i < 3; ++i)
            {
                if (false == _ffuManager.GetInformation(i, ref _unitItem))
                    continue;

                //EN_SVID_LIST ffuVariableId = EN_SVID_LIST.EFEM_FFU_SPEED_1 + i;
                _variablesToUpdate[1708 + i] = _unitItem.CurrentSpeed.ToString();
            }
        }
        private bool CheckRecipeFiles(string recipeName)
        {
            try
            {
                if (false == Directory.Exists(_recipePath))
                    Directory.CreateDirectory(_recipePath);

                string recipeFullPath = string.Empty;
                if (false == HasRecipeFile(_recipePath, recipeName, out recipeFullPath))
                {
                    // 파일이 있는지 체크
                    string targetRecipeName = string.Empty;
                    string[] files = Directory.GetFiles(_recipePath);
                    for (int i = 0; i < files.Length; ++i)
                    {
                        string fileName = Path.GetFileNameWithoutExtension(files[i]);
                        if (recipeName.Equals(fileName))
                        {
                            targetRecipeName = fileName;
                            break;
                        }
                    }

                    // 파일이 없으면,
                    if (string.IsNullOrEmpty(targetRecipeName))
                    {
                        targetRecipeName = recipeName;

                        string path = string.Empty, currentRecipe = string.Empty;
                        _recipe.GetProcessFileInformation(ref path, ref currentRecipe);
                        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(recipeName);

                        string sourceFileName = Path.Combine(path, currentRecipe);
                        string destFileName = Path.Combine(path, "EFEM", "RMS", string.Format("{0}{1}", fileNameWithoutExtension, FileFormat.FILEFORMAT_RECIPE));

                        try
                        {
                            WriteLog(string.Format("Create a recipe file (Source : {0}, Destination", sourceFileName, destFileName));
                            File.Copy(sourceFileName, destFileName);
                            WriteLog("Create a recipe file has completed");
                        }
                        catch (Exception ex)
                        {
                            WriteLog(string.Format("Recipe File Copy has Failed => {0}, {1}", ex.Message, ex.StackTrace));
                            return false;
                        }

                        //WriteLog(string.Format("There is no recipe file : {0}", recipeName));
                        //return false;
                    }
                }
                CopyRecipeFileToBasePath(_recipePath, "EFEM", recipeName, recipeFullPath, false);

                //if (RecipePath == null || RecipePath.Count <= 0)
                //{
                //    WriteLog("Invalid client path");
                //    return false;
                //}

                //foreach (var item in RecipePath)
                {
                    //string recipePathForClient = item.Value;
                    // 사용자 이름과 비밀번호가 잘못되었다고 나옴
                    //string recipePathForClient = @"\\ADT02-500BIN\Recipe\RMS";
                    //if (false == HasRecipeFile(_recipePathForPWA500BIN/*recipePathForClient*/, recipeName, out recipeFullPath))
                    if (false == File.Exists(_recipePathToUploadForPM))
                    {
                        WriteLog(string.Format("There is no recipe file in client : {0}", _recipePathToUploadForPM));
                        return false;
                    }

                    //CopyRecipeFileToBasePath(_recipePath, NameOfPM, recipeName, _recipePathToUploadForPM, true);
                }

                #region MyRegion
                //recipeFullPath = string.Empty;

                ////string[] files = Directory.GetFiles(path);

                //string sourceFileName = recipeName;
                //if (recipeName.Contains(FileFormat.FILEFORMAT_RECIPE))
                //{
                //    sourceFileName = Path.GetFileNameWithoutExtension(recipeName);
                //}

                //for (int i = 0; i < 1; ++i)
                //{
                //    string fileName = string.Format(@"{0}\{1}{2}", RecipePathForPM, recipeName, FileFormat.FILEFORMAT_RECIPE);
                //    //if (fileName.Contains(FileFormat.FILEFORMAT_RECIPE))
                //    //{
                //    //    fileName = Path.GetFileNameWithoutExtension(fileName);
                //    //}
                //    //if (fileName.Equals(sourceFileName))
                //    {
                //        recipeFullPath = fileName;
                //        //return true;
                //    }
                //}

                ////return false;

                ////foreach (var item in RecipePath)
                //{
                //    //string recipePathForClient = item.Value;
                //    //if (false == HasRecipeFile(recipePathForClient, recipeName, out recipeFullPath))
                //    //{
                //    //    WriteLog(string.Format("There is no recipe file in client({0}) : {1}", item.Key, recipeName));
                //    //    return false;
                //    //}
                //    string temp = RecipePath.Keys.ToArray()[0];
                //    //CopyRecipeFileToBasePath(_recipePath, temp, recipeName, recipeFullPath);
                //}
                #endregion

                return true;
            }
            catch (Exception ex)
            {
                WriteLog(string.Format("{0} -> {1}", ex.Message, ex.StackTrace));
                return false;
            }
        }
        private bool ExtractFile(string targetFileFullPath, string basePath, string recipeName)
        {
            try
            {
                string outputPath = string.Format(@"{0}\Download\{1}", basePath, recipeName);
                if (Directory.Exists(outputPath))
                {
                    string[] filesToDelete = Directory.GetFiles(outputPath);
                    for (int i = 0; i < filesToDelete.Length; ++i)
                    {
                        File.Delete(filesToDelete[i]);
                    }
                }

                System.IO.Compression.ZipFile.ExtractToDirectory(targetFileFullPath, outputPath);

                string[] filesToMove = Directory.GetFiles(outputPath);
                for (int i = 0; i < filesToMove.Length; ++i)
                {
                    string targetPath = string.Empty;

                    string onlyFileName = Path.GetFileName(filesToMove[i]);
                    switch (onlyFileName)
                    {
                        case "EFEM.rcp":
                            targetPath = _recipePath;
                            break;

                        default:
                            //foreach (var item in RecipePath)
                            {
                                string fileName = string.Format("{0}.rcp", NameOfPM);
                                if (onlyFileName.Equals(fileName))
                                {
                                    //string fullPath = string.Format(@"\\192.168.100.150\EFEM\RMS\Download\{0}\{1}{2}", NameOfPM, recipeName, FileFormat.FILEFORMAT_RECIPE);

                                    // TODO : 임시                                   
                                    //targetPath = @"\\192.168.100.150\EFEM\RMS\Download";// outputPath.Replace("127.0.0.1", "192.168.100.150");                                  
                                    targetPath = string.Format(@"{0}\Download\{1}", _recipePath, recipeName);
                                    if (false == Directory.Exists(targetPath))
                                        Directory.CreateDirectory(targetPath);
                                    //targetPath = string.Format(@"{0}\Download", _recipePathForPWA500BIN);
                                }
                            }
                            break;
                    }

                    string targetFullPath = string.Format(@"{0}\{1}", targetPath, recipeName);
                    if (false == Path.HasExtension(targetFullPath))
                    {
                        targetFullPath = string.Format(@"{0}{1}", targetFullPath, FileFormat.FILEFORMAT_RECIPE);
                    }

                    if (File.Exists(targetFullPath))
                        File.Delete(targetFullPath);

                    File.Move(filesToMove[i], targetFullPath);
                }

                return true;
            }
            catch (Exception ex)
            {
                WriteLog(string.Format("{0} -> {1}", ex.Message, ex.StackTrace));

                return false;
            }

        }
        private bool CompressFiles(string basePath, string recipeName, out string fullPathToUpload)
        {
            try
            {
                string outputPath = string.Format(@"{0}\Upload", basePath);
                string pathToCompress = string.Format(@"{0}\{1}", outputPath, recipeName);
                string outputFile = string.Format(@"{0}\{1}.zip", outputPath, recipeName);
                if (File.Exists(outputFile))
                    File.Delete(outputFile);

                System.IO.Compression.ZipFile.CreateFromDirectory(pathToCompress, outputFile);

                string[] files = Directory.GetFiles(pathToCompress);
                for (int i = 0; i < files.Length; ++i)
                {
                    File.Delete(files[i]);
                }

                Directory.Delete(pathToCompress);

                fullPathToUpload = string.Format(@"{0}\{1}", outputPath, recipeName);
                if (File.Exists(fullPathToUpload))
                    File.Delete(fullPathToUpload);

                File.Move(outputFile, fullPathToUpload);

                return true;
            }
            catch (Exception ex)
            {
                WriteLog(string.Format("{0} -> {1}", ex.Message, ex.StackTrace));
                fullPathToUpload = string.Empty;
                return false;
            }

        }
        private bool HasRecipeFile(string path, string recipeName, out string recipeFullPath)
        {
            recipeFullPath = string.Empty;

            string[] files = Directory.GetFiles(path);

            string sourceFileName = recipeName;
            if (recipeName.Contains(FileFormat.FILEFORMAT_RECIPE))
            {
                sourceFileName = Path.GetFileNameWithoutExtension(recipeName);
            }

            for (int i = 0; i < files.Length; ++i)
            {
                string fileName = files[i];
                if (fileName.Contains(FileFormat.FILEFORMAT_RECIPE))
                {
                    fileName = Path.GetFileNameWithoutExtension(files[i]);
                }
                if (fileName.Equals(sourceFileName))
                {
                    recipeFullPath = files[i];
                    return true;
                }
            }

            return false;
        }
        private void CopyRecipeFileToBasePath(string basePath, string nameOfClent, string recipeName, string fullPathForTargetFile, bool moveFiles)
        {
            string pathToCopy = string.Format(@"{0}\Upload\{1}\{2}{3}", basePath, recipeName, nameOfClent, FileFormat.FILEFORMAT_RECIPE);
            string pathName = Path.GetDirectoryName(pathToCopy);

            if (File.Exists(pathToCopy))
                File.Delete(pathToCopy);

            if (File.Exists(pathName))
                File.Delete(pathName);

            if (false == Directory.Exists(pathName))
                Directory.CreateDirectory(pathName);

            if (false == moveFiles)
            {
                File.Copy(fullPathForTargetFile, pathToCopy, true);
            }
            else
            {
                File.Move(fullPathForTargetFile, pathToCopy);
            }
        }

        private string GetSoftwareVersion()
        {
            var fv = System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var assemblyVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            var fullVersion = string.Format("{0}.{1}.{2}", fv.FileVersion.ToString(), assemblyVersion.Build.ToString(), assemblyVersion.Revision.ToString());
            return fullVersion;
        }
        private string GetRecipeId()
        {
            return _recipe.GetValue(EN_RECIPE_TYPE.COMMON, PARAM_COMMON.PROCESS_FILE_NAME.ToString(), "NONE");
        }

        private bool CheckSendData(Dictionary<string, string> data, params string[] keys)
        {
            foreach (string key in keys)
            {
                if (false == data.ContainsKey(key))
                    return false;
            }
            return true;
        }
        private bool GetRecipeFileList(out List<string> result)
        {
            result = new List<string>();

            System.IO.DirectoryInfo dInfo = new System.IO.DirectoryInfo(_recipePath);
            try
            {
                foreach (var fInfo in dInfo.GetFiles())
                {
                    if (fInfo.Extension.ToLower().Equals(Define.DefineConstant.FileFormat.FILEFORMAT_RECIPE))
                    {
                        result.Add(System.IO.Path.GetFileNameWithoutExtension(fInfo.Name));
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }

            return true;
        }
        private bool LoadPpid(string receivedPpid)
        {
            string ppid = AddExtensionToFileName(receivedPpid);
            WriteLog(string.Format("> target file {0}\\{1}", _recipePath, ppid));

            if (false == FunctionsETC.FileExistCheck(_recipePath, ppid))
            {
                WriteLog("> file not found");
                return false;
            }

            string path = _recipePath;
            string strErrorMsg = string.Empty;
            if (false == _recipe.LoadProcessRecipe(ref path, ref ppid, ref strErrorMsg))
            {
                WriteLog(string.Format("> recipe load fail : {0}" + strErrorMsg));
                return false;
            }
            return true;
        }

        private void ExecuteQueuedScenario()
        {
            if (_executingScenarioInfo != null)
            {
                var result = ExecuteScenario(_executingScenarioInfo.Scenario);
                switch (result)
                {
                    case EN_SCENARIO_RESULT.PROCEED:
                        return;

                    case EN_SCENARIO_RESULT.COMPLETED:
                    case EN_SCENARIO_RESULT.ERROR:
                    case EN_SCENARIO_RESULT.TIMEOUT_ERROR:
                        {
                            var messageResult = EN_MESSAGE_RESULT.NG;
                            if (result.Equals(EN_SCENARIO_RESULT.COMPLETED))
                            {
                                messageResult = EN_MESSAGE_RESULT.OK;
                            }

                            if (_executingScenarioInfo.Scenario.GetType() == typeof(ScenarioListTypes))
                            {
                                ScenarioListTypes typeOfScenario = (ScenarioListTypes)_executingScenarioInfo.Scenario;
                                var resultOfScenario = GetScenarioResultData(typeOfScenario);

                                // 검증 필요
                                _scenarioManager.ExecuteAfterScenarioCompletion(typeOfScenario,
                                        _executingScenarioInfo.ScenarioParams,
                                        resultOfScenario,
                                        _executingScenarioInfo.AdditionalParams,
                                        messageResult,
                                        false);

                                //ExecuteAfterScenarioCompletion(typeOfScenario, _executingScenarioInfo.ScenarioParams, resultOfScenario, _executingScenarioInfo.AdditionalParams, messageResult);
                            }
                            _executingScenarioInfo = null;
                        }
                        break;
                    default:
                        break;
                }
            }
            else
            {
                if (QueuedScenario.Count <= 0)
                    return;

                if (QueuedScenario.TryPeek(out QueuedScenarioInfo _temporaryScenario))
                {
                    if (IsScenarioRunning(_temporaryScenario.Scenario))
                    {
                        QueuedScenario.TryDequeue(out _temporaryScenario);
                        QueuedScenario.Enqueue(_temporaryScenario);
                        return;
                    }

                    QueuedScenario.TryDequeue(out _executingScenarioInfo);

                    // 파라메터 갱신
                    Enum scenario = _executingScenarioInfo.Scenario;
                    var scenarioParams = _executingScenarioInfo.ScenarioParams;
                    UpdateScenarioParams(scenario.ToString(), scenarioParams);
                }
            }
        }
        private void EnqueueScenarioAsync(Enum scenario, Dictionary<string, string> scenarioParams, Dictionary<string, string> additionaiParams = null)
        {
            Dictionary<string, string> params2;
            if (additionaiParams == null)
            {
                params2 = new Dictionary<string, string>();
            }
            else
            {
                params2 = new Dictionary<string, string>(additionaiParams);
            }

            if (scenarioParams == null)
            {
                scenarioParams = new Dictionary<string, string>();
            }

            QueuedScenarioInfo scenarioInfo = new QueuedScenarioInfo
            {
                Scenario = scenario,
                ScenarioParams = new Dictionary<string, string>(scenarioParams),
                AdditionalParams = params2
            };

            QueuedScenario.Enqueue(scenarioInfo);
        }
        private bool ParseMessagesAndAck(string nameOfEq, MessagesToReceive messageName, string scenarioName, Dictionary<string, string> messagePairs, EN_MESSAGE_RESULT result, ref bool useLogging)
        {
            switch (messageName)
            {
                case MessagesToReceive.RequestAssignRingId:
                    {
                        #region
                        if (false == messagePairs.TryGetValue(AssignRingIdKeys.KeyOldRingId, out string oldRingId))
                            return false;
                        if (false == messagePairs.TryGetValue(AssignRingIdKeys.KeyNewRingId, out string newRingId))
                            return false;

                        return ExecuteScenarioToAssignSubstrateRingId(nameOfEq, oldRingId, newRingId);
                        #endregion
                    }

                case MessagesToReceive.RequestDownloadMapFile:
                    {
                        #region
                        if (false == messagePairs.TryGetValue(RequestDownloadMapFileKeys.KeySubstrateName, out string substrateName))
                            return false;

                        if (false == messagePairs.TryGetValue(/*"RingId"*/RequestDownloadMapFileKeys.KeyRingId, out string ringId))
                            return false;

                        if (Task.TaskOperator.GetInstance().IsSimulationMode())
                        {
                            return ExecuteScenarioToDownloadMapFile(nameOfEq, substrateName, ringId, 0, " ", "AUTO", true);
                        }

                        if (false == messagePairs.TryGetValue(RequestDownloadMapFileKeys.KeyWaferAngle, out string angle))
                            return false;
                        if (false == double.TryParse(angle, out double waferAngle))
                            return false;
                        if (false == messagePairs.TryGetValue(RequestDownloadMapFileKeys.KeyUserId, out string userId))
                            return false;
                        if (false == messagePairs.TryGetValue(RequestDownloadMapFileKeys.KeyNullBinCode, out string nullBinCode))
                            return false;

                        // 이름의 유효성을 체크한다.
                        if (false == _substrateManager.IsValidSubstrateName(substrateName))
                            return false;

                        bool useEventHandling = !UseCoreMapHandlingOnly;
                        return ExecuteScenarioToDownloadMapFile(nameOfEq, substrateName, ringId, waferAngle, nullBinCode, userId, useEventHandling);
                        #endregion
                    }

                case MessagesToReceive.RequestUploadRecipe:
                    {
                        #region
                        if (false == messagePairs.TryGetValue(RecipeHandlingKeys.KeyRecipeId, out string recipeId))
                            return false;

                        // 파일이 있는지 체크
                        string targetRecipeName = string.Empty;
                        string[] files = Directory.GetFiles(_recipePath);
                        for (int i = 0; i < files.Length; ++i)
                        {
                            string fileName = Path.GetFileNameWithoutExtension(files[i]);
                            if (recipeId.Equals(fileName))
                            {
                                targetRecipeName = fileName;
                                break;
                            }
                        }

                        // 파일이 없으면,
                        if (string.IsNullOrEmpty(targetRecipeName))
                        {
                            targetRecipeName = recipeId;

                            string path = string.Empty, currentRecipe = string.Empty;
                            _recipe.GetProcessFileInformation(ref path, ref currentRecipe);
                            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(recipeId);

                            string sourceFileName = Path.Combine(path, currentRecipe);
                            string destFileName = Path.Combine(path, string.Format("{0}{1}", fileNameWithoutExtension, FileFormat.FILEFORMAT_RECIPE));

                            try
                            {
                                WriteLog(string.Format("Create a recipe file (Source : {0}, Destination", sourceFileName, destFileName));
                                File.Copy(sourceFileName, destFileName);
                                WriteLog("Create a recipe file has completed");
                            }
                            catch (Exception ex)
                            {
                                WriteLog(string.Format("Recipe File Copy has Failed => {0}, {1}", ex.Message, ex.StackTrace));
                                return false;
                            }
                        }
                        #endregion

                        #region <업로드 시나리오 비동기 실행>
                        var scenario = ScenarioListTypes.SCENARIO_REQ_RECIPE_UPLOAD;
                        var paramList = GetScenarioParameterList(scenario);
                        if (paramList == null)
                            return false;

                        Dictionary<string, string> paramsToUpdate = new Dictionary<string, string>();
                        for (int i = 0; i < paramList.Count; ++i)
                        {
                            string paramName = paramList[i];
                            string paramValue = string.Empty;
                            if (paramName.Equals(RecipeHandlingKeys.KeyParamRecipeId))
                            {
                                paramValue = targetRecipeName;
                            }
                            else if (paramName.Equals(RecipeHandlingKeys.KeyUseCommunicationToPM))
                            {
                                paramValue = bool.TrueString;
                            }

                            paramsToUpdate[paramName] = paramValue;
                        }

                        EnqueueScenarioAsync(scenario, paramsToUpdate);

                        return true;
                        #endregion </업로드 시나리오 비동기 실행>
                    }

                case MessagesToReceive.RequestStartDetaching:
                    {
                        #region 
                        string substrateName = string.Empty, ringId = string.Empty, recipeId = string.Empty, userId = string.Empty;
                        //if (false == Task.TaskOperator.GetInstance().IsSimulationMode())
                        {
                            if (false == messagePairs.TryGetValue(DetachingKeys.KeySubstarateName, out substrateName))
                                return false;

                            if (false == messagePairs.TryGetValue(DetachingKeys.KeyRingId, out ringId))
                                return false;

                            if (false == messagePairs.TryGetValue(DetachingKeys.KeyRecipeId, out recipeId))
                                return false;

                            if (false == messagePairs.TryGetValue(DetachingKeys.KeyUserId, out userId))
                                return false;
                        }
                        //else
                        //{
                        //    if (false == messagePairs.TryGetValue(DetachingKeys.KeySubstarateName, out substrateName))
                        //        return false;
                        //    ringId = substrateName;
                        //    recipeId = "TEST_RECIPE";
                        //    userId = "AUTO";
                        //}

                        Substrate substrate = new Substrate("");
                        if (false == FindSubstrateByNameOrRingId(substrateName, ringId, ref substrate))
                            return false;

                        int portId = substrate.GetSourcePortId();
                        int slot = substrate.GetSourceSlot();
                        //if (portId <= 0 || slot < 0)
                        //    return false;

                        //if (false == _carrierServer.HasCarrier(portId))
                        //    return false;

                        string carrierId = _carrierServer.GetCarrierId(portId);
                        _lotHistoryLog.WriteSubstrateHistoryForStartOrFinishDetaching(portId, carrierId, substrateName, true);

                        substrate.SetProcessingStatus(EFEM.Defines.MaterialTracking.ProcessingStates.InProcess);
                        if (UseCoreMapHandlingOnly)
                        {
                            ExecuteToSendSimpleResultToClient(EN_MESSAGE_RESULT.OK, MessagesToSend.ResponseStartDetaching.ToString(), nameOfEq);
                            return true;
                        }

                        string lotId = substrate.GetLotId();
                        string partId = substrate.GetAttribute(PWA500BINSubstrateAttributes.PartId);

                        #region <테스트용>
                        //string carrierId = string.Empty;
                        //int portId = 6;
                        //int slot = 1;
                        //string lotId = string.Empty;
                        //string partId = string.Empty;
                        #endregion </테스트용>

                        Dictionary<string, string> scenarioParam = new Dictionary<string, string>
                        {
                            [DetachingKeys.KeyParamCarrierId] = carrierId,
                            [DetachingKeys.KeyParamPortId] = _scenarioManager.GetPortName(portId),
                            [DetachingKeys.KeyParamLotId] = lotId,
                            [DetachingKeys.KeyParamPartId] = partId,
                            [DetachingKeys.KeyParamRecipeId] = _scenarioManager.GetRecipeId(),
                            [DetachingKeys.KeyParamWaferId] = substrateName,
                            // TODO : 슬롯을 1부터 매기도록 바꿔야하나?
                            [DetachingKeys.KeyParamSlotId] = (slot + 1).ToString(),
                            [DetachingKeys.KeyParamOperatorId] = userId
                        };

                        foreach (var item in _traceDataAtDetaching)
                        {
                            scenarioParam[item.Key] = item.Value;
                        }

                        return ExecuteSimpleScenarioAndSendClientMessage(ScenarioListTypes.SCENARIO_CORE_WAFER_DETACH_START, scenarioParam, nameOfEq, MessagesToSend.ResponseStartDetaching.ToString());
                        #endregion
                    }

                case MessagesToReceive.RequestFinishDetaching:
                    {
                        #region 
                        if (false == messagePairs.TryGetValue(DetachingKeys.KeySubstarateName, out string substrateName))
                            return false;

                        if (false == messagePairs.TryGetValue(DetachingKeys.KeyRingId, out string ringId))
                            return false;

                        if (false == messagePairs.TryGetValue(DetachingKeys.KeyRecipeId, out string recipeId))
                            return false;

                        if (false == messagePairs.TryGetValue(DetachingKeys.KeyUserId, out string userId))
                            return false;

                        Substrate substrate = new Substrate("");
                        if (false == FindSubstrateByNameOrRingId(substrateName, ringId, ref substrate))
                            return false;

                        int portId = substrate.GetSourcePortId();
                        int slot = substrate.GetSourceSlot();
                        //if (portId <= 0 || slot < 0)
                        //    return false;

                        //if (false == _carrierServer.HasCarrier(portId))
                        //    return false;

                        string carrierId = _carrierServer.GetCarrierId(portId);
                        _lotHistoryLog.WriteSubstrateHistoryForStartOrFinishDetaching(portId, carrierId, substrateName, false);

                        substrate.SetProcessingStatus(EFEM.Defines.MaterialTracking.ProcessingStates.Processed);
                        if (UseCoreMapHandlingOnly)
                        {
                            ExecuteToSendSimpleResultToClient(EN_MESSAGE_RESULT.OK, MessagesToSend.ResponseFinishDetaching.ToString(), nameOfEq);
                            return true;
                        }

                        string lotId = substrate.GetLotId();
                        string partId = substrate.GetAttribute(PWA500BINSubstrateAttributes.PartId);
                        Dictionary<string, string> scenarioParam = new Dictionary<string, string>
                        {
                            [DetachingKeys.KeyParamCarrierId] = carrierId,
                            [DetachingKeys.KeyParamPortId] = _scenarioManager.GetPortName(portId),
                            [DetachingKeys.KeyParamLotId] = lotId,
                            [DetachingKeys.KeyParamPartId] = partId,
                            [DetachingKeys.KeyParamRecipeId] = _scenarioManager.GetRecipeId(),
                            [DetachingKeys.KeyParamWaferId] = substrateName,
                            // TODO : 슬롯을 1부터 매기도록 바꿔야하나?
                            [DetachingKeys.KeyParamSlotId] = (slot + 1).ToString(),
                            [DetachingKeys.KeyParamOperatorId] = userId
                        };

                        return ExecuteSimpleScenarioAndSendClientMessage(ScenarioListTypes.SCENARIO_CORE_WAFER_DETACH_END, scenarioParam, nameOfEq, MessagesToSend.ResponseFinishDetaching.ToString());

                        //Dictionary<string, string> messageContentToSend = new Dictionary<string, string>
                        //{
                        //    [ResultKeys.KeyResult] = EN_MESSAGE_RESULT.OK.ToString(),
                        //    [ResultKeys.KeyDescription] = string.Empty,
                        //};

                        //return SendClientToClientMessage(nameOfEq, MessagesToSend.ResponseFinishDetaching.ToString(),
                        //            string.Empty, string.Empty,
                        //            messageContentToSend.Keys.ToArray(), messageContentToSend.Values.ToArray(),
                        //            result, true);
                        #endregion
                    }

                case MessagesToReceive.RequestStartSorting:
                    {
                        #region 
                        if (false == messagePairs.TryGetValue(SortingKeys.KeyRingId, out string ringId))
                            return false;

                        if (false == messagePairs.TryGetValue(SortingKeys.KeyRecipeId, out string recipeId))
                            return false;

                        if (false == messagePairs.TryGetValue(SortingKeys.KeySubstrateType, out string subType))
                            return false;
                        if (false == Enum.TryParse(subType, out SubstrateType substrateType))
                            return false;

                        if (false == messagePairs.TryGetValue(SortingKeys.KeyBinCode, out string binCode))
                            return false;

                        Substrate substrate = new Substrate("");
                        if (false == FindSubstrateByNameOrRingId(ringId, ringId, ref substrate))
                            return false;

                        // 2025.01.02. jhlim [DEL] 공테이프는 캐리어가 없을 수도 있다. -> 나간 시점
                        //int portId = substrate.GetSourcePortId();
                        //int slot = substrate.GetSourceSlot();
                        //if (portId <= 0 || slot < 0)
                        //    return false;

                        //if (false == _carrierServer.HasCarrier(portId))
                        //    return false;

                        int portId = substrate.GetSourcePortId();
                        _lotHistoryLog.WriteSubstrateHistoryForStartSorting(portId, ringId);

                        substrate.SetProcessingStatus(EFEM.Defines.MaterialTracking.ProcessingStates.InProcess);
                        if (UseCoreMapHandlingOnly)
                        {
                            ExecuteToSendSimpleResultToClient(EN_MESSAGE_RESULT.OK, MessagesToSend.ResponseStartSorting.ToString(), nameOfEq);
                            return true;
                        }

                        string lotId = substrate.GetLotId();
                        Dictionary<string, string> scenarioParam = new Dictionary<string, string>
                        {
                            [SortingKeys.KeyParamLotId] = lotId,
                            [SortingKeys.KeyParamBinType] = binCode,
                            [SortingKeys.KeyParamRingFrameId] = ringId,
                        };

                        ScenarioListTypes scenario;
                        switch (substrateType)
                        {
                            case SubstrateType.Bin1:
                                scenario = ScenarioListTypes.SCENARIO_BIN_SORTING_START_1;
                                break;
                            case SubstrateType.Bin2:
                                scenario = ScenarioListTypes.SCENARIO_BIN_SORTING_START_2;
                                break;
                            case SubstrateType.Bin3:
                                scenario = ScenarioListTypes.SCENARIO_BIN_SORTING_START_3;
                                break;
                            default:
                                return false;
                        }

                        substrate.SetAttribute(PWA500BINSubstrateAttributes.BinCode, binCode);
                        return ExecuteSimpleScenarioAndSendClientMessage(scenario, scenarioParam, nameOfEq, MessagesToSend.ResponseStartSorting.ToString());

                        //Dictionary<string, string> messageContentToSend = new Dictionary<string, string>
                        //{
                        //    [ResultKeys.KeyResult] = EN_MESSAGE_RESULT.OK.ToString(),
                        //    [ResultKeys.KeyDescription] = string.Empty,
                        //};

                        //return SendClientToClientMessage(nameOfEq, MessagesToSend.ResponseStartSorting.ToString(),
                        //            string.Empty, string.Empty,
                        //            messageContentToSend.Keys.ToArray(), messageContentToSend.Values.ToArray(),
                        //            result, true);
                        #endregion
                    }

                case MessagesToReceive.RequestFinishSorting:
                    {
                        #region 
                        if (false == messagePairs.TryGetValue(SortingKeys.KeyRingId, out string ringId))
                            return false;
                        if (false == messagePairs.TryGetValue(SortingKeys.KeyRecipeId, out string recipeId))
                            return false;
                        if (false == messagePairs.TryGetValue(SortingKeys.KeySubstrateType, out string subType))
                            return false;
                        if (false == Enum.TryParse(subType, out SubstrateType substrateType))
                            return false;
                        if (false == messagePairs.TryGetValue(SortingKeys.KeyBinCode, out string binCode))
                            return false;
                        if (false == messagePairs.TryGetValue(SortingKeys.KeyChipQty, out string qty))
                            return false;
                        if (false == int.TryParse(qty, out int chipQty))
                            return false;

                        Substrate substrate = new Substrate("");
                        if (false == FindSubstrateByNameOrRingId(ringId, ringId, ref substrate))
                            return false;

                        // 2025.01.02. jhlim [DEL] 공테이프는 캐리어가 없을 수도 있다. -> 나간 시점
                        //int slot = substrate.GetSourceSlot();
                        //if (portId <= 0 || slot < 0)
                        //    return false;

                        //if (false == _carrierServer.HasCarrier(portId))
                        //    return false;

                        int portId = substrate.GetSourcePortId();
                        string lotId = substrate.GetLotId();
                        string parentLotId = substrate.GetAttribute(PWA500BINSubstrateAttributes.ParentLotId);
                        _lotHistoryLog.WriteSubstrateHistoryForFinishSorting(portId, ringId, lotId, parentLotId);

                        substrate.SetProcessingStatus(EFEM.Defines.MaterialTracking.ProcessingStates.Processed);

                        if (UseCoreMapHandlingOnly)
                        {
                            ExecuteToSendSimpleResultToClient(EN_MESSAGE_RESULT.OK, MessagesToSend.ResponseFinishSorting.ToString(), nameOfEq);
                            return true;
                        }

                        Dictionary<string, string> scenarioParam = new Dictionary<string, string>
                        {
                            [SortingKeys.KeyParamLotId] = lotId,
                            [SortingKeys.KeyParamBinType] = binCode,
                            [SortingKeys.KeyParamRingFrameId] = ringId,
                            [SortingKeys.KeyParamChipQty] = qty,
                            // TODO :
                            [SortingKeys.KeyParamParentLotId] = parentLotId,
                        };

                        ScenarioListTypes scenario;
                        switch (substrateType)
                        {
                            case SubstrateType.Bin1:
                                scenario = ScenarioListTypes.SCENARIO_BIN_SORTING_END_1;
                                break;
                            case SubstrateType.Bin2:
                                scenario = ScenarioListTypes.SCENARIO_BIN_SORTING_END_2;
                                break;
                            case SubstrateType.Bin3:
                                scenario = ScenarioListTypes.SCENARIO_BIN_SORTING_END_3;
                                break;
                            default:
                                return false;
                        }

                        Dictionary<string, string> additionalParams = new Dictionary<string, string>
                        {
                            [AdditionalParamKeys.KeyNameOfEq] = nameOfEq,
                            [AdditionalParamKeys.KeyRingId] = ringId,
                            [AdditionalParamKeys.KeySubstrateType] = subType,
                            [AdditionalParamKeys.KeyChipQty] = qty
                        };

                        substrate.SetAttribute(PWA500BINSubstrateAttributes.SubstrateType, substrateType.ToString());
                        substrate.SetAttribute(PWA500BINSubstrateAttributes.ChipQty, qty.ToString());
                        substrate.SetAttribute(PWA500BINSubstrateAttributes.BinCode, binCode);
                        EnqueueScenarioAsync(scenario, scenarioParam, additionalParams);

                        return true;
                        #endregion
                    }

                case MessagesToReceive.RequestSplitCoreChip:
                    {
                        // 2024.08.18 : [START] 코어맵 핸들링만 사용하는 경우 이후 시나리오를 무시한다.
                        if (UseCoreMapHandlingOnly)
                        {
                            ExecuteToSendSimpleResultToClient(EN_MESSAGE_RESULT.OK, MessagesToSend.ResponseSplitCoreChip.ToString(), nameOfEq);
                            return true;
                        }
                        // [END]

                        #region 
                        if (false == messagePairs.TryGetValue(SplitCoreChipKeys.KeyCoreSubstrateName,
                            out string coreSubstrateName))
                            return false;
                        if (false == messagePairs.TryGetValue(/*"BinRingId"*/SplitCoreChipKeys.KeyBinRingId,
                            out string ringId))
                            return false;
                        if (false == messagePairs.TryGetValue(SplitCoreChipKeys.KeySubstrateType, out string subType))
                            return false;
                        if (false == Enum.TryParse(subType, out SubstrateType substrateType))
                            return false;
                        if (false == messagePairs.TryGetValue(SplitCoreChipKeys.KeyRecipeId,
                            out string recipeId))
                            return false;
                        if (false == messagePairs.TryGetValue(SplitCoreChipKeys.KeySplitQty, out string qty))
                            return false;
                        if (false == int.TryParse(qty, out int splitQty))
                            return false;

                        // 보내준 데이터에 잔여 수량이 있는 경우(없는 경우는 본설비 업데이트 이전) 잔여 수량이 0이면 Full Split 시나리오 실행,
                        // 데이터가 없거나, 잔여 수량이 0이 아니면(파싱 실패 포함) 기본 스플릿 시나리오 실행
                        bool isSplittedFully = false;
                        if (messagePairs.TryGetValue(SplitCoreChipKeys.KeyRemainingChips, out string remainingChipsString))
                        {
                            if (int.TryParse(remainingChipsString, out int remainingChips))
                            {
                                if (remainingChips <= 0)
                                {
                                    isSplittedFully = true;
                                }
                            }
                        }

                        if (false == messagePairs.TryGetValue(SplitCoreChipKeys.KeyIsFirstSorting, out string firstSortingFlag))
                            return false;
                        if (false == bool.TryParse(firstSortingFlag, out bool isFirstSorting))
                            return false;
                        if (false == messagePairs.TryGetValue(SplitCoreChipKeys.KeyUserId, out string userId))
                            return false;
                        if (false == messagePairs.TryGetValue(SplitCoreChipKeys.KeyBinCode, out string binCode))
                            return false;

                        return ExecuteScenarioToSplitChip(nameOfEq, isFirstSorting, isSplittedFully, coreSubstrateName, ringId, substrateType, splitQty, binCode, userId);
                        #endregion
                    }

                case MessagesToReceive.RequestUploadCoreFile:
                    {
                        #region
                        if (false == messagePairs.TryGetValue(UploadCoreOrBinFileKeys.KeySubstrateName, out string substrateName))
                            return false;
                        if (false == messagePairs.TryGetValue(UploadCoreOrBinFileKeys.KeyRingId, out string ringId))
                            return false;
                        if (false == messagePairs.TryGetValue(UploadCoreOrBinFileKeys.KeyRecipeId, out string recipeId))
                            return false;
                        //if (false == messagePairs.TryGetValue(UploadCoreOrBinFileKeys.KeyPMSBody, out string pmsBody))
                        //    return false;
                        if (false == messagePairs.TryGetValue(UploadCoreOrBinFileKeys.KeyChipQty, out string qty))
                            return false;
                        if (false == int.TryParse(qty, out int chipQty))
                            return false;
                        if (false == messagePairs.TryGetValue(UploadCoreOrBinFileKeys.KeyCountRow, out string row))
                            return false;
                        if (false == int.TryParse(row, out int countRow))
                            return false;
                        if (false == messagePairs.TryGetValue(UploadCoreOrBinFileKeys.KeyCountCol, out string col))
                            return false;
                        if (false == int.TryParse(col, out int countCol))
                            return false;
                        if (false == messagePairs.TryGetValue(UploadCoreOrBinFileKeys.KeyWaferAngle, out string angle))
                            return false;
                        if (false == double.TryParse(angle, out double waferAngle))
                            return false;
                        if (false == messagePairs.TryGetValue(UploadCoreOrBinFileKeys.KeyNullBinCode, out string nullBinCode))
                            return false;
                        if (false == messagePairs.TryGetValue(UploadCoreOrBinFileKeys.KeyMapData, out string mapData))
                            return false;
                        if (false == messagePairs.TryGetValue(UploadCoreOrBinFileKeys.KeyUserId, out string userId))
                            return false;

                        // 길이 비교
                        int count = countRow * countCol;
                        if (mapData.Length != count)
                        {
                            Dictionary<string, string> messageContentToSend = new Dictionary<string, string>
                            {
                                [ResultKeys.KeyResult] = EN_MESSAGE_RESULT.NG.ToString(),
                                [ResultKeys.KeyDescription] = string.Format("Invalid Length : Row:{0}, Col{1}, DataLength:{2}", countRow, countCol, mapData.Length)
                            };

                            return SendClientToClientMessage(nameOfEq, MessagesToSend.ResponseUploadCoreFile.ToString(),
                                        string.Empty, string.Empty,
                                        messageContentToSend.Keys.ToArray(), messageContentToSend.Values.ToArray(),
                                        result, true);
                        }
                        //

                        bool useEventHandling = !UseCoreMapHandlingOnly;
                        return ExecuteScenarioToWorkEnd(nameOfEq, substrateName, ringId, chipQty, waferAngle, countRow, countCol, nullBinCode, mapData, userId, true, useEventHandling);
                        #endregion

                    }

                case MessagesToReceive.RequestUploadScrapInfo:
                    {
                        // TODO : 코드 정해지면 그 때 검증하자
                        Dictionary<string, string> messageContentToSend = new Dictionary<string, string>
                        {
                            [ResultKeys.KeyResult] = EN_MESSAGE_RESULT.OK.ToString(),
                            [ResultKeys.KeyDescription] = string.Empty,
                        };

                        return SendClientToClientMessage(nameOfEq, MessagesToSend.ResponseFinishSorting.ToString(),
                                    string.Empty, string.Empty,
                                    messageContentToSend.Keys.ToArray(), messageContentToSend.Values.ToArray(),
                                    result, true);
                    }

                default:
                    return false;
            }

            //return false;
        }
        private bool ParseMessages(string nameOfEq, MessagesToReceive messageName, string scenarioName, Dictionary<string, string> messagePairs, EN_MESSAGE_RESULT result, ref bool useLogging)
        {
            //if (false == messagePairs.TryGetValue(ResultKeys.KeyResult, out string resultValue) ||
            //    false == messagePairs.TryGetValue(ResultKeys.KeyDescription, out string resultDescription))
            //    return false;

            switch (messageName)
            {
                case MessagesToReceive.RequestUpdateEquipmentData:
                    {
                        #region
                        //Dictionary<int, Dictionary<string, string>> ecidToUpdate = new Dictionary<int, Dictionary<string, string>>();

                        //int count = 0;
                        //int key = 0;
                        //ecidToUpdate[key] = new Dictionary<string, string>();
                        //foreach (var item in messagePairs)
                        //{
                        //    if (++count > 2)
                        //    {
                        //        count = 0;
                        //        ++key;
                        //        ecidToUpdate[key] = new Dictionary<string, string>();
                        //    }

                        //    if (EquipmentConstantList.TryGetValue(item.Key, out _))
                        //    {
                        //        ecidToUpdate[key][item.Key] = item.Value;
                        //    }
                        //}

                        //if (ecidToUpdate.Count > 0)
                        //{
                        //    foreach (var item in ecidToUpdate)
                        //    {
                        //        UpdateEquipmentConstants(item.Value.Keys.ToArray(), item.Value.Values.ToArray());
                        //    }
                        //    useLogging = false;
                        //}
                        //else
                        //{
                        //    useLogging = true;
                        //}

                        //return (ecidToUpdate.Count > 0);
                        _ecidToUpdate.Clear();
                        foreach (var item in messagePairs)
                        {
                            if (EquipmentConstantList.ContainsKey(item.Key))
                            {
                                if (false == EquipmentConstantList[item.Key].Value.Equals(item.Value))
                                {
                                    EquipmentConstantList[item.Key].Value = item.Value;
                                    _ecidToUpdate[item.Key] = item.Value;
                                }
                            }
                        }

                        useLogging = false;
                        if (_ecidToUpdate.Count > 0)
                        {
                            UpdateEquipmentConstants(_ecidToUpdate.Keys.ToArray(), _ecidToUpdate.Values.ToArray());
                        }

                        return true;
                        #endregion
                    }

                case MessagesToReceive.RequestUpdateTraceData:
                    {
                        useLogging = false;

                        #region
                        UpdateTraceDataFromPWA500BIN(ref messagePairs);
                        #endregion
                    }
                    break;

                //case MessagesToReceive.RequestUpdateMachineInfo:
                //    {
                //        #region
                //        bool hasTraceData = GetTraceDataById(ref _variablesToUpdate);
                //        foreach (var item in messagePairs)
                //        {
                //            // LotId
                //            if (item.Key.Equals(MachineInfoKeys.KeyLotId))
                //            {
                //                _processModuleGroup.SetLotId(ProcessModuleIndex, item.Value);
                //            }
                //            // RecipeId
                //            else if (item.Key.Equals(MachineInfoKeys.KeyRecipeId))
                //            {
                //                _processModuleGroup.SetRecipeId(ProcessModuleIndex, item.Value);
                //            }
                //            // Status
                //            //else if (item.Key.Equals(MachineInfoKeys.KeyEquipmentState))
                //            //{
                //            //    if (false == Enum.TryParse(item.Value, out EquipmentState_.EQUIPMENT_STATE equipmentStatus))
                //            //        return false;

                //            //    _processModuleGroup.SetEquipmentState(ProcessModuleIndex, equipmentStatus);
                //            //}
                //            else
                //            {
                //                if (false == long.TryParse(item.Key, out long id))
                //                    continue;

                //                if (_variablesToUpdate.ContainsKey(id))
                //                {
                //                    _variablesToUpdate[id] = item.Value;
                //                }
                //            }

                //            if (hasTraceData)
                //            {
                //                UpdateTraceDataById(_variablesToUpdate);
                //            }

                //        }                                          

                //        return true;
                //        #endregion
                //    }

                case MessagesToReceive.RequestUpdateEquipmentState:
                    {
                        #region
                        // Status
                        if (false == messagePairs.TryGetValue(MachineInfoKeys.KeyEquipmentState, out string status))
                            return false;
                        if (false == Enum.TryParse(status, out EquipmentState_.EQUIPMENT_STATE equipmentStatus))
                            return false;

                        var prevEquipmentStatus = _processModuleGroup.GetEquipmentState(ProcessModuleIndex);
                        switch (equipmentStatus)
                        {
                            case EquipmentState_.EQUIPMENT_STATE.IDLE:
                                {
                                    // Finishing to Idle, Executing(씹힌 경우) to Idle이면 정지 이벤트 발생
                                    if (prevEquipmentStatus.Equals(EquipmentState_.EQUIPMENT_STATE.FINISHING) ||
                                        prevEquipmentStatus.Equals(EquipmentState_.EQUIPMENT_STATE.EXECUTING))
                                    {
                                        var param = _scenarioManager.MakeParamToEquipmentStatus();
                                        EnqueueScenarioAsync(ScenarioListTypes.SCENARIO_EQUIPMENT_END, param, null);
                                    }
                                }
                                break;
                            case EquipmentState_.EQUIPMENT_STATE.EXECUTING:
                                {
                                    // Ready to Executing, Idle to Executing(씹힌 경우) 시작 이벤트 발생
                                    if (prevEquipmentStatus.Equals(EquipmentState_.EQUIPMENT_STATE.IDLE) ||
                                        prevEquipmentStatus.Equals(EquipmentState_.EQUIPMENT_STATE.READY))
                                    {
                                        var param = _scenarioManager.MakeParamToEquipmentStatus();
                                        EnqueueScenarioAsync(ScenarioListTypes.SCENARIO_EQUIPMENT_START, param, null);
                                    }
                                }
                                break;

                            default:
                                break;
                        }

                        _processModuleGroup.SetEquipmentState(ProcessModuleIndex, equipmentStatus);

                        // Recipe
                        if (false == messagePairs.TryGetValue(MachineInfoKeys.KeyRecipeId, out string recipeId))
                            return false;
                        _processModuleGroup.SetRecipeId(ProcessModuleIndex, recipeId);

                        useLogging = false;
                        return true;
                        #endregion
                    }

                case MessagesToReceive.RequestNotifyAlarmStatus:
                    {
                        #region
                        if (_queuedAlarmsFromPM.Count == 0)
                        {
                            var param = _scenarioManager.MakeParamToEquipmentStatus();
                            EnqueueScenarioAsync(ScenarioListTypes.SCENARIO_ERROR_START, param, null);
                        }

                        foreach (var item in messagePairs)
                        {
                            string alidString = item.Key;
                            string statusString = item.Value;

                            if (false == int.TryParse(alidString, out int alid))
                                continue;

                            if (false == Enum.TryParse(statusString, out EN_GEM_ALARM_STATE status))
                                continue;

                            int alarmId = alid + AlarmOffset;
                            base.ExecuteReportAlarm(alid + AlarmOffset, status);
                            
                            switch (status)
                            {
                                case EN_GEM_ALARM_STATE.CLEARED:
                                    _queuedAlarmsFromPM.Enqueue(alarmId);
                                    break;
                                case EN_GEM_ALARM_STATE.OCCURED:
                                    _queuedAlarmsFromPM.TryDequeue(out _);
                                    break;
                            }
                        }

                        if (_queuedAlarmsFromPM.Count == 0)
                        {
                            var param = _scenarioManager.MakeParamToEquipmentStatus();
                            EnqueueScenarioAsync(ScenarioListTypes.SCENARIO_ERROR_STOP, param, null);
                        }

                        // Id
                        //if (false == messagePairs.TryGetValue(NotifyAlarmKeys.KeyAlarmId, out string id))
                        //    return false;
                        //if (false == int.TryParse(id, out int alarmId))
                        //    return false;

                        //// Status
                        //if (false == messagePairs.TryGetValue(NotifyAlarmKeys.KeyStatus, out string status))
                        //    return false;
                        //if (false == int.TryParse(status, out int alarmStatus))
                        //    return false;
                        //if (false == Enum.IsDefined(typeof(EN_GEM_ALARM_STATE), alarmStatus))
                        //    return false;


                        return true;
                        #endregion
                    }

                case MessagesToReceive.ResponseDownloadRecipe:
                    {
                        bool scenarioPermission = true;
                        if (false == messagePairs.TryGetValue(ResultKeys.KeyResult, out string resultFromClient) ||
                            false == messagePairs.TryGetValue(ResultKeys.KeyDescription, out _))
                            scenarioPermission = false;

                        if (resultFromClient.Equals(EN_MESSAGE_RESULT.OK.ToString()))
                            scenarioPermission = true;
                        else
                            scenarioPermission = false;

                        if (IsScenarioRunning(ScenarioListTypes.SCENARIO_REQ_RECIPE_DOWNLOAD))
                        {
                            UpdateScenarioPermission(ScenarioListTypes.SCENARIO_REQ_RECIPE_DOWNLOAD, scenarioPermission);
                        }

                        return scenarioPermission;
                    }

                case MessagesToReceive.ResponseUploadRecipe:
                    {
                        _recipePathToUploadForPM = string.Empty;
                        messagePairs.TryGetValue(ResultKeys.KeyResult, out string resultMessage);
                        result = resultMessage.Equals(EN_MESSAGE_RESULT.OK.ToString()) ? EN_MESSAGE_RESULT.OK : EN_MESSAGE_RESULT.NG;

                        bool scenarioPermission = result.Equals(EN_MESSAGE_RESULT.OK) ? true : false;

                        if (false == messagePairs.TryGetValue(RecipeHandlingKeys.KeyRecipeId, out string recipeId))
                            scenarioPermission = false;

                        if (false == messagePairs.TryGetValue(RecipeHandlingKeys.KeyRecipeBody, out string recipeBody))
                            scenarioPermission = false;

                        if (scenarioPermission)
                        {
                            string pathToWrite = string.Format(@"{0}\Upload\{1}\{2}{3}", _recipePath, recipeId, NameOfPM, FileFormat.FILEFORMAT_RECIPE);
                            string pathName = Path.GetDirectoryName(pathToWrite);
                            if (File.Exists(pathName))
                                File.Delete(pathName);
                            if (false == Directory.Exists(pathName))
                                Directory.CreateDirectory(pathName);

                            using (StreamWriter sw = new StreamWriter(pathToWrite))
                            {
                                sw.Write(recipeBody);
                            }

                            _recipePathToUploadForPM = pathToWrite;// Path.GetDirectoryName(recipeFullPath);
                            //RecipePath[NameOfPM] = pathToUpload;
                        }

                        messagePairs.TryGetValue(ResultKeys.KeyDescription, out string description);
                        ExecuteToSendSimpleResultToClient(result, MessagesToReceive.RequestUploadRecipeResult.ToString(), nameOfEq, description);

                        if (IsScenarioRunning(ScenarioListTypes.SCENARIO_REQ_RECIPE_UPLOAD))
                        {
                            UpdateScenarioPermission(ScenarioListTypes.SCENARIO_REQ_RECIPE_UPLOAD, scenarioPermission);
                        }

                        return scenarioPermission;
                    }

                case MessagesToReceive.ResponseDeleteRecipe:
                    {
                        // TODO : RMS는 추후 구현
                    }
                    break;
                case MessagesToReceive.ResponseAssignSubstrateId:
                    {
                        bool scenarioPermission = true;
                        if (false == messagePairs.TryGetValue(ResultKeys.KeyResult, out string resultFromClient) ||
                            false == messagePairs.TryGetValue(ResultKeys.KeyDescription, out _))
                            scenarioPermission = false;

                        if (resultFromClient.Equals(EN_MESSAGE_RESULT.OK.ToString()))
                            scenarioPermission = true;
                        else
                            scenarioPermission = false;

                        if (IsScenarioRunning(ScenarioListTypes.SCENARIO_ASSIGN_SUBSTRATE_ID))
                        {
                            ScenarioList[ScenarioListTypes.SCENARIO_ASSIGN_SUBSTRATE_ID].UpdatePermission(scenarioPermission);
                            return true;
                        }

                        return false;
                    }

                case MessagesToReceive.ResponseAssignLotId:
                    {
                        return true;
                    }

                case MessagesToReceive.ResponseUploadBinFile:
                    {
                        #region
                        bool scenarioPermission = true;
                        if (false == messagePairs.TryGetValue(ResultKeys.KeyResult, out string resultFromClient) ||
                            false == messagePairs.TryGetValue(ResultKeys.KeyDescription, out _))
                            scenarioPermission = false;

                        if (resultFromClient.Equals(EN_MESSAGE_RESULT.OK.ToString()))
                            scenarioPermission = true;
                        else
                            scenarioPermission = false;

                        if (false == scenarioPermission)
                            return scenarioPermission;

                        if (false == messagePairs.TryGetValue(UploadCoreOrBinFileKeys.KeySubstrateName, out string substrateName))
                            return false;
                        if (false == messagePairs.TryGetValue(UploadCoreOrBinFileKeys.KeyRingId, out string ringId))
                            return false;
                        if (false == messagePairs.TryGetValue(UploadCoreOrBinFileKeys.KeyRecipeId, out string recipeId))
                            return false;
                        if (false == messagePairs.TryGetValue(UploadCoreOrBinFileKeys.KeySubstrateType, out string subType))
                            return false;
                        if (false == Enum.TryParse(subType, out SubstrateType substrateType))
                            return false;
                        if (false == messagePairs.TryGetValue(UploadCoreOrBinFileKeys.KeyChipQty, out string qty))
                            return false;
                        if (false == int.TryParse(qty, out int chipQty))
                            return false;
                        if (false == messagePairs.TryGetValue(UploadCoreOrBinFileKeys.KeyPMSBody, out string pmsBody))
                            return false;
                        if (false == messagePairs.TryGetValue(UploadCoreOrBinFileKeys.KeyCountRow, out string row))
                            return false;
                        if (false == int.TryParse(row, out int countRow))
                            return false;
                        if (false == messagePairs.TryGetValue(UploadCoreOrBinFileKeys.KeyCountCol, out string col))
                            return false;
                        if (false == int.TryParse(col, out int countCol))
                            return false;
                        if (false == messagePairs.TryGetValue(UploadCoreOrBinFileKeys.KeyWaferAngle, out string angle))
                            return false;
                        if (false == double.TryParse(angle, out double waferAngle))
                            return false;
                        if (false == messagePairs.TryGetValue(UploadCoreOrBinFileKeys.KeyNullBinCode, out string nullBinCode))
                            return false;
                        if (false == messagePairs.TryGetValue(UploadCoreOrBinFileKeys.KeyBinCode, out string binCode))
                            return false;
                        if (false == messagePairs.TryGetValue(UploadCoreOrBinFileKeys.KeyMapData, out string mapData))
                            return false;

                        _scenarioManager.MakeBinDataToUpload(nameOfEq, substrateName, ringId, chipQty, waferAngle, countRow, countCol, nullBinCode, mapData, pmsBody, "AUTO", true);


                        if (IsScenarioRunning(ScenarioListTypes.SCENARIO_REQ_UPLOAD_BINFILE))
                        {
                            UpdateScenarioPermission(ScenarioListTypes.SCENARIO_REQ_UPLOAD_BINFILE, scenarioPermission);
                        }

                        return true;
                        //return ExecuteScenarioToUploadBinData(nameOfEq, substrateName, ringId, chipQty, waferAngle, countRow, countCol, nullBinCode, mapData, pmsBody, "AUTO", true);
                        #endregion
                    }

                default:
                    return false;
            }
            return false;
        }
        // TODO : 이거는 전부 PM에서 찾으면 됨
        private bool FindSubstrateByNameOrRingId(string substrateName, string ringId, ref Substrate substrate)
        {
            if (_substrateManager.GetSubstrateByName(substrateName, ref substrate) ||
                _substrateManager.GetSubstrateByName(ringId, ref substrate))
                return true;

            return false;
        }
        private bool ExecuteToSendSimpleResultToClient(EN_MESSAGE_RESULT result, string messageNameToSend, string nameOfEq, string description = "")
        {
            if (messageNameToSend == null || string.IsNullOrEmpty(messageNameToSend))
                return true;

            Dictionary<string, string> messageContentToSend = new Dictionary<string, string>
            {
                [ResultKeys.KeyResult] = result.ToString(),
                [ResultKeys.KeyDescription] = description,
            };

            return SendClientToClientMessage(nameOfEq, messageNameToSend.ToString(),
                        string.Empty, string.Empty,
                        messageContentToSend.Keys.ToArray(),
                        messageContentToSend.Values.ToArray(),
                        result, true);
        }
        private bool ExecuteSimpleScenarioAndSendClientMessage(ScenarioListTypes scenario, Dictionary<string, string> scenarioParam, string nameOfEq, string messageNameToSend)
        {
            if (nameOfEq == null)
                nameOfEq = string.Empty;

            if (messageNameToSend == null)
                messageNameToSend = string.Empty;

            Dictionary<string, string> additionalParams = new Dictionary<string, string>
            {
                [AdditionalParamKeys.KeyNameOfEq] = nameOfEq,
                [AdditionalParamKeys.KeyMessageNameToSend] = messageNameToSend,
            };

            EnqueueScenarioAsync(scenario, scenarioParam, additionalParams);
            return true;
        }
        //private bool ExecuteScenarioToSplitWafer(string nameOfEq, string substrateId, string ringId, string userId, bool isLast)
        //{
        //    Substrate substrate = new Substrate("");
        //    if (false == _substrateManager.GetSubstrateByName(substrateId, ref substrate))
        //        return false;

        //    var scenarioParam = new Dictionary<string, string>
        //    {
        //        [AssignSubstrateLotIdKeys.KeyParamLotId] = substrate.GetLotId(),
        //        [AssignSubstrateLotIdKeys.KeyParamWaferId] = substrateId,
        //        [AssignSubstrateLotIdKeys.KeyParamPartId] = substrate.GetAttribute(PWA500BINSubstrateAttributes.PartId),
        //        [AssignSubstrateLotIdKeys.KeyParamRecipeId] = _scenarioManager.GetRecipeId(),
        //        // TODO : 슬롯을 1부터 매기도록 바꿔야하나?
        //        [AssignSubstrateLotIdKeys.KeyParamSlotId] = (substrate.GetSourceSlot() + 1).ToString(),
        //        [AssignSubstrateLotIdKeys.KeyParamOperatorId] = userId
        //    };

        //    Dictionary<string, string> additionalParams = new Dictionary<string, string>
        //    {
        //        [AdditionalParamKeys.KeyNameOfEq] = nameOfEq,
        //        [AdditionalParamKeys.KeySubstrateId] = substrateId,
        //        [AdditionalParamKeys.KeyRingId] = ringId
        //    };

        //    ScenarioListTypes scenario;
        //    if (false == isLast)
        //    {
        //        scenario = ScenarioListTypes.SCENARIO_REQ_CORE_WAFER_SPLIT;
        //    }
        //    else
        //    {
        //        scenario = ScenarioListTypes.SCENARIO_REQ_CORE_WAFER_SPLIT_LAST;
        //    }

        //    EnqueueScenarioAsync(scenario, scenarioParam, additionalParams);
        //    return true;
        //}
        //private bool ExecuteScenarioToAssignSubstrateId(string nameOfEq, string ringId, SubstrateType substrateType)
        //{
        //    Substrate binSubstrate = new Substrate("");
        //    if (_substrateManager.FindSubstrateByName(ringId, ref binSubstrate))
        //    {
        //        Dictionary<string, string> messageContentToSend = new Dictionary<string, string>
        //        {
        //            [ResultKeys.KeyResult] = EN_MESSAGE_RESULT.OK.ToString(),
        //            [ResultKeys.KeyDescription] = string.Empty,
        //        };

        //        if (false == SendClientToClientMessage(nameOfEq, MessagesToSend.ResponseFinishSorting.ToString(),
        //                    string.Empty, string.Empty,
        //                    messageContentToSend.Keys.ToArray(), messageContentToSend.Values.ToArray(),
        //                    EN_MESSAGE_RESULT.OK, true))
        //            return false;

        //        Dictionary<string, string> scenarioParam = new Dictionary<string, string>()
        //        {
        //            [AssignSubstrateIdKeys.KeyParamLotId] = binSubstrate.GetLotId(),
        //            [AssignSubstrateIdKeys.KeyParamBinType] = substrateType.ToString(),
        //            [AssignSubstrateIdKeys.KeyParamRingFrameId] = ringId,
        //        };

        //        Dictionary<string, string> additionalParams = new Dictionary<string, string>
        //        {
        //            [AdditionalParamKeys.KeyNameOfEq] = nameOfEq,
        //            [AdditionalParamKeys.KeySubstrateId] = ringId,
        //        };

        //        EnqueueScenarioAsync(ScenarioListTypes.SCENARIO_REQ_BIN_WAFER_ID_ASSIGN, scenarioParam, additionalParams);
        //        return true;
        //    }
        //    else
        //    {
        //        Dictionary<string, string> messageContentToSend = new Dictionary<string, string>
        //        {
        //            [ResultKeys.KeyResult] = EN_MESSAGE_RESULT.NG.ToString(),
        //            [ResultKeys.KeyDescription] = "Doesn't have substrate id",
        //        };

        //        return SendClientToClientMessage(nameOfEq, MessagesToSend.ResponseFinishSorting.ToString(),
        //                    string.Empty, string.Empty,
        //                    messageContentToSend.Keys.ToArray(), messageContentToSend.Values.ToArray(),
        //                    EN_MESSAGE_RESULT.NG, true);
        //    }
        //}
        private bool ExecuteScenarioToSplitChip(string nameOfEq, bool isFirstSorting, bool isSplittedFully, string coreSubstrateId, string ringId,
            SubstrateType substrateType, int splitQty, string binCode, string userId)
        {
            ScenarioListTypes scenario;
            if (false == isSplittedFully)
            {
                if (isFirstSorting)
                {
                    scenario = ScenarioListTypes.SCENARIO_REQ_CORE_CHIP_SPLIT_FIRST;
                }
                else
                {
                    scenario = ScenarioListTypes.SCENARIO_REQ_CORE_CHIP_SPLIT;
                }
            }
            else
            {
                if (isFirstSorting)
                {
                    scenario = ScenarioListTypes.SCENARIO_REQ_CORE_CHIP_FULL_SPLIT_FIRST;
                }
                else
                {
                    scenario = ScenarioListTypes.SCENARIO_REQ_CORE_CHIP_FULL_SPLIT;
                }
            }

            Substrate coreSubstrate = new Substrate("");
            Substrate binSubstrate = new Substrate("");

            if (_substrateManager.GetSubstrateByName(coreSubstrateId, ref coreSubstrate) &&
                _substrateManager.GetSubstrateByName(ringId, ref binSubstrate))
            {
                Dictionary<string, string> scenarioParam = new Dictionary<string, string>()
                {
                    [SplitCoreChipKeys.KeyParamLotId] = coreSubstrate.GetLotId(),
                    [SplitCoreChipKeys.KeyParamSplitWaferId] = coreSubstrateId,
                    [SplitCoreChipKeys.KeyParamRingFrameId] = ringId,
                    [SplitCoreChipKeys.KeyParamBinType] = binCode,
                    [SplitCoreChipKeys.KeyParamSplitChipQty] = splitQty.ToString(),
                };

                Dictionary<string, string> additionalParams = new Dictionary<string, string>
                {
                    [AdditionalParamKeys.KeyNameOfEq] = nameOfEq,
                    [AdditionalParamKeys.KeySubstrateId] = ringId
                };

                binSubstrate.SetAttribute(PWA500BINSubstrateAttributes.BinCode, binCode);
                EnqueueScenarioAsync(scenario, scenarioParam, additionalParams);
                return true;
            }
            else
            {
                Dictionary<string, string> messageContentToSend = new Dictionary<string, string>
                {
                    [ResultKeys.KeyResult] = EN_MESSAGE_RESULT.NG.ToString(),
                    [ResultKeys.KeyDescription] = "Doesn't have substrate id",
                };

                return SendClientToClientMessage(nameOfEq, MessagesToSend.ResponseSplitCoreChip.ToString(),
                            string.Empty, string.Empty,
                            messageContentToSend.Keys.ToArray(), messageContentToSend.Values.ToArray(),
                            EN_MESSAGE_RESULT.NG, true);
            }
        }
        private bool ExecuteScenarioToAssignSubstrateRingId(string nameOfEq, string oldSubstrateId, string newSubstrateId)
        {
            Substrate substrate = new Substrate("");

            bool result = true;
            if (_substrateManager.GetSubstrateByName(oldSubstrateId, ref substrate) ||
                _substrateManager.GetSubstrateByName(newSubstrateId, ref substrate))
            {
                string oldName = substrate.GetName();
                if (false == oldName.Equals(newSubstrateId))
                {
                    int portId = substrate.GetSourcePortId();
                    _lotHistoryLog.WriteSubstrateHistoryForReadRingId(portId, oldSubstrateId, newSubstrateId);
                }

                // 읽은 1D를 이름으로 설정한다. -> 원래는 Ring Id 이며, 나중에 Id를 Assign 받는다.
                substrate.SetAttribute(PWA500BINSubstrateAttributes.RingId, newSubstrateId);
                substrate.SetName(newSubstrateId);

                if (UseCoreMapHandlingOnly)
                {
                    return ExecuteToSendSimpleResultToClient(EN_MESSAGE_RESULT.OK, MessagesToSend.ResponseAssignRingId.ToString(), nameOfEq);
                }
                else
                {
                    Dictionary<string, string> scenarioParams = new Dictionary<string, string>
                    {
                        [AssignRingIdKeys.KeyParamLotId] = substrate.GetLotId(),
                        [AssignRingIdKeys.KeyParamWaferId] = newSubstrateId,
                    };

                    Dictionary<string, string> additionalParams = new Dictionary<string, string>();
                    additionalParams[AdditionalParamKeys.KeyNameOfEq] = nameOfEq;
                    additionalParams[AdditionalParamKeys.KeySubstrateId] = newSubstrateId;
                    additionalParams[AdditionalParamKeys.KeyMessageNameToSend] = MessagesToSend.ResponseAssignRingId.ToString();
                    EnqueueScenarioAsync(ScenarioListTypes.SCENARIO_BIN_WAFER_ID_READ, scenarioParams, additionalParams);
                }
            }
            else
            {
                Dictionary<string, string> messageContentToSend = new Dictionary<string, string>
                {
                    [ResultKeys.KeyResult] = EN_MESSAGE_RESULT.NG.ToString(),
                    [ResultKeys.KeyDescription] = "Doesn't have ring id",
                    [AssignRingIdKeys.KeyOldRingId] = oldSubstrateId,
                    [AssignRingIdKeys.KeyNewRingId] = newSubstrateId
                };

                SendClientToClientMessage(nameOfEq, MessagesToSend.ResponseAssignRingId.ToString(),
                    string.Empty, string.Empty,
                    messageContentToSend.Keys.ToArray(), messageContentToSend.Values.ToArray(),
                    EN_MESSAGE_RESULT.NG,
                    true);
                result = false;
            }

            return result;
        }

        private bool ExecuteScenarioToDownloadMapFile(string nameOfEq, string substrateId, string ringId, double waferAngle, string nullBinCode, string userId, bool useEventHandling)
        {
            Substrate substrate = new Substrate("");
            // TODO : 여기서 자재 정보를 Set 할게 아니라, WorkStart 이후 정상일 때에만 Set 하도록 해야한다.
            // 2025.01.22. jhlim [MOD] RingId를 이용해 찾도록 추가
            // 2024.12.29. jhlim [MOD] RingId가 고유하므로, RingId 부터 찾는다.
            if (_substrateManager.GetSubstrateByAttribute(PWA500BINSubstrateAttributes.RingId, ringId, ref substrate) ||
                _substrateManager.GetSubstrateByName(ringId, ref substrate)||
                _substrateManager.GetSubstrateByName(substrateId, ref substrate))
            {
                int portId, slot;
                portId = substrate.GetSourcePortId();
                slot = substrate.GetSourceSlot();

                if (portId <= 0 || slot < 0)
                    return false;

                string carrierId = _carrierServer.GetCarrierId(portId);
                string lotId = substrate.GetLotId();
                string partId = substrate.GetAttribute(PWA500BINSubstrateAttributes.PartId);
                string recipeId = _scenarioManager.GetRecipeId();

                Dictionary<string, string> scenarioParams = new Dictionary<string, string>
                {
                    [RequestDownloadMapFileKeys.KeyParamCarrierId] = carrierId,
                    [RequestDownloadMapFileKeys.KeyParamPortId] = portId.ToString(),
                    [RequestDownloadMapFileKeys.KeyParamLotId] = lotId,
                    [RequestDownloadMapFileKeys.KeyParamPartId] = partId,
                    [RequestDownloadMapFileKeys.KeyParamRecipeId] = recipeId,
                    [RequestDownloadMapFileKeys.KeyParamOperatorId] = userId,
                    [RequestDownloadMapFileKeys.KeyParamWaferId] = substrateId,
                    [RequestDownloadMapFileKeys.KeyParamAngle] = waferAngle.ToString(),
                    [RequestDownloadMapFileKeys.KeyNullBinCode] = nullBinCode,
                    [RequestDownloadMapFileKeys.KeyUseEventHandling] = useEventHandling.ToString(),

                };

                // 공정설비에서 받은 이름을 이 웨이퍼의 이름으로 설정한다.
                substrate.SetName(substrateId);

                Dictionary<string, string> additionalParams = new Dictionary<string, string>();
                additionalParams[AdditionalParamKeys.KeyNameOfEq] = nameOfEq;
                additionalParams[AdditionalParamKeys.KeySubstrateId] = substrateId;
                additionalParams[AdditionalParamKeys.KeyRingId] = ringId;
                additionalParams[AdditionalParamKeys.KeyUserId] = userId;

                _lotHistoryLog.WriteSubstrateHistoryForDownloadMap(portId, carrierId, substrateId, ringId);

                EnqueueScenarioAsync(ScenarioListTypes.SCENARIO_WORK_START, scenarioParams, additionalParams);
                return true;
            }
            else
            {
                if (UseCoreMapHandlingOnly)
                {
                    Dictionary<string, string> scenarioParams = new Dictionary<string, string>
                    {
                        [RequestDownloadMapFileKeys.KeyParamCarrierId] = string.Empty,
                        [RequestDownloadMapFileKeys.KeyParamPortId] = string.Empty,
                        [RequestDownloadMapFileKeys.KeyParamLotId] = string.Empty,
                        [RequestDownloadMapFileKeys.KeyParamPartId] = string.Empty,
                        [RequestDownloadMapFileKeys.KeyParamRecipeId] = _scenarioManager.GetRecipeId(),
                        [RequestDownloadMapFileKeys.KeyParamOperatorId] = userId,
                        [RequestDownloadMapFileKeys.KeyParamWaferId] = substrateId,
                        [RequestDownloadMapFileKeys.KeyParamAngle] = waferAngle.ToString(),
                        [RequestDownloadMapFileKeys.KeyNullBinCode] = nullBinCode,
                        [RequestDownloadMapFileKeys.KeyUseEventHandling] = useEventHandling.ToString()
                    };

                    Dictionary<string, string> additionalParams = new Dictionary<string, string>();
                    additionalParams[AdditionalParamKeys.KeyNameOfEq] = nameOfEq;
                    additionalParams[AdditionalParamKeys.KeySubstrateId] = substrateId;
                    additionalParams[AdditionalParamKeys.KeyRingId] = ringId;
                    additionalParams[AdditionalParamKeys.KeyUserId] = userId;

                    EnqueueScenarioAsync(ScenarioListTypes.SCENARIO_WORK_START, scenarioParams, additionalParams);
                    return true;
                }
                else
                {
                    Dictionary<string, string> messageContentToSend = new Dictionary<string, string>
                    {
                        [ResultKeys.KeyResult] = EN_MESSAGE_RESULT.NG.ToString(),
                        [ResultKeys.KeyDescription] = "Doesn't have ring id",
                        [RequestDownloadMapFileKeys.KeyParamWaferId] = substrateId,
                        [RequestDownloadMapFileKeys.KeyCountRow] = string.Empty,
                        [RequestDownloadMapFileKeys.KeyCountCol] = string.Empty,
                        [RequestDownloadMapFileKeys.KeyParamAngle] = string.Empty,
                        [RequestDownloadMapFileKeys.KeyMapData] = string.Empty,
                    };

                    SendClientToClientMessage(nameOfEq, MessagesToSend.ResponseDownloadMapFile.ToString(),
                        string.Empty, string.Empty,
                        messageContentToSend.Keys.ToArray(), messageContentToSend.Values.ToArray(),
                        EN_MESSAGE_RESULT.NG,
                        true);

                    return false;
                }
            }
        }

        //private bool ExecuteScenarioToTrackOut(string substrateId, int chipQty, string userId, bool isCore)
        //{
        //    Substrate substrate = new Substrate("");
        //    if (false == _substrateManager.GetSubstrateByName(substrateId, ref substrate))
        //        return false;

        //    ScenarioListTypes scenario;
        //    string lotId = substrate.GetLotId();
        //    string partId = substrate.GetAttribute(PWA500BINSubstrateAttributes.PartId);
        //    string stepId = substrate.GetAttribute(PWA500BINSubstrateAttributes.StepSeq);
        //    string recipeId = _scenarioManager.GetRecipeId();
        //    int portId;
        //    if (false == isCore)
        //    {
        //        portId = substrate.GetDestinationPortId();

        //        // 2024.10.29. jhlim [MOD] StepSeq가 설정값과 다르면 값을 셋한다.
        //        string stepSeqFromParam = _scenarioManager.GetStepIdForBinWafer();
        //        if (stepId.Equals(stepSeqFromParam))
        //        {
        //            substrate.SetAttribute(PWA500BINSubstrateAttributes.StepSeq, stepSeqFromParam);
        //        }

        //        stepId = stepSeqFromParam;
        //        // 2024.10.29. jhlim [END]

        //        scenario = ScenarioListTypes.SCENARIO_REQ_BIN_WAFER_TRACK_OUT;
        //    }
        //    else
        //    {
        //        portId = substrate.GetSourcePortId();
        //        scenario = ScenarioListTypes.SCENARIO_REQ_CORE_WAFER_TRACK_OUT;
        //    }

        //    if (portId <= 0 || _carrierServer.HasCarrier(portId))
        //        return false;

        //    string carrierId = _carrierServer.GetCarrierId(portId);
        //    //if (isCore)
        //    //{
        //    //    string isLastString = substrate.GetAttribute(PWA500BINSubstrateAttributes.IsLastSubstrate);
        //    //    bool.TryParse(isLastString, out bool isLast);
        //    //    if (isLast)
        //    //    {
        //    //        lotId = _carrierServer.GetCarrierLotId(portId);
        //    //        // EnqueueScenarioAsync(ScenarioListTyps.SCENARIO_PROCESS_END,....);
        //    //    }

        //    //}

        //    Dictionary<string, string> scenarioParams = new Dictionary<string, string>();
        //    scenarioParams[TrackInOrOut.KeyParamCarrierId] = carrierId;
        //    scenarioParams[TrackInOrOut.KeyParamPortId] = _scenarioManager.GetPortName(portId);
        //    scenarioParams[TrackInOrOut.KeyParamLotId] = lotId;
        //    scenarioParams[TrackInOrOut.KeyParamPartId] = partId;
        //    scenarioParams[TrackInOrOut.KeyParamStepSeq] = stepId;
        //    scenarioParams[TrackInOrOut.KeyParamRecipeId] = recipeId;
        //    scenarioParams[TrackInOrOut.KeyParamChipQty] = chipQty.ToString();
        //    if (false == isCore)
        //    {
        //        scenarioParams[TrackInOrOut.KeyParamBinType] = substrate.GetAttribute(PWA500BINSubstrateAttributes.BinCode);
        //    }
        //    scenarioParams[TrackInOrOut.KeyParamOperatorId] = userId;


        //    EnqueueScenarioAsync(scenario, scenarioParams);
        //    return true;
        //}

        private bool ExecuteScenarioToWorkEnd(string nameOfEq, string substrateId, string ringId,
            int chipQty, double angle, int countRow, int countCol, string nullBinCode, string mapData,
            string userId, bool isCore, bool useEventHandling)
        {
            Substrate substrate = new Substrate("");
            if (_substrateManager.GetSubstrateByName(substrateId, ref substrate) ||
                _substrateManager.GetSubstrateByName(ringId, ref substrate))
            {
                string lotId = substrate.GetLotId();

                ScenarioListTypes scenario;
                int portId, slot;
                string partId = substrate.GetAttribute(PWA500BINSubstrateAttributes.PartId);
                string recipeId = _scenarioManager.GetRecipeId();

                Dictionary<string, string> additionalParams = null;
                if (false == isCore)
                {
                    portId = substrate.GetDestinationPortId();
                    slot = substrate.GetDestinationSlot();
                    scenario = ScenarioListTypes.SCENARIO_BIN_WORK_END;

                    additionalParams = new Dictionary<string, string>
                    {
                        [AdditionalParamKeys.KeySubstrateId] = substrateId,
                        [AdditionalParamKeys.KeyChipQty] = chipQty.ToString(),
                        [AdditionalParamKeys.KeyUserId] = userId,
                    };
                }
                else
                {
                    portId = substrate.GetSourcePortId();
                    slot = substrate.GetSourceSlot();
                    scenario = ScenarioListTypes.SCENARIO_WORK_END;
                    additionalParams = new Dictionary<string, string>
                    {
                        [AdditionalParamKeys.KeyNameOfEq] = nameOfEq,
                        [AdditionalParamKeys.KeySubstrateId] = substrateId,
                        [AdditionalParamKeys.KeyChipQty] = chipQty.ToString(),
                        [AdditionalParamKeys.KeyUserId] = userId,
                    };
                }

                if (portId <= 0 || slot < 0)
                    return false;

                string carrierId = _carrierServer.GetCarrierId(portId);
                Dictionary<string, string> scenarioParams = new Dictionary<string, string>
                {
                    [UploadCoreOrBinFileKeys.KeyParamCarrierId] = carrierId,
                    [UploadCoreOrBinFileKeys.KeyParamPortId] = _scenarioManager.GetPortName(portId),
                    [UploadCoreOrBinFileKeys.KeyParamLotId] = lotId,
                    [UploadCoreOrBinFileKeys.KeyParamPartId] = partId,
                    [UploadCoreOrBinFileKeys.KeyParamRecipeId] = recipeId,
                    
                    // 슬롯이 없다??
                    // TODO : 슬롯을 1부터 매기도록 바꿔야하나?
                    //[UploadCoreOrBinFileKeys.KeyParamSlotId] = (slot + 1).ToString(),

                    [UploadCoreOrBinFileKeys.KeyParamOperatorId] = userId,
                    [UploadCoreOrBinFileKeys.KeyChipQty] = chipQty.ToString(),

                    [UploadCoreOrBinFileKeys.KeySubstrateName] = substrateId,

                    [UploadCoreOrBinFileKeys.KeyWaferAngle] = angle.ToString(),
                    [UploadCoreOrBinFileKeys.KeyCountRow] = countRow.ToString(),
                    [UploadCoreOrBinFileKeys.KeyCountCol] = countCol.ToString(),
                    [UploadCoreOrBinFileKeys.KeyReferenceX] = substrate.GetAttribute(PWA500BINSubstrateAttributes.RefPositionX),
                    [UploadCoreOrBinFileKeys.KeyReferenceY] = substrate.GetAttribute(PWA500BINSubstrateAttributes.RefPositionY),
                    [UploadCoreOrBinFileKeys.KeyStartingPosX] = substrate.GetAttribute(PWA500BINSubstrateAttributes.StartingPositionX),
                    [UploadCoreOrBinFileKeys.KeyStartingPosY] = substrate.GetAttribute(PWA500BINSubstrateAttributes.StartingPositionY),
                    [UploadCoreOrBinFileKeys.KeyNullBinCode] = nullBinCode,
                    [UploadCoreOrBinFileKeys.KeyMapData] = mapData,

                    [UploadCoreOrBinFileKeys.KeyUseEventHandling] = useEventHandling.ToString(),
                };

                substrate.SetAttribute(PWA500BINSubstrateAttributes.MapData, mapData);
                substrate.SetAttribute(PWA500BINSubstrateAttributes.ChipQty, chipQty.ToString());

                EnqueueScenarioAsync(scenario, scenarioParams, additionalParams);
                return true;
            }
            else
            {
                if (false == useEventHandling)
                {
                    Dictionary<string, string> scenarioParams = new Dictionary<string, string>
                    {
                        [UploadCoreOrBinFileKeys.KeyParamCarrierId] = string.Empty,
                        [UploadCoreOrBinFileKeys.KeyParamPortId] = string.Empty,
                        [UploadCoreOrBinFileKeys.KeyParamLotId] = string.Empty,
                        [UploadCoreOrBinFileKeys.KeyParamPartId] = string.Empty,
                        [UploadCoreOrBinFileKeys.KeyParamRecipeId] = _scenarioManager.GetRecipeId(),
                        [UploadCoreOrBinFileKeys.KeyParamOperatorId] = userId,
                        [UploadCoreOrBinFileKeys.KeyChipQty] = chipQty.ToString(),

                        [UploadCoreOrBinFileKeys.KeyPMSFileName] = string.Empty,
                        [UploadCoreOrBinFileKeys.KeyPMSFileBody] = string.Empty,

                        [UploadCoreOrBinFileKeys.KeySubstrateName] = substrateId,

                        [UploadCoreOrBinFileKeys.KeyWaferAngle] = angle.ToString(),
                        [UploadCoreOrBinFileKeys.KeyCountRow] = countRow.ToString(),
                        [UploadCoreOrBinFileKeys.KeyCountCol] = countCol.ToString(),
                        [UploadCoreOrBinFileKeys.KeyNullBinCode] = nullBinCode,
                        [UploadCoreOrBinFileKeys.KeyMapData] = mapData,
                        [UploadCoreOrBinFileKeys.KeyReferenceX] = "0",
                        [UploadCoreOrBinFileKeys.KeyReferenceY] = "0",
                        [UploadCoreOrBinFileKeys.KeyStartingPosX] = "0",
                        [UploadCoreOrBinFileKeys.KeyStartingPosY] = "0",

                        [UploadCoreOrBinFileKeys.KeyUseEventHandling] = useEventHandling.ToString(),
                    };

                    Dictionary<string, string> additionalParams = new Dictionary<string, string>
                    {
                        [AdditionalParamKeys.KeyNameOfEq] = nameOfEq,
                        [AdditionalParamKeys.KeySubstrateId] = substrateId,
                        [AdditionalParamKeys.KeyChipQty] = chipQty.ToString(),
                        [AdditionalParamKeys.KeyUserId] = userId,
                    };

                    if (isCore)
                    {
                        EnqueueScenarioAsync(ScenarioListTypes.SCENARIO_WORK_END, scenarioParams, additionalParams);
                    }
                    else
                    {
                        EnqueueScenarioAsync(ScenarioListTypes.SCENARIO_BIN_WORK_END, scenarioParams, additionalParams);
                    }
                    return true;

                    // 2024.08.18 : [END]
                }
            }

            return false;
        }

        private bool ExecuteScenarioToUploadBinData(string nameOfEq, string substrateId, string ringId,
            int chipQty, double angle, int countRow, int countCol, string nullBinCode, string mapData,
            string pmsFileBody, string userId, bool useEventHandling)
        {
            ScenarioListTypes scenario = ScenarioListTypes.SCENARIO_BIN_DATA_UPLOAD;
            string recipeId = _scenarioManager.GetRecipeId();

            Substrate substrate = new Substrate("");
            if (_substrateManager.GetSubstrateByName(substrateId, ref substrate) ||
                _substrateManager.GetSubstrateByName(ringId, ref substrate))
            {
                string lotId = substrate.GetLotId();                
                int portId, slot;
                string partId = substrate.GetAttribute(PWA500BINSubstrateAttributes.PartId);
                

                Dictionary<string, string> additionalParams = null;
                portId = substrate.GetDestinationPortId();
                slot = substrate.GetDestinationSlot();
                
                string fileName = _scenarioManager.GetPMSFileName(lotId, substrateId);
                string fullPath = string.Empty;
                if (false == _scenarioManager.MakePMSFile(lotId, substrateId, fileName, pmsFileBody, ref fullPath))
                    return false;

                additionalParams = new Dictionary<string, string>
                {
                    [AdditionalParamKeys.KeySubstrateId] = substrateId,
                    [AdditionalParamKeys.KeyChipQty] = chipQty.ToString(),
                    [AdditionalParamKeys.KeyUserId] = userId,
                };

                if (portId <= 0 || slot < 0)
                    return false;

                string carrierId = _carrierServer.GetCarrierId(portId);
                Dictionary<string, string> scenarioParams = new Dictionary<string, string>
                {
                    [UploadCoreOrBinFileKeys.KeyParamCarrierId] = carrierId,
                    [UploadCoreOrBinFileKeys.KeyParamPortId] = _scenarioManager.GetPortName(portId),
                    [UploadCoreOrBinFileKeys.KeyParamLotId] = lotId,
                    [UploadCoreOrBinFileKeys.KeyParamPartId] = partId,
                    [UploadCoreOrBinFileKeys.KeyParamRecipeId] = recipeId,
                    
                    // 슬롯 번호가 없다??
                    // TODO : 슬롯을 1부터 매기도록 바꿔야하나?
                    //[UploadCoreOrBinFileKeys.KeyParamSlotId] = (slot + 1).ToString(),

                    [UploadCoreOrBinFileKeys.KeyParamOperatorId] = userId,
                    [UploadCoreOrBinFileKeys.KeyChipQty] = chipQty.ToString(),
                    [UploadCoreOrBinFileKeys.KeyPMSFileName] = fileName,
                    [UploadCoreOrBinFileKeys.KeyPMSFileBody] = fullPath,

                    [UploadCoreOrBinFileKeys.KeySubstrateName] = substrateId,

                    [UploadCoreOrBinFileKeys.KeyWaferAngle] = angle.ToString(),
                    [UploadCoreOrBinFileKeys.KeyCountRow] = countRow.ToString(),
                    [UploadCoreOrBinFileKeys.KeyCountCol] = countCol.ToString(),
                    [UploadCoreOrBinFileKeys.KeyReferenceX] = substrate.GetAttribute(PWA500BINSubstrateAttributes.RefPositionX),
                    [UploadCoreOrBinFileKeys.KeyReferenceY] = substrate.GetAttribute(PWA500BINSubstrateAttributes.RefPositionY),
                    [UploadCoreOrBinFileKeys.KeyStartingPosX] = substrate.GetAttribute(PWA500BINSubstrateAttributes.StartingPositionX),
                    [UploadCoreOrBinFileKeys.KeyStartingPosY] = substrate.GetAttribute(PWA500BINSubstrateAttributes.StartingPositionY),
                    [UploadCoreOrBinFileKeys.KeyNullBinCode] = nullBinCode,
                    [UploadCoreOrBinFileKeys.KeyMapData] = mapData,

                    [UploadCoreOrBinFileKeys.KeyUseEventHandling] = useEventHandling.ToString(),
                };

                substrate.SetAttribute(PWA500BINSubstrateAttributes.MapData, mapData);
                substrate.SetAttribute(PWA500BINSubstrateAttributes.ChipQty, chipQty.ToString());

                EnqueueScenarioAsync(scenario, scenarioParams, additionalParams);
                return true;
            }
            else
            {
                if (false == useEventHandling)
                {
                    Dictionary<string, string> scenarioParams = new Dictionary<string, string>
                    {
                        [UploadCoreOrBinFileKeys.KeyParamCarrierId] = string.Empty,
                        [UploadCoreOrBinFileKeys.KeyParamPortId] = string.Empty,
                        [UploadCoreOrBinFileKeys.KeyParamLotId] = string.Empty,
                        [UploadCoreOrBinFileKeys.KeyParamPartId] = string.Empty,
                        [UploadCoreOrBinFileKeys.KeyParamRecipeId] = _scenarioManager.GetRecipeId(),
                        
                        [UploadCoreOrBinFileKeys.KeyParamOperatorId] = userId,
                        [UploadCoreOrBinFileKeys.KeyChipQty] = chipQty.ToString(),

                        [UploadCoreOrBinFileKeys.KeyPMSFileName] = string.Empty,
                        [UploadCoreOrBinFileKeys.KeyPMSFileBody] = string.Empty,

                        [UploadCoreOrBinFileKeys.KeySubstrateName] = substrateId,

                        [UploadCoreOrBinFileKeys.KeyWaferAngle] = angle.ToString(),
                        [UploadCoreOrBinFileKeys.KeyCountRow] = countRow.ToString(),
                        [UploadCoreOrBinFileKeys.KeyCountCol] = countCol.ToString(),
                        [UploadCoreOrBinFileKeys.KeyNullBinCode] = nullBinCode,
                        [UploadCoreOrBinFileKeys.KeyMapData] = mapData,
                        [UploadCoreOrBinFileKeys.KeyReferenceX] = "0",
                        [UploadCoreOrBinFileKeys.KeyReferenceY] = "0",
                        [UploadCoreOrBinFileKeys.KeyStartingPosX] = "0",
                        [UploadCoreOrBinFileKeys.KeyStartingPosY] = "0",

                        [UploadCoreOrBinFileKeys.KeyUseEventHandling] = useEventHandling.ToString(),
                    };

                    Dictionary<string, string> additionalParams = new Dictionary<string, string>
                    {
                        [AdditionalParamKeys.KeyNameOfEq] = nameOfEq,
                        [AdditionalParamKeys.KeySubstrateId] = substrateId,
                        [AdditionalParamKeys.KeyChipQty] = chipQty.ToString(),
                        [AdditionalParamKeys.KeyUserId] = userId,
                    };

                    EnqueueScenarioAsync(scenario, scenarioParams, additionalParams);
                    
                    return true;

                    // 2024.08.18 : [END]
                }
            }

            return false;
        }
        #endregion /method
    }
}
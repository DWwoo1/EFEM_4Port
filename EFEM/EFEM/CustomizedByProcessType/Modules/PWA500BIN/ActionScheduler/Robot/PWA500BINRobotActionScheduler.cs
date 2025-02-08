using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFEM.Defines.AtmRobot;
using EFEM.MaterialTracking;
using EFEM.Defines.MaterialTracking;
using EFEM.Defines.LoadPort;
using EFEM.Defines.Common;
using EFEM.MaterialTracking.LocationServer;
using EFEM.ActionScheduler.RobotActionSchedulers;

using FrameOfSystem3.SECSGEM.Scenario;

namespace EFEM.CustomizedByProcessType.PWA500BIN
{
    public class PWA500BinSorterRobotActionScheduler : BaseRobotActionScheduler
    {
        #region <Constructors>
        public PWA500BinSorterRobotActionScheduler(int index) : base(index)
        {
            _requestedLoadingLocation = new List<string>();
            _requestedUnloadingLocation = new List<string>();
            
            _scenarioManager = ScenarioManagerForPWA500BIN_TP.Instance;

            CoreLoadPortIndex = new List<int>
            {
                (int)LoadPortType.Core_1,
                (int)LoadPortType.Core_2
            };

            //EmptyTapeLoadPortIndex = (int)LoadPortType.EmptyTape;

            BinLoadPortIndex = new List<int>
            {
                (int)LoadPortType.Bin_1,
                (int)LoadPortType.Bin_2,
                (int)LoadPortType.Bin_3
            };

            _seqNum = 0;

            LoadPortPorts = new Dictionary<int, int>();
            var ports = (LoadPortType[])Enum.GetValues(typeof(LoadPortType));
            foreach (var item in ports)
            {
                int lpIndex = (int)item;
                int port = _loadPortManager.GetLoadPortPortId(lpIndex);

                LoadPortPorts[lpIndex] = port;
            }

            WorkingInfosToPlace = new Dictionary<RobotArmTypes, RobotWorkingInfo>();
            LocationTypesToPlace = new Dictionary<RobotArmTypes, ModuleType>();
            ProcessModuleName = _processGroup.GetProcessModuleName(ProcessModuleIndex);
            
            _substratesAtProcessModule = new List<Substrate>();
        }
        #endregion </Constructors>

        #region <Fields>
        private const int ProcessModuleIndex = 0;
        private bool _turnLoad = false;
        private bool _turnUnload = false;

        private List<string> _requestedLoadingLocation;
        private List<string> _requestedUnloadingLocation;

        private readonly List<int> CoreLoadPortIndex = null;
        private readonly List<int> BinLoadPortIndex = null;

        private readonly Dictionary<int, int> LoadPortPorts = null;
        private readonly Dictionary<RobotArmTypes, ModuleType> LocationTypesToPlace = null;
        private readonly Dictionary<RobotArmTypes, RobotWorkingInfo> WorkingInfosToPlace = null;

        private readonly string ProcessModuleName;
        private static ScenarioManagerForPWA500BIN_TP _scenarioManager = null;

        private List<Substrate> _substratesAtProcessModule = null;
        #endregion </Fields>

        #region <Enum>
        //enum SubstrateType
        //{
        //    Core,
        //    Empty,
        //    Bin
        //}

        enum SchedulerStep
        {
            Start = 0,
            CollectData,
            SetupWorkInfoToPlace,
            SetupWorkInfoToPick,
            CheckRequestFromProcessModule,
            CheckAvailableArm,
            CheckLoadPortCondition,
            UpdateWorkingInfo,
            End = 1000
        }
        #endregion </Enum>

        #region <Properties>
        #endregion </Properties>

        #region <Methods>
        public override void InitScheduler()
        {
            base.InitScheduler();            
        }

        private bool IsLoadPortTransferStatusBlocked(int lpIndex)
        {
            if (false == LoadPortInformations.ContainsKey(lpIndex))
                return false;

            return (LoadPortInformations[lpIndex].TransferState.Equals(LoadPortTransferStates.TransferBlocked) && LoadPortInformations[lpIndex].DoorState);
        }
        private bool HasCarriers(SubstrateType type, bool loading, ref int lpIndex)
        {
            switch (type)
            {
                case SubstrateType.Core:
                    {
                        // 1. Access된 Carrier가 있는지 먼저 검색
                        int inAccessedCarrierIndex = -1;
                        for (int i = 0; i < CoreLoadPortIndex.Count; ++i)
                        {
                            int portId = _loadPortManager.GetLoadPortPortId(CoreLoadPortIndex[i]);
                            if (_carrierServer.GetCarrierAccessingStatus(portId).Equals(CarrierAccessStates.InAccessed))
                            {
                                inAccessedCarrierIndex = CoreLoadPortIndex[i];
                                break;
                            }
                        }

                        if (inAccessedCarrierIndex >= 0)
                        {
                            // 1-1. Access된 캐리어가 있으면 작업이 가능한 상태인지 검사
                            int portId = _loadPortManager.GetLoadPortPortId(inAccessedCarrierIndex);
                            if (_carrierServer.HasCarrier(portId) && IsLoadPortTransferStatusBlocked(inAccessedCarrierIndex) &&
                                false == _loadPortManager.IsLoadPortBusy(inAccessedCarrierIndex))
                            {
                                lpIndex = inAccessedCarrierIndex;
                                return true;
                            }
                        }
                        else
                        {
                            string processModuleName = _processGroup.GetProcessModuleName(ProcessModuleIndex);

                            // 1-2. Access된 캐리어가 없으면, 작업 가능한 것 중 아무거나 선택
                            for (int i = 0; i < CoreLoadPortIndex.Count; ++i)
                            {
                                int portId = _loadPortManager.GetLoadPortPortId(CoreLoadPortIndex[i]);
                                if (false == _carrierServer.HasCarrier(portId) ||
                                    false == IsLoadPortTransferStatusBlocked(CoreLoadPortIndex[i]) ||
                                    _loadPortManager.IsLoadPortBusy(CoreLoadPortIndex[i]))
                                    continue;
                                
                                // 모든 자재가 NeedProcessing 상태면 -> TrackIn 해야하는 상황에 공정 설비에 공테이프가 없으면 투입하지 말아야한다.
                                if (_substrateManager.AreAllSubstratesNeedProcessing(portId))
                                {
                                    if (false == _substrateManager.GetSubstratesAtProcessModule(processModuleName, ref _substratesAtProcessModule) ||
                                        _substratesAtProcessModule.Count <= 0)
                                        continue;
                                }

                                lpIndex = CoreLoadPortIndex[i];
                                
                                return true;
                            }
                        }

                        return false;

                        #region <Orioginal>
                        //for (int i = 0; i < CoreLoadPortIndex.Count; ++i)
                        //{
                        //    int portId = _loadPortManager.GetLoadPortPortId(CoreLoadPortIndex[i]);
                        //    if (_carrierServer.HasCarrier(portId) && IsLoadPortTransferStatusBlocked(CoreLoadPortIndex[i]) &&
                        //        _carrierServer.GetCarrierAccessingStatus(portId).Equals(CarrierAccessStates.InAccessed) &&
                        //        false == _loadPortManager.IsLoadPortBusy(CoreLoadPortIndex[i]))
                        //    {
                        //        lpIndex = CoreLoadPortIndex[i];
                        //        return true;
                        //    }
                        //}

                        //for (int i = 0; i < CoreLoadPortIndex.Count; ++i)
                        //{
                        //    int portId = _loadPortManager.GetLoadPortPortId(CoreLoadPortIndex[i]);
                        //    if (_carrierServer.HasCarrier(portId) && IsLoadPortTransferStatusBlocked(CoreLoadPortIndex[i]) &&
                        //        false == _loadPortManager.IsLoadPortBusy(CoreLoadPortIndex[i]))
                        //    {
                        //        lpIndex = CoreLoadPortIndex[i];
                        //        return true;
                        //    }
                        //}
                        #endregion </Orioginal>
                    }

                case SubstrateType.Empty:
                    {
                        if (loading)
                        {
                            string processModuleName = _processGroup.GetProcessModuleName(ProcessModuleIndex);

                            // TODO : Consume 이벤트를 적용하게 되면, EmptyWafer도 Core와 동일하게 공정 설비에 다른 Lot 자재가 있는 경우 투입하지 않도록 수정 필요하다.
                            for (int i = 0; i < _loadPortManager.Count; ++i)
                            {
                                SubstrateType convertedSubType = _scenarioManager.GetSubstrateTypeByLoadPortIndex(i);
                                if (false == convertedSubType.Equals(SubstrateType.Empty))
                                    continue;

                                int portId = _loadPortManager.GetLoadPortPortId(i);
                                if (false == _carrierServer.HasCarrier(portId) || false == IsLoadPortTransferStatusBlocked(i)
                                    || _loadPortManager.IsLoadPortBusy(i))
                                    continue;

                                string lotId = _carrierServer.GetCarrierLotId(portId);
                                if (_substrateManager.GetSubstratesAtProcessModule(processModuleName, ref _substratesAtProcessModule))
                                {
                                    for (int subs = 0; subs < _substratesAtProcessModule.Count; ++subs)
                                    {
                                        string subType = _substratesAtProcessModule[subs].GetAttribute(PWA500BINSubstrateAttributes.SubstrateType);
                                        if (false == Enum.TryParse(subType, out SubstrateType substrateTypeAtProcessModule))
                                            continue;

                                        if (false == substrateTypeAtProcessModule.Equals(SubstrateType.Empty))
                                            continue;

                                        if (_substratesAtProcessModule[subs].GetAttribute(PWA500BINSubstrateAttributes.ParentLotId) != null && 
                                            false == _substratesAtProcessModule[subs].GetAttribute(PWA500BINSubstrateAttributes.ParentLotId).Equals(lotId))
                                        {
                                            return false;
                                        }
                                    }

                                    lpIndex = i;
                                    return true;
                                }
                                else
                                {
                                    lpIndex = i;
                                    return true;
                                }
                            }

                            #region <OriginalCode>                           
                            // 이 부분 모든 코드 확인 필요 - 작업이 이상하게됨
                            //for (int i = 0; i < _loadPortManager.Count; ++i)
                            //{
                            //    // 2024.09.03. jhlim [MOD] SubType을 UI에는 Center/Left/Right로 지정되도록 변경
                            //    //var paramName = FrameOfSystem3.Recipe.PARAM_EQUIPMENT.LoadPortType1 + i;
                            //    //string subTypeByRecipe = FrameOfSystem3.Recipe.Recipe.GetInstance().GetValue(FrameOfSystem3.Recipe.EN_RECIPE_TYPE.EQUIPMENT,
                            //    //    paramName.ToString(),
                            //    //    SubstrateType.Empty.ToString());
                            //    //if (false == subTypeByRecipe.Equals(SubstrateType.Empty.ToString()))
                            //    //    continue;

                            //    SubstrateType convertedSubType = _scenarioManager.GetSubstrateTypeByLoadPortIndex(i);
                            //    if (false == convertedSubType.Equals(SubstrateType.Empty))
                            //        continue;
                            //    // 2024.09.03. jhlim [END]

                            //    int portId = _loadPortManager.GetLoadPortPortId(i);
                            //    // 2024.12.04. jhlim [DEL]
                            //    //if (_carrierServer.HasCarrier(portId) && IsLoadPortTransferStatusBlocked(i))
                            //    // 2024.12.04. jhlim [END]
                            //    {
                            //        lpIndex = i;
                            //        return true;
                            //    }
                            //}
                            #endregion </OriginalCode>
                        }

                        return false;
                    }

                case SubstrateType.Bin1:
                case SubstrateType.Bin2:
                case SubstrateType.Bin3:
                    {
                        if (loading)
                        {
                            for (int i = 0; i < BinLoadPortIndex.Count; ++i)
                            {
                                int portId = _loadPortManager.GetLoadPortPortId(BinLoadPortIndex[i]);
                                if (_carrierServer.HasCarrier(portId) && IsLoadPortTransferStatusBlocked(BinLoadPortIndex[i]) &&
                                    _loadPortManager.IsLoadPortBusy(BinLoadPortIndex[i]))
                                {
                                    lpIndex = BinLoadPortIndex[i];
                                    return true;
                                }
                            }
                        }

                        return false;
                    }
                default:
                    return false;
            }
        }
        private bool GetSubstrateTypeByLoadingLocation(string locationName, ref SubstrateType substrateType)
        {
            if (locationName.Contains(Constants.CoreName))
            {
                substrateType = SubstrateType.Core;
                return true;
            }
            else if (locationName.Contains(Constants.SortName))
            {
                substrateType = SubstrateType.Empty;
                return true;
            }
            
            return false;
        }

        private bool GetSubstrateTypeByUnloadingLocation(string locationName, ref SubstrateType substrateType)
        {
            if (locationName.Contains(Constants.CoreName))
            {
                substrateType = SubstrateType.Core;
                return true;
            }
            else if (locationName.Contains(Constants.SortName))
            {
                if (FrameOfSystem3.Recipe.Recipe.GetInstance().GetValue(FrameOfSystem3.Recipe.EN_RECIPE_TYPE.COMMON, FrameOfSystem3.Recipe.PARAM_COMMON.UseCycleMode.ToString(),
                    false))
                {
                    substrateType = SubstrateType.Empty;
                }
                else
                {
                    substrateType = SubstrateType.Empty;
                    // Bin은 메시지 받기 전까지는 알 수 없으니 True 리턴한다.
                    return true;
                }
                return true;
            }

            return false;
        }

        // LoadPort 혹은 Carrier의 PortId로 PM의 입구를 찾는다.
        private string GetProcessModuleLoadingLocationByPortId(int portId)
        {
            bool find = false;
            LoadPortType lpType = LoadPortType.Core_1;
            foreach (var item in LoadPortPorts)
            {
                if (item.Value == portId)
                {
                    find = true;
                    lpType = (LoadPortType)item.Key;
                    break;
                }
            }

            if (find)
            {
                switch (lpType)
                {
                    // Bin으로 들어올 수가 있나??
                    //case LoadPortType.Bin_3:
                    //case LoadPortType.Bin_2:
                    //case LoadPortType.Bin_1:
                    //    return string.Empty;

                    case LoadPortType.EmptyTape:
                        return Constants.ProcessModuleSortInputName;

                    case LoadPortType.Core_2:
                    case LoadPortType.Core_1:
                        return Constants.ProcessModuleCoreInputName;
                        
                    default:
                        break;
                }
            }

            return string.Empty;
        }

        private string GetProcessModuleUnloadingLocationByPortId(int portId)
        {
            bool find = false;
            LoadPortType lpType = LoadPortType.Core_1;
            foreach (var item in LoadPortPorts)
            {
                if (item.Value == portId)
                {
                    find = true;
                    lpType = (LoadPortType)item.Key;
                    break;
                }
            }

            if (find)
            {
                switch (lpType)
                {
                    case LoadPortType.Bin_3:
                    case LoadPortType.Bin_2:
                    case LoadPortType.Bin_1:
                        return Constants.ProcessModuleSortOutputName;

                    case LoadPortType.EmptyTape:
                        return Constants.ProcessModuleSortOutputName;

                    case LoadPortType.Core_2:
                    case LoadPortType.Core_1:
                        return Constants.ProcessModuleCoreOutputName;

                    default:
                        break;
                }
            }

            return string.Empty;
        }

        // 작업할 정보를 설정한다.
        private void SetWorkingInfoToWork(RobotArmTypes arm, string substrateName, Location targetLocation)
        {
            _workingInfo.ActionArm = arm;
            _workingInfo.SubstrateName = substrateName;
            _workingInfo.Location = targetLocation;
        }

        // TODO : 추후 Bin 정보를 이용하여야 한다.
        // 자재 정보를 이용하여 내려놓을 장소를 찾는다.
        private bool GetWorkingInfoToPlace(Substrate substrate, ref Location targetLocation, ref ModuleType locationType)
        {
            switch (substrate.GetProcessingStatus())
            {
                case ProcessingStates.NeedsProcessing:      // TO PM
                    {
                        // TODO
                        // 1. 라우트 레시피가 있는 경우 다음 공정 스텝으로 위치를 받아와야한다.
                        string subType = substrate.GetAttribute(PWA500BINSubstrateAttributes.SubstrateType);
                        if (false == Enum.TryParse(subType, out SubstrateType substrateType))
                        {

                        }
                        else
                        {
                            string locationName = string.Empty;
                            switch (substrateType)
                            {
                                case SubstrateType.Core:
                                    {
                                        locationName = Constants.ProcessModuleCoreInputName;
                                    }
                                    break;
                                case SubstrateType.Empty:
                                    {
                                        locationName = Constants.ProcessModuleSortInputName;
                                    }
                                    break;
                                default:
                                    // 여기에 Place 할 일이 없다.
                                    break;
                            }

                            string processModuleName = _processGroup.GetProcessModuleName(ProcessModuleIndex);
                            ProcessModuleLocation location = new ProcessModuleLocation(processModuleName, locationName);
                            _locationServer.GetProcessModuleLocation(processModuleName, locationName, ref location);
                            targetLocation = location;
                            locationType = ModuleType.ProcessModule;
                        }

                        return true;
                    }
                    
                //case ProcessingStates.InProcess:
                //    break;
                //case ProcessingStates.Processed:
                //    break;
                case ProcessingStates.Rejected:     // TO LP
                case ProcessingStates.Stopped:
                case ProcessingStates.Aborted:
                case ProcessingStates.Skipped:
                    {
                        LoadPortLocation location = new LoadPortLocation(-1, -1, "");
                        _locationServer.GetLoadPortSlotLocation(substrate.GetSourcePortId(), substrate.GetSourceSlot(), ref location);
                        targetLocation = location;
                        locationType = ModuleType.LoadPort;

                        //int lpIndex = _loadPortManager.GetLoadPortIndexByPortId(substrate.GetSourcePortId());
                        //string lpName = _loadPortManager.GetLoadPortName(lpIndex);
                        //targetLocation = lpName;
                        //targetSlot = substrate.GetSourceSlot();
                        //locationType = ModuleType.LoadPort;
                        return true;
                    }
                    
                case ProcessingStates.Lost:
                    return false;

                default:    // TO LP
                    {
                        int lpIndex = _loadPortManager.GetLoadPortIndexByPortId(substrate.GetDestinationPortId());
                        int targetSlot = 0;
                        if (lpIndex == (int)LoadPortType.Bin_1 ||
                            lpIndex == (int)LoadPortType.Bin_2 ||
                            lpIndex == (int)LoadPortType.Bin_3)
                        {
                            if (false == GetNextSlotInformationToPlace(lpIndex, ref targetSlot))
                                return false;
                        }
                        else
                        {
                            targetSlot = substrate.GetSourceSlot();
                        }
                        LoadPortLocation location = new LoadPortLocation(-1, -1, "");
                        _locationServer.GetLoadPortSlotLocation(substrate.GetDestinationPortId(), targetSlot, ref location);

                        targetLocation = location;
                        locationType = ModuleType.LoadPort;

                        return true;
                    }
            }
        }

        private bool HasSubstratateToLoadAtProcessModule(string targetLocation)
        {
            for(int i = 0; i < _requestedLoadingLocation.Count; ++i)
            {
                if (_requestedLoadingLocation[i].Equals(targetLocation))
                    return true;
            }
            
            return false;
        }

        private bool HasSubstratateToUnloadAtProcessModule(string targetLocation)
        {
            for (int i = 0; i < _requestedUnloadingLocation.Count; ++i)
            {
                if (_requestedLoadingLocation[i].Equals(targetLocation))
                    return true;
            }

            return false;
        }

        private bool GetNextSlotInformationToPick(int lpIndex, ref LoadPortLocation location, ref string substrateName)
        {
            int portId = _loadPortManager.GetLoadPortPortId(lpIndex);

            if (false == _substrateManager.HasAnySubstrateAtLoadPort(portId))
                return false;

            int capacity = _carrierServer.GetCapacity(portId);
            for (int i = 0; i < capacity; ++i)
            {
                LoadPortLocation lpLocation = new LoadPortLocation(portId, i, "");
                if (_locationServer.GetLoadPortSlotLocation(portId, i, ref lpLocation))
                {
                    SubstrateTransferStates transferStatus = SubstrateTransferStates.AtSource;
                    ProcessingStates processingStatus = ProcessingStates.NeedsProcessing;
                    if (_substrateManager.GetTransferStatus(lpLocation, "", ref transferStatus) &&
                        _substrateManager.GetProcessingStatus(lpLocation, "", ref processingStatus))
                    {
                        if (transferStatus.Equals(SubstrateTransferStates.AtSource) && 
                            processingStatus.Equals(ProcessingStates.NeedsProcessing))
                        {                                
                            _locationServer.GetLoadPortSlotLocation(portId, i, ref location);
                            substrateName = _substrateManager.GetSubstrateNameAtLoadPort(portId, i);

                            return true;
                        }
                    }
                }               
            }

            return false;
        }

        private bool GetNextSlotInformationToPlace(int lpIndex, ref int slot)
        {
            int portId = _loadPortManager.GetLoadPortPortId(lpIndex);
            if (false == _carrierServer.HasCarrier(portId))
                return false;

            // 문이 열려있지 않으면 리턴
            if (false == _loadPortManager.GetDoorState(lpIndex))
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

        private RobotScheduleType GetNotCompletedStatus(int newStep = (int)SchedulerStep.Start)
        {
            _seqNum = newStep;
            return RobotScheduleType.Selection;
        }

        protected override RobotScheduleType DecideNextAction()
        {
            // 1. 공정 설비의 요청 수집
            bool needLoading = _processGroup.IsLoadingRequested(ProcessModuleIndex, ref _requestedLoadingLocation);
            bool needUnloading = _processGroup.IsUnloadingRequested(ProcessModuleIndex, ref _requestedUnloadingLocation);

            switch (_seqNum)
            {
                case (int)SchedulerStep.Start:
                    InitWorkingInfo();
                    _seqNum = (int)SchedulerStep.CollectData;
                    break;

                case (int)SchedulerStep.CollectData:
                    {
                        // 2. 암이 자재를 갖고 있는지 수집
                        bool hasAnySubstrate = false;
                        _robotManager.GetSubstrates(Index, ref _substrates);
                        foreach (var item in _substrates)
                        {
                            if (item.Value == null)
                                continue;

                            bool targetLocationPrepared = false;
                            Location targetLocation = new Location("");
                            ModuleType locationType = ModuleType.UnknownLocation;
                            if (false == GetWorkingInfoToPlace(item.Value, ref targetLocation, ref locationType))
                            {
                                continue;
                            }

                            switch (locationType)
                            {
                                case ModuleType.LoadPort:
                                    {
                                        LoadPortLocation location = targetLocation as LoadPortLocation;
                                        if (location.PortId > 0)
                                        {
                                            int lpIndex = _loadPortManager.GetLoadPortIndexByPortId(location.PortId);
                                            targetLocationPrepared = (_carrierServer.HasCarrier(location.PortId) && LoadPortInformations[lpIndex].DoorState);
                                        }
                                    }
                                    break;
                                case ModuleType.ProcessModule:
                                    {
                                        ProcessModuleLocation location = targetLocation as ProcessModuleLocation;
                                        targetLocationPrepared = HasSubstratateToLoadAtProcessModule(location.Name);
                                    }
                                    break;
                                default:
                                    break;
                            }

                            hasAnySubstrate |= targetLocationPrepared;
                        }
                        //foreach (var item in _substrates)
                        //{
                        //    hasAnySubstrate |= (item.Value != null);

                        // 결과 1 -> 요청이 전부 없고, 들고 있는 자재도 없으면 할게 없으니 초기 단계로 리턴
                        if (false == needLoading && false == needUnloading && false == hasAnySubstrate)
                        {
                            return GetNotCompletedStatus();
                        }

                        if (hasAnySubstrate)
                        {
                            // 결과 2 -> 자재가 있으면 일단 플레이스를 하려한다.
                            return GetNotCompletedStatus((int)SchedulerStep.SetupWorkInfoToPlace);
                        }
                        else
                        {
                            // 결과 3 -> 자재가 없으면 픽업을 시도한다.
                            return GetNotCompletedStatus((int)SchedulerStep.SetupWorkInfoToPick);
                        }
                    }

                #region <Setup workinginfo to pick>
                case (int)SchedulerStep.SetupWorkInfoToPick:
                    {
                        // 픽하기 전 로드포트 준비상태를 체크한다.
                        // 로딩 요청인 경우
                        if (_requestedLoadingLocation.Count > 0)
                        {
                            if (false == _turnLoad)
                            {
                                _requestedLoadingLocation.Reverse();
                            }
                            _turnLoad = !_turnLoad;
                        }

                        for (int i = 0; i < _requestedLoadingLocation.Count; ++i)
                        {
                            SubstrateType substrateType = SubstrateType.Core;
                            if (GetSubstrateTypeByLoadingLocation(_requestedLoadingLocation[i], ref substrateType))
                            {
                                int lpIndex = 0;/*, slot = 0;*/
                                string substrateName = string.Empty;
                                bool hasCarrier = HasCarriers(substrateType, true, ref lpIndex);
                                if (hasCarrier)
                                {
                                    List<RobotArmTypes> arms = new List<RobotArmTypes>();
                                    if (false == _robotManager.GetAvailableArm(Index, true, ref arms))
                                        return GetNotCompletedStatus();

                                    RobotArmTypes armToWork = arms.First();
                                    switch (substrateType)
                                    {
                                        case SubstrateType.Core:
                                        case SubstrateType.Empty:
                                            {
                                                // Sub 정보를 가져온다.
                                                // 작업할 자재가 있을 때에만 작업하도록 수정
                                                LoadPortLocation targetLocation = new LoadPortLocation(-1, -1, "");
                                                if (GetNextSlotInformationToPick(lpIndex, ref targetLocation, ref substrateName))
                                                {
                                                    //ProcessModuleLocation targetLocation = new ProcessModuleLocation("", "");
                                                    //string processModuleName = _processGroup.GetProcessModuleName(ProcessModuleIndex);
                                                    SetWorkingInfoToWork(armToWork, substrateName, targetLocation);
                                                    return RobotScheduleType.Pick;

                                                }
                                            }
                                            break;

                                        default:
                                            return GetNotCompletedStatus();
                                    }
                                }
                            }
                        }

                        if (_requestedUnloadingLocation.Count > 0)
                        {
                            if (false == _turnUnload)
                            {
                                _requestedUnloadingLocation.Reverse();
                            }
                            _turnUnload = !_turnUnload;
                        }

                        for (int i = 0; i < _requestedUnloadingLocation.Count; ++i)
                        {
                            SubstrateType substrateType = SubstrateType.Core;
                            // TODO : 요게 좀 애매함.... 
                            if (GetSubstrateTypeByUnloadingLocation(_requestedUnloadingLocation[i], ref substrateType))
                            {
                                int lpIndex = 0;/*, slot = 0;*/
                                bool hasCarrier = HasCarriers(substrateType, true, ref lpIndex);
                                if (false == substrateType.Equals(SubstrateType.Empty))
                                {
                                    if (false == hasCarrier)
                                        continue;
                                }

                                int portId = _loadPortManager.GetLoadPortPortId(lpIndex);
                                string locationName = GetProcessModuleUnloadingLocationByPortId(portId);
                                if (string.IsNullOrEmpty(locationName))
                                    return GetNotCompletedStatus();

                                //Substrate substrate = new Substrate("");
                                //if (false == _processGroup.GetSubstrateInEntryWay(ProcessModuleIndex, locationName, ref substrate))
                                //    return GetNotCompletedStatus();

                                string substrateName = string.Empty;// substrate.Name;
                                RobotArmTypes armToWork = RobotArmTypes.LowerArm;
                                ProcessModuleLocation targetLocation = new ProcessModuleLocation("", "");
                                string processModuleName = _processGroup.GetProcessModuleName(ProcessModuleIndex);
                                if (_locationServer.GetProcessModuleLocation(processModuleName, _requestedUnloadingLocation[i], ref targetLocation))
                                {
                                    SetWorkingInfoToWork(armToWork, substrateName, targetLocation);
                                    return RobotScheduleType.Pick;
                                }
                            }
                        }

                        return GetNotCompletedStatus();
                    }
                #endregion </Setup workinginfo to pick>

                #region <Setup workinginfo to place>
                case (int)SchedulerStep.SetupWorkInfoToPlace:
                    {
                        LocationTypesToPlace.Clear();
                        WorkingInfosToPlace.Clear();
                        bool needLoadingToProcessModule = false;

                        // 로봇이 갖고 있는 자재정보를 받아온다.

                        #region <Get substrate informations in robot>
                        foreach (var item in _substrates)
                        {
                            if (item.Value == null)
                                continue;

                            Location targetLocation = new Location("");
                            ModuleType locationType = ModuleType.UnknownLocation;
                            if (false == GetWorkingInfoToPlace(item.Value, ref targetLocation, ref locationType))
                            {
                                continue; 
                                //return GetNotCompletedStatus();
                            }

                            RobotWorkingInfo info = new RobotWorkingInfo
                            {
                                ActionArm = item.Key,
                                SubstrateName = item.Value.GetName(),
                                Location = targetLocation,
                            };

                            // Target Location 을 이용해 PM으로 보낼 자재 중 요청받은 것이 있는지 여부를 확인한다.
                            needLoadingToProcessModule |= HasSubstratateToLoadAtProcessModule(targetLocation.Name);

                            LocationTypesToPlace.Add(item.Key, locationType);
                            WorkingInfosToPlace.Add(item.Key, info);
                        }
                        #endregion </Get substrate informations in robot>

                        // 작업할 Arm을 첫 인덱스로 초기화
                        RobotArmTypes armToWork = WorkingInfosToPlace.First().Key;

                        // 작업할 위치 유형을 찾는다.
                        ModuleType targetLocationType = ModuleType.LoadPort;
                        if (needLoadingToProcessModule)
                        {
                            targetLocationType = ModuleType.ProcessModule;
                        }

                        foreach (var item in LocationTypesToPlace)
                        {
                            if (item.Value.Equals(targetLocationType))
                            {
                                armToWork = item.Key;
                                break;
                            }
                        }

                        _workingInfo = WorkingInfosToPlace[armToWork];

                        return RobotScheduleType.Place;
                    }
                    #endregion </Setup workinginfo to place>

                case (int)SchedulerStep.CheckLoadPortCondition:
                    break;

                default:
                    break;
            }

            return RobotScheduleType.Selection;
        }       

        #endregion </Methods>
    }
}
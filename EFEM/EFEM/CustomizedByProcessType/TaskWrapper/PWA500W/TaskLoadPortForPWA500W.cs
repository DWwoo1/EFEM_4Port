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
using EFEM.CustomizedByProcessType.PWA500W;

// ConfigTask에서 이 namespace를 가지고 클래스 타입을 가져오기 때문에 변경 불가
namespace FrameOfSystem3.Task
{
    class TaskLoadPortForPWA500W : TaskLoadPort
    {
        #region <Constructors>
        public TaskLoadPortForPWA500W(int nIndexOfTask, string strTaskName)
            : base(nIndexOfTask, strTaskName, new TaskLoadPortRecovery500W(strTaskName, nIndexOfTask))
        {
            ScenarioTypeToCarrierLoad = ScenarioListTypes.SCENARIO_PORT_STATUS_LOAD_1 + LoadPortIndex;

            _scenarioManager = ScenarioManagerForPWA500W_NRD.Instance;

            _recovery = _recoveryData as TaskLoadPortRecovery500W;
        }
        #endregion </Constructors>

        #region <Fields>
        private readonly Enum ScenarioTypeToCarrierLoad;
        private const int CarrierMaxCapacity = 25;
        private const int ProcessModuleIndex = 0;       // 2025.01.07. by dwlim [ADD] Loading Mode를 안쓰는 안쓰는 Process Module이 있어서, 구분 위한 추가

        private CommandResults _commandResult = new CommandResults("", CommandResult.Invalid);
        private static TaskLoadPortRecovery500W _recovery;

        private static ScenarioManagerForPWA500W_NRD _scenarioManager = null;
        #endregion </Fields>

        #region <Properties>
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
            int relIndex = lpIndex * 4;
            indexOfDigital = (int)Define.DefineEnumProject.DigitalIO.PWA500W.EN_DIGITAL_IN.LP1_RUN + relIndex;

            return true;
        }
        protected override void ExecuteAtAlways()
        {
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

            return false;
        }
        protected override CommandResults ExecuteScenarioToCarrierIdRead()
        {
            _commandResult.CommandResult = CommandResult.Completed;
            return _commandResult;
        }
        protected override bool UpdateParamToIdVarification()
        {
            if (false == _scenarioOperator.UseScenario)
                return false;

            if (false == _carrierServer.HasCarrier(PortId))
                return false;

            return false;
        }
        protected override CommandResults ExecuteScenarioToIdVarification()
        {
            _commandResult.CommandResult = CommandResult.Completed;
            return _commandResult;
        }
        protected override bool UpdateParamToSlotMapVarification()
        {
            return false;
        }
        protected override CommandResults ExecuteToSlotMapVarification()
        {
            _commandResult.CommandResult = CommandResult.Completed;
            return _commandResult;
        }
        protected override bool EnqueueScenraioBeforeActionCompletion(out QueuedScenarioInfo scenarioInfo)
        {
            scenarioInfo = new QueuedScenarioInfo();
            return false;
        }
        protected override void ExecuteAfterScenarioCompletion(Enum scenario, EN_SCENARIO_RESULT result, Dictionary<string, string> scenarioParam, Dictionary<string, string> additionalParams)
        {
        }
        protected override bool UpdateParamToLoadCarrier()
        {
            switch (MySubstrateType)
            {
                case SubstrateType.Core_8:
                    {
                        int portId = _loadPortManager.GetLoadPortPortId((int)LoadPortType.Sort_12);
                        if (_carrierServer.HasCarrier(portId))
                            return false;

                        _commandResult.ActionName = ScenarioTypeToCarrierLoad.ToString();
                        _loadingMode = _loadPortManager.GetCarrierLoadingType(LoadPortIndex);
                        ScenarioListTypes typeOfScenario = (ScenarioListTypes)ScenarioTypeToCarrierLoad;

                        _scenarioManager.EnqueueScenarioCarrierHandlingAsync(PortId, _loadingMode, string.Empty, typeOfScenario);
                        return true;
                    }
                case SubstrateType.Core_12:
                    {
                        for (int i = 0; i < _loadPortManager.Count; ++i)
                        {
                            var substrateType = _scenarioManager.GetSubstrateTypeByLoadPortIndex(i);
                            if (false == substrateType.Equals(SubstrateType.Core_8))
                                continue;

                            int portId = _loadPortManager.GetLoadPortPortId(i);
                            if (_carrierServer.HasCarrier(portId))
                                return false;
                        }

                        _commandResult.ActionName = ScenarioTypeToCarrierLoad.ToString();
                        _loadingMode = _loadPortManager.GetCarrierLoadingType(LoadPortIndex);
                        ScenarioListTypes typeOfScenario = (ScenarioListTypes)ScenarioTypeToCarrierLoad;

                        _scenarioManager.EnqueueScenarioCarrierHandlingAsync(PortId, _loadingMode, string.Empty, typeOfScenario);
                        return true;
                    }
                case SubstrateType.Bin_12:
                    {
                        LoadPortLoadingMode loadingMode = LoadPortLoadingMode.Unknown;
                        for (int i = 0; i < _loadPortManager.Count; ++i)
                        {
                            var substrateType = _scenarioManager.GetSubstrateTypeByLoadPortIndex(i);
                            if (false == substrateType.Equals(SubstrateType.Core_8) || false == substrateType.Equals(SubstrateType.Core_12))
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
                            case LoadPortLoadingMode.Foup:
                                {
                                    binLotId = CarrierLotIdType.PEMAC.ToString();
                                }
                                break;
                            default:
                                return false;
                        }

                        _commandResult.ActionName = ScenarioTypeToCarrierLoad.ToString();
                        _loadingMode = _loadPortManager.GetCarrierLoadingType(LoadPortIndex);
                        ScenarioListTypes typeOfScenario = (ScenarioListTypes)ScenarioTypeToCarrierLoad;

                        _scenarioManager.EnqueueScenarioCarrierHandlingAsync(PortId, _loadingMode, binLotId, typeOfScenario);
                        return true;
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
        }
        protected override bool UpdateParamToUnloadCarrier()
        {
            return false;
        }
        protected override CommandResults ExecuteScenarioToUnloadCarrier()
        {
            _commandResult.CommandResult = CommandResult.Completed;
            return _commandResult;
        }
        protected override CommandResults WriteCarrierId()
        {
            _commandResult.CommandResult = CommandResult.Completed;
            return _commandResult;
        }
        protected override CommandResults WriteLotId()
        {
            _commandResult.CommandResult = CommandResult.Completed;
            return _commandResult;
        }
        protected override CommandResults ExecuteAfterWriting()
        {
            _commandResult.CommandResult = CommandResult.Completed;
            return _commandResult;
        }
        #endregion </Overrides>

        #region <Internal Interfaces>
        #endregion </Internal Interfaces>

        #endregion </Methods>
    }

    class TaskLoadPortRecovery500W : Work.RecoveryData
    {
        public TaskLoadPortRecovery500W(string taskName, int nPortCount)
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
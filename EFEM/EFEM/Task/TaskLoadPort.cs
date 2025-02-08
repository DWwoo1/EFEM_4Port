using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Concurrent;

using RunningTask_;

using FrameOfSystem3.Functional;
using FrameOfSystem3.DynamicLink_;
using FrameOfSystem3.Log;
using FrameOfSystem3.Work;
using FrameOfSystem3.SubSequence;

using Define.DefineEnumBase.Common;
using Define.DefineEnumBase.Log;
using Define.DefineEnumProject.Map;
using Define.DefineEnumProject.Task;
using Define.DefineEnumProject.Task.LoadPort;
using Define.DefineEnumProject.Mail;
using Define.DefineEnumProject.SubSequence;
using Define.DefineEnumProject.Tool;

using EFEM.Modules;
using EFEM.Defines.Common;
using EFEM.Defines.LoadPort;
using EFEM.Modules.LoadPort;
using EFEM.MaterialTracking;
using EFEM.ActionScheduler;
using EFEM.ActionScheduler.LoadPortActionSchedulers;

using FrameOfSystem3.SECSGEM;
using FrameOfSystem3.SECSGEM.DefineSecsGem;

namespace FrameOfSystem3.Task
{
    abstract class TaskLoadPort : RunningTaskWrapper
    {
        #region constructor
        public TaskLoadPort(int nIndexOfTask, string strTaskName, RecoveryData recovery)
            : base(nIndexOfTask, strTaskName, typeof(PARAM_PROCESS))
        {
            _taskOperator = TaskOperator.GetInstance();

            _recoveryData = recovery;
            //_recovery = new TaskLoadPortRecovery(strTaskName, 1);
            AddRecoveryData(strTaskName, _recoveryData);

            _loadPortManager = LoadPortManager.Instance;
            _carrierServer = CarrierManagementServer.Instance;
            _loadPortSchedulerManager = LoadPortActionSchedulerManager.Instance;
            _rfidManager = RFIDManager.Instance;
            _substrateManager = SubstrateManager.Instance;

            if (Enum.TryParse(strTaskName, out EN_TASK_LIST taskType))
            {
                LoadPortIndex = (int)taskType - (int)EN_TASK_LIST.LoadPort1;
                TaskIndex = (int)taskType;
            }

            PortId = _loadPortManager.GetLoadPortPortId(LoadPortIndex);

            #region <Attach Deligates>
            _loadPortManager.AttachMechanicalButtonEventHandlers(LoadPortIndex, LoadPortButtonTypes.Load,
                () => { CallbackLoadButtonClicked(); });
            _loadPortManager.AttachMechanicalButtonEventHandlers(LoadPortIndex, LoadPortButtonTypes.Unload,
                () => { CallbackUnloadButtonClicked(); });

            int busyStatusIndex = 0;
            if (GetBusyIndex(LoadPortIndex, ref busyStatusIndex))
            {
                _loadPortManager.AttachBusySignalByDigitalInput(LoadPortIndex, busyStatusIndex, DigitalIO_.DigitalIO.GetInstance().ReadInput);
            }
            #endregion </Attach Deligates>

            #region <Parallel I/O>
            _loadPortManager.AssignAMHSSignalControlFunctions(LoadPortIndex, 
                DigitalIO_.DigitalIO.GetInstance().ReadInput,
                DigitalIO_.DigitalIO.GetInstance().ReadOutput,
                DigitalIO_.DigitalIO.GetInstance().WriteOutput);
            #endregion </Parallel I/O>


            GetAtmRobotTaskName(out List<string> taskNames);
            RobotTaskNames = new List<string>(taskNames);

            _scenarioOperator = ScenarioOperator.Instance;
        }
        protected override void MakeMappingTableForAction()
        {
            foreach (TASK_ACTION enAction in Enum.GetValues(typeof(TASK_ACTION)))
            {
                m_mapppingForAction.Add(enAction.ToString(), enAction);
            }
        }
        #endregion constructor

        #region field

        #region default

        #region instance
        static TaskOperator _taskOperator = null;
        protected static RecoveryData _recoveryData = null;
        //protected static TaskLoadPortRecovery _recovery = null;
        #endregion /instance

        TASK_ACTION m_enAction = TASK_ACTION.STOP;
        Dictionary<string, TASK_ACTION> m_mapppingForAction = new Dictionary<string, TASK_ACTION>();
        #endregion /default

        #region <Alarm Sub Info>
        #endregion </Alarm Sub Info>

        #region <LoadPort>
        private bool _prevCarrierPresenceStatus = false;
        //private CarrierAccessStates _prevCarrierAccessStatus = CarrierAccessStates.Unknown;

        protected readonly int LoadPortIndex;
        protected readonly int PortId;
        protected bool _prevInitializationState = false;
        protected bool _prevPlacementErrorState = false;
        protected bool _prevCarrierOutErrorState = false;
        protected bool _prevTriggeredAlarm = false;
        protected string _triggeredAlarmDescription = string.Empty;
        //protected readonly Dictionary<LoadPortLoadingMode, int> LoadPortModeSignals = null;
        //protected readonly int CarrierPresenceIndex;

        //private readonly LoadPortController _loadPortController = null;
        //private readonly LoadPortStateInformation LoadPortState = new LoadPortStateInformation();
        protected static LoadPortManager _loadPortManager = null;
        protected static CarrierManagementServer _carrierServer = null;
        protected static LoadPortActionSchedulerManager _loadPortSchedulerManager = null;
        protected static RFIDManager _rfidManager = null;
        protected static SubstrateManager _substrateManager = null;

        protected LoadPortLoadingMode _loadingMode;
        protected Func<int, CommandResults> _manualActionToExecute = null;
        protected readonly int TaskIndex;
        protected RunningMain_.TaskData _taskData;

        protected bool _receivedCancelCarrier = false;
        protected const string Initialize = "INITIALIZE";        

        private QueuedScenarioInfo _executingScenario;
        private CommandResults _commandResult = new CommandResults(string.Empty, CommandResult.Invalid);
        private readonly Queue<QueuedScenarioInfo> QueuedScenario = new Queue<QueuedScenarioInfo>();
        private readonly TickCounter_.TickCounter TicksForHandling = new TickCounter_.TickCounter();
        protected const int DefualtWaitTime = 180000;

        private readonly List<string> RobotTaskNames = null;
        #endregion </LoadPort>

        #endregion /field

        #region <Properties>
        protected bool UseSecsGem
        {
            get
            {
                return _recipe.GetValue(Recipe.EN_RECIPE_TYPE.COMMON, Recipe.PARAM_COMMON.UseSecsGem.ToString(), true);
            }
        }        
        #endregion </Properties>

        #region inherit

        #region sequence
        protected override bool DoInitializeSequence()
        {
            if (_taskOperator.IsFinishingMode())
                return true;

            switch (m_nSeqNum)
            {
                case (int)STEP_INITIALIZE.START:
                    Views.Functional.Form_ProgressBar.GetInstance().ShowForm(GetTaskName(), (uint)STEP_INITIALIZE.END);
                    InitTemporaryData();
                    InitializeDynamicLinkState();
                    _loadPortManager.InitLoadPortAction(LoadPortIndex);

                    Recipe.PARAM_EQUIPMENT param = Recipe.PARAM_EQUIPMENT.UseLoadPort1 + LoadPortIndex;
                    bool useLoadPort = Recipe.Recipe.GetInstance().GetValue(Recipe.EN_RECIPE_TYPE.EQUIPMENT,
                        param.ToString(), 0, Recipe.EN_RECIPE_PARAM_TYPE.VALUE,
                        true);

                    if (false == CheckControllerConnectionStatus())
                        return true;

                    _loadPortManager.SetLoadPortEnabled(LoadPortIndex, useLoadPort);
                    m_nSeqNum = (int)STEP_INITIALIZE.PREPARE;
                    break;

                case (int)STEP_INITIALIZE.PREPARE:
                    {
                        // 2025.01.05. jhlim [MOD] TaskList를 프로젝트마다 따로 가져감에 따라, 아래 구문은 상속 프로젝트에서 이름을 가져와 검사하도록 변경
                        if (false == WaitSignalFromRobot())
                            break;
                        //if (false == WaitSignal(EN_TASK_LIST.AtmRobot.ToString()))
                        //    break;
                        // 2025.01.05. jhlim [END]

                        if (_loadPortManager.IsLoadPortBusy(LoadPortIndex))
                            break;

                        m_nSeqNum = (int)STEP_INITIALIZE.CHANGE_MODE;
                    }
                    break;

                case (int)STEP_INITIALIZE.CHANGE_MODE:
                    {
                        var result = _loadPortManager.FindCurrentLoadingMode(LoadPortIndex);
                        switch (result.CommandResult)
                        {
                            case CommandResult.Proceed:
                                break;
                            case CommandResult.Completed:
                                ++m_nSeqNum;
                                break;
                            case CommandResult.Skipped:
                            case CommandResult.Timeout:
                            case CommandResult.Error:
                            case CommandResult.Invalid:
                                {
                                    string[] arAlarmSubInfo = { GetTaskName(), "Find Loading Mode : Failed" };
                                    GenerateSequenceAlarm((int)EN_ALARM.LOADPORT_FAILED_TO_ACTION, false, ref arAlarmSubInfo);
                                    m_nSeqNum = (int)STEP_INITIALIZE.END;
                                }
                                break;

                            default:
                                break;
                        }
                    }
                    break;

                case (int)STEP_INITIALIZE.CHANGE_MODE + 1:
                    {
                        // 2025.01.05. jhlim [DEL] 아래 구문도 의미가 없다.
                        //LoadPortLoadingMode loadingType = LoadPortLoadingMode.Unknown;
                        //CommandResults result;
                        //if (IsAnyCarrierPlaced(ref loadingType))
                        //{
                        //    if (loadingType == LoadPortLoadingMode.Unknown)
                        //    {
                        //        string[] arAlarmSubInfo = { GetTaskName(), "Change Mode : Unknown Loading Type" };
                        //        GenerateSequenceAlarm((int)EN_ALARM.LOADPORT_FAILED_TO_ACTION, false, ref arAlarmSubInfo);
                        //        m_nSeqNum = (int)STEP_INITIALIZE.END;
                        //        break;
                        //    }
                        //}
                        //else
                        //{
                        //    loadingType = _loadPortManager.GetCarrierLoadingType(LoadPortIndex);
                        //}
                        // 2025.01.05. jhlim [END]

                        var loadingType = _loadPortManager.GetCarrierLoadingType(LoadPortIndex);
                        var result = _loadPortManager.ChangeLoadPortMode(LoadPortIndex, loadingType);
                        switch (result.CommandResult)
                        {
                            case CommandResult.Completed:
                                {
                                    m_nSeqNum = (int)STEP_INITIALIZE.INITIALIZE;
                                }
                                break;

                            case CommandResult.Timeout:
                            case CommandResult.Error:
                            case CommandResult.Invalid:
                                {
                                    string[] arAlarmSubInfo = { GetTaskName(), string.Format("Change Mode {0} - {1}", result.ActionName, result.Description) };
                                    GenerateSequenceAlarm((int)EN_ALARM.LOADPORT_FAILED_TO_ACTION, false, ref arAlarmSubInfo);
                                    m_nSeqNum = (int)STEP_INITIALIZE.END;
                                }
                                break;

                            default:
                                break;
                        }
                    }
                    break;

                case (int)STEP_INITIALIZE.INITIALIZE:
                    {
                        var result = _loadPortManager.InitializeLoadPort(LoadPortIndex);
                        switch (result.CommandResult)
                        {
                            case CommandResult.Completed:
                                {
                                    m_nSeqNum = (int)STEP_INITIALIZE.UPDATE_LINK;
                                }
                                break;

                            case CommandResult.Timeout:
                            case CommandResult.Error:
                            case CommandResult.Invalid:
                                {
                                    string[] arAlarmSubInfo = { GetTaskName(), string.Format("Initialize {0} - {1}", result.ActionName, result.Description) };
                                    GenerateSequenceAlarm((int)EN_ALARM.LOADPORT_FAILED_TO_ACTION, false, ref arAlarmSubInfo);
                                    m_nSeqNum = (int)STEP_INITIALIZE.END;
                                }
                                break;

                            default:
                                break;
                        }
                    }
                    break;

                case (int)STEP_INITIALIZE.UPDATE_LINK:
                    if (IsSimulation())
                    {
                        _taskOperator.TriggerLoadPortPlacedForSimul(PortId);
                    }
                    m_nSeqNum = (int)STEP_INITIALIZE.END;
                    break;

                case (int)STEP_INITIALIZE.END:
                    return true;
            }

            Views.Functional.Form_ProgressBar.GetInstance().UpdateStep(GetTaskName(), (uint)m_nSeqNum);
            return false;
        }
        protected override bool DoEntrySequence()
        {
            if (_taskOperator.IsFinishingMode())
                return true;

            switch (m_nSeqNum)
            {
                case (int)STEP_ENTRY.START:
                    InitTemporaryData();
                    InitializeDynamicLinkState();
                    m_nSeqNum = (int)STEP_ENTRY.PREPARE;
                    break;
                case (int)STEP_ENTRY.PREPARE:
                    if (false == CheckControllerConnectionStatus())
                        return true;

                    m_nSeqNum = (int)STEP_ENTRY.UPDATE_LINK;
                    break;

                case (int)STEP_ENTRY.UPDATE_LINK:
                    SetCarrierPortState(CARRIER_PORT_TYPE.SELECTION);
                    m_nSeqNum = (int)STEP_ENTRY.END;
                    break;

                case (int)STEP_ENTRY.END:
                    return true;
            }

            return false;
        }
        protected override bool DoSetupSequence()
        {
            base.DoSetupSequence();

            switch (m_enAction)
            {
                case TASK_ACTION.SCHEDULING:
                    return ActionScheduling();

                case TASK_ACTION.WAIT_FOR_LOADING:
                    return ActionWaitForLoading();

                case TASK_ACTION.WAIT_FOR_UNLOADING:
                    return ActionWaitForUnloading();

                case TASK_ACTION.CARRIER_LOADING:
                    return ActionCarrierLoading(true);

                case TASK_ACTION.CARRIER_UNLOADING:
                    return ActionCarrierUnloading(true);

                #region <Manual Only>
                case TASK_ACTION.CHANGE_LOADPORT_LOADING_MODE_TO_FOUP:
                case TASK_ACTION.CHANGE_LOADPORT_LOADING_MODE_TO_CASSETTE:
                case TASK_ACTION.CHANGE_LOADPORT_ACCESSE_MODE_TO_AUTO:
                case TASK_ACTION.CHANGE_LOADPORT_ACCESSE_MODE_TO_MANUAL:
                case TASK_ACTION.CARRIER_CLAMPING:
                case TASK_ACTION.CARRIER_UNCLAMPING:
                case TASK_ACTION.CARRIER_DOCKING:
                case TASK_ACTION.CARRIER_UNDOCKING:
                case TASK_ACTION.CARRIER_OPENING:
                case TASK_ACTION.CARRIER_CLOSING:
                case TASK_ACTION.CHANGE_CARRIER_ACCESS_STATUS_TO_NOT_ACCESSED:
                case TASK_ACTION.CHANGE_CARRIER_ACCESS_STATUS_TO_IN_ACCESSED:
                case TASK_ACTION.CHANGE_CARRIER_ACCESS_STATUS_TO_CARRIER_COMPLETED:
                case TASK_ACTION.CHANGE_CARRIER_ACCESS_STATUS_TO_CARRIER_STOPPED:
                case TASK_ACTION.INITIALIZE:
                case TASK_ACTION.RESET:
                    return ActionExecuteManually(m_enAction);
                #endregion </Manual Only>

                default:
                    return false;
            }
        }
        protected override bool DoExecutingSequence()
        {
            base.DoExecutingSequence();

            switch (m_enAction)
            {
                case TASK_ACTION.SCHEDULING:
                    return ActionScheduling();

                case TASK_ACTION.WAIT_FOR_LOADING:
                    return ActionWaitForLoading();

                case TASK_ACTION.WAIT_FOR_UNLOADING:
                    return ActionWaitForUnloading();

                case TASK_ACTION.CARRIER_LOADING:
                    return ActionCarrierLoading();

                case TASK_ACTION.CARRIER_UNLOADING:
                    return ActionCarrierUnloading();

                #region <Manual Only>
                case TASK_ACTION.CHANGE_LOADPORT_LOADING_MODE_TO_FOUP:
                case TASK_ACTION.CHANGE_LOADPORT_LOADING_MODE_TO_CASSETTE:
                case TASK_ACTION.CHANGE_LOADPORT_ACCESSE_MODE_TO_AUTO:
                case TASK_ACTION.CHANGE_LOADPORT_ACCESSE_MODE_TO_MANUAL:
                case TASK_ACTION.CARRIER_CLAMPING:
                case TASK_ACTION.CARRIER_UNCLAMPING:
                case TASK_ACTION.CARRIER_DOCKING:
                case TASK_ACTION.CARRIER_UNDOCKING:
                case TASK_ACTION.CARRIER_OPENING:
                case TASK_ACTION.CARRIER_CLOSING:
                case TASK_ACTION.CHANGE_CARRIER_ACCESS_STATUS_TO_NOT_ACCESSED:
                case TASK_ACTION.CHANGE_CARRIER_ACCESS_STATUS_TO_IN_ACCESSED:
                case TASK_ACTION.CHANGE_CARRIER_ACCESS_STATUS_TO_CARRIER_COMPLETED:
                case TASK_ACTION.CHANGE_CARRIER_ACCESS_STATUS_TO_CARRIER_STOPPED:
                case TASK_ACTION.INITIALIZE:
                case TASK_ACTION.RESET:
                    return ActionExecuteManually(m_enAction);
                #endregion </Manual Only>

                default:
                    return false;
            }
        }
        protected override void DoAlwaysSequence()
        {
            if (_loadPortManager != null)
            {
                if (_taskOperator.GetTaskInformation(TaskIndex, ref _taskData) &&
                    false == _taskData.strTaskState.Equals(Initialize))
                {
                    if (_prevInitializationState != _loadPortManager.GetInitializationState(LoadPortIndex))
                    {
                        _prevInitializationState = !_prevInitializationState;

                        if (false == _prevInitializationState)
                        {
                            GenerateAlarm((int)EN_ALARM.LOADPORT_HAS_NOT_BEEN_INITIALIZED);
                        }
                    }
                }

                if (_prevCarrierOutErrorState != _loadPortManager.HasErrorStatusByCarrierOut(LoadPortIndex))
                {
                    _prevCarrierOutErrorState = !_prevCarrierOutErrorState;

                    if (_prevCarrierOutErrorState)
                    {
                        GenerateAlarm((int)EN_ALARM.LOADPORT_CARRIER_OUT_ERROR);
                    }
                }

                if (_prevPlacementErrorState != _loadPortManager.HasErrorStatusByPlacementError(LoadPortIndex))
                {
                    _prevPlacementErrorState = !_prevPlacementErrorState;

                    if (_prevPlacementErrorState)
                    {
                        GenerateAlarm((int)EN_ALARM.LOADPORT_PLACEMENT_ERROR);
                    }
                }

                if (_prevTriggeredAlarm != _loadPortManager.HasTriggeredAlarm(LoadPortIndex, ref _triggeredAlarmDescription))
                {
                    _prevTriggeredAlarm = !_prevTriggeredAlarm;

                    if (_prevTriggeredAlarm)
                    {
                        GenerateAlarm((int)EN_ALARM.LOADPORT_HAS_ALARM, _triggeredAlarmDescription);
                        System.Threading.Tasks.Task.Run(() => ExecuteResetCommandAsync());
                    }
                }

                ExecuteAtAlways();
            }

            if (_prevCarrierPresenceStatus != _carrierServer.HasCarrier(PortId))
            {
                _prevCarrierPresenceStatus = _carrierServer.HasCarrier(PortId);
                if (_prevCarrierPresenceStatus)
                {
                    ExecuteOnCarrierPlaced();
                }
                else
                {
                    ExecuteOnCarrierRemoved();
                }
            }
            if (_prevCarrierPresenceStatus)
            {
                //if (false == _carrierServer.GetCarrierAccessingStatus(PortId).Equals(_prevCarrierAccessStatus))
                {
                    //_prevCarrierAccessStatus = _carrierServer.GetCarrierAccessingStatus(PortId);
                    OnCarrierAccessStatusChanged(_carrierServer.GetCarrierAccessingStatus(PortId));
                }
            }
        }
        /// <summary>
        /// 2020.06.02 by yjlee [ADD] Code the sequence for exit.
        /// - Before returning 'true', it will be called continuously.
        /// </summary>
        protected override bool DoExitSequence()
        {
            switch (m_nSeqNum)
            {
                case (int)STEP_EXIT.START:
                    if (_taskOperator.IsDryRunMode()
                        /*|| _recipe.GetValue(Recipe.EN_RECIPE_TYPE.COMMON, Recipe.PARAM_COMMON.UseCycleMode.ToString(), false)*/)
                    {
                        m_nSeqNum = (int)STEP_EXIT.CLEAR_DUMMY_DATA;
                    }
                    else
                    {
                        m_nSeqNum = (int)STEP_EXIT.END;
                    }
                    break;

                case (int)STEP_EXIT.CLEAR_DUMMY_DATA:
                    {
                        RunningMain_.TaskData tData = null;
                        if (false == _taskOperator.GetTaskInformation((int)EN_TASK_LIST.AtmRobot, ref tData))
                            break;

                        if (tData.strTaskState.Equals("RUN"))
                            break;

                        ++m_nSeqNum;
                    }
                    break;

                case (int)STEP_EXIT.CLEAR_DUMMY_DATA + 1:
                    var result = _loadPortManager.UnloadCarrierAtLoadPort(LoadPortIndex);
                    switch (result.CommandResult)
                    {
                        case CommandResult.Completed:
                            {
                                _loadPortManager.RecreateCarrierAtLoadPort(LoadPortIndex);
                                m_nSeqNum = (int)STEP_EXIT.END;
                            }
                            break;
                        case CommandResult.Timeout:
                        case CommandResult.Error:
                        case CommandResult.Invalid:
                            {
                                string[] arAlarmSubInfo = { GetTaskName(), string.Format("Unload {0} - {1}", result.ActionName, result.Description) };
                                GenerateSequenceAlarm((int)EN_ALARM.LOADPORT_FAILED_TO_ACTION, false, ref arAlarmSubInfo);
                                m_nSeqNum = (int)STEP_INITIALIZE.END;
                            }
                            break;

                        default:
                            ++m_nSeqNum;
                            break;
                    }
                    break;

                case (int)STEP_EXIT.CLEAR_DUMMY_DATA + 2:
                    --m_nSeqNum;
                    break;

                case (int)STEP_EXIT.END:
                    return true;
            }

            return false;
        }
        #endregion /sequence

        #region dynamic link
        /// <summary>
        /// 2020.06.02 by yjlee [ADD] Check whether a sequence is existent or not.
        /// </summary>
        protected override bool UpdateActionName(string actionName)
        {
            if (false == m_mapppingForAction.ContainsKey(actionName))
            {
                m_enAction = TASK_ACTION.STOP;
                return false;
            }

            m_enAction = m_mapppingForAction[actionName];
            return true;
        }
        /// <summary>
        /// Action pre condition과 flow post condition을 설정함.
        /// </summary>
        public override void InitializeActionCondition()
        {
        }
        protected override void DoExecutingPostcondition()
        {
            base.DoExecutingPostcondition();
        }
        #endregion /dynamic link

        #region sub sequence
        #endregion /sub sequence

        protected override bool CheckExternalDeviceStateIsIdle()
        {
            return (_loadPortManager.GetInitializationState(LoadPortIndex) &&
                false == _loadPortManager.HasErrorStatusByPlacementError(LoadPortIndex) &&
                false == _loadPortManager.HasErrorStatusByCarrierOut(LoadPortIndex));
        }
        #endregion /inherit

        #region <Abstract Methods>

        #region <Scenario>
        /// Id Read
        protected abstract bool UpdateParamToCarrierIdRead();
        protected abstract CommandResults ExecuteScenarioToCarrierIdRead();
        //

        // Id Verification
        protected abstract bool UpdateParamToIdVarification();
        protected abstract CommandResults ExecuteScenarioToIdVarification();
        //
        
        // Slot Verification
        protected abstract bool UpdateParamToSlotMapVarification();
        protected abstract CommandResults ExecuteToSlotMapVarification();
        // 
        
        // ETC
        protected abstract bool EnqueueScenraioBeforeActionCompletion(out QueuedScenarioInfo scenarioInfo);
        protected abstract void ExecuteAfterScenarioCompletion(Enum scenario, EN_SCENARIO_RESULT result, Dictionary<string, string> scenarioParam, Dictionary<string, string> additionalParams);
        //

        // Call OHT
        protected abstract bool UpdateParamToLoadCarrier();
        protected abstract CommandResults ExecuteScenarioToLoadCarrier();
        protected abstract bool UpdateParamToUnloadCarrier();
        protected abstract CommandResults ExecuteScenarioToUnloadCarrier();
        //
        #endregion </Scenario>

        #region <Carrier HandOff>
        protected abstract void OnCarrierAccessStatusChanged(CarrierAccessStates newAccessStatus);
        protected abstract void ExecuteOnCarrierPlaced();
        protected abstract void ExecuteOnCarrierRemoved();
        #endregion </Carrier HandOff>

        #region <RFID>
        protected abstract CommandResults WriteCarrierId();
        protected abstract CommandResults WriteLotId();
        protected abstract CommandResults ExecuteAfterWriting();
        #endregion </RFID>

        #region <Input/Output>
        protected abstract bool GetBusyIndex(int lpIndex, ref int indexOfDigital);
        #endregion </Input/Output>

        #region <Action>
        protected abstract void ExecuteAtAlways();
        protected abstract void GetAtmRobotTaskName(out List<string> taskNames);
        #endregion </Action>

        #endregion </Abstract Methods>

        #region action

        #region auto
        protected virtual bool ActionScheduling()
        {
            switch (m_nSeqNum)
            {
                case (int)STEP_SCHEDULING.START:
                    {
                        if (false == _loadPortManager.IsLoadPortEnabled(LoadPortIndex))
                        {
                            m_nSeqNum = (int)STEP_SCHEDULING.DUMMY_STEP;
                        }
                        else
                        {
                            m_nSeqNum = (int)STEP_SCHEDULING.CHECK_READY;
                        }
                    }
                    break;
                case (int)STEP_SCHEDULING.CHECK_READY:
                    {
                        if (_taskOperator.IsFinishingMode())
                        {
                            m_nSeqNum = (int)STEP_SCHEDULING.END;
                            break;
                        }

                        var currentPort = _loadPortSchedulerManager.ExecuteSchedulers(LoadPortIndex);
                        switch (currentPort)
                        {
                            case CARRIER_PORT_TYPE.SELECTION:
                                {
                                    ++m_nSeqNum;
                                }
                                break;
                            case CARRIER_PORT_TYPE.READY_TO_LOAD:
                            case CARRIER_PORT_TYPE.READY_TO_UNLOAD:
                            case CARRIER_PORT_TYPE.ACTION_LOAD:
                            case CARRIER_PORT_TYPE.ACTION_UNLOAD:
                                {
                                    SetCarrierPortState(currentPort);
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    m_nSeqNum = (int)STEP_SCHEDULING.CHECK_PORT;
                    break;

                case (int)STEP_SCHEDULING.DUMMY_STEP:
                    {
                        // 사용하지 않고 언도킹 중이면 언로드를 할 필요 없이 대기한다.
                        if (false == _loadPortManager.GetDockingState(LoadPortIndex))
                        {
                            m_nSeqNum = (int)STEP_SCHEDULING.DUMMY_STEP + 2;

                        }
                        else
                        {
                            _loadPortManager.InitLoadPortAction(LoadPortIndex);
                            ++m_nSeqNum;
                        }
                    }
                    break;

                case (int)STEP_SCHEDULING.DUMMY_STEP + 1:
                    {
                        var result = _loadPortManager.UnloadCarrierAtLoadPort(LoadPortIndex);
                        switch (result.CommandResult)
                        {
                            case CommandResult.Completed:
                                {
                                    m_nSeqNum = (int)STEP_SCHEDULING.DUMMY_STEP + 2;
                                }
                                break;
                            case CommandResult.Timeout:
                            case CommandResult.Error:
                            case CommandResult.Invalid:
                                {
                                    string[] arAlarmSubInfo = { GetTaskName(), string.Format("Unload {0} - {1} during scheduling", result.ActionName, result.Description) };
                                    GenerateSequenceAlarm((int)EN_ALARM.LOADPORT_FAILED_TO_ACTION, false, ref arAlarmSubInfo);
                                    m_nSeqNum = (int)STEP_SCHEDULING.END;
                                }
                                break;

                            default:
                                break;
                        }
                    }
                    break;

                case (int)STEP_SCHEDULING.DUMMY_STEP + 2:
                    {
                        if (_taskOperator.IsFinishingMode())
                        {
                            m_nSeqNum = (int)STEP_SCHEDULING.END;
                            break;
                        }
                        ++m_nSeqNum;
                    }
                    break;

                case (int)STEP_SCHEDULING.DUMMY_STEP + 3:
                    --m_nSeqNum;
                    break;

                case (int)STEP_SCHEDULING.CHECK_PORT:
                    if (false == IsPortNotChanged(CARRIER_PORT_TYPE.SELECTION))
                    {
                        m_nSeqNum = (int)STEP_SCHEDULING.END;
                    }
                    else
                    {
                        ++m_nSeqNum;
                    }
                    break;
                case (int)STEP_SCHEDULING.CHECK_PORT + 1:
                    m_nSeqNum = (int)STEP_SCHEDULING.CHECK_READY;
                    break;

                case (int)STEP_SCHEDULING.END:
                    return true;

                default:
                    return false;
            }

            return false;
        }
        protected virtual bool ActionWaitForLoading()
        {
            if (m_nSeqNum >= (int)STEP_WAIT_FOR_LOADING.START &&
                m_nSeqNum < (int)STEP_WAIT_FOR_LOADING.SAFTY_INTERLOCK_DETECTED)
            {
                if (IsSaftyInterLockError())
                {                    
                    m_nSeqNum = (int)STEP_WAIT_FOR_LOADING.SAFTY_INTERLOCK_DETECTED;
                    return false;
                }
            }

            switch (m_nSeqNum)
            {
                case (int)STEP_WAIT_FOR_LOADING.START:
                    if (false == _loadPortManager.IsLoadPortEnabled(LoadPortIndex))
                    {
                        m_nSeqNum = (int)STEP_WAIT_FOR_LOADING.END;
                        break;
                    }

                    if (_loadPortManager.GetAccessMode(LoadPortIndex).Equals(LoadPortAccessMode.Auto) &&
                        false == _taskOperator.IsDryRunMode())
                    {
                        _loadPortManager.InitializeAMHSSignals(LoadPortIndex);
                        m_nSeqNum = (int)STEP_WAIT_FOR_LOADING.CALL_OHT_CARRIER_TO_LOAD;
                    }
                    else
                    {
                        m_nSeqNum = (int)STEP_WAIT_FOR_LOADING.CHECK_READY;
                    }
                    break;

                case (int)STEP_WAIT_FOR_LOADING.CHECK_READY:
                    if (false == _loadPortManager.GetPresentState(LoadPortIndex) ||
                        false == _loadPortManager.GetPlacedState(LoadPortIndex))
                    {
                        ++m_nSeqNum;
                    }
                    else
                    {
                        m_nSeqNum = (int)STEP_WAIT_FOR_LOADING.UPDATE_LINK;
                    }
                    break;
                case (int)STEP_WAIT_FOR_LOADING.CHECK_READY + 1:
                    {
                        if (_taskOperator.IsFinishingMode())
                        {
                            m_nSeqNum = (int)STEP_WAIT_FOR_LOADING.END;
                            break;
                        }
                    }
                    --m_nSeqNum;
                    break;

                case (int)STEP_WAIT_FOR_LOADING.CALL_OHT_CARRIER_TO_LOAD:
                    {
                        // 정지 신호가 들어온 경우, 물류 보내기 전에 정지시킨다.(PIO 기다리는 중 정지의 경우 Skipped 처리되어 아래서 빠질 것)
                        if (_taskOperator.IsFinishingMode())
                        {
                            m_nSeqNum = (int)STEP_WAIT_FOR_LOADING.END;
                            break;
                        }

                        if (false == UseSecsGem || false == UpdateParamToLoadCarrier())
                        {
                            TicksForHandling.SetTickCount(1);
                            m_nSeqNum = (int)STEP_WAIT_FOR_LOADING.INTERFACE_BY_PIO;
                        }
                        else
                        {
                            ++m_nSeqNum;
                        }
                    }
                    break;

                case (int)STEP_WAIT_FOR_LOADING.CALL_OHT_CARRIER_TO_LOAD + 1:
                    {
                        var result = ExecuteScenarioToLoadCarrier();
                        switch (result.CommandResult)
                        {
                            case CommandResult.Completed:
                            case CommandResult.Skipped:
                                int waitTime = GetParameter(Recipe.PARAM_EQUIPMENT.HandlingWaitTime, DefualtWaitTime);
                                TicksForHandling.SetTickCount((uint)waitTime);
                                m_nSeqNum = (int)STEP_WAIT_FOR_LOADING.INTERFACE_BY_PIO;
                                break;
                            case CommandResult.Timeout:
                            case CommandResult.Error:
                            case CommandResult.Invalid:
                                {
                                    string[] arAlarmSubInfo = { GetTaskName(), string.Format("{0} - {1}", result.ActionName, result.Description) };
                                    GenerateSequenceAlarm((int)EN_ALARM.LOADPORT_HAS_FAILED_DURING_LOADING_HANDOFF, false, ref arAlarmSubInfo);
                                    m_nSeqNum = (int)STEP_WAIT_FOR_LOADING.UPDATE_LINK;
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    break;

                case (int)STEP_WAIT_FOR_LOADING.INTERFACE_BY_PIO:
                    {
                        var result = _loadPortManager.ExecuteAMHSHandlingToLoad(LoadPortIndex);
                        switch (result.CommandResult)
                        {
                            case CommandResult.Proceed:
                                ++m_nSeqNum;
                                break;

                            case CommandResult.Completed:                                
                            case CommandResult.Skipped: // 정지 신호에 의한 스킵
                                m_nSeqNum = (int)STEP_WAIT_FOR_LOADING.UPDATE_LINK;
                                break;
                                
                            default:
                                {
                                    string[] arAlarmSubInfo = { GetTaskName(), string.Format("{0} - {1}", result.ActionName, result.Description) };
                                    GenerateSequenceAlarm((int)EN_ALARM.LOADPORT_HAS_FAILED_DURING_LOADING_HANDOFF, false, ref arAlarmSubInfo);
                                    m_nSeqNum = (int)STEP_WAIT_FOR_LOADING.UPDATE_LINK;
                                }
                                break;
                        }
                    }
                    break;

                case (int)STEP_WAIT_FOR_LOADING.INTERFACE_BY_PIO + 1:
                    if (_taskOperator.IsSimulationMode())
                    {
                        SetDelayForSequence(500);
                    }

                    if (TicksForHandling.IsTickOver(true))
                    {
                        // Timeout에 의한 액션 재 실행
                        m_nSeqNum = (int)STEP_WAIT_FOR_LOADING.CALL_OHT_CARRIER_TO_LOAD;
                    }
                    else
                    {
                        --m_nSeqNum;
                    }
                    break;

                case (int)STEP_WAIT_FOR_LOADING.SAFTY_INTERLOCK_DETECTED:
                    {
                        _loadPortManager.WriteAMHSStopSignal(LoadPortIndex, true);
                        ++m_nSeqNum;
                    }
                    break;

                case (int)STEP_WAIT_FOR_LOADING.SAFTY_INTERLOCK_DETECTED + 1:
                    {
                        SetDelayForSequence(500);
                        GenerateAlarm((int)EN_ALARM.SAFTY_BAR_DETECTED_DURING_HANDOFF);
                        m_nSeqNum = (int)STEP_WAIT_FOR_LOADING.UPDATE_LINK;
                    }
                    break;

                case (int)STEP_WAIT_FOR_LOADING.UPDATE_LINK:
                    _loadPortManager.InitializeAMHSSignals(LoadPortIndex);
                    SetCarrierPortState(CARRIER_PORT_TYPE.SELECTION);
                    m_nSeqNum = (int)STEP_WAIT_FOR_LOADING.END;
                    break;

                case (int)STEP_WAIT_FOR_LOADING.END:
                    return true;

                default:
                    return false;
            }
            return false;
        }
        protected virtual bool ActionWaitForUnloading()
        {
            if (m_nSeqNum >= (int)STEP_WAIT_FOR_UNLOADING.START &&
                m_nSeqNum < (int)STEP_WAIT_FOR_UNLOADING.SAFTY_INTERLOCK_DETECTED)
            {
                if (IsSaftyInterLockError())
                {
                    m_nSeqNum = (int)STEP_WAIT_FOR_UNLOADING.SAFTY_INTERLOCK_DETECTED;
                    return false;
                }
            }

            switch (m_nSeqNum)
            {
                case (int)STEP_WAIT_FOR_UNLOADING.START:
                    if (false == _loadPortManager.IsLoadPortEnabled(LoadPortIndex))
                    {
                        m_nSeqNum = (int)STEP_WAIT_FOR_UNLOADING.END;
                        break;
                    }
                    if (_loadPortManager.GetAccessMode(LoadPortIndex).Equals(LoadPortAccessMode.Auto) &&
                        false == _taskOperator.IsDryRunMode())
                    {
                        _loadPortManager.InitializeAMHSSignals(LoadPortIndex);

                        // 2024.12.24. jhim [MOD] 캐리어 도착하자마자 바로 매뉴얼로 배출하는 경우 CarrierId, LotId가 없어 문제되니 Id를 읽고 하도록 개선
                        _rfidManager.InitAction(LoadPortIndex, _loadingMode);
                        m_nSeqNum = (int)STEP_WAIT_FOR_UNLOADING.READ_CARRIER_ID;
                    }
                    else
                    {
                        m_nSeqNum = (int)STEP_WAIT_FOR_UNLOADING.CHECK_READY;
                    }
                    break;

                case (int)STEP_WAIT_FOR_UNLOADING.READ_CARRIER_ID:
                    {
                        string carrierId = string.Empty;
                        var result = _rfidManager.ReadCarrierId(LoadPortIndex, _loadingMode, ref carrierId);
                        switch (result.CommandResult)
                        {
                            case CommandResult.Completed:
                                {
                                    // 적용
                                    _carrierServer.SetCarrierId(PortId, carrierId);
                                    _rfidManager.InitAction(LoadPortIndex, _loadingMode);
                                    m_nSeqNum = (int)STEP_WAIT_FOR_UNLOADING.READ_LOT_ID;
                                }
                                break;

                            case CommandResult.Timeout:
                            case CommandResult.Error:
                            case CommandResult.Invalid:
                                {
                                    string[] arAlarmSubInfo = { GetTaskName(), string.Format("RFID Reading Carrier Id {0} - {1}", result.ActionName, result.Description) };
                                    GenerateSequenceAlarm((int)EN_ALARM.RFID_READ_COMMAND_HAS_FAILED, false, ref arAlarmSubInfo);
                                    m_nSeqNum = (int)STEP_WAIT_FOR_UNLOADING.END;
                                }
                                break;

                            default:
                                break;
                        }
                    }
                    break;

                case (int)STEP_WAIT_FOR_UNLOADING.READ_LOT_ID:
                    {
                        string lotId = string.Empty;
                        var result = _rfidManager.ReadLotId(LoadPortIndex, _loadingMode, ref lotId);

                        switch (result.CommandResult)
                        {
                            case CommandResult.Completed:
                                {
                                    // 적용
                                    _carrierServer.SetCarrierLotId(PortId, lotId);

                                    if (_taskOperator.IsFinishingMode())
                                    {
                                        m_nSeqNum = (int)STEP_WAIT_FOR_UNLOADING.END;
                                        break;
                                    }

                                    m_nSeqNum = (int)STEP_WAIT_FOR_UNLOADING.CALL_OHT_CARRIER_TO_UNLOAD;
                                }
                                break;

                            case CommandResult.Timeout:
                            case CommandResult.Error:
                            case CommandResult.Invalid:
                                {
                                    string[] arAlarmSubInfo = { GetTaskName(), string.Format("RFID Reading Lot Id {0} - {1}", result.ActionName, result.Description) };
                                    GenerateSequenceAlarm((int)EN_ALARM.RFID_READ_COMMAND_HAS_FAILED, false, ref arAlarmSubInfo);
                                    m_nSeqNum = (int)STEP_WAIT_FOR_UNLOADING.END;
                                }
                                break;

                            default:
                                break;
                        }
                    }
                    break;



                case (int)STEP_WAIT_FOR_UNLOADING.CHECK_READY:
                    if (_loadPortManager.GetPresentState(LoadPortIndex)
                        || _loadPortManager.GetPlacedState(LoadPortIndex))
                    {
                        ++m_nSeqNum;
                        break;
                    }
                    m_nSeqNum = (int)STEP_WAIT_FOR_UNLOADING.UPDATE_LINK;
                    break;

                case (int)STEP_WAIT_FOR_UNLOADING.CHECK_READY + 1:
                    {
                        if (_taskOperator.IsFinishingMode())
                        {
                            m_nSeqNum = (int)STEP_WAIT_FOR_UNLOADING.END;
                            break;
                        }

                        if (_taskOperator.IsDryRunMode()
                            /*|| _recipe.GetValue(Recipe.EN_RECIPE_TYPE.COMMON, Recipe.PARAM_COMMON.UseCycleMode.ToString(), false)*/)
                        {
                            _loadPortManager.RecreateCarrierAtLoadPort(LoadPortIndex);
                            SetCarrierPortState(CARRIER_PORT_TYPE.ACTION_LOAD);
                            SetDelayForSequence(2000);
                            m_nSeqNum = (int)STEP_WAIT_FOR_UNLOADING.END;
                            break;
                        }
                    }
                    --m_nSeqNum;
                    break;

                case (int)STEP_WAIT_FOR_UNLOADING.CALL_OHT_CARRIER_TO_UNLOAD:
                    {
                        // 정지 신호가 들어온 경우, 물류 보내기 전에 정지시킨다.(PIO 기다리는 중 정지의 경우 Skipped 처리되어 아래서 빠질 것)
                        if (_taskOperator.IsFinishingMode())
                        {
                            m_nSeqNum = (int)STEP_WAIT_FOR_UNLOADING.UPDATE_LINK;
                        }

                        if (false == UseSecsGem || false == UpdateParamToUnloadCarrier())
                        {
                            TicksForHandling.SetTickCount(1);
                            m_nSeqNum = (int)STEP_WAIT_FOR_UNLOADING.INTERFACE_BY_PIO;
                        }
                        else
                        {
                            ++m_nSeqNum;
                        }
                    }
                    break;

                case (int)STEP_WAIT_FOR_UNLOADING.CALL_OHT_CARRIER_TO_UNLOAD + 1:
                    {
                        var result = ExecuteScenarioToUnloadCarrier();
                        switch (result.CommandResult)
                        {
                            case CommandResult.Completed:
                            case CommandResult.Skipped:
                                int waitTime = (int)GetParameter(Recipe.PARAM_EQUIPMENT.HandlingWaitTime, DefualtWaitTime);
                                TicksForHandling.SetTickCount((uint)waitTime);
                                m_nSeqNum = (int)STEP_WAIT_FOR_UNLOADING.INTERFACE_BY_PIO;
                                break;
                            case CommandResult.Timeout:
                            case CommandResult.Error:
                            case CommandResult.Invalid:
                                {
                                    string[] arAlarmSubInfo = { GetTaskName(), string.Format("{0} - {1}", result.ActionName, result.Description) };
                                    GenerateSequenceAlarm((int)EN_ALARM.LOADPORT_HAS_FAILED_DURING_UNLOADING_HANDOFF, false, ref arAlarmSubInfo);
                                    m_nSeqNum = (int)STEP_WAIT_FOR_UNLOADING.UPDATE_LINK;
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    break;

                case (int)STEP_WAIT_FOR_UNLOADING.INTERFACE_BY_PIO:
                    {
                        var result = _loadPortManager.ExecuteAMHSHandlingToUnload(LoadPortIndex);
                        switch (result.CommandResult)
                        {
                            case CommandResult.Proceed:
                                ++m_nSeqNum;
                                break;

                            case CommandResult.Completed:
                            case CommandResult.Skipped: // 정지 신호에 의한 스킵
                                m_nSeqNum = (int)STEP_WAIT_FOR_UNLOADING.UPDATE_LINK;
                                break;

                            default:
                                {
                                    string[] arAlarmSubInfo = { GetTaskName(), string.Format("{0} - {1}", result.ActionName, result.Description) };
                                    GenerateSequenceAlarm((int)EN_ALARM.LOADPORT_HAS_FAILED_DURING_UNLOADING_HANDOFF, false, ref arAlarmSubInfo);
                                    m_nSeqNum = (int)STEP_WAIT_FOR_UNLOADING.UPDATE_LINK;
                                }
                                break;
                        }
                    }
                    break;

                case (int)STEP_WAIT_FOR_UNLOADING.INTERFACE_BY_PIO + 1:
                    if (_taskOperator.IsSimulationMode())
                    {
                        SetDelayForSequence(500);
                    }

                    if (TicksForHandling.IsTickOver(true))
                    {
                        // Timeout에 의한 액션 재 실행
                        m_nSeqNum = (int)STEP_WAIT_FOR_UNLOADING.CALL_OHT_CARRIER_TO_UNLOAD;
                    }
                    else
                    {
                        --m_nSeqNum;
                    }
                    break;

                case (int)STEP_WAIT_FOR_UNLOADING.SAFTY_INTERLOCK_DETECTED:
                    {
                        _loadPortManager.WriteAMHSStopSignal(LoadPortIndex, true);
                        ++m_nSeqNum;
                    }
                    break;

                case (int)STEP_WAIT_FOR_UNLOADING.SAFTY_INTERLOCK_DETECTED + 1:
                    {
                        SetDelayForSequence(500);
                        GenerateAlarm((int)EN_ALARM.SAFTY_BAR_DETECTED_DURING_HANDOFF);
                        m_nSeqNum = (int)STEP_WAIT_FOR_UNLOADING.UPDATE_LINK;
                    }
                    break;

                case (int)STEP_WAIT_FOR_UNLOADING.UPDATE_LINK:
                    _loadPortManager.InitializeAMHSSignals(LoadPortIndex);
                    SetCarrierPortState(CARRIER_PORT_TYPE.SELECTION);
                    m_nSeqNum = (int)STEP_WAIT_FOR_UNLOADING.END;
                    break;


                case (int)STEP_WAIT_FOR_UNLOADING.END:
                    return true;

                default:
                    return false;
            }
            return false;
        }
        protected virtual bool ActionCarrierLoading(bool manual = false)
        {
            switch (m_nSeqNum)
            {
                case (int)STEP_CARRIER_LOADING.START:
                    if (false == _loadPortManager.IsLoadPortEnabled(LoadPortIndex))
                    {
                        m_nSeqNum = (int)STEP_CARRIER_LOADING.END;
                        break;
                    }

                    if (IsSimulation() && _recipe.GetValue(Recipe.EN_RECIPE_TYPE.COMMON, Recipe.PARAM_COMMON.UseSecsGem.ToString(), true))
                    {
                        //SetDelayForSequence(PortId * 2000);
                    }
                    m_nSeqNum = (int)STEP_CARRIER_LOADING.CHECK_READY;
                    break;

                case (int)STEP_CARRIER_LOADING.CHECK_READY:
                    {
                        if (_taskOperator.IsFinishingMode())
                        {
                            m_nSeqNum = (int)STEP_CARRIER_LOADING.END;
                            break;
                        }

                        _loadPortManager.InitLoadPortAction(LoadPortIndex);
                        if (manual)
                        {
                            SetDelayForSequence(500);   // 버튼 후딜레이가 있는듯.. 로드포트 비지 상태가 되어 바로 명령이 들어가면 Nack 된다.
                        }
                        
                        // 2024.12.23. jhlim [ADD] RFID 관련 플래그 초기화 위치 이동
                        _loadingMode = _loadPortManager.GetCarrierLoadingType(LoadPortIndex);
                        _rfidManager.InitAction(LoadPortIndex, _loadPortManager.GetCarrierLoadingType(LoadPortIndex));
                        // 2024.12.23. jhlim [END]
                        if (false == _loadPortManager.GetCarrierLoadingType(LoadPortIndex).Equals(LoadPortLoadingMode.Foup))
                        {
                            m_nSeqNum = (int)STEP_CARRIER_LOADING.READ_CARRIER_ID;
                        }
                        else
                        {
                            m_nSeqNum = (int)STEP_CARRIER_LOADING.CLAMP_CARRIER;
                        }
                    }
                    break;

                case (int)STEP_CARRIER_LOADING.CLAMP_CARRIER:
                    {
                        var result = _loadPortManager.ClampCarrierAtLoadPort(LoadPortIndex);
                        switch (result.CommandResult)
                        {
                            //case EN_COMMAND_RESULT.PROCEED:
                            //    break;
                            case CommandResult.Completed:
                                {
                                    if (_taskOperator.IsFinishingMode())
                                    {
                                        m_nSeqNum = (int)STEP_CARRIER_LOADING.END;
                                        break;
                                    }

                                    // 2024.12.23. jhlim [DEL] RFID 관련 플래그 초기화 위치 이동
                                    //_rfidManager.InitAction(LoadPortIndex, _loadingMode);
                                    // 2024.12.23. jhlim [END]

                                    m_nSeqNum = (int)STEP_CARRIER_LOADING.READ_CARRIER_ID;
                                }
                                break;

                            case CommandResult.Timeout:
                            case CommandResult.Error:
                            case CommandResult.Invalid:
                                {
                                    string[] arAlarmSubInfo = { GetTaskName(), string.Format("Clamp {0} - {1}", result.ActionName, result.Description) };
                                    GenerateSequenceAlarm((int)EN_ALARM.LOADPORT_FAILED_TO_ACTION, false, ref arAlarmSubInfo);
                                    m_nSeqNum = (int)STEP_CARRIER_LOADING.END;
                                }
                                break;

                            default:
                                break;
                        }
                    }
                    break;

                case (int)STEP_CARRIER_LOADING.READ_CARRIER_ID:
                    {
                        string carrierId = string.Empty;
                        var result = _rfidManager.ReadCarrierId(LoadPortIndex, _loadingMode, ref carrierId);
                        switch (result.CommandResult)
                        {
                            case CommandResult.Completed:
                                {
                                    // 적용
                                    _carrierServer.SetCarrierId(PortId, carrierId);
                                    _rfidManager.InitAction(LoadPortIndex, _loadingMode);
                                    m_nSeqNum = (int)STEP_CARRIER_LOADING.READ_LOT_ID;
                                }
                                break;

                            case CommandResult.Timeout:
                            case CommandResult.Error:
                            case CommandResult.Invalid:
                                {
                                    string[] arAlarmSubInfo = { GetTaskName(), string.Format("RFID Reading Carrier Id {0} - {1}", result.ActionName, result.Description) };
                                    GenerateSequenceAlarm((int)EN_ALARM.RFID_READ_COMMAND_HAS_FAILED, false, ref arAlarmSubInfo);
                                    m_nSeqNum = (int)STEP_CARRIER_LOADING.END;
                                }
                                break;

                            default:
                                break;
                        }
                    }
                    break;

                case (int)STEP_CARRIER_LOADING.READ_LOT_ID:
                    {
                        string lotId = string.Empty;
                        var result = _rfidManager.ReadLotId(LoadPortIndex, _loadingMode, ref lotId);
                        switch (result.CommandResult)
                        {
                            case CommandResult.Completed:
                                {
                                    // 적용
                                    _carrierServer.SetCarrierLotId(PortId, lotId);

                                    if (_taskOperator.IsFinishingMode())
                                    {
                                        m_nSeqNum = (int)STEP_CARRIER_LOADING.END;
                                        break;
                                    }

                                    m_nSeqNum = (int)STEP_CARRIER_LOADING.EXECUTE_SCENARIO_TO_ID_READ;
                                }
                                break;

                            case CommandResult.Timeout:
                            case CommandResult.Error:
                            case CommandResult.Invalid:
                                {
                                    string[] arAlarmSubInfo = { GetTaskName(), string.Format("RFID Reading Lot Id {0} - {1}", result.ActionName, result.Description) };
                                    GenerateSequenceAlarm((int)EN_ALARM.RFID_READ_COMMAND_HAS_FAILED, false, ref arAlarmSubInfo);
                                    m_nSeqNum = (int)STEP_CARRIER_LOADING.END;
                                }
                                break;

                            default:
                                break;
                        }
                    }
                    break;

                case (int)STEP_CARRIER_LOADING.EXECUTE_SCENARIO_TO_ID_READ:
                    {
                        if (false == UpdateParamToCarrierIdRead())
                        {
                            m_nSeqNum = (int)STEP_CARRIER_LOADING.CHECK_ID_VERIFICATION_BY_HOST;
                        }
                        else
                        {
                            ++m_nSeqNum;
                        }
                    }
                    break;
                case (int)STEP_CARRIER_LOADING.EXECUTE_SCENARIO_TO_ID_READ + 1:
                    {
                        var result = ExecuteScenarioToCarrierIdRead();
                        switch (result.CommandResult)
                        {
                            case CommandResult.Completed:
                            case CommandResult.Skipped:

                                if (_taskOperator.IsFinishingMode())
                                {
                                    m_nSeqNum = (int)STEP_CARRIER_LOADING.END;
                                    break;
                                }

                                // TODO : Id Read Update : NotRead -> WaitingForHost
                                m_nSeqNum = (int)STEP_CARRIER_LOADING.CHECK_ID_VERIFICATION_BY_HOST;
                                break;
                            case CommandResult.Timeout:
                            case CommandResult.Error:
                            case CommandResult.Invalid:                                
                                {
                                    GenerateAlarm((int)EN_ALARM.LOADPORT_FAILED_TO_EXECUTE_SCENARIO_ID_READ, result.ActionName);
                                    m_nSeqNum = (int)STEP_CARRIER_LOADING.END;
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    break;

                case (int)STEP_CARRIER_LOADING.CHECK_ID_VERIFICATION_BY_HOST:
                    {
                        if (false == UpdateParamToIdVarification())
                        {
                            m_nSeqNum = (int)STEP_CARRIER_LOADING.CHECK_ID_VERIFICATION_BY_HOST + 2;
                        }
                        else
                        {
                            ++m_nSeqNum;
                        }
                    }
                    break;
                case (int)STEP_CARRIER_LOADING.CHECK_ID_VERIFICATION_BY_HOST + 1:
                    {
                        var result = ExecuteScenarioToIdVarification();
                        switch (result.CommandResult)
                        {
                            case CommandResult.Completed:
                            case CommandResult.Skipped:
                                ++m_nSeqNum; 
                                break;

                            case CommandResult.Timeout:
                            case CommandResult.Error:
                            case CommandResult.Invalid:
                                {
                                    // TODO : Id Verification Update : WaitingForHost -> VerificationFailed
                                    GenerateAlarm((int)EN_ALARM.LOADPORT_FAILED_TO_EXECUTING_SCENARIO_ID_VERIFICATION, result.ActionName);
                                    m_nSeqNum = (int)STEP_CARRIER_LOADING.END;
                                }
                                break;

                            default:
                                break;
                        }
                    }
                    break;

                case (int)STEP_CARRIER_LOADING.CHECK_ID_VERIFICATION_BY_HOST + 2:
                    {
                        //
                        // TODO : Id Verification Update : WaitingForHost -> VerificationOk
                        //
                        if (_taskOperator.IsFinishingMode())
                        {
                            m_nSeqNum = (int)STEP_CARRIER_LOADING.END;
                            break;
                        }

                        m_nSeqNum = (int)STEP_CARRIER_LOADING.LOAD_CARRIER;
                    }
                    break;

                case (int)STEP_CARRIER_LOADING.LOAD_CARRIER:
                    {
                        var result = _loadPortManager.LoadCarrierAtLoadPort(LoadPortIndex);
                        switch (result.CommandResult)
                        {
                            case CommandResult.Completed:
                                {
                                    if (_taskOperator.IsDryRunMode())
                                    {
                                        _loadPortSchedulerManager.ChangeSlotMapForDryRun(LoadPortIndex);
                                    }

                                    m_nSeqNum = (int)STEP_CARRIER_LOADING.CHECK_SLOT_VALIDITY;
                                }
                                break;

                            case CommandResult.Timeout:
                            case CommandResult.Error:
                            case CommandResult.Invalid:
                                {
                                    string[] arAlarmSubInfo = { GetTaskName(), string.Format("Load {0} - {1}", result.ActionName, result.Description) };
                                    GenerateSequenceAlarm((int)EN_ALARM.LOADPORT_FAILED_TO_ACTION, false, ref arAlarmSubInfo);
                                    m_nSeqNum = (int)STEP_CARRIER_LOADING.END;
                                }
                                break;

                            default:
                                break;
                        }
                    }
                    break;

                case (int)STEP_CARRIER_LOADING.CHECK_SLOT_VALIDITY:
                    {
                        if (_loadPortManager.GetCarrierLoadingType(LoadPortIndex).Equals(LoadPortLoadingMode.Cassette))
                        {
                            var slots = _carrierServer.GetCarrierSlotMap(PortId);
                            if (slots == null)
                                break;

                            // TODO : 슬롯 사용유무가 필요할거같다.
                            if (slots[0].Equals(CarrierSlotMapStates.CorrectlyOccupied))
                            {
                                m_nSeqNum = (int)STEP_CARRIER_LOADING.UNLOAD_CARRIER_BY_ERROR;
                                break;
                            }

                            // TODO : 슬롯 상태가 비정상이면 또한 에러로 처리필요

                            for (int i = 0; i < slots.Length; ++i)
                            {
                                switch (slots[i])
                                {
                                    case CarrierSlotMapStates.Undefined:
                                    case CarrierSlotMapStates.NotEmpty:
                                    case CarrierSlotMapStates.DoubleSlotted:
                                    case CarrierSlotMapStates.CrossSlotted:
                                        {
                                            m_nSeqNum = (int)STEP_CARRIER_LOADING.UNLOAD_CARRIER_BY_ERROR;
                                            return false;
                                        }

                                    case CarrierSlotMapStates.Empty:
                                    case CarrierSlotMapStates.CorrectlyOccupied:
                                        break;
                                    default:
                                        break;
                                }
                            }

                        }

                        // TODO : Slot Verification Update : NotRead -> WaitingForHost
                        m_nSeqNum = (int)STEP_CARRIER_LOADING.CHECK_SLOTMAP_VERIFICATION_BY_HOST;
                    }
                    break;

                case (int)STEP_CARRIER_LOADING.CHECK_SLOTMAP_VERIFICATION_BY_HOST:
                    {
                        _receivedCancelCarrier = false;
                        if (false == UpdateParamToSlotMapVarification())
                        {
                            m_nSeqNum = (int)STEP_CARRIER_LOADING.CHECK_SLOTMAP_VERIFICATION_BY_HOST + 2;
                        }
                        else
                        {
                            ++m_nSeqNum;
                        }
                    }
                    break;
                case (int)STEP_CARRIER_LOADING.CHECK_SLOTMAP_VERIFICATION_BY_HOST + 1:
                    {
                        var result = ExecuteToSlotMapVarification();
                        switch (result.CommandResult)
                        {
                            case CommandResult.Completed:
                            case CommandResult.Skipped:
                                {
                                    if (_receivedCancelCarrier)
                                    {
                                        // TODO : Slot Verification Update : WaitingForHost -> VerificationFailed
                                        m_nSeqNum = (int)STEP_CARRIER_LOADING.UNLOAD_CARRIER_BY_ERROR;
                                        break;
                                    }
                                    else
                                    {
                                        ++m_nSeqNum;
                                    }
                                }
                                break;
                            case CommandResult.Timeout:
                            case CommandResult.Error:
                            case CommandResult.Invalid:
                                {
                                    // TODO : Slot Verification Update : WaitingForHost -> VerificationFailed
                                    GenerateAlarm((int)EN_ALARM.LOADPORT_FAILED_TO_EXECUTING_SCENARIO_SLOT_VERIFICATION, result.ActionName);
                                    m_nSeqNum = (int)STEP_CARRIER_LOADING.END;
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    break;

                case (int)STEP_CARRIER_LOADING.CHECK_SLOTMAP_VERIFICATION_BY_HOST + 2:
                    {
                        // TODO : Slot Verification Update : WaitingForHost -> VerificationOk
                        if (_taskOperator.IsFinishingMode())
                        {
                            m_nSeqNum = (int)STEP_CARRIER_LOADING.END;
                            break;
                        }

                        m_nSeqNum = (int)STEP_CARRIER_LOADING.UPDATE_LINK;
                    }
                    break;
                case (int)STEP_CARRIER_LOADING.UNLOAD_CARRIER_BY_ERROR:
                    {
                        var result = _loadPortManager.UnloadCarrierAtLoadPort(LoadPortIndex);
                        switch (result.CommandResult)
                        {
                            case CommandResult.Completed:
                            case CommandResult.Timeout:
                            case CommandResult.Error:
                            case CommandResult.Invalid:
                                {
                                    if (_receivedCancelCarrier)
                                    {
                                        GenerateAlarm((int)EN_ALARM.LOADPORT_FAILED_TO_EXECUTING_SCENARIO_SLOT_VERIFICATION, "Canceled by host");
                                    }
                                    else
                                    {
                                        GenerateAlarm((int)EN_ALARM.LOADPORT_SLOT_STATUS_IS_WRONG);
                                    }

                                    m_nSeqNum = (int)STEP_CARRIER_LOADING.END;
                                }
                                break;

                            default:
                                break;
                        }
                    }
                    break;

                case (int)STEP_CARRIER_LOADING.UPDATE_LINK:
                    SetCarrierPortState(CARRIER_PORT_TYPE.SELECTION);
                    m_nSeqNum = (int)STEP_CARRIER_LOADING.END;
                    break;

                case (int)STEP_CARRIER_LOADING.END:
                    return true;

                default:
                    return false;
            }

            return false;
        }
        protected virtual bool ActionCarrierUnloading(bool manual = false)
        {
            switch (m_nSeqNum)
            {
                case (int)STEP_CARRIER_UNLOADING.START:
                    InitQueuedScenario();
                    if (false == _loadPortManager.IsLoadPortEnabled(LoadPortIndex))
                    {
                        m_nSeqNum = (int)STEP_CARRIER_UNLOADING.END;
                        break;
                    }
                    m_nSeqNum = (int)STEP_CARRIER_UNLOADING.CHECK_READY;
                    break;

                case (int)STEP_CARRIER_UNLOADING.CHECK_READY:
                    {
                        _loadPortManager.InitLoadPortAction(LoadPortIndex);
                        if (manual)
                        {
                            SetDelayForSequence(500);   // 버튼 후딜레이가 있는듯.. 로드포트 비지 상태가 되어 바로 명령이 들어가면 Nack 된다.
                        }

                        var status = _loadPortManager.GetLoadPortState(LoadPortIndex);
                        if (false == status.DoorState &&
                            false == status.DockState &&
                            false == status.ClampState)
                        {
                            m_nSeqNum = (int)STEP_CARRIER_UNLOADING.UPDATE_LINK;
                        }
                        else
                        {
                            m_nSeqNum = (int)STEP_CARRIER_UNLOADING.EXECUTE_QUEUED_SCENARIO_BEFORE_END;
                        }
                    }
                    break;

                case (int)STEP_CARRIER_UNLOADING.EXECUTE_QUEUED_SCENARIO_BEFORE_END:
                    {
                        InitQueuedScenario();
                        if (false == _carrierServer.GetCarrierAccessingStatus(PortId).Equals(CarrierAccessStates.CarrierCompleted))
                        {
                            m_nSeqNum = (int)STEP_CARRIER_UNLOADING.UNLOAD_CARRIER;
                            break;
                        }
                        else
                        {
                            if (EnqueueScenraioBeforeActionCompletion(out QueuedScenarioInfo scenarioListToEnque))
                            {
                                EnqueueScenario(scenarioListToEnque.Scenario, scenarioListToEnque.ScenarioParams, scenarioListToEnque.AdditionalParams);
                            }
                        }
                        ++m_nSeqNum;
                    }
                    break;

                case (int)STEP_CARRIER_UNLOADING.EXECUTE_QUEUED_SCENARIO_BEFORE_END + 1:
                    {
                        if (false == DequeueQueuedScenario())
                        {
                            m_nSeqNum = (int)STEP_CARRIER_UNLOADING.WRITE_CARRIER_ID_TAG;
                        }
                        else
                        {
                            ++m_nSeqNum;
                        }
                    }
                    break;

                case (int)STEP_CARRIER_UNLOADING.EXECUTE_QUEUED_SCENARIO_BEFORE_END + 2:
                    {
                        var result = ExecuteQueuedScenario();
                        switch (result.CommandResult)
                        {
                            case CommandResult.Skipped:
                            case CommandResult.Completed:
                                m_nSeqNum = (int)STEP_CARRIER_UNLOADING.EXECUTE_QUEUED_SCENARIO_BEFORE_END + 1;
                                break;
                            case CommandResult.Timeout:
                            case CommandResult.Error:
                            case CommandResult.Invalid:
                                {
                                    GenerateAlarm((int)EN_ALARM.LOADPORT_SECSGEM_ERROR_BEFORE_UNLOADING_CARRIER, result.ActionName);
                                    m_nSeqNum = (int)STEP_CARRIER_UNLOADING.END;
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    break;

                case (int)STEP_CARRIER_UNLOADING.WRITE_CARRIER_ID_TAG:
                    {
                        var result = WriteCarrierId();
                        switch (result.CommandResult)
                        {
                            case CommandResult.Completed:
                            case CommandResult.Skipped:
                                m_nSeqNum = (int)STEP_CARRIER_UNLOADING.WRITE_CARRIER_LOT_ID_TAG;
                                break;
                            case CommandResult.Timeout:
                            case CommandResult.Error:
                            case CommandResult.Invalid:
                                {
                                    GenerateAlarm((int)EN_ALARM.RFID_WRITE_COMMAND_HAS_FAILED);
                                    m_nSeqNum = (int)STEP_CARRIER_UNLOADING.END;
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    break;

                case (int)STEP_CARRIER_UNLOADING.WRITE_CARRIER_LOT_ID_TAG:
                    {
                        var result = WriteLotId();
                        switch (result.CommandResult)
                        {
                            case CommandResult.Completed:
                            case CommandResult.Skipped:
                                m_nSeqNum = (int)STEP_CARRIER_UNLOADING.EXECUTE_AFTER_WRITING;
                                break;
                            case CommandResult.Timeout:
                            case CommandResult.Error:
                            case CommandResult.Invalid:
                                {
                                    GenerateAlarm((int)EN_ALARM.RFID_WRITE_COMMAND_HAS_FAILED);
                                    m_nSeqNum = (int)STEP_CARRIER_UNLOADING.END;
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    break;

                case (int)STEP_CARRIER_UNLOADING.EXECUTE_AFTER_WRITING:
                    {
                        var result = ExecuteAfterWriting();
                        switch (result.CommandResult)
                        {
                            case CommandResult.Completed:
                            case CommandResult.Skipped:
                                m_nSeqNum = (int)STEP_CARRIER_UNLOADING.UNLOAD_CARRIER;
                                break;
                            case CommandResult.Timeout:
                            case CommandResult.Error:
                            case CommandResult.Invalid:
                                {
                                    GenerateAlarm((int)EN_ALARM.RFID_WRITE_COMMAND_HAS_FAILED);
                                    m_nSeqNum = (int)STEP_CARRIER_UNLOADING.END;
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    break;

                case (int)STEP_CARRIER_UNLOADING.UNLOAD_CARRIER:
                    {
                        var result = _loadPortManager.UnloadCarrierAtLoadPort(LoadPortIndex);
                        switch (result.CommandResult)
                        {
                            case CommandResult.Completed:
                                {
                                    m_nSeqNum = (int)STEP_CARRIER_UNLOADING.UPDATE_LINK;
                                }
                                break;
                            case CommandResult.Timeout:
                            case CommandResult.Error:
                            case CommandResult.Invalid:
                                {
                                    string[] arAlarmSubInfo = { GetTaskName(), string.Format("Unload {0} - {1}", result.ActionName, result.Description) };
                                    GenerateSequenceAlarm((int)EN_ALARM.LOADPORT_FAILED_TO_ACTION, false, ref arAlarmSubInfo);
                                    m_nSeqNum = (int)STEP_CARRIER_UNLOADING.END;
                                }
                                break;

                            default:
                                break;
                        }
                    }
                    break;

                case (int)STEP_CARRIER_UNLOADING.UPDATE_LINK:
                    SetCarrierPortState(CARRIER_PORT_TYPE.SELECTION);
                    m_nSeqNum = (int)STEP_CARRIER_UNLOADING.END;
                    break;

                case (int)STEP_CARRIER_UNLOADING.END:
                    return true;

                default:
                    return false;
            }

            return false;
        }
        #endregion

        #region manual
        protected virtual bool ActionExecuteManually(TASK_ACTION manualActionType)
        {
            switch (m_nSeqNum)
            {
                case (int)STEP_LOADPORT_MANUAL_ACTION.START:
                    m_nSeqNum = (int)STEP_LOADPORT_MANUAL_ACTION.CHECK_READY;
                    break;

                case (int)STEP_LOADPORT_MANUAL_ACTION.CHECK_READY:
                    {
                        _manualActionToExecute = null;
                        _loadPortManager.InitLoadPortAction(LoadPortIndex);

                        #region <Check interlocks before executing>
                        switch (manualActionType)
                        {
                            case TASK_ACTION.CHANGE_LOADPORT_LOADING_MODE_TO_FOUP:
                            case TASK_ACTION.CHANGE_LOADPORT_LOADING_MODE_TO_CASSETTE:
                                {
                                    LoadPortLoadingMode typeToChange = manualActionType.Equals(TASK_ACTION.CHANGE_LOADPORT_LOADING_MODE_TO_FOUP) ?
                                        LoadPortLoadingMode.Foup : LoadPortLoadingMode.Cassette;

                                    LoadPortLoadingMode loadingType = LoadPortLoadingMode.Unknown;
                                    bool hasAnyCarrier = (_loadPortManager.GetPresentState(LoadPortIndex) || _loadPortManager.GetPlacedState(LoadPortIndex));
                                    if (hasAnyCarrier && false == loadingType.Equals(typeToChange))
                                    {
                                        GenerateAlarm((int)EN_ALARM.LOADPORT_FAILED_TO_CHANGE_MODE);
                                        return true;
                                    }
                                }
                                break;

                            case TASK_ACTION.CHANGE_CARRIER_ACCESS_STATUS_TO_NOT_ACCESSED:
                            case TASK_ACTION.CHANGE_CARRIER_ACCESS_STATUS_TO_IN_ACCESSED:
                            case TASK_ACTION.CHANGE_CARRIER_ACCESS_STATUS_TO_CARRIER_COMPLETED:
                            case TASK_ACTION.CHANGE_CARRIER_ACCESS_STATUS_TO_CARRIER_STOPPED:
                                {
                                    if (false == _carrierServer.HasCarrier(PortId))
                                    {
                                        _carrierServer.SetCarrierAccessingStatus(PortId, CarrierAccessStates.NotAccessed);
                                    }
                                    else
                                    {
                                        if (manualActionType.Equals(TASK_ACTION.CHANGE_CARRIER_ACCESS_STATUS_TO_NOT_ACCESSED))
                                        {
                                            _carrierServer.SetCarrierAccessingStatus(PortId, CarrierAccessStates.NotAccessed);
                                        }
                                        else if (manualActionType.Equals(TASK_ACTION.CHANGE_CARRIER_ACCESS_STATUS_TO_IN_ACCESSED))
                                        {
                                            _carrierServer.SetCarrierAccessingStatus(PortId, CarrierAccessStates.InAccessed);
                                        }
                                        else if (manualActionType.Equals(TASK_ACTION.CHANGE_CARRIER_ACCESS_STATUS_TO_CARRIER_COMPLETED))
                                        {
                                            _carrierServer.SetCarrierAccessingStatus(PortId, CarrierAccessStates.CarrierCompleted);
                                        }
                                        else if (manualActionType.Equals(TASK_ACTION.CHANGE_CARRIER_ACCESS_STATUS_TO_CARRIER_STOPPED))
                                        {
                                            _carrierServer.SetCarrierAccessingStatus(PortId, CarrierAccessStates.CarrierStopped);
                                        }
                                    }
                                    
                                    return true;
                                }
                                break;
                            default:
                                break;
                        }
                        #endregion </Check interlocks before executing>

                        m_nSeqNum = (int)STEP_LOADPORT_MANUAL_ACTION.EXECUTE_MANUAL_ACTION;
                    }
                    break;

                case (int)STEP_LOADPORT_MANUAL_ACTION.EXECUTE_MANUAL_ACTION:
                    {
                        switch (manualActionType)
                        {
                            case TASK_ACTION.STOP:
                                break;
                            case TASK_ACTION.SCHEDULING:
                                break;
                            case TASK_ACTION.WAIT_FOR_LOADING:
                                break;
                            case TASK_ACTION.WAIT_FOR_UNLOADING:
                                break;
                            case TASK_ACTION.CARRIER_LOADING:
                                break;
                            case TASK_ACTION.CARRIER_UNLOADING:
                                break;
                            case TASK_ACTION.CHANGE_LOADPORT_LOADING_MODE_TO_FOUP:
                                _manualActionToExecute = _loadPortManager.ChangeLoadPortModeToFoup;
                                break;
                            case TASK_ACTION.CHANGE_LOADPORT_LOADING_MODE_TO_CASSETTE:
                                _manualActionToExecute = _loadPortManager.ChangeLoadPortModeToCassette;
                                break;
                            case TASK_ACTION.CHANGE_LOADPORT_ACCESSE_MODE_TO_AUTO:
                                _manualActionToExecute = _loadPortManager.ChangeLoadPortAccessModeToAuto;
                                break;
                            case TASK_ACTION.CHANGE_LOADPORT_ACCESSE_MODE_TO_MANUAL:
                                _manualActionToExecute = _loadPortManager.ChangeLoadPortAccessModeToManual;
                                break;

                            case TASK_ACTION.CARRIER_CLAMPING:
                                _manualActionToExecute = _loadPortManager.ClampCarrierAtLoadPort;
                                break;
                            case TASK_ACTION.CARRIER_UNCLAMPING:
                                _manualActionToExecute = _loadPortManager.ReleaseCarrierAtLoadPort;
                                break;
                            case TASK_ACTION.CARRIER_DOCKING:
                                _manualActionToExecute = _loadPortManager.DockCarrierAtLoadPort;
                                break;
                            case TASK_ACTION.CARRIER_UNDOCKING:
                                _manualActionToExecute = _loadPortManager.UnDockCarrierAtLoadPort;
                                break;
                            case TASK_ACTION.CARRIER_OPENING:
                                _manualActionToExecute = _loadPortManager.OpenCarrierAtLoadPort;
                                break;
                            case TASK_ACTION.CARRIER_CLOSING:
                                _manualActionToExecute = _loadPortManager.CloseCarrierAtLoadPort;
                                break;
                            case TASK_ACTION.INITIALIZE:
                                _manualActionToExecute = _loadPortManager.InitializeLoadPort;
                                break;
                            case TASK_ACTION.RESET:
                                _manualActionToExecute = _loadPortManager.ClearAlarmLoadPort;
                                break;

                            case TASK_ACTION.CHANGE_CARRIER_ACCESS_STATUS_TO_NOT_ACCESSED:
                            case TASK_ACTION.CHANGE_CARRIER_ACCESS_STATUS_TO_IN_ACCESSED:
                            case TASK_ACTION.CHANGE_CARRIER_ACCESS_STATUS_TO_CARRIER_COMPLETED:
                            case TASK_ACTION.CHANGE_CARRIER_ACCESS_STATUS_TO_CARRIER_STOPPED:
                                return true;

                            default:
                                break;
                        }

                        //LoadPortLoadingMode loadingType;
                        //if (changeToFoup)
                        //{
                        //    loadingType = LoadPortLoadingMode.Foup;
                        //}
                        //else
                        //{
                        //    loadingType = LoadPortLoadingMode.Cassette;
                        //}

                        if (_manualActionToExecute == null)
                        {
                            string[] arAlarmSubInfo = { GetTaskName(), "Invalid Action" };
                            GenerateSequenceAlarm((int)EN_ALARM.LOADPORT_FAILED_TO_ACTION, false, ref arAlarmSubInfo);
                            m_nSeqNum = (int)STEP_INITIALIZE.END;
                            break;
                        }
                        var result = _manualActionToExecute(LoadPortIndex);
                        switch (result.CommandResult)
                        {
                            case CommandResult.Completed:
                                {
                                    m_nSeqNum = (int)STEP_LOADPORT_MANUAL_ACTION.END;
                                }
                                break;

                            case CommandResult.Timeout:
                            case CommandResult.Error:
                            case CommandResult.Invalid:
                                {
                                    string[] arAlarmSubInfo = { GetTaskName(), string.Format("Change Mode {0} - {1}", result.ActionName, result.Description) };
                                    GenerateSequenceAlarm((int)EN_ALARM.LOADPORT_FAILED_TO_ACTION, false, ref arAlarmSubInfo);
                                    m_nSeqNum = (int)STEP_INITIALIZE.END;
                                }
                                break;

                            default:
                                break;
                        }
                    }
                    break;

                case (int)STEP_LOADPORT_MANUAL_ACTION.END:
                    return true;

                default:
                    return false;
            }

            return false;
        }
        #endregion /manual

        #endregion /action

        #region <ETC>
        private bool WaitSignalFromRobot()
        {
            for (int i = 0; i < RobotTaskNames.Count; ++i)
            {
                if (false == WaitSignal(RobotTaskNames[i]))
                    return false;
            }

            return true;
        }
        private bool CheckControllerConnectionStatus()
        {
            if (false == _loadPortManager.IsConnectedWithController(LoadPortIndex))
            {
                GenerateAlarm((int)EN_ALARM.LOADPORT_CONTROLLER_NOT_CONNECTED);
                return false;
            }

            if (false == _rfidManager.IsConnected(LoadPortIndex))
            {
                GenerateAlarm((int)EN_ALARM.RFID_CONTROLLER_NOT_CONNECTED);
                return false;
            }

            return true;
        }
        protected bool IsSaftyInterLockError()
        {
            if (false == _loadPortManager.GetAMHSSaftyInterLockStatus(LoadPortIndex) &&
                _loadPortManager.GetAccessMode(LoadPortIndex).Equals(LoadPortAccessMode.Auto))
            {
                return true;
            }

            return false;
        }
        private void CallbackLoadButtonClicked()
        {
            if (false == _loadPortManager.IsLoadPortEnabled(LoadPortIndex))
                return;

            string[] taskName = { GetTaskName() };
            string[] actionNames = { TASK_ACTION.CARRIER_LOADING.ToString() };

            _taskOperator.SetOperation(ref taskName, ref actionNames);
        }
        private void CallbackUnloadButtonClicked()
        {
            if (false == _loadPortManager.IsLoadPortEnabled(LoadPortIndex))
                return;

            string[] taskName = { GetTaskName() };
            string[] actionNames = { TASK_ACTION.CARRIER_UNLOADING.ToString() };

            _taskOperator.SetOperation(ref taskName, ref actionNames);
        }
        protected virtual void ChangingModeButtonClicked(LoadPortLoadingMode loadingType)
        {
            TASK_ACTION action;
            switch (loadingType)
            {
                case LoadPortLoadingMode.Cassette:
                    action = TASK_ACTION.CHANGE_LOADPORT_LOADING_MODE_TO_CASSETTE;
                    break;
                case LoadPortLoadingMode.Foup:
                    action = TASK_ACTION.CHANGE_LOADPORT_LOADING_MODE_TO_FOUP;
                    break;
                default:
                    return;
            }

            string[] taskName = { GetTaskName() };
            string[] actionNames = { action.ToString() };

            _taskOperator.SetOperation(ref taskName, ref actionNames);

        }
        private void ExecuteResetCommandAsync()
        {
            _loadPortManager.InitLoadPortAction(LoadPortIndex);
            TickCounter_.TickCounter tick = new TickCounter_.TickCounter();
            tick.SetTickCount(1000);
            System.Threading.Thread.Sleep(500);
            while (true)
            {
                if (tick.IsTickOver(true))
                    break;

                System.Threading.Thread.Sleep(1);
                var result = _loadPortManager.ClearAlarmLoadPort(LoadPortIndex);
                if (false == result.CommandResult.Equals(CommandResult.Proceed))
                    break;
            }
        }
        protected bool IsPortNotChanged(CARRIER_PORT_TYPE portType)
        {
            var currentPort = GetPortStatus(EN_PORT.LOADPORT_STATE.ToString());
            var comparePort = _loadPortSchedulerManager.GetCarrierPortStatus(portType);

            return currentPort.Equals(comparePort);
        }
        protected void SetCarrierPortState(CARRIER_PORT_TYPE portType)
        {
            SetPortStatus(EN_PORT.LOADPORT_STATE.ToString(), _loadPortSchedulerManager.GetCarrierPortStatus(portType));
        }

        // 2025.01.05. jhlim [DEL] 필요 없어져서 폐기
        //protected bool IsAnyCarrierPlaced(ref LoadPortLoadingMode loadingTypes)
        //{
        //    foreach (var item in LoadPortModeSignals)
        //    {
        //        bool placed = DigitalIO_.DigitalIO.GetInstance().ReadInput(item.Value);
        //        if (placed)
        //        {
        //            loadingTypes = item.Key;
        //            return true;
        //        }
        //    }

        //    bool present = DigitalIO_.DigitalIO.GetInstance().ReadInput(CarrierPresenceIndex);
        //    if (present)
        //        return true;

        //    return false;
        //}
        // 2025.01.05. jhlim [END]

        private void InitQueuedScenario()
        {
            _executingScenario = null;
            QueuedScenario.Clear();
        }
        protected void EnqueueScenario(Enum scenario, Dictionary<string, string> scenarioParam, Dictionary<string, string> additionalParams)
        {
            QueuedScenarioInfo queuedScenario = new QueuedScenarioInfo
            {
                Scenario = scenario,
                ScenarioParams = scenarioParam,
                AdditionalParams = additionalParams
            };

            QueuedScenario.Enqueue(queuedScenario);
        }
        protected bool DequeueQueuedScenario()
        {
            if (QueuedScenario.Count <= 0)
                return false;

            _executingScenario = QueuedScenario.Dequeue();
            return _scenarioOperator.UpdateScenarioParam(_executingScenario.Scenario, _executingScenario.ScenarioParams);
        }
        private CommandResults ExecuteQueuedScenario()
        {
            if (_executingScenario == null)
            {
                _commandResult.ActionName = "Idle";
                _commandResult.Description = string.Empty;
                _commandResult.CommandResult = CommandResult.Skipped;

                return _commandResult;
            }

            var result = _scenarioOperator.ExecuteScenario(_executingScenario.Scenario);
            _commandResult.ActionName = _executingScenario.Scenario.ToString();
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
                    _commandResult.Description = _commandResult.ActionName;
                    break;
                case EN_SCENARIO_RESULT.TIMEOUT_ERROR:
                    _commandResult.CommandResult = CommandResult.Timeout;
                    _commandResult.Description = _commandResult.ActionName;
                    break;
                default:
                    break;
            }

            if (false == result.Equals(EN_SCENARIO_RESULT.PROCEED))
            {
                ExecuteAfterScenarioCompletion(_executingScenario.Scenario, result, _executingScenario.ScenarioParams, _executingScenario.AdditionalParams);
                _executingScenario = null;
            }

            return _commandResult;
        }
        #endregion </ETC>

        #region pre/post condition
        #endregion /pre/post condition

        #region common method

        #region sub sequence
        #endregion /sub sequence

        #endregion /common method

        #region enum

        #region action
        /// <summary>
        /// 2020.06.02 by yjlee [ADD] Enumerate the actions of the task.
        /// </summary>
        public enum TASK_ACTION
        {
            STOP = 0,
            SCHEDULING,
            WAIT_FOR_LOADING,
            WAIT_FOR_UNLOADING,
            CARRIER_LOADING,
            CARRIER_UNLOADING,
            CHANGE_LOADPORT_LOADING_MODE_TO_FOUP,
            CHANGE_LOADPORT_LOADING_MODE_TO_CASSETTE,
            CHANGE_LOADPORT_ACCESSE_MODE_TO_AUTO,
            CHANGE_LOADPORT_ACCESSE_MODE_TO_MANUAL,
            CHANGE_CARRIER_ACCESS_STATUS_TO_NOT_ACCESSED,
            CHANGE_CARRIER_ACCESS_STATUS_TO_IN_ACCESSED,
            CHANGE_CARRIER_ACCESS_STATUS_TO_CARRIER_COMPLETED,
            CHANGE_CARRIER_ACCESS_STATUS_TO_CARRIER_STOPPED,
            CARRIER_CLAMPING,
            CARRIER_UNCLAMPING,
            CARRIER_DOCKING,
            CARRIER_UNDOCKING,
            CARRIER_OPENING,
            CARRIER_CLOSING,
            INITIALIZE,
            RESET,
        }
        #endregion /action

        #region step
        private enum STEP_INITIALIZE
        {
            START = 0,
            PREPARE = 50,
            CHANGE_MODE = 200,
            INITIALIZE = 500,
            UPDATE_LINK = 900,
            END = 10000,
        }
        private enum STEP_ENTRY
        {
            START = 0,

            PREPARE = 50,

            UPDATE_LINK = 900,

            END = 10000,
        }
        private enum STEP_EXIT
        {
            START = 0,

            CLEAR_DUMMY_DATA = 500,

            END = 10000,
        }
        private enum STEP_SCHEDULING
        {
            START = 0,

            CHECK_READY = 100,

            DUMMY_STEP = 200,

            CHECK_PORT = 900,

            END = 10000,
        }
        private enum STEP_WAIT_FOR_LOADING
        {
            START = 0,

            CHECK_READY = 100,
            
            CALL_OHT_CARRIER_TO_LOAD = 200,

            INTERFACE_BY_PIO = 500,
            
            SAFTY_INTERLOCK_DETECTED = 800,
            
            UPDATE_LINK = 900,

            END = 10000,
        }
        private enum STEP_WAIT_FOR_UNLOADING
        {
            START = 0,

            CHECK_READY = 100,

            READ_CARRIER_ID = 200,

            READ_LOT_ID = 300,

            CALL_OHT_CARRIER_TO_UNLOAD = 400,

            INTERFACE_BY_PIO = 500,

            SAFTY_INTERLOCK_DETECTED = 800,

            UPDATE_LINK = 900,

            END = 10000,
        }
        private enum STEP_CARRIER_LOADING
        {
            START = 0,

            CHECK_READY = 100,

            CLAMP_CARRIER = 200,

            READ_CARRIER_ID = 300,

            READ_LOT_ID = 400,

            EXECUTE_SCENARIO_TO_ID_READ = 500,

            CHECK_ID_VERIFICATION_BY_HOST = 550,

            LOAD_CARRIER = 600,

            CHECK_SLOT_VALIDITY = 700,
            
            UNLOAD_CARRIER_BY_ERROR = 750,

            CHECK_SLOTMAP_VERIFICATION_BY_HOST = 800,

            UPDATE_LINK = 900,

            END = 10000,
        }
        private enum STEP_CARRIER_UNLOADING
        {
            START = 0,

            CHECK_READY = 100,

            EXECUTE_QUEUED_SCENARIO_BEFORE_END = 200,

            WRITE_CARRIER_ID_TAG = 300,

            WRITE_CARRIER_LOT_ID_TAG = 400,

            EXECUTE_AFTER_WRITING = 500,
            
            UNLOAD_CARRIER = 600,

            UPDATE_LINK = 900,

            END = 10000,
        }
        private enum STEP_LOADPORT_MANUAL_ACTION
        {
            START = 0,

            CHECK_READY = 100,

            EXECUTE_MANUAL_ACTION = 200,

            END = 10000,
        }
        #endregion /step

        #endregion /enum
    }

    struct LoadPortMode
    {
        public LoadPortMode(LoadPortLoadingMode loadingType, int inputIndex)
        {
            LoadingType = loadingType;
            DigitalInputIndex = inputIndex;
        }

        public int DigitalInputIndex { get; set; }
        public LoadPortLoadingMode LoadingType { get; set; }
    }

    //class TaskLoadPortRecovery : RecoveryData
    //{
    //    public TaskLoadPortRecovery(string taskName, int nPortCount)
    //        : base(taskName, nPortCount)
    //    {
    //    }

    //    protected override void LoadData(ref FileComposite_.FileComposite fComp, string sRootName)
    //    {
    //    }
    //    protected override void SaveData(ref FileComposite_.FileComposite fComp, string sRootName)
    //    {
    //    }
    //}
}
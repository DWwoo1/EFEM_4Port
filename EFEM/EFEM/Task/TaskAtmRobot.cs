using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

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
using Define.DefineEnumProject.Task.AtmRobot;
using Define.DefineEnumProject.Mail;
using Define.DefineEnumProject.SubSequence;
using Define.DefineEnumProject.Tool;

using EFEM.Modules;
using EFEM.Defines.Common;
using EFEM.Defines.AtmRobot;
using EFEM.Modules.AtmRobot;
using EFEM.MaterialTracking;
using EFEM.ActionScheduler;
using EFEM.ActionScheduler.RobotActionSchedulers;
using EFEM.MaterialTracking.LocationServer;

namespace FrameOfSystem3.Task
{
    abstract class TaskAtmRobot : RunningTaskWrapper
    {
        #region constructor
        public TaskAtmRobot(int nIndexOfTask, string strTaskName, RecoveryData recovery)
            : base(nIndexOfTask, strTaskName, typeof(PARAM_PROCESS))
        {
            _taskOperator = TaskOperator.GetInstance();
            
            _recoveryData = recovery;
            //_recovery = new TaskAtmRobotRecovery(strTaskName, 1);
            AddRecoveryData(strTaskName, _recoveryData);

            _robotManager = AtmRobotManager.Instance;

            if (Enum.TryParse(strTaskName, out EN_TASK_LIST taskType))
            {
                //switch (taskType)
                //{
                //    case EN_TASK_LIST.AtmRobot:
                //        RobotIndex = (int)taskType - (int)EN_TASK_LIST.AtmRobot;
                //        break;
                //    default:
                //        throw new Exception(string.Format("----- Invalid Task Name : {0} -----", taskType.ToString()));
                //}
                RobotIndex = (int)taskType - (int)EN_TASK_LIST.AtmRobot;
            }

            _robotSchedulerManager = RobotActionSchedulerManager.Instance;
            _locationServer = LocationServer.Instance;
            _substrateManager = SubstrateManager.Instance;
            _processGroup = ProcessModuleGroup.Instance;
            _carrierServer = CarrierManagementServer.Instance;

            RobotName = _robotManager.GetRobotName(RobotIndex);

            _loadPortManager = LoadPortManager.Instance;

            int busyStatusIndex = 0;
            if (GetBusySignalIndex(RobotIndex, ref busyStatusIndex))
            {
                _robotManager.AttachBusySignalByDigitalInput(RobotIndex, busyStatusIndex, DigitalIO_.DigitalIO.GetInstance().ReadInput);
            }

            int alarmStatusIndex = 0;
            if (GetAlarmSignalIndex(RobotIndex, ref alarmStatusIndex))
            {
                _robotManager.AttachAlarmSignalByDigitalInput(RobotIndex, alarmStatusIndex, DigitalIO_.DigitalIO.GetInstance().ReadInput);
            }

            int servoStatusIndex = 0;
            if (GetServoSignalIndex(RobotIndex, ref servoStatusIndex))
            {
                _robotManager.AttachServoSignalByDigitalInput(RobotIndex, servoStatusIndex, DigitalIO_.DigitalIO.GetInstance().ReadInput);
            }
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
        //static TaskAtmRobotRecovery _recovery = null;
        #endregion /instance

        TASK_ACTION m_enAction = TASK_ACTION.STOP;
        Dictionary<string, TASK_ACTION> m_mapppingForAction = new Dictionary<string, TASK_ACTION>();
        #endregion /default

        #region <Robot>
        protected readonly int RobotIndex;
        protected readonly string RobotName;
        //private readonly AtmRobotController _robotController = null;

        protected static AtmRobotManager _robotManager = null;
        protected static RobotActionSchedulerManager _robotSchedulerManager = null;
        protected static LocationServer _locationServer = null;
        protected static SubstrateManager _substrateManager = null;
        protected static CarrierManagementServer _carrierServer = null; 
        protected static ProcessModuleGroup _processGroup = null;

        protected RobotWorkingInfo _workingInfo = null;

        //private Func<bool, RobotArmTypes, string, int, string, CommandResults> _subActionPick;
        //private Func<bool, RobotArmTypes, string, int, string, CommandResults> _subActionPlace;
        //private SubStepPick _subActionStepPick;
        //private SubStepPlace _subActionStepPlace;
        //private CommandResults _subCommandResult = null;
        protected bool _prevInitializationState = false;
        protected readonly TickCounter_.TickCounter RobotTicks = new TickCounter_.TickCounter();

        protected static LoadPortManager _loadPortManager = null;
        #endregion </Robot>

        #endregion /field

        #region <Enum>
        enum SubStepPick
        {
            Init,
            BeforeApproachUnloading,
            ActionApproachUnloading,
            AfterApproachUnloading,
            BeforeActionUnloading,
            ActionUnloading,
            AfterActionUnloading,
            End
        }

        enum SubStepPlace
        {
            Init,
            BeforeApproachLoading,
            ActionApproachLoading,
            AfterApproachLoading,
            BeforeActionLoading,
            ActionLoading,
            AfterActionLoading,
            End
        }
        #endregion </Enum>

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
                    _robotManager.InitAtmRobotAction(RobotIndex);
                    if (false == CheckControllerConnectionStatus())
                        return true;

                    m_nSeqNum = (int)STEP_INITIALIZE.CHECK_ALARM_STATUS;
                    break;

                case (int)STEP_INITIALIZE.CHECK_ALARM_STATUS:
                    if (_robotManager.IsRobotBusy(RobotIndex))
                        break;

                    // 알람 해제 시도 5초 제한
                    RobotTicks.SetTickCount(5000);
                    ++m_nSeqNum;
                    break;

                case (int)STEP_INITIALIZE.CHECK_ALARM_STATUS + 1:
                    if (_robotManager.IsRobotBusy(RobotIndex))
                        break;

                    _robotManager.InitAtmRobotAction(RobotIndex);
                    //if (_robotManager.IsRobotAlarm(RobotIndex))
                    //{
                    //    ++m_nSeqNum;
                    //}
                    //else
                    {
                        m_nSeqNum = (int)STEP_INITIALIZE.PREPARE;
                    }
                    break;

                case (int)STEP_INITIALIZE.CHECK_ALARM_STATUS + 2:
                    {
                        var result = _robotManager.Clear(RobotIndex);
                        switch (result.CommandResult)
                        {
                            //case EN_COMMAND_RESULT.PROCEED:
                            //    break;
                            case CommandResult.Completed:
                            case CommandResult.Timeout:
                            case CommandResult.Error:
                            case CommandResult.Invalid:
                                {
                                    if (RobotTicks.IsTickOver(true))
                                    {
                                        string[] _arAlarmSubInfo = { GetTaskName(), result.Description };
                                        GenerateSequenceAlarm((int)EN_ALARM.ATM_ROBOT_ALARM_CLEARING_FAILED, false, ref _arAlarmSubInfo);
                                        m_nSeqNum = (int)STEP_INITIALIZE.END;
                                    }
                                    else
                                    {
                                        if (false == result.CommandResult.Equals(CommandResult.Completed))
                                        {
                                            SetDelayForSequence(100);
                                        }

                                        --m_nSeqNum;
                                    }
                                }
                                break;

                            default:
                                ++m_nSeqNum;
                                break;
                        }
                    }
                    break;

                case (int)STEP_INITIALIZE.CHECK_ALARM_STATUS + 3:
                    --m_nSeqNum;
                    break;

                case (int)STEP_INITIALIZE.PREPARE:
                    {
                        var result = _robotManager.InitializeAtmRobot(RobotIndex);
                        switch (result.CommandResult)
                        {
                            //case EN_COMMAND_RESULT.PROCEED:
                            //    break;
                            case CommandResult.Completed:
                                {
                                    SetSignal();
                                    m_nSeqNum = (int)STEP_INITIALIZE.END;
                                }
                                break;

                            case CommandResult.Timeout:
                            case CommandResult.Error:
                            case CommandResult.Invalid:
                                {
                                    string[] _arAlarmSubInfo = { GetTaskName(), string.Format("{0} - {1}", result.ActionName, result.Description) };
                                    GenerateSequenceAlarm((int)EN_ALARM.ATM_ROBOT_INITIALIZING_FAILED, false, ref _arAlarmSubInfo);
                                    m_nSeqNum = (int)STEP_INITIALIZE.END;
                                }
                                break;

                            default:
                                ++m_nSeqNum;
                                break;
                        }
                    }
                    break;

                case (int)STEP_INITIALIZE.PREPARE + 1:
                    {
                        --m_nSeqNum;
                    }
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
                    
                    if (false == CheckControllerConnectionStatus())
                        return true;

                    ++m_nSeqNum;
                    break;

                case (int)STEP_ENTRY.START + 1:
                    {
                        string[] tasks = null;
                        string[][] manual = null;
                        bool isManualOperation = _taskOperator.GetManualOperation(ref tasks, ref manual);
                        bool hasInvalidSubstrate = false;
                        if (tasks != null || false == isManualOperation)
                        {
                            //bool needToCheck = false;
                            if (false == isManualOperation)
                            {
                                //needToCheck = true;
                                Dictionary<RobotArmTypes, Substrate> substrateAtArm = new Dictionary<RobotArmTypes, Substrate>();
                                if (_robotManager.GetSubstrates(RobotIndex, ref substrateAtArm))
                                {
                                    foreach (var item in substrateAtArm)
                                    {
                                        if (item.Value == null)
                                            continue;

                                        if (item.Value.GetSourcePortId() <= 0)
                                        {
                                            hasInvalidSubstrate = true;
                                            break;
                                        }
                                    }
                                }
                            }

                            // 2025.02.07.jhlim [DEL] 아래 구문 때문에 매뉴얼 동작 시 정보가 이상하면 자재 정보 편집하지 않는 이상 이도저도 못하는 상태가 된다.
                            //else
                            //{
                            //    for (int i = 0; i < tasks.Length && isManualOperation; ++i)
                            //    {
                            //        if (tasks[i].Equals(GetTaskName()))
                            //        {
                            //            needToCheck = true;
                            //            break;
                            //        }
                            //    }
                            //}

                            //if (needToCheck)
                            //{
                            //    Dictionary<RobotArmTypes, Substrate> substrateAtArm = new Dictionary<RobotArmTypes, Substrate>();
                            //    if (_robotManager.GetSubstrates(RobotIndex, ref substrateAtArm))
                            //    {
                            //        foreach (var item in substrateAtArm)
                            //        {
                            //            if (item.Value == null)
                            //                continue;

                            //            if (item.Value.GetSourcePortId() <= 0)
                            //            {
                            //                hasInvalidSubstrate = true;
                            //                break;
                            //            }
                            //        }
                            //    }
                            //}
                        }

                        if (hasInvalidSubstrate)
                        {
                            GenerateAlarm((int)EN_ALARM.ATM_ROBOT_CANNOT_GET_WORKING_INFO);
                            m_nSeqNum = (int)STEP_ENTRY.END;
                        }
                        else
                        {
                            m_nSeqNum = (int)STEP_ENTRY.PREPARE;
                        }
                    }
                    break;

                case (int)STEP_ENTRY.PREPARE:
                    var result = _robotManager.Clear(RobotIndex);
                    switch (result.CommandResult)
                    {
                        //case EN_COMMAND_RESULT.PROCEED:
                        //    break;
                        case CommandResult.Completed:
                            {
                                m_nSeqNum = (int)STEP_ENTRY.END;
                            }
                            break;

                        case CommandResult.Timeout:
                        case CommandResult.Error:
                        case CommandResult.Invalid:
                            {
                                string[] _arAlarmSubInfo = { GetTaskName(), string.Format("{0} - {1}", result.ActionName, result.Description) };
                                GenerateSequenceAlarm((int)EN_ALARM.ATM_ROBOT_ALARM_CLEARING_FAILED, false, ref _arAlarmSubInfo);
                                m_nSeqNum = (int)STEP_ENTRY.END;
                            }
                            break;

                        default:
                            ++m_nSeqNum;
                            break;
                    }
                    break;

                case (int)STEP_ENTRY.PREPARE + 1:
                    {
                        if (_taskOperator.IsFinishingMode())
                        {
                            m_nSeqNum = (int)STEP_ENTRY.END;
                            break;
                        }
                    }
                    --m_nSeqNum;
                    break;

                case (int)STEP_ENTRY.END:
                    SetRobotPortState(RobotScheduleType.Selection);
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

                case TASK_ACTION.PICK:
                case TASK_ACTION.MANUAL_PICK:
                    return ActionPick(true);

                case TASK_ACTION.PLACE:
                case TASK_ACTION.MANUAL_PLACE:
                    return ActionPlace(true);

                //case TASK_ACTION.GEM_SIMUL:
                    //return ActionGemSimul();

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

                case TASK_ACTION.PICK:
                    return ActionPick();

                case TASK_ACTION.PLACE:
                    return ActionPlace();

                default:
                    return false;
            }
        }
        protected override void DoAlwaysSequence()
        {
            if (_robotManager != null)
            {
                if (_prevInitializationState != _robotManager.GetInitializationState(RobotIndex))
                {
                    _prevInitializationState = _robotManager.GetInitializationState(RobotIndex);

                    if (false == _prevInitializationState)
                    {
                        GenerateAlarm((int)EN_ALARM.ATM_ROBOT_IS_NOT_INITIALIZED);
                    }
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
                    _robotSchedulerManager.RemoveCurrentManualWorkingInfo(RobotIndex);
                    _processGroup.ResetSignalsAll();
                   
                    if (_taskOperator.IsDryRunMode())
                    {
                        _robotManager.RemoveSubstrateAll(RobotIndex);
                        _processGroup.RemoveSubstrateAll();
                    }

                    m_nSeqNum = (int)STEP_EXIT.END;
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
            if (_robotManager == null)
                return false;

            return _robotManager.GetInitializationState(RobotIndex);
        }
        #endregion /inherit

        #region <Abstract Methods>

        #region <Input/Output>
        protected abstract bool GetBusySignalIndex(int index, ref int indexOfDigital);
        protected abstract bool GetAlarmSignalIndex(int index, ref int indexOfDigital);
        protected abstract bool GetServoSignalIndex(int index, ref int indexOfDigital);
        #endregion </Input/Output>

        #region <Scenario>
        protected abstract void InitScenarioInfoPick();
        protected abstract bool UpdateParamToBeforePick();
        protected abstract CommandResults ExecuteScenarioToBeforePick();
        protected abstract bool UpdateParamToAfterPick();
        protected abstract CommandResults ExecuteScenarioToAfterPick();
        protected abstract void InitScenarioInfoPlace();
        protected abstract bool UpdateParamToBeforePlace();
        protected abstract CommandResults ExecuteScenarioToBeforePlace();
        protected abstract bool UpdateParamToAfterPlace();
        protected abstract CommandResults ExecuteScenarioToAfterPlace();
        #endregion </Scenario>

        #region <Material Handling With Process Module>

        #region <Recovery Data>
        protected abstract void UpdateRecoveryDataBeforePick();
        protected abstract void UpdateRecoveryDataAfterPick();
        protected abstract void UpdateRecoveryDataBeforePlace();
        protected abstract void UpdateRecoveryDataAfterPlace();
        #endregion </Recovery Data>

        #region <Init>
        protected abstract void InitMaterialHandlingInterface();
        #endregion <Init>

        #region <Loading>
        protected abstract CommandResults IsApproachLoadingPrepared();
        protected abstract CommandResults IsApproachLoadingCompleted();
        protected abstract CommandResults IsLoadingPrepared();
        protected abstract CommandResults IsLoadingCompleted();
        #endregion </Loading>

        #region <Unloading>
        protected abstract CommandResults IsApproachUnloadingPrepared();
        protected abstract CommandResults IsApproachUnloadingCompleted();
        protected abstract CommandResults IsUnloadingPrepared();
        protected abstract CommandResults IsUnloadingCompleted();
        #endregion </Unloading>

        #endregion </Material Handling With Process Module>

        #endregion </Abstract Methods>

        #region action

        #region auto
        protected virtual bool ActionScheduling()
        {
            switch (m_nSeqNum)
            {
                case (int)STEP_SCHEDULING.START:
                    {
                        InitMaterialHandlingInterface();
                        _robotSchedulerManager.InitSchedulers(RobotIndex);
                    }
                    m_nSeqNum = (int)STEP_SCHEDULING.CHECK_READY;
                    break;
                case (int)STEP_SCHEDULING.CHECK_READY:
                    {
                        if (_taskOperator.IsFinishingMode())
                        {
                            m_nSeqNum = (int)STEP_SCHEDULING.END;
                            break;
                        }

                        var currentPort = _robotSchedulerManager.ExecuteSchedulers(RobotIndex);
                        switch (currentPort)
                        {
                            case RobotScheduleType.Selection:
                                {
                                    ++m_nSeqNum;
                                }
                                break;
                            case RobotScheduleType.Pick:
                            case RobotScheduleType.Place:
                                {
                                    //
                                    //LocationInfo targetLocation = _robotSchedulerManager.GetTargetLocation(MyRobotIndex);
                                    SetRobotPortState(currentPort);
                                    m_nSeqNum = (int)STEP_SCHEDULING.END;
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    break;
                case (int)STEP_SCHEDULING.CHECK_READY + 1:
                    {
                        --m_nSeqNum;
                    }
                    break;

                case (int)STEP_SCHEDULING.END:
                    return true;

                default:
                    return false;
            }

            return false;
        }
        protected virtual bool ActionPick(bool manualAction = false)
        {
            switch (m_nSeqNum)
            {
                case (int)STEP_PICKING.START:
                    {
                        InitScenarioInfoPick();
                        UpdateRecoveryDataBeforePick();
                        InitMaterialHandlingInterface();
                        m_nSeqNum = (int)STEP_PICKING.CHECK_READY;
                    }
                    break;

                case (int)STEP_PICKING.CHECK_READY:
                    {
                        // 암의 Sub 정보와 Presence의 동기화 필요?
                        
                        List<RobotArmTypes> availableArms = new List<RobotArmTypes>();
                        if (false == _taskOperator.IsDryRunMode())
                        {
                            if (false == _robotManager.GetAvailableArm(RobotIndex, true, ref availableArms))
                            {
                                GenerateAlarm((int)EN_ALARM.ATM_ROBOT_HAS_NO_AVAILABLE_ARM);
                                m_nSeqNum = (int)STEP_PICKING.END;
                                break;
                            }
                        }
                        _workingInfo = new RobotWorkingInfo();
                        _robotManager.InitAtmRobotAction(RobotIndex);
                        ++m_nSeqNum;
                    }
                    break;

                case (int)STEP_PICKING.CHECK_READY + 1:
                    {
                        if (_taskOperator.IsFinishingMode())
                        {
                            m_nSeqNum = (int)STEP_PICKING.END;
                            break;
                        }

                        m_nSeqNum = (int)STEP_PICKING.EXECUTE_SCENARIO_BEFORE_PICK;
                    }
                    break;

                #region <Execute scenario before pick>
                case (int)STEP_PICKING.EXECUTE_SCENARIO_BEFORE_PICK:
                    if (false == UpdateParamToBeforePick())
                    {
                        m_nSeqNum = (int)STEP_PICKING.PICK;
                    }
                    else
                    {
                        ++m_nSeqNum;
                    }
                    break;

                case (int)STEP_PICKING.EXECUTE_SCENARIO_BEFORE_PICK + 1:
                    {
                        var result = ExecuteScenarioToBeforePick();
                        switch (result.CommandResult)
                        {
                            case CommandResult.Proceed:
                                break;
                            case CommandResult.Completed:
                            case CommandResult.Skipped:
                                {
                                    --m_nSeqNum;
                                }
                                break;
                            case CommandResult.Timeout:
                            case CommandResult.Error:
                            case CommandResult.Invalid:
                                {
                                    GenerateAlarm((int)EN_ALARM.ATM_ROBOT_SECSGEM_ERROR_BEFORE_PICK, result.Description);
                                    m_nSeqNum = (int)STEP_PICKING.END;
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    break;

                case (int)STEP_PICKING.EXECUTE_SCENARIO_BEFORE_PICK + 2:
                    --m_nSeqNum;
                    break;
                #endregion </Execute scenario before pick>

                case (int)STEP_PICKING.PICK:
                    {
                        string description = string.Empty;
                        if (false == GetWorkingInformation(manualAction, ref _workingInfo, ref description))
                        {
                            GenerateAlarm((int)EN_ALARM.ATM_ROBOT_CANNOT_GET_WORKING_INFO);
                            m_nSeqNum = (int)STEP_PICKING.END;
                            break;
                        }
                        else
                        {
                            if (_workingInfo.IsLocationProcessModule())
                            {
                                m_nSeqNum = (int)STEP_PICKING.PREPARE_APPROACH_UNLOADING;
                                break;
                            }
                        }
                        
                        var result = _robotManager.Pick(RobotIndex, _workingInfo.ActionArm, _workingInfo.Location, false, _workingInfo.SubstrateName);
                        switch (result.CommandResult)
                        {
                            case CommandResult.Completed:
                            case CommandResult.Skipped:
                                {
                                    m_nSeqNum = (int)STEP_PICKING.END;
                                }
                                break;

                            case CommandResult.Timeout:
                            case CommandResult.Error:
                            case CommandResult.Invalid:
                                {
                                    string[] _arAlarmSubInfo = { GetTaskName(), string.Format("{0} - {1}", result.ActionName, result.Description) };
                                    GenerateSequenceAlarm((int)EN_ALARM.ATM_ROBOT_PICKING_ACTION_FAILED, false, ref _arAlarmSubInfo);
                                    m_nSeqNum = (int)STEP_PICKING.END;
                                }
                                break;

                            default:
                                ++m_nSeqNum;
                                break;
                        }
                    }
                    break;

                case (int)STEP_PICKING.PICK + 1:
                    --m_nSeqNum;
                    break;

                #region <Approach Unloading>
                case (int)STEP_PICKING.PREPARE_APPROACH_UNLOADING:
                    {
                        string description = string.Empty;
                        if (false == GetWorkingInformation(manualAction, ref _workingInfo, ref description))
                        {
                            GenerateAlarm((int)EN_ALARM.ATM_ROBOT_CANNOT_GET_WORKING_INFO);
                            m_nSeqNum = (int)STEP_PICKING.END;
                            break;
                        }

                        var result = IsApproachUnloadingPrepared();
                        switch (result.CommandResult)
                        {
                            case CommandResult.Proceed:
                                break;
                            case CommandResult.Completed:
                                {
                                    m_nSeqNum = (int)STEP_PICKING.ACTION_APPROACH_UNLOADING;
                                }
                                break;
                            case CommandResult.Skipped:
                                {
                                    m_nSeqNum = (int)STEP_PICKING.END;
                                }
                                break;
                            default:
                                {
                                    GenerateAlarm(result.AlarmCode, result.Description);
                                    m_nSeqNum = (int)STEP_PICKING.END;
                                }
                                break;
                        }
                    }
                    break;
                case (int)STEP_PICKING.ACTION_APPROACH_UNLOADING:
                    {
                        string description = string.Empty;
                        if (false == GetWorkingInformation(manualAction, ref _workingInfo, ref description))
                        {
                            GenerateAlarm((int)EN_ALARM.ATM_ROBOT_CANNOT_GET_WORKING_INFO);
                            m_nSeqNum = (int)STEP_PICKING.END;
                            break;
                        }
                        var result = _robotManager.ApproachForPick(RobotIndex, _workingInfo.ActionArm, _workingInfo.Location);
                        switch (result.CommandResult)
                        {
                            case CommandResult.Proceed:
                                ++m_nSeqNum;
                                break;
                            case CommandResult.Completed:
                                m_nSeqNum = (int)STEP_PICKING.APPROACH_UNLOADING_COMPLETED;
                                break;
                            case CommandResult.Skipped:
                                {
                                    m_nSeqNum = (int)STEP_PICKING.END;
                                }
                                break;
                            default:
                                {
                                    GenerateAlarm((int)EN_ALARM.ATM_ROBOT_APPROACH_UNLOADING_FAILED);
                                    m_nSeqNum = (int)STEP_PICKING.END;
                                }
                                break;
                        }
                    }
                    break;
                case (int)STEP_PICKING.ACTION_APPROACH_UNLOADING + 1:
                    --m_nSeqNum;
                    break;
                case (int)STEP_PICKING.APPROACH_UNLOADING_COMPLETED:
                    {
                        string description = string.Empty;
                        if (false == GetWorkingInformation(manualAction, ref _workingInfo, ref description))
                        {
                            GenerateAlarm((int)EN_ALARM.ATM_ROBOT_CANNOT_GET_WORKING_INFO);
                            m_nSeqNum = (int)STEP_PICKING.END;
                            break;
                        }

                        var result = IsApproachUnloadingCompleted();
                        switch (result.CommandResult)
                        {
                            case CommandResult.Proceed:
                                break;
                            case CommandResult.Completed:
                                m_nSeqNum = (int)STEP_PICKING.PREPARE_ACTION_UNLOADING;
                                break;
                            case CommandResult.Skipped:
                                {
                                    m_nSeqNum = (int)STEP_PICKING.END;
                                }
                                break;
                            default:
                                {
                                    GenerateAlarm(result.AlarmCode, result.Description);
                                    m_nSeqNum = (int)STEP_PICKING.END;
                                }
                                break;
                        }
                    }
                    break;
                #endregion </Approach Unloading>

                #region <Unloading>
                case (int)STEP_PICKING.PREPARE_ACTION_UNLOADING:
                    {
                        string description = string.Empty;
                        if (false == GetWorkingInformation(manualAction, ref _workingInfo, ref description))
                        {
                            GenerateAlarm((int)EN_ALARM.ATM_ROBOT_CANNOT_GET_WORKING_INFO);
                            m_nSeqNum = (int)STEP_PICKING.END;
                            break;
                        }

                        var result = IsUnloadingPrepared();
                        switch (result.CommandResult)
                        {
                            case CommandResult.Proceed:
                                break;
                            case CommandResult.Completed:
                                {
                                    m_nSeqNum = (int)STEP_PICKING.ACTION_UNLOADING;
                                }
                                break;
                            case CommandResult.Skipped:
                                {
                                    m_nSeqNum = (int)STEP_PICKING.END;
                                }
                                break;
                            default:
                                {
                                    GenerateAlarm(result.AlarmCode, result.Description);
                                    m_nSeqNum = (int)STEP_PICKING.END;
                                }
                                break;
                        }
                    }
                    break;
                case (int)STEP_PICKING.ACTION_UNLOADING:
                    {
                        string description = string.Empty;
                        if (false == GetWorkingInformation(manualAction, ref _workingInfo, ref description))
                        {
                            GenerateAlarm((int)EN_ALARM.ATM_ROBOT_CANNOT_GET_WORKING_INFO);
                            m_nSeqNum = (int)STEP_PICKING.END;
                            break;
                        }
                        var result = _robotManager.Pick(RobotIndex, _workingInfo.ActionArm, _workingInfo.Location, false, _workingInfo.SubstrateName);
                        switch (result.CommandResult)
                        {
                            case CommandResult.Proceed:
                                ++m_nSeqNum;
                                break;
                            case CommandResult.Completed:
                                m_nSeqNum = (int)STEP_PICKING.ACTION_UNLOADING_COMPLETED;
                                break;
                            case CommandResult.Skipped:
                                {
                                    m_nSeqNum = (int)STEP_PICKING.END;
                                }
                                break;
                            default:
                                {
                                    GenerateAlarm((int)EN_ALARM.ATM_ROBOT_UNLOADING_FAILED, result.Description);
                                    m_nSeqNum = (int)STEP_PICKING.END;
                                }
                                break;
                        }
                    }
                    break;
                case (int)STEP_PICKING.ACTION_UNLOADING + 1:
                    --m_nSeqNum;
                    break;
                case (int)STEP_PICKING.ACTION_UNLOADING_COMPLETED:
                    {
                        string description = string.Empty;
                        if (false == GetWorkingInformation(manualAction, ref _workingInfo, ref description))
                        {
                            GenerateAlarm((int)EN_ALARM.ATM_ROBOT_CANNOT_GET_WORKING_INFO);
                            m_nSeqNum = (int)STEP_PICKING.END;
                            break;
                        }
                        var result = IsUnloadingCompleted();
                        switch (result.CommandResult)
                        {
                            case CommandResult.Proceed:
                                break;
                            case CommandResult.Completed:
                                {
                                    RobotTicks.SetTickCount(5000);
                                    ++m_nSeqNum;
                                }
                                break;
                            case CommandResult.Skipped:
                                {
                                    m_nSeqNum = (int)STEP_PICKING.END;
                                }
                                break;
                            default:
                                {
                                    GenerateAlarm(result.AlarmCode, result.Description);
                                    m_nSeqNum = (int)STEP_PICKING.END;
                                }
                                break;
                        }
                    }
                    break;
                case (int)STEP_PICKING.ACTION_UNLOADING_COMPLETED + 1:
                    {
                        if (RobotTicks.IsTickOver(true))
                        {
                            GenerateAlarm((int)EN_ALARM.INTERFACE_AFTER_UNLOADING_SMEMA_TIMEOUT);
                            m_nSeqNum = (int)STEP_PICKING.END;
                            break;
                        }

                        string description = string.Empty;
                        if (false == GetWorkingInformation(manualAction, ref _workingInfo, ref description))
                        {
                            GenerateAlarm((int)EN_ALARM.ATM_ROBOT_CANNOT_GET_WORKING_INFO);
                            m_nSeqNum = (int)STEP_PICKING.END;
                            break;
                        }
                        int pmIndex = _processGroup.GetProcessModuleIndexByLocation(_workingInfo.Location.Name);
                        if (false == _processGroup.IsUnloadingRequested(pmIndex, _workingInfo.Location.Name) || _taskOperator.IsDryRunOrSimulationMode())
                        {
                            m_nSeqNum = (int)STEP_PICKING.EXECUTE_SCENARIO_AFTER_PICK;
                        }
                        else
                        {
                            ++m_nSeqNum;
                        }
                    }
                    break;
                case (int)STEP_PICKING.ACTION_UNLOADING_COMPLETED + 2:
                    {
                        --m_nSeqNum;
                    }
                    break;
                #endregion </Unloading>

                #region <Execute scenario after pick>
                case (int)STEP_PICKING.EXECUTE_SCENARIO_AFTER_PICK:
                    if (false == UpdateParamToAfterPick())
                    {
                        m_nSeqNum = (int)STEP_PICKING.UPDATE_LINK;
                    }
                    else
                    {
                        ++m_nSeqNum;
                    }
                    break;

                case (int)STEP_PICKING.EXECUTE_SCENARIO_AFTER_PICK + 1:
                    {
                        var result = ExecuteScenarioToAfterPick();
                        switch (result.CommandResult)
                        {
                            case CommandResult.Proceed:
                                break;
                            case CommandResult.Completed:
                            case CommandResult.Skipped:
                                {
                                    --m_nSeqNum;
                                }
                                break;
                            case CommandResult.Timeout:
                            case CommandResult.Error:
                            case CommandResult.Invalid:
                                {
                                    GenerateAlarm((int)EN_ALARM.ATM_ROBOT_SECSGEM_ERROR_AFTER_PICK, result.Description);
                                    m_nSeqNum = (int)STEP_PICKING.END;
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    break;

                case (int)STEP_PICKING.EXECUTE_SCENARIO_AFTER_PICK + 2:
                    --m_nSeqNum;
                    break;
                #endregion </Execute scenario after pick>

                case (int)STEP_PICKING.UPDATE_LINK:
                    {
                        string description = string.Empty;
                        if (false == GetWorkingInformation(manualAction, ref _workingInfo, ref description))
                        {
                            GenerateAlarm((int)EN_ALARM.ATM_ROBOT_CANNOT_GET_WORKING_INFO);
                            m_nSeqNum = (int)STEP_PICKING.END;
                            break;
                        }

                        Substrate substrate = new Substrate("");
                        if (false == _robotManager.GetSubstrate(RobotIndex, _workingInfo.SubstrateName, ref substrate))
                        {
                            GenerateAlarm((int)EN_ALARM.ATM_ROBOT_CANNOT_GET_WORKING_INFO);
                            m_nSeqNum = (int)STEP_PICKING.END;
                            break;
                        }
                        else
                        {
                            if (substrate.GetSourcePortId() <= 0 || substrate.GetSourceSlot() < 0)
                            {
                                GenerateAlarm((int)EN_ALARM.ATM_ROBOT_CANNOT_GET_WORKING_INFO);
                                m_nSeqNum = (int)STEP_PICKING.END;
                                break;
                            }
                        }
                        UpdateRecoveryDataAfterPick();
                        //SetRobotPortState(RobotScheduleType.Selection);
                        m_nSeqNum = (int)STEP_PICKING.END;
                    }
                    break;

                case (int)STEP_PICKING.END:
                    {
                        // 2024.12.29. jhlim [ADD] 위치 변경 -> 스킵 시 상태를 변경하여 다시 스케쥴링을 하기 위함
                        SetRobotPortState(RobotScheduleType.Selection);
                        // 2024.12.29. jhlim [MOD]
                        string description = string.Empty;
                        if (GetWorkingInformation(manualAction, ref _workingInfo, ref description))
                        {
                            int pmIndex = _processGroup.GetProcessModuleIndexByLocation(_workingInfo.Location.Name);
                            _processGroup.SetUnloadingSignal(pmIndex, _workingInfo.Location.Name, false);
                        }

                        if (manualAction)
                        {
                            _robotSchedulerManager.RemoveCurrentManualWorkingInfo(RobotIndex);
                        }
                    }
                    return true;

                default:
                    break;
            }

            return false;
        }
        protected virtual bool ActionPlace(bool manualAction = false)
        {
            switch (m_nSeqNum)
            {
                case (int)STEP_PLACING.START:
                    {
                        InitScenarioInfoPlace();
                        UpdateRecoveryDataBeforePlace();
                        InitMaterialHandlingInterface();
                        m_nSeqNum = (int)STEP_PLACING.CHECK_READY;
                    }
                    break;

                case (int)STEP_PLACING.CHECK_READY:
                    {
                        // 암의 Sub 정보와 Presence의 동기화 필요?
                        List<RobotArmTypes> availableArms = new List<RobotArmTypes>();
                        if (false == _taskOperator.IsDryRunMode())
                        {
                            if (false == _robotManager.GetAvailableArm(RobotIndex, false, ref availableArms))
                            {
                                GenerateAlarm((int)EN_ALARM.ATM_ROBOT_HAS_NO_AVAILABLE_ARM);
                                m_nSeqNum = (int)STEP_PLACING.END;
                                break;
                            }
                        }
                        _workingInfo = new RobotWorkingInfo();
                        _robotManager.InitAtmRobotAction(RobotIndex);
                        ++m_nSeqNum;
                    }
                    break;

                case (int)STEP_PLACING.CHECK_READY + 1:
                    {
                        if (_taskOperator.IsFinishingMode())
                        {
                            m_nSeqNum = (int)STEP_PLACING.END;
                            break;
                        }

                        m_nSeqNum = (int)STEP_PLACING.EXECUTE_SCENARIO_BEFORE_PLACE;
                    }
                    break;

                #region <Execute scenario before place>
                case (int)STEP_PLACING.EXECUTE_SCENARIO_BEFORE_PLACE:
                    if (false == UpdateParamToBeforePlace())
                    {
                        m_nSeqNum = (int)STEP_PLACING.PLACE;
                    }
                    else
                    {
                        ++m_nSeqNum;
                    }
                    break;

                case (int)STEP_PLACING.EXECUTE_SCENARIO_BEFORE_PLACE + 1:
                    {
                        var result = ExecuteScenarioToBeforePlace();
                        switch (result.CommandResult)
                        {
                            case CommandResult.Proceed:
                                break;
                            case CommandResult.Completed:
                            case CommandResult.Skipped:
                                {
                                    if (_taskOperator.IsFinishingMode())
                                    {
                                        m_nSeqNum = (int)STEP_PLACING.END;
                                        break;
                                    }

                                    --m_nSeqNum;
                                }
                                break;
                            case CommandResult.Timeout:
                            case CommandResult.Error:
                            case CommandResult.Invalid:
                                {
                                    GenerateAlarm((int)EN_ALARM.ATM_ROBOT_SECSGEM_ERROR_BEFORE_PLACE, result.Description);
                                    m_nSeqNum = (int)STEP_PLACING.END;
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    break;

                case (int)STEP_PLACING.EXECUTE_SCENARIO_BEFORE_PLACE + 2:
                    --m_nSeqNum;
                    break;
                #endregion </Execute scenario before place>

                case (int)STEP_PLACING.PLACE:
                    {
                        string description = string.Empty;
                        if (false == GetWorkingInformation(manualAction, ref _workingInfo, ref description))
                        {
                            GenerateAlarm((int)EN_ALARM.ATM_ROBOT_CANNOT_GET_WORKING_INFO);
                            m_nSeqNum = (int)STEP_PLACING.END;
                            break;
                        }
                        else
                        {
                            if (_workingInfo.IsLocationProcessModule())
                            {
                                m_nSeqNum = (int)STEP_PLACING.PREPARE_APPROACH_LOADING;
                                break;
                            }
                        }

                        var result = _robotManager.Place(RobotIndex, _workingInfo.ActionArm, _workingInfo.Location, false, _workingInfo.SubstrateName);
                        switch (result.CommandResult)
                        {
                            case CommandResult.Completed:
                            case CommandResult.Skipped:
                                {
                                    m_nSeqNum = (int)STEP_PLACING.EXECUTE_SCENARIO_AFTER_PLACE;
                                }
                                break;

                            case CommandResult.Timeout:
                            case CommandResult.Error:
                            case CommandResult.Invalid:
                                {
                                    string[] _arAlarmSubInfo = { GetTaskName(), string.Format("{0} - {1}", result.ActionName, result.Description) };
                                    GenerateSequenceAlarm((int)EN_ALARM.ATM_ROBOT_PLACING_ACTION_FAILED, false, ref _arAlarmSubInfo);
                                    m_nSeqNum = (int)STEP_PLACING.END;
                                }
                                break;

                            default:
                                ++m_nSeqNum;
                                break;
                        }
                    }
                    break;

                case (int)STEP_PLACING.PLACE + 1:
                    {
                        //if (_taskOperator.IsFinishingMode())
                        //{
                        //    m_nSeqNum = (int)STEP_INITIALIZE.END;
                        //    break;
                        //}
                    }
                    --m_nSeqNum;
                    break;

                #region <Approach Loading>
                case (int)STEP_PLACING.PREPARE_APPROACH_LOADING:
                    {
                        string description = string.Empty;
                        if (false == GetWorkingInformation(manualAction, ref _workingInfo, ref description))
                        {
                            GenerateAlarm((int)EN_ALARM.ATM_ROBOT_CANNOT_GET_WORKING_INFO);
                            m_nSeqNum = (int)STEP_PLACING.END;
                            break;
                        }

                        var result = IsApproachLoadingPrepared();
                        switch (result.CommandResult)
                        {
                            case CommandResult.Proceed:
                                break;
                            case CommandResult.Completed:
                                {
                                    m_nSeqNum = (int)STEP_PLACING.ACTION_APPROACH_LOADING;
                                }
                                break;
                            case CommandResult.Skipped:
                                {
                                    m_nSeqNum = (int)STEP_PLACING.END;
                                }
                                break;
                            default:
                                {
                                    GenerateAlarm(result.AlarmCode, result.Description);
                                    m_nSeqNum = (int)STEP_PLACING.END;
                                }
                                break;
                        }
                    }
                    break;

                case (int)STEP_PLACING.ACTION_APPROACH_LOADING:
                    {
                        string description = string.Empty;
                        if (false == GetWorkingInformation(manualAction, ref _workingInfo, ref description))
                        {
                            GenerateAlarm((int)EN_ALARM.ATM_ROBOT_CANNOT_GET_WORKING_INFO);
                            m_nSeqNum = (int)STEP_PLACING.END;
                            break;
                        }

                        var result = _robotManager.ApproachForPlace(RobotIndex, _workingInfo.ActionArm, _workingInfo.Location);
                        switch (result.CommandResult)
                        {
                            case CommandResult.Proceed:
                                ++m_nSeqNum;
                                break;
                            case CommandResult.Completed:
                                m_nSeqNum = (int)STEP_PLACING.APPROACH_LOADING_COMPLETED;
                                break;
                            case CommandResult.Skipped:
                                {
                                    m_nSeqNum = (int)STEP_PLACING.END;
                                }
                                break;
                            default:
                                {
                                    GenerateAlarm((int)EN_ALARM.ATM_ROBOT_APPROACH_LOADING_FAILED);
                                    m_nSeqNum = (int)STEP_PLACING.END;
                                }
                                break;
                        }
                    }
                    break;

                case (int)STEP_PLACING.ACTION_APPROACH_LOADING + 1:
                    {
                        --m_nSeqNum;
                    }
                    break;

                case (int)STEP_PLACING.APPROACH_LOADING_COMPLETED:
                    {
                        string description = string.Empty;
                        if (false == GetWorkingInformation(manualAction, ref _workingInfo, ref description))
                        {
                            GenerateAlarm((int)EN_ALARM.ATM_ROBOT_CANNOT_GET_WORKING_INFO);
                            m_nSeqNum = (int)STEP_PLACING.END;
                            break;
                        }
                        var result = IsApproachLoadingCompleted();
                        switch (result.CommandResult)
                        {
                            case CommandResult.Proceed:
                                break;
                            case CommandResult.Completed:
                                m_nSeqNum = (int)STEP_PLACING.PREPARE_ACTION_LOADING;
                                break;
                            case CommandResult.Skipped:
                                {
                                    m_nSeqNum = (int)STEP_PLACING.END;
                                }
                                break;
                            default:
                                {
                                    GenerateAlarm(result.AlarmCode, result.Description);
                                    m_nSeqNum = (int)STEP_PLACING.END;
                                }
                                break;
                        }
                    }
                    break;
                #endregion </Approach Loading>

                #region <Loading>
                case (int)STEP_PLACING.PREPARE_ACTION_LOADING:
                    {
                        string description = string.Empty;
                        if (false == GetWorkingInformation(manualAction, ref _workingInfo, ref description))
                        {
                            GenerateAlarm((int)EN_ALARM.ATM_ROBOT_CANNOT_GET_WORKING_INFO);
                            m_nSeqNum = (int)STEP_PLACING.END;
                            break;
                        }

                        var result = IsLoadingPrepared();
                        switch (result.CommandResult)
                        {
                            case CommandResult.Proceed:
                                break;
                            case CommandResult.Completed:
                                {
                                    m_nSeqNum = (int)STEP_PLACING.ACTION_LOADING;
                                }
                                break;
                            case CommandResult.Skipped:
                                {
                                    m_nSeqNum = (int)STEP_PLACING.END;
                                }
                                break;
                            default:
                                {
                                    GenerateAlarm(result.AlarmCode, result.Description);
                                    m_nSeqNum = (int)STEP_PLACING.END;
                                }
                                break;
                        }
                    }
                    break;

                case (int)STEP_PLACING.ACTION_LOADING:
                    {
                        string description = string.Empty;
                        if (false == GetWorkingInformation(manualAction, ref _workingInfo, ref description))
                        {
                            GenerateAlarm((int)EN_ALARM.ATM_ROBOT_CANNOT_GET_WORKING_INFO);
                            m_nSeqNum = (int)STEP_PLACING.END;
                            break;
                        }

                        var result = _robotManager.Place(RobotIndex, _workingInfo.ActionArm, _workingInfo.Location, false, _workingInfo.SubstrateName);
                        switch (result.CommandResult)
                        {
                            case CommandResult.Proceed:
                                ++m_nSeqNum;
                                break;
                            case CommandResult.Completed:
                                m_nSeqNum = (int)STEP_PLACING.ACTION_LOADING_COMPLETED;
                                break;
                            case CommandResult.Skipped:
                                {
                                    m_nSeqNum = (int)STEP_PLACING.END;
                                }
                                break;
                            default:
                                {
                                    GenerateAlarm((int)EN_ALARM.ATM_ROBOT_LOADING_FAILED);
                                    m_nSeqNum = (int)STEP_PLACING.END;
                                }
                                break;
                        }
                    }
                    break;

                case (int)STEP_PLACING.ACTION_LOADING + 1:
                    {
                        --m_nSeqNum;
                    }
                    break;

                case (int)STEP_PLACING.ACTION_LOADING_COMPLETED:
                    {
                        string description = string.Empty;
                        if (false == GetWorkingInformation(manualAction, ref _workingInfo, ref description))
                        {
                            GenerateAlarm((int)EN_ALARM.ATM_ROBOT_CANNOT_GET_WORKING_INFO);
                            m_nSeqNum = (int)STEP_PLACING.END;
                            break;
                        }

                        var result = IsLoadingCompleted();
                        switch (result.CommandResult)
                        {
                            case CommandResult.Proceed:
                                break;
                            case CommandResult.Completed:
                                {
                                    RobotTicks.SetTickCount(5000);
                                    ++m_nSeqNum;
                                }
                                break;
                            case CommandResult.Skipped:
                                {
                                    m_nSeqNum = (int)STEP_PLACING.END;
                                }
                                break;
                            default:
                                {
                                    GenerateAlarm(result.AlarmCode, result.Description);
                                    m_nSeqNum = (int)STEP_PLACING.END;
                                }
                                break;
                        }
                    }
                    break;
                case (int)STEP_PLACING.ACTION_LOADING_COMPLETED + 1:
                    {
                        if (RobotTicks.IsTickOver(true))
                        {
                            GenerateAlarm((int)EN_ALARM.INTERFACE_AFTER_LOADING_SMEMA_TIMEOUT);
                            m_nSeqNum = (int)STEP_PLACING.END;
                            break;
                        }

                        string description = string.Empty;
                        if (false == GetWorkingInformation(manualAction, ref _workingInfo, ref description))
                        {
                            GenerateAlarm((int)EN_ALARM.ATM_ROBOT_CANNOT_GET_WORKING_INFO);
                            m_nSeqNum = (int)STEP_PLACING.END;
                            break;
                        }
                        int pmIndex = _processGroup.GetProcessModuleIndexByLocation(_workingInfo.Location.Name);
                        if (false == _processGroup.IsLoadingRequested(pmIndex, _workingInfo.Location.Name) || _taskOperator.IsDryRunOrSimulationMode())
                        {
                            m_nSeqNum = (int)STEP_PLACING.EXECUTE_SCENARIO_AFTER_PLACE;
                        }
                        else
                        {
                            ++m_nSeqNum;
                        }
                    }
                    break;
                case (int)STEP_PLACING.ACTION_LOADING_COMPLETED + 2:
                    {
                        --m_nSeqNum;
                    }
                    break;
                #endregion </Loading>

                #region <Execute scenario after place>
                case (int)STEP_PLACING.EXECUTE_SCENARIO_AFTER_PLACE:
                    if (false == UpdateParamToAfterPlace())
                    {
                        m_nSeqNum = (int)STEP_PLACING.UPDATE_LINK;
                    }
                    else
                    {
                        ++m_nSeqNum;
                    }
                    break;

                case (int)STEP_PLACING.EXECUTE_SCENARIO_AFTER_PLACE + 1:
                    {
                        var result = ExecuteScenarioToAfterPlace();
                        switch (result.CommandResult)
                        {
                            case CommandResult.Proceed:
                                break;
                            case CommandResult.Completed:
                            case CommandResult.Skipped:
                                {
                                    --m_nSeqNum;
                                }
                                break;
                            case CommandResult.Timeout:
                            case CommandResult.Error:
                            case CommandResult.Invalid:
                                {
                                    GenerateAlarm((int)EN_ALARM.ATM_ROBOT_SECSGEM_ERROR_BEFORE_PLACE, result.Description);
                                    m_nSeqNum = (int)STEP_PLACING.END;
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    break;

                case (int)STEP_PLACING.EXECUTE_SCENARIO_AFTER_PLACE + 2:
                    --m_nSeqNum;
                    break;
                #endregion </Execute scenario after place>

                case (int)STEP_PLACING.UPDATE_LINK:
                    {
                        UpdateRecoveryDataAfterPlace();
                        //SetRobotPortState(RobotScheduleType.Selection);
                        m_nSeqNum = (int)STEP_PLACING.END;
                    }
                    break;

                case (int)STEP_PLACING.END:
                    {
                        // 2024.12.29. jhlim [ADD] 위치 변경 -> 스킵 시 상태를 변경하여 다시 스케쥴링을 하기 위함
                        SetRobotPortState(RobotScheduleType.Selection);
                        // 2024.12.29. jhlim [MOD]

                        string description = string.Empty;
                        if (GetWorkingInformation(manualAction, ref _workingInfo, ref description))
                        {
                            int pmIndex = _processGroup.GetProcessModuleIndexByLocation(_workingInfo.Location.Name);
                            _processGroup.SetLoadingSignal(pmIndex, _workingInfo.Location.Name, false);
                        }

                        if (manualAction)
                        {
                            _robotSchedulerManager.RemoveCurrentManualWorkingInfo(RobotIndex);
                        }

                    }
                    return true;

                default:
                    break;
            }

            return false;
        }

        #region <Gem Simul Only>
        //List<int> _coreSlots;
        //List<int> _binSlots;
        //int _curSubCore = 0;
        //int _curSubBin = 0;
        //int _curStep = 0;
        //private bool ActionGemSimul()
        //{
        //    switch (m_nSeqNum)
        //    {
        //        case 0:
        //            _curStep = 0;
        //            _curSubCore = 0;
        //            _curSubBin = 0;
        //            _coreSlots = new List<int>();
        //            _binSlots = new List<int>();
        //            _taskOperator.InitSimulInfos();
        //            _scenarioOperator.InitScenarioAll();
        //            m_nSeqNum = 10;
        //            break;

        //        #region <Port Status Load ~ Slot Info>
        //        #region <Port Status Load>
        //        case 10:
        //            {
        //                _scenarioOperator.UpdateScenarioParam(ScenarioListTypes.SCENARIO_PORT_STATUS_LOAD,
        //                    new Dictionary<string, string>()
        //                    {
        //                        ["PORTID"] = _taskOperator.CarrierForSimuls[_curStep].PortId,
        //                        ["CARRIERID"] = _taskOperator.CarrierForSimuls[_curStep].CarrierId,
        //                        ["CARRIER_TYPE"] = "CASSETTE",
        //                        ["STATUS"] = "UNLOAD",
        //                        ["OPERID"] = "AUTO"
        //                    });
        //                ++m_nSeqNum;
        //                SetDelayForSequence(1000);
        //            }
        //            break;
        //        case 11:
        //            {
        //                var result = _scenarioOperator.ExecuteScenario(ScenarioListTypes.SCENARIO_PORT_STATUS_LOAD);
        //                switch (result)
        //                {
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.PROCEED:
        //                        break;
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.COMPLETED:
        //                        m_nSeqNum = 20;
        //                        break;
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.ERROR:                                
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.TIMEOUT_ERROR:
        //                        return true;
                                
        //                    default:
        //                        break;
        //                }
        //            }
        //            break;
        //        #endregion </Port Status Load>

        //        #region <Carrier Load>
        //        case 20:
        //            {
        //                _scenarioOperator.UpdateScenarioParam(ScenarioListTypes.SCENARIO_CARRIER_LOAD,
        //                    new Dictionary<string, string>()
        //                    {
        //                        ["CARRIERID"] = _taskOperator.CarrierForSimuls[_curStep].CarrierId,
        //                        ["LOTID"] = _taskOperator.CarrierForSimuls[_curStep].LotId,
        //                    });
        //                ++m_nSeqNum;
        //                SetDelayForSequence(1000);
        //            }
        //            break;
        //        case 21:
        //            {
        //                var result = _scenarioOperator.ExecuteScenario(ScenarioListTypes.SCENARIO_CARRIER_LOAD);
        //                switch (result)
        //                {
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.PROCEED:
        //                        break;
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.COMPLETED:
        //                        m_nSeqNum = 30;
        //                        break;
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.ERROR:
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.TIMEOUT_ERROR:
        //                        return true;

        //                    default:
        //                        break;
        //                }
        //            }
        //            break;
        //        #endregion </Carrier Load>

        //        #region <RFID Read>
        //        case 30:
        //            {
        //                var scenario = ScenarioListTypes.SCENARIO_RFID_READ_CORE_1;
        //                int scenarioIndex = _taskOperator.GetScenarioByStep(_curStep, scenario);
        //                if (scenarioIndex < 0)
        //                {
        //                    m_nSeqNum = 40;
        //                    break;
        //                }
        //                else
        //                {
        //                    scenario = (ScenarioListTypes)scenarioIndex;
        //                    _scenarioOperator.UpdateScenarioParam(scenario,
        //                        new Dictionary<string, string>()
        //                        {
        //                            ["CARRIERID"] = _taskOperator.CarrierForSimuls[_curStep].CarrierId,
        //                            ["LOTID"] = _taskOperator.CarrierForSimuls[_curStep].LotId,
        //                        });
        //                    ++m_nSeqNum;
        //                    SetDelayForSequence(1000);
        //                }
        //            }
        //            break;
        //        case 31:
        //            {
        //                var scenario = ScenarioListTypes.SCENARIO_RFID_READ_CORE_1;
        //                int scenarioIndex = _taskOperator.GetScenarioByStep(_curStep, scenario);
        //                if(scenarioIndex < 0)
        //                {
        //                    m_nSeqNum = 40;
        //                    break;
        //                }
        //                else
        //                {
        //                    scenario = (ScenarioListTypes)scenarioIndex;
        //                    var result = _scenarioOperator.ExecuteScenario(scenario);
        //                    switch (result)
        //                    {
        //                        case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.PROCEED:
        //                            break;
        //                        case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.COMPLETED:
        //                            m_nSeqNum = 40;
        //                            break;
        //                        case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.ERROR:
        //                        case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.TIMEOUT_ERROR:
        //                            return true;

        //                        default:
        //                            break;
        //                    }
        //                }                       
        //            }
        //            break;
        //        #endregion </RFID Read>

        //        #region <LOT Info>
        //        case 40:
        //            {
        //                var scenario = ScenarioListTypes.SCENARIO_REQ_LOT_INFO_CORE_1;
        //                int scenarioIndex = _taskOperator.GetScenarioByStep(_curStep, scenario);
        //                if (scenarioIndex < 0)
        //                {
        //                    m_nSeqNum = 50;
        //                    break;
        //                }
        //                else
        //                {
        //                    scenario = (ScenarioListTypes)scenarioIndex;
        //                    _scenarioOperator.UpdateScenarioParam(scenario,
        //                        new Dictionary<string, string>()
        //                        {
        //                            ["CARRIERID"] = _taskOperator.CarrierForSimuls[_curStep].CarrierId,
        //                            ["LOTID"] = _taskOperator.CarrierForSimuls[_curStep].LotId,
        //                        });
        //                    ++m_nSeqNum;
        //                    SetDelayForSequence(1000);
        //                }
        //            }
        //            break;
        //        case 41:
        //            {
        //                var scenario = ScenarioListTypes.SCENARIO_REQ_LOT_INFO_CORE_1;
        //                int scenarioIndex = _taskOperator.GetScenarioByStep(_curStep, scenario);
        //                if (scenarioIndex < 0)
        //                {
        //                    m_nSeqNum = 50;
        //                    break;
        //                }
        //                else
        //                {
        //                    scenario = (ScenarioListTypes)scenarioIndex;

        //                    var result = _scenarioOperator.ExecuteScenario(scenario);
        //                    switch (result)
        //                    {
        //                        case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.PROCEED:
        //                            break;
        //                        case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.COMPLETED:
        //                            {
        //                                var resultData = _scenarioOperator.GetScenarioResultData(scenario);
        //                                _taskOperator.CarrierForSimuls[_curStep].LotId = resultData["LotId"];
        //                                _taskOperator.CarrierForSimuls[_curStep].PartId = resultData["PartId"];
        //                                _taskOperator.CarrierForSimuls[_curStep].StepSeq = resultData["StepSeq"];
        //                                _taskOperator.CarrierForSimuls[_curStep].LotType = resultData["LotType"];
        //                                 if (int.TryParse(resultData["LotQty"], out int qty))
        //                                {
        //                                    _taskOperator.CarrierForSimuls[_curStep].LotQty = qty;
        //                                }
        //                                _taskOperator.CarrierForSimuls[_curStep].RecipeId = resultData["RecipeId"];
        //                            }
        //                            m_nSeqNum = 50;
        //                            break;
        //                        case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.ERROR:
        //                        case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.TIMEOUT_ERROR:
        //                            return true;

        //                        default:
        //                            break;
        //                    }
        //                }
        //            }
        //            break;
        //        #endregion </LOT Info>

        //        #region <Slot Info>
        //        case 50:
        //            {
        //                var scenario = ScenarioListTypes.SCENARIO_REQ_SLOT_INFO_CORE_1;
        //                int scenarioIndex = _taskOperator.GetScenarioByStep(_curStep, scenario);
        //                if (scenarioIndex < 0)
        //                {
        //                    m_nSeqNum = 60;
        //                    break;
        //                }
        //                else
        //                {
        //                    scenario = (ScenarioListTypes)scenarioIndex;
        //                    _scenarioOperator.UpdateScenarioParam(scenario,
        //                        new Dictionary<string, string>()
        //                        {
        //                            ["CARRIERID"] = _taskOperator.CarrierForSimuls[_curStep].CarrierId,
        //                            ["LOTID"] = _taskOperator.CarrierForSimuls[_curStep].LotId,
        //                        });
        //                    ++m_nSeqNum;
        //                    SetDelayForSequence(1000);
        //                }
        //            }
        //            break;
        //        case 51:
        //            {
        //                var scenario = ScenarioListTypes.SCENARIO_REQ_SLOT_INFO_CORE_1;
        //                int scenarioIndex = _taskOperator.GetScenarioByStep(_curStep, scenario);
        //                if (scenarioIndex < 0)
        //                {
        //                    m_nSeqNum = 60;
        //                    break;
        //                }
        //                else
        //                {
        //                    scenario = (ScenarioListTypes)scenarioIndex;
        //                    var result = _scenarioOperator.ExecuteScenario(scenario);
        //                    switch (result)
        //                    {
        //                        case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.PROCEED:
        //                            break;
        //                        case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.COMPLETED:
        //                            {
        //                                string lotId = _taskOperator.CarrierForSimuls[_curStep].LotId;
        //                                var resultData = _scenarioOperator.GetScenarioResultData(scenario);
        //                                if (int.TryParse(resultData["Capacity"], out int capacity))
        //                                {
        //                                    int capa = capacity;
        //                                    for (int i = 0; i < capa; ++i)
        //                                    {
        //                                        string keyStatus = string.Format("Status_{0}", i);
        //                                        if (false == resultData.TryGetValue(keyStatus, out string state))
        //                                            continue;

        //                                        if (false == state.Equals("4"))
        //                                            continue;

        //                                        string keyName = string.Format("Name_{0}", i);
        //                                        if (resultData.TryGetValue(keyName, out string subName))
        //                                        {
        //                                            _taskOperator.CarrierForSimuls[_curStep].Substrates[i] = new SubstrateInfoForSimul(lotId, subName);
        //                                            if( _curStep == 0)
        //                                            {
        //                                                _coreSlots.Add(i);
        //                                                _taskOperator.CarrierForSimuls[_curStep].Substrates[i].SubstrateType = "Core";
        //                                            }
        //                                            else if(_curStep == 1)
        //                                            {
        //                                                _binSlots.Add(i);
        //                                                _taskOperator.CarrierForSimuls[_curStep].Substrates[i].SubstrateType = "Empty";
        //                                            }
        //                                        }
        //                                    }
        //                                }                                        
        //                            }
        //                            m_nSeqNum = 60;
        //                            break;
        //                        case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.ERROR:
        //                        case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.TIMEOUT_ERROR:
        //                            return true;

        //                        default:
        //                            break;
        //                    }
        //                }
        //            }
        //            break;
        //        #endregion </Slot Info>

        //        #region <결산>
        //        case 60:
        //            {
        //                if (_curStep >= 3)
        //                {
        //                    _curStep = 0;
        //                    m_nSeqNum = 100;
        //                }
        //                else
        //                {
        //                    ++_curStep;
        //                    m_nSeqNum = 10;
        //                }
        //            }
        //            break;
        //        #endregion </결산>
        //        #endregion </Port Status Load ~ Slot Info>

        //        #region <Recipe Download ~ Track In>

        //        #region <Request Download Recipe>
        //        case 100:
        //            {
        //                _scenarioOperator.UpdateScenarioParam(ScenarioListTypes.SCENARIO_REQ_RECIPE_DOWNLOAD,
        //                    new Dictionary<string, string>()
        //                    {
        //                        ["LOTID"] = _taskOperator.CarrierForSimuls[_curStep].LotId,
        //                        ["RECIPEID"] = _taskOperator.CarrierForSimuls[_curStep].RecipeId,
        //                        ["PARTID"] = _taskOperator.CarrierForSimuls[_curStep].PartId,
        //                        ["STEPSEQ"] = _taskOperator.CarrierForSimuls[_curStep].StepSeq,
        //                        ["LOTTYPE"] = _taskOperator.CarrierForSimuls[_curStep].LotType,
        //                    });
        //                ++m_nSeqNum;
        //                SetDelayForSequence(1000);
        //            }
        //            break;
        //        case 101:
        //            {
        //                var result = _scenarioOperator.ExecuteScenario(ScenarioListTypes.SCENARIO_REQ_RECIPE_DOWNLOAD);
        //                switch (result)
        //                {
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.PROCEED:
        //                        break;
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.COMPLETED:
        //                        m_nSeqNum = 110;
        //                        break;
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.ERROR:
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.TIMEOUT_ERROR:
        //                        return true;

        //                    default:
        //                        break;
        //                }
        //            }
        //            break;
        //        #endregion </Request Download Recipe>

        //        #region <Request Core Track In>
        //        case 110:
        //            {
        //                _scenarioOperator.UpdateScenarioParam(ScenarioListTypes.SCENARIO_REQ_TRACK_IN,
        //                    new Dictionary<string, string>()
        //                    {
        //                        ["CARRIERID"] = _taskOperator.CarrierForSimuls[_curStep].CarrierId,
        //                        ["PORTID"] = _taskOperator.CarrierForSimuls[_curStep].PortId,
        //                        ["LOTID"] = _taskOperator.CarrierForSimuls[_curStep].LotId,
        //                        ["PARTID"] = _taskOperator.CarrierForSimuls[_curStep].PartId,
        //                        ["STEPSEQ"] = _taskOperator.CarrierForSimuls[_curStep].StepSeq,
        //                        ["RECIPEID"] = _taskOperator.CarrierForSimuls[_curStep].RecipeId,
        //                        ["OPERID"] = "AUTO"
        //                    });
        //                ++m_nSeqNum;
        //                SetDelayForSequence(1000);
        //            }
        //            break;
        //        case 111:
        //            {
        //                var result = _scenarioOperator.ExecuteScenario(ScenarioListTypes.SCENARIO_REQ_TRACK_IN);
        //                switch (result)
        //                {
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.PROCEED:
        //                        break;
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.COMPLETED:
        //                        m_nSeqNum = 120;
        //                        break;
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.ERROR:
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.TIMEOUT_ERROR:
        //                        return true;

        //                    default:
        //                        break;
        //                }
        //            }
        //            break;
        //        #endregion </Request Core Track In>

        //        #region <Process Start>
        //        case 120:
        //            {
        //                _scenarioOperator.UpdateScenarioParam(ScenarioListTypes.SCENARIO_PROCESS_START,
        //                    new Dictionary<string, string>()
        //                    {
        //                        ["CARRIERID"] = _taskOperator.CarrierForSimuls[_curStep].CarrierId,
        //                        ["PORTID"] = _taskOperator.CarrierForSimuls[_curStep].PortId,
        //                        ["LOTID"] = _taskOperator.CarrierForSimuls[_curStep].LotId,
        //                        ["PARTID"] = _taskOperator.CarrierForSimuls[_curStep].PartId,
        //                        ["RECIPEID"] = _taskOperator.CarrierForSimuls[_curStep].RecipeId,
        //                        ["OPERID"] = "AUTO"
        //                    });
        //                ++m_nSeqNum;
        //                SetDelayForSequence(1000);
        //            }
        //            break;
        //        case 121:
        //            {
        //                var result = _scenarioOperator.ExecuteScenario(ScenarioListTypes.SCENARIO_PROCESS_START);
        //                switch (result)
        //                {
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.PROCEED:
        //                        break;
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.COMPLETED:
        //                        m_nSeqNum = 130;
        //                        break;
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.ERROR:
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.TIMEOUT_ERROR:
        //                        return true;

        //                    default:
        //                        break;
        //                }
        //            }
        //            break;
        //        #endregion </Process Start>

        //        #region <Request Lot Match>
        //        case 130:
        //            {
        //                _curStep = 1;
        //                _scenarioOperator.UpdateScenarioParam(ScenarioListTypes.SCENARIO_REQ_LOT_MATCH,
        //                    new Dictionary<string, string>()
        //                    {
        //                        ["LOTID"] = _taskOperator.CarrierForSimuls[_curStep].LotId,
        //                        ["CARRIERID"] = _taskOperator.CarrierForSimuls[_curStep].CarrierId,
        //                    });
        //                ++m_nSeqNum;
        //                SetDelayForSequence(1000);
        //            }
        //            break;
        //        case 131:
        //            {
        //                var result = _scenarioOperator.ExecuteScenario(ScenarioListTypes.SCENARIO_REQ_LOT_MATCH);
        //                switch (result)
        //                {
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.PROCEED:
        //                        break;
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.COMPLETED:
        //                        _curStep = 0;
        //                        m_nSeqNum = 200;
        //                        break;
                                
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.ERROR:
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.TIMEOUT_ERROR:
        //                        return true;

        //                    default:

        //                        break;
        //                }
        //            }
        //            break;
        //        #endregion </Request Lot Match>
        //        #endregion </Recipe Download ~ Track In>

        //        #region <Work Start ~ Req Chip Split>

        //        #region <Work Start>
        //        case 200:
        //            {
        //                int key = _coreSlots[_curSubCore];
        //                _scenarioOperator.UpdateScenarioParam(ScenarioListTypes.SCENARIO_WORK_START,
        //                    new Dictionary<string, string>()
        //                    {
        //                        ["CARRIERID"] = _taskOperator.CarrierForSimuls[_curStep].CarrierId,
        //                        ["PORTID"] = _taskOperator.CarrierForSimuls[_curStep].PortId,
        //                        ["LOTID"] = _taskOperator.CarrierForSimuls[_curStep].LotId,
        //                        ["PARTID"] = _taskOperator.CarrierForSimuls[_curStep].PartId,
        //                        ["RECIPEID"] = _taskOperator.CarrierForSimuls[_curStep].RecipeId,
        //                        ["OPERID"] = "AUTO",
        //                        ["WAFERID"] = _taskOperator.CarrierForSimuls[_curStep].Substrates[key].SubstrateId,
        //                        ["ANGLE"] = _taskOperator.CarrierForSimuls[_curStep].Substrates[key].Angle.ToString()
        //                    });
        //                ++m_nSeqNum;
        //                SetDelayForSequence(1000);
        //            }
        //            break;
        //        case 201:
        //            {
        //                var result = _scenarioOperator.ExecuteScenario(ScenarioListTypes.SCENARIO_WORK_START);
        //                switch (result)
        //                {
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.PROCEED:
        //                        break;
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.COMPLETED:
        //                        {
        //                            var resultData = _scenarioOperator.GetScenarioResultData(ScenarioListTypes.SCENARIO_WORK_START);

        //                            int key = _coreSlots[_curSubCore];
        //                            SubstrateInfoForSimul substrate = _taskOperator.CarrierForSimuls[_curStep].Substrates[key];

        //                            double.TryParse(resultData["Angle"], out double angle);
        //                            int.TryParse(resultData["CountRow"], out int countRow);
        //                            int.TryParse(resultData["CountCol"], out int countCol);
        //                            int.TryParse(resultData["Qty"], out int qty);

        //                            substrate.SubstrateId = resultData["SubstrateId"];
        //                            substrate.Angle = angle;
        //                            substrate.CountRow = countRow;
        //                            substrate.CountCol = countCol;
        //                            substrate.Qty = qty;
        //                            substrate.MapData = resultData["MapData"];

        //                            _taskOperator.CarrierForSimuls[_curStep].Substrates[key] = substrate;
        //                        }
        //                        m_nSeqNum = 210;
        //                        break;
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.ERROR:
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.TIMEOUT_ERROR:
        //                        return true;

        //                    default:
        //                        break;
        //                }
        //            }
        //            break;
        //        #endregion </Work Start>

        //        #region <Lot Split>
        //        case 210:
        //            {
        //                int key = _coreSlots[_curSubCore];
        //                _scenarioOperator.UpdateScenarioParam(ScenarioListTypes.SCENARIO_REQ_CORE_WAFER_SPLIT,
        //                    new Dictionary<string, string>()
        //                    {
        //                        ["LOTID"] = _taskOperator.CarrierForSimuls[_curStep].LotId,
        //                        ["WAFERID"] = _taskOperator.CarrierForSimuls[_curStep].Substrates[key].SubstrateId,
        //                        ["PARTID"] = _taskOperator.CarrierForSimuls[_curStep].PartId,
        //                        ["RECIPEID"] = _taskOperator.CarrierForSimuls[_curStep].RecipeId,
        //                        ["SLOTID"] = key.ToString(),
        //                        ["OPERID"] = "AUTO"
        //                    });
        //                ++m_nSeqNum;
        //                SetDelayForSequence(1000);
        //            }
        //            break;
        //        case 211:
        //            {
        //                var result = _scenarioOperator.ExecuteScenario(ScenarioListTypes.SCENARIO_REQ_CORE_WAFER_SPLIT);
        //                switch (result)
        //                {
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.PROCEED:
        //                        break;
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.COMPLETED:
        //                        {
        //                            var resultData = _scenarioOperator.GetScenarioResultData(ScenarioListTypes.SCENARIO_REQ_CORE_WAFER_SPLIT);

        //                            int key = _coreSlots[_curSubCore];
        //                            SubstrateInfoForSimul substrate = _taskOperator.CarrierForSimuls[_curStep].Substrates[key];
        //                            substrate.LotId = resultData["LotId"];
        //                            int.TryParse(resultData["Qty"], out int qty);
        //                            substrate.Qty = qty;

        //                            _taskOperator.CarrierForSimuls[_curStep].Substrates[key] = substrate;
        //                        }
        //                        m_nSeqNum = 220;
        //                        break;
                                
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.ERROR:
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.TIMEOUT_ERROR:
        //                        return true;

        //                    default:
        //                        break;
        //                }
        //            }
        //            break;
        //        #endregion </Lot Split>

        //        #region <Detach Start>
        //        case 220:
        //            {
        //                _curStep = 0;
        //                int key = _coreSlots[_curSubCore];
        //                _scenarioOperator.UpdateScenarioParam(ScenarioListTypes.SCENARIO_CORE_WAFER_DETACH_START,
        //                    new Dictionary<string, string>()
        //                    {
        //                        ["CARRIERID"] = _taskOperator.CarrierForSimuls[_curStep].CarrierId,
        //                        ["PORTID"] = _taskOperator.CarrierForSimuls[_curStep].PortId,
        //                        ["LOTID"] = _taskOperator.CarrierForSimuls[_curStep].LotId,
        //                        ["PARTID"] = _taskOperator.CarrierForSimuls[_curStep].PartId,
        //                        ["RECIPEID"] = _taskOperator.CarrierForSimuls[_curStep].RecipeId,
        //                        ["WAFERID"] = _taskOperator.CarrierForSimuls[_curStep].Substrates[key].SubstrateId,
        //                        ["SLOTID"] = key.ToString(),
        //                        ["OPERID"] = "AUTO",
        //                        ["NEEDLE_HEIGHT"] = "0.3",
        //                        ["EXPENSION_HEIGHT"] = "5",
        //                        ["PICK_SEARCH_LEVEL"] = "3",
        //                        ["PICK_SEARCH_SPEED"] = "3",
        //                        ["PICK_DELAY"] = "5000",
        //                        ["PICK_FORCE"] = "150",
        //                        ["PICK_SLOWUP_LEVEL"] = "3",
        //                        ["PICK_SLOWUP_SPEED"] = "3",
        //                        ["PLACE_SEARCH_LEVEL"] = "3",
        //                        ["PLACE_SEARCH_SPEED"] = "3",
        //                        ["PLACE_DELAY"] = "500",
        //                        ["PLACE_FORCE"] = "150",
        //                        ["PLACE_SLOWUP_LEVEL"] = "3",
        //                        ["PLACE_SLOWUP_SPEED"] = "3"
        //                    });
        //                ++m_nSeqNum;
        //                SetDelayForSequence(1000);
        //            }
        //            break;
        //        case 221:
        //            {
        //                var result = _scenarioOperator.ExecuteScenario(ScenarioListTypes.SCENARIO_CORE_WAFER_DETACH_START);
        //                switch (result)
        //                {
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.PROCEED:
        //                        break;
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.COMPLETED:
        //                        {
        //                            m_nSeqNum = 230;
        //                            break;
        //                        }

        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.ERROR:
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.TIMEOUT_ERROR:
        //                        return true;

        //                    default:
        //                        break;
        //                }
        //            }
        //            break;
        //        #endregion </Detach Start>

        //        #region <Empty Wafer Id Read>
        //        case 230:
        //            {
        //                _curStep = 1;
        //                int key = _binSlots[_curSubBin];
        //                _scenarioOperator.UpdateScenarioParam(ScenarioListTypes.SCENARIO_BIN_WAFER_ID_READ,
        //                    new Dictionary<string, string>()
        //                    {
        //                        ["LOTID"] = _taskOperator.CarrierForSimuls[_curStep].LotId,
        //                        ["WAFERID"] = _taskOperator.CarrierForSimuls[_curStep].Substrates[key].SubstrateId,
        //                    });
        //                ++m_nSeqNum;
        //                SetDelayForSequence(1000);
        //            }
        //            break;
        //        case 231:
        //            {
        //                var result = _scenarioOperator.ExecuteScenario(ScenarioListTypes.SCENARIO_BIN_WAFER_ID_READ);
        //                switch (result)
        //                {
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.PROCEED:
        //                        break;
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.COMPLETED:
        //                        {
        //                            m_nSeqNum = 240;
        //                            break;
        //                        }

        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.ERROR:
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.TIMEOUT_ERROR:
        //                        return true;

        //                    default:
        //                        break;
        //                }
        //            }
        //            break;
        //        #endregion </Empty Wafer Id Read>

        //        #region <Bin Sorting Start>
        //        case 240:
        //            {
        //                int key = _binSlots[_curSubBin];
        //                var scenario = ScenarioListTypes.SCENARIO_BIN_SORTING_START_1;
        //                if (_curSubBin == 0)
        //                {
        //                    _taskOperator.CarrierForSimuls[_curStep].Substrates[key].SubstrateType = "Bin1";
        //                    scenario = ScenarioListTypes.SCENARIO_BIN_SORTING_START_1;
        //                }
        //                else
        //                {
        //                    _taskOperator.CarrierForSimuls[_curStep].Substrates[key].SubstrateType = "Bin2";
        //                    scenario = ScenarioListTypes.SCENARIO_BIN_SORTING_START_2;
        //                }

        //                _scenarioOperator.UpdateScenarioParam(scenario,
        //                    new Dictionary<string, string>()
        //                    {
        //                        ["LOTID"] = _taskOperator.CarrierForSimuls[_curStep].LotId,
        //                        ["BIN_TYPE"] = _taskOperator.CarrierForSimuls[_curStep].Substrates[key].SubstrateType,
        //                        ["RINGFRAME_ID"] = _taskOperator.CarrierForSimuls[_curStep].Substrates[key].SubstrateId,
        //                    });
        //                ++m_nSeqNum;
        //                SetDelayForSequence(1000);
        //            }
        //            break;
        //        case 241:
        //            {
        //                var scenario = ScenarioListTypes.SCENARIO_BIN_SORTING_START_1;
        //                if (_curSubBin == 0)
        //                {
        //                    scenario = ScenarioListTypes.SCENARIO_BIN_SORTING_START_1;
        //                }
        //                else
        //                {
        //                    scenario = ScenarioListTypes.SCENARIO_BIN_SORTING_START_2;
        //                }
        //                var result = _scenarioOperator.ExecuteScenario(scenario);
        //                switch (result)
        //                {
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.PROCEED:
        //                        break;
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.COMPLETED:
        //                        {
        //                            m_nSeqNum = 250;
        //                            break;
        //                        }

        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.ERROR:
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.TIMEOUT_ERROR:
        //                        return true;

        //                    default:
        //                        break;
        //                }
        //            }
        //            break;
        //        #endregion </Bin Sorting Start>

        //        #region <Request Chip Split>
        //        case 250:
        //            {
        //                int keyCore = _coreSlots[_curSubCore];
        //                int keyBin = _binSlots[_curSubBin];

        //                _scenarioOperator.UpdateScenarioParam(ScenarioListTypes.SCENARIO_REQ_CORE_CHIP_SPLIT_FIRST,
        //                    new Dictionary<string, string>()
        //                    {
        //                        ["LOTID"] = _taskOperator.CarrierForSimuls[0].LotId,
        //                        ["SPLIT_WAFERID"] = _taskOperator.CarrierForSimuls[0].Substrates[keyCore].SubstrateId,
        //                        ["RINGFRAME_ID"] = _taskOperator.CarrierForSimuls[1].Substrates[keyBin].SubstrateId,
        //                        ["BIN_TYPE"] = _taskOperator.CarrierForSimuls[1].Substrates[keyBin].SubstrateType,
        //                        // [Gem Simul] 지금은 그냥 QTY인데, Bin 별로 따로 저장해야할듯
        //                        ["CHIP_QTY"] = _taskOperator.CarrierForSimuls[0].Substrates[keyCore].Qty.ToString(),
        //                    });
        //                ++m_nSeqNum;
        //                SetDelayForSequence(1000);
        //            }
        //            break;
        //        case 251:
        //            {
        //                var result = _scenarioOperator.ExecuteScenario(ScenarioListTypes.SCENARIO_REQ_CORE_CHIP_SPLIT_FIRST);
        //                switch (result)
        //                {
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.PROCEED:
        //                        break;
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.COMPLETED:
        //                        {
        //                            var resultData = _scenarioOperator.GetScenarioResultData(ScenarioListTypes.SCENARIO_REQ_CORE_CHIP_SPLIT_FIRST);

        //                            int key = _binSlots[_curSubBin];
        //                            SubstrateInfoForSimul substrate = _taskOperator.CarrierForSimuls[1].Substrates[key];
        //                            substrate.LotId = resultData["LotId"];
        //                            _taskOperator.CarrierForSimuls[1].Substrates[key] = substrate;

        //                            // [Gem Simul] : 여기서 분기
        //                            if (++_curSubBin < _binSlots.Count)
        //                            {
        //                                m_nSeqNum = 230;
        //                            }
        //                            else
        //                            {
        //                                m_nSeqNum = 300;
        //                            }
        //                            break;
        //                        }

        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.ERROR:
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.TIMEOUT_ERROR:
        //                        return true;

        //                    default:
        //                        break;
        //                }
        //            }
        //            break;
        //        #endregion </Request Chip Split>

        //        #endregion </Work Start ~ Req Chip Split>

        //        #region <Detach End ~ Core Track out>

        //        #region <Detach End>
        //        case 300:
        //            {
        //                _curStep = 0;
        //                _curSubCore = 0;
        //                int key = _coreSlots[_curSubCore];
        //                _scenarioOperator.UpdateScenarioParam(ScenarioListTypes.SCENARIO_CORE_WAFER_DETACH_END,
        //                    new Dictionary<string, string>()
        //                    {
        //                        ["CARRIERID"] = _taskOperator.CarrierForSimuls[_curStep].CarrierId,
        //                        ["PORTID"] = _taskOperator.CarrierForSimuls[_curStep].PortId,
        //                        ["LOTID"] = _taskOperator.CarrierForSimuls[_curStep].LotId,
        //                        ["PARTID"] = _taskOperator.CarrierForSimuls[_curStep].PartId,
        //                        ["RECIPEID"] = _taskOperator.CarrierForSimuls[_curStep].RecipeId,
        //                        ["WAFERID"] = _taskOperator.CarrierForSimuls[_curStep].Substrates[key].SubstrateId,
        //                        ["SLOTID"] = key.ToString(),
        //                        ["OPERID"] = "AUTO"                                
        //                    });
        //                ++m_nSeqNum;
        //                SetDelayForSequence(1000);
        //            }
        //            break;
        //        case 301:
        //            {
        //                var result = _scenarioOperator.ExecuteScenario(ScenarioListTypes.SCENARIO_CORE_WAFER_DETACH_END);
        //                switch (result)
        //                {
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.PROCEED:
        //                        break;
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.COMPLETED:
        //                        {
        //                            // [Gem Simul] : WorkEnd 자체가 시뮬에서 이상하다. 해결되고 테스트하자.
        //                            //m_nSeqNum = 320;
        //                            m_nSeqNum = 310;
        //                            break;
        //                        }

        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.ERROR:
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.TIMEOUT_ERROR:
        //                        return true;

        //                    default:
        //                        break;
        //                }
        //            }
        //            break;
        //        #endregion </Detach End>

        //        #region <Work End>
        //        case 310:
        //            {
        //                _curStep = 0;
        //                _curSubCore = 0;
        //                int key = _coreSlots[_curSubCore];
        //                _taskOperator.MakeDummyMappingFile(_taskOperator.CarrierForSimuls[_curStep].Substrates[key].SubstrateId,
        //                    out string xmlName,
        //                    out string pmsName,
        //                    out string xmlFullPath,
        //                    out string pmsFullPath);
        //                _scenarioOperator.UpdateScenarioParam(ScenarioListTypes.SCENARIO_WORK_END,
        //                    new Dictionary<string, string>()
        //                    {
        //                        ["CARRIERID"] = _taskOperator.CarrierForSimuls[_curStep].CarrierId,
        //                        ["PORTID"] = _taskOperator.CarrierForSimuls[_curStep].PortId,
        //                        ["LOTID"] = _taskOperator.CarrierForSimuls[_curStep].LotId,
        //                        ["PARTID"] = _taskOperator.CarrierForSimuls[_curStep].PartId,
        //                        ["RECIPEID"] = _taskOperator.CarrierForSimuls[_curStep].RecipeId,
        //                        ["OPERID"] = "AUTO",
        //                        ["CHIP_QTY"] = _taskOperator.CarrierForSimuls[_curStep].Substrates[key].Qty.ToString(),
        //                        ["XML_FILENAME"] = xmlName,
        //                        ["XML_FILEBODY"] = xmlFullPath,
        //                        ["PMS_FILENAME"] = pmsName,
        //                        ["PMS_FILEBODY"] = pmsFullPath,
        //                        ["SubstrateId"] = _taskOperator.CarrierForSimuls[_curStep].Substrates[key].SubstrateId,
        //                        ["Angle"] = _taskOperator.CarrierForSimuls[_curStep].Substrates[key].Angle.ToString(),
        //                        ["CountRow"] = _taskOperator.CarrierForSimuls[_curStep].Substrates[key].CountRow.ToString(),
        //                        ["CountCol"] = _taskOperator.CarrierForSimuls[_curStep].Substrates[key].CountCol.ToString(),
        //                        ["MapData"] = _taskOperator.CarrierForSimuls[_curStep].Substrates[key].MapData
        //                    });

        //                ++m_nSeqNum;
        //                SetDelayForSequence(1000);
        //            }
        //            break;
        //        case 311:
        //            {
        //                var result = _scenarioOperator.ExecuteScenario(ScenarioListTypes.SCENARIO_WORK_END);
        //                switch (result)
        //                {
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.PROCEED:
        //                        break;
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.COMPLETED:
        //                        {
        //                            m_nSeqNum = 320;
        //                            break;
        //                        }

        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.ERROR:
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.TIMEOUT_ERROR:
        //                        return true;

        //                    default:
        //                        break;
        //                }
        //            }
        //            break;
        //        #endregion </Work End>

        //        #region <Request Core Track Out>
        //        case 320:
        //            {
        //                _curStep = 0;
        //                _curSubCore = 0;
        //                int key = _coreSlots[_curSubCore];
        //                _scenarioOperator.UpdateScenarioParam(ScenarioListTypes.SCENARIO_REQ_CORE_WAFER_TRACK_OUT,
        //                    new Dictionary<string, string>()
        //                    {
        //                        ["CARRIERID"] = _taskOperator.CarrierForSimuls[_curStep].CarrierId,
        //                        ["PORTID"] = _taskOperator.CarrierForSimuls[_curStep].PortId,
        //                        ["LOTID"] = _taskOperator.CarrierForSimuls[_curStep].LotId,
        //                        ["PARTID"] = _taskOperator.CarrierForSimuls[_curStep].PartId,
        //                        ["STEPSEQ"] = _taskOperator.CarrierForSimuls[_curStep].StepSeq,
        //                        ["RECIPEID"] = _taskOperator.CarrierForSimuls[_curStep].RecipeId,
        //                        ["CHIP_QTY"] = _taskOperator.CarrierForSimuls[_curStep].Substrates[key].Qty.ToString(),
        //                        ["OPERID"] = "AUTO"
        //                    });
        //                ++m_nSeqNum;
        //                SetDelayForSequence(1000);
        //            }
        //            break;
        //        case 321:
        //            {
        //                var result = _scenarioOperator.ExecuteScenario(ScenarioListTypes.SCENARIO_REQ_CORE_WAFER_TRACK_OUT);
        //                switch (result)
        //                {
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.PROCEED:
        //                        break;
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.COMPLETED:
        //                        {
        //                            m_nSeqNum = 400;
        //                        }
        //                        break;

        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.ERROR:
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.TIMEOUT_ERROR:
        //                        return true;

        //                    default:
        //                        break;
        //                }
        //            }
        //            break;
        //        #endregion </Request Core Track Out>
        //        #endregion </Detach End ~ Core Track out>

        //        #region <WorkStart ~ Bin Track Out>

        //        #region <Work Start>
        //        case 400:
        //            {
        //                _curStep = 0;
        //                _curSubCore = 1;
        //                _curSubBin = 0;
        //                int key = _coreSlots[_curSubCore];
        //                _scenarioOperator.UpdateScenarioParam(ScenarioListTypes.SCENARIO_WORK_START,
        //                    new Dictionary<string, string>()
        //                    {
        //                        ["CARRIERID"] = _taskOperator.CarrierForSimuls[_curStep].CarrierId,
        //                        ["PORTID"] = _taskOperator.CarrierForSimuls[_curStep].PortId,
        //                        ["LOTID"] = _taskOperator.CarrierForSimuls[_curStep].LotId,
        //                        ["PARTID"] = _taskOperator.CarrierForSimuls[_curStep].PartId,
        //                        ["RECIPEID"] = _taskOperator.CarrierForSimuls[_curStep].RecipeId,
        //                        ["OPERID"] = "AUTO",
        //                        ["WAFERID"] = _taskOperator.CarrierForSimuls[_curStep].Substrates[key].SubstrateId,
        //                        ["ANGLE"] = _taskOperator.CarrierForSimuls[_curStep].Substrates[key].Angle.ToString()
        //                    });
        //                ++m_nSeqNum;
        //                SetDelayForSequence(1000);
        //            }
        //            break;
        //        case 401:
        //            {
        //                var result = _scenarioOperator.ExecuteScenario(ScenarioListTypes.SCENARIO_WORK_START);
        //                switch (result)
        //                {
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.PROCEED:
        //                        break;
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.COMPLETED:
        //                        {
        //                            var resultData = _scenarioOperator.GetScenarioResultData(ScenarioListTypes.SCENARIO_WORK_START);

        //                            int key = _coreSlots[_curSubCore];
        //                            SubstrateInfoForSimul substrate = _taskOperator.CarrierForSimuls[_curStep].Substrates[key];

        //                            double.TryParse(resultData["Angle"], out double angle);
        //                            int.TryParse(resultData["CountRow"], out int countRow);
        //                            int.TryParse(resultData["CountCol"], out int countCol);
        //                            int.TryParse(resultData["Qty"], out int qty);

        //                            substrate.SubstrateId = resultData["SubstrateId"];
        //                            substrate.Angle = angle;
        //                            substrate.CountRow = countRow;
        //                            substrate.CountCol = countCol;
        //                            substrate.Qty = qty;
        //                            substrate.MapData = resultData["MapData"];

        //                            _taskOperator.CarrierForSimuls[_curStep].Substrates[key] = substrate;
        //                        }
        //                        m_nSeqNum = 410;
        //                        break;
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.ERROR:
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.TIMEOUT_ERROR:
        //                        return true;

        //                    default:
        //                        break;
        //                }
        //            }
        //            break;
        //        #endregion </Work Start>

        //        #region <Lot Split>
        //        case 410:
        //            {
        //                int key = _coreSlots[_curSubCore];
        //                _scenarioOperator.UpdateScenarioParam(ScenarioListTypes.SCENARIO_REQ_CORE_WAFER_SPLIT_LAST,
        //                    new Dictionary<string, string>()
        //                    {
        //                        ["LOTID"] = _taskOperator.CarrierForSimuls[_curStep].LotId,
        //                        ["WAFERID"] = _taskOperator.CarrierForSimuls[_curStep].Substrates[key].SubstrateId,
        //                        ["PARTID"] = _taskOperator.CarrierForSimuls[_curStep].PartId,
        //                        ["RECIPEID"] = _taskOperator.CarrierForSimuls[_curStep].RecipeId,
        //                        ["SLOTID"] = key.ToString(),
        //                        ["OPERID"] = "AUTO"
        //                    });
        //                ++m_nSeqNum;
        //                SetDelayForSequence(1000);
        //            }
        //            break;
        //        case 411:
        //            {
        //                var result = _scenarioOperator.ExecuteScenario(ScenarioListTypes.SCENARIO_REQ_CORE_WAFER_SPLIT_LAST);
        //                switch (result)
        //                {
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.PROCEED:
        //                        break;
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.COMPLETED:
        //                        {
        //                            m_nSeqNum = 420;
        //                        }
        //                        break;

        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.ERROR:
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.TIMEOUT_ERROR:
        //                        return true;

        //                    default:
        //                        break;
        //                }
        //            }
        //            break;
        //        #endregion </Lot Split>

        //        #region <Detach Start>
        //        case 420:
        //            {
        //                _curStep = 0;
        //                int key = _coreSlots[_curSubCore];
        //                _scenarioOperator.UpdateScenarioParam(ScenarioListTypes.SCENARIO_CORE_WAFER_DETACH_START,
        //                    new Dictionary<string, string>()
        //                    {
        //                        ["CARRIERID"] = _taskOperator.CarrierForSimuls[_curStep].CarrierId,
        //                        ["PORTID"] = _taskOperator.CarrierForSimuls[_curStep].PortId,
        //                        ["LOTID"] = _taskOperator.CarrierForSimuls[_curStep].LotId,
        //                        ["PARTID"] = _taskOperator.CarrierForSimuls[_curStep].PartId,
        //                        ["RECIPEID"] = _taskOperator.CarrierForSimuls[_curStep].RecipeId,
        //                        ["WAFERID"] = _taskOperator.CarrierForSimuls[_curStep].Substrates[key].SubstrateId,
        //                        ["SLOTID"] = key.ToString(),
        //                        ["OPERID"] = "AUTO",
        //                        ["NEEDLE_HEIGHT"] = "0.3",
        //                        ["EXPENSION_HEIGHT"] = "5",
        //                        ["PICK_SEARCH_LEVEL"] = "3",
        //                        ["PICK_SEARCH_SPEED"] = "3",
        //                        ["PICK_DELAY"] = "5000",
        //                        ["PICK_FORCE"] = "150",
        //                        ["PICK_SLOWUP_LEVEL"] = "3",
        //                        ["PICK_SLOWUP_SPEED"] = "3",
        //                        ["PLACE_SEARCH_LEVEL"] = "3",
        //                        ["PLACE_SEARCH_SPEED"] = "3",
        //                        ["PLACE_DELAY"] = "500",
        //                        ["PLACE_FORCE"] = "150",
        //                        ["PLACE_SLOWUP_LEVEL"] = "3",
        //                        ["PLACE_SLOWUP_SPEED"] = "3"
        //                    });
        //                ++m_nSeqNum;
        //                SetDelayForSequence(1000);
        //            }
        //            break;
        //        case 421:
        //            {
        //                var result = _scenarioOperator.ExecuteScenario(ScenarioListTypes.SCENARIO_CORE_WAFER_DETACH_START);
        //                switch (result)
        //                {
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.PROCEED:
        //                        break;
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.COMPLETED:
        //                        {
        //                            m_nSeqNum = 430;
        //                            break;
        //                        }

        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.ERROR:
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.TIMEOUT_ERROR:
        //                        return true;

        //                    default:
        //                        break;
        //                }
        //            }
        //            break;
        //        #endregion </Detach Start>

        //        #region <Request Chip Split And Merge>
        //        case 430:
        //            {
        //                int keyCore = _coreSlots[_curSubCore];
        //                int keyBin = _binSlots[_curSubBin];

        //                //_scenarioOperator.UpdateScenarioParam(ScenarioListTypes.SCENARIO_REQ_CORE_CHIP_SPLIT_AND_MERGE,
        //                //    new Dictionary<string, string>()
        //                //    {
        //                //        ["LOTID"] = _taskOperator.CarrierForSimuls[0].LotId,
        //                //        ["SPLIT_WAFERID"] = _taskOperator.CarrierForSimuls[0].Substrates[keyCore].SubstrateId,
        //                //        ["RINGFRAME_ID"] = _taskOperator.CarrierForSimuls[1].Substrates[keyBin].SubstrateId,
        //                //        ["BIN_TYPE"] = _taskOperator.CarrierForSimuls[1].Substrates[keyBin].SubstrateType,
        //                //        // [Gem Simul] 지금은 그냥 QTY인데, Bin 별로 따로 저장해야할듯
        //                //        ["CHIP_QTY"] = _taskOperator.CarrierForSimuls[0].Substrates[keyCore].Qty.ToString(),
        //                //    });
        //                ++m_nSeqNum;
        //                SetDelayForSequence(1000);
        //            }
        //            break;
        //        case 431:
        //            {
        //                //var result = _scenarioOperator.ExecuteScenario(ScenarioListTypes.SCENARIO_REQ_CORE_CHIP_SPLIT_AND_MERGE);
        //                //switch (result)
        //                //{
        //                //    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.PROCEED:
        //                //        break;
        //                //    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.COMPLETED:
        //                //        {
        //                //            m_nSeqNum = 440;
        //                //        }
        //                //        break;

        //                //    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.ERROR:
        //                //    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.TIMEOUT_ERROR:
        //                //        return true;

        //                //    default:
        //                //        break;
        //                //}
        //            }
        //            break;
        //        #endregion </Request Chip Split And Merge>

        //        #region <Bin Sorting End>
        //        case 440:
        //            {
        //                int key = _binSlots[_curSubBin];
        //                ScenarioListTypes scenario;
        //                if (_curSubBin == 0)
        //                {
        //                    scenario = ScenarioListTypes.SCENARIO_BIN_SORTING_END_1;
        //                }
        //                else
        //                {
        //                    scenario = ScenarioListTypes.SCENARIO_BIN_SORTING_END_2;
        //                }

        //                _scenarioOperator.UpdateScenarioParam(scenario,
        //                    new Dictionary<string, string>()
        //                    {
        //                        ["LOTID"] = _taskOperator.CarrierForSimuls[_curStep].LotId,
        //                        ["BIN_TYPE"] = _taskOperator.CarrierForSimuls[_curStep].Substrates[key].SubstrateType,
        //                        ["RINGFRAME_ID"] = _taskOperator.CarrierForSimuls[_curStep].Substrates[key].SubstrateId,
        //                    });
        //                ++m_nSeqNum;
        //                SetDelayForSequence(1000);
        //            }
        //            break;
        //        case 441:
        //            {
        //                ScenarioListTypes scenario;
        //                if (_curSubBin == 0)
        //                {
        //                    scenario = ScenarioListTypes.SCENARIO_BIN_SORTING_END_1;
        //                }
        //                else
        //                {
        //                    scenario = ScenarioListTypes.SCENARIO_BIN_SORTING_END_2;
        //                }
        //                var result = _scenarioOperator.ExecuteScenario(scenario);
        //                switch (result)
        //                {
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.PROCEED:
        //                        break;
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.COMPLETED:
        //                        {
        //                            m_nSeqNum = 450;
        //                            break;
        //                        }

        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.ERROR:
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.TIMEOUT_ERROR:
        //                        return true;

        //                    default:
        //                        break;
        //                }
        //            }
        //            break;
        //        #endregion </Bin Sorting End>

        //        #region <Bin Wafer Assign>
        //        case 450:
        //            {
        //                int key = _binSlots[_curSubBin];
        //                _scenarioOperator.UpdateScenarioParam(ScenarioListTypes.SCENARIO_REQ_BIN_WAFER_ID_ASSIGN,
        //                    new Dictionary<string, string>()
        //                    {
        //                        ["LOTID"] = _taskOperator.CarrierForSimuls[_curStep].LotId,
        //                        ["BIN_TYPE"] = _taskOperator.CarrierForSimuls[_curStep].Substrates[key].SubstrateType,
        //                        ["RINGFRAME_ID"] = _taskOperator.CarrierForSimuls[_curStep].Substrates[key].SubstrateId,
        //                    });
        //                ++m_nSeqNum;
        //                SetDelayForSequence(1000);
        //            }
        //            break;
        //        case 451:
        //            {
        //                var result = _scenarioOperator.ExecuteScenario(ScenarioListTypes.SCENARIO_REQ_BIN_WAFER_ID_ASSIGN);
        //                switch (result)
        //                {
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.PROCEED:
        //                        break;
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.COMPLETED:
        //                        {
        //                            var resultData = _scenarioOperator.GetScenarioResultData(ScenarioListTypes.SCENARIO_REQ_BIN_WAFER_ID_ASSIGN);

        //                            int key = _binSlots[_curSubBin];
        //                            SubstrateInfoForSimul substrate = _taskOperator.CarrierForSimuls[1].Substrates[key];
        //                            substrate.SubstrateId = resultData["SubstrateId"];
        //                            _taskOperator.CarrierForSimuls[1].Substrates[key] = substrate;

        //                            // [Gem Simul] : WorkEnd 자체가 시뮬에서 이상하다. 해결되고 테스트하자.
        //                            //m_nSeqNum = 470;
        //                            m_nSeqNum = 460;
        //                            break;
        //                        }

        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.ERROR:
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.TIMEOUT_ERROR:
        //                        return true;

        //                    default:
        //                        break;
        //                }
        //            }
        //            break;
        //        #endregion </Bin Wafer Assign>

        //        #region <Bin Work End>
        //        case 460:
        //            {
        //                _curStep = 1;
        //                int key = _binSlots[_curSubBin];
        //                _taskOperator.MakeDummyMappingFile(_taskOperator.CarrierForSimuls[_curStep].Substrates[key].SubstrateId,
        //                    out string xmlName,
        //                    out string pmsName,
        //                    out string xmlFullPath,
        //                    out string pmsFullPath);
        //                _scenarioOperator.UpdateScenarioParam(ScenarioListTypes.SCENARIO_BIN_WORK_END,
        //                    new Dictionary<string, string>()
        //                    {
        //                        ["CARRIERID"] = _taskOperator.CarrierForSimuls[_curStep].CarrierId,
        //                        ["PORTID"] = _taskOperator.CarrierForSimuls[_curStep].PortId,
        //                        ["LOTID"] = _taskOperator.CarrierForSimuls[_curStep].LotId,
        //                        ["PARTID"] = _taskOperator.CarrierForSimuls[_curStep].PartId,
        //                        ["RECIPEID"] = _taskOperator.CarrierForSimuls[_curStep].RecipeId,
        //                        ["OPERID"] = "AUTO",
        //                        ["CHIP_QTY"] = _taskOperator.CarrierForSimuls[_curStep].Substrates[key].Qty.ToString(),
        //                        ["XML_FILENAME"] = xmlName,
        //                        ["XML_FILEBODY"] = xmlFullPath,
        //                        ["PMS_FILENAME"] = pmsName,
        //                        ["PMS_FILEBODY"] = pmsFullPath,
        //                        ["SubstrateId"] = _taskOperator.CarrierForSimuls[_curStep].Substrates[key].SubstrateId,
        //                        ["Angle"] = _taskOperator.CarrierForSimuls[_curStep].Substrates[key].Angle.ToString(),
        //                        ["CountRow"] = _taskOperator.CarrierForSimuls[_curStep].Substrates[key].CountRow.ToString(),
        //                        ["CountCol"] = _taskOperator.CarrierForSimuls[_curStep].Substrates[key].CountCol.ToString(),
        //                        ["MapData"] = _taskOperator.CarrierForSimuls[_curStep].Substrates[key].MapData
        //                    });
        //                ++m_nSeqNum;
        //                SetDelayForSequence(1000);
        //            }
        //            break;
        //        case 461:
        //            {
        //                var result = _scenarioOperator.ExecuteScenario(ScenarioListTypes.SCENARIO_BIN_WORK_END);
        //                switch (result)
        //                {
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.PROCEED:
        //                        break;
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.COMPLETED:
        //                        {
        //                            m_nSeqNum = 470;
        //                            break;
        //                        }

        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.ERROR:
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.TIMEOUT_ERROR:
        //                        return true;

        //                    default:
        //                        break;
        //                }
        //            }
        //            break;
        //        #endregion </Bin Work End>

        //        #region <Request Bin Track Out>
        //        case 470:
        //            {
        //                _curStep = 1;
        //                int key = _binSlots[_curSubBin];
        //                _scenarioOperator.UpdateScenarioParam(ScenarioListTypes.SCENARIO_REQ_BIN_WAFER_TRACK_OUT,
        //                    new Dictionary<string, string>()
        //                    {
        //                        ["CARRIERID"] = _taskOperator.CarrierForSimuls[_curStep].CarrierId,
        //                        ["PORTID"] = _taskOperator.CarrierForSimuls[_curStep].PortId,
        //                        ["LOTID"] = _taskOperator.CarrierForSimuls[_curStep].LotId,
        //                        ["PARTID"] = _taskOperator.CarrierForSimuls[_curStep].PartId,
        //                        ["STEPSEQ"] = _taskOperator.CarrierForSimuls[_curStep].StepSeq,
        //                        ["RECIPEID"] = _taskOperator.CarrierForSimuls[_curStep].RecipeId,
        //                        ["CHIP_QTY"] = _taskOperator.CarrierForSimuls[_curStep].Substrates[key].Qty.ToString(),
        //                        ["OPERID"] = "AUTO"
        //                    });
        //                ++m_nSeqNum;
        //                SetDelayForSequence(1000);
        //            }
        //            break;
        //        case 471:
        //            {
        //                var result = _scenarioOperator.ExecuteScenario(ScenarioListTypes.SCENARIO_REQ_BIN_WAFER_TRACK_OUT);
        //                switch (result)
        //                {
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.PROCEED:
        //                        break;
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.COMPLETED:
        //                        {
        //                            // [Gem Simul] : 여기서 분기
        //                            if (++_curSubBin < _binSlots.Count)
        //                            {
        //                                m_nSeqNum = 430;
        //                            }
        //                            else
        //                            {
        //                                m_nSeqNum = 500;
        //                            }
        //                        }
        //                        break;

        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.ERROR:
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.TIMEOUT_ERROR:
        //                        return true;

        //                    default:
        //                        break;
        //                }
        //            }
        //            break;
        //        #endregion </Request Bin Track Out>

        //        #endregion </WorkStart ~ Bin Track Out>

        //        #region <Detach End ~ Core Track Out>

        //        #region <Detach End>
        //        case 500:
        //            {
        //                _curStep = 0;
        //                _curSubCore = 1;
        //                int key = _coreSlots[_curSubCore];
        //                _scenarioOperator.UpdateScenarioParam(ScenarioListTypes.SCENARIO_CORE_WAFER_DETACH_END,
        //                    new Dictionary<string, string>()
        //                    {
        //                        ["CARRIERID"] = _taskOperator.CarrierForSimuls[_curStep].CarrierId,
        //                        ["PORTID"] = _taskOperator.CarrierForSimuls[_curStep].PortId,
        //                        ["LOTID"] = _taskOperator.CarrierForSimuls[_curStep].LotId,
        //                        ["PARTID"] = _taskOperator.CarrierForSimuls[_curStep].PartId,
        //                        ["RECIPEID"] = _taskOperator.CarrierForSimuls[_curStep].RecipeId,
        //                        ["WAFERID"] = _taskOperator.CarrierForSimuls[_curStep].Substrates[key].SubstrateId,
        //                        ["SLOTID"] = key.ToString(),
        //                        ["OPERID"] = "AUTO"
        //                    });
        //                ++m_nSeqNum;
        //                SetDelayForSequence(1000);
        //            }
        //            break;
        //        case 501:
        //            {
        //                var result = _scenarioOperator.ExecuteScenario(ScenarioListTypes.SCENARIO_CORE_WAFER_DETACH_END);
        //                switch (result)
        //                {
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.PROCEED:
        //                        break;
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.COMPLETED:
        //                        {
        //                            // [Gem Simul] : WorkEnd 자체가 시뮬에서 이상하다. 해결되고 테스트하자.
        //                            //m_nSeqNum = 520;
        //                            m_nSeqNum = 510;
        //                            break;
        //                        }

        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.ERROR:
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.TIMEOUT_ERROR:
        //                        return true;

        //                    default:
        //                        break;
        //                }
        //            }
        //            break;
        //        #endregion </Detach End>

        //        #region <Work End>
        //        case 510:
        //            {
        //                int key = _coreSlots[_curSubCore];
        //                _taskOperator.MakeDummyMappingFile(_taskOperator.CarrierForSimuls[_curStep].Substrates[key].SubstrateId,
        //                    out string xmlName,
        //                    out string pmsName,
        //                    out string xmlFullPath,
        //                    out string pmsFullPath);
        //                _scenarioOperator.UpdateScenarioParam(ScenarioListTypes.SCENARIO_WORK_END,
        //                    new Dictionary<string, string>()
        //                    {
        //                        ["CARRIERID"] = _taskOperator.CarrierForSimuls[_curStep].CarrierId,
        //                        ["PORTID"] = _taskOperator.CarrierForSimuls[_curStep].PortId,
        //                        ["LOTID"] = _taskOperator.CarrierForSimuls[_curStep].LotId,
        //                        ["PARTID"] = _taskOperator.CarrierForSimuls[_curStep].PartId,
        //                        ["RECIPEID"] = _taskOperator.CarrierForSimuls[_curStep].RecipeId,
        //                        ["OPERID"] = "AUTO",
        //                        ["CHIP_QTY"] = _taskOperator.CarrierForSimuls[_curStep].Substrates[key].Qty.ToString(),
        //                        ["XML_FILENAME"] = xmlName,
        //                        ["XML_FILEBODY"] = xmlFullPath,
        //                        ["PMS_FILENAME"] = pmsName,
        //                        ["PMS_FILEBODY"] = pmsFullPath,
        //                        ["SubstrateId"] = _taskOperator.CarrierForSimuls[_curStep].Substrates[key].SubstrateId,
        //                        ["Angle"] = _taskOperator.CarrierForSimuls[_curStep].Substrates[key].Angle.ToString(),
        //                        ["CountRow"] = _taskOperator.CarrierForSimuls[_curStep].Substrates[key].CountRow.ToString(),
        //                        ["CountCol"] = _taskOperator.CarrierForSimuls[_curStep].Substrates[key].CountCol.ToString(),
        //                        ["MapData"] = _taskOperator.CarrierForSimuls[_curStep].Substrates[key].MapData
        //                    });
        //                ++m_nSeqNum;
        //                SetDelayForSequence(1000);
        //            }
        //            break;
        //        case 511:
        //            {
        //                var result = _scenarioOperator.ExecuteScenario(ScenarioListTypes.SCENARIO_WORK_END);
        //                switch (result)
        //                {
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.PROCEED:
        //                        break;
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.COMPLETED:
        //                        {
        //                            m_nSeqNum = 520;
        //                            break;
        //                        }

        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.ERROR:
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.TIMEOUT_ERROR:
        //                        return true;

        //                    default:
        //                        break;
        //                }
        //            }
        //            break;
        //        #endregion </Work End>

        //        #region <Process End>
        //        case 520:
        //            {
        //                _scenarioOperator.UpdateScenarioParam(ScenarioListTypes.SCENARIO_PROCESS_END,
        //                    new Dictionary<string, string>()
        //                    {
        //                        ["CARRIERID"] = _taskOperator.CarrierForSimuls[_curStep].CarrierId,
        //                        ["PORTID"] = _taskOperator.CarrierForSimuls[_curStep].PortId,
        //                        ["LOTID"] = _taskOperator.CarrierForSimuls[_curStep].LotId,
        //                        ["PARTID"] = _taskOperator.CarrierForSimuls[_curStep].PartId,
        //                        ["RECIPEID"] = _taskOperator.CarrierForSimuls[_curStep].RecipeId,
        //                        ["OPERID"] = "AUTO"
        //                    });
        //                ++m_nSeqNum;
        //                SetDelayForSequence(1000);
        //            }
        //            break;
        //        case 521:
        //            {
        //                var result = _scenarioOperator.ExecuteScenario(ScenarioListTypes.SCENARIO_PROCESS_END);
        //                switch (result)
        //                {
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.PROCEED:
        //                        break;
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.COMPLETED:
        //                        m_nSeqNum = 530;
        //                        break;
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.ERROR:
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.TIMEOUT_ERROR:
        //                        return true;

        //                    default:
        //                        break;
        //                }
        //            }
        //            break;
        //        #endregion </Process End>

        //        #region <Request Core Track Out>
        //        case 530:
        //            {
        //                int key = _coreSlots[_curSubCore];
        //                _scenarioOperator.UpdateScenarioParam(ScenarioListTypes.SCENARIO_REQ_CORE_WAFER_TRACK_OUT,
        //                    new Dictionary<string, string>()
        //                    {
        //                        ["CARRIERID"] = _taskOperator.CarrierForSimuls[_curStep].CarrierId,
        //                        ["PORTID"] = _taskOperator.CarrierForSimuls[_curStep].PortId,
        //                        ["LOTID"] = _taskOperator.CarrierForSimuls[_curStep].LotId,
        //                        ["PARTID"] = _taskOperator.CarrierForSimuls[_curStep].PartId,
        //                        ["STEPSEQ"] = _taskOperator.CarrierForSimuls[_curStep].StepSeq,
        //                        ["RECIPEID"] = _taskOperator.CarrierForSimuls[_curStep].RecipeId,
        //                        ["CHIP_QTY"] = _taskOperator.CarrierForSimuls[_curStep].Substrates[key].Qty.ToString(),
        //                        ["OPERID"] = "AUTO"
        //                    });
        //                ++m_nSeqNum;
        //                SetDelayForSequence(1000);
        //            }
        //            break;
        //        case 531:
        //            {
        //                var result = _scenarioOperator.ExecuteScenario(ScenarioListTypes.SCENARIO_REQ_CORE_WAFER_TRACK_OUT);
        //                switch (result)
        //                {
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.PROCEED:
        //                        break;
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.COMPLETED:
        //                        {
        //                            m_nSeqNum = 540;
        //                        }
        //                        break;

        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.ERROR:
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.TIMEOUT_ERROR:
        //                        return true;

        //                    default:
        //                        break;
        //                }
        //            }
        //            break;
        //        #endregion </Request Core Track Out>

        //        #region <결산>
        //        case 540:
        //            {
        //                // Core는 냅둠

        //                for (int i = 0; i < _binSlots.Count; ++i)
        //                {
        //                    int key = _binSlots[i];
        //                    if (i == 0)
        //                    {
        //                        _taskOperator.CarrierForSimuls[2].Substrates[0] = _taskOperator.CarrierForSimuls[1].Substrates[key];

        //                    }
        //                    else
        //                    {
        //                        _taskOperator.CarrierForSimuls[3].Substrates[0] = _taskOperator.CarrierForSimuls[1].Substrates[key];
        //                    }
        //                    _taskOperator.CarrierForSimuls[1].Substrates.Remove(key);
        //                }

        //                // Bin 1
        //                _taskOperator.CarrierForSimuls[2].PartId = _taskOperator.CarrierForSimuls[1].PartId;
        //                _taskOperator.CarrierForSimuls[2].StepSeq = _taskOperator.CarrierForSimuls[1].StepSeq;
        //                _taskOperator.CarrierForSimuls[2].LotType = _taskOperator.CarrierForSimuls[1].LotType;
        //                _taskOperator.CarrierForSimuls[2].RecipeId = _taskOperator.CarrierForSimuls[1].RecipeId;

        //                // Bin 2
        //                _taskOperator.CarrierForSimuls[3].PartId = _taskOperator.CarrierForSimuls[1].PartId;
        //                _taskOperator.CarrierForSimuls[3].StepSeq = _taskOperator.CarrierForSimuls[1].StepSeq;
        //                _taskOperator.CarrierForSimuls[3].LotType = _taskOperator.CarrierForSimuls[1].LotType;
        //                _taskOperator.CarrierForSimuls[3].RecipeId = _taskOperator.CarrierForSimuls[1].RecipeId;

        //                m_nSeqNum = 600;
        //            }
        //            break;
        //        #endregion </결산>
        //        #endregion </Detach End ~ Core Track Out>

        //        #region <Core Slot Mapping ~ Bin Lot Id Change>

        //        #region <Core Slot Mapping>
        //        case 600:
        //            {
        //                var dataToUpdate = new Dictionary<string, string>()
        //                {
        //                    ["LOTID"] = _taskOperator.CarrierForSimuls[_curStep].LotId,
        //                    ["CARRIERID"] = _taskOperator.CarrierForSimuls[_curStep].CarrierId,
        //                };

        //                for (int i = 0; i < 25; ++i)
        //                {
        //                    string dataKey = string.Format("SLOT{0}_WAFER_ID", i + 1);
        //                    string dataValue = string.Empty;

        //                    bool hasSubstrate = false;
        //                    for (int slot = 0; slot < _coreSlots.Count; ++slot)
        //                    {
        //                        if (i == _coreSlots[slot])
        //                        {
        //                            hasSubstrate = true;
        //                            break;
        //                        }
        //                    }
        //                    if (hasSubstrate)
        //                    {
        //                        dataValue = _taskOperator.CarrierForSimuls[_curStep].Substrates[i].SubstrateId;
        //                    }
                            
        //                    dataToUpdate[dataKey] = dataValue;
        //                }

        //                for (int i = 0; i < 25; ++i)
        //                {
        //                    string dataKey = string.Format("SLOT{0}_WAFER_CHIP_QTY", i + 1);
        //                    string dataValue = "0";

        //                    bool hasSubstrate = false;
        //                    for (int slot = 0; slot < _coreSlots.Count; ++slot)
        //                    {
        //                        if (i == _coreSlots[slot])
        //                        {
        //                            hasSubstrate = true;
        //                            break;
        //                        }
        //                    }
        //                    if (hasSubstrate)
        //                    {
        //                        dataValue = _taskOperator.CarrierForSimuls[_curStep].Substrates[i].Qty.ToString();
        //                    }

        //                    dataToUpdate[dataKey] = dataValue;
        //                }

        //                _scenarioOperator.UpdateScenarioParam(ScenarioListTypes.SCENARIO_SLOT_WAFER_MAPPING_CORE_1, dataToUpdate);
        //                ++m_nSeqNum;
        //                SetDelayForSequence(1000);
        //            }
        //            break;
        //        case 601:
        //            {
        //                var result = _scenarioOperator.ExecuteScenario(ScenarioListTypes.SCENARIO_SLOT_WAFER_MAPPING_CORE_1);
        //                switch (result)
        //                {
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.PROCEED:
        //                        break;
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.COMPLETED:
        //                        {
        //                            m_nSeqNum = 610;
        //                        }
        //                        break;

        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.ERROR:
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.TIMEOUT_ERROR:
        //                        return true;

        //                    default:
        //                        break;
        //                }
        //            }
        //            break;
        //        #endregion </Core Slot Mapping>

        //        #region <Core Lot Merge>
        //        case 610:
        //            {
        //                var dataToUpdate = new Dictionary<string, string>()
        //                {
        //                    ["LOTID"] = _taskOperator.CarrierForSimuls[_curStep].LotId,
        //                    ["CARRIERID"] = _taskOperator.CarrierForSimuls[_curStep].CarrierId,
        //                    ["PARTID"] = _taskOperator.CarrierForSimuls[_curStep].PartId,
        //                    ["RECIPEID"] = _taskOperator.CarrierForSimuls[_curStep].RecipeId,
        //                    ["OPERID"] = "AUTO"
        //                };

        //                for (int i = 0; i < 25; ++i)
        //                {
        //                    string dataKey = string.Format("SLOT{0}_WAFER_LOT_ID", i + 1);
        //                    string dataValue = string.Empty;

        //                    bool hasSubstrate = false;
        //                    for (int slot = 0; slot < _coreSlots.Count; ++slot)
        //                    {
        //                        if (i == _coreSlots[slot])
        //                        {
        //                            hasSubstrate = true;
        //                            break;
        //                        }
        //                    }
        //                    if (hasSubstrate)
        //                    {
        //                        dataValue = _taskOperator.CarrierForSimuls[_curStep].Substrates[i].LotId;
        //                    }

        //                    dataToUpdate[dataKey] = dataValue;
        //                }

        //                _scenarioOperator.UpdateScenarioParam(ScenarioListTypes.SCENARIO_REQ_LOT_MERGE_CORE_1, dataToUpdate);
        //                ++m_nSeqNum;
        //                SetDelayForSequence(1000);
        //            }
        //            break;
        //        case 611:
        //            {
        //                var result = _scenarioOperator.ExecuteScenario(ScenarioListTypes.SCENARIO_REQ_LOT_MERGE_CORE_1);
        //                switch (result)
        //                {
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.PROCEED:
        //                        break;
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.COMPLETED:
        //                        {
        //                            var resultData = _scenarioOperator.GetScenarioResultData(ScenarioListTypes.SCENARIO_REQ_LOT_MERGE_CORE_1);
        //                            _taskOperator.CarrierForSimuls[_curStep].LotId = resultData["LotId"];
        //                            m_nSeqNum = 620;
        //                        }
        //                        break;

        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.ERROR:
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.TIMEOUT_ERROR:
        //                        return true;

        //                    default:
        //                        break;
        //                }
        //            }
        //            break;
        //        #endregion </Core Lot Merge>

        //        #region <Empty Slot Mapping>
        //        case 620:
        //            {
        //                ++_curStep;
        //                var dataToUpdate = new Dictionary<string, string>()
        //                {
        //                    ["LOTID"] = _taskOperator.CarrierForSimuls[_curStep].LotId,
        //                    ["CARRIERID"] = _taskOperator.CarrierForSimuls[_curStep].CarrierId,
        //                };

        //                for (int i = 0; i < 25; ++i)
        //                {
        //                    string dataKey = string.Format("SLOT{0}_WAFER_ID", i + 1);
        //                    string dataValue = string.Empty;

        //                    dataToUpdate[dataKey] = dataValue;
        //                }

        //                for (int i = 0; i < 25; ++i)
        //                {
        //                    string dataKey = string.Format("SLOT{0}_WAFER_CHIP_QTY", i + 1);
        //                    string dataValue = "0";

        //                    dataToUpdate[dataKey] = dataValue;
        //                }

        //                _scenarioOperator.UpdateScenarioParam(ScenarioListTypes.SCENARIO_SLOT_WAFER_MAPPING_EMPTY_TAPE, dataToUpdate);
        //                ++m_nSeqNum;
        //                SetDelayForSequence(1000);
        //            }
        //            break;
        //        case 621:
        //            {
        //                var result = _scenarioOperator.ExecuteScenario(ScenarioListTypes.SCENARIO_SLOT_WAFER_MAPPING_EMPTY_TAPE);
        //                switch (result)
        //                {
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.PROCEED:
        //                        break;
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.COMPLETED:
        //                        {
        //                            m_nSeqNum = 630;
        //                        }
        //                        break;

        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.ERROR:
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.TIMEOUT_ERROR:
        //                        return true;

        //                    default:
        //                        break;
        //                }
        //            }
        //            break;
        //        #endregion </Empty Slot Mapping>

        //        #region <Bins Slot Mapping>
        //        case 630:
        //            {
        //                ++_curStep;
        //                var dataToUpdate = new Dictionary<string, string>()
        //                {
        //                    ["LOTID"] = _taskOperator.CarrierForSimuls[_curStep].LotId,
        //                    ["CARRIERID"] = _taskOperator.CarrierForSimuls[_curStep].CarrierId,
        //                };

        //                for (int i = 0; i < 25; ++i)
        //                {
        //                    string dataKey = string.Format("SLOT{0}_WAFER_ID", i + 1);
        //                    string dataValue = string.Empty;

        //                    bool hasSubstrate = _taskOperator.CarrierForSimuls[_curStep].Substrates.ContainsKey(i);
        //                    if (hasSubstrate)
        //                    {
        //                        dataValue = _taskOperator.CarrierForSimuls[_curStep].Substrates[i].SubstrateId;
        //                    }

        //                    dataToUpdate[dataKey] = dataValue;
        //                }

        //                for (int i = 0; i < 25; ++i)
        //                {
        //                    string dataKey = string.Format("SLOT{0}_WAFER_CHIP_QTY", i + 1);
        //                    string dataValue = "0";

        //                    bool hasSubstrate = _taskOperator.CarrierForSimuls[_curStep].Substrates.ContainsKey(i);
        //                    if (hasSubstrate)
        //                    {
        //                        dataValue = _taskOperator.CarrierForSimuls[_curStep].Substrates[i].Qty.ToString();
        //                    }

        //                    dataToUpdate[dataKey] = dataValue;
        //                }

        //                var scenario = ScenarioListTypes.SCENARIO_SLOT_WAFER_MAPPING_BIN_1 + (_curStep - 2);
        //                _scenarioOperator.UpdateScenarioParam(scenario, dataToUpdate);
        //                ++m_nSeqNum;
        //                SetDelayForSequence(1000);
        //            }
        //            break;
        //        case 631:
        //            {
        //                var scenario = ScenarioListTypes.SCENARIO_SLOT_WAFER_MAPPING_BIN_1 + (_curStep - 2);
        //                var result = _scenarioOperator.ExecuteScenario(scenario);
        //                switch (result)
        //                {
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.PROCEED:
        //                        break;
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.COMPLETED:
        //                        {
        //                            m_nSeqNum = 640;
        //                        }
        //                        break;

        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.ERROR:
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.TIMEOUT_ERROR:
        //                        return true;

        //                    default:
        //                        break;
        //                }
        //            }
        //            break;
        //        #endregion </Bins Slot Mapping>

        //        #region <Bins Lot Merge>
        //        case 640:
        //            {
        //                var dataToUpdate = new Dictionary<string, string>()
        //                {
        //                    ["LOTID"] = _taskOperator.CarrierForSimuls[_curStep].LotId,
        //                    ["CARRIERID"] = _taskOperator.CarrierForSimuls[_curStep].CarrierId,
        //                    ["PARTID"] = _taskOperator.CarrierForSimuls[_curStep].PartId,
        //                    ["RECIPEID"] = _taskOperator.CarrierForSimuls[_curStep].RecipeId,
        //                    ["OPERID"] = "AUTO"
        //                };

        //                for (int i = 0; i < 25; ++i)
        //                {
        //                    string dataKey = string.Format("SLOT{0}_WAFER_LOT_ID", i + 1);
        //                    string dataValue = string.Empty;

        //                    bool hasSubstrate = _taskOperator.CarrierForSimuls[_curStep].Substrates.ContainsKey(i);
        //                    if (hasSubstrate)
        //                    {
        //                        dataValue = _taskOperator.CarrierForSimuls[_curStep].Substrates[i].LotId;
        //                    }

        //                    dataToUpdate[dataKey] = dataValue;
        //                }

        //                var scenario = ScenarioListTypes.SCENARIO_REQ_LOT_MERGE_BIN_1 + (_curStep - 2);
        //                _scenarioOperator.UpdateScenarioParam(scenario, dataToUpdate);
        //                ++m_nSeqNum;
        //                SetDelayForSequence(1000);
        //            }
        //            break;
        //        case 641:
        //            {
        //                var scenario = ScenarioListTypes.SCENARIO_REQ_LOT_MERGE_BIN_1 + (_curStep - 2);
        //                var result = _scenarioOperator.ExecuteScenario(scenario);
        //                switch (result)
        //                {
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.PROCEED:
        //                        break;
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.COMPLETED:
        //                        {
        //                            var resultData = _scenarioOperator.GetScenarioResultData(scenario);
        //                            _taskOperator.CarrierForSimuls[_curStep].LotId = resultData["LotId"];
        //                            m_nSeqNum = 650;
        //                        }
        //                        break;

        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.ERROR:
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.TIMEOUT_ERROR:
        //                        return true;

        //                    default:
        //                        break;
        //                }
        //            }
        //            break;
        //        #endregion </Bins Lot Merge>

        //        #region <Bins Lot Id Change>
        //        case 650:
        //            {
        //                var dataToUpdate = new Dictionary<string, string>()
        //                {
        //                    ["LOTID"] = _taskOperator.CarrierForSimuls[_curStep].LotId,
        //                    ["CARRIERID"] = _taskOperator.CarrierForSimuls[_curStep].CarrierId,
        //                };

        //                var scenario = ScenarioListTypes.SCENARIO_REQ_LOT_ID_CHANGE_BIN_1 + (_curStep - 2);
        //                _scenarioOperator.UpdateScenarioParam(scenario, dataToUpdate);
        //                ++m_nSeqNum;
        //                SetDelayForSequence(1000);
        //            }
        //            break;
        //        case 651:
        //            {
        //                var scenario = ScenarioListTypes.SCENARIO_REQ_LOT_ID_CHANGE_BIN_1 + (_curStep - 2);
        //                var result = _scenarioOperator.ExecuteScenario(scenario);
        //                switch (result)
        //                {
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.PROCEED:
        //                        break;
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.COMPLETED:
        //                        {
        //                            var resultData = _scenarioOperator.GetScenarioResultData(scenario);
        //                            _taskOperator.CarrierForSimuls[_curStep].LotId = resultData["LotId"];

        //                            if (_curStep < 3)
        //                            {
        //                                m_nSeqNum  = 630;
        //                            }
        //                            else
        //                            {
        //                                _curStep = 0;
        //                                m_nSeqNum = 700;
        //                            }
        //                            //m_nSeqNum = 650;
        //                        }
        //                        break;

        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.ERROR:
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.TIMEOUT_ERROR:
        //                        return true;

        //                    default:
        //                        break;
        //                }
        //            }
        //            break;
        //        #endregion </Bins Lot Id Change>

        //        #endregion </Core Slot Mapping ~ Bin Lot Id Change>

        //        #region <Carrier Unload ~ Port Status Unload>
        //        #region <Carrier Load>
        //        case 700:
        //            {
        //                _scenarioOperator.UpdateScenarioParam(ScenarioListTypes.SCENARIO_CARRIER_UNLOAD,
        //                    new Dictionary<string, string>()
        //                    {
        //                        ["LOTID"] = _taskOperator.CarrierForSimuls[_curStep].LotId,
        //                        ["PARTID"] = _taskOperator.CarrierForSimuls[_curStep].PartId,
        //                        ["STEPSEQ"] = _taskOperator.CarrierForSimuls[_curStep].StepSeq,
        //                        ["LOTTYPE"] = _taskOperator.CarrierForSimuls[_curStep].LotType,
        //                    });
        //                ++m_nSeqNum;
        //                SetDelayForSequence(1000);
        //            }
        //            break;
        //        case 701:
        //            {
        //                var result = _scenarioOperator.ExecuteScenario(ScenarioListTypes.SCENARIO_CARRIER_UNLOAD);
        //                switch (result)
        //                {
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.PROCEED:
        //                        break;
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.COMPLETED:
        //                        m_nSeqNum = 710;
        //                        break;
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.ERROR:
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.TIMEOUT_ERROR:
        //                        return true;

        //                    default:
        //                        break;
        //                }
        //            }
        //            break;
        //        #endregion </Carrier Load>

        //        #region <Port Status Load>
        //        case 710:
        //            {
        //                _scenarioOperator.UpdateScenarioParam(ScenarioListTypes.SCENARIO_PORT_STATUS_UNLOAD,
        //                    new Dictionary<string, string>()
        //                    {
        //                        ["PORTID"] = _taskOperator.CarrierForSimuls[_curStep].PortId,
        //                        ["LOTID"] = _taskOperator.CarrierForSimuls[_curStep].LotId,
        //                        ["CARRIERID"] = _taskOperator.CarrierForSimuls[_curStep].CarrierId,
        //                        ["CARRIER_TYPE"] = "CASSETTE",
        //                        ["STATUS"] = "LOAD",
        //                        ["OPERID"] = "AUTO"
        //                    });
        //                ++m_nSeqNum;
        //                SetDelayForSequence(1000);
        //            }
        //            break;
        //        case 711:
        //            {
        //                var result = _scenarioOperator.ExecuteScenario(ScenarioListTypes.SCENARIO_PORT_STATUS_UNLOAD);
        //                switch (result)
        //                {
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.PROCEED:
        //                        break;
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.COMPLETED:
        //                        {
        //                            if (_curStep < 3)
        //                            {
        //                                ++_curStep;
        //                                m_nSeqNum = 700;
        //                            }
        //                            else
        //                                return true;
        //                        }
        //                        break;
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.ERROR:
        //                    case SECSGEM.DefineSecsGem.EN_SCENARIO_RESULT.TIMEOUT_ERROR:
        //                        return true;

        //                    default:
        //                        break;
        //                }
        //            }
        //            break;
        //        #endregion </Port Status Load>

        //        #endregion </Carrier Unload ~ Port Status Unload>
        //        case 10000:
        //            return true;

        //        default:
        //            return true;
        //    }
        //    return false;
        //}
        #endregion </Gem Simul Only>

        protected bool GetWorkingInformation(bool manualAction, ref RobotWorkingInfo workingInfo, ref string description)
        {
            if (false == manualAction)
            {
                if (false == _robotSchedulerManager.GetWorkingInformation(RobotIndex, ref workingInfo))
                {
                    description = "Can't get working information";
                    return false;
                }
            }
            else
            {
                if (false == _robotSchedulerManager.GetManualWorkingInformation(RobotIndex, ref workingInfo))
                {
                    description = "Can't get working information";
                    return false;
                }
            }

            description = string.Empty;
            return true;
        }
        protected bool IsPortNotChanged(RobotScheduleType portType)
        {
            var currentPort = GetPortStatus(EN_PORT.ROBOT_STATE.ToString());
            var comparePort = _robotSchedulerManager.ConvertPortStatusFromRobotPortType(portType);

            return currentPort.Equals(comparePort);
        }
        protected void SetRobotPortState(RobotScheduleType portType)
        {
            DynamicLink_.EN_PORT_STATUS newPortStatus = _robotSchedulerManager.ConvertPortStatusFromRobotPortType(portType);
            if (false == IsPortNotChanged(portType))
            {
                SetPortStatus(EN_PORT.ROBOT_STATE.ToString(), newPortStatus);
            }
        }

        //private void InitSubStep(bool pick)
        //{
        //    if (pick)
        //    {
        //        _subActionStepPick = SubStepPick.Init;
        //        _subCommandResult = new CommandResults("SubActionPickFromProcessModule", CommandResult.Proceed, false, string.Empty);
        //    }
        //    else
        //    {
        //        _subActionStepPlace = SubStepPlace.Init;
        //        _subCommandResult = new CommandResults("SubActionPlaceFromProcessModule", CommandResult.Proceed, false, string.Empty);
        //    }

        //    _robotSchedulerManager.InitSchedulers(MyRobotIndex);
        //}
        //private CommandResults SubActionPick(bool manualAction, RobotArmTypes armType, string targetLocation, int slot, string substrateName)
        //{
        //    var result = _robotManager.Pick(MyRobotIndex, armType, targetLocation, slot, false, substrateName);
        //    switch (result.CommandResult)
        //    {
        //        case CommandResult.Proceed:
        //            break;
        //        case CommandResult.Completed:
        //        case CommandResult.Skipped:
        //        case CommandResult.Timeout:
        //        case CommandResult.Error:
        //        case CommandResult.Invalid:
        //            if (manualAction)
        //            {
        //                _robotSchedulerManager.RemoveCurrentManualWorkingInfo(MyRobotIndex);
        //            }
        //            break;
        //        default:
        //            break;
        //    }

        //    return result;
        //}
        //private CommandResults SubActionPickFromProcessModule(bool manualAction, RobotArmTypes armType, string targetLocation, int slot, string substrateName)
        //{
        //    if (false == manualAction)
        //    {
        //        if (false == _robotSchedulerManager.GetWorkingInformation(MyRobotIndex, ref _workingInfo))
        //        {
        //            _subCommandResult.CommandResult = CommandResult.Error;
        //            _subCommandResult.Description = "Can't get working information";
        //            return _subCommandResult;
        //        }
        //    }
        //    else
        //    {
        //        if (false == _robotSchedulerManager.GetManualWorkingInformation(MyRobotIndex, ref _workingInfo))
        //        {
        //            _subCommandResult.CommandResult = CommandResult.Error;
        //            _subCommandResult.Description = "Can't get working information";
        //            return _subCommandResult;
        //        }
        //    }

        //    switch (_subActionStepPick)
        //    {
        //        case SubStepPick.Init:
        //            _subActionStepPick = SubStepPick.BeforeApproachUnloading;
        //            break;

        //        case SubStepPick.BeforeApproachUnloading:
        //            {
        //                //CommandResult commandResult = _robotSchedulerManager.IsApproachUnloadingPrepared(MyRobotIndex, targetLocation, substrateName);
        //                //switch (commandResult)
        //                //{
        //                //    case CommandResult.Proceed:
        //                //        break;
        //                //    case CommandResult.Completed:
        //                //        // 여기까진 스메마 켜기 전이라 중단해도 된다.
        //                //        if (_taskOperator.IsFinishingMode())
        //                //        {
        //                //            _subCommandResult.ActionName = _subActionStepPick.ToString();
        //                //            _subCommandResult.CommandResult = CommandResult.Skipped;
        //                //            return _subCommandResult;
        //                //        }
        //                //        _subActionStepPick = SubStepPick.ActionApproachUnloading;
        //                //        break;
        //                //    default:
        //                //        _subCommandResult.CommandResult = commandResult;
        //                //        break;
        //                //}
        //            }
        //            break;

        //        case SubStepPick.ActionApproachUnloading:
        //            {
        //                //var result = _robotManager.ApproachForPick(MyRobotIndex, armType, targetLocation, slot);
        //                //switch (result.CommandResult)
        //                //{
        //                //    case CommandResult.Proceed:
        //                //        break;
        //                //    case CommandResult.Completed:
        //                //        // 여기까진 스메마 켜기 전이라 중단해도 된다.
        //                //        if (_taskOperator.IsFinishingMode())
        //                //        {
        //                //            _subCommandResult.ActionName = _subActionStepPick.ToString();
        //                //            _subCommandResult.CommandResult = CommandResult.Skipped;
        //                //            return _subCommandResult;
        //                //        }
        //                //        _subActionStepPick = SubStepPick.AfterApproachUnloading;
        //                //        break;
        //                //    default:
        //                //        _subCommandResult = result;
        //                //        break;
        //                //}
        //            }
        //            break;

        //        case SubStepPick.AfterApproachUnloading:
        //            {
        //                //CommandResult commandResult = _robotSchedulerManager.IsApproachUnloadingCompleted(MyRobotIndex, targetLocation, substrateName);
        //                //switch (commandResult)
        //                //{
        //                //    case CommandResult.Proceed:
        //                //        break;
        //                //    case CommandResult.Completed:
        //                //        _subActionStepPick = SubStepPick.BeforeActionUnloading;
        //                //        break;
        //                //    default:
        //                //        _subCommandResult.CommandResult = commandResult;
        //                //        break;
        //                //}
        //            }
        //            break;

        //        case SubStepPick.BeforeActionUnloading:
        //            {
        //                //CommandResult commandResult = _robotSchedulerManager.IsUnloadingPrepared(MyRobotIndex, targetLocation, substrateName);
        //                //switch (commandResult)
        //                //{
        //                //    case CommandResult.Proceed:
        //                //        break;
        //                //    case CommandResult.Completed:
        //                //        _subActionStepPick = SubStepPick.ActionUnloading;
        //                //        break;
        //                //    default:
        //                //        _subCommandResult.CommandResult = commandResult;
        //                //        break;
        //                //}
        //            }
        //            break;

        //        case SubStepPick.ActionUnloading:
        //            {
        //                //var result = _robotManager.Pick(MyRobotIndex, armType, targetLocation, slot, false, substrateName);
        //                //switch (result.CommandResult)
        //                //{
        //                //    case CommandResult.Proceed:
        //                //        break;
        //                //    case CommandResult.Completed:
        //                //        _subActionStepPick = SubStepPick.AfterActionUnloading;
        //                //        break;
        //                //    default:
        //                //        _subCommandResult = result;
        //                //        break;
        //                //}
        //            }
        //            break;

        //        case SubStepPick.AfterActionUnloading:
        //            {
        //                CommandResult commandResult = _robotSchedulerManager.IsUnloadingCompleted(MyRobotIndex, targetLocation, substrateName);
        //                switch (commandResult)
        //                {
        //                    case CommandResult.Proceed:
        //                        break;
        //                    case CommandResult.Completed:
        //                        _subCommandResult.CommandResult = CommandResult.Completed;
        //                        break;
        //                    default:
        //                        _subCommandResult.CommandResult = commandResult;
        //                        break;
        //                }
        //            }
        //            break;

        //        default:
        //            break;
        //    }

        //    _subCommandResult.ActionName = _subActionStepPick.ToString();
        //    if (false == _subCommandResult.CommandResult.Equals(CommandResult.Proceed) &&
        //        false == _subCommandResult.CommandResult.Equals(CommandResult.Completed))
        //    {
        //        // 진행 중이 아니고, 완료가 아니면 스메마를 끈다.
        //        int pmIndex = _processGroup.GetProcessModuleIndexByLocation(targetLocation);
        //        _processGroup.SetUnloadingSignal(pmIndex, targetLocation, false);

        //        if (manualAction)
        //        {
        //            _robotSchedulerManager.RemoveCurrentManualWorkingInfo(MyRobotIndex);
        //        }
        //    }

        //    return _subCommandResult;
        //}
        //private CommandResults SubActionPlace(bool manualAction, RobotArmTypes armType, string targetLocation, int slot, string substrateName)
        //{
        //    var result = _robotManager.Place(MyRobotIndex, armType, targetLocation, slot, false, substrateName);
        //    switch (result.CommandResult)
        //    {
        //        case CommandResult.Proceed:
        //            break;
        //        case CommandResult.Completed:
        //        case CommandResult.Skipped:
        //        case CommandResult.Timeout:
        //        case CommandResult.Error:
        //        case CommandResult.Invalid:
        //            if (manualAction)
        //            {
        //                _robotSchedulerManager.RemoveCurrentManualWorkingInfo(MyRobotIndex);
        //            }
        //            break;
        //        default:
        //            break;
        //    }

        //    return result;
        //}
        //private CommandResults SubActionPlaceToProcessModule(bool manualAction, RobotArmTypes armType, string targetLocation, int slot, string substrateName)
        //{
        //    if (false == manualAction)
        //    {
        //        if (false == _robotSchedulerManager.GetWorkingInformation(MyRobotIndex, ref _workingInfo))
        //        {
        //            _subCommandResult.CommandResult = CommandResult.Error;
        //            _subCommandResult.Description = "Can't get working information";
        //            return _subCommandResult;
        //        }
        //    }
        //    else
        //    {
        //        if (false == _robotSchedulerManager.GetManualWorkingInformation(MyRobotIndex, ref _workingInfo))
        //        {
        //            _subCommandResult.CommandResult = CommandResult.Error;
        //            _subCommandResult.Description = "Can't get working information";
        //            return _subCommandResult;
        //        }
        //    }

        //    switch (_subActionStepPlace)
        //    {
        //        case SubStepPlace.Init:
        //            _subActionStepPlace = SubStepPlace.BeforeApproachLoading;
        //            break;

        //        case SubStepPlace.BeforeApproachLoading:
        //            //{
        //            //    CommandResult commandResult = _robotSchedulerManager.IsApproachLoadingPrepared(MyRobotIndex, targetLocation, substrateName);
        //            //    switch (commandResult)
        //            //    {
        //            //        case CommandResult.Proceed:
        //            //            break;
        //            //        case CommandResult.Completed:
        //            //            {
        //            //                _subActionStepPlace = SubStepPlace.ActionApproachLoading;
        //            //            }
        //            //            break;
        //            //        default:
        //            //            _subCommandResult.CommandResult = commandResult;
        //            //            break;
        //            //    }
        //            //}
        //            break;

        //        case SubStepPlace.ActionApproachLoading:
        //            {
        //                //var result = _robotManager.ApproachForPlace(MyRobotIndex, armType, targetLocation, slot);
        //                //switch (result.CommandResult)
        //                //{
        //                //    case CommandResult.Proceed:
        //                //        break;
        //                //    case CommandResult.Completed:
        //                //        _subActionStepPlace = SubStepPlace.AfterApproachLoading;
        //                //        break;
        //                //    default:
        //                //        _subCommandResult = result;
        //                //        break;
        //                //}
        //            }
        //            break;

        //        case SubStepPlace.AfterApproachLoading:
        //            {
        //                //CommandResult commandResult = _robotSchedulerManager.IsApproachLoadingCompleted(MyRobotIndex, targetLocation, substrateName);
        //                //switch (commandResult)
        //                //{
        //                //    case CommandResult.Proceed:
        //                //        break;
        //                //    case CommandResult.Completed:
        //                //        _subActionStepPlace = SubStepPlace.BeforeActionLoading;
        //                //        break;
        //                //    default:
        //                //        _subCommandResult.CommandResult = commandResult;
        //                //        break;
        //                //}
        //            }
        //            break;

        //        case SubStepPlace.BeforeActionLoading:
        //            {
        //                //CommandResult commandResult = _robotSchedulerManager.IsLoadingPrepared(MyRobotIndex, targetLocation, substrateName);
        //                //switch (commandResult)
        //                //{
        //                //    case CommandResult.Proceed:
        //                //        break;
        //                //    case CommandResult.Completed:
        //                //        _subActionStepPlace = SubStepPlace.ActionLoading;
        //                //        break;
        //                //    default:
        //                //        _subCommandResult.CommandResult = commandResult;
        //                //        break;
        //                //}
        //            }
        //            break;

        //        case SubStepPlace.ActionLoading:
        //            {
        //                //var result = _robotManager.Place(MyRobotIndex, armType, targetLocation, slot, false, substrateName);
        //                //switch (result.CommandResult)
        //                //{
        //                //    case CommandResult.Proceed:
        //                //        break;
        //                //    case CommandResult.Completed:
        //                //        _subActionStepPlace = SubStepPlace.AfterActionLoading;
        //                //        break;
        //                //    default:
        //                //        _subCommandResult = result;
        //                //        break;
        //                //}
        //            }
        //            break;

        //        case SubStepPlace.AfterActionLoading:
        //            {
        //                //CommandResult commandResult = _robotSchedulerManager.IsLoadingCompleted(MyRobotIndex, targetLocation, substrateName);
        //                //switch (commandResult)
        //                //{
        //                //    case CommandResult.Proceed:
        //                //        break;
        //                //    case CommandResult.Completed:
        //                //        {
        //                //            _subCommandResult.CommandResult = CommandResult.Completed;
        //                //        }
        //                //        break;
        //                //    default:
        //                //        _subCommandResult.CommandResult = commandResult;
        //                //        break;
        //                //}
        //            }
        //            break;

        //        default:
        //            break;
        //    }

        //    _subCommandResult.ActionName = _subActionStepPlace.ToString();
        //    if (false == _subCommandResult.CommandResult.Equals(CommandResult.Proceed) &&
        //        false == _subCommandResult.CommandResult.Equals(CommandResult.Completed))
        //    {
        //        // 진행 중이 아니고, 완료가 아니면 스메마를 끈다.
        //        int pmIndex = _processGroup.GetProcessModuleIndexByLocation(targetLocation);
        //        _processGroup.SetLoadingSignal(pmIndex, targetLocation, false);

        //        if (manualAction)
        //        {
        //            _robotSchedulerManager.RemoveCurrentManualWorkingInfo(MyRobotIndex);
        //        }
        //    }

        //    return _subCommandResult;
        //}
        #endregion

        #region manual
        #endregion /manual

        #endregion /action

        #region <ETC>
        private bool CheckControllerConnectionStatus()
        {
            if (false == _robotManager.IsConnectedWithController(RobotIndex))
            {
                GenerateAlarm((int)EN_ALARM.ATM_ROBOT_CONTROLLER_NOT_CONNECTED);
                return false;
            }

            return true;
        }
        #endregion </ETC>

        #region pre/post condition
        #endregion /pre/post condition

        #region common method

        #endregion /common method

        #region enum

        #region action
        /// <summary>
        /// 2020.06.02 by yjlee [ADD] Enumerate the actions of the task.
        /// </summary>
        public enum TASK_ACTION
        {
            STOP = 0,

            // Auto
            SCHEDULING,
            PICK,
            PLACE,

            // Manual
            MANUAL_PICK,
            MANUAL_PLACE,

            // Gem Simul
            //GEM_SIMUL,
        }
        #endregion /action

        #region step
        private enum STEP_INITIALIZE
        {
            START = 0,
            CHECK_ALARM_STATUS = 100,
            PREPARE = 500,
            END = 10000,
        }
        private enum STEP_ENTRY
        {
            START = 0,
            PREPARE = 50,

            END = 10000,
        }
        private enum STEP_EXIT
        {
            START = 0,
            END = 10000,
        }
        private enum STEP_SCHEDULING
        {
            START = 0,

            CHECK_READY = 100,

            CHECK_PORT = 900,

            END = 10000,
        }
        private enum STEP_PICKING
        {
            START = 0,

            CHECK_READY = 50,

            EXECUTE_SCENARIO_BEFORE_PICK = 100,

            PICK = 200,

            PREPARE_APPROACH_UNLOADING = 300,
            ACTION_APPROACH_UNLOADING = 330,
            APPROACH_UNLOADING_COMPLETED = 350,

            PREPARE_ACTION_UNLOADING = 400,
            ACTION_UNLOADING = 430,
            ACTION_UNLOADING_COMPLETED = 440,

            EXECUTE_SCENARIO_AFTER_PICK = 800,

            UPDATE_LINK = 900,

            END = 10000,
        }
        private enum STEP_PLACING
        {
            START = 0,

            CHECK_READY = 50,

            EXECUTE_SCENARIO_BEFORE_PLACE = 100,

            PLACE = 200,

            PREPARE_APPROACH_LOADING = 300,
            ACTION_APPROACH_LOADING = 330,
            APPROACH_LOADING_COMPLETED = 350,

            PREPARE_ACTION_LOADING = 400,
            ACTION_LOADING = 430,
            ACTION_LOADING_COMPLETED = 440,

            EXECUTE_SCENARIO_AFTER_PLACE = 800,

            UPDATE_LINK = 900,

            END = 10000,
        }
        #endregion /step

        #endregion /enum
    }
}
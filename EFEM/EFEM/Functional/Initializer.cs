using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Define.DefineConstant;
using Define.DefineEnumBase.ThreadTimer;
using Define.DefineEnumBase.Initialize;

using ThreadTimer_;

using Account_;
using Alarm_;

using Motion_;

using Cylinder_;

using DigitalIO_;
using AnalogIO_;

using Socket_;
using Serial_;
using Trigger_;
using Interrupt_;
using TaskDevice_;

//using Vision_;
using RegisteredInstances_;

using DesignPattern_.Observer_;

namespace FrameOfSystem3.Functional
{
    /// <summary>
    /// 2020.05.13 by yjlee [ADD] Initialize the instances of the dll.
    /// </summary>
    public class Initializer
    {
        #region Variables
        // 2022.01.17. [ADD] PROGRESS BAR DEBUG 모드 확인용 (기본값 false)
        private static bool m_bShowProgressWhenAttachedDebuger = false;

        private EN_INITIALIZATION_STEP m_enInitializeStep = EN_INITIALIZATION_STEP.INIT_START;

        #region Delegate
        /// <summary>
        /// 2020.05.12 by yjlee [ADD] Declare the delegates to pass to the Dll.
        /// </summary>
        #region Thread Timer
        private deleCallbackFunction delegateThreadTimerForFileIO = null;
        private deleCallbackFunction delegateThreadTimerForDigitalIO = null;
        private deleCallbackFunction delegateThreadTimerForAnalogIO = null;
        private deleCallbackFunction delegateThreadTimerForMotion = null;
        private deleCallbackFunction delegateThreadTimerForMotionGathering = null;
        private deleCallbackFunction delegateThreadTimerForCommunication = null;
        private deleCallbackFunction delegateThreadTimerForETC = null;
        #endregion

        #region Cylinder
        private DelegateForReadingIO delegateCylinderForReadingInput = null;
        private DelegateForReadingIO delegateCylinderForReadingOutput = null;
        private DelegateForWritingIO delegateCylinderForWritingOutput = null;
        #endregion

        #region Interrupt
        private DelegateForReadingInput delegateInterruptForReadingInput = null;
        private DelegateForWriteDigitalOutput delegateInterruptForWriteOutput = null;
        private DelegateForInterruptAction delegateInterruptForActionStart = null;
        private DelegateForInterruptAction delegateInterruptForActionStop = null;
        private DelegateForInterruptAction delegateInterruptForActionReset = null;
        private DelegateForInterruptAction delegateInterruptForActionAlarm = null;
        #endregion

        #region Trigger
        private Trigger_.DelegateForWritingOutput delegateTriggerForWritingOutput = null;
        #endregion

        #endregion

        #region Instances for Obserber
        private Subject subjectEquipmentState = null;
        private Subject subjectAlarm = null;
        #endregion

        #region for Form progress
        FrameOfSystem3.Views.Functional.Form_Progress m_Progress = null;
        System.Timers.Timer m_timerForProgressForm = null;
        #endregion

        #region Instance for Interfaces
        RegisteredInterfaces m_pRegisteredInterface = null;
        #endregion Instance for Interfaces

        #region DLL instances
        private Motion_.Motion m_instanceMotion = null;
        private Socket m_instanceSocket = null;
        private Serial m_instanceSerial = null;
        private Vision_.Vision m_instanceVision = null;
        private Interrupt m_instanceInterrupt = null;
        private Cylinder m_instanceCylinder = null;
        private Trigger m_instanceTrigger = null;
        private RegisteredInstanceManager m_instanceRegisteredManager = null;
        #endregion

        #region Controller
        //Vision_.VisionController m_visionController     = null;
        AnalogIOController[] m_arAnalogIOController = null;
        DigitalIOController[] m_arDigitalIOController = null;
        MotionController[] m_arMotionController = null;       // 2023.02.13. jhlim [MOD] 멀티 컨트롤러 사용을 위해 배열로 변경
        #endregion
        #endregion Variables

        #region Contructor & Destructor
        public Initializer() { }

        #endregion

        #region Internal Interface
        /// <summary>
        /// 2020.05.12 by yjlee [ADD] Register the event of the observer.
        /// </summary>
        private void RegisterObserverEvent()
        {
            subjectEquipmentState = EquipmentState_.EquipmentState.GetInstance();
            subjectAlarm = Alarm_.Alarm.GetInstance();

            Interrupt.GetInstance().RegisterSubject(subjectEquipmentState);

            Trigger.GetInstance().RegisterSubject(subjectEquipmentState);
            Trigger.GetInstance().RegisterSubject(subjectAlarm);

            Config.ConfigAlarm.GetInstance().RegisterSubject(subjectAlarm);
        }

        /// <summary>
        /// 2020.10.07 by yjlee [ADD] Get the instances of the dlls.
        /// </summary>
        private void GetDllInstance()
        {
            m_instanceMotion = Motion_.Motion.GetInstance();
            m_instanceSocket = Socket_.Socket.GetInstance();
            m_instanceSerial = Serial_.Serial.GetInstance();
            m_instanceVision = Vision_.Vision.GetInstance();
            m_instanceInterrupt = Interrupt_.Interrupt.GetInstance();
            m_instanceCylinder = Cylinder.GetInstance();
            m_instanceTrigger = Trigger.GetInstance();
            m_instanceRegisteredManager = RegisteredInstanceManager.GetInstance();
        }

        /// <summary>
        /// 2020.05.12 by yjlee [ADD] Set the thread timer to run.
        /// </summary>
        private bool SetThreadTimer()
        {
            delegateThreadTimerForFileIO = new deleCallbackFunction(FileIOManager_.FileIOManager.GetInstance().Execute);
            ThreadTimer.GetInstance().AddTimer((int)EN_THREADTIMER_INDEX.THREADTIMER_INDEX_FILEIO
                , ThreadTimerInterval.THREADTIMER_INTERVAL_FILEIO
                , delegateThreadTimerForFileIO);
            ThreadTimer.GetInstance().StartTimer((int)EN_THREADTIMER_INDEX.THREADTIMER_INDEX_FILEIO);

            delegateThreadTimerForDigitalIO = new deleCallbackFunction(DigitalIO.GetInstance().Execute);
            ThreadTimer.GetInstance().AddTimer((int)EN_THREADTIMER_INDEX.THREADTIMER_INDEX_DIGITALIO
                , ThreadTimerInterval.THREADTIMER_INTERVAL_DIGITALIO
                , delegateThreadTimerForDigitalIO);
            ThreadTimer.GetInstance().StartTimer((int)EN_THREADTIMER_INDEX.THREADTIMER_INDEX_DIGITALIO);

            delegateThreadTimerForAnalogIO = new deleCallbackFunction(AnalogIO.GetInstance().Execute);
            ThreadTimer.GetInstance().AddTimer((int)EN_THREADTIMER_INDEX.THERADTIMER_INDEX_ANALOGIO
                , ThreadTimerInterval.THREADTIMER_INTERVAL_ANALOGIO
                , delegateThreadTimerForAnalogIO);
            ThreadTimer.GetInstance().StartTimer((int)EN_THREADTIMER_INDEX.THERADTIMER_INDEX_ANALOGIO);

            delegateThreadTimerForMotion = new deleCallbackFunction(Motion_.Motion.GetInstance().Execute);
            ThreadTimer.GetInstance().AddTimer((int)EN_THREADTIMER_INDEX.THREADTIMER_INDEX_MOTION
                , ThreadTimerInterval.THREADTIMER_INTERVAL_MOTION
                , delegateThreadTimerForMotion);
            ThreadTimer.GetInstance().StartTimer((int)EN_THREADTIMER_INDEX.THREADTIMER_INDEX_MOTION);

            delegateThreadTimerForMotionGathering = new deleCallbackFunction(ExecuteForMotionGathering);
            ThreadTimer.GetInstance().AddTimer((int)EN_THREADTIMER_INDEX.THREADTIMER_INDEX_MOTION_GATHERING
                , ThreadTimerInterval.THREADTIMER_INTERVAL_MOTION_GATHERING
                , delegateThreadTimerForMotionGathering);
            ThreadTimer.GetInstance().StartTimer((int)EN_THREADTIMER_INDEX.THREADTIMER_INDEX_MOTION_GATHERING);

            delegateThreadTimerForCommunication = new deleCallbackFunction(ExecuteForCommunication);
            ThreadTimer.GetInstance().AddTimer((int)EN_THREADTIMER_INDEX.THREADTIMER_INDEX_COMMUNICATION
                , ThreadTimerInterval.THREADTIMER_INTERVAL_COMMUNICATION
                , delegateThreadTimerForCommunication);
            ThreadTimer.GetInstance().StartTimer((int)EN_THREADTIMER_INDEX.THREADTIMER_INDEX_COMMUNICATION);

            // 2024.06.14. jhlim [ADD] etc timer는 아래서 설정한다.
            //delegateThreadTimerForETC = new deleCallbackFunction(ExecuteForETC);
            //ThreadTimer.GetInstance().AddTimer((int)EN_THREADTIMER_INDEX.THREADTIMER_INDEX_ETC_INSTANCES
            //    , ThreadTimerInterval.THREADTIMER_INTERVAL_ETC_INSTANCES
            //    , delegateThreadTimerForETC);
            //ThreadTimer.GetInstance().StartTimer((int)EN_THREADTIMER_INDEX.THREADTIMER_INDEX_ETC_INSTANCES);

            return true;
        }
        
        private void StartETCThreadTimer()
        {
            delegateThreadTimerForETC = new deleCallbackFunction(ExecuteForETC);
            ThreadTimer.GetInstance().AddTimer((int)EN_THREADTIMER_INDEX.THREADTIMER_INDEX_ETC_INSTANCES
                , ThreadTimerInterval.THREADTIMER_INTERVAL_ETC_INSTANCES
                , delegateThreadTimerForETC);
            ThreadTimer.GetInstance().StartTimer((int)EN_THREADTIMER_INDEX.THREADTIMER_INDEX_ETC_INSTANCES);
        }
        #region Execute
        /// <summary>
        /// 2020.05.21 by yjlee [ADD] Execute to gather the data for the motion.
        /// </summary>
        private void ExecuteForMotionGathering()
        {
            var enControllerState = Motion_.CONTROLLER_STATE.STOP;
            m_instanceMotion.ExecuteForGathering(ref enControllerState);
        }

        /// <summary>
        /// 2020.05.21 by yjlee [ADD] Execute to communicate the external devices.
        /// </summary>
        private void ExecuteForCommunication()
        {
            m_instanceSocket.Execute();
            m_instanceSerial.Execute();
            m_instanceVision.Execute();
        }

        /// <summary>
        /// 2020.05.21 by yjlee [ADD] Execute for the ETC Instances.
        /// </summary>
        private void ExecuteForETC()
        {
            SECSGEM.ScenarioOperator.Instance.Execute();

            m_instanceInterrupt.Execute();
            m_instanceCylinder.Execute();
            m_instanceTrigger.Execute();
            Scheduler.GetInstance().Excute();
			EquipmentProperty.EquipmentProperty.GetInstance().Execute();
			EquipmentMonitor.RAM_Metrics.GetInstance().Execute();
			
            // 2023.12.28. jhlim [ADD]
            EFEM.Modules.LoadPortManager.Instance.Execute();
            EFEM.Modules.AtmRobotManager.Instance.Execute();
            EFEM.Modules.RFIDManager.Instance.ExecuteAll();
            EFEM.Modules.ProcessModuleGroup.Instance.ExecuteAll();
            
            ExternalDevice.Socket.ModbusTCPClient.GetInstance((int)Define.DefineEnumProject.Socket.EN_SOCKET_INDEX.MODBUS).Execute();
            ExternalDevice.Serial.FanFilterUnit.FanFilterUnitManager.Instance.Execute();
        }
        #endregion

        #region Progress Form
        private bool ShowProgressForm()
        {
            if (!m_bShowProgressWhenAttachedDebuger == System.Diagnostics.Debugger.IsAttached)
            {
                return true;
            }
            else if (null != m_Progress && false != m_Progress.IsFormLoad())
            {
                m_Progress.SetEndStep(Enum.GetValues(typeof(EN_INITIALIZATION_STEP)).Length);
                return true;
            }
            return false;
        }
        /// <summary>
        /// 2020.05.13 by yjlee [ADD] Initialize the progress form.
        /// </summary>
        private void InitProgressForm()
        {
            if (!m_bShowProgressWhenAttachedDebuger == System.Diagnostics.Debugger.IsAttached)
            {
                return;
            }

            #region Init Timer
            m_timerForProgressForm = new System.Timers.Timer();
            m_timerForProgressForm.BeginInit();
            m_timerForProgressForm.Elapsed += new System.Timers.ElapsedEventHandler(CallbackFunctionForTimer);
            m_timerForProgressForm.AutoReset = false;
            m_timerForProgressForm.Interval = InitializationProgressForm.INTERVAL_CHECKING_INIT_STATE;
            m_timerForProgressForm.EndInit();
            #endregion

            m_timerForProgressForm.Start();
        }

        /// <summary>
        /// 2020.05.13 by yjlee [ADD] Release the resources.
        /// </summary>
        private void ExitProgressForm()
        {
            if (null == m_timerForProgressForm)
            {
                return;
            }

            string temp = Enum.GetValues(typeof(EN_INITIALIZATION_STEP)).Length.ToString();
            m_Progress.EnqueueResult(true, ref temp);

            m_timerForProgressForm.Dispose();
            m_timerForProgressForm = null;
        }

        /// <summary>
        /// 2020.05.13 by yjlee [ADD] It will be called by the timer routine.
        /// </summary>
        private void CallbackFunctionForTimer(object sender, System.Timers.ElapsedEventArgs args)
        {
            m_Progress = new Views.Functional.Form_Progress(InitializationProgressForm.INTERVAL_CHECKING_QUEUE_OF_PROGRESS);
            m_Progress.ShowDialog();

            m_Progress.Dispose();
            m_Progress = null;
        }
        #endregion

        private void UpdateEquipmentProperty()
        {
            if (EquipmentProperty.RawMaterialPortManager.GetInstance().GetRawMaterialExist())
                EquipmentProperty.EquipmentProperty.GetInstance().SetValue(EquipmentProperty.EN_EQUIPMENT_PROPERTY_LIST.MATERIAL_EXIST, EquipmentProperty.EN_MATERIAL_EXIST_VALUES.EXIST);
            else
                EquipmentProperty.EquipmentProperty.GetInstance().SetValue(EquipmentProperty.EN_EQUIPMENT_PROPERTY_LIST.MATERIAL_EXIST, EquipmentProperty.EN_MATERIAL_EXIST_VALUES.EMPTY);
        }
        #endregion

        #region External Interface
        /// <summary>
        /// 2020.02.05 by yjlee [ADD] Initialize the software.
        /// </summary>
        public void Init(DelegateForInterruptAction delegateStart
            , DelegateForInterruptAction delegateStop
            , DelegateForInterruptAction delegateReset
            , DelegateForInterruptAction delegateAlarm)
        {
            m_enInitializeStep = EN_INITIALIZATION_STEP.INIT_START;

            // 2020.05.18 by yjlee [ADD] Set an interrupt actions.
            delegateInterruptForActionStart = delegateStart;
            delegateInterruptForActionStop = delegateStop;
            delegateInterruptForActionReset = delegateReset;
            delegateInterruptForActionAlarm = delegateAlarm;

            InitProgressForm();
        }
        /// <summary>
        /// 2020.02.05 by yjlee [ADD] Exit the software.
        /// 2021.08.18. by shkim [MOD] Task Thread가 정지되기 전에 Recipe 인스턴스를 소멸시키면 Exception 발생하여 위치 변경
        /// </summary>
        public void Exit()
        {
            // Save 이후 인스턴스 소멸동작이 아닌
            // 클래스 내부에서 사용하는 인스턴스들을 유지하고, Process Recipe Save 동작만 하도록 변경
            #region <Modules>
            EFEM.Modules.ProcessModuleGroup.Instance.ExitProcessModuleAll();
            EFEM.Defines.Common.AsyncLoggerForEFEM.Instance.Exit();
            #endregion </Modules>

            #region <FFU>
            ExternalDevice.Serial.FanFilterUnit.FanFilterUnitManager.Instance.Deactivate();
            #endregion </FFU>

            #region Recipe
            Recipe.Recipe.GetInstance().SaveProcessRecipe();
            #endregion

            #region Logging
            Log.LogManager.GetInstance().Exit();
            Log.LogWriter.GetInstance().Deactivate();
            #endregion

            #region Config
            Account_.Account.GetInstance().Exit();
            Alarm.GetInstance().Exit();
            Socket.GetInstance().Exit();
            Serial.GetInstance().Exit();
            Cylinder.GetInstance().Exit();
            Interrupt.GetInstance().Exit();
            Trigger.GetInstance().Exit();
            AnalogIO_.AnalogIO.GetInstance().Exit();
            DigitalIO_.DigitalIO.GetInstance().Exit();
            Motion_.Motion.GetInstance().Exit();
            Vision_.Vision.GetInstance().Exit();
            Language_.Language.GetInstance().Exit();
            JogManager_.JogManager.GetInstance().Exit();
            #endregion

            #region Task
            RegisteredInstances_.RegisteredInstanceManager.GetInstance().Exit();
            TaskAction_.TaskActionFlow.GetInstance().Exit();
            TaskAction_.TaskActionManager.GetInstance().Exit();
            TaskDevice_.TaskDevice.GetInstance().Exit();
            #endregion

            #region Recipe
            RecipeManager_.RecipeManager.GetInstance().Exit();
            #endregion

            #region File Management
            FileBorn_.FileBorn.GetInstance().Exit();
            FileComposite_.FileComposite.GetInstance().Exit();
            FileIOManager_.FileIOManager.GetInstance().Exit();
            #endregion

            #region Thread Timer
            ThreadTimer.GetInstance().Exit();
            #endregion
        }
        /// <summary>
        /// 2020.05.12 by yjlee [ADD] Initialize the instances of the DLL.
        /// </summary>
        public bool DoInitializeSequence()
        {
            bool bResult = false;
            string strContentsResult = null;

            switch (m_enInitializeStep)
            {
                case EN_INITIALIZATION_STEP.INIT_START:
                    if (false == ShowProgressForm()) { return false; }
                    strContentsResult = "The System is being Start... ";
                    break;

                #region Observer
                case EN_INITIALIZATION_STEP.INIT_OBSERVER_START:
                    strContentsResult = "The observers are being attached to the subjects... ";
                    break;

                case EN_INITIALIZATION_STEP.INIT_OBSERVER_END:
                    RegisterObserverEvent();
                    bResult = true;
                    break;
                #endregion

                #region File IO
                case EN_INITIALIZATION_STEP.INIT_FILEIO_START:
                    strContentsResult = "The file I/O is being initialized... ";
                    break;

                case EN_INITIALIZATION_STEP.INIT_FILEIO_END:
                    Log.LogWriter.GetInstance().Activate();
                    bResult = FileIOManager_.FileIOManager.GetInstance().Init();
                    break;
                #endregion

                #region Account
                case EN_INITIALIZATION_STEP.INIT_ACCOUNT_START:
                    strContentsResult = "The accout is being initialized... ";
                    break;

                case EN_INITIALIZATION_STEP.INIT_ACCOUNT_END:
                    bResult = Account_.Account.GetInstance().Init(System.Diagnostics.Debugger.IsAttached);

                    break;
                #endregion

                #region Alarm
                case EN_INITIALIZATION_STEP.INIT_ALARM_START:
                    strContentsResult = "The alarm is being initialized... ";
                    break;

                case EN_INITIALIZATION_STEP.INIT_ALARM_END:
                    bResult = Alarm_.Alarm.GetInstance().Init();
                    break;
                #endregion

                #region Socket
                case EN_INITIALIZATION_STEP.INIT_SOCKET_START:
                    strContentsResult = "The socket communication is being initialized... ";
                    break;

                case EN_INITIALIZATION_STEP.INIT_SOCKET_END:
                    bResult = Socket_.Socket.GetInstance().Init();
                    break;
                #endregion

                #region Serial
                case EN_INITIALIZATION_STEP.INIT_SERIAL_START:
                    strContentsResult = "The serial communication is being initialized... ";
                    break;

                case EN_INITIALIZATION_STEP.INIT_SERIAL_END:
                    bResult = Serial.GetInstance().Init();
                    break;
                #endregion

                #region Analog IO
                case EN_INITIALIZATION_STEP.INIT_ANALOG_IO_START:
                    strContentsResult = "The analog I/O is being initialized... ";
                    break;

                case EN_INITIALIZATION_STEP.INIT_ANALOG_IO_END:
                    {
                        m_arAnalogIOController = new AnalogIOController[1];
                        Define.DefineEnumProject.AppConfig.EN_ANALOG_IO_CONTROLLER controllerName
                            = Work.AppConfigManager.Instance.ControllerAnalog;

                        switch (controllerName)
                        {
                            case Define.DefineEnumProject.AppConfig.EN_ANALOG_IO_CONTROLLER.CREVIS_MODBUS_TCP:
                                m_arAnalogIOController[0] = new FrameOfSystem3.Controller.AnalogIO.CrevisModbusAnalogIOController();
                                break;
                            default:
                                m_arAnalogIOController[0] = null;
                                break;
                        }

                        bResult = AnalogIO.GetInstance().Init(ref m_arAnalogIOController);
                        if (m_arAnalogIOController[0] == null)
                        {
                            bResult = true;
                        }

                        bResult = true;
                    }
                    break;
                #endregion

                #region Digital IO
                case EN_INITIALIZATION_STEP.INIT_DIGITAL_IO_START:
                    strContentsResult = "The digital I/O is being initialized... ";
                    break;

                case EN_INITIALIZATION_STEP.INIT_DIGITAL_IO_END:
                    {
                        m_arDigitalIOController = new DigitalIOController[1];
                        Define.DefineEnumProject.AppConfig.EN_DIGITAL_IO_CONTROLLER controllerName
                            = Work.AppConfigManager.Instance.ControllerDigital;

                        switch (controllerName)
                        {
                            case Define.DefineEnumProject.AppConfig.EN_DIGITAL_IO_CONTROLLER.CREVIS_MODBUS_TCP:
                                m_arDigitalIOController[0] = new FrameOfSystem3.Controller.DigitalIO.CrevisModbusDigitalIOController();
                                break;
                            default:
                                m_arDigitalIOController[0] = null;
                                break;
                        }

                        bResult = DigitalIO.GetInstance().Init(ref m_arDigitalIOController);
                        if (m_arDigitalIOController[0] == null)
                            bResult = true;
                    }
                    break;
                #endregion

                #region Cylinder
                case EN_INITIALIZATION_STEP.INIT_CYLINDER_START:
                    strContentsResult = "The cylinder is being initialized... ";
                    break;

                case EN_INITIALIZATION_STEP.INIT_CYLINDER_END:
                    delegateCylinderForReadingInput = new DelegateForReadingIO(DigitalIO.GetInstance().ReadInput);
                    delegateCylinderForReadingOutput = new DelegateForReadingIO(DigitalIO.GetInstance().ReadOutput);
                    delegateCylinderForWritingOutput = new DelegateForWritingIO(DigitalIO.GetInstance().WriteOutput);

                    bResult = Cylinder.GetInstance().Init(delegateCylinderForReadingInput
                        , delegateCylinderForReadingOutput
                        , delegateCylinderForWritingOutput);
                    break;
                #endregion

                #region Interrupt
                case EN_INITIALIZATION_STEP.INIT_INTERRUPT_START:
                    strContentsResult = "The interrupt is being initialized... ";
                    break;

                case EN_INITIALIZATION_STEP.INIT_INTERRUPT_END:
                    delegateInterruptForReadingInput = new DelegateForReadingInput(DigitalIO.GetInstance().ReadInput);
                    delegateInterruptForWriteOutput = new DelegateForWriteDigitalOutput(DigitalIO.GetInstance().WriteOutput);

                    bResult = Interrupt.GetInstance().Init(delegateInterruptForReadingInput
                        , delegateInterruptForWriteOutput
                        , delegateInterruptForActionStart
                        , delegateInterruptForActionStop
                        , delegateInterruptForActionReset
                        , delegateInterruptForActionAlarm);
                    break;
                #endregion

                #region Trigger
                case EN_INITIALIZATION_STEP.INIT_TRIGGER_START:
                    strContentsResult = "The trigger is being initialized... ";
                    break;

                case EN_INITIALIZATION_STEP.INIT_TRIGGER_END:
                    delegateTriggerForWritingOutput = new DelegateForWritingOutput(DigitalIO.GetInstance().WriteOutput);
                    bResult = Trigger.GetInstance().Init(delegateTriggerForWritingOutput);
                    break;
                #endregion

                #region Motion
                case EN_INITIALIZATION_STEP.INIT_MOTION_START:
                    strContentsResult = "The motion is being initialized... ";
                    break;

                case EN_INITIALIZATION_STEP.INIT_MOTION_END:
                    {
                        m_arMotionController = new MotionController[1];
                        Define.DefineEnumProject.AppConfig.EN_MOTION_CONTROLLER controllerName = Work.AppConfigManager.Instance.ControllerMotion;
                        switch (controllerName)
                        {
                            default:
                                m_arMotionController[0] = null;
                                break;
                        }

                        bResult = Motion_.Motion.GetInstance().Init(ref m_arMotionController, Define.DefineConstant.Motion.INTERVAL_CHECKING_CONNECTION);
                        
                        bResult = true;
                    }
                    break;
                #endregion

                #region Langauge
                case EN_INITIALIZATION_STEP.INIT_LANGUAGE_START:
                    strContentsResult = "The language is being initialized... ";
                    break;

                case EN_INITIALIZATION_STEP.INIT_LANGUAGE_END:
					{
						var language = Language_.Language.GetInstance();
						bResult = language.Init();
						if (bResult)
						{
							language.SetLanguage(Work.AppConfigManager.Instance.Language);
						}
					}
                    break;
                #endregion

                #region TaskDevice
                case EN_INITIALIZATION_STEP.INIT_TASK_DEVICE_START:
                    strContentsResult = "The Task Device is being initialized... ";
                    break;

                case EN_INITIALIZATION_STEP.INIT_TASK_DEVICE_END:
                    bResult = TaskDevice.GetInstance().Init();
                    break;
                #endregion

                #region Registered Instances
                case EN_INITIALIZATION_STEP.INIT_REGISTERED_INSTANCES_START:
                    strContentsResult = "The registered manager is being initialized... ";
                    break;

                case EN_INITIALIZATION_STEP.INIT_REGISTERED_INSTANCES_END:
                    Interface.RegisteredInterface pInterface = new Interface.RegisteredInterface();
                    m_pRegisteredInterface = pInterface as RegisteredInterfaces;

                    bResult = RegisteredInstanceManager.GetInstance().Init(m_pRegisteredInterface);
                    break;
                #endregion

                #region Thread Timer
                case EN_INITIALIZATION_STEP.INIT_THREADTIMER_START:
                    strContentsResult = "The ThreadTimer is being initialized... ";
                    break;

                case EN_INITIALIZATION_STEP.INIT_THREADTIMER_END:
                    GetDllInstance();

                    bResult = SetThreadTimer();
                    break;
                #endregion

                #region Recipe
                case EN_INITIALIZATION_STEP.INIT_RECIPE_START:
                    strContentsResult = "The instance of the recipe is being initialized... ";
                    break;

                case EN_INITIALIZATION_STEP.INIT_RECIPE_END:
                    bResult = RecipeManager_.RecipeManager.GetInstance().Init();
                    break;
                #endregion

                #region Config Files
                case EN_INITIALIZATION_STEP.INIT_CONFIG_INSTANCES_START:
                    strContentsResult = "The system makes the instances for the device configurations... ";
                    break;

                case EN_INITIALIZATION_STEP.INIT_CONFIG_INSTANCES_END:
                    Functional.Storage.GetInstance().Init();
                    FileBorn_.FileBorn.GetInstance().Init();

                    bResult         = true;
                    bResult         &= Config.ConfigTask.GetInstance().Init();
                    bResult         &= Config.ConfigDigitalIO.GetInstance().Init();
					bResult         &= Config.ConfigAnalogIO.GetInstance().Init();
					bResult			&= Config.ConfigCylinder.GetInstance().Init();
					bResult			&= Config.ConfigSocket.GetInstance().Init();
					bResult			&= Config.ConfigSerial.GetInstance().Init();
					bResult			&= Config.ConfigInterrupt.GetInstance().Init();
					bResult			&= Config.ConfigTrigger.GetInstance().Init();
					bResult			&= Config.ConfigLanguage.GetInstance().Init();
					bResult			&= Config.ConfigAlarm.GetInstance().Init();
					bResult			&= Config.ConfigMotion.GetInstance().Init();
					bResult			&= Config.ConfigMotionSpeed.GetInstance().Init();
					bResult			&= Config.ConfigJog.GetInstance().Init();
					bResult			&= Config.ConfigDevice.GetInstance().Init();
                    bResult 		&= Config.ConfigPort.GetInstance().Init();
                    bResult 		&= Config.ConfigDynamicLink.GetInstance().Init();
                    bResult 		&= Config.ConfigFlow.GetInstance().Init();
                    bResult 		&= Config.ConfigTool.GetInstance().Init();          // 2021.09.27 by jhchoo [ADD]
					bResult			&= Config.ConfigWCF.GetInstance().Init();           // 2024.02.01 by jhlee [ADD]
					bResult			&= Account.CAccount.GetInstance().Init();
                    break;
                #endregion

                #region Task
                case EN_INITIALIZATION_STEP.INIT_TASK_START:
                    strContentsResult = "The system makes the instances of the task... ";
                    break;

                case EN_INITIALIZATION_STEP.INIT_TASK_END:
                    bResult = Task.TaskOperator.GetInstance().InitializeTask();
                    break;
                #endregion

                #region Load Recipe
                case EN_INITIALIZATION_STEP.LOAD_RECIPE_START:
                    strContentsResult = "The system is loading the file of the recipe... ";
                    break;

                case EN_INITIALIZATION_STEP.LOAD_RECIPE_END:
                    bResult = Recipe.Recipe.GetInstance().Init();
                    //bResult = true;
                    break;
                #endregion

                #region Vision
                //case EN_INITIALIZATION_STEP.INIT_VISION_START:
                //    strContentsResult       = "The vision is being initialized... ";
                //    break;

                //case EN_INITIALIZATION_STEP.INIT_VISION_END:
                //   Vision_.Vision vision = Vision_.Vision.GetInstance();
                //    bResult = vision.Init(new Controller.Vision.ProtecVisionController((int)Define.DefineEnumProject.Socket.EN_SOCKET_INDEX.VISION), Define.DefineConstant.Vision.COUNT_CAM);
                //    if(bResult)
                //    {
                //        FrameOfSystem3.Task.TaskOperator.GetInstance().AddDelegateSetOperation(new RunningMain_.RunningMain.DelegateWithSetOperation(vision.ResetVision));

                //        // check here : vision algorithm assine
                //        //vision.AddResultParsingDelegate((int)EN_CAMERA_LIST.ALIGN, (int)EN_VISION_ALGORITHM.FLUX_SUBJECT_1st, VisionResultParser_BP5000IR.ALIGN_MATCHING_DB);
                //    }
                //    break;
                #endregion

                #region LOG
                case EN_INITIALIZATION_STEP.INIT_LOG_START:
                    strContentsResult = "The log instance is being initialized... ";
                    break;

                case EN_INITIALIZATION_STEP.INIT_LOG_END:
                    {
                        var logManager = Log.LogManager.GetInstance();
                        bResult = logManager.Init();
                        logManager.CaptionMaterialType = "WAFER";   // TODO : Material type 설정 (나중에 해도 됨)
                        logManager.RegisterGetLotIdFunction(new Log.LogManager.DeleGetLotId(() => { return Log.LogManager.EMPTY_DATA; }));  // TODO : lot id 반환 함수 등록 (나중에 해도 됨)
                        logManager.RegisterGetMaterialIdFromTaskName(new Log.LogManager.DeleGetMaterialIdFromTaskName((taskName) => { return string.Format("{0}_{1}", taskName, Log.LogManager.EMPTY_DATA); }));  // TODO : 자재 id 반환 함수 등록 (나중에 해도 됨)
                    }
                    break;
                #endregion

                #region INTERLOCK
                case EN_INITIALIZATION_STEP.INIT_INTERLOCK_START:
                    strContentsResult = "The Interlock instance is being initialized... ";
                    break;

                case EN_INITIALIZATION_STEP.INIT_INTERLOCK_END:
                    bResult = Config.ConfigInterlock.GetInstance().Init();
                    break;
                #endregion

                #region SCHEDULER
                case EN_INITIALIZATION_STEP.INIT_SCHEDULER_START:
                    strContentsResult = "The Scheduler is being initialized... ";
                    break;

                case EN_INITIALIZATION_STEP.INIT_SCHEDULER_END:
                    //                     Scheduler.Schedule DeleteLog = new Scheduler.Schedule();
                    //                     DeleteLog.Hour = 0;
                    //                     DeleteLog.delFunction = new Scheduler.delGenerateFunction(FunctionsETC.DeleteLogFile);
                    //                     Scheduler.GetInstance().AddSchedule("DeleteFile", DeleteLog);

                    //                     Scheduler.Schedule BackUpFile = new Scheduler.Schedule();
                    //                     BackUpFile.Hour = 0;
                    //                     BackUpFile.delFunction = new Scheduler.delGenerateFunction(FunctionsETC.ImportantFileBackup);
                    //                     Scheduler.GetInstance().AddSchedule("BackUpFile", BackUpFile);

                    bResult = Scheduler.GetInstance().Init();
                    break;
                #endregion

				#region EQUIPMENT PROPERTY
                case EN_INITIALIZATION_STEP.INIT_EQUIPMENT_PROPERTY_START:
                    strContentsResult = "The Equipment Property is being initialized... ";
                    break;

                case EN_INITIALIZATION_STEP.INIT_EQUIPMENT_PROPERTY_END:
                    EquipmentProperty.EquipmentProperty.GetInstance().delegateUpdateProperty = new EquipmentProperty.DelegateUpdateProperty(UpdateEquipmentProperty);
                    break;
				#endregion

                #region RAM Metrics
                case EN_INITIALIZATION_STEP.INIT_RAM_METRICS_START:
                    strContentsResult = "The RAM Metrics is being initialized... ";
                    break;

                case EN_INITIALIZATION_STEP.INIT_RAM_METRICS_END:
                    EquipmentMonitor.RAM_Metrics.GetInstance().Init();
                    bResult = true;
                    break;
                #endregion

                #region FTP
                case EN_INITIALIZATION_STEP.INIT_FTP_START:
                    strContentsResult = "The FTP is being initialized... ";
                    break;

                case EN_INITIALIZATION_STEP.INIT_FTP_END:
                    #region FTP
                    Config.ConfigFTP.GetInstance().Init();
                    #endregion
                    bResult = true;
                    break;
                #endregion
				
                #region <Init EFEM Modules>
                case EN_INITIALIZATION_STEP.INIT_EFEM_MODULES_START:
                    strContentsResult = "The EFEM modules are being initialized... ";
                    break;
                case EN_INITIALIZATION_STEP.INIT_EFEM_MODULES_END:
                    {
                        int i;

                        #region <LoadPorts>
                        var loadPortControllerType
                            = Work.AppConfigManager.Instance.LoadPortControllerType;
                        
                        int countLoadPort = Work.AppConfigManager.Instance.CountLoadPort;

                        for (i = 0; i < countLoadPort; ++i)
                        {
                            int commIndex = 0;
                            int portId = i + 1;
                            string name = string.Format("LP{0}", portId);

                            EFEM.Modules.LoadPort.LoadPortController lp;
                            switch (loadPortControllerType)
                            {
                                case Define.DefineEnumProject.AppConfig.EN_LOADPORT_CONTROLLER.NONE:        // DuraPort
                                    {
                                        commIndex = -1;
                                        lp = new EFEM.Modules.LoadPort.LoadPortControllers.LoadPortControllerSimulator(portId, name, Define.DefineEnumBase.Common.EN_CONNECTION_TYPE.SERIAL, commIndex);
                                    }
                                    break;
                                case Define.DefineEnumProject.AppConfig.EN_LOADPORT_CONTROLLER.DURAPORT:
                                    {
                                        commIndex = (int)Define.DefineEnumProject.Serial.EN_SERIAL_INDEX.LOADPORT_1 + i;
                                        lp = new EFEM.Modules.LoadPort.LoadPortControllers.DuraportController(portId, name, Define.DefineEnumBase.Common.EN_CONNECTION_TYPE.SERIAL, commIndex);
                                    }
                                    break;
                                case Define.DefineEnumProject.AppConfig.EN_LOADPORT_CONTROLLER.SELOP8:
                                    {
                                        commIndex = (int)Define.DefineEnumProject.Serial.EN_SERIAL_INDEX.LOADPORT_1 + i;
                                        lp = new EFEM.Modules.LoadPort.LoadPortControllers.SELOP8Controller(portId, name, Define.DefineEnumBase.Common.EN_CONNECTION_TYPE.SERIAL, commIndex);
                                    }
                                    break;

                                default:
                                    lp = null;
                                    break;
                            }

                            EFEM.Defines.LoadPort.AutomatedMaterialHandlingSystemController amhsControl = null;

                            #region <LoadPorts - PWA-500>
                            if (false == Work.AppConfigManager.Instance.Customer.Equals(Define.DefineEnumProject.AppConfig.EN_CUSTOMER.S_TP))
                            {
                                switch (Work.AppConfigManager.Instance.InterfaceTypePIO)
                                {
                                    case Define.DefineEnumProject.AppConfig.EN_PIO_INTERFACE_TYPE.E84:
                                        {
                                            const int Offset = 8;

                                            int saftyInterLockIndex = (int)Define.DefineEnumProject.DigitalIO.PWA500W.EN_DIGITAL_IN.PROTECTION_BAR_LP;

                                            int baseInputIndex = (int)Define.DefineEnumProject.DigitalIO.PWA500W.EN_DIGITAL_IN.LP1_PIO_VALID + i * Offset;
                                            int baseOutputIndex = (int)Define.DefineEnumProject.DigitalIO.PWA500W.EN_DIGITAL_OUT.LP1_PIO_L_REQ + i * Offset;

                                            Dictionary<int, Tuple<int, string>> inputs = new Dictionary<int, Tuple<int, string>>();
                                            Dictionary<int, Tuple<int, string>> outputs = new Dictionary<int, Tuple<int, string>>();
                                            for (int index = 0; index < Offset; ++index)
                                            {
                                                EFEM.Defines.LoadPort.E84InputSignals inputSignalEnums = EFEM.Defines.LoadPort.E84InputSignals.Valid + index;
                                                inputs[index] = new Tuple<int, string>(index + baseInputIndex, inputSignalEnums.ToString());

                                                EFEM.Defines.LoadPort.E84OutputSignals outputSignalEnums = EFEM.Defines.LoadPort.E84OutputSignals.LoadRequest + index;
                                                outputs[index] = new Tuple<int, string>(index + baseOutputIndex, outputSignalEnums.ToString());
                                            }

                                            amhsControl = new EFEM.Defines.LoadPort.E84Handler(i, saftyInterLockIndex, inputs, outputs);
                                            amhsControl.AssignActionModeChangeBeforeCarrierLoad(EFEM.Modules.LoadPortManager.Instance.ChangeLoadPortMode);
                                        }
                                        break;
                                    case Define.DefineEnumProject.AppConfig.EN_PIO_INTERFACE_TYPE.E23:
                                        {
                                            const int Offset = 8;

                                            int saftyInterLockIndex = (int)Define.DefineEnumProject.DigitalIO.PWA500W.EN_DIGITAL_IN.PROTECTION_BAR_LP;

                                            int baseInputIndex = (int)Define.DefineEnumProject.DigitalIO.PWA500W.EN_DIGITAL_IN.LP1_PIO_VALID + i * Offset;
                                            int baseOutputIndex = (int)Define.DefineEnumProject.DigitalIO.PWA500W.EN_DIGITAL_OUT.LP1_PIO_L_REQ + i * Offset;

                                            Dictionary<int, Tuple<int, string>> inputs = new Dictionary<int, Tuple<int, string>>();
                                            Dictionary<int, Tuple<int, string>> outputs = new Dictionary<int, Tuple<int, string>>();
                                            for (int index = 0; index < Offset; ++index)
                                            {
                                                EFEM.Defines.LoadPort.E23InputSignals inputSignalEnums = EFEM.Defines.LoadPort.E23InputSignals.Valid + index;
                                                inputs[index] = new Tuple<int, string>(index + baseInputIndex, inputSignalEnums.ToString());

                                                EFEM.Defines.LoadPort.E23OutputSignals outputSignalEnums = EFEM.Defines.LoadPort.E23OutputSignals.LoadRequest + index;
                                                outputs[index] = new Tuple<int, string>(index + baseOutputIndex, outputSignalEnums.ToString());
                                            }

                                            amhsControl = new EFEM.Defines.LoadPort.CustomizedE23(i, saftyInterLockIndex, inputs, outputs);
                                            amhsControl.AssignActionModeChangeBeforeCarrierLoad(EFEM.Modules.LoadPortManager.Instance.ChangeLoadPortMode);
                                        }
                                        break;
                                    default:
                                        break;
                                }
                            }
                            #endregion </LoadPorts - PWA-500>

                            #region <LoadPorts - PWA-500Bin>
                            else
                            {
                            switch (Work.AppConfigManager.Instance.InterfaceTypePIO)
                            {
                                case Define.DefineEnumProject.AppConfig.EN_PIO_INTERFACE_TYPE.E84:
                                    {
                                        const int Offset = 8;

                                        int interLockOffset = i / (countLoadPort / 2);
                                        int saftyInterLockIndex = (int)Define.DefineEnumProject.DigitalIO.PWA500BIN.EN_DIGITAL_IN.PROTECTION_BAR_LP_1_2_3 + interLockOffset;
                                        
                                        int baseInputIndex = (int)Define.DefineEnumProject.DigitalIO.PWA500BIN.EN_DIGITAL_IN.LP1_PIO_VALID + i * Offset;
                                        int baseOutputIndex = (int)Define.DefineEnumProject.DigitalIO.PWA500BIN.EN_DIGITAL_OUT.LP1_PIO_L_REQ + i * Offset;

                                        Dictionary<int, Tuple<int, string>> inputs = new Dictionary<int, Tuple<int, string>>();
                                        Dictionary<int, Tuple<int, string>> outputs = new Dictionary<int, Tuple<int, string>>();
                                        for (int index = 0; index < Offset; ++index)
                                        {
                                            EFEM.Defines.LoadPort.E84InputSignals inputSignalEnums = EFEM.Defines.LoadPort.E84InputSignals.Valid + index;
                                            inputs[index] = new Tuple<int, string>(index + baseInputIndex, inputSignalEnums.ToString());

                                            EFEM.Defines.LoadPort.E84OutputSignals outputSignalEnums = EFEM.Defines.LoadPort.E84OutputSignals.LoadRequest + index;
                                            outputs[index] = new Tuple<int, string>(index + baseOutputIndex, outputSignalEnums.ToString());
                                        }

                                        amhsControl = new EFEM.Defines.LoadPort.E84Handler(i, saftyInterLockIndex, inputs, outputs);
                                        amhsControl.AssignActionModeChangeBeforeCarrierLoad(EFEM.Modules.LoadPortManager.Instance.ChangeLoadPortMode);
                                    }
                                    break;
                                case Define.DefineEnumProject.AppConfig.EN_PIO_INTERFACE_TYPE.E23:
                                    {
                                        const int Offset = 8;

                                        int interLockOffset = i / (countLoadPort / 2);
                                        int saftyInterLockIndex = (int)Define.DefineEnumProject.DigitalIO.PWA500BIN.EN_DIGITAL_IN.PROTECTION_BAR_LP_1_2_3 + interLockOffset;
                                        
                                        int baseInputIndex = (int)Define.DefineEnumProject.DigitalIO.PWA500BIN.EN_DIGITAL_IN.LP1_PIO_VALID + i * Offset;
                                        int baseOutputIndex = (int)Define.DefineEnumProject.DigitalIO.PWA500BIN.EN_DIGITAL_OUT.LP1_PIO_L_REQ + i * Offset;

                                        Dictionary<int, Tuple<int, string>> inputs = new Dictionary<int, Tuple<int, string>>();
                                        Dictionary<int, Tuple<int, string>> outputs = new Dictionary<int, Tuple<int, string>>();
                                        for (int index = 0; index < Offset; ++index)
                                        {
                                            EFEM.Defines.LoadPort.E23InputSignals inputSignalEnums = EFEM.Defines.LoadPort.E23InputSignals.Valid + index;
                                            inputs[index] = new Tuple<int, string> (index + baseInputIndex, inputSignalEnums.ToString());

                                            EFEM.Defines.LoadPort.E23OutputSignals outputSignalEnums = EFEM.Defines.LoadPort.E23OutputSignals.LoadRequest + index;
                                            outputs[index] = new Tuple<int, string>(index + baseOutputIndex, outputSignalEnums.ToString());
                                        }

                                        amhsControl = new EFEM.Defines.LoadPort.CustomizedE23(i, saftyInterLockIndex, inputs, outputs);
                                        amhsControl.AssignActionModeChangeBeforeCarrierLoad(EFEM.Modules.LoadPortManager.Instance.ChangeLoadPortMode);
                                    }
                                    break;
                                default:
                                    break;
                            }
                            }
                            #endregion </LoadPorts - PWA-500Bin>
                            
                            Work.AppConfigManager.Instance.LoadPortLocationNames.TryGetValue(i, out Dictionary<string, string> locationNames);
                            EFEM.Modules.LoadPortManager.Instance.AssignLoadPorts(new EFEM.Modules.LoadPort.LoadPortOperator(portId,
                                name,
                                lp,
                                amhsControl,
                                locationNames));

                            #region <LoadPort Scheduler>
                            var processType = Work.AppConfigManager.Instance.ProcessType;
                            EFEM.ActionScheduler.LoadPortActionSchedulers.BaseLoadPortActionScheduler scheduler = null;
                            switch (processType)
                            {
                                case Define.DefineEnumProject.AppConfig.EN_PROCESS_TYPE.NONE:
                                case Define.DefineEnumProject.AppConfig.EN_PROCESS_TYPE.BIN_SORTER:
                                    {
                                        scheduler = null;// new EFEM.ActionScheduler.LoadPortActionSchedulers.ProcessTypes.PWA500BinSorterRobotActionScheduler(i);
                                        EFEM.CustomizedByProcessType.PWA500BIN.LoadPortType loadPortType = (EFEM.CustomizedByProcessType.PWA500BIN.LoadPortType)i;
                                        switch (loadPortType)
                                        {
                                            case EFEM.CustomizedByProcessType.PWA500BIN.LoadPortType.Bin_3:
                                            case EFEM.CustomizedByProcessType.PWA500BIN.LoadPortType.Bin_2:
                                            case EFEM.CustomizedByProcessType.PWA500BIN.LoadPortType.Bin_1:
                                                scheduler = new EFEM.CustomizedByProcessType.PWA500BIN.BinLoadPortActionScheduler(i);
                                                break;
                                            case EFEM.CustomizedByProcessType.PWA500BIN.LoadPortType.EmptyTape:
                                                scheduler = new EFEM.CustomizedByProcessType.PWA500BIN.EmptyTapeLoadPortActionScheduler(i);
                                                break;
                                            case EFEM.CustomizedByProcessType.PWA500BIN.LoadPortType.Core_2:
                                            case EFEM.CustomizedByProcessType.PWA500BIN.LoadPortType.Core_1:
                                                scheduler = new EFEM.CustomizedByProcessType.PWA500BIN.CoreLoadPortActionScheduler(i);
                                                break;
                                            default:
                                                break;
                                        }
                                    }
                                    break;
                                case Define.DefineEnumProject.AppConfig.EN_PROCESS_TYPE.DIE_TRANSFER:
                                    {
                                        scheduler = null;// new EFEM.ActionScheduler.LoadPortActionSchedulers.ProcessTypes.PWA500BinSorterRobotActionScheduler(i);
                                        EFEM.CustomizedByProcessType.PWA500W.LoadPortType loadPortType = (EFEM.CustomizedByProcessType.PWA500W.LoadPortType)i;
                                        switch (loadPortType)
                                        {
                                            case EFEM.CustomizedByProcessType.PWA500W.LoadPortType.Sort_12:
                                                scheduler = new EFEM.CustomizedByProcessType.PWA500W.BinLoadPortActionScheduler(i);
                                                break;
                                            case EFEM.CustomizedByProcessType.PWA500W.LoadPortType.Core_12:
                                            case EFEM.CustomizedByProcessType.PWA500W.LoadPortType.Core_8_2:
                                            case EFEM.CustomizedByProcessType.PWA500W.LoadPortType.Core_8_1:
                                                scheduler = new EFEM.CustomizedByProcessType.PWA500W.CoreLoadPortActionScheduler(i);
                                                break;
                                            default:
                                                break;
                                        }
                                    }
                                    break;

                                default:
                                    break;
                            }

                            EFEM.ActionScheduler.LoadPortActionSchedulerManager.Instance.CreateScheduler(i, scheduler);
                            #endregion </LoadPort Scheduler>

                            #region <Rfid>

                            #region <Foup>
                            {
                                Define.DefineEnumProject.AppConfig.EN_RFID_CONTROLLER controllerRfidFoup = Work.AppConfigManager.Instance.ControllerRfidFoup;
                                bool failed = false;
                                int relIndex = (int)Define.DefineEnumProject.Serial.EN_SERIAL_INDEX.RFID_FOUP_1 + i;
                                RFIDOnly.RRFIDReader reader = null;
                                switch (controllerRfidFoup)
                                {
                                    case Define.DefineEnumProject.AppConfig.EN_RFID_CONTROLLER.NONE:
                                        reader = new EFEM.Modules.RFID.Controllers.RfidSimulator(
                                                portId,
                                                Define.DefineEnumBase.Common.EN_CONNECTION_TYPE.SERIAL,
                                                relIndex);
                                        break;
                                    case Define.DefineEnumProject.AppConfig.EN_RFID_CONTROLLER.XEDION:
                                        reader = new EFEM.Modules.RFID.Controllers.XedionRfid(
                                                portId,
                                                Define.DefineEnumBase.Common.EN_CONNECTION_TYPE.SERIAL,
                                                relIndex);
                                        break;
                                    case Define.DefineEnumProject.AppConfig.EN_RFID_CONTROLLER.CEYON:
                                        reader = new EFEM.Modules.RFID.Controllers.CeyonRfid(
                                                portId,
                                                Define.DefineEnumBase.Common.EN_CONNECTION_TYPE.SERIAL,
                                                relIndex);
                                        break;
                                    default:
                                        failed = true;
                                        break;
                                }

                                if (failed)
                                    continue;

                                EFEM.Modules.RFIDManager.Instance.AssigReader(i, EFEM.Defines.LoadPort.LoadPortLoadingMode.Foup, reader);
                                EFEM.Modules.RFIDManager.Instance.SetCarrierIdAddress(i, EFEM.Defines.LoadPort.LoadPortLoadingMode.Foup,
                                    Work.AppConfigManager.Instance.FoupRfidCarrierIdAddress,
                                    Work.AppConfigManager.Instance.FoupRfidCarrierIdLength);
                                EFEM.Modules.RFIDManager.Instance.SetLotIdAddress(i, EFEM.Defines.LoadPort.LoadPortLoadingMode.Foup,
                                    Work.AppConfigManager.Instance.FoupRfidLotIdAddress,
                                    Work.AppConfigManager.Instance.FoupRfidLotIdLength);
                            }
                            #endregion </Foup>

                            #region <Cassette>
                            {
                                Define.DefineEnumProject.AppConfig.EN_RFID_CONTROLLER controllerRfidCassette
                                    = Work.AppConfigManager.Instance.ControllerRfidCassette;
                                int countRfidCassette = Work.AppConfigManager.Instance.CountRfidCassette;

                                bool failed = false;
                                int relIndex = (int)Define.DefineEnumProject.Serial.EN_SERIAL_INDEX.RFID_CASSETTE_1 + i;
                                RFIDOnly.RRFIDReader reader = null;
                                switch (controllerRfidCassette)
                                {
                                    case Define.DefineEnumProject.AppConfig.EN_RFID_CONTROLLER.NONE:
                                        reader = new EFEM.Modules.RFID.Controllers.RfidSimulator(
                                                portId,
                                                Define.DefineEnumBase.Common.EN_CONNECTION_TYPE.SERIAL,
                                                relIndex);
                                        break;
                                    case Define.DefineEnumProject.AppConfig.EN_RFID_CONTROLLER.XEDION:
                                        reader = new EFEM.Modules.RFID.Controllers.XedionRfid(
                                                portId,
                                                Define.DefineEnumBase.Common.EN_CONNECTION_TYPE.SERIAL,
                                                relIndex);
                                        break;
                                    case Define.DefineEnumProject.AppConfig.EN_RFID_CONTROLLER.CEYON:
                                        reader = new EFEM.Modules.RFID.Controllers.CeyonRfid(
                                                portId,
                                                Define.DefineEnumBase.Common.EN_CONNECTION_TYPE.SERIAL,
                                                relIndex);
                                        break;
                                    default:
                                        failed = true;
                                        break;
                                }

                                if (failed)
                                    continue;

                                EFEM.Modules.RFIDManager.Instance.AssigReader(i, EFEM.Defines.LoadPort.LoadPortLoadingMode.Cassette, reader);
                                EFEM.Modules.RFIDManager.Instance.SetCarrierIdAddress(i, EFEM.Defines.LoadPort.LoadPortLoadingMode.Cassette,
                                    Work.AppConfigManager.Instance.CassetteRfidCarrierIdAddress,
                                    Work.AppConfigManager.Instance.CassetteRfidCarrierIdLength);
                                EFEM.Modules.RFIDManager.Instance.SetLotIdAddress(i, EFEM.Defines.LoadPort.LoadPortLoadingMode.Cassette,
                                    Work.AppConfigManager.Instance.CassetteRfidLotIdAddress,
                                    Work.AppConfigManager.Instance.CassetteRfidLotIdLength);
                            }
                            #endregion </Cassette>

                            #endregion </Rfid>

                        }
                        #endregion </LoadPorts>

                        #region <Robot>
                        var atmRobotControllerType
                            = Work.AppConfigManager.Instance.AtmRobotControllerType;
                        int countRobot = Work.AppConfigManager.Instance.CountRobot;

                        for (i = 0; i < countRobot; ++i)
                        {
                            #region <Robot Scheduler>
                            var processType = Work.AppConfigManager.Instance.ProcessType;
                            EFEM.ActionScheduler.RobotActionSchedulers.BaseRobotActionScheduler scheduler = null;
                            switch (processType)
                            {
                                case Define.DefineEnumProject.AppConfig.EN_PROCESS_TYPE.NONE:
                                case Define.DefineEnumProject.AppConfig.EN_PROCESS_TYPE.BIN_SORTER:
                                    scheduler = new EFEM.CustomizedByProcessType.PWA500BIN.PWA500BinSorterRobotActionScheduler(i);
                                    break;
                                case Define.DefineEnumProject.AppConfig.EN_PROCESS_TYPE.DIE_TRANSFER:
                                    scheduler = new EFEM.CustomizedByProcessType.PWA500W.PWA500WRobotActionScheduler(i);
                                    break;
                                default:
                                    break;
                            }
                            
                            EFEM.ActionScheduler.RobotActionSchedulerManager.Instance.CreateScheduler(i, scheduler);
                            #endregion </Robot Scheduler>

                            int commIndex = 0;
                            string name = string.Format("Robot{0}", i + 1);

                            EFEM.Modules.AtmRobot.AtmRobotController robot;

                            Work.AppConfigManager.Instance.RobotStationNames.TryGetValue(i, out Dictionary<string, string> stationNames);
                            
                            switch (atmRobotControllerType)
                            {
                                case Define.DefineEnumProject.AppConfig.EN_ROBOT_CONTROLLER.NONE:
                                    {
                                        commIndex = -1;
                                        robot = new EFEM.Modules.AtmRobot.AtmRobotControllers.RobotControllerSimulator(i,
                                            stationNames,
                                            Define.DefineEnumBase.Common.EN_CONNECTION_TYPE.SERIAL, commIndex);
                                    }
                                    break;
                                case Define.DefineEnumProject.AppConfig.EN_ROBOT_CONTROLLER.QUADRA_ATM_ROBOT:
                                    {
                                        commIndex = (int)Define.DefineEnumProject.Serial.EN_SERIAL_INDEX.ATM_ROBOT + i;
                                        robot = new EFEM.Modules.AtmRobot.AtmRobotControllers.QuadraRobotController(i,
                                            stationNames,
                                            Define.DefineEnumBase.Common.EN_CONNECTION_TYPE.SERIAL, commIndex);
                                    }
                                    break;
                                case Define.DefineEnumProject.AppConfig.EN_ROBOT_CONTROLLER.NRC:
                                    {
                                        commIndex = (int)Define.DefineEnumProject.Socket.EN_SOCKET_INDEX.ATM_ROBOT + i;
                                        robot = new EFEM.Modules.AtmRobot.AtmRobotControllers.NRCRobotController(i,
                                            stationNames,
                                            Define.DefineEnumBase.Common.EN_CONNECTION_TYPE.TCP, commIndex);

                                    }
                                    break;
                                default:
                                    robot = null;
                                    break;
                            }

                            EFEM.Modules.AtmRobotManager.Instance.AssignRobots(new EFEM.Modules.AtmRobot.AtmRobotOperator(i, name, robot, stationNames));
                        }
                        #endregion </Robot>

                        #region <Process Module>
                        bool simulation = Work.AppConfigManager.Instance.ProcessModuleSimulation;
                        int countProcessModule = Work.AppConfigManager.Instance.CountProcessModule;
                        for (i = 0; i < countProcessModule; ++i)
                        {
                            // type
                            var ProcessType = Work.AppConfigManager.Instance.ProcessType;

                            // name
                            string name = ProcessType.ToString();

                            // location
                            Work.AppConfigManager.Instance.ProcessModuleLocationNames.TryGetValue(i, out string[] locationNames);

                            EFEM.Modules.ProcessModule.BaseProcessModule module = null;
                            EFEM.Modules.ProcessModule.Communicator.BaseProcessModuleCommunicator communicator = null;
                            switch (ProcessType)
                            {
                                case Define.DefineEnumProject.AppConfig.EN_PROCESS_TYPE.NONE:
                                case Define.DefineEnumProject.AppConfig.EN_PROCESS_TYPE.BIN_SORTER:
                                    {
                                        communicator = new EFEM.CustomizedByProcessType.PWA500BIN.PWA500BINCommunicator(locationNames, simulation);
                                        module = new EFEM.CustomizedByProcessType.PWA500BIN.ProcessModulePWA500BIN(i, communicator, name, locationNames, simulation);
                                    }
                                    break;
                                case Define.DefineEnumProject.AppConfig.EN_PROCESS_TYPE.DIE_TRANSFER:
                                    {
                                        communicator = new EFEM.CustomizedByProcessType.PWA500W.PWA500WCommunicator(locationNames, simulation);
                                        module = new EFEM.CustomizedByProcessType.PWA500W.ProcessModulePWA500W(i, communicator, name, locationNames, simulation);
                                    }
                                    break;
                                default:
                                    break;
                            }

                            if (module != null)
                            {
                                EFEM.Modules.ProcessModuleGroup.Instance.AssignProcessModule(i, module);
                            }
                            bResult = true;
                        }
                        #endregion </Process Module>

                        StartETCThreadTimer();

                        bResult = true;
                    }
                    break;
                #endregion </Init EFEM Modules>

                #region <External Devices>
                case EN_INITIALIZATION_STEP.INIT_EXTERNAL_DEVICE_START:
                    strContentsResult = "The external devices are being initialized... ";
                    break;
                case EN_INITIALIZATION_STEP.INIT_EXTERNAL_DEVICE_END:
                    {
                        #region <Modbus>
                        // 2024.11.06. jhlim [DEL] 필요한가??
                        //ExternalDevice.Socket.ModbusTCPClient.GetInstance((int)Define.DefineEnumProject.Socket.EN_SOCKET_INDEX.MODBUS).Init();
                        #endregion </Modbus>

                        #region <Fan Filter Unit>
                        bResult &= ExternalDevice.Serial.FanFilterUnit.FanFilterUnitManager.Instance.Activate();
                        ExternalDevice.Serial.FanFilterUnit.FanFilterUnitController controller
                            = new ExternalDevice.Serial.FanFilterUnit.Bluecord.FanFilterUnitControllerBluecord((int)Define.DefineEnumProject.Serial.EN_SERIAL_INDEX.FFU,
                            Define.DefineEnumBase.Common.EN_CONNECTION_TYPE.SERIAL,
                            Work.AppConfigManager.Instance.UseDifferentialPressureMode,
                            Work.AppConfigManager.Instance.CountFanFilterUnit);
                        ExternalDevice.Serial.FanFilterUnit.FanFilterUnitManager.Instance.AddController(controller);
                        #endregion </Fan Filter Unit>

                        bResult = true;
                    }
                    break;

                #endregion </External Devices>

                #region <Init EFEM Module Information>
                case EN_INITIALIZATION_STEP.INIT_EFEM_MODULE_INFORMATION_START:
                    strContentsResult = "The EFEM module informations are being initialized... ";
                    break;

                case EN_INITIALIZATION_STEP.INIT_EFEM_MODULE_INFORMATION_END:
                    {
                        // 로드포트 활성화
                        for (int i = 0; i < EFEM.Modules.LoadPortManager.Instance.Count; ++i)
                        {
                            Recipe.PARAM_EQUIPMENT param = Recipe.PARAM_EQUIPMENT.UseLoadPort1 + i;
                            bool useLoadPort = FrameOfSystem3.Recipe.Recipe.GetInstance().GetValue(Recipe.EN_RECIPE_TYPE.EQUIPMENT,
                                param.ToString(), 0, Recipe.EN_RECIPE_PARAM_TYPE.VALUE,
                                true);
                            EFEM.Modules.LoadPortManager.Instance.SetLoadPortEnabled(i, useLoadPort);
                        }

                        EFEM.MaterialTracking.SubstrateManager.Instance.LoadRecoveryDataAll();

                        //for(int i = 0; i < EFEM.Modules.AtmRobotManager.Instance.Count; ++i)
                        //{
                        //    string robotName = EFEM.Modules.AtmRobotManager.Instance.GetRobotName(i);
                        //    Dictionary<EFEM.Defines.AtmRobot.RobotArmTypes, EFEM.MaterialTracking.Substrate> substrates = new Dictionary<EFEM.Defines.AtmRobot.RobotArmTypes, EFEM.MaterialTracking.Substrate>();
                        //    if (EFEM.MaterialTracking.SubstrateManager.Instance.GetSubstratesAtRobotAll(robotName, ref substrates))
                        //    {
                        //        EFEM.Modules.AtmRobotManager.Instance.AssignSubstrate(i, substrates);
                        //    }
                        //}

                        // Language 설정
                        string language = Recipe.Recipe.GetInstance().GetValue(Recipe.EN_RECIPE_TYPE.EQUIPMENT, Recipe.PARAM_EQUIPMENT.MachineLanguage.ToString(), Config.ConfigLanguage.EN_PARAM_LANGUAGE.ENGLISH.ToString());
                        if (Enum.TryParse(language, out Config.ConfigLanguage.EN_PARAM_LANGUAGE targetLanguage))
                        {
                            Config.ConfigLanguage.GetInstance().SetLanguage(targetLanguage);
                        }

                        bResult = true;
                    }
                    break;
                #endregion </Init EFEM Module Information>

                case EN_INITIALIZATION_STEP.INIT_END:
                    ExitProgressForm();
                    return true;
            }

            //Release 모드이다.
            if (m_bShowProgressWhenAttachedDebuger == System.Diagnostics.Debugger.IsAttached)
            {
                int nInitializeStep = (int)m_enInitializeStep;

                if (0 != nInitializeStep % 2 && nInitializeStep > 2)
                {
                    m_Progress.EnqueueResult(false, ref bResult);
                }
                else
                {
                    m_Progress.EnqueueResult(true, ref strContentsResult);
                }
            }

            ++m_enInitializeStep;

            return false;
        }
        /// <summary>
        /// 2020.05.18 by yjlee [ADD] Check whether the initialization sequence is end or not.
        /// </summary>
        public bool IsInitializationEnd()
        {
            return m_enInitializeStep == EN_INITIALIZATION_STEP.INIT_END
                && null == m_Progress;
        }
        #endregion
    }
}
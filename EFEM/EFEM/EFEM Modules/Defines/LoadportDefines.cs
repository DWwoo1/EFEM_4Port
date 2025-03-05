using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Concurrent;

using EFEM.Defines.Common;
using EFEM.MaterialTracking;

namespace EFEM.Defines.LoadPort
{
    #region <Enumerations>
    #region <E87>
    public enum LoadPortTransferStates
    {
        Unknown,
        OutOfService,
        InService,
        TransferBlocked,
        ReadyToLoad,
        ReadyToUnload
    }

    public enum CarrierIdVerificationStates
    {
        Unknown,
        NotRead,
        WaitingForHost,
        VerificationOk,
        VerificationFailed
    }
    
    public enum CarrierSlotMapVerificationStates
    {
        Unknown,
        NotRead,
        WaitingForHost,
        VerificationOk,
        VerificationFailed
    }

    public enum CarrierAccessStates
    {
        Unknown,
        NotAccessed,
        InAccessed,
        CarrierCompleted,
        CarrierStopped
    }
    public enum LoadPortCarrierAssociationStates
    {
        NotAssociated,
        Associated,
    }

    public enum CarrierSlotMapStates
    {
        Undefined = 0,          // 초기 상태
        Empty,                  // 자재 없음
        NotEmpty,              // 있으나 사용 불가
        CorrectlyOccupied,     // 자재 있음(정상)
        DoubleSlotted,         // 슬롯 중첩(이중 감지)
        CrossSlotted           // 
    }

    public enum LoadPortAccessMode
    {
        Auto = 0,
        Manual
    }
    #endregion </E87>

    #region <Customs>
    public enum LoadPortButtonTypes
    {
        Load = 0,
        Unload,
    }
    public enum LoadPortIndicatorTypes
    {
        Load = 0,
        Unload,
        Auto,
        Manual,
        Reserved1,
        Reserved2,
        Reserved3,
        Reserved4,
    }
    public enum LoadPortIndicatorStates
    {
        Off = 0,
        On,
        Blink
    }
    
    public enum LoadPortActionStates
    {
        Idle,
        Busy,
        Fault,
    }
    public enum LoadPortCommands
    {
        Idle,
        Load,
        Unload,
        Clamp,
        Unclamp,
        Dock,
        Undock,
        DoorOpen,
        DoorClose,
        Hello,
        Initialize,
        Scan,
        ScanDown,
        Reset,
        AmpOn,
        AmpOff,
        GetState,
        GetMap,
        GetCapacity,
        FindLoadingMode,
        ChangeToCassette,
        ChangeToFoup,
        ChangeAccessModeToAuto,
        ChangeAccessModeToManual,
        GetAcceessingMode,
        AMHSLoading,
        AMHSUnloading,

        LedOn,          // 2024.11.14. by dwlim [ADD] SELOP8 LED I/F 추가
        LedOff,         // 2024.11.14. by dwlim [ADD] SELOP8 LED I/F 추가
        LedBlink,       // 2024.11.14. by dwlim [ADD] SELOP8 LED I/F 추가
        LedStatus,      // 2024.11.18. by dwlim [ADD] SELOP8 LED Status 추가
    }
    public enum LoadPortLoadingMode
    {
        Unknown = -1,
        Foup,
        Cassette,
    }

    public enum VarificationResults
    {
        Proceed,
        Completed,
        Error,
    }
    public enum E23InputSignals
    {
        Valid = 0,
        CarrierStage_0,
        CarrierStage_1,
        CarrierStage_2,
        CarrierStage_3,
        TransferRequest,
        Busy,
        Complete,
    }
    public enum E23OutputSignals
    {
        LoadRequest = 0,
        UnloadRequest,
        Abort,
        Ready,
        Spare_1,
        Spare_2,
        Spare_3,
        Spare_4,
    }
    public enum E84InputSignals
    {
        Valid = 0,
        CarrierStage_0,
        CarrierStage_1,
        TransferRequest,
        Busy,
        Complete,
        ContinuousHandoff,
        Spare_1,
    }
    public enum E84OutputSignals
    {
        LoadRequest = 0,
        UnloadRequest,
        Ready,
        HandoffAvailable,
        EmergencyStop,
        Spare_1,
        Spare_2,
        Spare_3,
    }
    #endregion </Customs>

    #endregion </Enumerations>

    #region <Class&Struct>
    public class LoadPortLogger : ModuleLogger
    {
        public LoadPortLogger(string typeOfLog, string name) : base(typeOfLog, name, true) { }

        public void WriteOperationStartLog(LoadPortCommands command)
        {
            WriteLog(LogTitleTypes.OPER, string.Format("----- {0} -----", command.ToString()));
        }
        public void WriteOperationEndLog(LoadPortCommands command, CommandResults result)
        {
            if (result.CommandResult.Equals(CommandResult.Proceed))
                return;

            WriteLog(LogTitleTypes.OPER, string.Format("----- Result : {0}, Description : {1}", result.CommandResult.ToString(), result.Description));
        }
        public void WriteCommLog(string message, bool received)
        {
            if (false == received)
            {
                WriteLog(LogTitleTypes.SEND, message);
            }
            else
            {
                WriteLog(LogTitleTypes.RECV, message);
            }

        }        
        public void WriteSignalChangedLog(string signalName, bool changedValue, bool input)
        {
            string message = string.Format("Signal Changed : {0} -> {1}", signalName, changedValue.ToString());
            if (input)
            {
                WriteLog(LogTitleTypes.IN, message);
            }
            else
            {
                WriteLog(LogTitleTypes.OUT, message);
            }
        }
        public void WriteCarrierStatusChangedLog(string signalName, bool changedValue)
        {
            string message = string.Format("Signal Changed : {0} -> {1}", signalName, changedValue.ToString());
            WriteLog(LogTitleTypes.CARR, message);            
        }
    }
    public class LoadPortStateInformation
    {
        #region <Properties>
        public bool Enabled { get; set; }
        public bool Initialized { get; set; }
        public bool Present { get; set; }
        public bool Placed { get; set; }
        public bool ClampState { get; set; }
        public bool DockState { get; set; }
        public bool DoorState { get; set; }
        public bool PlacementErrorState { get; set; }
        public bool CarrierOutErrorState { get; set; }
        public string TriggeredAlarm { get; set; }
        public LoadPortAccessMode AccessMode { get; set; }
        public LoadPortLoadingMode LoadingType { get; set; }
        public LoadPortCarrierAssociationStates AssociationState { get; set; }
        public LoadPortTransferStates TransferState { get; set; }
        public CarrierAccessStates CarrierAccessingState { get; set; }
        public CarrierIdVerificationStates CarrierIdVerificationStatus { get; set; }
        public CarrierSlotMapVerificationStates CarrierSlotMapVerificationStatus { get; set; }
        #endregion </Properties>

        #region <Methods>
        public void CopyTo(ref LoadPortStateInformation instance)
        {
            if (instance == null)
            {
                instance = new LoadPortStateInformation();
            }

            instance.Initialized = Initialized;
            instance.Present = Present;
            instance.Placed = Placed;
            instance.ClampState = ClampState;
            instance.DockState = DockState;
            instance.DoorState = DoorState;
            instance.AccessMode = AccessMode;
            instance.LoadingType = LoadingType;
            instance.AssociationState = AssociationState;
            instance.TransferState = TransferState;
            instance.PlacementErrorState = PlacementErrorState;
            instance.CarrierOutErrorState = CarrierOutErrorState;
            instance.TriggeredAlarm = TriggeredAlarm;
            instance.CarrierAccessingState = CarrierAccessingState;
            instance.CarrierIdVerificationStatus = CarrierIdVerificationStatus;
            instance.CarrierSlotMapVerificationStatus = CarrierSlotMapVerificationStatus;
        }
        #endregion </Methods>
    }
    public class AMHSInformation
    {
        public AMHSInformation(Define.DefineEnumProject.AppConfig.EN_PIO_INTERFACE_TYPE interfaceType, int interlockIndex,
            Dictionary<int, Tuple<int, string>> digitalInputs, Dictionary<int, Tuple<int, string>> digitalOutputs)
        {
            InterfaceType = interfaceType;

            SaftyInterLockIndex = interlockIndex;

            if (digitalInputs != null)
            {
                DigitalInputs = new ReadOnlyDictionary<int, Tuple<int, string>>(digitalInputs);
            }

            if (digitalOutputs != null)
            {
                DigitalOutputs = new ReadOnlyDictionary<int, Tuple<int, string>>(digitalOutputs);
            }
        }

        public readonly Define.DefineEnumProject.AppConfig.EN_PIO_INTERFACE_TYPE InterfaceType;
        
        public readonly int SaftyInterLockIndex;
        public readonly ReadOnlyDictionary<int, Tuple<int, string>> DigitalInputs = null;
        public readonly ReadOnlyDictionary<int, Tuple<int, string>> DigitalOutputs = null;
    }
    public abstract class AutomatedMaterialHandlingSystemController
    {
        public AutomatedMaterialHandlingSystemController(int lpIndex, AMHSInformation information)
        {
            Index = lpIndex;
            Information = information;

            InputSignalValues = new ConcurrentDictionary<int, bool>();
            InputSignalNames = new Dictionary<int, string>();
            if (Information.DigitalInputs != null)
            {
                foreach (var item in Information.DigitalInputs)
                {
                    int index = item.Value.Item1;
                    InputSignalValues[index] = false;
                    InputSignalNames[index] = item.Value.Item2;
                }
            }

            OutputSignalValues = new ConcurrentDictionary<int, bool>();
            OutputSignalNames = new Dictionary<int, string>();
            if (Information.DigitalOutputs != null)
            {
                foreach (var item in Information.DigitalOutputs)
                {
                    int index = item.Value.Item1;
                    OutputSignalValues[index] = false;
                    OutputSignalNames[index] = item.Value.Item2;
                }
            }

            SaftyInterLockIndex = Information.SaftyInterLockIndex;
            EmergencyStopIndex = GetEmergencyStopSignalIndex();
            
            _taskOperator = FrameOfSystem3.Task.TaskOperator.GetInstance();

            _status = new LoadPortStateInformation();

            InputSignalValuesForSimulation = new ConcurrentDictionary<int, bool>(InputSignalValues);
            OutputSignalValuesForSimulation = new ConcurrentDictionary<int, bool>(OutputSignalValues);
        }

        #region <Fields>
        protected Func<int, CommandResults> actionBeforeCarrierLoad = null;
        protected Func<int, CommandResults> actionBeforeCarrierUnload = null;
        protected Func<int, LoadPortLoadingMode, CommandResults> modeChangeBeforeCarrierLoad = null;

        private Func<int, bool> readInput = null;
        private Func<int, bool> readOutput = null;
        private Func<int, bool, DigitalIO_.DIO_RESULT> _writeOutput = null;
        private readonly int SaftyInterLockIndex;
        private readonly int EmergencyStopIndex;
        private static FrameOfSystem3.Task.TaskOperator _taskOperator = null;

        protected readonly int Index;
        protected int _seqNum = 0;
        protected CommandResults _commandResult;

        private readonly TickCounter_.TickCounter TimerOverTicks = new TickCounter_.TickCounter();
        private readonly TickCounter_.TickCounter TimerOverTicksForPresence = new TickCounter_.TickCounter();

        protected readonly ConcurrentDictionary<int, bool> InputSignalValues = null;        // Key : SignalIndex, Value : Signal Value
        protected readonly ConcurrentDictionary<int, bool> OutputSignalValues = null;       // Key : SignalIndex, Value : Signal Value

        protected readonly AMHSInformation Information = null;

        protected LoadPortStateInformation _status;
        protected LoadPortLogger _logger;

        private bool _temporaryReadInputValue = false;
        private bool _temporaryReadOutputValue = false;
        private readonly Dictionary<int, string> InputSignalNames = null;
        private readonly Dictionary<int, string> OutputSignalNames = null;

        private readonly ConcurrentDictionary<int, bool> InputSignalValuesForSimulation = null;        // Key : SignalIndex, Value : Signal Value
        private readonly ConcurrentDictionary<int, bool> OutputSignalValuesForSimulation = null;       // Key : SignalIndex, Value : Signal Value
        private const string CarrierPresence = "CarrierPresence";
        private const string CarrierPlacement = "CarrierPlacement";
        private bool _carrierPresenceStatus;
        private bool _carrierPlacementStatus;
        #endregion </Fields>

        #region <Properites>
        public Define.DefineEnumProject.AppConfig.EN_PIO_INTERFACE_TYPE InterfaceType
        {
            get
            {
                if (Information == null)
                    return Define.DefineEnumProject.AppConfig.EN_PIO_INTERFACE_TYPE.E84;

                return Information.InterfaceType;
            }
        }
        public int IndexOfEmergencyStopSignal
        {
            get
            {
                if (Information == null)
                    return -1;

                return EmergencyStopIndex;
            }
        }
        public int PortId { get; set; }
        #endregion </Properites>

        #region <Methods>

        #region <Assigns>
        public void AssignSignalControlFunctions(
            Func<int, bool> functionToReadInput,
            Func<int, bool> functionToReadOutput,
            Func<int, bool, DigitalIO_.DIO_RESULT> functionToWriteOutput,
            ref LoadPortLogger lpLogger)
        {
            readInput = functionToReadInput;
            readOutput = functionToReadOutput;
            _writeOutput = functionToWriteOutput;
            _logger = lpLogger;
        }
        public void AssignActionBeforeCarrierLoad(Func<int, CommandResults> action)
        {
            actionBeforeCarrierLoad = action;
        }
        public void AssignActionBeforeCarrierUnload(Func<int, CommandResults> action)
        {
            actionBeforeCarrierUnload = action;
        }
        public void AssignActionModeChangeBeforeCarrierLoad(Func<int, LoadPortLoadingMode, CommandResults> action)
        {
            modeChangeBeforeCarrierLoad = action;
        }
        #endregion </Assigns>

        #region <TaskOperator>
        protected bool IsFinishingMode()
        {
            return _taskOperator.IsFinishingMode();
        }
        protected bool IsSimulationMode()
        {
            return _taskOperator.IsSimulationMode();
        }
        #endregion </TaskOperator>

        #region <IO Control>        
        public void GetSignalInformation(ref AMHSInformation information)
        {
            information = Information;
        }
        public void GetSignalValues(ref Dictionary<int, bool> inputs, ref Dictionary<int, bool> outputs)
        {
            inputs = InputSignalValues.ToDictionary(item => item.Key, item => item.Value);
            outputs = OutputSignalValues.ToDictionary(item => item.Key, item => item.Value);
        }
        public bool IsInterLockDetected()
        {
            if (readInput == null)
                return true;

            return readInput(SaftyInterLockIndex);
        }
        public void ExecuteGatheringSignals(LoadPortStateInformation status)
        {
            _status = status;

            if (readInput != null)
            {
                foreach (var item in InputSignalValues)
                {
                    int index = item.Key;
                    if (false == IsSimulationMode())
                    {
                        _temporaryReadInputValue = readInput(index);
                    }
                    else
                    {
                        _temporaryReadInputValue = InputSignalValuesForSimulation[index];
                    }

                    if (InputSignalValues[index] != _temporaryReadInputValue)
                    {
                        InputSignalValues[index] = _temporaryReadInputValue;

                        _logger.WriteSignalChangedLog(InputSignalNames[index], _temporaryReadInputValue, true);
                    }
                    
                }
            }

            if (readOutput != null)
            {
                foreach (var item in OutputSignalValues)
                {
                    int index = item.Key;
                    if (false == IsSimulationMode())
                    {
                        _temporaryReadOutputValue = readOutput(index);
                    }
                    else
                    {
                        _temporaryReadOutputValue = OutputSignalValuesForSimulation[index];
                    }

                    if (OutputSignalValues[index] != _temporaryReadOutputValue)
                    {
                        OutputSignalValues[index] = _temporaryReadOutputValue;
                            
                        _logger.WriteSignalChangedLog(OutputSignalNames[index], _temporaryReadOutputValue, false);
                    }                    
                }
            }

            if (_status != null)
            {
                if (_carrierPresenceStatus != _status.Present)
                {
                    _carrierPresenceStatus = _status.Present;
                    _logger.WriteCarrierStatusChangedLog(CarrierPresence, _carrierPresenceStatus);
                }

                if (_carrierPlacementStatus != _status.Placed)
                {
                    _carrierPlacementStatus = _status.Placed;
                    _logger.WriteCarrierStatusChangedLog(CarrierPlacement, _carrierPlacementStatus);
                }

            }
        }
        protected bool ReadInput(int index, bool defaultSignal)
        {
            if (_taskOperator.IsSimulationMode())
            {
                InputSignalValuesForSimulation[index] = defaultSignal;
            }

            return InputSignalValues[index];
        }
        public bool WriteOutput(int index, bool newValue)
        {
            if (_taskOperator.IsSimulationMode())
            {
                OutputSignalValuesForSimulation[index] = newValue;
                return true;
            }

            //OutputSignalValues[index] = newValue;
            return _writeOutput(index, newValue).Equals(DigitalIO_.DIO_RESULT.OK);
        }
        #endregion </IO Control>

        #region <Timer>
        protected void SetTickCountForPresence(uint ticks)
        {
            TimerOverTicksForPresence.SetTickCount(ticks);
        }
        protected bool IsTickOverForPresence()
        {
            return TimerOverTicksForPresence.IsTickOver(true);
        }
        protected void SetTickCount(uint ticks)
        {
            TimerOverTicks.SetTickCount(ticks);
        }
        protected bool IsTickOver()
        {
            return TimerOverTicks.IsTickOver(true);
        }
        #endregion </Timer>

        #region <Seq>
        protected CommandResults ReturnResultGoodOrNg(LoadPortCommands command, CommandResult commandResult, string description)
        {
            InitializeSignals();

            _commandResult.ActionName = command.ToString();
            _commandResult.CommandResult = commandResult;
            _commandResult.Description = description;

            if (IsSimulationMode())
            {
                foreach (var item in InputSignalValuesForSimulation)
                {
                    InputSignalValuesForSimulation[item.Key] = false;
                }
            }

            return _commandResult;
        }
        protected CommandResults ExecuteActionBeforeLoad(int lpIndex, LoadPortCommands command)
        {
            if (actionBeforeCarrierLoad == null)
                return new CommandResults(command.ToString(), CommandResult.Completed);

            return actionBeforeCarrierLoad(lpIndex);
        }
        protected CommandResults ExecuteActionBeforeUnload(int lpIndex, LoadPortCommands command)
        {
            if (actionBeforeCarrierUnload == null)
                return new CommandResults(command.ToString(), CommandResult.Completed);

            return actionBeforeCarrierUnload(lpIndex);
        }
        protected CommandResults ExecuteModeChangeAction(int lpIndex, LoadPortLoadingMode mode, LoadPortCommands command)
        {
            if (mode.Equals(LoadPortLoadingMode.Unknown))
                return new CommandResults(command.ToString(), CommandResult.Completed);

            if (modeChangeBeforeCarrierLoad == null)
                return new CommandResults(command.ToString(), CommandResult.Completed);

            return modeChangeBeforeCarrierLoad(lpIndex, mode);
        }
        #endregion </Seq>

        #region <Abstracts>
        public abstract void InitializeSignals();
        public abstract CommandResults ExecuteHandlingToLoad(LoadPortCommands command);
        public abstract CommandResults ExecuteHandlingToUnload(LoadPortCommands command);
        public abstract int GetEmergencyStopSignalIndex();
        public virtual LoadPortLoadingMode CheckTriggerLoadingMode()
        {
            return LoadPortLoadingMode.Unknown;
        }
        protected bool GetTriggerCarrierPresence(LoadPortCommands command)
        {
            if (_status == null)
                return false;
            
            switch (command)
            {
                case LoadPortCommands.AMHSLoading:
                    {
                        if (IsSimulationMode())
                        {
                            _taskOperator.TriggerLoadPortPlacedForSimul(PortId);
                        }

                        return (_carrierPresenceStatus && _carrierPlacementStatus);
                    }
                    
                case LoadPortCommands.AMHSUnloading:
                    {
                        if (IsSimulationMode())
                        {
                            _taskOperator.TriggerLoadPortRemovedForSimul(PortId);
                        }

                        return (false == _carrierPresenceStatus && false == _carrierPlacementStatus);
                    }

                default:
                    return false;
            }
        }
        #endregion </Abstracts>

        #endregion </Methods>
    }

    /*
     * - ABORT는 NORMAL OFF 이며, 에러 발생 시 ON
     * - ERROR 초기화 시 신호 RESET
     * - VALID 신호 OFF 시 신호 RESET
     * 
     * 1. OHT의 [VALID], [CS0~3] ON 신호를 보고 설비의 [L_REQ]/[U_REQ] ON
     * 2. OHT는 설비의 [L_REQ][U_REQ] ON 신호를 보고 [TR_REQ] ON (OHT [TR_REQ] 감시 timeout parameter 필요)
     * 3. OHT의 [TR_REQ] ON 신호를 보고 [READY] ON
     * 4. OHT는 설비의 [READY] ON 신호를 보고 [BUSY] ON (OHT [BUSY] 감시 timeout parameter 필요)
     * 5. 설비는 자재 감지/미감지되면 각각 [L_REQ]/[U_REQ] OFF (설비의 [READY] ON 이후 자재 감지까지의 timeout parameter 필요)
     * 6. OHT는 전송 완료 시 [BUSY] OFF
     * 7. OHT는 [BUSY] OFF 후 [COMPT] ON
     * 8. OHT는 [COMPT] ON 후 [TR_REQ] OFF
     * 9. 설비는 OHT [COMPT] ON, [TR_REQ] OFF 확인 후 [READY] OFF
     * 10. OHT는 설비의 [READY] OFF 확인 후 [COMPT], [CS0~3], [VALID] 전부 OFF (설비 timeout parameter 필요)
     *
     */
    public class E23Handler : AutomatedMaterialHandlingSystemController
    {
        #region <Constructors>
        public E23Handler(int lpIndex,
            int saftyInterLockIndex,
            Dictionary<int, Tuple<int, string>> inputs,
            Dictionary<int, Tuple<int, string>> outputs)
            : base(lpIndex,
                  new AMHSInformation(Define.DefineEnumProject.AppConfig.EN_PIO_INTERFACE_TYPE.E23,
                  saftyInterLockIndex, inputs, outputs))
        {
            var input = new Dictionary<E23InputSignals, int>();
            foreach (var item in inputs)
            {
                if (false == Enum.TryParse(item.Value.Item2, out E23InputSignals inputEnums))
                    continue;

                input[inputEnums] = item.Value.Item1;
            }

            var output = new Dictionary<E23OutputSignals, int>();
            foreach (var item in outputs)
            {
                if (false == Enum.TryParse(item.Value.Item2, out E23OutputSignals outputEnums))
                    continue;

                output[outputEnums] = item.Value.Item1;
            }

            InputSignals = new ReadOnlyDictionary<E23InputSignals, int>(input);
            OutputSignals = new ReadOnlyDictionary<E23OutputSignals, int>(output);
        }
        #endregion </Constructors>

        #region <Fields>
        private readonly ReadOnlyDictionary<E23InputSignals, int> InputSignals = null;
        private readonly ReadOnlyDictionary<E23OutputSignals, int> OutputSignals = null;
        #endregion </Fields>

        #region <Types>
        private enum Timers
        {
            Outputs,
            Long,
            T1,     // L,UL Req ~ TR_REQ ON 까지(3sec)
            T3,     // READY ON ~ BUSY ON 까지(3sec)
            T6      // READY OFF ~ COMP OFF 까지(3sec)
        }
        #endregion </Types>

        #region <Methods>

        #region <Overrides>
        public override void InitializeSignals()
        {
            _seqNum = 0;
            _commandResult = new CommandResults("", CommandResult.Proceed);
            foreach (var item in OutputSignals)
            {
                WriteOutput(item.Value, false);
            }
        }
        public override CommandResults ExecuteHandlingToLoad(LoadPortCommands command)
        {
            return ExecuteHandling(command);
        }
        public override CommandResults ExecuteHandlingToUnload(LoadPortCommands command)
        {
            return ExecuteHandling(command);
        }
        public override int GetEmergencyStopSignalIndex()
        {
            foreach (var item in Information.DigitalOutputs)
            {
                if (item.Value.Item2.Equals(E23OutputSignals.Abort.ToString()))
                {
                    return item.Value.Item1;
                }
            }

            return -1;
        }
        #endregion </Overrides>

        #region <Timers>
        private void SetTimer(Timers timer)
        {
            switch (timer)
            {
                case Timers.Outputs:
                    SetTickCount(1000);
                    break;
                case Timers.Long:
                    SetTickCount(10000);
                    break;
                case Timers.T1:
                case Timers.T3:
                case Timers.T6:
                    SetTickCount(3000);
                    break;
                default:
                    break;
            }
        }
        #endregion </Timers>

        #region <Wrapping Interfaces>
        private bool ReadInput(E23InputSignals input, bool defaultSignal)
        {
            if (false == InputSignals.ContainsKey(input))
                return defaultSignal;

            return ReadInput(InputSignals[input], defaultSignal);
        }
        private bool WriteOutput(E23OutputSignals output, bool newSignal)
        {
            if (false == OutputSignals.ContainsKey(output))
                return false;

            return WriteOutput(OutputSignals[output], newSignal);
        }
        private CommandResults ExecuteHandling(LoadPortCommands command)
        {
            switch (_seqNum)
            {
                #region <Case 0~10:OHT의 [VALID], [CS0~3] ON 신호를 보고 설비의 [L_REQ]/[U_REQ] ON>
                case 0:
                    {
                        if (ReadInput(E23InputSignals.Valid, true) && IsAnyCarrierStageSignalsOn())
                        {
                            SetTimer(Timers.Outputs);
                            ++_seqNum;
                        }
                    }
                    break;
                case 1:
                    {
                        if (IsTickOver())
                        {
                            return ReturnResultGoodOrNg(command, CommandResult.Timeout, "Request output signal on timeout");
                        }

                        E23OutputSignals output = command.Equals(LoadPortCommands.AMHSLoading) ?
                                E23OutputSignals.LoadRequest : E23OutputSignals.UnloadRequest;

                        if (false == WriteOutput(output, true))
                            break;

                        _seqNum = 10;
                    }
                    break;
                #endregion </Case 0~10:OHT의 [VALID], [CS0~3] ON 신호를 보고 설비의 [L_REQ]/[U_REQ] ON>

                #region <Case 10~11:OHT는 설비의[L_REQ][U_REQ] ON 신호를 보고[TR_REQ] ON(OHT[TR_REQ] 감시 timeout parameter 필요)>
                case 10:
                    {
                        SetTimer(Timers.T1);     // TR_REQ 감시
                        ++_seqNum;
                    }
                    break;
                case 11:
                    {
                        if (IsTickOver())
                        {
                            return ReturnResultGoodOrNg(command, CommandResult.Timeout, "Transfer Request signal timeout");
                        }

                        if (false == ReadInput(E23InputSignals.TransferRequest, true))
                            break;

                        _seqNum = 20;
                    }
                    break;
                #endregion </Case 10~11:OHT는 설비의[L_REQ][U_REQ] ON 신호를 보고[TR_REQ] ON(OHT[TR_REQ] 감시 timeout parameter 필요)>

                #region <Case 20:Ready 전 액션 실행>
                case 20:
                    {
                        SetTimer(Timers.Long);
                        ++_seqNum;
                    }
                    break;

                case 21:
                    {
                        if (IsTickOver())
                        {
                            return ReturnResultGoodOrNg(command, CommandResult.Timeout, "Action timeout before ready signals on");
                        }

                        switch (command)
                        {
                            case LoadPortCommands.AMHSLoading:
                                {
                                    var result = ExecuteActionBeforeLoad(Index, command);
                                    switch (result.CommandResult)
                                    {
                                        case CommandResult.Proceed:
                                            break;
                                        case CommandResult.Completed:
                                        case CommandResult.Skipped:
                                            _seqNum = 30;
                                            break;

                                        default:
                                            return ReturnResultGoodOrNg(command, CommandResult.Error, "Action has error before ready signals on");
                                    }
                                }
                                break;
                            case LoadPortCommands.AMHSUnloading:
                                {
                                    var result = ExecuteActionBeforeUnload(Index, command);
                                    switch (result.CommandResult)
                                    {
                                        case CommandResult.Proceed:
                                            break;
                                        case CommandResult.Completed:
                                        case CommandResult.Skipped:
                                            _seqNum = 30;
                                            break;

                                        default:
                                            return ReturnResultGoodOrNg(command, CommandResult.Error, "Action has error before ready signals on");
                                    }
                                }
                                break;
                            default:
                                _seqNum = 30;
                                break;
                        }
                    }
                    break;
                #endregion </Case 20:Ready 전 액션 실행>

                #region <Case 30~31:OHT의[TR_REQ] ON 신호를 보고[READY] ON>
                case 30:
                    {
                        SetTimer(Timers.Outputs);
                        ++_seqNum;
                    }
                    break;
                case 31:
                    {
                        if (IsTickOver())
                        {
                            return ReturnResultGoodOrNg(command, CommandResult.Timeout, "Ready output signal timeout");
                        }

                        if (false == WriteOutput(E23OutputSignals.Ready, true))
                            break;

                        SetTickCountForPresence(30000);     // 자재 안착 감시
                        _seqNum = 40;
                    }
                    break;
                #endregion </Case 30~31:OHT의[TR_REQ] ON 신호를 보고[READY] ON>

                #region <Case 40~41:OHT는 설비의 [READY] ON 신호를 보고 [BUSY] ON (OHT [BUSY] 감시 timeout parameter 필요)>
                case 40:
                    {
                        SetTimer(Timers.T3);     // Busy 감시
                        ++_seqNum;
                    }
                    break;
                case 41:
                    {
                        if (IsTickOver())
                        {
                            return ReturnResultGoodOrNg(command, CommandResult.Timeout, "Busy signal timeout");
                        }

                        if (false == ReadInput(E23InputSignals.Busy, true))
                            break;

                        _seqNum = 50;
                    }
                    break;
                #endregion </Case 40~41:OHT는 설비의 [READY] ON 신호를 보고 [BUSY] ON (OHT [BUSY] 감시 timeout parameter 필요)>

                #region <Case 50~52:설비는 자재 감지/미감지되면 각각 [L_REQ]/[U_REQ] OFF (설비의 [READY] ON 이후 자재 감지까지의 timeout parameter 필요)>
                case 50:
                    {
                        if (IsTickOverForPresence())
                        {
                            return ReturnResultGoodOrNg(command, CommandResult.Timeout, "Carrier presence timeout");
                        }

                        if (false == GetTriggerCarrierPresence(command))
                            break;

                        ++_seqNum;
                    }
                    break;
                case 51:
                    {
                        SetTimer(Timers.Outputs);
                        ++_seqNum;
                    }
                    break;
                case 52:
                    {
                        if (IsTickOver())
                        {
                            return ReturnResultGoodOrNg(command, CommandResult.Timeout, "Request output signal off timeout");
                        }

                        E23OutputSignals output = command.Equals(LoadPortCommands.AMHSLoading) ?
                                E23OutputSignals.LoadRequest : E23OutputSignals.UnloadRequest;

                        if (false == WriteOutput(output, false))
                            break;

                        _seqNum = 60;
                    }
                    break;
                #endregion </Case 50~52:설비는 자재 감지/미감지되면 각각 [L_REQ]/[U_REQ] OFF (설비의 [READY] ON 이후 자재 감지까지의 timeout parameter 필요)>

                #region <Case 60~61:OHT는 전송 완료 시 [BUSY] OFF, [COMPT] ON, [TR_REQ] OFF>
                case 60:
                    {
                        // 없는거지만 감시한다.
                        SetTickCount(3000);
                        ++_seqNum;
                    }
                    break;
                case 61:
                    {
                        if (IsTickOver())
                        {
                            return ReturnResultGoodOrNg(command, CommandResult.Timeout, "Handling Completion timeout");
                        }

                        if (false == IsHandlingCompleted())
                            break;

                        _seqNum = 70;
                    }
                    break;
                #endregion </Case 60~61:OHT는 전송 완료 시 [BUSY] OFF, [COMPT] ON, [TR_REQ] OFF>

                #region <Case 70:설비는 OHT [COMPT] ON, [TR_REQ] OFF 확인 후 [READY] OFF>
                case 70:
                    {
                        SetTimer(Timers.Outputs);
                        _seqNum = 80;
                    }
                    break;
                case 71:
                    {
                        if (IsTickOver())
                        {
                            return ReturnResultGoodOrNg(command, CommandResult.Timeout, "Ready output signal off timeout");
                        }

                        if (false == WriteOutput(E23OutputSignals.Ready, false))
                            break;

                        SetTimer(Timers.T6);     // COMP 감시
                        _seqNum = 80;
                    }
                    break;
                #endregion </Case 70:설비는 OHT [COMPT] ON, [TR_REQ] OFF 확인 후 [READY] OFF>

                #region <Case 80:OHT는 설비의 [READY] OFF 확인 후 [COMPT], [CS0~3], [VALID] 전부 OFF (설비 timeout parameter 필요)>
                case 80:
                    {
                        if (IsTickOver())
                        {
                            return ReturnResultGoodOrNg(command, CommandResult.Timeout, "Interface Completion timeout");
                        }

                        if (false == IsInterfaceCompleted())
                            break;

                        return ReturnResultGoodOrNg(command, CommandResult.Completed, string.Empty);
                    }
                #endregion </Case 80:OHT는 설비의 [READY] OFF 확인 후 [COMPT], [CS0~3], [VALID] 전부 OFF (설비 timeout parameter 필요)>

                default:
                    break;
            }

            _commandResult.ActionName = command.ToString();
            _commandResult.CommandResult = CommandResult.Proceed;
            return _commandResult;
        }
        #endregion </Wrapping Interfaces>

        #region <Signal Wrappers>
        private bool IsAnyCarrierStageSignalsOn()
        {
            return ReadInput(E23InputSignals.CarrierStage_0, true)
                || ReadInput(E23InputSignals.CarrierStage_1, true)
                || ReadInput(E23InputSignals.CarrierStage_2, true)
                || ReadInput(E23InputSignals.CarrierStage_3, true);
        }
        private bool IsHandlingCompleted()
        {
            // Busy Off -> Complete On -> TransferRequest Off면 완료
            return (false == ReadInput(E23InputSignals.Busy, false) &&
                false == ReadInput(E23InputSignals.TransferRequest, false) &&
                ReadInput(E23InputSignals.Complete, true));
        }
        private bool IsInterfaceCompleted()
        {
            return (false == ReadInput(E23InputSignals.Complete, false) &&
                false == ReadInput(E23InputSignals.CarrierStage_0, false) &&
                false == ReadInput(E23InputSignals.CarrierStage_1, false) &&
                false == ReadInput(E23InputSignals.CarrierStage_2, false) &&
                false == ReadInput(E23InputSignals.CarrierStage_3, false) &&
                false == ReadInput(E23InputSignals.Valid, false));
        }
        #endregion </Signal Wrappers>

        #endregion </Methods>
    }
    public class CustomizedE23 : AutomatedMaterialHandlingSystemController
    {
        #region <Constructors>
        public CustomizedE23(int lpIndex,
            int saftyInterLockIndex,
            Dictionary<int, Tuple<int, string>> inputs,
            Dictionary<int, Tuple<int, string>> outputs)
            : base(lpIndex,
                  new AMHSInformation(Define.DefineEnumProject.AppConfig.EN_PIO_INTERFACE_TYPE.E23,
                  saftyInterLockIndex, inputs, outputs))
        {
            var input = new Dictionary<E23InputSignals, int>();
            foreach (var item in inputs)
            {
                if (false == Enum.TryParse(item.Value.Item2, out E23InputSignals inputEnums))
                    continue;

                input[inputEnums] = item.Value.Item1;
            }

            var output = new Dictionary<E23OutputSignals, int>();
            foreach (var item in outputs)
            {
                if (false == Enum.TryParse(item.Value.Item2, out E23OutputSignals outputEnums))
                    continue;

                output[outputEnums] = item.Value.Item1;
            }
            
            InputSignals = new ReadOnlyDictionary<E23InputSignals, int>(input);
            OutputSignals = new ReadOnlyDictionary<E23OutputSignals, int>(output);
        }
        #endregion </Constructors>

        #region <Fields>
        private readonly ReadOnlyDictionary<E23InputSignals, int> InputSignals = null;
        private readonly ReadOnlyDictionary<E23OutputSignals, int> OutputSignals = null;
        #endregion </Fields>

        #region <Types>
        private enum Timers
        {
            Outputs,
            Long,
            T1,     // L,UL Req ~ TR_REQ ON 까지(3sec)
            T3,     // READY ON ~ BUSY ON 까지(3sec)
            T6      // READY OFF ~ COMP OFF 까지(3sec)
        }
        #endregion </Types>

        #region <Methods>
        
        #region <Overrides>
        public override void InitializeSignals()
        {
            _seqNum = 0;
            _commandResult = new CommandResults("", CommandResult.Proceed);
            foreach (var item in OutputSignals)
            {
                WriteOutput(item.Value, false);
            }
        }
        public override CommandResults ExecuteHandlingToLoad(LoadPortCommands command)
        {
            return ExecuteHandling(command);
        }
        public override CommandResults ExecuteHandlingToUnload(LoadPortCommands command)
        {
            return ExecuteHandling(command);
        }
        public override int GetEmergencyStopSignalIndex()
        {
            foreach (var item in Information.DigitalOutputs)
            {
                if (item.Value.Item2.Equals(E23OutputSignals.Abort.ToString()))
                {
                    return item.Value.Item1;
                }
            }

            return -1;
        }
        public override LoadPortLoadingMode CheckTriggerLoadingMode()
        {
            bool cs0 = ReadInput(E23InputSignals.CarrierStage_0, false);
            bool cs1 = ReadInput(E23InputSignals.CarrierStage_1, true);
            if (cs0 && false == cs1)
                return LoadPortLoadingMode.Foup;
            else if (cs1 && false == cs0)
                return LoadPortLoadingMode.Cassette;
            else 
                return LoadPortLoadingMode.Unknown;

        }
        #endregion </Overrides>

        #region <Timers>
        private void SetTimer(Timers timer)
        {
            switch (timer)
            {
                case Timers.Outputs:
                    SetTickCount(1000);
                    break;
                case Timers.Long:
                    SetTickCount(10000);
                    break;
                case Timers.T1:
                case Timers.T3:
                case Timers.T6:
                    SetTickCount(10000);
                    break;
                default:
                    break;
            }
        }
        #endregion </Timers>

        #region <Wrapping Interfaces>
        private bool ReadInput(E23InputSignals input, bool defaultSignal)
        {
            if (false == InputSignals.ContainsKey(input))
                return defaultSignal;

            return ReadInput(InputSignals[input], defaultSignal);
        }
        private bool WriteOutput(E23OutputSignals output, bool newSignal)
        {
            if (false == OutputSignals.ContainsKey(output))
                return false;

            return WriteOutput(OutputSignals[output], newSignal);
        }
        private CommandResults ExecuteHandling(LoadPortCommands command)
        {            
            switch (_seqNum)
            {
                #region <Case 0~10:OHT의 [VALID], [CS0~3] ON 신호를 보고 설비의 [L_REQ]/[U_REQ] ON>
                case 0:
                    {
                        if (IsFinishingMode())
                        {
                            return ReturnResultGoodOrNg(command, CommandResult.Skipped, "Stopping Requested");
                        }

                        if (ReadInput(E23InputSignals.Valid, true) && IsAnyCarrierStageSignalsOn())
                        {
                            SetTimer(Timers.Outputs);
                            ++_seqNum;
                        }
                    }
                    break;
                case 1:
                    {
                        if (IsTickOver())
                        {
                            return ReturnResultGoodOrNg(command, CommandResult.Timeout, "Request output signal on timeout");
                        }

                        E23OutputSignals output = command.Equals(LoadPortCommands.AMHSLoading) ?
                                E23OutputSignals.LoadRequest : E23OutputSignals.UnloadRequest;

                        if (false == WriteOutput(output, true))
                            break;

                        _seqNum = 10;
                    }
                    break;
                #endregion </Case 0~10:OHT의 [VALID], [CS0~3] ON 신호를 보고 설비의 [L_REQ]/[U_REQ] ON>

                #region <Case 10~11:OHT는 설비의[L_REQ][U_REQ] ON 신호를 보고[TR_REQ] ON(OHT[TR_REQ] 감시 timeout parameter 필요)>
                case 10:
                    {
                        SetTimer(Timers.T1);     // TR_REQ 감시
                        ++_seqNum;
                    }
                    break;
                case 11:
                    {
                        if (IsTickOver())
                        {
                            return ReturnResultGoodOrNg(command, CommandResult.Timeout, "Transfer Request signal timeout");
                        }

                        if (false == ReadInput(E23InputSignals.TransferRequest, true))
                            break;

                        _seqNum = 20;
                    }
                    break;
                #endregion </Case 10~11:OHT는 설비의[L_REQ][U_REQ] ON 신호를 보고[TR_REQ] ON(OHT[TR_REQ] 감시 timeout parameter 필요)>

                #region <Case 20:Ready 전 액션 실행>
                case 20:
                    {
                        SetTimer(Timers.Long);
                        ++_seqNum;
                    }
                    break;
                
                case 21:
                    { 
                        if (IsTickOver())
                        {
                            return ReturnResultGoodOrNg(command, CommandResult.Timeout, "Action timeout before ready signals on");
                        }

                        switch (command)
                        {
                            case LoadPortCommands.AMHSLoading:
                                {
                                    
                                    var result = ExecuteActionBeforeLoad(Index, command);
                                    switch (result.CommandResult)
                                    {
                                        case CommandResult.Proceed:
                                            break;
                                        case CommandResult.Completed:
                                        case CommandResult.Skipped:
                                            _seqNum = 25;
                                            break;
                                        case CommandResult.Timeout:
                                        case CommandResult.Error:
                                        case CommandResult.Invalid:
                                            return ReturnResultGoodOrNg(command, CommandResult.Error, "Action has error before ready signals on");

                                        default:
                                            return ReturnResultGoodOrNg(command, CommandResult.Error, "Action has error before ready signals on");
                                    }
                                }
                                break;
                            case LoadPortCommands.AMHSUnloading:
                                {
                                    var result = ExecuteActionBeforeUnload(Index, command);
                                    switch (result.CommandResult)
                                    {
                                        case CommandResult.Proceed:
                                            break;
                                        case CommandResult.Completed:
                                        case CommandResult.Skipped:
                                            _seqNum = 25;
                                            break;
                                        case CommandResult.Timeout:
                                        case CommandResult.Error:
                                        case CommandResult.Invalid:
                                            return ReturnResultGoodOrNg(command, CommandResult.Error, "Action has error before ready signals on");

                                        default:
                                            return ReturnResultGoodOrNg(command, CommandResult.Error, "Action has error before ready signals on");
                                    }
                                }
                                break;
                            default:
                                _seqNum = 25;
                                break;
                        }
                    }
                    break;

                case 25:
                    {
                        SetTimer(Timers.Long);
                        ++_seqNum;
                    }
                    break;
                case 26:
                    {
                        if (IsTickOver())
                        {
                            return ReturnResultGoodOrNg(command, CommandResult.Timeout, "Changing Mode Action timeout");
                        }

                        var mode = CheckTriggerLoadingMode();
                        var result = ExecuteModeChangeAction(Index, mode, command);
                        switch (result.CommandResult)
                        {
                            case CommandResult.Proceed:
                                break;
                            case CommandResult.Completed:
                            case CommandResult.Skipped:
                                _seqNum = 30;
                                break;
                            case CommandResult.Timeout:
                            case CommandResult.Error:
                            case CommandResult.Invalid:
                                return ReturnResultGoodOrNg(command, CommandResult.Error, "Changing Mode Action error");

                            default:
                                _seqNum = 30;
                                break;
                        }
                    }
                    break;
                #endregion </Case 20:Ready 전 액션 실행>

                #region <Case 30~31:OHT의[TR_REQ] ON 신호를 보고[READY] ON>
                case 30:
                    {
                        SetTimer(Timers.Outputs);
                        ++_seqNum;
                    }
                    break;
                case 31:
                    {
                        if (IsTickOver())
                        {
                            return ReturnResultGoodOrNg(command, CommandResult.Timeout, "Ready output signal timeout");
                        }

                        if (false == WriteOutput(E23OutputSignals.Ready, true))
                            break;

                        SetTickCountForPresence(30000);     // 자재 안착 감시
                        _seqNum = 40;
                    }
                    break;
                #endregion </Case 30~31:OHT의[TR_REQ] ON 신호를 보고[READY] ON>

                #region <Case 40~41:OHT는 설비의 [READY] ON 신호를 보고 [BUSY] ON (OHT [BUSY] 감시 timeout parameter 필요)>
                case 40:
                    {
                        SetTimer(Timers.T3);     // Busy 감시
                        ++_seqNum;
                    }
                    break;
                case 41:
                    {
                        if (IsTickOver())
                        {
                            return ReturnResultGoodOrNg(command, CommandResult.Timeout, "Busy signal timeout");
                        }

                        if (false == ReadInput(E23InputSignals.Busy, true))
                            break;

                        _seqNum = 50;
                    }
                    break;
                #endregion </Case 40~41:OHT는 설비의 [READY] ON 신호를 보고 [BUSY] ON (OHT [BUSY] 감시 timeout parameter 필요)>

                #region <Case 50~52:설비는 자재 감지/미감지되면 각각 [L_REQ]/[U_REQ] OFF (설비의 [READY] ON 이후 자재 감지까지의 timeout parameter 필요)>
                case 50:
                    {
                        if (IsTickOverForPresence())
                        {
                            return ReturnResultGoodOrNg(command, CommandResult.Timeout, "Carrier presence timeout");
                        }

                        if (false == GetTriggerCarrierPresence(command))
                            break;

                        ++_seqNum;
                    }
                    break;
                case 51:
                    {
                        SetTimer(Timers.Outputs);
                        ++_seqNum;
                    }
                    break;
                case 52:
                    {
                        if (IsTickOver())
                        {
                            return ReturnResultGoodOrNg(command, CommandResult.Timeout, "Request output signal off timeout");
                        }

                        E23OutputSignals output = command.Equals(LoadPortCommands.AMHSLoading) ?
                                E23OutputSignals.LoadRequest : E23OutputSignals.UnloadRequest;

                        if (false == WriteOutput(output, false))
                            break;

                        _seqNum = 60;
                    }
                    break;
                #endregion </Case 50~52:설비는 자재 감지/미감지되면 각각 [L_REQ]/[U_REQ] OFF (설비의 [READY] ON 이후 자재 감지까지의 timeout parameter 필요)>

                #region <Case 60~61:OHT는 전송 완료 시 [BUSY] OFF, [COMPT] ON, [TR_REQ] OFF>
                case 60:
                    {
                        // 없는거지만 감시한다.
                        SetTickCount(6000);
                        ++_seqNum;
                    }
                    break;
                case 61:
                    {
                        if (IsTickOver())
                        {
                            return ReturnResultGoodOrNg(command, CommandResult.Timeout, "Handling Completion timeout");
                        }

                        if (false == IsHandlingCompleted())
                            break;

                        _seqNum = 70;
                    }
                    break;
                #endregion </Case 60~61:OHT는 전송 완료 시 [BUSY] OFF, [COMPT] ON, [TR_REQ] OFF>

                #region <Case 70:설비는 OHT [COMPT] ON, [TR_REQ] OFF 확인 후 [READY] OFF>
                case 70:
                    {
                        SetTimer(Timers.Outputs);
                        ++_seqNum;
                    }
                    break;
                case 71:
                    {
                        if (IsTickOver())
                        {
                            return ReturnResultGoodOrNg(command, CommandResult.Timeout, "Ready output signal off timeout");
                        }

                        if (false == WriteOutput(E23OutputSignals.Ready, false))
                            break;

                        SetTimer(Timers.T6);     // COMP 감시
                        _seqNum = 80;
                    }
                    break;
                #endregion </Case 70:설비는 OHT [COMPT] ON, [TR_REQ] OFF 확인 후 [READY] OFF>

                #region <Case 80:OHT는 설비의 [READY] OFF 확인 후 [COMPT], [CS0~3], [VALID] 전부 OFF (설비 timeout parameter 필요)>
                case 80:
                    {
                        if (IsTickOver())
                        {
                            return ReturnResultGoodOrNg(command, CommandResult.Timeout, "Interface Completion timeout");
                        }

                        if (false == IsInterfaceCompleted())
                            break;

                        return ReturnResultGoodOrNg(command, CommandResult.Completed, string.Empty);
                    }
                #endregion </Case 80:OHT는 설비의 [READY] OFF 확인 후 [COMPT], [CS0~3], [VALID] 전부 OFF (설비 timeout parameter 필요)>

                default:
                    break;
            }

            _commandResult.ActionName = command.ToString();
            _commandResult.CommandResult = CommandResult.Proceed;
            return _commandResult;
        }
        #endregion </Wrapping Interfaces>

        #region <Signal Wrappers>
        private bool IsAnyCarrierStageSignalsOn()
        {
            return ReadInput(E23InputSignals.CarrierStage_0, true)
                || ReadInput(E23InputSignals.CarrierStage_1, true)
                || ReadInput(E23InputSignals.CarrierStage_2, true)
                || ReadInput(E23InputSignals.CarrierStage_3, true);
        }
        private bool IsHandlingCompleted()
        {
            // Busy Off -> Complete On -> TransferRequest Off면 완료
            return (false == ReadInput(E23InputSignals.Busy, false) &&
                false == ReadInput(E23InputSignals.TransferRequest, false) &&
                ReadInput(E23InputSignals.Complete, true));
        }
        private bool IsInterfaceCompleted()
        {
            return (false == ReadInput(E23InputSignals.Complete, false) &&
                false == ReadInput(E23InputSignals.CarrierStage_0, false) &&
                false == ReadInput(E23InputSignals.CarrierStage_1, false) &&
                false == ReadInput(E23InputSignals.CarrierStage_2, false) &&
                false == ReadInput(E23InputSignals.CarrierStage_3, false) &&
                false == ReadInput(E23InputSignals.Valid, false));
        }
        #endregion </Signal Wrappers>

        #endregion </Methods>
    }

    /*
     * - HO_AVBL은 NORMAL ON 이며, Handoff 불가능할 시 OFF
     * - ES는 NORMAL ON 이며, 에러 발생 시 OFF
     * - ERROR 초기화 시 신호 RESET
     * - VALID 신호 OFF 시 신호 RESET
     * 
     * 1. OHT의 [VALID], [CS0~1] ON 신호를 보고 설비의 [L_REQ]/[U_REQ] ON
     * 2. OHT는 설비의 [L_REQ][U_REQ] ON 신호를 보고 [TR_REQ] ON (OHT [TR_REQ] 감시 timeout parameter 필요)
     * 3. OHT의 [TR_REQ] ON 신호를 보고 [READY] ON
     * 4. OHT는 설비의 [READY] ON 신호를 보고 [BUSY] ON (OHT [BUSY] 감시 timeout parameter 필요)
     * 5. 설비는 자재 감지/미감지되면 각각 [L_REQ]/[U_REQ] OFF (설비의 [READY] ON 이후 자재 감지까지의 timeout parameter 필요)
     * 6. OHT는 설비의 [L_REQ] OFF 신호를 보고 [BUSY] OFF
     * 7. OHT는 [BUSY] OFF 후 [TR_REQ] Off, [COMPT] ON
     * 8. 설비는 OHT의 [COMPT] ON 신호를 보고 [READY] OFF
     * 9. OHT는 설비의 [READY] OFF 확인 후 [VALID], [COMPT], [CS0~1] 전부 OFF (설비 timeout parameter 필요)
     *
     */
    public class E84Handler : AutomatedMaterialHandlingSystemController
    {
        #region <Constructors>
        public E84Handler(int lpIndex,
            int saftyInterLockIndex,
            Dictionary<int, Tuple<int, string>> inputs,
            Dictionary<int, Tuple<int, string>> outputs)
            : base(lpIndex,
                  new AMHSInformation(Define.DefineEnumProject.AppConfig.EN_PIO_INTERFACE_TYPE.E84,
                  saftyInterLockIndex, inputs, outputs))
        {
            var input = new Dictionary<E84InputSignals, int>();
            foreach (var item in inputs)
            {
                if (false == Enum.TryParse(item.Value.Item2, out E84InputSignals inputEnums))
                    continue;

                input[inputEnums] = item.Value.Item1;
            }

            var output = new Dictionary<E84OutputSignals, int>();
            foreach (var item in outputs)
            {
                if (false == Enum.TryParse(item.Value.Item2, out E84OutputSignals outputEnums))
                    continue;

                output[outputEnums] = item.Value.Item1;
            }

            InputSignals = new ReadOnlyDictionary<E84InputSignals, int>(input);
            OutputSignals = new ReadOnlyDictionary<E84OutputSignals, int>(output);
        }
        #endregion </Constructors>

        #region <Fields>
        private readonly ReadOnlyDictionary<E84InputSignals, int> InputSignals = null;
        private readonly ReadOnlyDictionary<E84OutputSignals, int> OutputSignals = null;
        #endregion </Fields>

        #region <Types>
        private enum Timers
        {
            Outputs,
            Long,
            T1,     // L,UL Req ~ TR_REQ ON 까지(2sec)
            T2,     // READY ON ~ BUSY ON 까지(2sec)
            T5      // READY OFF ~ COMP OFF 까지(2sec)
        }
        #endregion </Types>

        #region <Methods>

        #region <Overrides>
        public override void InitializeSignals()
        {
            _seqNum = 0;
            _commandResult = new CommandResults("", CommandResult.Proceed);
            foreach (var item in OutputSignals)
            {
                WriteOutput(item.Value, false);
            }
        }
        public override CommandResults ExecuteHandlingToLoad(LoadPortCommands command)
        {
            return ExecuteHandling(command);
        }
        public override CommandResults ExecuteHandlingToUnload(LoadPortCommands command)
        {
            return ExecuteHandling(command);
        }
        public override int GetEmergencyStopSignalIndex()
        {
            foreach (var item in Information.DigitalOutputs)
            {
                if (item.Value.Item2.Equals(E84OutputSignals.EmergencyStop.ToString()))
                {
                    return item.Value.Item1;
                }
            }

            return -1;
        }
        #endregion </Overrides>

        #region <Timers>
        private void SetTimer(Timers timer)
        {
            switch (timer)
            {
                case Timers.Outputs:
                    SetTickCount(2000);
                    break;
                case Timers.Long:
                    SetTickCount(10000);
                    break;
                case Timers.T1:
                case Timers.T2:
                case Timers.T5:
                    SetTickCount(2000);
                    break;
                default:
                    break;
            }
        }
        #endregion </Timers>

        #region <Wrapping Interfaces>
        private bool ReadInput(E84InputSignals input, bool defaultSignal)
        {
            if (false == InputSignals.ContainsKey(input))
                return defaultSignal;

            return ReadInput(InputSignals[input], defaultSignal);
        }
        private bool WriteOutput(E84OutputSignals output, bool newSignal)
        {
            if (false == OutputSignals.ContainsKey(output))
                return false;

            return WriteOutput(OutputSignals[output], newSignal);
        }
        private CommandResults ExecuteHandling(LoadPortCommands command)
        {           
            switch (_seqNum)
            {
                #region <Case 0~10:OHT의 [VALID], [CS0~1] ON 신호를 보고 설비의 [L_REQ]/[U_REQ] ON>
                case 0:
                    {
                        if (ReadInput(E84InputSignals.Valid, true) && IsAnyCarrierStageSignalsOn())
                        {
                            SetTimer(Timers.Outputs);
                            ++_seqNum;
                        }
                    }
                    break;
                case 1:
                    {
                        if (IsTickOver())
                        {
                            return ReturnResultGoodOrNg(command, CommandResult.Timeout, "Request output signal on timeout");
                        }

                        E84OutputSignals output = command.Equals(LoadPortCommands.AMHSLoading) ?
                                E84OutputSignals.LoadRequest : E84OutputSignals.UnloadRequest;

                        if (false == WriteOutput(output, true))
                            break;

                        _seqNum = 10;
                    }
                    break;
                #endregion </Case 0~10:OHT의 [VALID], [CS0~1] ON 신호를 보고 설비의 [L_REQ]/[U_REQ] ON>

                #region <Case 10~11:OHT는 설비의[L_REQ][U_REQ] ON 신호를 보고[TR_REQ] ON(OHT[TR_REQ] 감시 timeout parameter 필요)>
                case 10:
                    {
                        SetTimer(Timers.T1);     // TR_REQ 감시
                        ++_seqNum;
                    }
                    break;
                case 11:
                    {
                        if (IsTickOver())
                        {
                            return ReturnResultGoodOrNg(command, CommandResult.Timeout, "Transfer Request signal timeout");
                        }

                        if (false == ReadInput(E84InputSignals.TransferRequest, true))
                            break;

                        _seqNum = 20;
                    }
                    break;
                #endregion </Case 10~11:OHT는 설비의[L_REQ][U_REQ] ON 신호를 보고[TR_REQ] ON(OHT[TR_REQ] 감시 timeout parameter 필요)>

                #region <Case 20:Ready 전 액션 실행>
                case 20:
                    {
                        SetTimer(Timers.Long);
                        ++_seqNum;
                    }
                    break;

                case 21:
                    {
                        if (IsTickOver())
                        {
                            return ReturnResultGoodOrNg(command, CommandResult.Timeout, "Action timeout before ready signals on");
                        }

                        switch (command)
                        {
                            case LoadPortCommands.AMHSLoading:
                                {
                                    var result = ExecuteActionBeforeLoad(Index, command);
                                    switch (result.CommandResult)
                                    {
                                        case CommandResult.Proceed:
                                            break;
                                        case CommandResult.Completed:
                                        case CommandResult.Skipped:
                                            _seqNum = 30;
                                            break;

                                        default:
                                            return ReturnResultGoodOrNg(command, CommandResult.Error, "Action has error before ready signals on");
                                    }
                                }
                                break;
                            case LoadPortCommands.AMHSUnloading:
                                {
                                    var result = ExecuteActionBeforeUnload(Index, command);
                                    switch (result.CommandResult)
                                    {
                                        case CommandResult.Proceed:
                                            break;
                                        case CommandResult.Completed:
                                        case CommandResult.Skipped:
                                            _seqNum = 30;
                                            break;

                                        default:
                                            return ReturnResultGoodOrNg(command, CommandResult.Error, "Action has error before ready signals on");
                                    }
                                }
                                break;
                            default:
                                _seqNum = 30;
                                break;
                        }
                    }
                    break;
                #endregion </Case 20:Ready 전 액션 실행>

                #region <Case 30~31:OHT의[TR_REQ] ON 신호를 보고[READY] ON>
                case 30:
                    {
                        SetTimer(Timers.Outputs);
                        ++_seqNum;
                    }
                    break;
                case 31:
                    {
                        if (IsTickOver())
                        {
                            return ReturnResultGoodOrNg(command, CommandResult.Timeout, "Ready output signal timeout");
                        }

                        if (false == WriteOutput(E84OutputSignals.Ready, true))
                            break;

                        SetTickCountForPresence(30000);     // 자재 안착 감시
                        _seqNum = 40;
                    }
                    break;
                #endregion </Case 30~31:OHT의[TR_REQ] ON 신호를 보고[READY] ON>

                #region <Case 40~41:OHT는 설비의 [READY] ON 신호를 보고 [BUSY] ON (OHT [BUSY] 감시 timeout parameter 필요)>
                case 40:
                    {
                        SetTimer(Timers.T2);     // Busy 감시
                        ++_seqNum;
                    }
                    break;
                case 41:
                    {
                        if (IsTickOver())
                        {
                            return ReturnResultGoodOrNg(command, CommandResult.Timeout, "Busy signal timeout");
                        }

                        if (false == ReadInput(E84InputSignals.Busy, true))
                            break;

                        _seqNum = 50;
                    }
                    break;
                #endregion </Case 40~41:OHT는 설비의 [READY] ON 신호를 보고 [BUSY] ON (OHT [BUSY] 감시 timeout parameter 필요)>

                #region <Case 50~52:설비는 자재 감지/미감지되면 각각 [L_REQ]/[U_REQ] OFF (설비의 [READY] ON 이후 자재 감지까지의 timeout parameter 필요)>
                case 50:
                    {
                        if (IsTickOverForPresence())
                        {
                            return ReturnResultGoodOrNg(command, CommandResult.Timeout, "Carrier presence timeout");
                        }

                        if (false == GetTriggerCarrierPresence(command))
                            break;

                        ++_seqNum;
                    }
                    break;
                case 51:
                    {
                        SetTimer(Timers.Outputs);
                        ++_seqNum;
                    }
                    break;
                case 52:
                    {
                        if (IsTickOver())
                        {
                            return ReturnResultGoodOrNg(command, CommandResult.Timeout, "Request output signal off timeout");
                        }

                        E84OutputSignals output = command.Equals(LoadPortCommands.AMHSLoading) ?
                                E84OutputSignals.LoadRequest : E84OutputSignals.UnloadRequest;

                        if (false == WriteOutput(output, false))
                            break;

                        _seqNum = 60;
                    }
                    break;
                #endregion </Case 50~52:설비는 자재 감지/미감지되면 각각 [L_REQ]/[U_REQ] OFF (설비의 [READY] ON 이후 자재 감지까지의 timeout parameter 필요)>

                #region <Case 60~61:OHT는 전송 완료 시 [BUSY] OFF, [COMPT] ON, [TR_REQ] OFF>
                case 60:
                    {
                        // 없는거지만 감시한다.
                        SetTickCount(3000);
                        ++_seqNum;
                    }
                    break;
                case 61:
                    {
                        if (IsTickOver())
                        {
                            return ReturnResultGoodOrNg(command, CommandResult.Timeout, "Handling Completion timeout");
                        }

                        if (false == IsHandlingCompleted())
                            break;

                        _seqNum = 70;
                    }
                    break;
                #endregion </Case 60~61:OHT는 전송 완료 시 [BUSY] OFF, [COMPT] ON, [TR_REQ] OFF>

                #region <Case 70:설비는 OHT [COMPT] ON 확인 후 [READY] OFF>
                case 70:
                    {
                        SetTimer(Timers.Outputs);
                        _seqNum = 80;
                    }
                    break;
                case 71:
                    {
                        if (IsTickOver())
                        {
                            return ReturnResultGoodOrNg(command, CommandResult.Timeout, "Ready output signal off timeout");
                        }

                        if (false == WriteOutput(E84OutputSignals.Ready, false))
                            break;

                        SetTimer(Timers.T5);     // COMP 감시
                        _seqNum = 80;
                    }
                    break;
                #endregion </Case 70:설비는 OHT [COMPT] ON 확인 후 [READY] OFF>

                #region <Case 80:OHT는 설비의 [READY] OFF 확인 후 [COMPT], [CS0~1], [VALID] 전부 OFF (설비 timeout parameter 필요)>
                case 80:
                    {
                        if (IsTickOver())
                        {
                            return ReturnResultGoodOrNg(command, CommandResult.Timeout, "Interface Completion timeout");
                        }

                        if (false == IsInterfaceCompleted())
                            break;

                        return ReturnResultGoodOrNg(command, CommandResult.Completed, string.Empty);
                    }
                #endregion </Case 80:OHT는 설비의 [READY] OFF 확인 후 [COMPT], [CS0~1], [VALID] 전부 OFF (설비 timeout parameter 필요)>
                default:
                    break;
            }
            _commandResult.ActionName = command.ToString();
            _commandResult.CommandResult = CommandResult.Proceed;
            return _commandResult;
        }
        #endregion </Wrapping Interfaces>

        #region <Signal Wrappers>
        private bool IsAnyCarrierStageSignalsOn()
        {
            return ReadInput(E84InputSignals.CarrierStage_0, true)
                || ReadInput(E84InputSignals.CarrierStage_1, true);
        }
        private bool IsHandlingCompleted()
        {
            // Busy Off -> Complete On -> TransferRequest Off면 완료
            return (false == ReadInput(E84InputSignals.Busy, false) &&
                false == ReadInput(E84InputSignals.TransferRequest, false) &&
                ReadInput(E84InputSignals.Complete, true));
        }
        private bool IsInterfaceCompleted()
        {
            return (false == ReadInput(E84InputSignals.Complete, false) &&
                false == ReadInput(E84InputSignals.CarrierStage_0, false) &&
                false == ReadInput(E84InputSignals.CarrierStage_1, false) &&
                false == ReadInput(E84InputSignals.Valid, false));
        }
        #endregion </Signal Wrappers>

        #endregion </Methods>
    }
    public class CustomizedE84 : AutomatedMaterialHandlingSystemController
    {
        #region <Constructors>
        public CustomizedE84(int lpIndex,
            int saftyInterLockIndex,
            Dictionary<int, Tuple<int, string>> inputs,
            Dictionary<int, Tuple<int, string>> outputs)
            : base(lpIndex,
                  new AMHSInformation(Define.DefineEnumProject.AppConfig.EN_PIO_INTERFACE_TYPE.E84,
                  saftyInterLockIndex, inputs, outputs))
        {
            var input = new Dictionary<E84InputSignals, int>();
            foreach (var item in inputs)
            {
                if (false == Enum.TryParse(item.Value.Item2, out E84InputSignals inputEnums))
                    continue;

                input[inputEnums] = item.Value.Item1;
            }

            var output = new Dictionary<E84OutputSignals, int>();
            foreach (var item in outputs)
            {
                if (false == Enum.TryParse(item.Value.Item2, out E84OutputSignals outputEnums))
                    continue;

                output[outputEnums] = item.Value.Item1;
            }

            InputSignals = new ReadOnlyDictionary<E84InputSignals, int>(input);
            OutputSignals = new ReadOnlyDictionary<E84OutputSignals, int>(output);
        }
        #endregion </Constructors>

        #region <Fields>
        private readonly ReadOnlyDictionary<E84InputSignals, int> InputSignals = null;
        private readonly ReadOnlyDictionary<E84OutputSignals, int> OutputSignals = null;
        #endregion </Fields>

        #region <Types>
        private enum Timers
        {
            Outputs,
            Long,
            T1,     // L,UL Req ~ TR_REQ ON 까지(3sec)
            T2,     // READY ON ~ BUSY ON 까지(3sec)
            T5      // READY OFF ~ COMP OFF 까지(3sec)
        }
        #endregion </Types>

        #region <Methods>

        #region <Overrides>
        public override void InitializeSignals()
        {
            _seqNum = 0;
            _commandResult = new CommandResults("", CommandResult.Proceed);
            foreach (var item in OutputSignals)
            {
                WriteOutput(item.Value, false);
            }
        }
        public override CommandResults ExecuteHandlingToLoad(LoadPortCommands command)
        {
            return ExecuteHandling(command);
        }
        public override CommandResults ExecuteHandlingToUnload(LoadPortCommands command)
        {
            return ExecuteHandling(command);
        }
        public override LoadPortLoadingMode CheckTriggerLoadingMode()
        {
            bool cs0 = ReadInput(E84InputSignals.CarrierStage_0, false);
            bool cs1 = ReadInput(E84InputSignals.CarrierStage_1, true);
            if (cs0 && false == cs1)
                return LoadPortLoadingMode.Foup;
            else if (cs1 && false == cs0)
                return LoadPortLoadingMode.Cassette;
            else
                return LoadPortLoadingMode.Unknown;

        }
        public override int GetEmergencyStopSignalIndex()
        {
            foreach (var item in Information.DigitalOutputs)
            {
                if (item.Value.Item2.Equals(E84OutputSignals.EmergencyStop.ToString()))
                {
                    return item.Value.Item1;
                }
            }

            return -1;
        }
        #endregion </Overrides>

        #region <Timers>
        private void SetTimer(Timers timer)
        {
            switch (timer)
            {
                case Timers.Outputs:
                    SetTickCount(2000);
                    break;
                case Timers.Long:
                    SetTickCount(10000);
                    break;
                case Timers.T1:
                case Timers.T2:
                case Timers.T5:
                    SetTickCount(2000);
                    break;
                default:
                    break;
            }
        }
        #endregion </Timers>

        #region <Wrapping Interfaces>
        private bool ReadInput(E84InputSignals input, bool defaultSignal)
        {
            if (false == InputSignals.ContainsKey(input))
                return defaultSignal;

            return ReadInput(InputSignals[input], defaultSignal);
        }
        private bool WriteOutput(E84OutputSignals output, bool newSignal)
        {
            if (false == OutputSignals.ContainsKey(output))
                return false;

            return WriteOutput(OutputSignals[output], newSignal);
        }
        private CommandResults ExecuteHandling(LoadPortCommands command)
        {
            switch (_seqNum)
            {
                #region <Case 0~10:OHT의 [VALID], [CS0~1] ON 신호를 보고 설비의 [L_REQ]/[U_REQ] ON>
                case 0:
                    {
                        if (IsFinishingMode())
                        {
                            return ReturnResultGoodOrNg(command, CommandResult.Skipped, "Stopping Requested");
                        }
                        if (ReadInput(E84InputSignals.Valid, true) && IsAnyCarrierStageSignalsOn())
                        {
                            SetTimer(Timers.Outputs);
                            ++_seqNum;
                        }
                    }
                    break;
                case 1:
                    {
                        if (IsTickOver())
                        {
                            return ReturnResultGoodOrNg(command, CommandResult.Timeout, "Request output signal on timeout");
                        }

                        E84OutputSignals output = command.Equals(LoadPortCommands.AMHSLoading) ?
                                E84OutputSignals.LoadRequest : E84OutputSignals.UnloadRequest;

                        if (false == WriteOutput(output, true))
                            break;

                        _seqNum = 10;
                    }
                    break;
                #endregion </Case 0~10:OHT의 [VALID], [CS0~1] ON 신호를 보고 설비의 [L_REQ]/[U_REQ] ON>

                #region <Case 10~11:OHT는 설비의[L_REQ][U_REQ] ON 신호를 보고[TR_REQ] ON(OHT[TR_REQ] 감시 timeout parameter 필요)>
                case 10:
                    {
                        SetTimer(Timers.T1);     // TR_REQ 감시
                        ++_seqNum;
                    }
                    break;
                case 11:
                    {
                        if (IsTickOver())
                        {
                            return ReturnResultGoodOrNg(command, CommandResult.Timeout, "Transfer Request signal timeout");
                        }

                        if (false == ReadInput(E84InputSignals.TransferRequest, true))
                            break;

                        _seqNum = 20;
                    }
                    break;
                #endregion </Case 10~11:OHT는 설비의[L_REQ][U_REQ] ON 신호를 보고[TR_REQ] ON(OHT[TR_REQ] 감시 timeout parameter 필요)>

                #region <Case 20:Ready 전 액션 실행>
                case 20:
                    {
                        SetTimer(Timers.Long);
                        ++_seqNum;
                    }
                    break;

                case 21:
                    {
                        if (IsTickOver())
                        {
                            return ReturnResultGoodOrNg(command, CommandResult.Timeout, "Action timeout before ready signals on");
                        }

                        switch (command)
                        {
                            case LoadPortCommands.AMHSLoading:
                                {

                                    var result = ExecuteActionBeforeLoad(Index, command);
                                    switch (result.CommandResult)
                                    {
                                        case CommandResult.Proceed:
                                            break;
                                        case CommandResult.Completed:
                                        case CommandResult.Skipped:
                                            _seqNum = 25;
                                            break;
                                        case CommandResult.Timeout:
                                        case CommandResult.Error:
                                        case CommandResult.Invalid:
                                            return ReturnResultGoodOrNg(command, CommandResult.Error, "Action has error before ready signals on");

                                        default:
                                            return ReturnResultGoodOrNg(command, CommandResult.Error, "Action has error before ready signals on");
                                    }
                                }
                                break;
                            case LoadPortCommands.AMHSUnloading:
                                {
                                    var result = ExecuteActionBeforeUnload(Index, command);
                                    switch (result.CommandResult)
                                    {
                                        case CommandResult.Proceed:
                                            break;
                                        case CommandResult.Completed:
                                        case CommandResult.Skipped:
                                            _seqNum = 25;
                                            break;
                                        case CommandResult.Timeout:
                                        case CommandResult.Error:
                                        case CommandResult.Invalid:
                                            return ReturnResultGoodOrNg(command, CommandResult.Error, "Action has error before ready signals on");

                                        default:
                                            return ReturnResultGoodOrNg(command, CommandResult.Error, "Action has error before ready signals on");
                                    }
                                }
                                break;
                            default:
                                _seqNum = 25;
                                break;
                        }
                    }
                    break;

                case 25:
                    {
                        SetTimer(Timers.Long);
                        ++_seqNum;
                    }
                    break;
                case 26:
                    {
                        if (IsTickOver())
                        {
                            return ReturnResultGoodOrNg(command, CommandResult.Timeout, "Changing Mode Action timeout");
                        }

                        var mode = CheckTriggerLoadingMode();
                        var result = ExecuteModeChangeAction(Index, mode, command);
                        switch (result.CommandResult)
                        {
                            case CommandResult.Proceed:
                                break;
                            case CommandResult.Completed:
                            case CommandResult.Skipped:
                                _seqNum = 30;
                                break;
                            case CommandResult.Timeout:
                            case CommandResult.Error:
                            case CommandResult.Invalid:
                                return ReturnResultGoodOrNg(command, CommandResult.Error, "Changing Mode Action error");

                            default:
                                _seqNum = 30;
                                break;
                        }
                    }
                    break;
                #endregion </Case 20:Ready 전 액션 실행>

                #region <Case 30~31:OHT의[TR_REQ] ON 신호를 보고[READY] ON>
                case 30:
                    {
                        SetTimer(Timers.Outputs);
                        ++_seqNum;
                    }
                    break;
                case 31:
                    {
                        if (IsTickOver())
                        {
                            return ReturnResultGoodOrNg(command, CommandResult.Timeout, "Ready output signal timeout");
                        }

                        if (false == WriteOutput(E84OutputSignals.Ready, true))
                            break;

                        SetTickCountForPresence(30000);     // 자재 안착 감시
                        _seqNum = 40;
                    }
                    break;
                #endregion </Case 30~31:OHT의[TR_REQ] ON 신호를 보고[READY] ON>

                #region <Case 40~41:OHT는 설비의 [READY] ON 신호를 보고 [BUSY] ON (OHT [BUSY] 감시 timeout parameter 필요)>
                case 40:
                    {
                        SetTimer(Timers.T2);     // Busy 감시
                        ++_seqNum;
                    }
                    break;
                case 41:
                    {
                        if (IsTickOver())
                        {
                            return ReturnResultGoodOrNg(command, CommandResult.Timeout, "Busy signal timeout");
                        }

                        if (false == ReadInput(E84InputSignals.Busy, true))
                            break;

                        _seqNum = 50;
                    }
                    break;
                #endregion </Case 40~41:OHT는 설비의 [READY] ON 신호를 보고 [BUSY] ON (OHT [BUSY] 감시 timeout parameter 필요)>

                #region <Case 50~52:설비는 자재 감지/미감지되면 각각 [L_REQ]/[U_REQ] OFF (설비의 [READY] ON 이후 자재 감지까지의 timeout parameter 필요)>
                case 50:
                    {
                        if (IsTickOverForPresence())
                        {
                            return ReturnResultGoodOrNg(command, CommandResult.Timeout, "Carrier presence timeout");
                        }

                        if (false == GetTriggerCarrierPresence(command))
                            break;

                        ++_seqNum;
                    }
                    break;
                case 51:
                    {
                        SetTimer(Timers.Outputs);
                        ++_seqNum;
                    }
                    break;
                case 52:
                    {
                        if (IsTickOver())
                        {
                            return ReturnResultGoodOrNg(command, CommandResult.Timeout, "Request output signal off timeout");
                        }

                        E84OutputSignals output = command.Equals(LoadPortCommands.AMHSLoading) ?
                                E84OutputSignals.LoadRequest : E84OutputSignals.UnloadRequest;

                        if (false == WriteOutput(output, false))
                            break;

                        _seqNum = 60;
                    }
                    break;
                #endregion </Case 50~52:설비는 자재 감지/미감지되면 각각 [L_REQ]/[U_REQ] OFF (설비의 [READY] ON 이후 자재 감지까지의 timeout parameter 필요)>

                #region <Case 60~61:OHT는 전송 완료 시 [BUSY] OFF, [COMPT] ON, [TR_REQ] OFF>
                case 60:
                    {
                        // 없는거지만 감시한다.
                        SetTickCount(6000);
                        ++_seqNum;
                    }
                    break;
                case 61:
                    {
                        if (IsTickOver())
                        {
                            return ReturnResultGoodOrNg(command, CommandResult.Timeout, "Handling Completion timeout");
                        }

                        if (false == IsHandlingCompleted())
                            break;

                        _seqNum = 70;
                    }
                    break;
                #endregion </Case 60~61:OHT는 전송 완료 시 [BUSY] OFF, [COMPT] ON, [TR_REQ] OFF>

                #region <Case 70:설비는 OHT [COMPT] ON 확인 후 [READY] OFF>
                case 70:
                    {
                        SetTimer(Timers.Outputs);
                        ++_seqNum;
                    }
                    break;
                case 71:
                    {
                        if (IsTickOver())
                        {
                            return ReturnResultGoodOrNg(command, CommandResult.Timeout, "Ready output signal off timeout");
                        }

                        if (false == WriteOutput(E84OutputSignals.Ready, false))
                            break;

                        SetTimer(Timers.T5);     // COMP 감시
                        _seqNum = 80;
                    }
                    break;
                #endregion </Case 70:설비는 OHT [COMPT] ON 확인 후 [READY] OFF>

                #region <Case 80:OHT는 설비의 [READY] OFF 확인 후 [COMPT], [CS0~1], [VALID] 전부 OFF (설비 timeout parameter 필요)>
                case 80:
                    {
                        if (IsTickOver())
                        {
                            return ReturnResultGoodOrNg(command, CommandResult.Timeout, "Interface Completion timeout");
                        }

                        if (false == IsInterfaceCompleted())
                            break;

                        return ReturnResultGoodOrNg(command, CommandResult.Completed, string.Empty);
                    }
                #endregion </Case 80:OHT는 설비의 [READY] OFF 확인 후 [COMPT], [CS0~1], [VALID] 전부 OFF (설비 timeout parameter 필요)>

                default:
                    break;
            }

            _commandResult.ActionName = command.ToString();
            _commandResult.CommandResult = CommandResult.Proceed;
            return _commandResult;
        }
        #endregion </Wrapping Interfaces>

        #region <Signal Wrappers>
        private bool IsAnyCarrierStageSignalsOn()
        {
            return ReadInput(E84InputSignals.CarrierStage_0, true)
                || ReadInput(E84InputSignals.CarrierStage_1, true);
        }
        private bool IsHandlingCompleted()
        {
            // Busy Off -> Complete On -> TransferRequest Off면 완료
            return (false == ReadInput(E84InputSignals.Busy, false) &&
                false == ReadInput(E84InputSignals.TransferRequest, false) &&
                ReadInput(E84InputSignals.Complete, true));
        }
        private bool IsInterfaceCompleted()
        {
            return (false == ReadInput(E84InputSignals.Complete, false) &&
                false == ReadInput(E84InputSignals.CarrierStage_0, false) &&
                false == ReadInput(E84InputSignals.CarrierStage_1, false) &&
                false == ReadInput(E84InputSignals.Valid, false));
        }
        #endregion </Signal Wrappers>

        #endregion </Methods>
    }
    #endregion </Class&Struct>

    #region <Delegates>
    public delegate void ButtonPressedEventHandler();
    public delegate void LoadPortModeEventHandler(bool trigger);
    public delegate void SlotMapStateUpdatedEventHandler(int portId, CarrierSlotMapStates[] slotMap);
    #endregion </Delegates>
}
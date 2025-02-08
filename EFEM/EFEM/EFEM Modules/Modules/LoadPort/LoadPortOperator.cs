using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Define.DefineEnumBase.Common;
using EFEM.Defines.Common;
using EFEM.Defines.LoadPort;
using EFEM.MaterialTracking;
using EFEM.MaterialTracking.LocationServer;

namespace EFEM.Modules.LoadPort
{
    public class LoadPortOperator
    {
        #region <Constructors>
        public LoadPortOperator(int portId, string name, LoadPortController controller, AutomatedMaterialHandlingSystemController amhsController, Dictionary<string, string> locationNames)
        {
            PortId = portId;
            Name = name;

            string typeOfLog = string.Format("{0}{1}", BaseLogTypes.LogTypeLoadPort, portId);
            Logger = new LoadPortLogger(typeOfLog, Name);
            State = new LoadPortStateInformation();
            StateTransitionManager = new StateTransitionManager(portId, ref State);

            Controller = controller;
            if (Controller != null)
            {
                Controller.AttachSlotMapStateUpdatedEventHandler(UpdateCarrierSlotMap);
                Controller.AssignLogger(ref Logger);
            }
            

            _carrierServer = CarrierManagementServer.Instance;
            
            _locationServer = LocationServer.Instance;
            LoadPortSlots = new Dictionary<int, LoadPortLocation>();
            
            LoadPortLocations = new Dictionary<LoadPortLoadingMode, string>();
            foreach (var item in locationNames)
            {
                if (Enum.TryParse(item.Key, out LoadPortLoadingMode mode))
                {
                    LoadPortLocations[mode] = item.Value;
                }
            }

            for (int i = 0; i < MaxCapacity; ++i)
            {
                LoadPortSlots[i] = new LoadPortLocation(PortId, i, Name);
            }
            _locationServer.AddLoadPortLocation(PortId, LoadPortLocations, LoadPortSlots);

            _substrateManager = SubstrateManager.Instance;
            _substrateManager.AddLoadPortBuffers(portId, LoadPortSlots.Count);

            AMHSController = amhsController;
        }
        #endregion </Constructors>

        #region <Fields>
        private const int MaxCapacity = 25;
        private readonly StateTransitionManager StateTransitionManager = null;
        private readonly LoadPortController Controller = null;
        private readonly LoadPortStateInformation State = null;
        //private Carrier _carrier = null;
        private LoadPortTransferStates _backupTransferState;

        //private Carrier _myCarrier = null;
        private static CarrierManagementServer _carrierServer = null;
        private readonly LoadPortLogger Logger = null;
        private int _actionStep = 0;

        private static LocationServer _locationServer = null;
        protected readonly Dictionary<int, LoadPortLocation> LoadPortSlots = null;
        private Dictionary<LoadPortLoadingMode, string> LoadPortLocations = null;

        private static SubstrateManager _substrateManager = null;

        private int _seqCheckingPlacementStatus = 0;
        private const uint PlacementTimeOver = 10000;
        private const string PlacementError = "Placement Error";
        private readonly TickCounter_.TickCounter _placementStatusChecker = new TickCounter_.TickCounter();

        private int _seqCheckingCarrierOutStatus = 0;
        private const uint CarrierOutTimeOver = 1000;
        private const string CarrierOutError = "Carrier Out Error";
        private readonly TickCounter_.TickCounter _carrierOutStatusChecker = new TickCounter_.TickCounter();

        private readonly AutomatedMaterialHandlingSystemController AMHSController = null;

        private bool _carrierHasCreated = false;
        private Func<bool> _functionToReadInput = null;
        #endregion </Fields>

        #region <Properties>
        public bool Enabled { get; set; }
        public int PortId { get; private set; }
        public string Name { get; private set; }
        public bool IsConnected
        {
            get
            {
                if (Controller == null)
                    return false;

                return Controller.IsConnected();
            }
        }
        public bool Initialized
        {
            get
            {
                if (State == null)
                    return false;

                return State.Initialized;
            }
        }
        public bool IsLoadPortBusy
        {
            get
            {
                if (_functionToReadInput == null || Controller is LoadPortControllers.LoadPortControllerSimulator)
                {
                    return Controller.State.Equals(LoadPortActionStates.Busy);
                }

                return _functionToReadInput();
            }
        }
        public bool Present
        {
            get
            {
                if (State == null)
                    return false;

                return State.Present;
            }
        }
        public bool Placed
        {
            get
            {
                if (State == null)
                    return false;

                return State.Placed;
            }
        }
        public bool ClampState
        {
            get
            {
                if (State == null)
                    return false;

                return State.ClampState;
            }
        }
        public bool DockState
        {
            get
            {
                if (State == null)
                    return false;

                return State.DockState;
            }
        }
        public bool DoorState
        {
            get
            {
                if (State == null)
                    return false;

                return State.DoorState;
            }
        }
        public bool PlacementErrorState
        {
            get
            {
                if (State == null)
                    return true;

                return State.PlacementErrorState;
            }
        }
        public bool CarrierOutErrorState
        {
            get
            {
                if (State == null)
                    return true;

                return State.CarrierOutErrorState;
            }
        }
        public string TriggeredControllerAlarm
        {
            get
            {
                if (State == null)
                    return string.Empty;

                return State.TriggeredAlarm;
            }
        }
        public LoadPortTransferStates TransferState
        {
            get
            {
                return StateTransitionManager.TransferState;
            }
        }
        public CarrierIdVerificationStates CarrierIdState
        {
            get
            {
                return StateTransitionManager.CarrierIdState;
            }
        }
        public CarrierSlotMapVerificationStates CarrierSlotMapState
        {
            get
            {
                return StateTransitionManager.CarrierSlotMapState;
            }
        }
        //public CarrierSlotMapStates[] SlotState
        //{
        //    get
        //    {
        //        if (_myCarrier == null)
        //            return null;

        //        return _myCarrier.SlotState;
        //    }
        //}
        public LoadPortLoadingMode LoadingType
        {
            get
            {
                if (State == null)
                    return LoadPortLoadingMode.Cassette;

                return State.LoadingType;
            }
        }

        public LoadPortAccessMode AccessMode
        {
            get
            {
                if (State == null)
                    return LoadPortAccessMode.Manual;

                return State.AccessMode;
            }
        }
        #endregion </Properties>

        #region <Methods>

        #region <Event Handler>
        public void AttachModeChangerEventHandler(LoadPortLoadingMode type, LoadPortModeEventHandler eventHandler)
        {
            if (Controller == null)
                return;

            Controller.AttachModeChangerEventHandler(type, eventHandler);
        }
        public void AttachMechanicalButtonEventHandlers(LoadPortButtonTypes type, ButtonPressedEventHandler eventHandler)
        {
            Controller.AttachMechanicalButtonEventHandlers(type, eventHandler);
        }
        public void AttachBusySignalByDigitalInput(Func<bool> functionToReadInput)
        {
            _functionToReadInput = functionToReadInput;
        }
        #endregion </Event Handler>

        #region <Carrier>
        public void RecreateCarrier()
        {
            if (TransferState.Equals(LoadPortTransferStates.TransferBlocked) ||
                TransferState.Equals(LoadPortTransferStates.ReadyToUnload))
            {
                if (_carrierServer.HasCarrier(PortId))
                {
                    _carrierServer.RemoveCarrier(PortId);
                    Controller.RemoveCarrierMap();
                    StateTransitionManager.InitTransferState();
                }
            }
        }

        private void AssignCarrierByTransferState()
        {
            if (false == TransferState.Equals(_backupTransferState))
            {
                switch (TransferState)
                {
                    case LoadPortTransferStates.TransferBlocked:
                        if (false == _carrierServer.HasCarrier(PortId))
                        {
                            _carrierServer.CreateCarrier(PortId, LoadPortSlots);
                            _carrierHasCreated = true;
                        }
                        break;
                    case LoadPortTransferStates.ReadyToLoad:
                        if (_carrierServer.HasCarrier(PortId))
                        {
                            _carrierHasCreated = false;
                            _carrierServer.RemoveCarrier(PortId);
                            Controller.RemoveCarrierMap();
                        }
                        break;

                    default:
                        break;
                }

                _backupTransferState = TransferState;
            }
        }
        #endregion </Carrier>

        #region <Actions>
        public void InitAction()
        {
            _actionStep = 0;
            Controller.InitAction();
        }
        public CommandResults Initialize()
        {
            switch (_actionStep)
            {
                case 0:
                    Logger.WriteOperationStartLog(LoadPortCommands.Initialize);
                    ++_actionStep;
                    break;                    
            }

            var result = Controller.DoInitialize();
            switch (result.CommandResult)
            {
                case CommandResult.Proceed:
                    break;
                default:
                    _actionStep = 0;
                    Logger.WriteOperationEndLog(LoadPortCommands.Initialize, result);
                    break;

            }

            return result;
        }
        public CommandResults Load()
        {
            switch (_actionStep)
            {
                case 0:
                    Logger.WriteOperationStartLog(LoadPortCommands.Load);
                    ++_actionStep;
                    break;
            }

            var result = Controller.DoLoad();
            switch (result.CommandResult)
            {
                case CommandResult.Proceed:
                    break;
                default:
                    _actionStep = 0;
                    Logger.WriteOperationEndLog(LoadPortCommands.Load, result);
                    break;

            }

            return result;
        }
        public CommandResults Unload()
        {
            switch (_actionStep)
            {
                case 0:
                    Logger.WriteOperationStartLog(LoadPortCommands.Unload);
                    ++_actionStep;
                    break;
            }

            var result = Controller.DoUnload();
            switch (result.CommandResult)
            {
                case CommandResult.Proceed:
                    break;
                default:
                    _actionStep = 0;
                    Logger.WriteOperationEndLog(LoadPortCommands.Unload, result);
                    break;

            }

            return result;
        }
        public CommandResults Clamp()
        {
            switch (_actionStep)
            {
                case 0:
                    Logger.WriteOperationStartLog(LoadPortCommands.Clamp);
                    ++_actionStep;
                    break;
            }

            var result = Controller.DoClamp();
            switch (result.CommandResult)
            {
                case CommandResult.Proceed:
                    break;
                default:
                    _actionStep = 0;
                    Logger.WriteOperationEndLog(LoadPortCommands.Clamp, result);
                    break;

            }

            return result;
        }
        public CommandResults UnClamp()
        {
            switch (_actionStep)
            {
                case 0:
                    Logger.WriteOperationStartLog(LoadPortCommands.Unclamp);
                    ++_actionStep;
                    break;
            }

            var result = Controller.DoUnClamp();
            switch (result.CommandResult)
            {
                case CommandResult.Proceed:
                    break;
                default:
                    _actionStep = 0;
                    Logger.WriteOperationEndLog(LoadPortCommands.Unclamp, result);
                    break;

            }

            return result;
        }
        public CommandResults Dock()
        {
            switch (_actionStep)
            {
                case 0:
                    Logger.WriteOperationStartLog(LoadPortCommands.Dock);
                    ++_actionStep;
                    break;
            }

            var result = Controller.DoDock();
            switch (result.CommandResult)
            {
                case CommandResult.Proceed:
                    break;
                default:
                    _actionStep = 0;
                    Logger.WriteOperationEndLog(LoadPortCommands.Dock, result);
                    break;

            }

            return result;
        }
        public CommandResults UnDock()
        {
            switch (_actionStep)
            {
                case 0:
                    Logger.WriteOperationStartLog(LoadPortCommands.Undock);
                    ++_actionStep;
                    break;
            }

            var result = Controller.DoUnDock();
            switch (result.CommandResult)
            {
                case CommandResult.Proceed:
                    break;
                default:
                    _actionStep = 0;
                    Logger.WriteOperationEndLog(LoadPortCommands.Undock, result);
                    break;

            }

            return result;
        }
        public CommandResults OpenDoor()
        {
            switch (_actionStep)
            {
                case 0:
                    Logger.WriteOperationStartLog(LoadPortCommands.DoorOpen);
                    ++_actionStep;
                    break;
            }

            var result = Controller.DoOpenDoor();
            switch (result.CommandResult)
            {
                case CommandResult.Proceed:
                    break;
                default:
                    _actionStep = 0;
                    Logger.WriteOperationEndLog(LoadPortCommands.DoorOpen, result);
                    break;

            }

            return result;
        }
        public CommandResults CloseDoor()
        {
            switch (_actionStep)
            {
                case 0:
                    Logger.WriteOperationStartLog(LoadPortCommands.DoorClose);
                    ++_actionStep;
                    break;
            }

            var result = Controller.DoCloseDoor();
            switch (result.CommandResult)
            {
                case CommandResult.Proceed:
                    break;
                default:
                    _actionStep = 0;
                    Logger.WriteOperationEndLog(LoadPortCommands.DoorClose, result);
                    break;

            }

            return result;
        }
        public CommandResults Scan()
        {
            switch (_actionStep)
            {
                case 0:
                    Logger.WriteOperationStartLog(LoadPortCommands.ScanDown);
                    ++_actionStep;
                    break;
            }

            var result = Controller.DoScan();
            switch (result.CommandResult)
            {
                case CommandResult.Proceed:
                    break;
                default:
                    _actionStep = 0;
                    Logger.WriteOperationEndLog(LoadPortCommands.ScanDown, result);
                    break;

            }

            return result;
        }
        public CommandResults GetSlotMap()
        {
            switch (_actionStep)
            {
                case 0:
                    Logger.WriteOperationStartLog(LoadPortCommands.GetMap);
                    ++_actionStep;
                    break;
            }

            var result = Controller.DoGetSlotMap();
            switch (result.CommandResult)
            {
                case CommandResult.Proceed:
                    break;
                default:
                    _actionStep = 0;
                    Logger.WriteOperationEndLog(LoadPortCommands.GetMap, result);
                    break;

            }

            return result;
        }
        public CommandResults FindCarrierMode()
        {
            LoadPortCommands action = LoadPortCommands.FindLoadingMode;

            switch (_actionStep)
            {
                case 0:
                    {
                        Logger.WriteOperationStartLog(action);
                        ++_actionStep;
                    }
                    break;
            }

            var result = Controller.DoFindLoadingMode();
            switch (result.CommandResult)
            {
                case CommandResult.Proceed:
                    break;
                default:
                    _actionStep = 0;
                    Logger.WriteOperationEndLog(action, result);
                    break;

            }

            return result;
        }
        public CommandResults ChangeCarrierMode(LoadPortLoadingMode mode)
        {
            LoadPortCommands action = mode == LoadPortLoadingMode.Cassette ?
                LoadPortCommands.ChangeToCassette :
                LoadPortCommands.ChangeToFoup;

            switch (_actionStep)
            {
                case 0:
                    {
                        Logger.WriteOperationStartLog(action);
                        ++_actionStep;
                    }
                    break;
            }

            var result = Controller.DoChangeLoadingMode(mode);
            switch (result.CommandResult)
            {
                case CommandResult.Proceed:
                    break;
                default:
                    _actionStep = 0;
                    Logger.WriteOperationEndLog(action, result);
                    break;

            }

            return result;
        }
        public CommandResults ChangeAcceessMode(LoadPortAccessMode mode)
        {
            LoadPortCommands action = mode == LoadPortAccessMode.Auto?
                LoadPortCommands.ChangeAccessModeToAuto :
                LoadPortCommands.ChangeAccessModeToManual;

            switch (_actionStep)
            {
                case 0:
                    Logger.WriteOperationStartLog(action);
                    ++_actionStep;
                    break;
            }

            var result = Controller.DoChangeAccessMode(mode);
            switch (result.CommandResult)
            {
                case CommandResult.Proceed:
                    break;
                default:
                    _actionStep = 0;
                    Logger.WriteOperationEndLog(action, result);
                    break;

            }

            return result;
        }
        public CommandResults ClearAlarm()
        {
            switch (_actionStep)
            {
                case 0:
                    Logger.WriteOperationStartLog(LoadPortCommands.Reset);
                    ++_actionStep;
                    break;
            }

            var result = Controller.DoClearAlarm();
            switch (result.CommandResult)
            {
                case CommandResult.Proceed:
                    break;
                default:
                    _actionStep = 0;
                    Logger.WriteOperationEndLog(LoadPortCommands.Reset, result);
                    break;

            }

            return result;
        }
        public CommandResults AmpControl(bool enabled)
        {
            LoadPortCommands action = enabled ?
                LoadPortCommands.AmpOn :
                LoadPortCommands.AmpOff;

            switch (_actionStep)
            {
                case 0:
                    Logger.WriteOperationStartLog(action);
                    ++_actionStep;
                    break;
            }

            var result = Controller.DoAmpControl(enabled);
            switch (result.CommandResult)
            {
                case CommandResult.Proceed:
                    break;
                default:
                    _actionStep = 0;
                    Logger.WriteOperationEndLog(action, result);
                    break;

            }

            return result;
        }
        #endregion </Actions>

        #region <States>
        public LoadPortStateInformation GetLoadPortState()
        {
            return State;
        }
        private bool IsCurrectlyPlaced(bool placed)
        {
            return ((placed == State.Present) && (placed == State.Placed));
        }
        private bool CanCarrierOut()
        {
            return (false == State.DockState
                && false == State.ClampState
                && false == State.DoorState);
        }
        private bool CheckPlaceStatus()
        {
            if (false == State.Enabled)
                return true;

            switch (_seqCheckingPlacementStatus)
            {
                case 0:
                    {
                        if (State.Present == State.Placed)
                            break;

                        _placementStatusChecker.SetTickCount(10000);
                    }
                    ++_seqCheckingPlacementStatus;
                    break;

                case 1:
                    {
                        if (_placementStatusChecker.IsTickOver(true))
                        {
                            if (State.Present == State.Placed)
                            {
                                _seqCheckingPlacementStatus = 0;
                                break;
                            }
                            
                            return false;
                        }

                            if (State.Present != State.Placed)
                            break;

                        --_seqCheckingPlacementStatus;
                    }
                    break;
            }
            return true;
        }
        private bool CheckCarrierOutStatus()
        {
            if (false == State.Enabled)
                return true;

            switch (_seqCheckingCarrierOutStatus)
            {
                case 0:
                    {
                        if (false == CanCarrierOut())
                        {
                            if (false == State.Present || false == State.Placed)
                            {
                                _carrierOutStatusChecker.SetTickCount(CarrierOutTimeOver);
                                ++_seqCheckingCarrierOutStatus;
                            }
                        }   
                    }
                    break;

                case 1:
                    {
                        if (_carrierOutStatusChecker.IsTickOver(true))
                        {
                            if (State.Present && State.Placed)
                            {
                                _seqCheckingCarrierOutStatus = 0;
                                break;
                            }

                            return false;
                        }

                        if (false == State.Present || false == State.Placed)
                            break;

                        --_seqCheckingCarrierOutStatus;
                    }
                    break;
            }
            return true;
        }
        private void UpdateLoadPortState()
        {
            State.Enabled = Enabled;
            State.Initialized = Controller.Initialized;
            State.Placed = Controller.Placed;
            State.Present = Controller.Present;
            State.ClampState = Controller.ClampState;
            State.DockState = Controller.DockState;
            State.DoorState = Controller.DoorState;
            State.LoadingType = Controller.LoadingType;
            State.TransferState = TransferState;
            State.AccessMode = Controller.AccessMode;

            if (_carrierServer.HasCarrier(PortId))
            {
                State.CarrierAccessingState = _carrierServer.GetCarrierAccessingStatus(PortId);
            }
            else
            {
                State.CarrierAccessingState = CarrierAccessStates.Unknown;
            }

            State.TriggeredAlarm = Controller.GetTriggeredControllerAlarm();
            State.PlacementErrorState = (false == CheckPlaceStatus());
            State.CarrierOutErrorState = (false == CheckCarrierOutStatus());

            //if (State.Enabled != Enabled)
            //{
            //}

            //if (State.Initialized != Controller.Initialized)
            //{
            //}

            //if (State.Placed != Controller.Placed)
            //{
            //}

            //if (State.Present != Controller.Present)
            //{
            //}

            //if (State.ClampState != Controller.ClampState)
            //{
            //}

            //if (State.DockState != Controller.DockState)
            //{
            //}

            //if (State.DoorState != Controller.DoorState)
            //{
            //}

            //if (State.LoadingType != Controller.LoadingType)
            //{
            //}

            //if (State.TransferState != TransferState)
            //{
            //}
        }
        private void UpdateCarrierSlotMap(int portId, CarrierSlotMapStates[] slotMap)
        {
            if (Controller.SlotState == null)
                return;

            if (false == _carrierServer.HasCarrier(PortId))
            {
                return;
            }

            _carrierServer.SetCarrierSlotMap(PortId, Controller.SlotState);
        }
        #endregion </States>

        #region <Gathering>
        public void Execute()
        {
            if (Controller == null || State == null)
                return;

            StateTransitionManager.ExecuteTransition();

            AssignCarrierByTransferState();

            UpdateLoadPortState();

            UpdateAMHSValues();
           
            Controller.Monitoring();

        }
        #endregion </Gathering>

        #region <ETC>
        public string GetCurrentLocationName()
        {
            return LoadPortLocations[LoadingType];
        }
        #endregion </ETC>

        #region <AMHS>
        public bool AssignAMHSSignalControlFunctions(
            Func<int, bool> functionToReadInput,
            Func<int, bool> functionToReadOutput,
            Func<int, bool, DigitalIO_.DIO_RESULT> functionToWriteOutput)
        {
            if (AMHSController == null)
                return false;

            AMHSController.AssignSignalControlFunctions(functionToReadInput, functionToReadOutput, functionToWriteOutput);
            return true;
        }

        public bool AssignActionBeforeCarrierLoads(Func<int, CommandResults> action)
        {
            if (AMHSController == null)
                return false;

            AMHSController.AssignActionBeforeCarrierLoad(action);
            return true;
        }
        public bool GetAMHSSaftyInterLockStatus()
        {
            if (AMHSController == null)
                return false;

            return (false == AMHSController.IsInterLockDetected());
        }
        public bool GetAMHSSignalValues(ref Dictionary<int, bool> inputs, ref Dictionary<int, bool> outputs)
        {
            if (AMHSController == null)
                return false;

            AMHSController.GetSignalValues(ref inputs, ref outputs);
            return true;
        }
        public bool GetAMHSInformation(ref AMHSInformation information)
        {
            if (AMHSController == null)
                return false;

            AMHSController.GetSignalInformation(ref information);
            return information != null;
        }

        public void UpdateAMHSValues()
        {
            if (AMHSController == null)
                return;

            AMHSController.ExecuteGatheringSignals(State);
        }

        public bool InitializeSignals()
        {
            if (AMHSController == null)
                return false;

            AMHSController.InitializeSignals();
            return true;
        }

        public CommandResults ExecuteAMHSHandlingToLoad()
        {
            if (AMHSController == null)
                return new CommandResults(LoadPortCommands.AMHSLoading.ToString(), CommandResult.Error);

            return AMHSController.ExecuteHandlingToLoad();
        }
        public CommandResults ExecuteAMHSHandlingToUnload()
        {
            if (AMHSController == null)
                return new CommandResults(LoadPortCommands.AMHSUnloading.ToString(), CommandResult.Error);

            return AMHSController.ExecuteHandlingToUnload();
        }
        public bool WriteAMHSOutput(int index, bool newValue)
        {
            if (AMHSController == null)
                return false;

            return AMHSController.WriteOutput(index, newValue);
        }
        public bool WriteAMHSStopSignal(bool newValue)
        {
            if (AMHSController == null)
                return false;

            int index = AMHSController.IndexOfEmergencyStopSignal;
            bool value = newValue;
            switch (AMHSController.InterfaceType)
            {
                case Define.DefineEnumProject.AppConfig.EN_PIO_INTERFACE_TYPE.E84:
                    value = !value;
                    break;
                case Define.DefineEnumProject.AppConfig.EN_PIO_INTERFACE_TYPE.E23:
                    break;

                default:
                    return false;
            }
            return AMHSController.WriteOutput(index, value);
        }
        #endregion </AMHS>

        #endregion </Methods>
    }
}
using System;
using System.Collections.Generic;
using System.Linq;

using EFEM.Defines.Common;
using EFEM.Modules.LoadPort;
using EFEM.Defines.LoadPort;

namespace EFEM.Modules
{
    public class LoadPortManager
    {
        #region <Constructors>
        public LoadPortManager() { }
        #endregion </Constructors>

        #region <Fields>
        private static LoadPortManager _instance = null;

        private readonly Dictionary<int, LoadPortOperator> LoadPorts = new Dictionary<int, LoadPortOperator>();
        #endregion </Fields>

        #region <Properties>
        public static LoadPortManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new LoadPortManager();
                }

                return _instance;
            }
        }

        public int Count
        {
            get
            {
                return LoadPorts.Count;
            }
        }
        #endregion </Properties>

        #region <Methods>

        #region <Assign, Object>
        public void AssignLoadPorts(LoadPortOperator loadPort)
        {
            int index = LoadPorts.Count;
            LoadPorts.Add(index, loadPort);

            //_loadPortes[index].InitController();
        }

        //public bool GetLoadPortOperator(int lpIndex, ref LoadPortOperator loadPort)
        //{
        //    if (false == LoadPorts.ContainsKey(lpIndex))
        //        return false;

        //    loadPort = LoadPorts[lpIndex];
        //    return true;
        //}

        public int GetLoadPortIndexByPortId(int portId)
        {
            foreach (var item in LoadPorts)
            {
                if (item.Value.PortId == portId)
                    return item.Key;
            }

            return -1;
        }

        public int GetLoadPortPortId(int lpIndex)
        {
            if (false == LoadPorts.ContainsKey(lpIndex))
                return -1;

            return LoadPorts[lpIndex].PortId;
        }

        public int GetLoadPortPortId(string name)
        {
            foreach (var item in LoadPorts)
            {
                if (item.Value.Name.Equals(name))
                    return item.Value.PortId;
            }

            return -1;
        }

        public void SetLoadPortEnabled(int lpIndex, bool enabled)
        {
            if (false == LoadPorts.ContainsKey(lpIndex))
                return;

            LoadPorts[lpIndex].Enabled = enabled;
        }
        public bool IsLoadPortEnabled(int lpIndex)
        {
            if (false == LoadPorts.ContainsKey(lpIndex))
                return false;

            return LoadPorts[lpIndex].Enabled;
        }
        //public bool GetCarrier(int lpIndex, ref Carrier carrier)
        //{
        //    if (false == LoadPorts.ContainsKey(lpIndex))
        //        return false;

        //    return LoadPorts[lpIndex].GetCarrier(ref carrier);
        //}
        public LoadPortStateInformation GetLoadPortState(int lpIndex)
        {
            if (false == LoadPorts.ContainsKey(lpIndex))
                return null;

            return LoadPorts[lpIndex].GetLoadPortState();
        }
        public bool GetLoadPortTransferState(int lpIndex, ref LoadPortTransferStates transferState)
        {
            if (false == LoadPorts.ContainsKey(lpIndex))
                return false;

            transferState = LoadPorts[lpIndex].TransferState;
            return true;
        }
        public bool GetLoadPortCarrierIdState(int lpIndex, ref CarrierIdVerificationStates carrierIdState)
        {
            if (false == LoadPorts.ContainsKey(lpIndex))
                return false;

            carrierIdState = LoadPorts[lpIndex].CarrierIdState;
            return true;
        }
        public bool GetLoadPortCarrierSlotMapState(int lpIndex, ref CarrierSlotMapVerificationStates slotMapState)
        {
            if (false == LoadPorts.ContainsKey(lpIndex))
                return false;

            slotMapState = LoadPorts[lpIndex].CarrierSlotMapState;
            return true;
        }

        public void RecreateCarrierAtLoadPort(int lpIndex)
        {
            if (false == LoadPorts.ContainsKey(lpIndex))
                return;

            LoadPorts[lpIndex].RecreateCarrier();
        }
        #endregion </Assign, Object>

        #region <Event Handler>
        public void AttachModeChangerEventHandler(int lpIndex, LoadPortLoadingMode type, LoadPortModeEventHandler eventHandler)
        {
            if (false == LoadPorts.ContainsKey(lpIndex))
                return;

            LoadPorts[lpIndex].AttachModeChangerEventHandler(type, eventHandler);
        }
        public void AttachMechanicalButtonEventHandlers(int lpIndex, LoadPortButtonTypes type, ButtonPressedEventHandler eventHandler)
        {
            if (false == LoadPorts.ContainsKey(lpIndex))
                return;

            LoadPorts[lpIndex].AttachMechanicalButtonEventHandlers(type, eventHandler);
        }
        public void AttachBusySignalByDigitalInput(int lpIndex, int signalIndex, Func<int, bool> functionToReadInput)
        {
            if (false == LoadPorts.ContainsKey(lpIndex))
                return;

            if (signalIndex >= 0)
            {
                LoadPorts[lpIndex].AttachBusySignalByDigitalInput(() => functionToReadInput(signalIndex));
            }
        }
        #endregion </Event Handler>

        #region <Execute>
        public void Execute()
        {
            foreach (var item in LoadPorts)
            {
                item.Value.Execute();
            }
        }
        #endregion </Execute>

        #region <Actions>
        public void InitLoadPortAction(int lpIndex)
        {
            if (false == LoadPorts.ContainsKey(lpIndex))
                return;

            LoadPorts[lpIndex].InitAction();
        }

        public CommandResults InitializeLoadPort(int lpIndex)
        {
            if (false == LoadPorts.ContainsKey(lpIndex))
                return new CommandResults(LoadPortCommands.Initialize.ToString(), CommandResult.Error);

            return LoadPorts[lpIndex].Initialize();
        }
        public CommandResults ClearAlarmLoadPort(int lpIndex)
        {
            if (false == LoadPorts.ContainsKey(lpIndex))
                return new CommandResults(LoadPortCommands.Reset.ToString(), CommandResult.Error);

            return LoadPorts[lpIndex].ClearAlarm();
        }
        public CommandResults LoadCarrierAtLoadPort(int lpIndex)
        {
            if (false == LoadPorts.ContainsKey(lpIndex))
                return new CommandResults(LoadPortCommands.Load.ToString(), CommandResult.Error);

            return LoadPorts[lpIndex].Load();
        }
        public CommandResults UnloadCarrierAtLoadPort(int lpIndex)
        {
            if (false == LoadPorts.ContainsKey(lpIndex))
                return new CommandResults(LoadPortCommands.Unload.ToString(), CommandResult.Error);

            return LoadPorts[lpIndex].Unload();
        }
        public CommandResults ClampCarrierAtLoadPort(int lpIndex)
        {
            if (false == LoadPorts.ContainsKey(lpIndex))
                return new CommandResults(LoadPortCommands.Clamp.ToString(), CommandResult.Error);

            return LoadPorts[lpIndex].Clamp();
        }
        public CommandResults ReleaseCarrierAtLoadPort(int lpIndex)
        {
            if (false == LoadPorts.ContainsKey(lpIndex))
                return new CommandResults(LoadPortCommands.Unclamp.ToString(), CommandResult.Error);

            return LoadPorts[lpIndex].UnClamp();
        }
        public CommandResults DockCarrierAtLoadPort(int lpIndex)
        {
            if (false == LoadPorts.ContainsKey(lpIndex))
                return new CommandResults(LoadPortCommands.Dock.ToString(), CommandResult.Error);

            return LoadPorts[lpIndex].Dock();
        }
        public CommandResults UnDockCarrierAtLoadPort(int lpIndex)
        {
            if (false == LoadPorts.ContainsKey(lpIndex))
                return new CommandResults(LoadPortCommands.Undock.ToString(), CommandResult.Error);

            return LoadPorts[lpIndex].UnDock();
        }
        public CommandResults OpenCarrierAtLoadPort(int lpIndex)
        {
            if (false == LoadPorts.ContainsKey(lpIndex))
                return new CommandResults(LoadPortCommands.DoorOpen.ToString(), CommandResult.Error);

            return LoadPorts[lpIndex].OpenDoor();
        }
        public CommandResults CloseCarrierAtLoadPort(int lpIndex)
        {
            if (false == LoadPorts.ContainsKey(lpIndex))
                return new CommandResults(LoadPortCommands.DoorClose.ToString(), CommandResult.Error);

            return LoadPorts[lpIndex].CloseDoor();
        }
        public CommandResults ScanCarrierAtLoadPort(int lpIndex)
        {
            if (false == LoadPorts.ContainsKey(lpIndex))
                return new CommandResults(LoadPortCommands.ScanDown.ToString(), CommandResult.Error);

            return LoadPorts[lpIndex].Scan();
        }
        public CommandResults GetMapCarrierAtLoadPort(int lpIndex)
        {
            if (false == LoadPorts.ContainsKey(lpIndex))
                return new CommandResults(LoadPortCommands.GetMap.ToString(), CommandResult.Error);

            return LoadPorts[lpIndex].GetSlotMap();
        }
        public CommandResults ChangeLoadPortModeToFoup(int lpIndex)
        {
            if (false == LoadPorts.ContainsKey(lpIndex))
                return new CommandResults(LoadPortCommands.ChangeToFoup.ToString(), CommandResult.Error);

            return LoadPorts[lpIndex].ChangeCarrierMode(LoadPortLoadingMode.Foup);
        }
        public CommandResults ChangeLoadPortModeToCassette(int lpIndex)
        {
            if (false == LoadPorts.ContainsKey(lpIndex))
                return new CommandResults(LoadPortCommands.ChangeToCassette.ToString(), CommandResult.Error);

            return LoadPorts[lpIndex].ChangeCarrierMode(LoadPortLoadingMode.Cassette);
        }
        public CommandResults FindCurrentLoadingMode(int lpIndex)
        {
            if (false == LoadPorts.ContainsKey(lpIndex))
                return new CommandResults(LoadPortCommands.FindLoadingMode.ToString(), CommandResult.Error);

            return LoadPorts[lpIndex].FindCarrierMode();
        }
        public CommandResults ChangeLoadPortMode(int lpIndex, LoadPortLoadingMode type)
        {
            if (false == LoadPorts.ContainsKey(lpIndex))
                return new CommandResults(LoadPortCommands.GetMap.ToString(), CommandResult.Error);

            return LoadPorts[lpIndex].ChangeCarrierMode(type);
        }
        public CommandResults ChangeLoadPortAccessModeToAuto(int lpIndex)
        {
            if (false == LoadPorts.ContainsKey(lpIndex))
                return new CommandResults(LoadPortCommands.ChangeAccessModeToAuto.ToString(), CommandResult.Error);

            return LoadPorts[lpIndex].ChangeAcceessMode(LoadPortAccessMode.Auto);
        }
        public CommandResults ChangeLoadPortAccessModeToManual(int lpIndex)
        {
            if (false == LoadPorts.ContainsKey(lpIndex))
                return new CommandResults(LoadPortCommands.ChangeAccessModeToManual.ToString(), CommandResult.Error);

            return LoadPorts[lpIndex].ChangeAcceessMode(LoadPortAccessMode.Manual);
        }
        public CommandResults ChangeLoadPortAccessMode(int lpIndex, LoadPortAccessMode mode)
        {
            if (false == LoadPorts.ContainsKey(lpIndex))
                return new CommandResults(LoadPortCommands.GetMap.ToString(), CommandResult.Error);

            return LoadPorts[lpIndex].ChangeAcceessMode(mode);
        }
        #endregion </Actions>

        #region <States>
        public bool IsConnectedWithController(int lpIndex)
        {
            if (false == LoadPorts.ContainsKey(lpIndex))
                return false;

            return LoadPorts[lpIndex].IsConnected;
        }
        public bool GetInitializationState(int lpIndex)
        {
            if (false == LoadPorts.ContainsKey(lpIndex))
                return false;

            return LoadPorts[lpIndex].Initialized;
        }
        public bool IsLoadPortBusy(int lpIndex)
        {
            if (false == LoadPorts.ContainsKey(lpIndex))
                return false;

            return LoadPorts[lpIndex].IsLoadPortBusy;
        }
        public bool GetPresentState(int lpIndex)
        {
            if (false == LoadPorts.ContainsKey(lpIndex))
                return false;

            return LoadPorts[lpIndex].Present;
        }
        public bool GetPlacedState(int lpIndex)
        {
            if (false == LoadPorts.ContainsKey(lpIndex))
                return false;

            return LoadPorts[lpIndex].Placed;
        }
        public bool GetClampingState(int lpIndex)
        {
            if (false == LoadPorts.ContainsKey(lpIndex))
                return false;

            return LoadPorts[lpIndex].ClampState;
        }

        public bool GetDockingState(int lpIndex)
        {
            if (false == LoadPorts.ContainsKey(lpIndex))
                return false;

            return LoadPorts[lpIndex].DockState;
        }

        public bool GetDoorState(int lpIndex)
        {
            if (false == LoadPorts.ContainsKey(lpIndex))
                return false;

            return LoadPorts[lpIndex].DoorState;
        }

        public bool HasErrorStatusByPlacementError(int lpIndex)
        {
            if (false == LoadPorts.ContainsKey(lpIndex))
                return false;

            return LoadPorts[lpIndex].PlacementErrorState;
        }

        public bool HasErrorStatusByCarrierOut(int lpIndex)
        {
            if (false == LoadPorts.ContainsKey(lpIndex))
                return false;

            return LoadPorts[lpIndex].CarrierOutErrorState;
        }

        public bool HasTriggeredAlarm(int lpIndex, ref string alarmDescription)
        {
            if (false == LoadPorts.ContainsKey(lpIndex))
                return false;

            alarmDescription = LoadPorts[lpIndex].TriggeredControllerAlarm;
            return (false == string.IsNullOrEmpty(alarmDescription));
        }
        public LoadPortLoadingMode GetCarrierLoadingType(int lpIndex)
        {
            if (false == LoadPorts.ContainsKey(lpIndex))
                return LoadPortLoadingMode.Unknown;

            return LoadPorts[lpIndex].LoadingType;
        }

        public LoadPortAccessMode GetAccessMode(int lpIndex)
        {
            if (false == LoadPorts.ContainsKey(lpIndex))
                return LoadPortAccessMode.Manual;

            return LoadPorts[lpIndex].AccessMode;
        }
        #endregion <States>

        #region <ETC>
        public string GetLoadPortLocationName(string name)
        {
            foreach (var item in LoadPorts)
            {
                if (item.Value.Name.Equals(name))
                {
                    return item.Value.GetCurrentLocationName();
                }
            }

            return string.Empty;
        }

        public string GetLoadPortName(int lpIndex)
        {
            if (false == LoadPorts.ContainsKey(lpIndex))
                return null;

            return LoadPorts[lpIndex].Name;
        }

        public string GetCurrentLocationName(int portId)
        {
            foreach (var item in LoadPorts)
            {
                if (item.Value.PortId.Equals(portId))
                {
                    return item.Value.GetCurrentLocationName();
                }
            }
            return string.Empty;
        }
        #endregion </ETC>

        #region <AMHS>
        public bool AssignAMHSSignalControlFunctions(int lpIndex,
            Func<int, bool> functionToReadInput,
            Func<int, bool> functionToReadOutput,
            Func<int, bool, DigitalIO_.DIO_RESULT> functionToWriteOutput)
        {
            if (false == LoadPorts.ContainsKey(lpIndex))
                return false;

            return LoadPorts[lpIndex].AssignAMHSSignalControlFunctions(functionToReadInput, functionToReadOutput, functionToWriteOutput);
        }

        public bool AssignActionBeforeCarrierLoads(int lpIndex, Func<int, CommandResults> action)
        {
            if (false == LoadPorts.ContainsKey(lpIndex))
                return false;

            return LoadPorts[lpIndex].AssignActionBeforeCarrierLoads(action);
        }
        public bool GetAMHSSaftyInterLockStatus(int lpIndex)
        {
            if (false == LoadPorts.ContainsKey(lpIndex))
                return false;

            return LoadPorts[lpIndex].GetAMHSSaftyInterLockStatus();
        }
        public bool GetAMHSSignalValues(int lpIndex, ref Dictionary<int, bool> inputs, ref Dictionary<int, bool> outputs)
        {
            if (false == LoadPorts.ContainsKey(lpIndex))
                return false;

            return LoadPorts[lpIndex].GetAMHSSignalValues(ref inputs, ref outputs);
        }
        public bool GetAMHSInformation(int lpIndex, ref AMHSInformation information)
        {
            if (false == LoadPorts.ContainsKey(lpIndex))
                return false;

            return LoadPorts[lpIndex].GetAMHSInformation(ref information);
        }

        public bool InitializeAMHSSignals(int lpIndex)
        {
            if (false == LoadPorts.ContainsKey(lpIndex))
                return false;

            return LoadPorts[lpIndex].InitializeSignals();
        }

        public CommandResults ExecuteAMHSHandlingToLoad(int lpIndex)
        {
            if (false == LoadPorts.ContainsKey(lpIndex))
                return new CommandResults(LoadPortCommands.AMHSLoading.ToString(), CommandResult.Error);

            return LoadPorts[lpIndex].ExecuteAMHSHandlingToLoad();
        }
        public CommandResults ExecuteAMHSHandlingToUnload(int lpIndex)
        {
            if (false == LoadPorts.ContainsKey(lpIndex))
                return new CommandResults(LoadPortCommands.AMHSUnloading.ToString(), CommandResult.Error);

            return LoadPorts[lpIndex].ExecuteAMHSHandlingToUnload();
        }
        public bool WriteAMHSOutput(int lpIndex, int index, bool newValue)
        {
            if (false == LoadPorts.ContainsKey(lpIndex))
                return false;

            return LoadPorts[lpIndex].WriteAMHSOutput(index, newValue);
        }
        public bool WriteAMHSStopSignal(int lpIndex, bool newValue)
        {
            if (false == LoadPorts.ContainsKey(lpIndex))
                return false;

            return LoadPorts[lpIndex].WriteAMHSStopSignal(newValue);
        }
        #endregion </AMHS>

        #endregion </Methods>
    }
}
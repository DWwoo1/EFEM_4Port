using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EFEM.Modules;
using EFEM.Defines.LoadPort;
using EFEM.Defines.AtmRobot;
using EFEM.Modules.LoadPort;
using EFEM.Modules.AtmRobot;
using EFEM.Defines.MaterialTracking;
using EFEM.MaterialTracking.LocationServer;
using EFEM.MaterialTracking;

namespace EFEM.ActionScheduler.LoadPortActionSchedulers
{
    #region <Enumerations>
    public enum CARRIER_PORT_TYPE
    {
        SELECTION,
        READY_TO_LOAD,
        ACTION_LOAD,
        READY_TO_UNLOAD,
        ACTION_UNLOAD
    }
    #endregion <Enumerations>

    public abstract class BaseLoadPortActionScheduler
    {
        #region <Constructors>
        public BaseLoadPortActionScheduler(int lpIndex)
        {
            _loadPortManager = LoadPortManager.Instance;
            _carrierServer = CarrierManagementServer.Instance;
            _locationServer = LocationServer.Instance;
            _substrateManager = SubstrateManager.Instance;

            Index = lpIndex;
            PortId = _loadPortManager.GetLoadPortPortId(Index);

            _loadPortInformation = new LoadPortStateInformation();
        }

        #endregion </Constructors>

        #region <Fields>
        protected static LoadPortManager _loadPortManager = null;
        protected static CarrierManagementServer _carrierServer = null;
        protected static LocationServer _locationServer = null;
        protected static SubstrateManager _substrateManager = null;

        protected LoadPortStateInformation _loadPortInformation = null;
        protected int _seqNum;
        #endregion </Fields>

        #region <Properties>
        public int Index { get; private set; }
        public int PortId { get; private set; }
        #endregion </Properties>

        #region <Methods>
        public void InitScheduler()
        {
            _seqNum = 0;
        }

        public CARRIER_PORT_TYPE ExecuteSchedulers()
        {
            _loadPortInformation = _loadPortManager.GetLoadPortState(Index);
            
            if (IsCarrierCompleted())
            {
                _carrierServer.SetCarrierAccessingStatus(PortId, CarrierAccessStates.CarrierCompleted);
            }

            return DecideNextAction();
        }

        public virtual void PrepareCarrierIdVerificationInformation(Dictionary<string, string> idInformation)
        {
        }

        public virtual VarificationResults ExecuteCarrierIdVerification()
        {
            return VarificationResults.Completed;
        }
        public virtual void PrepareCarrierSlotMapVerificationInformation(Dictionary<int, CarrierSlotMapStates> slotStatus)
        {
        }

        public virtual VarificationResults ExecuteCarrierSlotVerification()
        {
            return VarificationResults.Completed;
        }

        public virtual void ChangeSlotMapForDryRun()
        {
            if (false == _carrierServer.HasCarrier(PortId))
                return;

            var slotMaps = _carrierServer.GetCarrierSlotMap(PortId);
            if (slotMaps == null)
                return;

            for (int i = 0; i < slotMaps.Length; ++i)
            {
                LoadPortLocation location = new LoadPortLocation(PortId, i, "");
                if (_locationServer.GetLoadPortSlotLocation(PortId, i, ref location))
                {
                    bool hasSubstrate = _substrateManager.HasSubstrateAtLoadPort(PortId, i);
                    switch (slotMaps[i])
                    {
                        case CarrierSlotMapStates.Empty:
                            slotMaps[i] = CarrierSlotMapStates.CorrectlyOccupied;
                            if (false == hasSubstrate)
                            {
                                _substrateManager.CreateSubstrate(location.Name, location);
                            }
                            break;
                        case CarrierSlotMapStates.NotEmpty:
                        case CarrierSlotMapStates.CorrectlyOccupied:
                        case CarrierSlotMapStates.DoubleSlotted:
                        case CarrierSlotMapStates.CrossSlotted:
                            slotMaps[i] = CarrierSlotMapStates.Empty;
                            if (hasSubstrate)
                            {
                                string name = _substrateManager.GetSubstrateNameAtLoadPort(PortId, i);
                                _substrateManager.RemoveSubstrate(name, location);
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            _carrierServer.SetCarrierSlotMap(PortId, slotMaps);
            _substrateManager.SaveRecoveryDataAll();
        }

        public void InitWorkingInfo()
        {
            _seqNum = 0;
        }

        protected bool ShouldUnloadCarrier()
        {
            if (false == _carrierServer.HasCarrier(PortId))
                return false;

            CarrierAccessStates accessState = _carrierServer.GetCarrierAccessingStatus(PortId);

            return (accessState.Equals(CarrierAccessStates.CarrierCompleted)
                || accessState.Equals(CarrierAccessStates.CarrierStopped));
        }

        protected virtual bool IsCarrierCompleted()
        {
            if (false == _loadPortInformation.TransferState.Equals(LoadPortTransferStates.TransferBlocked))
            {
                return false;
            }
            else
            {
                if (false == _carrierServer.HasCarrier(PortId))
                    return false;

                int capacity = _carrierServer.GetCapacity(PortId);
                if (capacity <= 0)
                    return false;

                //if (false == _substrateManager.HasAnySubstrateInLoadPort(PortId))
                {
                    // 자재가 없으면, 모든 자재를 검사하여 모두 도착했는지 비교한다.
                    if (false == _substrateManager.IsSubstrateAtDestination(PortId))
                        return false;
                }

                for (int i = 0; i < capacity; ++i)
                {
                    if (false == _substrateManager.HasSubstrateAtLoadPort(PortId, i))
                        continue;

                    LoadPortLocation lpLocation = new LoadPortLocation(PortId, i, "");
                    if (_locationServer.GetLoadPortSlotLocation(PortId, i, ref lpLocation))
                    {
                        ProcessingStates processingStatus = ProcessingStates.NeedsProcessing;
                        SubstrateTransferStates transferStatus = SubstrateTransferStates.AtSource;
                        if (_substrateManager.GetTransferStatus(lpLocation, "", ref transferStatus) &&
                            _substrateManager.GetProcessingStatus(lpLocation, "", ref processingStatus))
                        {
                            if (false == _substrateManager.IsProcessingCompleted(transferStatus, processingStatus))
                                return false;
                        }

                    }

                }
            }
            
            return true;
        }

        protected virtual CARRIER_PORT_TYPE DecideNextAction()
        {
            switch (_loadPortInformation.TransferState)
            {
                case LoadPortTransferStates.TransferBlocked:
                    if (ShouldUnloadCarrier())
                    {
                        if (_loadPortInformation.DoorState ||
                            _loadPortInformation.DockState ||
                            _loadPortInformation.ClampState)
                        {
                            return CARRIER_PORT_TYPE.ACTION_UNLOAD;
                        }
                        else
                        {
                            return CARRIER_PORT_TYPE.READY_TO_UNLOAD;
                        }
                    }
                    else
                    {
                        if (false == _loadPortInformation.DoorState)
                        {
                            return CARRIER_PORT_TYPE.ACTION_LOAD;
                        }
                    }
                    break;

                case LoadPortTransferStates.ReadyToLoad:
                    return CARRIER_PORT_TYPE.READY_TO_LOAD;
                    
                case LoadPortTransferStates.ReadyToUnload:
                    return CARRIER_PORT_TYPE.READY_TO_UNLOAD;
                    
            }
            
            return CARRIER_PORT_TYPE.SELECTION;
        }
        #endregion </Methods>
    }
}

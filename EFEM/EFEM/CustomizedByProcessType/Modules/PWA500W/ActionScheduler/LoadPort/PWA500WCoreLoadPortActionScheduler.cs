﻿using System;
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
using EFEM.MaterialTracking;
using EFEM.Defines.MaterialTracking;
using EFEM.MaterialTracking.LocationServer;
using EFEM.ActionScheduler.LoadPortActionSchedulers;

using FrameOfSystem3.SECSGEM.Scenario;

namespace EFEM.CustomizedByProcessType.PWA500W
{
    public class CoreLoadPortActionScheduler : BaseLoadPortActionScheduler
    {
        #region <Constructors>
        public CoreLoadPortActionScheduler(int lpIndex) : base(lpIndex)
        {
            _processGroup = ProcessModuleGroup.Instance;
            _temporaryLoadingLocations = new List<string>();
        }
        #endregion </Constructors>

        #region <Fields>
        private static ProcessModuleGroup _processGroup = null;

        private List<string> _temporaryLoadingLocations = null;

        private const int ProcessModuleIndex = 0;
        #endregion </Fields>

        #region <Properties>
        #endregion </Properties>

        #region <Methods>
        public override void ChangeSlotMapForDryRun()
        {
            if (false == _carrierServer.HasCarrier(PortId))
                return;

            var slotMaps = _carrierServer.GetCarrierSlotMap(PortId);
            if (slotMaps == null)
                return;

            LoadPortLoadingMode loadingMode = _loadPortManager.GetCarrierLoadingType(Index);
            string carrierId = _carrierServer.GetCarrierId(PortId);
            for (int i = 0; i < slotMaps.Length; ++i)
            {
                if (i == 0 && loadingMode.Equals(LoadPortLoadingMode.Cassette))
                {
                    slotMaps[i] = CarrierSlotMapStates.NotEmpty;
                }

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
            SubstrateManager.Instance.SaveRecoveryDataAll();
        }

        protected override CARRIER_PORT_TYPE DecideNextAction()
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
                            switch (Index)
                            {
                                case (int)LoadPortType.Core_8_1:
                                    if (false == CheckLoadingRequest(SubstrateTypeForUI.Core_8)
                                        || _carrierServer.GetCarrierAccessingStatus((int)LoadPortType.Core_12).Equals(CarrierAccessStates.InAccessed))
                                    {
                                        return CARRIER_PORT_TYPE.SELECTION;
                                    }
                                    else
                                    {
                                        return CARRIER_PORT_TYPE.ACTION_LOAD;
                                    }
                                case (int)LoadPortType.Core_8_2:
                                    if (false == CheckLoadingRequest(SubstrateTypeForUI.Core_8)
                                        || _carrierServer.GetCarrierAccessingStatus((int)LoadPortType.Core_12).Equals(CarrierAccessStates.InAccessed))
                                    {
                                        return CARRIER_PORT_TYPE.SELECTION;
                                    }
                                    else
                                    {
                                        return CARRIER_PORT_TYPE.ACTION_LOAD;
                                    }
                                case (int)LoadPortType.Core_12:
                                    if (false == CheckLoadingRequest(SubstrateTypeForUI.Core_12)
                                        || _carrierServer.GetCarrierAccessingStatus((int)LoadPortType.Core_8_1).Equals(CarrierAccessStates.InAccessed)
                                        || _carrierServer.GetCarrierAccessingStatus((int)LoadPortType.Core_8_2).Equals(CarrierAccessStates.InAccessed))
                                    {
                                        return CARRIER_PORT_TYPE.SELECTION;
                                    }
                                    else
                                    {
                                        return CARRIER_PORT_TYPE.ACTION_LOAD;
                                    }
                            }
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
        private bool CheckLoadingRequest(SubstrateTypeForUI subsType)
        {
            _processGroup.IsLoadingRequested(ProcessModuleIndex, ref _temporaryLoadingLocations);

            if (_temporaryLoadingLocations == null)
                return false;

            foreach (var item in _temporaryLoadingLocations)
            {
                if (item.Contains(subsType.ToString()))
                    return true;
            }
            return false;
        }
        #endregion </Methods>
    }
}
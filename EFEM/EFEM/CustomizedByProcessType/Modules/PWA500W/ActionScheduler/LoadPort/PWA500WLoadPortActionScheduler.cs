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
using EFEM.MaterialTracking;
using EFEM.MaterialTracking.LocationServer;
using EFEM.Defines.MaterialTracking;
using EFEM.ActionScheduler.LoadPortActionSchedulers;
using FrameOfSystem3.Recipe;

namespace EFEM.CustomizedByProcessType.PWA500W
{
    public class BinLoadPortActionScheduler : BaseLoadPortActionScheduler
    {
        #region <Constructors>
        public BinLoadPortActionScheduler(int lpIndex) : base(lpIndex) 
        {
            _recipe = Recipe.GetInstance();

            PARAM_EQUIPMENT paramUseCapacity = PARAM_EQUIPMENT.UseCapacityLimitBin1 + lpIndex;
            PARAM_EQUIPMENT paramCapacityLimit = PARAM_EQUIPMENT.AvailableCarrierCapacityBin1 + lpIndex;

            ParamUseCapacityLimit = paramUseCapacity.ToString();
            ParamUseAvailableCapacity = paramCapacityLimit.ToString();

            int taskIndex = (int)Define.DefineEnumProject.Task.EN_TASK_LIST.LoadPort1 + lpIndex;          
            TaskName = ((Define.DefineEnumProject.Task.EN_TASK_LIST)taskIndex).ToString();
        }
        #endregion </Constructors>

        #region <Fields>
        private static Recipe _recipe;
        private readonly string TaskName;
        private readonly string ParamUseCapacityLimit;
        private readonly string ParamUseAvailableCapacity;

        private bool _useCapacityLimit;
        private int _availableCapacity;
        private int _maxCapacity;
        #endregion </Fields>

        #region <Properties>
        #endregion </Properties>

        #region <Methods>
        protected override bool IsCarrierCompleted()
        {
            if (false == _loadPortInformation.TransferState.Equals(LoadPortTransferStates.TransferBlocked))
            {
                return false;
            }

            if (false == _carrierServer.HasCarrier(PortId))
                return false;

            int capacity = _carrierServer.GetCapacity(PortId);
            if (capacity <= 0)
                return false;

            _useCapacityLimit = _recipe.GetValue(EN_RECIPE_TYPE.EQUIPMENT, ParamUseCapacityLimit, false);
            _maxCapacity = _carrierServer.GetCapacity(PortId);
            LoadPortLoadingMode loadingMode = _loadPortManager.GetCarrierLoadingType(Index);

            int count = 0;
            bool completed = true;
            for (int i = 0; i < capacity; ++i)
            {
                // 카세트는 0번을 작업하지 못한다.
                if (i == 0 && loadingMode.Equals(LoadPortLoadingMode.Cassette))
                {
                    continue;
                }

                bool hasSubstrate = _substrateManager.HasSubstrateAtLoadPort(PortId, i);
                if (false == hasSubstrate)
                {
                    // 용량 제한을 사용하지 않는 경우에는 하나라도 비어있으면 작업을 해야한다.
                    if (false == _useCapacityLimit)
                    {
                        completed = false;
                        break;
                    }
                    else
                    {
                        continue;
                    }
                }

                LoadPortLocation lpLocation = new LoadPortLocation(PortId, i, "");
                if (_locationServer.GetLoadPortSlotLocation(PortId, i, ref lpLocation))
                {
                    SubstrateTransferStates transferStatus = SubstrateTransferStates.AtSource;
                    if (_substrateManager.GetTransferStatus(lpLocation, "", ref transferStatus))
                    {
                        if (transferStatus.Equals(SubstrateTransferStates.AtDestination))
                        {
                            ++count;
                        }

                    }
                }
            }

            // 용량제한을 사용하는 경우
            if (_useCapacityLimit)
            {
                _availableCapacity = _recipe.GetValue(EN_RECIPE_TYPE.EQUIPMENT, ParamUseAvailableCapacity, _maxCapacity);
                completed = count >= _availableCapacity;
            }

            return completed;
        }

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
                            if (hasSubstrate)
                            {
                                string name = _substrateManager.GetSubstrateNameAtLoadPort(PortId, i);
                                _substrateManager.RemoveSubstrate(name, location);
                            }

                            break;
                        case CarrierSlotMapStates.NotEmpty:
                        case CarrierSlotMapStates.CorrectlyOccupied:
                        case CarrierSlotMapStates.DoubleSlotted:
                        case CarrierSlotMapStates.CrossSlotted:
                            slotMaps[i] = CarrierSlotMapStates.CorrectlyOccupied;
                            if (false == hasSubstrate)
                            {
                                _substrateManager.CreateSubstrate(location.Name, location);
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
        protected override CARRIER_PORT_TYPE DecideNextAction()
        {
            return base.DecideNextAction();
        }
        #endregion </Methods>
    }
}

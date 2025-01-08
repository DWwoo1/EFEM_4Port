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

namespace EFEM.CustomizedByCustomer.PWA500BIN
{
    public class EmptyTapeLoadPortActionScheduler : BaseLoadPortActionScheduler
    {
        #region <Constructors>
        public EmptyTapeLoadPortActionScheduler(int lpIndex) : base(lpIndex) { }
        #endregion </Constructors>

        #region <Fields>
        #endregion </Fields>

        #region <Properties>
        #endregion </Properties>

        #region <Methods>
        // 2024.11.27. jhlim [MOD] 고객사 요청으로 캐리어 완료 조건을 부모 클래스의 조건에서 모든 자재가 배출되는 것으로 변경
        protected override bool IsCarrierCompleted()
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

                // 자재가 뭐라도 있으면 완료된게 아님
                if (_substrateManager.HasAnySubstrateAtLoadPort(PortId))
                    return false;
                
                // TODO : 한계시간에 의한 조건을 추가해야 한다. -> 고객사에서 운용 시나리오가 아직 확립되지 않음

                #region <부모클래스 조건>
                ////if (false == _substrateManager.HasAnySubstrateInLoadPort(PortId))
                //{
                //    // 자재가 없으면, 모든 자재를 검사하여 모두 도착했는지 비교한다.
                //    if (false == _substrateManager.IsSubstrateAtDestination(PortId))
                //        return false;
                //}

                //for (int i = 0; i < capacity; ++i)
                //{
                //    if (false == _substrateManager.HasSubstrateAtLoadPort(PortId, i))
                //        continue;

                //    LoadPortLocation lpLocation = new LoadPortLocation(PortId, i, "");
                //    if (_locationServer.GetLoadPortSlotLocation(PortId, i, ref lpLocation))
                //    {
                //        ProcessingStates processingStatus = ProcessingStates.NeedsProcessing;
                //        SubstrateTransferStates transferStatus = SubstrateTransferStates.AtSource;
                //        if (_substrateManager.GetTransferStatus(lpLocation, "", ref transferStatus) &&
                //            _substrateManager.GetProcessingStatus(lpLocation, "", ref processingStatus))
                //        {
                //            if (false == _substrateManager.IsProcessingCompleted(transferStatus, processingStatus))
                //                return false;
                //        }

                //    }

                //}
                #endregion </부모클래스 조건>
            }

            return true;
        }
        // 2024.11.27. jhlim [END]

        //protected override bool IsCarrierCompleted()
        //{
        //    if (false == _loadPortInformation.TransferState.Equals(LoadPortTransferStates.TransferBlocked))
        //    {
        //        return false;
        //    }
        //    else
        //    {
        //        if (false == _carrierServer.HasCarrier(PortId))
        //            return false;

        //        int capacity = _carrierServer.GetCapacity(PortId);
        //        if (capacity <= 0)
        //            return false;

        //        for (int i = 0; i < capacity; ++i)
        //        {
        //            LoadPortLocation lpLocation = new LoadPortLocation(PortId, i, "");
        //            if (_locationServer.GetLoadPortSlotLocation(PortId, i, ref lpLocation))
        //            {
        //                SubstrateTransferStates transferStatus = SubstrateTransferStates.AtSource;
        //                if (_substrateManager.GetTransferStatus(lpLocation, "", ref transferStatus))
        //                {
        //                    if (transferStatus.Equals(SubstrateTransferStates.AtDestination))
        //                        continue;

        //                    return false;
        //                }
        //            }
        //        }
        //    }

        //    return true;
        //}

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
                                SubstrateManager.Instance.CreateSubstrate(location.Name, location);                                
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
        }

        protected override CARRIER_PORT_TYPE DecideNextAction()
        {
            return base.DecideNextAction();
        }
        #endregion </Methods>
    }
}

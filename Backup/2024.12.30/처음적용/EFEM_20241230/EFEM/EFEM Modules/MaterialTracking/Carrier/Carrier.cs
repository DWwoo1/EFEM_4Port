using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;

using EFEM.Defines.LoadPort;
using EFEM.Defines.MaterialTracking;
using EFEM.MaterialTracking.LocationServer;
using EFEM.MaterialTracking.CarrierAttribute;

namespace EFEM.MaterialTracking
{
    public class Carrier
    {
        #region <Constructors>
        public Carrier(int portId, BaseCarrierAttribute attributes, Dictionary<int, LoadPortLocation> locations)
        {
            _substrateManager = SubstrateManager.Instance;
            _locationServer = LocationServer.LocationServer.Instance;

            Attributes = attributes;
            //Attributes.CreateCarrierAttributes();

            PortId = portId;
            //SetLotId(string.Empty);

            //if (string.IsNullOrEmpty(CarrierId))
            //{
            //    SetCarrierId(string.Format("CARRIER{0:d2}", PortId));
            //}
            //if (string.IsNullOrEmpty(AccessingStatus.ToString()) ||
            //    AccessingStatus.Equals(CarrierAccessStates.Unknown))
            //{
            //    SetAccessingStatus(CarrierAccessStates.NotAccessed);
            //}
            _locations = locations;

            OnCarrierCreated();
        }
        #endregion </Constructors>

        #region <Fields>
        private readonly ConcurrentDictionary<int, CarrierSlotMapStates> SlotInformation
            = new ConcurrentDictionary<int, CarrierSlotMapStates>();

        private Dictionary<int, LoadPortLocation> _locations = null;

        private static SubstrateManager _substrateManager = null;
        private static LocationServer.LocationServer _locationServer = null;

        private readonly ReaderWriterLockSlim SlimLock = new ReaderWriterLockSlim();
        private readonly BaseCarrierAttribute Attributes = null;
        #endregion </Fields>

        #region <Properties>
        public int PortId { get; private set; }
        public int Capacity
        {
            get
            {
                SlimLock.EnterReadLock();

                int capacity = SlotInformation.Count;

                SlimLock.ExitReadLock();
                
                return capacity;
            }
        }
        public string CarrierId
        {
            get
            {
                return Attributes.CarrierId;
            }
        }
        public string LotId 
        {
            get
            {
                return Attributes.LotId;
            }
        }
        public DateTime LoadingTime 
        {
            get
            {
                return Attributes.LoadTime;
            }
        }
        public DateTime UnloadingTime
        {
            get
            {
                return Attributes.UnloadTime;
            }
        }
        public CarrierAccessStates AccessingStatus 
        {
            get
            {
                return Attributes.AccessingStatus;
            }
        }
        public CarrierSlotMapStates[] SlotState
        {
            get
            {
                SlimLock.EnterReadLock();
                var status = SlotInformation.Values.ToArray();
                SlimLock.ExitReadLock();

                return status;
            }
        }
        #endregion </Properties>

        #region <Methods>

        #region <Carrier>
        public void SetCarrierId(string carrierId)
        {
            Attributes.SetCarrierId(carrierId);
        }
        public void SetLotId(string lotId)
        {
            Attributes.SetLotId(lotId);
        }
        public void SetSlotMap(CarrierSlotMapStates[] newSlotState)
        {
            SlimLock.EnterWriteLock();

            if (SlotInformation.Count != newSlotState.Length)
            {
                SlotInformation.Clear();
            }

            for (int i = 0; i < newSlotState.Length; ++i)
            {
                SlotInformation[i] = newSlotState[i];
                LoadPortLocation location = new LoadPortLocation(-1, -1, "");
                if (_locationServer.GetLoadPortSlotLocation(PortId, i, ref location))
                {
                    switch (newSlotState[i])
                    {
                        case CarrierSlotMapStates.CorrectlyOccupied:
                            {
                                if (false == _substrateManager.HasSubstrateAtLoadPort(PortId, i))
                                {
                                    // 슬롯 맵핑 결과 감지되었고, 현재 자재 정보가 없다. -> 생성 : 첫 스캔의 경우
                                    _substrateManager.CreateSubstrate(location.Name, location);
                                    _substrateManager.SetSubstrateLotInfoByLocation(location, location.Name, LotId, string.Empty, CarrierId);
                                    //_substrateManager.GetSubstrate(location, ref _substrates[i]);
                                }
                            }
                            break;
                        //case CARRIER_MAP_STATES.UNDEFINED:
                        //    break;
                        case CarrierSlotMapStates.Empty:
                            {
                                // 2024.08.22. jhlim [MOD] 슬롯맵핑 결과 감지되지 않았는데, 이미 나간 자재가 있으면?? 로직 변경
                                // 해당 캐리어 자재인지 여부는 포트, 슬롯, 캐리어 이름으로 비교한다.
                                Substrate substrate = new Substrate("");
                                if (_substrateManager.GetSubstrateBySourceCarrierInfo(PortId, i, CarrierId, ref substrate))
                                {
                                    var processingStatus = substrate.GetProcessingStatus();
                                    switch (processingStatus)
                                    {
                                        case ProcessingStates.Lost:
                                            {
                                                // 1. 자재가 제거되었다. : 제거된 경우니 없는게 정상이다.
                                            }
                                            break;
                                        default:
                                            {
                                                // 2. 자재가 공정을 위해 나가서 없는 상태다.
                                                SlotInformation[i] = CarrierSlotMapStates.CorrectlyOccupied;
                                            }
                                            break;
                                    }
                                }                                                          

                                #region <기존>
                                //string substrateName = _substrateManager.GetSubstrateNameAtLoadPort(PortId, i);
                                //if (false == string.IsNullOrEmpty(substrateName))
                                //{
                                //    ProcessingStates processingStatus = ProcessingStates.NeedsProcessing;
                                //    if (_substrateManager.GetProcessingStatus(location, substrateName, ref processingStatus))
                                //    {
                                //        switch (processingStatus)
                                //        {
                                //            case ProcessingStates.Lost:
                                //                {
                                //                    // 1. 자재가 제거되었다. : 제거된 경우니 없는게 정상이다.
                                //                }
                                //                break;
                                //            default:
                                //                {
                                //                    // 2. 자재가 공정을 위해 나가서 없는 상태다.
                                //                    SlotInformation[i] = CarrierSlotMapStates.CorrectlyOccupied;
                                //                }
                                //                break;
                                //        }
                                //    }
                                //}
                                #endregion </기존>
                            }
                            break;
                        default:
                            break;
                    }
                }
            }

            SlimLock.ExitWriteLock();
        }
        public void SetAccessingStatus(CarrierAccessStates newState)
        {
            Attributes.SetCarrierAccessStatus(newState);
        }
        public string GetAttribute(string attributeName)
        {
            if (Attributes == null)
                return string.Empty;

            return Attributes.GetCarrierAttributes(attributeName);
        }
        public void SetAttribute(string attributeName, string attributeValue)
        {
            if (Attributes == null)
                return;

            Attributes.SetCarrierAttributes(attributeName, attributeValue);
        }
        public void ClearAttributes()
        {
            if (Attributes == null)
                return;

            Attributes.ClearCarrierAttributes();
        }

        public void OnCarrierCreated()
        {
            if (false == Attributes.HasCarrier)
            {
                // 새로 생성된 경우
                Attributes.Init();
                Attributes.SetCarrierAccessStatus(CarrierAccessStates.NotAccessed);
                Attributes.SetCarrierLoadTime(DateTime.Now);
                Attributes.OnCarrierCreated();                
            }
            
            Attributes.SetCarrierPresence(true);
        }
        public void OnCarrierRemoved()
        {
            Attributes.SetCarrierPresence(false);
            Attributes.OnCarrierRemoved();
            Attributes.ClearCarrierAttributes();
            Attributes.Save();
        }
        #endregion </Carrier>

        #region <Substrate>
        public bool HasAnySubstrate()
        {
            return _substrateManager.HasAnySubstrateAtLoadPort(PortId);
        }
        public bool HasSubstrateAtSlot(int slot)
        {
            return _substrateManager.HasSubstrateAtLoadPort(PortId, slot);
        }
        public void AssignSubstrate(int slot, Substrate substrate)
        {
            _substrateManager.AssignSubstrateInLoadPort(PortId, slot, substrate);
        }
        public void RemoveSubstrate(int slot)
        {
            _substrateManager.RemoveSubstrateInLoadPort(PortId, slot);
        }
        public void RemoveSubstratesAll()
        {           
            _substrateManager.RemoveSubstrateInLoadPortAll(PortId);
        }
        #endregion </Substrate>

        #endregion </Methods>
    }
}
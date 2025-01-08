using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

using EFEM.MaterialTracking;
using EFEM.Defines.MaterialTracking;
using EFEM.Defines.LoadPort;
using EFEM.MaterialTracking.LocationServer;

namespace EFEM.CarrierManagement
{
    public class Carrier
    {
        #region <Constructors>
        public Carrier(int portId, int slotCount)
        {
            _substrateManager = SubstrateManager.Instance;
            _locationServer = LocationServer.Instance;

            PortId = portId;
            _slotCapacity = slotCount;
            LoadingTime = DateTime.Now;
            CarrierId = LoadingTime.ToString("HHmmss");
            
            CarrierSlots = new ConcurrentDictionary<int, CarrierSlot>();
            for(int i = 0; i < CapacityMax; ++i)
            {
                CarrierSlots[i] = new CarrierSlot();
            }
        }
        #endregion </Constructors>

        #region <Fields>
        private readonly ConcurrentDictionary<int, CarrierSlot> CarrierSlots = null;

        //private Substrate[] _substrates = null;

        private const int CapacityMax = 25;
        private int _slotCapacity;

        private static SubstrateManager _substrateManager = null;
        private static LocationServer _locationServer = null;
        //private readonly ConcurrentDictionary<int, Substrate> Substrates
        //    = new ConcurrentDictionary<int, Substrate>();
        #endregion </Fields>

        #region <Properties>
        public int PortId { get; private set; }
        public int Capacity
        {
            get
            {
                return _slotCapacity;
            }
            set
            {
                _slotCapacity = value;
            }
        }
        public string CarrierId { get; private set; }
        public DateTime LoadingTime {get; private set; }
        public DateTime UnloadingTime { get; private set; }
        public CarrierAccessStates AccessingState { get; set; }
        public CarrierIdVerificationStates IdVerificationState { get; set; }
        public CarrierSlotMapVerificationStates SlotMapVerificationState { get; set; }
        #endregion </Properties>

        #region <Methods>
        
        #region <Carrier>

        public void SetCapacity(int capacity)
        {
            Capacity = capacity;
        }
        public void SetCarrierId(string carrierId)
        {
            CarrierId = carrierId;
        }
        public void SetSlotMap(CarrierSlotMapStates[] newSlotState)
        {
            if (Capacity != newSlotState.Length)
            {
                SetCapacity(newSlotState.Length);
            }

            for (int i = 0; i < newSlotState.Length; ++i)
            {
                CarrierSlots[i] = new CarrierSlot();
                CarrierSlots[i].SetSlotMapState(newSlotState[i]);
                Substrate substrate = CarrierSlots[i].GetSubstrate();
                
                switch (newSlotState[i])
                {
                    case CarrierSlotMapStates.CorrectlyOccupied:
                        {
                            if (substrate == null)
                            {
                                var substrateName = _locationServer.GetLocationNameByCarrierInfo(CarrierId, i);

                                _substrateManager.CreateSubstrate(substrateName, PortId, i);
                                _substrateManager.GetSubstrate(substrateName, ref substrate);
                                
                                CarrierSlots[i].AssignSlot(substrate);
                            }
                        }
                        break;
                    //case CARRIER_MAP_STATES.UNDEFINED:
                    //    break;
                    case CarrierSlotMapStates.Empty:
                        {
                            // 자재 정보가 생성된 것이 있는데 없어진 거면
                            if (substrate != null)
                            {
                                var state = substrate.TransportState;
                                switch (state)
                                {
                                    case SubstrateTransferStates.AtSource:
                                    case SubstrateTransferStates.AtDestination:
                                        CarrierSlots[i].SetSlotMapState(CarrierSlotMapStates.CorrectlyOccupied);
                                        break;
                                }
                            }
                        }
                        break;
                    //case CARRIER_MAP_STATES.NOT_EMPTY:
                    //    break;
                    //case CARRIER_MAP_STATES.DOUBLE_SLOTTED:
                    //    break;
                    //case CARRIER_MAP_STATES.CROSS_SLOTTED:
                    //    break;
                    default:
                        break;
                }
                //_slotInformation.TryAdd(i, newSlotState[i]);
            }
        }
        #endregion </Carrier>

        #region <Substrate>
        public bool HasAnySubstrate()
        {
            foreach (var item in CarrierSlots)
            {
                if (item.Value.MySubstrate == null)
                    return false;
            }
            
            return true;
        }
        public bool HasCarrierSubstrateAtSlot(int slot)
        {
            if (CarrierSlots[slot] == null)
                return false;

            string location = _locationServer.GetLocationNameByCarrierInfo(CarrierId, slot);
            return CarrierSlots[slot].MySubstrate.Location.Equals(location);
        }

        public bool GetSubstrate(int slot, ref Substrate substrate)
        {
            substrate = CarrierSlots[slot].MySubstrate;
            
            return (substrate != null);
        }
        public bool GetSubstrate(string substrateName, ref Substrate substrate)
        {
            foreach (var item in CarrierSlots)
            {
                if (item.Value.MySubstrate != null &&
                    item.Value.MySubstrate.Name.Equals(substrateName))
                {
                    substrate = item.Value.MySubstrate;
                    return true;
                }
            }
                
            return false;
        }
        
        public CarrierSlotMapStates[] GetSlotMapStates()
        {
            List<CarrierSlotMapStates> slotMapStates = new List<CarrierSlotMapStates>();
            foreach (var item in CarrierSlots)
            {
                slotMapStates.Add(item.Value.SlotMapState);
            }

            return slotMapStates.ToArray();
        }

        public Substrate[] GetSubstrates()
        {
            List<Substrate> substrates = new List<Substrate>();
            foreach (var item in CarrierSlots)
            {
                substrates.Add(item.Value.MySubstrate);
            }

            return substrates.ToArray();
            //return new Dictionary<int, Substrate>(Substrates);
        }
        public void AssignSubstrate(int slot, Substrate substrate)
        {
            CarrierSlots[slot].AssignSlot(substrate);
        }
        public void RemoveSubstratesAll()
        {
            UnloadingTime = DateTime.Now;
            
            foreach (var item in CarrierSlots)
            {
                CarrierSlots[item.Key].AssignSlot(null);
            }
        }
        #endregion </Substrate>

        #endregion </Methods>
    }

    public class CarrierSlot
    {
        public Substrate MySubstrate { get; private set; }
        public CarrierSlotMapStates SlotMapState { get; private set; }
        public CarrierSlot()
        {
            MySubstrate = null;
        }

        public void AssignSlot(Substrate substrate)
        {
            MySubstrate = substrate;
        }

        public void ClearMaterial()
        {
            MySubstrate = null;
        }

        public void SetSlotMapState(CarrierSlotMapStates state)
        {
            SlotMapState = state;
        }
        public Substrate GetSubstrate()
        {
            return MySubstrate;
        }
    }
}
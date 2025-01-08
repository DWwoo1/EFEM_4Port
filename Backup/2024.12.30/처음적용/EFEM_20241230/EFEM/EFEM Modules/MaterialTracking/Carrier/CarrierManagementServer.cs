using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FrameOfSystem3.Work;

using EFEM.Defines.LoadPort;
using EFEM.MaterialTracking.CarrierAttribute;
using EFEM.MaterialTracking.LocationServer;

namespace EFEM.MaterialTracking
{
    public class CarrierManagementServer
    {
        #region <Constructors>
        private CarrierManagementServer()
        {
            Carriers = new ConcurrentDictionary<int, Carrier>();
        }
        #endregion </Constructors>

        #region <Fields>
        private static CarrierManagementServer _instance = null;
        private readonly ConcurrentDictionary<int, Carrier> Carriers = null;
        #endregion </Fields>

        #region <Properties>
        public static CarrierManagementServer Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new CarrierManagementServer();

                return _instance;
            }
        }
        #endregion </Properties>

        #region <Methods>
        public void CreateCarrier(int portId, Dictionary<int, LoadPortLocation> locations)
        {
            BaseCarrierAttribute attribute = null;
            var processType = AppConfigManager.Instance.ProcessType;
            switch (processType)
            {
                case Define.DefineEnumProject.AppConfig.EN_PROCESS_TYPE.BIN_SORTER:
                    attribute = new CustomizedByCustomer.PWA500BIN.PWA500BINCarrierAttributes(portId);
                    break;
                default:
                    break;
            }

            if (attribute != null)
                attribute.Load();

            Carriers[portId] = new Carrier(portId, attribute, locations);
        }
        public void RemoveCarrier(int portId)
        {
            if (Carriers.ContainsKey(portId))
            {
                Carriers[portId].OnCarrierRemoved();
                Carriers[portId].RemoveSubstratesAll();
                Carriers[portId].ClearAttributes();
                Carriers.TryRemove(portId, out _);
            }
        }
        public void AssignCarrier(int portId, Carrier carrier)
        {
            Carriers[portId] = carrier;
        }
        public bool HasCarrier(int portId)
        {
            return Carriers.ContainsKey(portId);
        }
        public bool SetCarrierId(int portId, string carrierId)
        {
            if (false == Carriers.ContainsKey(portId))
                return false;

            Carriers[portId].SetCarrierId(carrierId);
            return true;
        }
        public string GetCarrierId(int portId)
        {
            if (false == Carriers.ContainsKey(portId))
                return string.Empty;

            return Carriers[portId].CarrierId;
        }
        public bool SetCarrierLotId(int portId, string lotId)
        {
            if (false == Carriers.ContainsKey(portId))
                return false;

            Carriers[portId].SetLotId(lotId);
            return true;
        }
        public string GetCarrierLotId(int portId)
        {
            if (false == Carriers.ContainsKey(portId))
                return string.Empty;

            return Carriers[portId].LotId;
        }
        public int GetCapacity(int portId)
        {
            if (false == Carriers.ContainsKey(portId))
                return 0;

            return Carriers[portId].Capacity;
        }
        public void SetCarrierSlotMap(int portId, CarrierSlotMapStates[] slotMap)
        {
            if (false == Carriers.ContainsKey(portId))
                return;

            Carriers[portId].SetSlotMap(slotMap);
        }
        public CarrierSlotMapStates[] GetCarrierSlotMap(int portId)
        {
            if (false == Carriers.ContainsKey(portId))
                return null;

            return Carriers[portId].SlotState;
        }
        public CarrierAccessStates GetCarrierAccessingStatus(int portId)
        {
            if (false == Carriers.ContainsKey(portId))
                return CarrierAccessStates.Unknown;

            return Carriers[portId].AccessingStatus;
        }
        public void SetCarrierAccessingStatus(int portId, CarrierAccessStates newState)
        {
            if (false == Carriers.ContainsKey(portId))
                return;

            Carriers[portId].SetAccessingStatus(newState);
        }

        #region <Substrate>
        //public bool AssignSubstrate(int portId, int slot, ref Substrate substrate)
        //{
        //    if (false == Carriers.ContainsKey(portId))
        //        return false;

        //    Carriers[portId].AssignSubstrate(slot, ref substrate);
        //    return true;
        //}
        //public bool RemoveSubstrate(int portId, int slot)
        //{
        //    if (false == Carriers.ContainsKey(portId))
        //        return false;

        //    Carriers[portId].RemoveSubstrate(slot);
        //    return true;
        //}
        //public bool HasAnySubstrate(int portId)
        //{
        //    if (false == Carriers.ContainsKey(portId))
        //        return false;

        //    return Carriers[portId].HasAnySubstrate();
        //}

        //public bool HasSubstrateAtSlot(int portId, int slot)
        //{
        //    if (false == Carriers.ContainsKey(portId))
        //        return false;

        //    return Carriers[portId].HasSubstrateAtSlot(slot);
        //}

        //public bool GetSubstrate(int portId, int slot, ref Substrate substrate)
        //{
        //    if (false == Carriers.ContainsKey(portId))
        //        return false;

        //    return Carriers[portId].GetSubstrate(slot, ref substrate);
        //}
        //public bool GetSubstrate(int portId, string substrateName, ref Substrate substrate)
        //{
        //    if (false == Carriers.ContainsKey(portId))
        //        return false;

        //    return Carriers[portId].GetSubstrate(substrateName, ref substrate);
        //}
        //public Substrate[] GetSubstrates(int portId)
        //{
        //    if (false == Carriers.ContainsKey(portId))
        //        return null;

        //    return Carriers[portId].GetSubstrates();
        //}

        #endregion </Substrate>

        #region <Attributes>
        public bool SetAttribute(int portId, string attributeKey, string attributeValue)
        {
            if (false == HasCarrier(portId))
                return false;

            Carriers[portId].SetAttribute(attributeKey, attributeValue);
            return true;
        }
        public string GetAttribute(int portId, string attributeKey)
        {
            if (false == HasCarrier(portId))
                return string.Empty;

            return Carriers[portId].GetAttribute(attributeKey);            
        }
        #endregion </Attributes>

        #endregion </Methods>
    }
}
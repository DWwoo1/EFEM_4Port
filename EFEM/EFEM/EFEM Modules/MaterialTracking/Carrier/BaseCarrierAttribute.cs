using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using EFEM.Defines.Common;
using EFEM.Defines.LoadPort;
using EFEM.Defines.MaterialTracking;

namespace EFEM.MaterialTracking.CarrierAttribute
{
    public abstract class BaseCarrierAttribute
    {
        #region <Constructors>
        public BaseCarrierAttribute(int portId)
        {
            PortId = portId;

            RecoveryFileName = CarrierAttributeRecoveryDefines.FileNameWithExtension;
            FileStorage = new XmlFileStorage(Name);
            Attributes = new ConcurrentDictionary<string, string>();

            Attributes[BaseCarrierAttributeKeys.KeyHasCarrier] = string.Empty;
            Attributes[BaseCarrierAttributeKeys.KeyLotId] = string.Empty;
            Attributes[BaseCarrierAttributeKeys.KeyCarrierId] = string.Empty;
            Attributes[BaseCarrierAttributeKeys.KeyCarrierAccessStatus] = string.Empty;
            Attributes[BaseCarrierAttributeKeys.KeyLoadTime] = string.Empty;
            Attributes[BaseCarrierAttributeKeys.KeyUnloadTime] = string.Empty;

            Init();
        }
        #endregion </Constructors>

        #region <Fields>
        protected readonly XmlFileStorage FileStorage = null;
        protected readonly int PortId;
        protected readonly string RecoveryFileName;
        protected const string Name = "CarrierAttributes";

        private Dictionary<string, string> _temporaryAttributes = new Dictionary<string, string>();
        protected readonly ConcurrentDictionary<string, string> Attributes = null;

        private bool _hasCarrier;
        private DateTime _loadTime;
        private DateTime _unloadTime;
        private CarrierAccessStates _accessingStatus;
        #endregion </Fields>

        #region <Properties>
        public DateTime LoadTime
        {
            get
            {
                return _loadTime; 
            }
            protected set
            {
                if (false == _loadTime.Equals(value))
                {
                    _loadTime = value;
                    SetAttribute(BaseCarrierAttributeKeys.KeyLoadTime, _loadTime.ToString(BaseCarrierAttributeKeys.DateTimeFormat));
                }
            }
        }
        public DateTime UnloadTime
        {
            get 
            {
                return _unloadTime; 
            }
            protected set
            {
                if (false == _unloadTime.Equals(value))
                {
                    _unloadTime = value;
                    SetAttribute(BaseCarrierAttributeKeys.KeyUnloadTime, _unloadTime.ToString(BaseCarrierAttributeKeys.DateTimeFormat));
                }
            }
        }
        public CarrierAccessStates AccessingStatus
        {
            get 
            {
                return _accessingStatus; 
            }
            protected set
            {
                if (false == _accessingStatus.Equals(value))
                {
                    _accessingStatus = value;
                    SetAttribute(BaseCarrierAttributeKeys.KeyCarrierAccessStatus, _accessingStatus.ToString());
                }
            }
        }
        public bool HasCarrier
        {
            get
            {
                return _hasCarrier;
            }
            protected set
            {
                if (false == _hasCarrier.Equals(value))
                {
                    _hasCarrier = value;
                    SetAttribute(BaseCarrierAttributeKeys.KeyHasCarrier, _hasCarrier.ToString());
                }
            }
        }
        public string LotId
        {
            get
            {
                return Attributes[BaseCarrierAttributeKeys.KeyLotId]; 
            }
            protected set
            {
                SetAttribute(BaseCarrierAttributeKeys.KeyLotId, value);
            }
        }
        public string CarrierId
        {
            get
            {
                return Attributes[BaseCarrierAttributeKeys.KeyCarrierId];
            }
            protected set
            {
                SetAttribute(BaseCarrierAttributeKeys.KeyCarrierId, value);
            }
        }
        #endregion </Properties>

        #region <Methods>
        #region <File Control>
        public bool Load()
        {
            string filePath = string.Format(@"{0}{1}", CarrierAttributeRecoveryDefines.FilePath, PortId);
            string fullPath = string.Format(@"{0}\{1}", filePath, CarrierAttributeRecoveryDefines.FileNameWithExtension);

            try
            {
                if (false == File.Exists(fullPath))
                    return false;

                _temporaryAttributes.Clear();
                GetAttributesAll(ref _temporaryAttributes);

                bool needOverrite = false;
                var attributesFromFile = FileStorage.LoadDictionaryFromFile(fullPath);
                if(_temporaryAttributes.Count != attributesFromFile.Count)
                {
                    needOverrite = true;
                }
                SetCarrierAttributesAll(attributesFromFile);

                UpdateBaseAttributes();

                if (needOverrite)
                {
                    File.Delete(fullPath);
                    Save();
                }

                return true;
            }
            catch (Exception ex)
            {
                EFEMLogger.Instance.WriteLog(string.Format("LoadRecoveryData Exception > {0}, {1}, {2}",
                    fullPath, ex.Message, ex.StackTrace));

                return false;
            }
        }
        public bool Save()
        {
            string filePath = string.Format(@"{0}{1}", CarrierAttributeRecoveryDefines.FilePath, PortId);
            if (false == Directory.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);
            }

            string fullPath = string.Format(@"{0}\{1}", filePath, CarrierAttributeRecoveryDefines.FileNameWithExtension);
            try
            {
                _temporaryAttributes.Clear();
                GetAttributesAll(ref _temporaryAttributes);
                FileStorage.SaveChangedItemsToFile(_temporaryAttributes, fullPath);
                return true;
            }
            catch (Exception ex)
            {
                EFEMLogger.Instance.WriteLog(string.Format("SaveRecoveryData Exception > {0}, {1}, {2}",
                    fullPath, ex.Message, ex.StackTrace));

                return false;
            }
        }
        #endregion </File Control>

        #region <Base Attribute>
        public void SetCarrierPresence(bool present)
        {
            HasCarrier = present;
        }
        public void SetLotId(string lotId)
        {
            SetAttribute(BaseCarrierAttributeKeys.KeyLotId, lotId);
        }
        public void SetCarrierId(string carrierId)
        {
            SetAttribute(BaseCarrierAttributeKeys.KeyCarrierId, carrierId);
        }
        public void SetCarrierAccessStatus(CarrierAccessStates accessStatus)
        {
            AccessingStatus = accessStatus;
        }
        public void SetCarrierLoadTime(DateTime dateTime)
        {
            SetAttribute(BaseCarrierAttributeKeys.KeyLoadTime, dateTime.ToString(BaseCarrierAttributeKeys.DateTimeFormat));
        }
        public void SetCarrierUnloadTime(DateTime dateTime)
        {
            SetAttribute(BaseCarrierAttributeKeys.KeyUnloadTime, dateTime.ToString(BaseCarrierAttributeKeys.DateTimeFormat));
        }
        public void SetCarrierAttributes(string attributeName, string attributeValue)
        {
            SetAttribute(attributeName, attributeValue);
        }
        #endregion </Base Attribute>

        #region <Absolute Methods>
        protected abstract void InitAttributes(ref Dictionary<string, string> additionalAttributes);
        public abstract void OnCarrierCreated();
        public abstract void OnCarrierRemoved();
        #endregion </Absolute Methods>

        #region <Internals>
        public void Init()
        {
            Attributes[BaseCarrierAttributeKeys.KeyLotId] = string.Empty;
            Attributes[BaseCarrierAttributeKeys.KeyCarrierId] = string.Empty;

            _hasCarrier = false;
            _accessingStatus = CarrierAccessStates.Unknown;
            _loadTime = DateTime.Now;
            _unloadTime = DateTime.Now;
            
            Dictionary<string, string> additionalAttributes = new Dictionary<string, string>();
            InitAttributes(ref additionalAttributes);
            foreach (var item in additionalAttributes)
            {
                Attributes[item.Key] = item.Value;
            }
        }
        protected void SetAttribute(string key, string value)
        {
            if (Attributes.TryGetValue(key, out string temporaryValue))
            {
                if (false == value.Equals(temporaryValue))
                {
                    Attributes[key] = value;
                    Save();
                }
            }
        }
        private void UpdateBaseAttributes()
        {
            if (Attributes.TryGetValue(BaseCarrierAttributeKeys.KeyHasCarrier, out string hasCarrier))
            {
                bool.TryParse(hasCarrier, out _hasCarrier);
            }

            if (Attributes.TryGetValue(BaseCarrierAttributeKeys.KeyCarrierAccessStatus, out string accessStatus))
            {
                if (false == Enum.TryParse(accessStatus, out _accessingStatus))
                    _accessingStatus = CarrierAccessStates.Unknown;
            }

            if (Attributes.TryGetValue(BaseCarrierAttributeKeys.KeyLoadTime, out string loadTime))
            {
                if (false == DateTime.TryParse(loadTime, out _loadTime))
                    _loadTime = DateTime.Now;
            }

            if (Attributes.TryGetValue(BaseCarrierAttributeKeys.KeyUnloadTime, out string unloadTime))
            {
                if (false == DateTime.TryParse(unloadTime, out _unloadTime))
                    _unloadTime = DateTime.Now;
            }
        }
        public string GetCarrierAttributes(string attributeName)
        {
            if (Attributes.TryGetValue(attributeName, out string value))
                return value;

            return string.Empty;
        }
        public void ClearCarrierAttributes()
        {
            foreach (var item in Attributes)
            {
                Attributes[item.Key] = string.Empty;
            }

            UpdateBaseAttributes();
        }
        public void GetAttributesAll(ref Dictionary<string, string> attributes)
        {
            foreach (var item in Attributes)
            {
                attributes[item.Key] = item.Value;
            }
        }
        public void SetCarrierAttributesAll(Dictionary<string, string> attributes)
        {
            foreach (var item in attributes)
            {
                Attributes[item.Key] = item.Value;
            }
        }
        #endregion </Internals>
        #endregion </Methods>
    }
}

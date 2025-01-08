using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Linq;

using FrameOfSystem3.Work;
using EFEM.Defines.Common;
using EFEM.Defines.MaterialTracking;
using EFEM.MaterialTracking.LocationServer;
using EFEM.MaterialTracking.Attributes;

namespace EFEM.MaterialTracking
{
    public class Substrate
    {
        #region <Constructors>
        public Substrate(string name)
        {
            FileStorage = new XmlFileStorage(Name);

            _locationServer = LocationServer.LocationServer.Instance;

            //AdditionalAttributes.Attributes = new Dictionary<string, string>();
            switch (AppConfigManager.Instance.ProcessType)
            {
                case Define.DefineEnumProject.AppConfig.EN_PROCESS_TYPE.NONE:
                    break;
                case Define.DefineEnumProject.AppConfig.EN_PROCESS_TYPE.BIN_SORTER:
                    AdditionalAttributes = new EFEM.CustomizedByCustomer.PWA500BIN.PWA500BINAttributes();
                    break;

            }

            InitInformation(name);

            _location = new Location("UnknownLocation");
            // CarrierId
            // Port
            // Slot
            // DestPort
            // Dest Slot
            // CJ, PJ
            // PM
            // Recipe Id
            // Transport State
            // Processing State
            // Lot Id
            // Usage
            // DoNotProcessFlag
        }
        #endregion </Constructors>

        #region <Fields>
        private readonly BaseAdditionalAttributes AdditionalAttributes = null;
        //private ConcurrentDictionary<string, object> _additionalAttributes
        //    = new ConcurrentDictionary<string, object>();
        //private Dictionary<string, string> AdditionalAttributes.Attributes = null;
        //private Dictionary<string, string> AdditionalAttributes.Attributes = null;
        private static LocationServer.LocationServer _locationServer = null;
        private readonly XmlFileStorage FileStorage = null;

        private Location _location = null;
        private const string Name = "SubstrateAttributes";
        #endregion </Fields>

        #region <Methods>

        #region <File Control>
        public bool LoadRecoveryData(string fileName)
        {
            bool hasExtension = Path.GetExtension(fileName).Equals(string.Format(".{0}", RecoveryFileDefines.FileExtension));
            string fileNameWithExtension;
            if (hasExtension)
                fileNameWithExtension = fileName;
            else
                fileNameWithExtension = string.Format("{0}.{1}", fileName, RecoveryFileDefines.FileExtension);

            try
            {
                string fullPath = string.Format(@"{0}\{1}", RecoveryFileDefines.RecoveryFilePath, fileNameWithExtension);
                if (false == File.Exists(fullPath))
                    return false;

                var attributesFromFile = FileStorage.LoadDictionaryFromFile(fullPath);
                foreach (var item in attributesFromFile)
                {
                    if (AdditionalAttributes.Attributes.ContainsKey(item.Key))
                        AdditionalAttributes.Attributes[item.Key] = item.Value;
                }

                string location = GetAttribute(BaseSubstrateAttributeKeys.Location);

                return _locationServer.GetLocationByName(location, ref _location);
            }
            catch (Exception ex)
            {
                EFEMLogger.Instance.WriteLog(string.Format("LoadRecoveryData Exception > {0}, {1}, {2}",
                    fileNameWithExtension, ex.Message, ex.StackTrace));

                return false;
            }
        }
        public bool SaveRecoveryData()
        {
            string fileNameWithExtension = string.Format("{0}.{1}", GetName(), RecoveryFileDefines.FileExtension);

            if (string.IsNullOrEmpty(GetName()))
                return false;
            try
            {
                string fullPath = string.Format(@"{0}\{1}", RecoveryFileDefines.RecoveryFilePath, fileNameWithExtension);
                FileStorage.SaveChangedItemsToFile(AdditionalAttributes.Attributes, fullPath);
                return true;
            }
            catch (Exception ex)
            {
                EFEMLogger.Instance.WriteLog(string.Format("SaveRecoveryData Exception > {0}, {1}, {2}",
                    fileNameWithExtension, ex.Message, ex.StackTrace));

                return false;
            }
        }
        public bool DeleteRecoveryData()
        {
            string fileNameWithExtension = string.Format("{0}.{1}", GetName(), RecoveryFileDefines.FileExtension);
            try
            {
                string fullPath = string.Format(@"{0}\{1}", RecoveryFileDefines.RecoveryFilePath, fileNameWithExtension);
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }
            catch (Exception ex)
            {
                EFEMLogger.Instance.WriteLog(string.Format("DeleteRecoveryData Exception > {0}, {1}, {2}",
                    fileNameWithExtension, ex.Message, ex.StackTrace));

                return false;
            }

            return true;
        }

        #endregion </File Control>

        public string GetName()
        {
            return AdditionalAttributes.Attributes[BaseSubstrateAttributeKeys.Name];
        }
        public void SetName(string name)
        {
            SetAttribute(BaseSubstrateAttributeKeys.Name, name);
        }
        public int GetSourcePortId()
        {
            int.TryParse(AdditionalAttributes.Attributes[BaseSubstrateAttributeKeys.SourcePortId], out int portId);

            return portId;
        }
        public void SetSourcePortId(int portId)
        {
            SetAttribute(BaseSubstrateAttributeKeys.SourcePortId, portId.ToString());
        }
        public int GetSourceSlot()
        {
            int.TryParse(AdditionalAttributes.Attributes[BaseSubstrateAttributeKeys.SourceSlot], out int slot);

            return slot;
        }
        public void SetSourceSlot(int slot)
        {
            SetAttribute(BaseSubstrateAttributeKeys.SourceSlot, slot.ToString());
        }
        public int GetDestinationPortId()
        {
            int.TryParse(AdditionalAttributes.Attributes[BaseSubstrateAttributeKeys.DestinationPortId], out int portId);

            return portId;
        }
        public void SetDestinationPortId(int portId)
        {
            SetAttribute(BaseSubstrateAttributeKeys.DestinationPortId, portId.ToString());
        }
        public int GetDestinationSlot()
        {
            int.TryParse(AdditionalAttributes.Attributes[BaseSubstrateAttributeKeys.DestinationSlot], out int slot);

            return slot;
        }
        public void SetDestinationSlot(int slot)
        {
            SetAttribute(BaseSubstrateAttributeKeys.DestinationSlot, slot.ToString());
        }
        public Location GetLocation()
        {
            return _location;
        }
        public void SetLocation(Location location)
        {
            _location = location;
            SetAttribute(BaseSubstrateAttributeKeys.Location, location.Name);
        }
        //

        // 상태
        public SubstrateTransferStates GetTransferStatus()
        {
            Enum.TryParse(AdditionalAttributes.Attributes[BaseSubstrateAttributeKeys.TransPortState],
                                out SubstrateTransferStates status);

            return status;
        }
        public void SetTransferStatus(SubstrateTransferStates state)
        {
            SetAttribute(BaseSubstrateAttributeKeys.TransPortState, state.ToString());
        }
        public ProcessingStates GetProcessingStatus()
        {
            Enum.TryParse(AdditionalAttributes.Attributes[BaseSubstrateAttributeKeys.ProcessingState],
                                out ProcessingStates status);

            return status;
        }
        public void SetProcessingStatus(ProcessingStates state)
        {
            SetAttribute(BaseSubstrateAttributeKeys.ProcessingState, state.ToString());
        }
        public IdReadingStates GetIdReadingStatus()
        {
            Enum.TryParse(AdditionalAttributes.Attributes[BaseSubstrateAttributeKeys.IdReadingState],
                                out IdReadingStates status);

            return status;
        }
        public void SetIdReadingStatus(IdReadingStates state)
        {
            SetAttribute(BaseSubstrateAttributeKeys.IdReadingState, state.ToString());
        }

        public string GetLotId()
        {
            return AdditionalAttributes.Attributes[BaseSubstrateAttributeKeys.LotId];
        }
        public void SetLotId(string lotId)
        {
            SetAttribute(BaseSubstrateAttributeKeys.LotId, lotId);
        }
        public string GetSourceCarrierId()
        {
            return AdditionalAttributes.Attributes[BaseSubstrateAttributeKeys.SourceCarrierId];
        }
        public void SetSourceCarrierId(string carrierId)
        {
            SetAttribute(BaseSubstrateAttributeKeys.SourceCarrierId, carrierId);
        }
        public string GetRecipeId()
        {
            return AdditionalAttributes.Attributes[BaseSubstrateAttributeKeys.RecipeId];
        }
        public void SetRecipeId(string recipeId)
        {
            SetAttribute(BaseSubstrateAttributeKeys.RecipeId, recipeId);
        }
        public void SetLoadingTimeToSource()
        {
            SetAttribute(BaseSubstrateAttributeKeys.LoadingTimeToSource, DateTime.Now.ToString(ETC.DateTimeFormat));
        }
        public void SetUnladingTimeFromSource()
        {
            SetAttribute(BaseSubstrateAttributeKeys.UnloadingTimeFromSource, DateTime.Now.ToString(ETC.DateTimeFormat));
        }
        public void SetLoadingTimeToDestination()
        {
            SetAttribute(BaseSubstrateAttributeKeys.LoadingTimeToDestination, DateTime.Now.ToString(ETC.DateTimeFormat));
        }
        public void SetUnladingTimeFromDestination()
        {
            SetAttribute(BaseSubstrateAttributeKeys.UnloadingTimeFromDestination, DateTime.Now.ToString(ETC.DateTimeFormat));
        }
        public void SetLoadingTimeToProcessModule(string name)
        {
            string attrName = string.Format("{0}{1}", BaseSubstrateAttributeKeys.LoadingTimeToPM, name);
            SetAttribute(attrName, DateTime.Now.ToString(ETC.DateTimeFormat));
        }
        public void SetUnloadingTimeFromProcessModule(string name)
        {
            string attrName = string.Format("{0}{1}", BaseSubstrateAttributeKeys.UnloadingTimeFromPM, name);
            SetAttribute(attrName, DateTime.Now.ToString(ETC.DateTimeFormat));
        }
        private void InitInformation(string name)
        {
            if (false == string.IsNullOrEmpty(name))
            {
                SetName(name);
                SetTransferStatus(SubstrateTransferStates.AtSource);
                SetProcessingStatus(ProcessingStates.NeedsProcessing);
                SetIdReadingStatus(IdReadingStates.NotConfirmed);

                string[] attributes = AdditionalAttributes.Attributes.Keys.ToArray();
                for(int i = 0; i < attributes.Length; ++i)
                {
                    if (AdditionalAttributes.Attributes[attributes[i]] == null)
                    {
                        SetAttribute(attributes[i], string.Empty);
                    }
                }    
                SaveRecoveryData();
            }
        }
        public void InitAttributes()
        {
            AdditionalAttributes.InitAddirionalAttributes();

            SaveRecoveryData();
        }
        public string GetAttribute(string key)
        {
            if (AdditionalAttributes == null)
                return string.Empty;

            if (false == AdditionalAttributes.Attributes.TryGetValue(key, out string value))
                return string.Empty;

            return value;
        }
        public void SetAttribute(string key, string value)
        {
            if (AdditionalAttributes == null)
                return;

            if (key.Equals(BaseSubstrateAttributeKeys.Name))
            {
                DeleteRecoveryData();
            }

            if (value == null)
                value = string.Empty;

            AdditionalAttributes.Attributes[key] = value;
            SaveRecoveryData();
        }
        public Dictionary<string, string> GetAttributesAll()
        {
            var attributes = new Dictionary<string, string>(AdditionalAttributes.Attributes);

            return attributes;
        }
        public bool SetAttributesAll(Dictionary<string, string> newAttributes)
        {
            if (false == AdditionalAttributes.Attributes[BaseSubstrateAttributeKeys.Name].Equals(newAttributes[BaseSubstrateAttributeKeys.Name]))
            {
                DeleteRecoveryData();
            }

            foreach (var item in newAttributes)
            {
                if (false == AdditionalAttributes.Attributes.ContainsKey(item.Key))
                    return false;

                AdditionalAttributes.Attributes[item.Key] = item.Value;
            }

            SaveRecoveryData();

            return true;
        }
        public bool NeedToUnload()
        {
            switch (GetProcessingStatus())
            {
                case ProcessingStates.NeedsProcessing:
                case ProcessingStates.InProcess:
                case ProcessingStates.Lost:
                    return false;
                case ProcessingStates.Processed:
                case ProcessingStates.Rejected:
                case ProcessingStates.Stopped:
                case ProcessingStates.Aborted:
                case ProcessingStates.Skipped:
                    return true;

                default:
                    return false;
            }
        }
        #endregion </Methods>
    }
}
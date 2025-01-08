using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FrameOfSystem3.AppConfig;
using EFEM.Defines.MaterialTracking;
using EFEM.MaterialTracking.Attributes;

namespace EFEM.MaterialTracking
{
    public class Substrate
    {
        #region <Constructors>
        public Substrate(string name)
        {
            _locationServer = LocationServer.LocationServer.Instance;

            switch (AppConfigManager.Instance.ProcessType)
            {
                case Define.DefineEnumProject.AppConfig.EN_PROCESS_TYPE.NONE:
                    break;
                case Define.DefineEnumProject.AppConfig.EN_PROCESS_TYPE.BIN_SORTER:
                    AdditionalAttributes = new PWA500BINAttributes();
                    break;
                
            }

            InitInformation(name);

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

        private static LocationServer.LocationServer _locationServer = null;
        #endregion </Fields>

        #region <Properties>
        public string Name { get; private set; }

        // 위치 정보
        public int SourcePortId { get; private set; }
        public int SourceSlot { get; private set; }
        public int DestinationPortId { get; private set; }
        public int DestinationSlot { get; private set; }
        public string Location { get; private set; }
        //

        // 상태
        public SubstrateTransferStates TransportState { get; private set; }
        public ProcessingStates ProcessingState { get; private set; }
        public IdReadingStates IdReadingState { get; private set; }
        //

        // 공정 정보
        public string LotId { get; private set; }
        public string RecipeId { get; private set; }
        //
        #endregion </Properties>

        #region <Methods>
        private void InitInformation(string name)
        {
            Name = name;

            TransportState = SubstrateTransferStates.AtSource;
            ProcessingState = ProcessingStates.NeedsProcessing;
            IdReadingState = IdReadingStates.NotConfirmed;
        }
        public void SetSourceCarrierInfo(int portId, int slot)
        {
            SourcePortId = portId;
            SourceSlot = slot;
        }
        public void SetDestinationCarrierInfo(int portId, int slot)
        {
            DestinationPortId = portId;
            DestinationSlot = slot;
        }
        public void SetLocation(string locationName)
        {
            Location = locationName;
        }
        public void SetInformation(string lotId, string recipeId)
        {
            LotId = lotId;
            RecipeId = recipeId;
        }

        public void SetTransferState(SubstrateTransferStates state)
        {
            TransportState = state;
        }
        public void SetState(ProcessingStates state)
        {
            ProcessingState = state;
        }
        public void InitAttributes(int portId)
        {
            AdditionalAttributes.InitAttributes(portId);
        }

        public string GetAttribute(string key)
        {
            if (AdditionalAttributes == null)
                return string.Empty;

            return AdditionalAttributes.GetAttributes(key);
        }
        public void SetAttribute(string key, string value)
        {
            if (AdditionalAttributes == null)
                return;

            AdditionalAttributes.SetAttributes(key, value);
        }
        public Dictionary<string, string> GetAttributesAll()
        {
            return AdditionalAttributes.GetAttributesAll();
        }
        #endregion </Methods>
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EFEM.Defines.LoadPort;
using EFEM.Defines.MaterialTracking;
using EFEM.MaterialTracking.CarrierAttribute;
using EFEM.MaterialTracking;

namespace EFEM.CustomizedByProcessType.PWA500BIN
{
    public class PWA500BINCarrierAttributes : BaseCarrierAttribute
    {
        #region <Constructors>
        public PWA500BINCarrierAttributes(int portId) : base(portId) { }
        #endregion </Constructors>

        #region <Fields>
        #endregion </Fields>

        #region <Properties>
        #endregion </Properties>

        #region <Methods>
        protected override void InitAttributes(ref Dictionary<string, string> additionalAttributes)
        {
            additionalAttributes[PWA500BINCarrierAttributeKeys.KeyPartId] = string.Empty;
            additionalAttributes[PWA500BINCarrierAttributeKeys.KeyStepSeq] = string.Empty;
            additionalAttributes[PWA500BINCarrierAttributeKeys.KeySlotMappingCompletion] = "False";
            additionalAttributes[PWA500BINCarrierAttributeKeys.KeyLotMergeCompletion] = "False";
            additionalAttributes[PWA500BINCarrierAttributeKeys.KeyLotIdChangeCompletion] = "False";
        }
        public override void OnCarrierCreated()
        {
        }
        public override void OnCarrierRemoved()
        {
        }
        #endregion </Methods>
    }
}

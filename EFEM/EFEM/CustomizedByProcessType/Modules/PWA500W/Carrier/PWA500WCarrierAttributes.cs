using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EFEM.Defines.LoadPort;
using EFEM.Defines.MaterialTracking;
using EFEM.MaterialTracking.CarrierAttribute;
using EFEM.MaterialTracking;

namespace EFEM.CustomizedByProcessType.PWA500W
{
    public class PWA500WCarrierAttributes : BaseCarrierAttribute
    {
        #region <Constructors>
        public PWA500WCarrierAttributes(int portId) : base(portId) { }
        #endregion </Constructors>

        #region <Fields>
        #endregion </Fields>

        #region <Properties>
        #endregion </Properties>

        #region <Methods>
        protected override void InitAttributes(ref Dictionary<string, string> additionalAttributes)
        {
            additionalAttributes[PWA500WCarrierAttributeKeys.KeyPartId] = string.Empty;
            additionalAttributes[PWA500WCarrierAttributeKeys.KeyStepSeq] = string.Empty;
            additionalAttributes[PWA500WCarrierAttributeKeys.KeySlotMappingCompletion] = "False";
            additionalAttributes[PWA500WCarrierAttributeKeys.KeyLotMergeCompletion] = "False";
            additionalAttributes[PWA500WCarrierAttributeKeys.KeyLotIdChangeCompletion] = "False";
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

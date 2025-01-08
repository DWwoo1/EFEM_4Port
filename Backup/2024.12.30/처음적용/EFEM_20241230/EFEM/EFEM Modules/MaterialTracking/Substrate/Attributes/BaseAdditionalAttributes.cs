using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EFEM.Defines.MaterialTracking;

namespace EFEM.MaterialTracking.Attributes
{
    public abstract class BaseAdditionalAttributes
    {
        public readonly Dictionary<string, string> Attributes = null;

        public BaseAdditionalAttributes()
        {
            Attributes = new Dictionary<string, string>();

            AddBaseAttributes();
        }

        public abstract void InitAddirionalAttributes();

        private void AddBaseAttributes()
        {
            Attributes[BaseSubstrateAttributeKeys.Name] = string.Empty;
            Attributes[BaseSubstrateAttributeKeys.LotId] = string.Empty;
            Attributes[BaseSubstrateAttributeKeys.SourceCarrierId] = string.Empty;
            Attributes[BaseSubstrateAttributeKeys.SourcePortId] = string.Empty;
            Attributes[BaseSubstrateAttributeKeys.SourceSlot] = string.Empty;
            Attributes[BaseSubstrateAttributeKeys.DestinationPortId] = string.Empty;
            Attributes[BaseSubstrateAttributeKeys.DestinationSlot] = string.Empty;
            Attributes[BaseSubstrateAttributeKeys.Location] = string.Empty;
            Attributes[BaseSubstrateAttributeKeys.ProcessJobId] = string.Empty;
            Attributes[BaseSubstrateAttributeKeys.ControlJobId] = string.Empty;
            Attributes[BaseSubstrateAttributeKeys.RecipeId] = string.Empty;
            Attributes[BaseSubstrateAttributeKeys.TransPortState] = string.Empty;
            Attributes[BaseSubstrateAttributeKeys.ProcessingState] = string.Empty;
            Attributes[BaseSubstrateAttributeKeys.Usage] = string.Empty;
            Attributes[BaseSubstrateAttributeKeys.DoNotProcessFlag] = string.Empty;

            Attributes[BaseSubstrateAttributeKeys.LoadingTimeToSource]          = DateTime.Now.ToString(ETC.DateTimeFormat);
            Attributes[BaseSubstrateAttributeKeys.LoadingTimeToDestination]     = DateTime.Now.ToString(ETC.DateTimeFormat);
            Attributes[BaseSubstrateAttributeKeys.UnloadingTimeFromSource]      = DateTime.Now.ToString(ETC.DateTimeFormat);
            Attributes[BaseSubstrateAttributeKeys.UnloadingTimeFromDestination] = DateTime.Now.ToString(ETC.DateTimeFormat);

            for (int i = 0; i < Modules.ProcessModuleGroup.Instance.Count; ++i)
            {
                string pmName = Modules.ProcessModuleGroup.Instance.GetProcessModuleName(i);
                string attrNameLoading = string.Format("{0}{1}", BaseSubstrateAttributeKeys.LoadingTimeToPM, pmName);
                Attributes[attrNameLoading] = DateTime.Now.ToString(ETC.DateTimeFormat);

                string attrNameUnloading = string.Format("{0}{1}", BaseSubstrateAttributeKeys.UnloadingTimeFromPM, pmName);
                Attributes[attrNameUnloading] = DateTime.Now.ToString(ETC.DateTimeFormat);
            }
        }
    }
}

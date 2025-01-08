using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EFEM.Defines.MaterialTracking;
using EFEM.MaterialTracking.Attributes;

namespace EFEM.CustomizedByCustomer.PWA500BIN
{
    public class PWA500BINAttributes : BaseAdditionalAttributes
    {
        public PWA500BINAttributes() : base()
        {
            Attributes[PWA500BINSubstrateAttributes.SubstrateType] = SubstrateType.Empty.ToString();
            Attributes[PWA500BINSubstrateAttributes.RingId] = string.Empty;

            Attributes[PWA500BINSubstrateAttributes.PartId] = string.Empty;
            Attributes[PWA500BINSubstrateAttributes.LotType] = string.Empty;
            Attributes[PWA500BINSubstrateAttributes.StepSeq] = string.Empty;
            Attributes[PWA500BINSubstrateAttributes.ChipQty] = string.Empty;
            Attributes[PWA500BINSubstrateAttributes.BinCode] = string.Empty;
            
            Attributes[PWA500BINSubstrateAttributes.RefPositionX] = string.Empty;
            Attributes[PWA500BINSubstrateAttributes.RefPositionY] = string.Empty;
            Attributes[PWA500BINSubstrateAttributes.StartingPositionX] = string.Empty;
            Attributes[PWA500BINSubstrateAttributes.StartingPositionY] = string.Empty;
            Attributes[PWA500BINSubstrateAttributes.CountX] = string.Empty;
            Attributes[PWA500BINSubstrateAttributes.CountY] = string.Empty;
            Attributes[PWA500BINSubstrateAttributes.Angle] = string.Empty;
            Attributes[PWA500BINSubstrateAttributes.MapData] = string.Empty;
            Attributes[PWA500BINSubstrateAttributes.SplittedLotId] = string.Empty;
            Attributes[PWA500BINSubstrateAttributes.IsLastSubstrate] = "False";
            //SetAttributes(SubstrateType., string.Empty);
        }

        public override void InitAddirionalAttributes()
        {
            if (false == int.TryParse(Attributes[BaseSubstrateAttributeKeys.SourcePortId], out int portId))
                return;

            string substrateType = PWA500BINSubstrateAttributes.SubstrateType;
            switch (portId)
            {
                case 1:
                    Attributes[substrateType] = SubstrateType.Bin1.ToString();
                    break;
                case 2:
                    Attributes[substrateType] = SubstrateType.Bin2.ToString();
                    break;
                case 3:
                    Attributes[substrateType] = SubstrateType.Bin3.ToString();
                    break;
                case 4:
                    Attributes[substrateType] = SubstrateType.Empty.ToString();
                    break;
                case 5:
                case 6:
                    Attributes[substrateType] = SubstrateType.Core.ToString();
                    break;
                default:
                    break;
            }
        }
    }
}
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EFEM.Defines.MaterialTracking;
using EFEM.MaterialTracking.Attributes;

namespace EFEM.CustomizedByProcessType.PWA500W
{
    public class PWA500WAttributes : BaseAdditionalAttributes
    {
        public PWA500WAttributes() : base()
        {
            Attributes[PWA500WSubstrateAttributes.SubstrateType] = SubstrateType.Core_8.ToString();
            Attributes[PWA500WSubstrateAttributes.RingId] = string.Empty;

            Attributes[PWA500WSubstrateAttributes.PartId] = string.Empty;
            Attributes[PWA500WSubstrateAttributes.LotType] = string.Empty;
            Attributes[PWA500WSubstrateAttributes.StepSeq] = string.Empty;
            Attributes[PWA500WSubstrateAttributes.ChipQty] = "0";
            Attributes[PWA500WSubstrateAttributes.BinCode] = string.Empty;
            
            Attributes[PWA500WSubstrateAttributes.RefPositionX] = string.Empty;
            Attributes[PWA500WSubstrateAttributes.RefPositionY] = string.Empty;
            Attributes[PWA500WSubstrateAttributes.StartingPositionX] = string.Empty;
            Attributes[PWA500WSubstrateAttributes.StartingPositionY] = string.Empty;
            Attributes[PWA500WSubstrateAttributes.CountX] = string.Empty;
            Attributes[PWA500WSubstrateAttributes.CountY] = string.Empty;
            Attributes[PWA500WSubstrateAttributes.Angle] = string.Empty;
            Attributes[PWA500WSubstrateAttributes.MapData] = string.Empty;
            Attributes[PWA500WSubstrateAttributes.ParentLotId] = string.Empty;
            Attributes[PWA500WSubstrateAttributes.SplittedLotId] = string.Empty;
            Attributes[PWA500WSubstrateAttributes.IsLastSubstrate] = bool.FalseString;
            Attributes[PWA500WSubstrateAttributes.IsTrackOutCompleted] = bool.FalseString;
            Attributes[PWA500WSubstrateAttributes.BinUnloadingStep] = "0";
            //SetAttributes(SubstrateType., string.Empty);
        }

        public override void InitAddirionalAttributes()
        {
            if (false == int.TryParse(Attributes[BaseSubstrateAttributeKeys.SourcePortId], out int portId))
                return;

            string substrateType = PWA500WSubstrateAttributes.SubstrateType;
            switch (portId)
            {
                case 1:
                    Attributes[substrateType] = SubstrateType.Sort_12.ToString();
                    break;
                case 2:
                    Attributes[substrateType] = SubstrateType.Core_12.ToString();
                    break;
                case 3:
                case 4:
                    Attributes[substrateType] = SubstrateType.Core_8.ToString();
                    break;
                default:
                    break;
            }
        }
    }
}
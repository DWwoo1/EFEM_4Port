using System;
using System.Collections.Concurrent;

namespace EFEM.CustomizedByProcessType.PWA500W
{
    #region <Constants>
    public static class Constants
    {
        public static readonly string LoadingToken = "Input";
        public static readonly string UnloadingToken = "Output";

        public const string ProcessModuleCore_8_InputName = "PM1.Core_8_Input";
        public const string ProcessModuleCore_8_OutputName = "PM1.Core_8_Output";
        public const string ProcessModuleCore_12_InputName = "PM1.Core_12_Input";
        public const string ProcessModuleCore_12_OutputName = "PM1.Core_12_Output";
        public const string ProcessModuleSort_12_InputName = "PM1.Sort_12_Input";
        public const string ProcessModuleSort_12_OutputName = "PM1.Sort_12_Output";

        public static readonly string Core_8_Name = "Core_8";
        public static readonly string Core_12_Name = "Core_12";
        public static readonly string Sort_12_Name = "Sort_12";

        public const string EmptyWaferChangeReason = "FINISH_CHANGE";
        public const string EmptyWaferMaterialType = "TM_TAPE";
    }
    public static class PWA500WCarrierAttributeKeys
    {       
        public const string KeyPartId = "PartId";
        public const string KeyStepSeq = "StepSeq";
        public const string KeyLotType = "LotType";
        public const string KeySlotMappingCompletion = "SlotMappingCompletion";
        public const string KeyLotMergeCompletion   = "LotMergeCompletion";
        public const string KeyLotIdChangeCompletion = "LotIdChangeCompletion";
    }
    public static class PWA500WSubstrateAttributes
    {
        public const string HandlingResult = "HandlingResult";
        public const string SubstrateName = "SubstrateName";
        public const string LotId = "LotId";
        public const string RecipeId = "RecipeId";

        public const string SubstrateType = "SubstrateType";
        public const string RingId = "RingId";
        public const string PortId = "PortId";
        public const string SlotId = "SlotId";

        public const string PartId = "PartId";
        public const string LotType = "LotType";
        public const string StepSeq = "StepSeq";
        public const string ChipQty = "ChipQty";
        public const string BinCode = "BinCode";

        public const string RefPositionX = "RefPositionX";
        public const string RefPositionY = "RefPositionY";
        public const string StartingPositionX = "StartingPositionX";
        public const string StartingPositionY = "StartingPositionY";
        public const string CountX = "CountX";
        public const string CountY = "CountY";
        public const string Angle = "Angle";
        public const string MapData = "MapData";

        public const string ParentLotId = "ParentLotId";
        public const string SplittedLotId = "SplittedLotId";
        public const string IsLastSubstrate = "IsLastSubstrate";

        public const string BinUnloadingStep = "BinUnloadingStep";
        public const string IsTrackOutCompleted = "IsTrackOutCompleted";

        //public const string SubstrateTypeCore = "Core";
        //public const string SubstrateTypeEmpty = "Empty";
        //public const string SubstrateTypeBin1 = "Bin1";
        //public const string SubstrateTypeBin2 = "Bin2";
        //public const string SubstrateTypeBin3 = "Bin3";

        public const string HandlingResultOk = "Ok";
        public const string HandlingResultNg = "Ng";
    }
    #endregion </Constants>

    #region <Enumerations>
    public enum SubstrateTypeForUI
    {
        Core_8,
        Core_12,
        Sort_12,
    }
    public enum SubstrateType
    {
        Core_8,
        Core_12,
        Sort_12,
    }
    public enum SubstrateTypeForControl
    {
        Core_8,
        Core_12,
        Sort_12,
    }
    public enum WCFServiceIndex
    {
        //SecsGem,
        EFEM,
        //CoreIn,
        //SortIn,
        //CoreOut,
        //SortOut,
    }
    public enum WCFClientIndex
    {
        //Main,
        Core_8_In,
        Core_8_Out,
        Core_12_In,
        Core_12_Out,
        Sort_12_In,
        Sort_12_Out,
    }
    public enum ProcessModuleEntryWays
    {
        Core_8_In = 0,
        Core_12_In,
        Sort_12_In,
        Core_8_Out,
        Core_12_Out,
        Sort_12_Out,
        //Core_8_In = 0,
        //Core_8_Out,
        //Core_12_In,
        //Core_12_Out,
        //Sort_12_In,
        //Sort_12_Out,
    }
    public enum LoadPortType
    {
        Sort_12 = 0,
        Core_12,
        Core_8_1,
        Core_8_2,
    }
    public enum RequestMessages
    {
        RequestApproachLoading,
        RequestActionLoading,
        RequestConfirmLoading,
     
        RequestApproachUnloading,
        RequestActionUnloading,
        RequestConfirmUnloading,

        RequestStartUnloading,

        RequastMaterialInfo,
    }

    public enum ResponseMessages
    {
        ResponseApproachLoading,
        ResponseActionLoading,
        ResponseConfirmLoading,
        
        ResponseApproachUnloading,
        ResponseActionUnloading,
        ResponseConfirmUnloading,

        ResponseStartUnloading,

        ResponseMaterialOnfo,
    }
    #endregion </Enumerations>
}

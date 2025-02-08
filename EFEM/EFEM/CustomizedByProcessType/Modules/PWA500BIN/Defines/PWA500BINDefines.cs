using System;
using System.Collections.Concurrent;

namespace EFEM.CustomizedByProcessType.PWA500BIN
{
    #region <Constants>
    public static class Constants
    {
        public static readonly string LoadingToken = "Input";
        public static readonly string UnloadingToken = "Output";

        public const string ProcessModuleCoreInputName = "PM1.CoreInput";
        public const string ProcessModuleSortInputName = "PM1.SortInput";
        public const string ProcessModuleCoreOutputName = "PM1.CoreOutput";
        public const string ProcessModuleSortOutputName = "PM1.SortOutput";

        public static readonly string CoreName = "Core";
        public static readonly string SortName = "Sort";

        public const string EmptyWaferChangeReason = "FINISH_CHANGE";
        public const string EmptyWaferMaterialType = "TM_TAPE";
    }
    public static class PWA500BINCarrierAttributeKeys
    {       
        public const string KeyPartId = "PartId";
        public const string KeyStepSeq = "StepSeq";
        public const string KeySlotMappingCompletion = "SlotMappingCompletion";
        public const string KeyLotMergeCompletion   = "LotMergeCompletion";
        public const string KeyLotIdChangeCompletion = "LotIdChangeCompletion";
    }
    public static class PWA500BINSubstrateAttributes
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
        Core,
        Empty,
        StageCenter,
        StageLeft,
        StageRight,
    }
    public enum SubstrateType
    {
        Core,
        Empty,
        Bin1,
        Bin2,
        Bin3,
    }
    public enum SubstrateTypeForControl
    {
        Core,
        EmptyTape,
        Bin,
        //All
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
        SortIn,
        CoreIn,
        CoreOut,
        SortOut,
    }
    public enum ProcessModuleEntryWays
    {
        CoreIn = 0,
        SortIn,
        CoreOut,
        SortOut,
    }
    public enum LoadPortType
    {
        Bin_3 = 0,
        Bin_2,
        Bin_1,
        EmptyTape,
        Core_2,
        Core_1,
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

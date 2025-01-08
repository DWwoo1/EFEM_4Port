using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FrameOfSystem3.SECSGEM.DefineSecsGem;

namespace EFEM.CustomizedByProcessType.PWA500W
{
    #region <Param Range>
    public class ParamRange : ParamRangeBase
    {
        public override int SvidStart { get { return 0; } }
        public override int SvidEnd { get { return 0; } }
        public override int EcidStart { get { return 100000; } }
        public override int EcidCommonStart { get { return 0; } }
        public override int EcidCommonEnd { get { return 0; } }
        public override int EcidEquipStart { get { return 100000; } }
        public override int EcidEquipEnd { get { return 199999; } }
        public override int EcidEnd { get { return 199999; } }
        public override int PreDefinedEcidStart { get { return 101; } }
        public override int PreDefinedEcidEnd { get { return 130; } }
    }
    #endregion </Param Range>

    #region <Enums>
    public enum ScenarioListTypes
    {
        //AlarmStateChanged,

        //RecipeChanged,
        //RecipeParameterChanged,
        //RecipeFileChanged,

        // 완
        SCENARIO_REQ_LOT_INFO_CORE_1,
        SCENARIO_REQ_LOT_INFO_CORE_2,
        SCENARIO_REQ_LOT_INFO_EMPTY_TAPE,
        /// 

        // 완
        SCENARIO_PORT_STATUS_LOAD_1,
        SCENARIO_PORT_STATUS_LOAD_2,
        SCENARIO_PORT_STATUS_LOAD_3,
        SCENARIO_PORT_STATUS_LOAD_4,
        SCENARIO_PORT_STATUS_LOAD_5,
        SCENARIO_PORT_STATUS_LOAD_6,
        SCENARIO_PORT_STATUS_UNLOAD_1,
        SCENARIO_PORT_STATUS_UNLOAD_2,
        SCENARIO_PORT_STATUS_UNLOAD_3,
        SCENARIO_PORT_STATUS_UNLOAD_4,
        SCENARIO_PORT_STATUS_UNLOAD_5,
        SCENARIO_PORT_STATUS_UNLOAD_6,

        SCENARIO_EQUIPMENT_START,
        SCENARIO_EQUIPMENT_END,
        SCENARIO_ERROR_START,
        SCENARIO_ERROR_STOP,
        
        SCENARIO_PROCESS_START,
        SCENARIO_PROCESS_END,
        ///
        
        #region 추후구현
        SCENARIO_CARRIER_LOAD,
        SCENARIO_CARRIER_UNLOAD,
        #endregion

        // 완
        SCENARIO_RFID_READ_CORE_1,
        SCENARIO_RFID_READ_CORE_2,
        SCENARIO_RFID_READ_EMPTY_TAPE,
        SCENARIO_RFID_READ_BIN_1,
        SCENARIO_RFID_READ_BIN_2,
        SCENARIO_RFID_READ_BIN_3,

        SCENARIO_REQ_SLOT_INFO_CORE_1,
        SCENARIO_REQ_SLOT_INFO_CORE_2,
        SCENARIO_REQ_SLOT_INFO_EMPTY_TAPE,
        ///

        #region 추후구현
        SCENARIO_REQ_RECIPE_DOWNLOAD,
        SCENARIO_REQ_RECIPE_UPLOAD,
        #endregion

        // 완
        SCENARIO_REQ_TRACK_IN,
        SCENARIO_REQ_CORE_WAFER_TRACK_OUT,
        SCENARIO_REQ_LOT_MATCH,
        SCENARIO_REQ_BIN_WAFER_TRACK_OUT,
        SCENARIO_SLOT_WAFER_MAPPING_CORE_1,
        SCENARIO_SLOT_WAFER_MAPPING_CORE_2,
        SCENARIO_SLOT_WAFER_MAPPING_EMPTY_TAPE,
        SCENARIO_SLOT_WAFER_MAPPING_BIN_1,
        SCENARIO_SLOT_WAFER_MAPPING_BIN_2,
        SCENARIO_SLOT_WAFER_MAPPING_BIN_3,
        SCENARIO_REQ_LOT_MERGE_CORE_1,
        SCENARIO_REQ_LOT_MERGE_CORE_2,
        SCENARIO_REQ_LOT_ID_MERGE_AND_CHANGE_BIN_1,
        SCENARIO_REQ_LOT_ID_MERGE_AND_CHANGE_BIN_2,
        SCENARIO_REQ_LOT_ID_MERGE_AND_CHANGE_BIN_3,
        SCENARIO_WORK_START,
        SCENARIO_WORK_END,
        SCENARIO_REQ_CORE_WAFER_SPLIT,
        SCENARIO_REQ_CORE_WAFER_SPLIT_LAST,
        SCENARIO_CORE_WAFER_DETACH_START,
        SCENARIO_CORE_WAFER_DETACH_END,
        SCENARIO_REQ_CORE_CHIP_SPLIT_FIRST,
        SCENARIO_REQ_CORE_CHIP_SPLIT,
        SCENARIO_REQ_CORE_CHIP_FULL_SPLIT_FIRST,
        SCENARIO_REQ_CORE_CHIP_FULL_SPLIT,
        SCENARIO_REQ_CORE_CHIP_MERGE,

        #region 추후구현
        SCENARIO_UPLOAD_SCRAP_DATA,
        #endregion

        // 완
        SCENARIO_BIN_WAFER_ID_READ,
        SCENARIO_BIN_WORK_END,

        // TODO
        SCENARIO_BIN_PART_ID_INFO_REQ,
        SCENARIO_BIN_DATA_UPLOAD,
        //

        SCENARIO_REQ_BIN_WAFER_ID_ASSIGN,
        SCENARIO_BIN_SORTING_START_1,
        SCENARIO_BIN_SORTING_END_1,
        SCENARIO_BIN_SORTING_START_2,
        SCENARIO_BIN_SORTING_END_2,
        SCENARIO_BIN_SORTING_START_3,
        SCENARIO_BIN_SORTING_END_3,
        ///

        #region 추후 구현
        SCENARIO_REQ_COLLET_CHANGE_1,
        SCENARIO_REQ_COLLET_CHANGE_2,
        SCENARIO_REQ_HOOD_CHANGE,
        #endregion

        // 완  -> 실제 BinWorkEnd 발생하는 시나리오
        SCENARIO_REQ_UPLOAD_BINFILE,
        SCENARIO_ASSIGN_SUBSTRATE_ID,
    }

    public enum EN_EVENT_LIST
    {
        EQUIPMENT_START,
        EQUIPMENT_END,
        PROCESS_START,
        PROCESS_END,
        ERROR_START,
        ERROR_STOP,
        PORT_STATUS_LOAD,
        PORT_STATUS_UNLOAD,
        CARRIER_LOAD,
        CARRIER_UNLOAD,
        RFID_READ_CORE_1,
        RFID_READ_CORE_2,
        RFID_READ_EMPTY_TAPE,
        RFID_READ_BIN_1,
        RFID_READ_BIN_2,
        RFID_READ_BIN_3,
        REQ_LOT_INFO_CORE_1,
        REQ_LOT_INFO_CORE_2,
        REQ_LOT_INFO_EMPTY_TAPE,
        REQ_SLOT_INFO_CORE_1,
        REQ_SLOT_INFO_CORE_2,
        REQ_SLOT_INFO_EMPTY_TAPE,
        REQ_RECIPE_DOWNLOAD,
        REQ_RECIPE_UPLOAD,
        REQ_TRACK_IN,
        REQ_CORE_WAFER_TRACK_OUT,
        REQ_LOT_MATCH,
        REQ_BIN_WAFER_TRACK_OUT,
        BIN_PART_ID_INFO_REQ,       // TODO
        UPLOAD_WAFER_MAP,
        UPLOAD_PMS_DATA,
        SLOT_WAFER_MAPPING_CORE_1,
        SLOT_WAFER_MAPPING_CORE_2,
        SLOT_WAFER_MAPPING_EMPTY_TAPE,
        SLOT_WAFER_MAPPING_BIN_1,
        SLOT_WAFER_MAPPING_BIN_2,
        SLOT_WAFER_MAPPING_BIN_3,
        REQ_LOT_MERGE_CORE_1,
        REQ_LOT_MERGE_CORE_2,
        REQ_LOT_MERGE_BIN_1,
        REQ_LOT_MERGE_BIN_2,
        REQ_LOT_MERGE_BIN_3,
        REQ_LOT_ID_CHANGE_BIN_1,
        REQ_LOT_ID_CHANGE_BIN_2,
        REQ_LOT_ID_CHANGE_BIN_3,
        WORK_START,
        WORK_END,
        REQ_CORE_WAFER_SPLIT,
        REQ_CORE_WAFER_SPLIT_LAST,
        CORE_WAFER_DETACH_START,
        CORE_WAFER_DETACH_END,
        REQ_CORE_CHIP_SPLIT_FIRST,
        REQ_CORE_CHIP_SPLIT,
        REQ_CORE_CHIP_FULL_SPLIT_FIRST,       // TODO
        REQ_CORE_CHIP_FULL_SPLIT,       // TODO
        REQ_CORE_CHIP_MERGE,
        UPLOAD_SCRAP_DATA,
        BIN_WAFER_ID_READ,
        BIN_WORK_END,
        BIN_DATA_UPLOAD,       // TODO
        REQ_BIN_WAFER_ID_ASSIGN,
        REQ_BIN_WAFER_ID_CONFIRM,
        BIN_SORTING_START_1,
        BIN_SORTING_END_1,
        BIN_SORTING_START_2,
        BIN_SORTING_END_2,
        BIN_SORTING_START_3,
        BIN_SORTING_END_3,
        REQ_COLLET_CHANGE,
        REQ_HOOD_CHANGE,
    }

    public enum EN_SVID_LIST
    {
        PORTID,
        CARRIERID,
        CARRIER_TYPE,
        STATUS,
        SLOTID,
        OPERID,
        LOTID,
        RECIPEID,
        PARTID,
        STEPSEQ,
        LOTTYPE,
        XML_FILENAME,
        XML_FILEBODY,
        PMS_FILENAME,
        PMS_FILEBODY,
        WAFERID,
        SPLIT_WAFERID,
        SCRAP_QTY,
        SCRAP_INFO,
        PICKER_COLLET_1,
        PICKER_COLLET_2,
        EJECT_HOOD_ID,
        CHANGE_REASON,
        MATERIAL_TYPE,
        RINGFRAME_ID,
        ASSIGNED_WAFERID,
        BIN_TYPE,
        CHIP_QTY,
        SLOT1_WAFERID,
        SLOT2_WAFERID,
        SLOT3_WAFERID,
        SLOT4_WAFERID,
        SLOT5_WAFERID,
        SLOT6_WAFERID,
        SLOT7_WAFERID,
        SLOT8_WAFERID,
        SLOT9_WAFERID,
        SLOT10_WAFERID,
        SLOT11_WAFERID,
        SLOT12_WAFERID,
        SLOT13_WAFERID,
        SLOT14_WAFERID,
        SLOT15_WAFERID,
        SLOT16_WAFERID,
        SLOT17_WAFERID,
        SLOT18_WAFERID,
        SLOT19_WAFERID,
        SLOT20_WAFERID,
        SLOT21_WAFERID,
        SLOT22_WAFERID,
        SLOT23_WAFERID,
        SLOT24_WAFERID,
        SLOT25_WAFERID,
        SLOT1_WAFER_CHIP_QTY,
        SLOT2_WAFER_CHIP_QTY,
        SLOT3_WAFER_CHIP_QTY,
        SLOT4_WAFER_CHIP_QTY,
        SLOT5_WAFER_CHIP_QTY,
        SLOT6_WAFER_CHIP_QTY,
        SLOT7_WAFER_CHIP_QTY,
        SLOT8_WAFER_CHIP_QTY,
        SLOT9_WAFER_CHIP_QTY,
        SLOT10_WAFER_CHIP_QTY,
        SLOT11_WAFER_CHIP_QTY,
        SLOT12_WAFER_CHIP_QTY,
        SLOT13_WAFER_CHIP_QTY,
        SLOT14_WAFER_CHIP_QTY,
        SLOT15_WAFER_CHIP_QTY,
        SLOT16_WAFER_CHIP_QTY,
        SLOT17_WAFER_CHIP_QTY,
        SLOT18_WAFER_CHIP_QTY,
        SLOT19_WAFER_CHIP_QTY,
        SLOT20_WAFER_CHIP_QTY,
        SLOT21_WAFER_CHIP_QTY,
        SLOT22_WAFER_CHIP_QTY,
        SLOT23_WAFER_CHIP_QTY,
        SLOT24_WAFER_CHIP_QTY,
        SLOT25_WAFER_CHIP_QTY,
        SLOT1_WAFER_LOT_ID,
        SLOT2_WAFER_LOT_ID,
        SLOT3_WAFER_LOT_ID,
        SLOT4_WAFER_LOT_ID,
        SLOT5_WAFER_LOT_ID,
        SLOT6_WAFER_LOT_ID,
        SLOT7_WAFER_LOT_ID,
        SLOT8_WAFER_LOT_ID,
        SLOT9_WAFER_LOT_ID,
        SLOT10_WAFER_LOT_ID,
        SLOT11_WAFER_LOT_ID,
        SLOT12_WAFER_LOT_ID,
        SLOT13_WAFER_LOT_ID,
        SLOT14_WAFER_LOT_ID,
        SLOT15_WAFER_LOT_ID,
        SLOT16_WAFER_LOT_ID,
        SLOT17_WAFER_LOT_ID,
        SLOT18_WAFER_LOT_ID,
        SLOT19_WAFER_LOT_ID,
        SLOT20_WAFER_LOT_ID,
        SLOT21_WAFER_LOT_ID,
        SLOT22_WAFER_LOT_ID,
        SLOT23_WAFER_LOT_ID,
        SLOT24_WAFER_LOT_ID,
        SLOT25_WAFER_LOT_ID,
        EFEM_MAIN_CDA_PRESSURE,
        EFEM_MAIN_VAC_PRESSURE,
        ROBOT_CDA_PRESSURE,
        IONIZER_PRESSURE,
        IONIZER_FLOW_METER_1,
        IONIZER_FLOW_METER_2,
        IONIZER_FLOW_METER_3,
        IONIZER_FLOW_METER_4,
        EFEM_FFU_SPEED_1,
        EFEM_FFU_SPEED_2,
        EFEM_FFU_SPEED_3,
        SUPPLY_BUFFER_IONIZER_FLOW,
        SORTING_BUFFER_IONIZER_FLOW,
        SUPPLY_STAGE_IONIZER_FLOW,
        SORTING_STAGE_IONIZER_FLOW,
        PM_FFU_SPEED_1,
        PM_FFU_SPEED_2,
        PM_FFU_SPEED_3,
        EJECT_MEMBRANE_AIR_REGULATOR,
        EJECT_MEMBRANE_VAC_PRESS,
        EJECT_VAC_PRESS,
        NEEDLE_HEIGHT,
        EXPENSION_HEIGHT,
        PICK_SEARCH_LEVEL,
        PICK_SEARCH_SPEED,
        PICK_DELAY,
        PICK_FORCE,
        PICK_SLOWUP_LEVEL,
        PICK_SLOWUP_SPEED,
        PLACE_SEARCH_LEVEL,
        PLACE_SEARCH_SPEED,
        PLACE_DELAY,
        PLACE_FORCE,
        PLACE_SLOWUP_LEVEL,
        PLACE_SLOWUP_SPEED,
    }

    public enum RemoteCommandTypes
    {
        STOP = 0,
        NEXT_WORK_REQ,
        STOP_WORK_REQ,
    }

    public enum ObjectNames
    {
        LOTINFO,
        CHANGELOTINFO,
        ASSIGN_WAFER_LOT_ID,
        ASSIGN_SPLIT_LOT_ID,
        ASSIGN_WAFER_ID,
        LOT_MERGE,
        LOT_ID_CHANGE,
        ProceedWithCarrier,
        BIN_PART_ID_INFO,
    }

    public enum AttributeNames
    {
        LOTID,
        SPLIT_LOTID,
    }
    public enum OHTHandlingStatus
    { 
        LOAD,
        UNLOAD
    }
    public enum OHTHandlingCarrierType
    {
        MAC,
        CASSETTE
    }
    public enum CarrierLotIdType
    {
        // 빈용기 투입 요청 Lot Id
        PEMAC,
        ECASSETTE,

        // Terminated Wafer 포함된 Carrier Id(배출 시 Tag에 쓸 이름)
        // 2024.11.27. jhlim [MOD] 고객사 요청으로 명칭 변경
        PRMAC,
        //RCMAC,
        // 2024.11.27. jhlim [END]
        RCASSETTE,

        // Core or Empty 요청 -> 공란
        // 완성된 Bin Carrier -> LotId
    }
    public enum MessagesToReceive
    {
        #region <Request>
        RequestUpdateEquipmentData,
        RequestUpdateTraceData,
        RequestUpdateEquipmentState,


        RequestNotifyAlarmStatus,
        RequestAssignRingId,
        RequestDownloadMapFile,
        RequestStartDetaching,
        RequestFinishDetaching,
        RequestStartSorting,
        RequestFinishSorting,
        RequestSplitCoreChip,
        RequestUploadCoreFile,
        RequestUploadScrapInfo,
        #endregion </Request>

        #region <Response>
        ResponseDownloadRecipe,
        ResponseUploadRecipe,
        ResponseDeleteRecipe,
        ResponseAssignSubstrateId,
        ResponseAssignLotId,
        ResponseUploadBinFile,
        #endregion </Response>
    }

    public enum MessagesToSend
    {
        #region <Request>
        RequestStop,
        RequestCallOperator,
        RequestDownloadRecipe,
        RequestUploadRecipe,
        RequestDeleteRecipe,
        RequestAssignSubstrateId,
        RequestAssignLotId,
        RequestUploadBinFile,
        #endregion </Request>

        #region <Response>
        ResponseAssignRingId,
        ResponseDownloadMapFile,
        ResponseStartDetaching,
        ResponseFinishDetaching,
        ResponseStartSorting,
        ResponseFinishSorting,
        ResponseSplitCoreChip,
        ResponseUploadCoreFile,
        ResponseUploadScrapInfo,
        #endregion </Response>
    }
    #endregion </Enums>

    #region <Class>
    public static class AdditionalParamKeys
    {
        public static readonly string KeyNameOfEq = "NameOfEq";
        public static readonly string KeySubstrateId = "SubstrateId";
        public static readonly string KeyRingId = "RingId";
        public static readonly string KeyUserId = "UserId";
        public static readonly string KeyMessageNameToSend = "ScenarioNameToSend";
        public static readonly string KeySubstrateType = "SubstrateType";
        public static readonly string KeyChipQty = "ChipQty";
    }
    public static class ResultKeys
    {
        public static readonly string KeyResult = "Result";
        public static readonly string KeyDescription = "Description";
    }
    public static class NotifyAlarmKeys
    {
        public static readonly int BaseAlarmIndexOffset = 2000000;
        public static readonly string KeyAlarmId = "AlarmId";
        public static readonly string KeyStatus = "Status";
    }

    public static class MachineInfoKeys
    {
        public static readonly string KeyLotId = "LotId";
        public static readonly string KeyRecipeId = "RecipeId";
        public static readonly string KeyEquipmentState = "EquipmentState";
    }
    public static class EESKeys
    {
        public static readonly string KeyCarrierId = "CARRIERID";
        public static readonly string KeyPortId = "PORTID";
        public static readonly string KeyLotId = "LOTID";
        public static readonly string KeyPartId = "PARTID";
        public static readonly string KeyParamRecipeId = "RECIPEID";
        public static readonly string KeyOperatorId = "OPERID";
    }
    public static class RFIDReadKeys
    {
        public static readonly string KeyParamLotId = "LOTID";
        public static readonly string KeyParamCarrierId = "CARRIERID";
        public static readonly string KeyParamPortId = "PORTID";
        public static readonly string KeyParamOperatorId = "OPERID";
    }
    public static class LotInfoKeys
    {
        public static readonly string KeyParamLotId = "LOTID";
        public static readonly string KeyParamCarrierId = "CARRIERID";

        public static readonly string KeyResultLotId = "LotId";
        public static readonly string KeyResultPartId = "PartId";
        public static readonly string KeyResultStepSeq = "StepSeq";
        public static readonly string KeyResultLotType = "LotType";
        public static readonly string KeyResultRecipeId = "RecipeId";
    }
    public static class SlotMapVefiricationKeys
    {
        public static readonly string KeyResultLotId = "LotId";
        public static readonly string KeyResultName = "Name";
        public static readonly string KeyResultStatus = "Status";
        public static readonly string KeyIsCancelCarrier = "IsCancelCarrier";
    }
    public static class RecipeHandlingKeys
    {
        public static readonly string KeyRecipeId = "RecipeId";
        public static readonly string KeyRecipeBody = "RecipeBody";
        public static readonly string KeyUseCommunicationToPM = "UseCommunicationToPM";

        public static readonly string KeyParamLotId = "LOTID";
        public static readonly string KeyParamRecipeId = "RECIPEID";
        public static readonly string KeyParamPartId = "PARTID";
        public static readonly string KeyParamStepSeq = "STEPSEQ";
        public static readonly string KeyParamLotType = "LOTTYPE";
    }
    public static class AssignRingIdKeys
    {
        public static readonly string KeyOldRingId = "OldRingId";
        public static readonly string KeyNewRingId = "NewRingId";

        public static readonly string KeyParamLotId = "LOTID";
        public static readonly string KeyParamWaferId = "WAFERID";
    }
    public static class AssignSubstrateLotIdKeys
    {
        public static readonly string KeySubstrateName = "SubstrateName";
        public static readonly string KeyLotId = "LotId";

        public static readonly string KeyParamLotId = "LOTID";
        public static readonly string KeyParamWaferId = "WAFERID";
        public static readonly string KeyParamPartId = "PARTID";
        public static readonly string KeyParamRecipeId = "RECIPEID";
        public static readonly string KeyParamSlotId = "SLOTID";
        public static readonly string KeyParamOperatorId = "OPERID";

        // 시나리오 결과용
        public static readonly string KeyResultLotId = "LotId";
        public static readonly string KeyResultSubstrateId = "SubstrateId";
        public static readonly string KeyResultQty = "Qty";
    }
    public static class AssignSubstrateIdKeys
    {
        public static readonly string KeySubstrateName = "SubstrateName";
        public static readonly string KeyRingId = "RingId";
        public static readonly string KeyRecipeId = "RecipeId";
        public static readonly string KeySubstrateType = "SubstrateType";
        public static readonly string KeyChipQty = "ChipQty";
        
        // Param 전송용
        public static readonly string KeyParamLotId = "LOTID";
        public static readonly string KeyParamBinType = "BIN_TYPE";
        public static readonly string KeyParamRingFrameId = "RINGFRAME_ID";
        public static readonly string KeyParamSlotId = "SLOTID";
        public static readonly string KeyParamChipQty = "CHIP_QTY";

        // 시나리오 결과용
        public static readonly string KeyResultSubstrateId = "SubstrateId";
    }
    public static class AssignBinLotIdKeys
    {
        public static readonly string KeySubstarateName = "SubstrateName";
        public static readonly string KeyLotId = "LotId";
    }
    public static class SortingKeys
    {
        public static readonly string KeyRingId = "RingId";
        public static readonly string KeyRecipeId = "RecipeId";
        public static readonly string KeySubstrateType = "SubstrateType";
        public static readonly string KeyBinCode = "BinCode";
        public static readonly string KeyChipQty = "ChipQty";

        // Param용
        public static readonly string KeyParamLotId = "LOTID";
        public static readonly string KeyParamBinType = "BIN_TYPE";
        public static readonly string KeyParamRingFrameId = "RINGFRAME_ID";
        public static readonly string KeyParamChipQty = "CHIP_QTY";
    }
    public static class DetachingKeys
    {
        public static readonly string KeySubstarateName = "SubstrateName";
        public static readonly string KeyRingId = "RingId";
        public static readonly string KeyRecipeId = "RecipeId";
        public static readonly string KeyUserId = "UserId";
        public static readonly string KeySubstrateType = "SubstrateType";


        // Param용
        public static readonly string KeyParamCarrierId = "CARRIERID";
        public static readonly string KeyParamPortId = "PORTID";
        public static readonly string KeyParamLotId = "LOTID";
        public static readonly string KeyParamPartId = "PARTID";
        public static readonly string KeyParamRecipeId = "RECIPEID";
        public static readonly string KeyParamWaferId = "WAFERID";
        public static readonly string KeyParamSlotId = "SLOTID";
        public static readonly string KeyParamOperatorId = "OPERID";
    }
    public static class SplitCoreChipKeys
    {
        public static readonly string KeyCoreSubstrateName = "CoreSubstrateName";
        public static readonly string KeyBinRingId = "BinRingId";
        
        public static readonly string KeySubstrateType = "SubstrateType";
        public static readonly string KeyRecipeId = "RecipeId";
        public static readonly string KeySplitQty = "SplitQty";
        public static readonly string KeyRemainingChips = "RemainingChips";
        public static readonly string KeyIsFirstSorting = "IsFirstSorting";
        public static readonly string KeyUserId = "UserId";
        public static readonly string KeyBinCode = "BinCode";

        // Param 전송용
        public static readonly string KeyParamLotId = "LOTID";
        public static readonly string KeyParamSplitLotId = "SPLIT_LOTID";
        public static readonly string KeyParamSplitWaferId = "SPLIT_WAFERID";
        public static readonly string KeyParamRingFrameId = "RINGFRAME_ID";
        public static readonly string KeyParamBinType = "BIN_TYPE";
        public static readonly string KeyParamSplitChipQty = "CHIP_QTY";

        // 시나리오 결과용
        public static readonly string KeyResultLotId = "LotId";
        public static readonly string KeyResultSplittedLotId = "SplittedLotId";
        public static readonly string KeyResultQty = "Qty";
    }
    public static class RequestDownloadMapFileKeys
    {
        public static readonly string KeySubstrateName = "SubstrateName";
        public static readonly string KeyRingId = "RingId";
        public static readonly string KeyWaferAngle = "WaferAngle";
        public static readonly string KeyUserId = "UserId";

        public static readonly string KeyCountRow = "CountRow";
        public static readonly string KeyCountCol = "CountCol";
        public static readonly string KeyChipQty = "ChipQty";
        public static readonly string KeyMapData = "MapData";
        public static readonly string KeyNullBinCode = "NullBinCode";
        public static readonly string KeyUseEventHandling = "UseEventHandling";

        // Param 전송용
        public static readonly string KeyParamCarrierId = "CARRIERID";
        public static readonly string KeyParamPortId = "PORTID";
        public static readonly string KeyParamLotId = "LOTID";
        public static readonly string KeyParamPartId = "PARTID";
        public static readonly string KeyParamRecipeId = "RECIPEID";
        public static readonly string KeyParamOperatorId = "OPERID";
        public static readonly string KeyParamWaferId = "WAFERID";
        public static readonly string KeyParamAngle = "ANGLE";

        // 시나리오 결과용
        public static readonly string KeyResultSubstrateId = "SubstrateId";
        public static readonly string KeyResultCountRow = "CountRow";
        public static readonly string KeyResultCountCol = "CountCol";
        public static readonly string KeyResultReferenceX = "ReferenceX";
        public static readonly string KeyResultReferenceY = "ReferenceY";
        public static readonly string KeyResultStartingX = "StartingX";
        public static readonly string KeyResultStartingY = "StartingY";
        public static readonly string KeyResultAngle ="Angle";
        public static readonly string KeyResultQty = "Qty";
        public static readonly string KeyResultMapData = "MapData";
    }
    public static class TrackInOrOut
    {
        public static readonly string KeyParamCarrierId = "CARRIERID";
        public static readonly string KeyParamPortId = "PORTID";
        public static readonly string KeyParamLotId = "LOTID";
        public static readonly string KeyParamPartId = "PARTID";
        public static readonly string KeyParamStepSeq = "STEPSEQ";
        public static readonly string KeyParamRecipeId = "RECIPEID";
        public static readonly string KeyParamChipQty = "CHIP_QTY";
        public static readonly string KeyParamBinType = "BIN_TYPE";
        public static readonly string KeyParamOperatorId = "OPERID";

        public static readonly string KeyParamChangeReason = "CHANGE_REASON";
        public static readonly string KeyParamMaterialType = "MATERIAL_TYPE";
    }
    public static class UploadCoreOrBinFileKeys
    {
        public static readonly string KeySubstrateName = "SubstrateName";
        public static readonly string KeyRingId = "RingId";
        public static readonly string KeyRecipeId = "RecipeId";
        public static readonly string KeySubstrateType = "SubstrateType";
        public static readonly string KeyPMSBody = "PMSFileBody";
        public static readonly string KeyCountRow = "CountRow";
        public static readonly string KeyCountCol = "CountCol";
        public static readonly string KeyWaferAngle = "WaferAngle";
        public static readonly string KeyMapData = "MapData";
        public static readonly string KeyChipQty = "ChipQty";
        public static readonly string KeyNullBinCode = "NullBinCode";
        public static readonly string KeyUserId = "UserId";
        public static readonly string KeyBinCode = "BinCode";
        public static readonly string KeyUseEventHandling = "UseEventHandling";

        //public static readonly string KeyXMLFileName = "XMLFileName";
        //public static readonly string KeyXMLFileBody = "XMLFileBody";
        public static readonly string KeyPMSFileName = "PMSFileName";
        public static readonly string KeyPMSFileBody = "PMSFileBody";

        public static readonly string KeyReferenceX = "ReferenceX";
        public static readonly string KeyReferenceY = "ReferenceY";
        public static readonly string KeyStartingPosX = "StartingPosX";
        public static readonly string KeyStartingPosY = "StartingPosY";

        public static readonly string KeyStepId = "StepId";
        public static readonly string KeyEquipId = "EquipId";
        public static readonly string KeyPartId = "PartId";
        public static readonly string KeySlot = "Slot";
        public static readonly string KeyLotId = "LotId";

        // Param 전송용
        public static readonly string KeyParamCarrierId = "CARRIERID";
        public static readonly string KeyParamPortId = "PORTID";
        public static readonly string KeyParamLotId = "LOTID";
        public static readonly string KeyParamRecipeId = "RECIPEID";
        public static readonly string KeyParamPartId = "PARTID";
        public static readonly string KeyParamChipQty = "CHIP_QTY";
        public static readonly string KeyParamWaferId = "WAFERID";
        public static readonly string KeyParamSlotId = "SLOTID";
        public static readonly string KeyParamOperatorId = "OPERID";
    }
    public static class SlotMappingKeys
    {
        public static readonly string KeyParamLotId = "LOTID";
        public static readonly string KeyParamCarrierId = "CARRIERID";

        public static readonly string KeyParamSlotNamePre = "SLOT";
        public static readonly string KeyParamSlotNamePost= "WAFERID";

        public static readonly string KeyParamSlotQtyPre = "SLOT";
        public static readonly string KeyParamSlotQtyPost = "WAFER_CHIP_QTY";
    }
    public static class LotMergeKeys
    {
        // PARAM
        public const string KeyParamLotId = "LOTID";
        public const string KeyParamCarrierId = "CARRIERID";
        public const string KeyParamPartId = "PARTID";
        public const string KeyParamRecipeId = "RECIPEID";
        public const string KeyOperatorId = "OPERID";

        public const string KeyParamSlotLotIdPre = "SLOT";
        public const string KeyParamSlotLotIdPost = "WAFER_LOT_ID";

        // Result
        public const string KeyResultLotId = "LotId";
    }
    public static class ChangeToLotIdKeys
    {
        // PARAM
        public static readonly string KeyParamLotId = "LOTID";
        public static readonly string KeyParamCarrierId = "CARRIERID";
    }
    public static class AMHSHandlingKeys
    {
        // PARAM
        public static readonly string KeyParamPortId = "PORTID";
        public static readonly string KeyParamLotId = "LOTID";
        public static readonly string KeyParamCarrierId = "CARRIERID";
        public static readonly string KeyParamCarrierType = "CARRIER_TYPE";
        public static readonly string KeyParamStatus = "STATUS";
        public static readonly string KeyParamOperId = "OPERID";        
    }
    public static class ProcessModuleStatusChangedKeys
    {
        public static readonly string KeyParamLotId = "LOTID";
        public static readonly string KeyParamPartId = "PARTID";
        public static readonly string KeyParamStepSeq = "STEPSEQ";
    }
    #endregion </Class>
}
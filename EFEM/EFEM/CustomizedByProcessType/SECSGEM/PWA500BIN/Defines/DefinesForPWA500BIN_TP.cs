using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using FrameOfSystem3.SECSGEM.DefineSecsGem;

namespace EFEM.CustomizedByProcessType.PWA500BIN
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
        ESD_SENSOR_01,
        ESD_SENSOR_02,
        ESD_SENSOR_03,
        ESD_SENSOR_04,
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

        RequestUploadRecipe,
        RequestUploadRecipeResult,
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
        public static readonly string KeyParamParentLotId = "MATERIAL_LOT_ID_TO_COMSUME";
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
    
    public class LotHistoryLog
    {
        #region <Constructors>
        private LotHistoryLog()
        {
            BasePath = string.Format(@"{0}\History", Define.DefineConstant.FilePath.FILEPATH_LOG);
            CurrentWorkingPath = new Dictionary<int, string>();

            BasePathForSubstrate = string.Format(@"{0}\CurrentWorking", BasePath);
        }
        #endregion </Constructors>

        #region <Types>
        enum CarrierBasedEventType
        {
            IdRead,
            LotInfo,
            ReqSlotMap,
            TrackIn,
            LotMatch,
            TrackOut,
            LotMerge,
            LotMergeAndChange,
            SlotMapping,
        }

        enum SubstrateType
        {
            Core,
            Bin
        }

        enum SubstrateBasedEventType
        {
            WorkStart,
            WorkEnd,
            WaferSplit,
            StartDetaching,
            FinishDetaching,
            ChipSplit,
            ChipSplitAndMerge,
            TrackOut,
            RingIdRead,
            StartSorting,
            FinishSorting,
            IdAssign,
            ReqPartId,
            UploadBinData
        }
        #endregion </Types>

        #region <Fields>
        private const string LogFileExtension = ".log";

        private static LotHistoryLog _instance = null;
        private readonly string BasePath = null;
        private readonly string BasePathForSubstrate = null;
        private readonly Dictionary<int, string> CurrentWorkingPath = null;
        private readonly ConcurrentQueue<Tuple<string, string>> QueueToWrite = new ConcurrentQueue<Tuple<string, string>>();

        private Action<int, string> _logMessageToDisplay = null;
        #endregion </Fields>

        #region <Properties>
        public static LotHistoryLog Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new LotHistoryLog();

                return _instance;
            }
        }
        #endregion </Properties>

        #region <Methods>

        #region <AssignPath>
        public void AddLogInfo(int portId, string name)
        {
            string dir = string.Format(@"{0}\CurrentWorking\{1}", BasePath, name);
            CurrentWorkingPath[portId] = dir;
            if (false == Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }
        public void AttachDisplayLogAction(Action<int, string> action)
        {
            _logMessageToDisplay = action;
        }
        public string GetBackupHistoryPath(DateTime time, bool isCore)
        {
            if (isCore)
            {
                return string.Format(@"{0}\Backup\{1:0000}\{2:00}\{3:00}\Core", BasePath, time.Year, time.Month, time.Day);
            }
            else
            {
                return string.Format(@"{0}\Backup\{1:0000}\{2:00}\{3:00}\Bin", BasePath, time.Year, time.Month, time.Day);
            }
        }
        public string GetCarrierHistoryPath(int portId, string carrierId)
        {
            if (false == CurrentWorkingPath.TryGetValue(portId, out string basePath))
                return string.Empty;

            return string.Format(@"{0}\{1}{2}", basePath, carrierId, LogFileExtension);
        }
        public string GetSubstratePath(string substrateName, bool isCore)
        {
            SubstrateType substrateType = isCore ? SubstrateType.Core : SubstrateType.Bin;

            return string.Format(@"{0}\{1}\{2}{3}", BasePathForSubstrate, substrateType.ToString(), substrateName, LogFileExtension);
        }
        public void ClearPreviousHistory(int portId, string carrierId, string loadportName)
        {
            if (false == CurrentWorkingPath.TryGetValue(portId, out string basePath))
                return;

            DateTime date = DateTime.Now;
            string backupPath = string.Format(@"{0}\Backup\{1:0000}\{2:00}\{3:00}\NotCompleted\{4}", BasePath, date.Year, date.Month, date.Day, loadportName);
            if (false == Directory.Exists(backupPath))
                Directory.CreateDirectory(backupPath);
            
            string[] files = Directory.GetFiles(basePath);
            string sourceFilePath = string.Format(@"{0}\{1}{2}", basePath, carrierId, LogFileExtension);
            for(int i = 0; files != null && i < files.Length; ++i)
            {
                var file = files[i];
                if (false == file.Equals(sourceFilePath))
                {
                    try
                    {
                        string fileNameToMove = Path.GetFileName(file);
                        string destinationPath = Path.Combine(backupPath, fileNameToMove);
                        if (File.Exists(destinationPath))
                            File.Delete(destinationPath);

                        File.Move(file, destinationPath);
                    }
                    catch
                    {

                    }
                }
            }
            
        }        
        public void UpdateSubstrateHistoryToCarrierHistory(int portId, string carrierId, string substrateName)
        {
            try
            {
                var substrateHistoryFullPath = GetSubstratePath(substrateName, false);
                var substrateHistoryPath = Path.GetDirectoryName(substrateHistoryFullPath);
                if (false == Directory.Exists(substrateHistoryPath) ||
                    false == File.Exists(substrateHistoryFullPath))
                    return;

                var carrierHistoryFullPath = GetCarrierHistoryPath(portId, carrierId);
                var carrierHistoryPath = Path.GetDirectoryName(carrierHistoryFullPath);
                if (false == Directory.Exists(carrierHistoryPath) ||
                    false == File.Exists(carrierHistoryFullPath))
                    return;

                string[] lines;
                using (FileStream fs = new FileStream(substrateHistoryFullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (StreamReader sr = new StreamReader(fs))
                {
                    var tempList = new System.Collections.Generic.List<string>();
                    while (false == sr.EndOfStream)
                    {
                        tempList.Add(sr.ReadLine());
                    }
                    lines = tempList.ToArray();
                }

                if (lines == null || lines.Length <= 0)
                    return;

                UpdateRingIdToSubstrateId(substrateName, ref lines);

                using (FileStream fs = new FileStream(carrierHistoryFullPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    for (int i = 0; lines != null && i < lines.Length; ++i)
                    {
                        sw.WriteLine(lines[i]);
                    }
                }
            }
            catch
            {

            }
        }
        private void UpdateRingIdToSubstrateId(string substrateName, ref string[] linesToChange)
        {
            for (int i = 0; i < linesToChange.Length; ++i)
            {
                var parts = linesToChange[i].Split(new char[] { '\t' }, StringSplitOptions.None);
                if (parts.Length < 2)
                    continue;

                parts[2] = substrateName;
                linesToChange[i] = string.Join("\t", parts);
            }
        }
        public void BackupCarrierHistory(int portId, string carrierId, string lotId, List<string> substrates, bool isCore)
        {
            if (false == CurrentWorkingPath.TryGetValue(portId, out string basePath))
                return;

            string sourceFilePath = string.Format(@"{0}\{1}{2}", basePath, carrierId, LogFileExtension);
            DateTime date = DateTime.Now;
            string backupPath;
            if (isCore)
            {
                backupPath = string.Format(@"{0}\Backup\{1:0000}\{2:00}\{3:00}\Core\{4}", BasePath, date.Year, date.Month, date.Day, lotId);
            }
            else
            {
                backupPath = string.Format(@"{0}\Backup\{1:0000}\{2:00}\{3:00}\Bin\{4}", BasePath, date.Year, date.Month, date.Day, lotId);
            }
            //var backupPath = string.Format(@"{0}\Backup\{1:0000}\{2:00}\{3:00}\{4}", BasePath, date.Year, date.Month, date.Day, lotId);

            var backupFullPath = string.Format(@"{0}\{1}{2}", backupPath, carrierId, LogFileExtension);

            try
            {
                if (false == Directory.Exists(backupPath))
                    Directory.CreateDirectory(backupPath);

                if (File.Exists(backupFullPath))
                {
                    File.Delete(backupFullPath);
                }

                // Substrate Lists
                if (substrates != null)
                {
                    Dictionary<string, string> filebodies = null;
                    string backupSubstratePath = string.Format(@"{0}\Wafers", backupPath);
                    if (false == isCore)
                    {
                        filebodies = new Dictionary<string, string>();
                    }
                    
                    for (int i = 0; i < substrates.Count; ++i)
                    {
                        SubstrateType type;
                        if (isCore)
                        {
                            type = SubstrateType.Core;
                        }
                        else
                        {
                            type = SubstrateType.Bin;
                            string body = string.Empty;
                            body = GetSubstrateHistoryFromFile(type, substrates[i]);
                            filebodies[substrates[i]] = body;
                        }

                        MoveSubstrateHistoryFile(type, substrates[i], backupSubstratePath);
                    }

                    #region <원본>
                    // Bin인 경우, 읽은 바디를 정렬하여 Bin Carrier에 삽입한다.
                    //if (false == isCore && substrates.Count > 0)
                    //{
                    //    string carrierHistoryBody = string.Empty;
                    //    using (StreamReader sr = new StreamReader(sourceFilePath))
                    //    {
                    //        carrierHistoryBody = sr.ReadToEnd();
                    //    }

                    //    var rawLinesFromFile = new Dictionary<DateTime, string>();
                    //    var newBodiesBySorting = new List<string>();
                    //    string[] splittedCarrierHistory = carrierHistoryBody.Split('\n');
                    //    if (splittedCarrierHistory != null)
                    //    {
                    //        int i;
                    //        DateTime time;
                            
                    //        #region <Carrier History>
                    //        for (i = 0; i < splittedCarrierHistory.Length; ++i)
                    //        {
                    //            time = DateTime.Now;
                    //            if (false == GetHistoryTimeFromLog(splittedCarrierHistory[i], ref time))
                    //                continue;

                    //            rawLinesFromFile[time] = splittedCarrierHistory[i];
                    //        }
                    //        #endregion </Carrier History>

                    //        #region <Substrate History>
                    //        foreach (var item in filebodies)
                    //        {
                    //            string[] splittedSubstrateHistory = item.Value.Split('\n');
                    //            for (i = 0; i < splittedSubstrateHistory.Length; ++i)
                    //            {
                    //                time = DateTime.Now;
                    //                if (false == GetHistoryTimeFromLog(splittedSubstrateHistory[i], ref time))
                    //                    continue;

                    //                rawLinesFromFile[time] = splittedSubstrateHistory[i];
                    //            }
                    //        }
                    //        #endregion </Substrate History>

                    //        #region <Sort>                            
                    //        newBodiesBySorting = rawLinesFromFile.OrderBy(item => item.Key).ToDictionary(x => x.Key, x => x.Value).Values.ToList();

                    //        if(newBodiesBySorting.Count > 0)
                    //        {
                    //            // 파일을 지우고
                    //            File.Delete(sourceFilePath);
                                
                    //            // 정렬된 파일로 새로 쓴다.
                    //            using (StreamWriter sw = new StreamWriter(sourceFilePath))
                    //            {
                    //                for(i = 0; i < newBodiesBySorting.Count; ++i)
                    //                {
                    //                    sw.Write(newBodiesBySorting[i]);
                    //                }
                    //            }
                    //        }
                    //        #endregion </Sort>
                    //    }
                    //}
                    #endregion </원본>
                }

                // Carrier History
                File.Move(sourceFilePath, backupFullPath);                
            }
            catch
            {

            }
        }
        #endregion </AssignPath>

        #region <CarrierBasedEvents>
        public void WriteHistoryForIdRead(int portId, string carrierId, string lotId)
        {
            WriteCarrierLog(portId, carrierId, CarrierBasedEventType.IdRead, string.Format("아이디 읽음 : [랏:{0}], [캐리어:{1}]", lotId, carrierId));
        }
        public void WriteHistoryForLotInfo(int portId, string carrierId, string lotId, string partId, string stepSeq, string lotType)
        {
            WriteCarrierLog(portId, carrierId, CarrierBasedEventType.LotInfo, string.Format("랏 정보 요청 진행 : [랏:{0}], [파트:{1}], [스텝:{2}], [랏 타입:{3}]", lotId, partId, stepSeq, lotType));
        }
        public void WriteHistoryForSlotMap(int portId, string carrierId, Dictionary<int, string> status)
        {
            string logToWrite = string.Empty;
            foreach (var item in status)
            {
                if (false == string.IsNullOrEmpty(logToWrite))
                {
                    logToWrite = string.Format("{0}, [슬롯:{1}, 상태:{2}]", logToWrite, item.Key + 1, item.Value);
                }
                else
                {
                    logToWrite = string.Format("슬롯 정보 요청 진행 : [슬롯:{0}, 상태:{1}]", item.Key + 1, item.Value);
                }
            }

            WriteCarrierLog(portId, carrierId, CarrierBasedEventType.ReqSlotMap, logToWrite);
        }
        public void WriteHistoryForTrackIn(int portId, string carrierId, string scenario, string lotId)
        {
            if (scenario.Equals("SCENARIO_REQ_TRACK_IN"))
            {
                WriteCarrierLog(portId, carrierId, CarrierBasedEventType.TrackIn, string.Format("트랙인 진행 : [랏:{0}]", lotId));
            }
            else if (scenario.Equals("SCENARIO_REQ_LOT_MATCH"))
            {
                WriteCarrierLog(portId, carrierId, CarrierBasedEventType.LotMatch, string.Format("원부자재 교체 진행 [원부자재 랏:{0}]", lotId));
            }
        }
        public void WriteHistoryForTrackOut(int portId, string carrierId, string lotId)
        {
            WriteCarrierLog(portId, carrierId, CarrierBasedEventType.TrackOut, string.Format("트랙아웃 진행 : [랏:{0}]", lotId));
        }
        public void WriteHistoryForMerge(int portId, string carrierId, string newLotId, Dictionary<int, string> lotIdToMerge)
        {
            CarrierBasedEventType type;
            if (portId <= 3)
            {
                type = CarrierBasedEventType.LotMergeAndChange;
            }
            else
            {
                type = CarrierBasedEventType.LotMerge;
            }

            string logToWrite = string.Empty;
            foreach (var item in lotIdToMerge)
            {
                if (false == string.IsNullOrEmpty(logToWrite))
                {
                    logToWrite = string.Format("{0}, [슬롯:{1}, 랏:{2}]", logToWrite, item.Key + 1, item.Value);
                }
                else
                {
                    logToWrite = string.Format("랏 [{0}] 으로 머지 진행 : [슬롯:{1}, 랏:{2}]", newLotId, item.Key + 1, item.Value);
                }
            }

            //logToWrite = string.Format("{0} -> {1}", logToWrite, newLotId);
            WriteCarrierLog(portId, carrierId, type, logToWrite);
        }
        public void WriteHistoryForSlotMapping(int portId, string carrierId, Dictionary<int, Tuple<string, string>> substratesToMapping)
        {
            string logToWrite = string.Empty;
            if (substratesToMapping.Count > 0)
            {
                foreach (var item in substratesToMapping)
                {
                    if (false == string.IsNullOrEmpty(logToWrite))
                    {
                        logToWrite = string.Format("{0}, [슬롯:{1}, 이름:{2}, 수량:{3}]", logToWrite, item.Key + 1, item.Value.Item1, item.Value.Item2);
                    }
                    else
                    {
                        logToWrite = string.Format("슬롯 매핑 진행 : [슬롯:{0}, 이름:{1}, 수량:{2}]", item.Key + 1, item.Value.Item1, item.Value.Item2);
                    }
                }
            }
            else
            {
                logToWrite = "슬롯 매핑 진행 : 비었음";
            }

            //logToWrite = string.Format("{0}", logToWrite);
            WriteCarrierLog(portId, carrierId, CarrierBasedEventType.SlotMapping, logToWrite);
        }
        #endregion </CarrierBasedEvents>

        #region <SubstrateBasedEvents>
        public void WriteSubstrateHistoryForDownloadMap(int portId, string carrierId, string substrateName, string ringId)
        {
            WriteSubstrateLog(portId, carrierId, substrateName, SubstrateBasedEventType.WorkStart, SubstrateType.Core, string.Format("바코드 인식하여 이름이 [{0}] 에서 [{1}] 으로 변경됨", ringId, substrateName));
        }
        public void WriteSubstrateHistoryForWaferSplit(int portId, string carrierId, string substrateName, string oldLotId, string newLotId, bool isLast)
        {
            string logToWrite = string.Format("랏이 스플릿되어 [{0}] 에서 [{1}] 으로 변경됨", oldLotId, newLotId);
            if (isLast)
            {
                logToWrite = string.Format("랏이 스플릿 되었으나 유지됨 [{0} -> {1}]", oldLotId, newLotId);
            }
            
            WriteSubstrateLog(portId, carrierId, substrateName, SubstrateBasedEventType.WaferSplit, SubstrateType.Core, logToWrite);
        }
        public void WriteSubstrateHistoryForStartOrFinishDetaching(int portId, string carrierId, string substrateName, bool isStarting)
        {
            SubstrateBasedEventType eventType;
            string logToWrite = string.Empty;
            if (isStarting)
            {
                eventType = SubstrateBasedEventType.StartDetaching;
                logToWrite = "작업 시작";
            }
            else
            {
                eventType = SubstrateBasedEventType.FinishDetaching;
                logToWrite = "작업 종료";
            }

            WriteSubstrateLog(portId, carrierId, substrateName, eventType, SubstrateType.Core, logToWrite);
        }
        public void WriteSubstrateHistoryForChipSplit(int corePortId, string coreCarrierId, string coreSubstrateName, int binPortId, string binSubstrateName, string splittedQty, string binCode, string assignedLotId, bool isFirst, bool isFully)
        {
            SubstrateBasedEventType eventType;
            string logToWriteForCore, logToWriteForBin;
            if (isFirst)
            {
                eventType = SubstrateBasedEventType.ChipSplit;
                logToWriteForCore = string.Format("[{0}] 수량만큼 칩 스플릿 되어 랏 [{1}] 생성, 공테이프 웨이퍼 [{2}] 에 부여될 예정 (빈코드:{3})", splittedQty, assignedLotId, binSubstrateName, binCode);
                logToWriteForBin = string.Format("공테이프 웨이퍼 [{0}] 에 코어 웨이퍼 [{1}] 로부터 스플릿된 랏 [{2}] 과 칩 수량 [{3}] 부여됨 (빈코드:{4})", binSubstrateName, coreSubstrateName, assignedLotId, splittedQty, binCode);
            }
            else
            {
                eventType = SubstrateBasedEventType.ChipSplitAndMerge;
                logToWriteForCore = string.Format("[{0}] 수량만큼 칩 스플릿 되어 임시 랏 [{1}] 생성, 빈 웨이퍼 [{2}] 에 병합될 예정 (빈코드:{3})", splittedQty, assignedLotId, binSubstrateName, binCode);
                logToWriteForBin = string.Format("빈 웨이퍼 [{0}] 에 코어 웨이퍼 [{1}] 로부터 스플릿된 랏 [{2}] 과 칩 수량 [{3}] 병합됨 (빈코드:{4})", binSubstrateName, coreSubstrateName, assignedLotId, splittedQty, binCode);
            }
            
            if (isFully)
            {
                logToWriteForCore = string.Format("{0}, (전량)", logToWriteForCore);
                logToWriteForBin = string.Format("{0}, (전량)", logToWriteForBin);
            }

            WriteSubstrateLog(corePortId, coreCarrierId, coreSubstrateName, eventType, SubstrateType.Core, logToWriteForCore);
            WriteSubstrateLog(binSubstrateName, eventType, SubstrateType.Bin, logToWriteForBin);
        }
        public void WriteSubstrateHistoryForWorkEnd(int portId, string carrierId, string substrateName, string remainingChips)
        {
            WriteSubstrateLog(portId, carrierId, substrateName, SubstrateBasedEventType.WorkEnd, SubstrateType.Core, string.Format("맵 업로드 및 작업 종료 이벤트 송신 [남은 칩:{0}]", remainingChips));
        }
        public void WriteSubstrateHistoryForTrackOut(int portId, string carrierId, string substrateName, string lotId, string remainingChips, bool isLast)
        {
            WriteSubstrateLog(portId, carrierId, substrateName, SubstrateBasedEventType.TrackOut, SubstrateType.Core, string.Format("랏 [{0}] 트랙 아웃 [남은 칩:{1}]", lotId, remainingChips));            
        }

        public void WriteSubstrateHistoryForReadRingId(int portId, string oldRingId, string newRingId)
        {            
            WriteSubstrateLog(newRingId, SubstrateBasedEventType.RingIdRead, SubstrateType.Bin, string.Format("바코드 인식하여 이름이 [{0}] 에서 [{1}] 으로 변경됨", oldRingId, newRingId));
        }
        public void WriteSubstrateHistoryForStartSorting(int portId, string substrateName)
        {
            WriteSubstrateLog(substrateName, SubstrateBasedEventType.StartSorting, SubstrateType.Bin, "작업 시작");
        }
        public void WriteSubstrateHistoryForFinishSorting(int portId, string substrateName, string assignedLotId, string materialLotId)
        {
            WriteSubstrateLog(substrateName, SubstrateBasedEventType.FinishSorting, SubstrateType.Bin, string.Format("작업 종료 (부여된 랏:{0}, 원부자재 랏:{1}]", assignedLotId, materialLotId));
        }
        public void WriteSubstrateHistoryForAssignSubstrateId(int portId, string substrateName, string assignedSubstrateName)
        {
            RenameBinSubstrateFile(substrateName, assignedSubstrateName);

            WriteSubstrateLog(assignedSubstrateName, SubstrateBasedEventType.IdAssign, SubstrateType.Bin, string.Format("서버로부터 이름이 [{0}] 으로 할당됨 [링 이름:{1}]", assignedSubstrateName, substrateName));
        }
        public void WriteSubstrateHistoryForBinWorkEnd(int portId, string substrateName, string binCode, string remainingChips)
        {
            WriteSubstrateLog(substrateName, SubstrateBasedEventType.WorkEnd, SubstrateType.Bin, string.Format("작업 종료 이벤트 송신 -> [빈코드:{0}], [칩수량:{1}]", binCode, remainingChips));
        }
        public void WriteSubstrateHistoryForBinTrackOut(int portId, string substrateName, string lotId, string binCode, string remainingChips)
        {
            WriteSubstrateLog(substrateName, SubstrateBasedEventType.TrackOut, SubstrateType.Bin, string.Format("랏 [{0}] 트랙 아웃 진행 [빈코드:{1}], [칩수량:{2}]", lotId, binCode, remainingChips));
        }
        public void WriteSubstrateHistoryForReqBinPartId(int portId, string substrateName, string binCode, string oldPartId, string newPartId)
        {
            WriteSubstrateLog(substrateName, SubstrateBasedEventType.ReqPartId, SubstrateType.Bin, string.Format("파트 아이디를 부여받아 [{0}] 에서 [{1}] 로 변경 [빈코드:{2}]", oldPartId, newPartId, binCode));
        }
        public void WriteSubstrateHistoryForUploadBinData(int portId, string substrateName, string pmsPath)
        {
            var fullPath = Path.GetFullPath(pmsPath);
            WriteSubstrateLog(substrateName, SubstrateBasedEventType.UploadBinData, SubstrateType.Bin, string.Format("맵과 작업 정보 업로드 진행 [PMS파일 경로:{0}]", fullPath));
        }
        #endregion </SubstrateBasedEvents>

        #region <Executing>
        public void ExecuteWriteAsync()
        {
            if (QueueToWrite.Count <= 0)
                return;

            if (QueueToWrite.TryDequeue(out Tuple<string, string> logInfoToWrite))
            {
                WriteLog(logInfoToWrite.Item1, logInfoToWrite.Item2);
            }
        }
        #endregion </Executing>

        #region <Internal>
        private bool GetHistoryTimeFromLog(string message, ref DateTime time)
        {
            string[] splittedLine = message.Split('\t');
            if (splittedLine.Length <= 0)
                return false;

            return DateTime.TryParse(splittedLine[0], out time);            
        }
        private string GetSubstrateHistoryFromFile(SubstrateType type, string substrateName)
        {
            string sourceFilePath = string.Format(@"{0}\{1}\{2}{3}", BasePathForSubstrate, type.ToString(), substrateName, LogFileExtension);
            string fileBody = string.Empty;
            try
            {
                string sourcePath = Path.GetDirectoryName(sourceFilePath);
                if (false == Directory.Exists(sourcePath))
                    return fileBody;

                if (false == File.Exists(sourceFilePath))
                    return fileBody;

                using (StreamReader sr = new StreamReader(sourceFilePath))
                {
                    fileBody = sr.ReadToEnd();
                }

                return fileBody;
            }
            catch (Exception)
            {
                throw;
            }
        }
        private void MoveSubstrateHistoryFile(SubstrateType type, string substrateName, string newPath)
        {
            string sourceFilePath = string.Format(@"{0}\{1}\{2}{3}", BasePathForSubstrate, type.ToString(), substrateName, LogFileExtension);
            string destFilePath = string.Format(@"{0}\{1}{2}", newPath, substrateName, LogFileExtension);

            try
            {
                string sourcePath = Path.GetDirectoryName(sourceFilePath);
                if (false == Directory.Exists(sourcePath))
                    return;

                string destPath = Path.GetDirectoryName(destFilePath);
                if (false == Directory.Exists(destPath))
                    Directory.CreateDirectory(destPath);

                if (false == File.Exists(sourceFilePath))
                    return;

                if (File.Exists(destFilePath))
                    File.Delete(destFilePath);

                File.Move(sourceFilePath, destFilePath);
            }
            catch (Exception)
            {

                throw;
            }
        }
        private void RenameBinSubstrateFile(string oldName, string newName)
        {
            string sourceFilePath = string.Format(@"{0}\{1}\{2}{3}", BasePathForSubstrate, SubstrateType.Bin.ToString(), oldName, LogFileExtension);
            string destFilePath = string.Format(@"{0}\{1}\{2}{3}", BasePathForSubstrate, SubstrateType.Bin.ToString(), newName, LogFileExtension);

            try
            {
                string sourcePath = Path.GetDirectoryName(sourceFilePath);
                if (false == Directory.Exists(sourcePath))
                    return;

                string destPath = Path.GetDirectoryName(destFilePath);
                if (false == Directory.Exists(destPath))
                    Directory.CreateDirectory(destPath);

                if (false == File.Exists(sourceFilePath))
                    return;

                if (File.Exists(destFilePath))
                    File.Delete(destFilePath);

                File.Move(sourceFilePath, destFilePath);
            }
            catch (Exception)
            {

            }
        }        
        private void WriteSubstrateLog(int portId, string carrierId, string substrateName, SubstrateBasedEventType type, SubstrateType substrateType, string message)
        {
            // Substrate History 기록
            WriteSubstrateLog(substrateName, type, substrateType, message);

            // Carrier History 에도 기록
            WriteCarrierLog(portId, carrierId, substrateName, type, message);
        }
        private void WriteSubstrateLog(string substrateName, SubstrateBasedEventType type, SubstrateType substrateType, string message)
        {
            string filePath = string.Format(@"{0}\{1}\{2}{3}", BasePathForSubstrate, substrateType.ToString(), substrateName, LogFileExtension);
            DateTime time = DateTime.Now;
            var logEntry = string.Format("{0:d2}/{1:d2}-{2:d2}:{3:d2}:{4:d2}.{5:d3}\t{6}\t{7}\t{8}\t{9}",
                time.Month,
                time.Day,
                time.Hour,
                time.Minute,
                time.Second,
                time.Millisecond,
                string.Empty,       // Carrier Event Type
                substrateName,      // SubstrateName
                type.ToString(),    // Substrate Event Type
                message);

            EnqueueLogToWrite(filePath, logEntry);

            #region <원본>
            //try
            //{
            //    string dirName = Path.GetDirectoryName(filePath);
            //    if (false == Directory.Exists(dirName))
            //        Directory.CreateDirectory(dirName);

            //    using (FileStream fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
            //    using (StreamWriter sw = new StreamWriter(fs))
            //    {
            //        sw.AutoFlush = true;

            //        DateTime time = DateTime.Now;
            //        var logEntry = string.Format("{0:d2}:{1:d2}:{2:d2}.{3:d3}\t{4}\t{5}\t{6}\t{7}",
            //            time.Hour,
            //            time.Minute,
            //            time.Second,
            //            time.Millisecond,
            //            string.Empty,       // Carrier Event Type
            //            substrateName,      // SubstrateName
            //            type.ToString(),    // Substrate Event Type
            //            message);

            //        sw.WriteLine(logEntry);
            //    }
            //}
            //catch (Exception)
            //{
            //}
            #endregion </원본>
        }
        private void WriteCarrierLog(int portId, string carrierId, string substrateName, SubstrateBasedEventType type, string message)
        {
            if (false == CurrentWorkingPath.TryGetValue(portId, out string basePath))
                return;

            string filePath = string.Format(@"{0}\{1}{2}", basePath, carrierId, LogFileExtension);
            DateTime time = DateTime.Now;
            var logEntry = string.Format("{0:d2}/{1:d2}-{2:d2}:{3:d2}:{4:d2}.{5:d3}\t{6}\t{7}\t{8}\t{9}",
                time.Month,
                time.Day,
                time.Hour,
                time.Minute,
                time.Second,
                time.Millisecond,
                string.Empty,       // Carrier Event Type
                substrateName,      // SubstrateName
                type.ToString(),    // Substrate Event Type
                message);

            if(_logMessageToDisplay != null)
            {
                _logMessageToDisplay(portId, logEntry);
            }

            EnqueueLogToWrite(filePath, logEntry);

            #region <원본>
            //try
            //{
            //    string dirName = Path.GetDirectoryName(filePath);
            //    if (false == Directory.Exists(dirName))
            //        Directory.CreateDirectory(dirName);

            //    using (FileStream fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
            //    using (StreamWriter sw = new StreamWriter(fs))
            //    {
            //        sw.AutoFlush = true;

            //        DateTime time = DateTime.Now;
            //        var logEntry = string.Format("{0:d2}:{1:d2}:{2:d2}.{3:d3}\t{4}\t{5}\t{6}\t{7}",
            //            time.Hour,
            //            time.Minute,
            //            time.Second,
            //            time.Millisecond,
            //            string.Empty,       // Carrier Event Type
            //            substrateName,      // SubstrateName
            //            type.ToString(),    // Substrate Event Type
            //            message);

            //        sw.WriteLine(logEntry);
            //    }
            //}
            //catch (Exception)
            //{
            //}
            #endregion </원본>
        }
        private void WriteCarrierLog(int portId, string carrierId, CarrierBasedEventType type, string message)
        {
            if (false == CurrentWorkingPath.TryGetValue(portId, out string basePath))
                return;

            string filePath = string.Format(@"{0}\{1}{2}", basePath, carrierId, LogFileExtension);
            DateTime time = DateTime.Now;
            var logEntry = string.Format("{0:d2}/{1:d2}-{2:d2}:{3:d2}:{4:d2}.{5:d3}\t{6}\t{7}\t{8}\t{9}",
                time.Month,
                time.Day,
                time.Hour,
                time.Minute,
                time.Second,
                time.Millisecond,
                type.ToString(),        // Carrier Event Type
                string.Empty,           // SubstrateName
                string.Empty,           // Substrate Event Type
                message);

            EnqueueLogToWrite(filePath, logEntry);

            #region <원본>
            //try
            //{
            //    string dirName = Path.GetDirectoryName(filePath);
            //    if (false == Directory.Exists(dirName))
            //        Directory.CreateDirectory(dirName);

            //    using (FileStream fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
            //    using (StreamWriter sw = new StreamWriter(fs))
            //    {
            //        sw.AutoFlush = true;

            //        DateTime time = DateTime.Now;
            //        var logEntry = string.Format("{0:d2}:{1:d2}:{2:d2}.{3:d3}\t{4}\t{5}\t{6}\t{7}",
            //            time.Hour,
            //            time.Minute,
            //            time.Second,
            //            time.Millisecond,
            //            type.ToString(),        // Carrier Event Type
            //            string.Empty,           // SubstrateName
            //            string.Empty,           // Substrate Event Type
            //            message);

            //        sw.WriteLine(logEntry);
            //    }
            //}
            //catch (Exception)
            //{
            //}
            #endregion </원본>
        }

        private void EnqueueLogToWrite(string filePath, string logEntry)
        {
            QueueToWrite.Enqueue(Tuple.Create(filePath, logEntry));
        }
        private void WriteLog(string filePath, string logEntry)
        {
            try
            {
                string dirName = Path.GetDirectoryName(filePath);
                if (false == Directory.Exists(dirName))
                    Directory.CreateDirectory(dirName);

                using (FileStream fs = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.AutoFlush = true;

                    sw.WriteLine(logEntry);
                }
            }
            catch (Exception)
            {
            }
        }
        #endregion </Internal>

        #endregion </Methods>
    }
    #endregion </Class>
}
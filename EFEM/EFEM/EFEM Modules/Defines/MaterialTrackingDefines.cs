using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFEM.Defines.MaterialTracking
{
    #region <Enumerations>
    public enum ModuleType
    {
        UnknownLocation,
        LoadPort,
        Robot,
        ProcessModule
    }
    public enum SubstrateTransferStates
    {
        AtSource,
        AtWork,
        AtDestination
    }
    public enum ProcessingStates
    {
        NeedsProcessing,       // 공정 전
        InProcess,             // 공정 중
        Processed,              // 공정 완료(성공)
        Rejected,               // 실패
        Stopped,                // 정지
        Aborted,                // 취소
        Skipped,                // 스킵
        Lost,                   // 유실
    }
    public enum IdReadingStates
    {
        NotConfirmed,
        WaitingForHost,
        Confirmed,
        ConfirmationFailed
    }
    public enum SubstrateLocationState
    {
        Unoccupied,
        Occopied
    }
    #endregion </Enumerations>

    #region <Class>
    //public class LocationInfo
    //{
    //    #region <Constructors>
    //    public LocationInfo(string location, int slot)
    //    {
    //        Location = location;
    //        Slot = slot;
    //    }
    //    #endregion </Constructors>

    //    #region <Properties>
    //    public string Location { get; set; }
    //    public int Slot { get; set; }
    //    #endregion </Properties>
    //}
    public static class ETC
    {
        public const string DateTimeFormat = "yyyy/MM/dd HH:mm:ss";
    }
    public static class BaseCarrierAttributeKeys
    {
        public const string DateTimeFormat = "yyyy/MM/dd HH:mm:ss";

        public const string KeyHasCarrier = "HasCarrier";
        public const string KeyLoadTime = "LoadTime";
        public const string KeyLotId = "LotId";
        public const string KeyCarrierId = "CarrierId";
        public const string KeyCarrierAccessStatus = "CarrierAccessStatus";
        public const string KeyUnloadTime = "UnloadTime";
    }
    public static class BaseSubstrateAttributeKeys
    {
        public const string Name = "Name";
        public const string LotId = "LotId";
        public const string SourcePortId = "SourcePortId";
        public const string SourceSlot = "SourceSlot";
        public const string DestinationPortId = "DestinationPortId";
        public const string DestinationSlot = "DestinationSlot";
        public const string Location = "Location";
        public const string ProcessJobId = "ProcessJobId";
        public const string ControlJobId = "ControlJobId";
        public const string RecipeId = "RecipeId";
        public const string TransPortState = "TransPortState";
        public const string ProcessingState = "ProcessingState";
        public const string IdReadingState = "IdReadingState";
        public const string Usage = "Usage";
        public const string DoNotProcessFlag = "DoNotProcessFlag";
        public const string SourceCarrierId = "SourceCarrierId";

        public const string LoadingTimeToSource = "LoadingTimeToSource";
        public const string LoadingTimeToDestination = "LoadingTimeToDestination";
        public const string UnloadingTimeFromSource = "UnloadingTimeFromSource";
        public const string UnloadingTimeFromDestination = "UnloadingTimeFromDestination";

        public const string LoadingTimeToPM = "LoadingTimeTo";          // 이 이름 옆에 PM이름이 붙음
        public const string UnloadingTimeFromPM = "UnloadingTimeFrom";      // 이 이름 옆에 PM이름이 붙음
    }

    public static class CarrierAttributeRecoveryDefines
    {
        public const string FileName = "CarrierInformation";
        public const string FileExtension = "xml";
        public const string FileRootName = "CarrierAttributeInformation";
        public static readonly string FileNameWithExtension = string.Format("{0}.{1}", FileName, FileExtension);
        public static readonly string FilePath = string.Format(@"{0}\..\Recovery\LP", Environment.CurrentDirectory);
    }

    public static class RecoveryFileDefines
    {
        public const string FileExtension = "xml";
        public const string FileRootName = "SubstrateInformation";
        public static readonly string RecoveryFilePath = string.Format(@"{0}\..\Recovery\Substrates", Environment.CurrentDirectory);

        public const string LocationTypeKey = "LocationType";
        public const string LocationTypeLoadPort = "LocationTypeLoadPort";
        public const string LocationTypeProcessModule = "LocationTypeProcessModule";
        public const string LocationTypeRobot = "LocationTypeRobot";
        public const string LocationTypeUnknown = "LocationTypeUnknown";

        public const string LoadPortLocationLoadPortName = "LoadPortLocationLoadPortName";
        public const string LoadPortLocationPortId = "LoadPortLocationPortId";
        public const string LoadPortLocationSlot = "LoadPortLocationSlot";

        public const string ProcessModuleLocationProcessModuleName = "ProcessModuleLocationProcessModuleName";

        public const string RobotLocationRobotName = "RobotLocationRobotName";
        public const string RobotLocationArm = "RobotLocationArm";
    }
    #endregion </Class>
}
using System;
using System.Threading;
using System.Collections.Generic;

using EFEM.Defines.Common;
using EFEM.MaterialTracking.LocationServer;
using EFEM.Defines.MaterialTracking;
using EFEM.MaterialTracking;

namespace EFEM.Defines.AtmRobot
{
    #region <Enumerations>
    public enum RobotCommands
    {
        Idle,
        Initialize,
        ApproachForPick,
        MoveToPick,
        Pick,
        PickStepOne,       // 2024.10.10. by dwlim. [ADD] Pick 구분동작 추가로 인한 Command 추가
        PickStepTwo,       // 2024.10.10. by dwlim. [ADD] Pick 구분동작 추가로 인한 Command 추가
        PickTest,
        ApproachForPlace,
        MoveToPlace,
        Place,
        PlaceStepOne,       // 2024.09.25. by dwlim. [ADD] Place 구분동작 추가로 인한 Command 추가
        PlaceStepTwo,       // 2024.09.25. by dwlim. [ADD] Place 구분동작 추가로 인한 Command 추가
        PlaceTest,
        ZaxisUp,
        ZaxisMid,
        ZaxisDown,
        GetWaferPresence,
        AmpOn,
        AmpOff,
        SetLowSpeed,
        SetHighSpeed,
        GetLowSpeed,
        GetHighSpeed,
        ClearError,
        GetError,
        DoorLock,
        DoorUnlock,
        Grip,
        Ungrip,
        Hello,
        Status,             // 2024.09.25. by dwlim. [ADD] NRC는 Status로 Wafer Presence 확인할 수 있음
    }
    public enum RobotArmTypes
    {
        UpperArm,
        LowerArm,
        All
    }
    #endregion </Enumerations>

    #region <Classes>
    public class AtmRobotLogger : ModuleLogger
    {
        public AtmRobotLogger(string name) : base(LogTypes.AtmRobot, name) { }

        public void WriteOperationStartLog(RobotCommands command, RobotArmTypes arm, string location)
        {
            _logType = LogTitleTypes.OPER;

            WriteLog(string.Format("----- {0}, Arm : {1}, Location : {2} -----", command.ToString(), arm.ToString(), location));
        }

        public void WriteOperationStartLog(RobotCommands command)
        {
            _logType = LogTitleTypes.OPER;

            WriteLog(string.Format("----- {0} -----", command.ToString()));
        }
        public void WriteOperationEndLog(RobotCommands command, CommandResults result)
        {
            if (result.Equals(CommandResult.Proceed))
                return;

            _logType = LogTitleTypes.OPER;
            WriteLog(string.Format("----- Result : {0}, Description : {1}", result.CommandResult.ToString(), result.Description));
        }
        public void WriteCommLog(string message, bool received)
        {
            if (false == received)
            {
                _logType = LogTitleTypes.SEND;

            }
            else
            {
                _logType = LogTitleTypes.RECV;
            }

            WriteLog(message);
        }
    }

    public class RobotWorkingInfo
    {
        public void Init()
        {
            //Source = string.Empty;
            //Destination = string.Empty;
            //SourceSlot = -1;
            //DestinationSlot = -1;
            //TargetLocation = string.Empty;
            //Slot = -1;
            ActionArm = RobotArmTypes.All;
            SubstrateName = string.Empty;

            Location = new Location("Unknown");
        }
        public bool IsLocationProcessModule()
        {
            return Location is ProcessModuleLocation;
        }
        //public string TargetLocation { get; }
        //public int Slot { get; }
        public string SubstrateName { get; set; }
        //public string Source { get; set; }
        //public int SourceSlot { get; set; }
        //public string Destination { get; set; }
        //public int DestinationSlot { get; set; }
        public RobotArmTypes ActionArm { get; set; }
        public Location Location { get; set; }
    }

    public class RobotStateInformation
    {
        #region <Constructors>
        public RobotStateInformation()
        {
            //Substrates.Add(RobotArmTypes.UpperArm, null);
            //Substrates.Add(RobotArmTypes.LowerArm, null);
        }
        #endregion </Constructors>

        #region <Fields>
        private readonly ReaderWriterLockSlim SlimLock = new ReaderWriterLockSlim();
        //private readonly Dictionary<RobotArmTypes, Substrate> Substrates = new Dictionary<RobotArmTypes, Substrate>();
        #endregion </Fields>

        #region <Properties>
        public bool Initialized { get; private set; }
        #endregion </Properties>

        #region <Methods>
        public void UpdateInitialization(bool initialized)
        {
            Initialized = initialized;
        }
        //public void ClearSubstrate(RobotArmTypes armType)
        //{
        //    SlimLock.EnterWriteLock();

        //    Substrates[armType] = null;

        //    SlimLock.ExitWriteLock();
        //}
        //public void SetSubstrate(RobotArmTypes armType, ref Substrate substrate)
        //{
        //    SlimLock.EnterWriteLock();

        //    Substrates[armType] = substrate;

        //    SlimLock.ExitWriteLock();
        //}
        //public bool GetSubstrate(RobotArmTypes armType, ref Substrate substrate)
        //{
        //    SlimLock.EnterReadLock();

        //    substrate = Substrates[armType];

        //    SlimLock.ExitReadLock();

        //    return substrate != null;
        //}
        //public bool GetSubstrate(string substrateName, ref Substrate substrate)
        //{
        //    SlimLock.EnterReadLock();

        //    bool found = false;
        //    foreach (var item in Substrates)
        //    {
        //        if (item.Value == null)
        //            continue;

        //        if (item.Value.GetName().Equals(substrateName))
        //        {
        //            substrate = item.Value;
        //            found = true;
        //            break;
        //        }
        //    }

        //    SlimLock.ExitReadLock();

        //    return found;
        //}
        //public bool GetSubstrate(ref Dictionary<RobotArmTypes, Substrate> substrates)
        //{
        //    SlimLock.EnterReadLock();

        //    substrates.Clear();
        //    substrates = new Dictionary<RobotArmTypes, Substrate>(Substrates);
        //    SlimLock.ExitReadLock();

        //    return true;
        //}
        //public bool GetAvailableArm(ref List<RobotArmTypes> availableArms)
        //{
        //    SlimLock.EnterReadLock();

        //    foreach (var item in Substrates)
        //    {
        //        if (item.Value == null)
        //        {
        //            availableArms.Add(item.Key);
        //        }
        //    }
        //    SlimLock.ExitReadLock();

        //    return (availableArms.Count > 0);
        //}
        #endregion </Methods>
    }
    #endregion </Classes>
}

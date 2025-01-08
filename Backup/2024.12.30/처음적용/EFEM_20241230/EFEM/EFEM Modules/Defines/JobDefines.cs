using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFEM.Defines.Job
{
    #region <Control Job>
    public enum ControlJobStates
    {
        Unknown = 0,
        Queued,
        Selected,
        WaitingForStart,
        Executing,
        Paused,
        Completed
    }
    #endregion </Control Job>

    #region <Process Job>
    public enum ProcessJobStates
    {
        Queued = 0,
        SettingUp,
        WaitingForStart,
        Processing,
        ProcessComplete,
        Reserved,
        Pausing,
        Paused,
        Stopping,
        Aborting,
    }
    public enum MaterialTyps
    {
        Wafers,
        Cassettes,
        Die,
        Boats,
        Ingots,
        Leadframes,
        Lots,
        Magazines,
        Packages,
        Plates,
        Tubes,
        Waterframes,
        Carrier,        // (FOUP, SMIF pod, cassette)
        Substrate
    }
    #endregion </Process Job>
}

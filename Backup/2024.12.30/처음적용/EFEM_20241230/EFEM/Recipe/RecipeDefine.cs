using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrameOfSystem3.Recipe
{
    /// <summary>
    /// 2020.06.29 by yjlee [ADD] Enumerate the common parameters.
    /// </summary>
    public enum PARAM_COMMON
    {
        PROCESS_FILE_PATH,
        PROCESS_FILE_NAME,

        UseSecsGem,

        UseCycleMode,
        UseUtilityAlarm,
    }

    /// <summary>
    /// 2020.06.29 by yjlee [ADD] Enumerate the equipment parameters.
    /// </summary>
    public enum PARAM_EQUIPMENT
    {
		MachineLanguage,
		MachineName,
        UnlockParameterChange,

        UseRobotUpperArm,
        UseRobotLowerArm,

        UseLoadPort1,
        UseLoadPort2,
        UseLoadPort3,
        UseLoadPort4,
        UseLoadPort5,
        UseLoadPort6,

        LoadPortType1,
        LoadPortType2,
        LoadPortType3,
        LoadPortType4,
        LoadPortType5,
        LoadPortType6,

        UseCapacityLimitBin1,
        UseCapacityLimitBin2,
        UseCapacityLimitBin3,

        AvailableCarrierCapacityBin1,
        AvailableCarrierCapacityBin2,
        AvailableCarrierCapacityBin3,

        HandlingWaitTime,
        BinWaferStepId,
        UseRecipeDownload,
    }
}
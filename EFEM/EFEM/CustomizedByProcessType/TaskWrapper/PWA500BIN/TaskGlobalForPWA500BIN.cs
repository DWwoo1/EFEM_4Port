using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Define.DefineEnumProject.DigitalIO.PWA500BIN;

namespace FrameOfSystem3.Task
{
    class TaskGlobalForPWA500BIN : TaskGlobal
    {
        #region <Constructors>
        public TaskGlobalForPWA500BIN(int nIndexOfTask, string strTaskName)
            : base(nIndexOfTask, strTaskName)
        {

        }
        #endregion </Constructors>

        #region <Fields>
        #endregion </Fields>

        #region <Properties>
        #endregion </Properties>

        #region <Methods>

        #region <Overrides>
        protected override void GetDoorLockSensorSignalList(out List<int> indexOfSignals)
        {
            indexOfSignals = new List<int>
            {
                (int)EN_DIGITAL_IN.EFEM_DOOR_CLOSE
            };
        }
        protected override void GetFanAlarmSensorSignalList(out List<int> indexOfSignals)
        {
            indexOfSignals = new List<int>
            {
                (int)EN_DIGITAL_IN.EFEM_POWER_BOX_FAN_STATUS,
                (int)EN_DIGITAL_IN.EFEM_IO_BOX_FAN_STATUS,
                (int)EN_DIGITAL_IN.ROBOT_CONTROLLER_FAN_ALARM
            };
        }
        protected override void GetFFUAlarmSignalList(out List<int> indexOfSignals)
        {
            indexOfSignals = new List<int>
            {
                (int)EN_DIGITAL_IN.FFU_ALARM,
            };
        }
        protected override void GetIonizerAlarmSignalList(out List<int> indexOfSignals)
        {
            indexOfSignals = new List<int>
            {
                (int)EN_DIGITAL_IN.IONIZER_1_ALARM_STATUS,
                (int)EN_DIGITAL_IN.IONIZER_2_ALARM_STATUS,
                (int)EN_DIGITAL_IN.IONIZER_3_ALARM_STATUS,
                (int)EN_DIGITAL_IN.IONIZER_4_ALARM_STATUS
            };
        }
        protected override void GetAirAlarmSignalList(out List<int> indexOfSignals)
        {
            indexOfSignals = new List<int>
            {
                (int)EN_DIGITAL_IN.EFEM_MAIN_CDA_PRESSURE_SWITCH,
                (int)EN_DIGITAL_IN.EFEM_MAIN_VACUUM_PRESSURE_SWITCH,
                (int)EN_DIGITAL_IN.ROBOT_CDA_PRESSURE_SWITCH,
                (int)EN_DIGITAL_IN.IONIZER_CDA_PRESSURE_SWITCH,
                (int)EN_DIGITAL_IN.IONIZER_1_FLOW_METER,
                (int)EN_DIGITAL_IN.IONIZER_2_FLOW_METER,
                (int)EN_DIGITAL_IN.IONIZER_3_FLOW_METER,
                (int)EN_DIGITAL_IN.IONIZER_4_FLOW_METER
            };
        }
        #endregion </Overrides>
        #endregion </Methods>
    }
}

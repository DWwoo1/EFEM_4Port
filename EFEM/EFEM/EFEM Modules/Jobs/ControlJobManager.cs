using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EFEM.Defines.Job;
using EFEM.JobManager.CJ;

namespace EFEM.JobManager
{
    class ControlJobManager
    {
        #region <Constructors>
        private ControlJobManager() 
        {
            ControlJobs = new Dictionary<int, ControlJob>();
        }
        #endregion </Constructors>

        #region <Fields>
        private static ControlJobManager _instance = null;
        private readonly Dictionary<int, ControlJob> ControlJobs = null;
        #endregion </Fields>

        #region <Properties>
        public static ControlJobManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ControlJobManager();
                }

                return _instance;
            }
        }
        #endregion </Properties>

        #region <Methods>
        public void CreateControlJob(int portId, ControlJob cj)
        {
            ControlJobs[portId] = cj;
        }
        public void SetJobStatus(int portId, ControlJobStates status)
        {
            if (false == ControlJobs.ContainsKey(portId))
                return;

            ControlJobs[portId].SetControlJobStatus(status);
        }
        public ControlJobStates GetJobStatus(int portId)
        {
            if (false == ControlJobs.ContainsKey(portId))
                return ControlJobStates.Unknown;

            return ControlJobs[portId].ControlJobStatus;
        }
        #endregion </Methods>
    }
}

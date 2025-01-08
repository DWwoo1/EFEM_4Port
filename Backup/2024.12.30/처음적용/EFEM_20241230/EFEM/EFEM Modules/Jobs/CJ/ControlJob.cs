using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using EFEM.Defines.Job;
using EFEM.JobManager.PJ;

namespace EFEM.JobManager.CJ
{
    public class ControlJob
    {
        #region <Constructors>
        public ControlJob(int portId, string name)
        {
            PortId = portId;
            ControlJobId = name;

            SlimLock = new ReaderWriterLockSlim();
            _processJobs = new Dictionary<int, ProcessJob>();

            SetControlJobStatus(ControlJobStates.Queued);

            // TODO : Material Out Spec 추가 필요
        }
        #endregion </Constructors>

        #region <Fields>
        private Dictionary<int, ProcessJob> _processJobs = null;
        private readonly ReaderWriterLockSlim SlimLock = null;
        #endregion </Fields>

        #region <Properties>
        public int PortId { get; private set; }
        public string ControlJobId { get; private set; }
        public ControlJobStates ControlJobStatus { get; private set; }
        public bool AutoStart { get; private set; }
        #endregion </Properties>

        #region <Methods>
        public void SetControlJobStatus(ControlJobStates status)
        {
            ControlJobStatus = status;
        }
        public void CreateProcessJobs(Dictionary<int, ProcessJob> pjs)
        {
            SlimLock.EnterWriteLock();

            _processJobs = new Dictionary<int, ProcessJob>(pjs);            

            SlimLock.ExitWriteLock();
        }
        public void UpdateProcessJob(int slot, ProcessJob pj)
        {
            SlimLock.EnterWriteLock();

            _processJobs[slot] = pj;

            SlimLock.ExitWriteLock();
        }
        public void GetProcessJobs(ref Dictionary<int, ProcessJob> jobs)
        {
            SlimLock.EnterReadLock();

            if (_processJobs.Count != jobs.Count)
            {
                jobs = new Dictionary<int, ProcessJob>(_processJobs);
            }
            else
            {
                foreach (var item in _processJobs)
                {
                    jobs[item.Key] = item.Value;
                }
            }

            SlimLock.ExitReadLock();
        }
        #endregion </Methods>
    }
}

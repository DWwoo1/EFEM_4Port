using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using EFEM.Defines.Job;

namespace EFEM.JobManager.PJ
{
    public class ProcessJob
    {
        #region <Constructors>
        public ProcessJob(string name, MaterialTyps materialType,
            Dictionary<int, string> materials, string recipeId, string cjName,
            bool autoStart = true)
        {
            SlimLock = new ReaderWriterLockSlim();

            ProcessJobId = name;
            RecipeId = recipeId;

            ControlJobId = cjName;
            AutoStart = autoStart;
            MaterialType = materialType;

            MaterialsToProcess = new OrderedDictionary();
            AddMaterials(materials);
            
            ProcessJobStatus = ProcessJobStates.Queued;
        }
        #endregion </Constructors>

        #region <Fields>
        private readonly OrderedDictionary MaterialsToProcess = null;
        private readonly ReaderWriterLockSlim SlimLock = null;
        #endregion </Fields>

        #region <Properties>
        public string ProcessJobId { get; private set; }
        public ProcessJobStates ProcessJobStatus { get; private set; }
        public MaterialTyps MaterialType { get; private set; }
        public bool AutoStart { get; private set; }
        public string RecipeId { get; private set; }
        public string ControlJobId { get; private set; }
        public int CountOfSubstrates
        {
            get
            {
                SlimLock.EnterReadLock();
                int count = MaterialsToProcess.Count;
                SlimLock.ExitReadLock();
                
                return count;
            }
        }
        public List<string> MaterialList
        {
            get
            {
                SlimLock.EnterReadLock();
                List<string> materialNameList = MaterialsToProcess.Values.Cast<string>().ToList();
                SlimLock.ExitReadLock();

                return materialNameList;
            }
        }
        #endregion </Properties>

        #region <Methods>
        public void AddMaterials(Dictionary<int, string> materials)
        {
            SlimLock.EnterWriteLock();

            foreach (var item in materials)
            {
                MaterialsToProcess.Add(item.Key, item.Value);
            }  

            SlimLock.ExitWriteLock();
        }
        public void RemoveMaterial(int slot, string materialName)
        {
            SlimLock.EnterWriteLock();

            if (MaterialsToProcess.Contains(slot))
            {
                MaterialsToProcess.Remove(slot);
            }

            SlimLock.ExitWriteLock();
        }
        #endregion </Methods>
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EquipmentState_;

using FrameOfSystem3.DynamicLink_;
using EFEM.MaterialTracking;
using EFEM.Modules;
using EFEM.Defines.LoadPort;
using EFEM.ActionScheduler.LoadPortActionSchedulers;

namespace EFEM.ActionScheduler
{
    public class LoadPortActionSchedulerManager
    {
        #region <Constructors>        
        public LoadPortActionSchedulerManager()
        {
            _loadPortManager = LoadPortManager.Instance;
            LoadPortSchedulers = new Dictionary<int, BaseLoadPortActionScheduler>();
        }
        #endregion </Constructors>

        #region <Fields>
        private static LoadPortActionSchedulerManager _instance = null;
        private static LoadPortManager _loadPortManager = null;

        private readonly Dictionary<int, BaseLoadPortActionScheduler> LoadPortSchedulers = null;
        #endregion </Fields>

        #region <Properties>
        public static LoadPortActionSchedulerManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new LoadPortActionSchedulerManager();
                }

                return _instance;
            }
        }
        #endregion </Properties>

        #region <Methods>
        public bool CreateScheduler(int lpIndex, BaseLoadPortActionScheduler scheduler)
        {
            LoadPortSchedulers[lpIndex] = scheduler;
            return true;
        }
        
        public void InitSchedulers(int lpIndex)
        {
            if (LoadPortSchedulers == null || false == LoadPortSchedulers.ContainsKey(lpIndex))
                return;

            LoadPortSchedulers[lpIndex].InitScheduler();
        }
        public CARRIER_PORT_TYPE ExecuteSchedulers(int lpIndex)
        {           
            if (LoadPortSchedulers == null || false == LoadPortSchedulers.ContainsKey(lpIndex))
                return CARRIER_PORT_TYPE.SELECTION;

            return LoadPortSchedulers[lpIndex].ExecuteSchedulers();
        }

        public void PrepareCarrierIdVerificationInformation(int lpIndex, Dictionary<string, string> idInformation)
        {
            if (LoadPortSchedulers == null || false == LoadPortSchedulers.ContainsKey(lpIndex))
                return;

            LoadPortSchedulers[lpIndex].PrepareCarrierIdVerificationInformation(idInformation);
        }

        public VarificationResults ExecuteCarrierIdVerification(int lpIndex)
        {
            if (LoadPortSchedulers == null || false == LoadPortSchedulers.ContainsKey(lpIndex))
                return VarificationResults.Error;

            return LoadPortSchedulers[lpIndex].ExecuteCarrierIdVerification();
        }

        public void PrepareCarrierSlotMapVerificationInformation(int lpIndex, Dictionary<int, CarrierSlotMapStates> slotStatus)
        {
            if (LoadPortSchedulers == null || false == LoadPortSchedulers.ContainsKey(lpIndex))
                return;

            LoadPortSchedulers[lpIndex].PrepareCarrierSlotMapVerificationInformation(slotStatus);
        }

        public VarificationResults ExecuteCarrierSlotVerification(int lpIndex)
        {
            if (LoadPortSchedulers == null || false == LoadPortSchedulers.ContainsKey(lpIndex))
                return VarificationResults.Error;

            return LoadPortSchedulers[lpIndex].ExecuteCarrierIdVerification();
        }

        public EN_PORT_STATUS GetCarrierPortStatus(CARRIER_PORT_TYPE portType)
        {
            switch (portType)
            {
                case CARRIER_PORT_TYPE.SELECTION:
                    return EN_PORT_STATUS.SELECTION;

                case CARRIER_PORT_TYPE.READY_TO_LOAD:
                    return EN_PORT_STATUS.EMPTY;

                case CARRIER_PORT_TYPE.ACTION_LOAD:
                    return EN_PORT_STATUS.WORKING;

                case CARRIER_PORT_TYPE.READY_TO_UNLOAD:
                    return EN_PORT_STATUS.FINISHED;

                case CARRIER_PORT_TYPE.ACTION_UNLOAD:
                    return EN_PORT_STATUS.WORKING_BUT_FINISHED;

                default:
                    return EN_PORT_STATUS.UNABLE;
            }
        }
        public void ChangeSlotMapForDryRun(int lpIndex)
        {
            if (LoadPortSchedulers == null || false == LoadPortSchedulers.ContainsKey(lpIndex))
                return;

            LoadPortSchedulers[lpIndex].ChangeSlotMapForDryRun();
        }
        #endregion </Methods>

    }
}

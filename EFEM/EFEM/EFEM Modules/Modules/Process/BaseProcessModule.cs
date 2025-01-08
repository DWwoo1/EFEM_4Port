using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using EquipmentState_;
using FileIOManager_;
using FileComposite_;

using EFEM.MaterialTracking;
using EFEM.MaterialTracking.LocationServer;
using EFEM.Defines.Common;
using EFEM.Defines.ProcessModule;

namespace EFEM.Modules.ProcessModule
{
    public abstract class BaseProcessModule
    {
        #region <Constructors>
        public BaseProcessModule(int moduleIndex, Communicator.BaseProcessModuleCommunicator communicator, string name, string[] locationNames, bool simulation)
        {
            Name = name;
            ModuleIndex = moduleIndex;
            Simulation = simulation;
            Logger = new ProcessModuleLogger(name);

            EquipmentState = EQUIPMENT_STATE.UNDEFINED;
            RecipeId = "";
            
            Substrates = new ConcurrentDictionary<DateTime, Substrate>();
            //SubstratesInHandlingLocation = new ConcurrentDictionary<string, Substrate>();
            _communicator = communicator;
            _communicator.AssignLogger(ref Logger);

            _locationServer = LocationServer.Instance;
            ProcessModuleLocations = new Dictionary<string, ProcessModuleLocation>();
            LocationNames = new string[locationNames.Length];
            for (int i = 0; i < locationNames.Length; ++i)
            {
                LocationNames[i] = locationNames[i];
                ProcessModuleLocations[locationNames[i]] = new ProcessModuleLocation(Name, locationNames[i]);
            }
            _locationServer.AddProcessModuleLocation(Name, ProcessModuleLocations);
            
            _substrateManager = SubstrateManager.Instance;
            _substrateManager.AddProcessModuleBuffers(Name);

            PortIdByLocation = new Dictionary<string, int>();
            for (int i = 0; i < LocationNames.Length; ++i)
            {
                string locationName = LocationNames[i];
                //SubstratesInHandlingLocation[locationName] = null;
                PortIdByLocation[locationName] = 0;
            }

            int[] ports = new int[PortIdByLocation.Count];
            MappingCommunicatorPortByLocation(LocationNames, ref ports);
            if (ports != null)
            {
                for (int i = 0; i < ports.Length; ++i)
                {
                    string location = LocationNames[i];
                    int port = ports[i];

                    PortIdByLocation[location] = port;
                }
            }

            _receivedData = new ConcurrentDictionary<int, Dictionary<string, string>>();
            
        }
        #endregion </Constructors>

        #region <Fields>
        //protected readonly ConcurrentDictionary<string, Substrate> SubstratesInHandlingLocation = null;
        protected readonly ConcurrentDictionary<DateTime, Substrate> Substrates = null;

        protected ConcurrentDictionary<int, Dictionary<string, string>> _receivedData = null;
        protected Communicator.BaseProcessModuleCommunicator _communicator = null;
        protected readonly Dictionary<string, int> PortIdByLocation = null;
        protected readonly ProcessModuleLogger Logger = null;
        protected readonly bool Simulation;
        protected bool _requestExit = false;

        private static SubstrateManager _substrateManager = null;
        
        private const string FileRootName = "ProcessModuleInformation";
        
        private const string KeySubstrateName = "SubstrateName";
        
        private static LocationServer _locationServer = null;
        protected readonly Dictionary<string, ProcessModuleLocation> ProcessModuleLocations = null;
        #endregion </Fields>

        #region <Properties>
        public string Name { get; private set; }
        public int ModuleIndex { get; private set; }
        public string LotId { get; set; }
        public EQUIPMENT_STATE EquipmentState { get; set; }
        public string RecipeId { get; set; }
        public NetworkInformation CommunicationInfo
        {
            get
            {
                if (_communicator == null)
                    return null;

                return _communicator.CommunicatorInfo;
            }
        }
        public string[] LocationNames { get; private set; }
        #endregion </Properties>

        #region <Methods>

        #region <Substrates>
        public bool AssignSubstrate(Substrate substrate)
        {
            foreach (var item in Substrates)
            {
                if (item.Value.GetName().Equals(substrate.GetName()))
                    return false;
            }

            Substrates[DateTime.Now] = substrate;
            substrate.SetLoadingTimeToProcessModule(Name);
            //SubstratesInHandlingLocation[targetLocation] = substrate;       // 자재가 로딩 될 때마다 계속 바뀔거다..

            return true;
        }
        public void RemoveSubstrateAll()
        {
            Substrates.Clear();
        }

        public void RemoveSubstrate(string substrateName)
        {
            foreach (var item in Substrates)
            {
                if (item.Value.GetName().Equals(substrateName))
                {
                    Substrates.TryRemove(item.Key, out _);
                }
            }
            
            //SubstratesInHandlingLocation[targetLocation] = null;
        }
        public bool HasLocation(string targetLocation)
        {
            if (LocationNames == null)
                return false;

            for (int i = 0; i < LocationNames.Length; ++i)
            {
                if (LocationNames[i].Equals(targetLocation))
                    return true;
            }

            return false;
        }
        public bool GetSubstrates(ref List<Substrate> substrates)
        {
            if (Substrates == null)
                return false;

            substrates = Substrates.Values.ToList();
            return true;
        }
        public bool GetSubstrate(string substrateName, ref Substrate substrate)
        {
            if (Substrates == null)
                return false;

            foreach (var item in Substrates)
            {
                if (item.Value.GetName().Equals(substrateName))
                {
                    substrate = item.Value;
                    return true;
                }
            }

            return false;
        }
        //public bool GetSubstrateInHandlingLocation(string location, ref Substrate substrate)
        //{
        //    return SubstratesInHandlingLocation.TryGetValue(location, out substrate);
        //}

        #endregion </Substrates>

        #region <SMEMA>

        #region <Send>
        public void ResetSignalsAll()
        {
            _communicator.ResetSignalsAll();
        }
        public virtual void SetLoadingSignal(string location, bool enabled)
        {
            _communicator.SetLoadingSignal(location, enabled);
        }
        public virtual void SetUnloadingSignal(string location, bool enabled)
        {
            _communicator.SetUnloadingSignal(location, enabled);
        }
        #endregion </Send>

        #region <Receive>
        public bool IsLoadingRequested(ref List<string> locationNames)
        {
            if (_communicator.IsLoadingRequestedBySignal(ref locationNames))
                return true;

            return IsLoadingRequestReceived(ref locationNames);
        }
        public bool IsUnloadingRequested(ref List<string> locationNames)
        {
            if (_communicator.IsUnloadingRequestedBySignal(ref locationNames))
                return true;

            return IsUnloadingRequestReceived(ref locationNames);
        }
        #endregion </Receive>

        #endregion </SMEMA>

        #region <Communication>

        #region <Connection>
        public bool InitCommunication()
        {
            return _communicator.InitConnection();
        }
        public bool ExitProcessModule()
        {
            _requestExit = true;
            return _communicator.ExitCommunication();
        }
        #endregion </Connection>

        #endregion </Communication>

        #region <Executing>
        public void Execute()
        {
            if (_requestExit)
                return;

            _communicator.UpdateReceivingInformations();

            Executing();
        }
        #endregion </Executing>

        #region <Internals>
        #endregion </Internals>

        #region <Abstract>
        protected abstract void MappingCommunicatorPortByLocation(string[] locations, ref int[] ports);      
        protected abstract bool IsLoadingRequestReceived(ref List<string> locationNames);
        protected abstract bool IsUnloadingRequestReceived(ref List<string> locationNames);
        protected abstract void Executing();

        #region <WCF>

        #region <Send>
        public abstract bool SendMessage(Location location, string title, Dictionary<string, string> messagePairs);
        public abstract bool SendMessage(Location location, string title, string substrateName);
        public abstract CommunicationResult IsSendingCompleted(Location location, string title);
        public abstract bool GetSendingResult(Location location, string title, ref Dictionary<string, string> receivedData);
        #endregion </Send>

        #region <Receive>
        public abstract void SetAckToReceivedMessage(Location location, string title, CommunicationResult result, string description);
        public abstract CommunicationResult IsMessageReceived(Location location, string title);
        public abstract bool GetReceivedData(Location location, string title, ref Dictionary<string, string> receivedData);
        #endregion </Receive>

        #endregion </WCF>

        #endregion </Abstract>

        #endregion </Methods>
    }
}

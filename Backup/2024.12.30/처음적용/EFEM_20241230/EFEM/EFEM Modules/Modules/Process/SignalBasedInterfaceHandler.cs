using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DigitalIO_;

using EFEM.Defines.ProcessModule;

namespace EFEM.Modules.ProcessModule
{
    public class SignalBasedInterfaceHandler
    {
        #region <Constructors>
        public SignalBasedInterfaceHandler()
        {
            LoadingInputSignalsByLocation = new Dictionary<string, int>();
            LoadingOutputSignalsByLocation = new Dictionary<string, int>();

            UnloadingInputSignalsByLocation = new Dictionary<string, int>();
            UnloadingOutputSignalsByLocation = new Dictionary<string, int>();

            LoadingRequested = new ConcurrentDictionary<string, bool>();
            UnloadingRequested = new ConcurrentDictionary<string, bool>();

            _digitalIO = DigitalIO.GetInstance();
        }
        #endregion </Constructors>

        #region <Fields>
        private readonly Dictionary<string, int> LoadingInputSignalsByLocation = null;
        private readonly Dictionary<string, int> UnloadingInputSignalsByLocation = null;
        private readonly Dictionary<string, int> LoadingOutputSignalsByLocation = null;
        private readonly Dictionary<string, int> UnloadingOutputSignalsByLocation = null;

        private readonly ConcurrentDictionary<string, bool> LoadingRequested = null;
        private readonly ConcurrentDictionary<string, bool> UnloadingRequested = null;

        private static DigitalIO _digitalIO = null;
        #endregion </Fields>

        #region <Properties>
        #endregion </Properties>

        #region <Methods>
        
        #region <Assign>
        public void AssignInputSignalsInLoadingLocation(string location, int signalIndex)
        {
            LoadingInputSignalsByLocation[location] = signalIndex;

            LoadingRequested[location] = false;
        }
        public void AssignOutputSignalsInLoadingLocation(string location, int signalIndex)
        {
            LoadingOutputSignalsByLocation[location] = signalIndex;
        }
        public void AssignInputSignalsInUnloadingLocation(string location, int signalIndex)
        {
            UnloadingInputSignalsByLocation[location] = signalIndex;

            UnloadingRequested[location] = false;
        }
        public void AssignOutputSignalsInUnloadingLocation(string location, int signalIndex)
        {
            UnloadingOutputSignalsByLocation[location] = signalIndex;
        }
        #endregion </Assign>

        #region <Send>
        public void ResetSignalsAll()
        {
            foreach (var item in LoadingOutputSignalsByLocation)
            {
                SetLoadingSignal(item.Key, false);
            }

            foreach (var item in UnloadingOutputSignalsByLocation)
            {
                SetUnloadingSignal(item.Key, false);
            }
        }
        public void SetLoadingSignal(string location, bool enabled)
        {
            int index = LoadingOutputSignalsByLocation[location];
            if (index < 0)
                return;

            _digitalIO.WriteOutput(index, enabled);
        }
        public void SetUnloadingSignal(string location, bool enabled)
        {
            int index = UnloadingOutputSignalsByLocation[location];
            if (index < 0)
                return;

            _digitalIO.WriteOutput(index, enabled);
        }
        #endregion </Send>

        #region <Receive>
        public bool IsLoadingRequestedBySignal(ref List<string> requestedLocation)
        {
            requestedLocation.Clear();
            foreach (var item in LoadingRequested)
            {
                if (item.Value)
                {
                    requestedLocation.Add(item.Key);
                }
            }

            return requestedLocation.Count > 0;
        }
        public bool IsUnloadingRequestedBySignal(ref List<string> requestedLocation)
        {
            requestedLocation.Clear();
            foreach (var item in UnloadingRequested)
            {
                if (item.Value)
                {
                    requestedLocation.Add(item.Key);
                }
            }

            return requestedLocation.Count > 0;
        }
        #endregion </Receive>

        public void UpdateInformations()
        {
            foreach (var item in LoadingInputSignalsByLocation)
            {
                LoadingRequested[item.Key] = _digitalIO.ReadInput(item.Value);
            }

            foreach (var item in UnloadingInputSignalsByLocation)
            {
                UnloadingRequested[item.Key] = _digitalIO.ReadInput(item.Value);
            }
        }
        #endregion </Methods>
    }
}

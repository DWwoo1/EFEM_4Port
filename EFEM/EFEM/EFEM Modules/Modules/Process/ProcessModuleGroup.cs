using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EquipmentState_;

using EFEM.Defines.Common;
using EFEM.MaterialTracking;
using EFEM.MaterialTracking.LocationServer;
using EFEM.Defines.ProcessModule;
using EFEM.Modules.ProcessModule;

namespace EFEM.Modules
{
    public class ProcessModuleGroup
    {
        #region <Constructors>
        private ProcessModuleGroup()
        {

        }
        #endregion </Constructors>

        #region <Fields>
        private static ProcessModuleGroup _instance = null;

        private readonly ConcurrentDictionary<int, BaseProcessModule> ProcessModules
            = new ConcurrentDictionary<int, BaseProcessModule>();
        #endregion </Fields>

        #region <Properties>
        public static ProcessModuleGroup Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ProcessModuleGroup();

                return _instance;
            }
        }
        public int Count
        {
            get
            {
                if (ProcessModules == null)
                    return 0;

                return ProcessModules.Count;
            }
        }
        #endregion </Properties>

        #region <Methods>

        #region <Process Module>
        public void AssignProcessModule(int moduleIndex, BaseProcessModule processModule)
        {
            ProcessModules[moduleIndex] = processModule;
        }
        public void ExitProcessModuleAll()
        {
            foreach (var item in ProcessModules)
            {
                item.Value.ExitProcessModule();
            }
        }
        public string GetProcessModuleName(int moduleIndex)
        {
            if (false == ProcessModules.ContainsKey(moduleIndex))
                return string.Empty;

            return ProcessModules[moduleIndex].Name;
        }
        public string[] GetProcessModuleLocations(int moduleIndex)
        {
            if (false == ProcessModules.ContainsKey(moduleIndex))
                return null;

            return ProcessModules[moduleIndex].LocationNames;
        }
        public int GetProcessModuleIndexByLocation(string location)
        {
            int pmIndex = -1;

            foreach (var item in ProcessModules)
            {
                int length = item.Value.LocationNames.Length;
                for (int i = 0; i < length; ++i)
                {
                    if (item.Value.LocationNames[i].Equals(location))
                    {
                        return item.Key;
                    }
                }
            }

            return pmIndex;
        }
        #endregion </Process Module>

        #region <Communication>

        #region <SMEMA>

        #region <Send>
        public void ResetSignalsAll()
        {
            foreach (var item in ProcessModules)
            {
                item.Value.ResetSignalsAll();
            }
        }
        public bool SetLoadingSignal(int moduleIndex, string location, bool enabled)
        {
            if (false == ProcessModules.ContainsKey(moduleIndex))
                return false;

            ProcessModules[moduleIndex].SetLoadingSignal(location, enabled);
            return true;
        }

        public bool SetUnloadingSignal(int moduleIndex, string location, bool enabled)
        {
            if (false == ProcessModules.ContainsKey(moduleIndex))
                return false;

            ProcessModules[moduleIndex].SetUnloadingSignal(location, enabled);
            return true;
        }
        #endregion </Send>

        #endregion </SMEMA>

        #region <WCF>

        #region <Connection>
        public bool GetCommunicationInfo(int pmIndex, ref NetworkInformation communicationInfo)
        {
            if (false == ProcessModules.ContainsKey(pmIndex))
                return false;

            communicationInfo = ProcessModules[pmIndex].CommunicationInfo;

            return (communicationInfo != null);
        }
        public bool InitCommunication(int pmIndex)
        {
            if (false == ProcessModules.ContainsKey(pmIndex))
                return false;

            return ProcessModules[pmIndex].InitCommunication();
        }
        #endregion </Connection>

        #region <Send>
        public bool SendMessage(int moduleIndex, Location location, string title, Dictionary<string, string> messagePairs)
        {
            if (false == ProcessModules.ContainsKey(moduleIndex))
                return false;

            return ProcessModules[moduleIndex].SendMessage(location, title, messagePairs);
        }
        public bool SendMessage(int moduleIndex, Location location, string title, string substrateName)
        {
            if (false == ProcessModules.ContainsKey(moduleIndex))
                return false;

            return ProcessModules[moduleIndex].SendMessage(location, title, substrateName);
        }
        public CommunicationResult IsSendingCompleted(int moduleIndex, Location location, string title)
        {
            if (false == ProcessModules.ContainsKey(moduleIndex))
                return CommunicationResult.Error;

            return CommunicationResult.Ack;
        }
        public bool GetSendingResult(int moduleIndex, Location location, string title, ref Dictionary<string, string> receivedData)
        {
            if (false == ProcessModules.ContainsKey(moduleIndex))
                return false;

            return ProcessModules[moduleIndex].GetSendingResult(location, title, ref receivedData);
        }
        #endregion </Send>

        #region <Received>
        public bool SetAckReceivedMessage(int moduleIndex, Location location, string title, CommunicationResult result, string description)
        {
            if (false == ProcessModules.ContainsKey(moduleIndex))
                return false;

            ProcessModules[moduleIndex].SetAckToReceivedMessage(location, title, result, description);
            return true;
        }
        public CommunicationResult IsMessageReceived(int moduleIndex, Location location, string title)
        {
            if (false == ProcessModules.ContainsKey(moduleIndex))
                return CommunicationResult.Error;

            return ProcessModules[moduleIndex].IsMessageReceived(location, title);
        }
        public bool GetReceivedData(int moduleIndex, Location location, string title, ref Dictionary<string, string> receivedData)
        {
            if (false == ProcessModules.ContainsKey(moduleIndex))
                return false;

            return ProcessModules[moduleIndex].GetReceivedData(location, title, ref receivedData);
        }
        #endregion </Received>

        #endregion </WCF>

        #endregion </Communication>

        #region <Substrate>
        public bool AssignSubstrate(string targetLocation, Substrate substrate)
        {
            int moduleIndex = -1;
            foreach (var item in ProcessModules)
            {
                if (item.Value.HasLocation(targetLocation))
                {
                    moduleIndex = item.Key;
                    break;
                }
            }

            if (moduleIndex < 0)
                return false;

            return ProcessModules[moduleIndex].AssignSubstrate(substrate);
        }
        public void RemoveSubstrateAll()
        {
            foreach (var item in ProcessModules)
            {
                item.Value.RemoveSubstrateAll();
            }
        }
        public void RemoveSubstrateAll(int moduleIndex)
        {
            if (false == ProcessModules.ContainsKey(moduleIndex))
                return;
            ProcessModules[moduleIndex].RemoveSubstrateAll();
        }

        public bool RemoveSubstrate(string substrateName)
        {
            int moduleIndex = -1;
            foreach (var item in ProcessModules)
            {
                List<Substrate> substrates = new List<Substrate>();
                if (item.Value.GetSubstrates(ref substrates))
                {
                    if (substrates == null)
                        continue;

                    for (int i = 0; i < substrates.Count; ++i)
                    {
                        if (substrates[i].GetName().Equals(substrateName))
                        {
                            moduleIndex = item.Key;
                            break;
                        }
                    }
                }
            }

            if (moduleIndex < 0)
                return false;

            ProcessModules[moduleIndex].RemoveSubstrate(substrateName);
            
            return true;
        }
        public bool RemoveSubstrate(string targetLocation, string substrateName)
        {
            int moduleIndex = -1;
            foreach (var item in ProcessModules)
            {
                if (item.Value.HasLocation(targetLocation))
                {
                    moduleIndex = item.Key;
                    break;
                }
            }

            if (moduleIndex < 0)
                return false;

            ProcessModules[moduleIndex].RemoveSubstrate(substrateName);
            
            return true;
        }
        //public bool GetSubstrate(string substrateName, ref Substrate substrate)
        //{
        //    foreach (var item in ProcessModules)
        //    {
        //        if (item.Value.GetSubstrate(substrateName, ref substrate))
        //            return true;
        //    }
            
        //    return false;
        //}
        //public bool GetSubstrates(int moduleIndex, ref List<Substrate> substrates)
        //{
        //    if (false == ProcessModules.ContainsKey(moduleIndex))
        //        return false;

        //    return ProcessModules[moduleIndex].GetSubstrates(ref substrates);
        //}
        #endregion </Substrate>

        public bool SetEquipmentState(int moduleIndex, EQUIPMENT_STATE status)
        {
            if (false == ProcessModules.ContainsKey(moduleIndex))
                return false;

            ProcessModules[moduleIndex].EquipmentState = status;
            return true;
        }
        public EQUIPMENT_STATE GetEquipmentState(int moduleIndex)
        {
            if (false == ProcessModules.ContainsKey(moduleIndex))
                return EQUIPMENT_STATE.UNDEFINED;

            return ProcessModules[moduleIndex].EquipmentState;
        }
        public bool SetRecipeId(int moduleIndex, string recipeId)
        {
            if (false == ProcessModules.ContainsKey(moduleIndex))
                return false;

            ProcessModules[moduleIndex].RecipeId = recipeId;
            return true;
        }
        public string GetRecipeId(int moduleIndex)
        {
            if (false == ProcessModules.ContainsKey(moduleIndex))
                return string.Empty;

            return ProcessModules[moduleIndex].RecipeId;
        }
        public bool SetLotId(int moduleIndex, string lotID)
        {
            if (false == ProcessModules.ContainsKey(moduleIndex))
                return false;

            ProcessModules[moduleIndex].LotId = lotID;
            return true;
        }
        public string GetLotId(int moduleIndex)
        {
            if (false == ProcessModules.ContainsKey(moduleIndex))
                return string.Empty;

            return ProcessModules[moduleIndex].LotId;
        }
        public bool IsLoadingRequested(int moduleIndex, string locationName)
        {
            if (false == ProcessModules.ContainsKey(moduleIndex))
                return false;

            List<string> locations = new List<string>();
            if (false == IsLoadingRequested(moduleIndex, ref locations))
                return false;

            return locations.Contains(locationName);
        }
        public bool IsLoadingRequested(int moduleIndex, ref List<string> locationNames)
        {
            locationNames.Clear();
            if (false == ProcessModules.ContainsKey(moduleIndex))
                return false;

            return ProcessModules[moduleIndex].IsLoadingRequested(ref locationNames);
        }
        public bool IsUnloadingRequested(int moduleIndex, string locationName)
        {
            if (false == ProcessModules.ContainsKey(moduleIndex))
                return false;

            List<string> locations = new List<string>();
            if (false == IsUnloadingRequested(moduleIndex, ref locations))
                return false;

            return locations.Contains(locationName);
        }
        public bool IsUnloadingRequested(int moduleIndex, ref List<string> locationNames)
        {
            locationNames.Clear();
            if (false == ProcessModules.ContainsKey(moduleIndex))
                return false;

            return ProcessModules[moduleIndex].IsUnloadingRequested(ref locationNames);
        }
        public void ExecuteAll()
        {
            foreach (var item in ProcessModules)
            {
                if (item.Value == null)
                    continue;
                item.Value.Execute();
            }
        }
        #endregion </Methods>
    }
}

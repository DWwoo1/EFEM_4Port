using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Serialization;

using FileIOManager_;
using FileComposite_;

using EFEM.Defines.LoadPort;
using EFEM.Defines.MaterialTracking;
using EFEM.Defines.AtmRobot;
using EFEM.Modules;
using EFEM.Defines.Common;
using EFEM.MaterialTracking.Attributes;
using EFEM.MaterialTracking.LocationServer;
//using SerializableDictionary;

namespace EFEM.MaterialTracking
{
    public class SubstrateManager
    {
        #region <Constructors>
        private SubstrateManager()
        {
            //Substrates = new ConcurrentDictionary<string, Substrate>();

            _locationServer = LocationServer.LocationServer.Instance;
            _loadPortManager = LoadPortManager.Instance;
            _robotManager = AtmRobotManager.Instance;
            _carrierServer = CarrierManagementServer.Instance;
            _processModuleGroup = ProcessModuleGroup.Instance;

            SubstratesInLoadPortSlots = new ConcurrentDictionary<int, ConcurrentDictionary<int, Substrate>>();
            SubstratesInProcessModule = new ConcurrentDictionary<string, ConcurrentDictionary<int, Substrate>>();
            SubstratesInRobot = new ConcurrentDictionary<string, ConcurrentDictionary<RobotArmTypes, Substrate>>();
        }
        #endregion </Constructors>

        #region <Fields>
        private static SubstrateManager _instance = null;
        private static LoadPortManager _loadPortManager = null;
        private static AtmRobotManager _robotManager = null;
        private static CarrierManagementServer _carrierServer = null;
        private static ProcessModuleGroup _processModuleGroup = null;
        private static LocationServer.LocationServer _locationServer = null;

        //private readonly ConcurrentDictionary<string, Substrate> Substrates = null;

        private readonly ConcurrentDictionary<int, ConcurrentDictionary<int, Substrate>> SubstratesInLoadPortSlots = null;
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<int, Substrate>> SubstratesInProcessModule = null;
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<RobotArmTypes, Substrate>> SubstratesInRobot = null;
        #endregion </Fields>

        #region <Properties>
        public static SubstrateManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SubstrateManager();
                }

                return _instance;
            }
        }
        #endregion </Properties>

        #region <Methods>

        #region <File Control>
        public bool LoadRecoveryDataAll()
        {
            try
            {
                if (false == Directory.Exists(RecoveryFileDefines.RecoveryFilePath))
                {
                    Directory.CreateDirectory(RecoveryFileDefines.RecoveryFilePath);
                    return false;
                }

                string[] files = Directory.GetFiles(RecoveryFileDefines.RecoveryFilePath);
                if (files.Length <= 0)
                    return false;

                for (int i = 0; i < files.Length; ++i)
                {
                    Substrate substrate = new Substrate("");
                    string fileName = Path.GetFileName(files[i]);
                    if (substrate.LoadRecoveryData(fileName))
                    {
                        CreateSubstrate(substrate.GetName(), substrate.GetLocation());

                        Substrate substrateInManager = new Substrate("");
                        if (GetSubstrate(substrate.GetLocation(), substrate.GetName(), ref substrateInManager))
                        {
                            substrateInManager.SetAttributesAll(substrate.GetAttributesAll());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Instance.WriteDebugLog(string.Format("LoadRecoveryDataAll Exception > {0}, {1}",
                    ex.Message, ex.StackTrace));

                return false;
            }
            return true;
        }

        public bool SaveRecoveryDataAll()
        {
            try
            {
                if (false == Directory.Exists(RecoveryFileDefines.RecoveryFilePath))
                {
                    Directory.CreateDirectory(RecoveryFileDefines.RecoveryFilePath);
                    return false;
                }

                foreach (var lp in SubstratesInLoadPortSlots)
                {
                    foreach (var item in lp.Value)
                    {
                        if (false == item.Value.SaveRecoveryData())
                        {
                            DebugLogger.Instance.WriteDebugLog(string.Format("SaveRecoveryData failed > {0}", item.Key));
                        }
                    }
                }

                foreach (var pm in SubstratesInProcessModule)
                {
                    foreach (var item in pm.Value)
                    {
                        if (false == item.Value.SaveRecoveryData())
                        {
                            DebugLogger.Instance.WriteDebugLog(string.Format("SaveRecoveryData failed > {0}", item.Key));
                        }
                    }
                    //for (int i = 0; i < pm.Value.Count; ++i)
                    //{
                    //    if (false == pm.Value[i].SaveRecoveryData())
                    //    {
                    //        DebugLogger.Instance.WriteDebugLog(string.Format("SaveRecoveryData failed > {0}", pm.Key));
                    //    }
                    //}
                }

                foreach (var robot in SubstratesInRobot)
                {
                    foreach (var item in robot.Value)
                    {
                        if (false == item.Value.SaveRecoveryData())
                        {
                            DebugLogger.Instance.WriteDebugLog(string.Format("SaveRecoveryData failed > {0}", item.Key));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Instance.WriteDebugLog(string.Format("SaveRecoveryDataAll Exception > {0}, {1}",
                    ex.Message, ex.StackTrace));

                return false;
            }

            return true;
        }

        public bool IsValidSubstrateName(string filename)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            return !filename.Any(c => invalidChars.Contains(c));
        }
        #endregion <File Control>

        #region <Create, Remove>
        public void AddLoadPortBuffers(int portId, int capacity)
        {
            SubstratesInLoadPortSlots[portId] = new ConcurrentDictionary<int, Substrate>();
            //Dictionary<int, Substrate> slots = new Dictionary<int, Substrate>();
            //for(int i = 0; i < capacity; ++i)
            //{
            //    slots[i] = null;
            //}
            //SubstratesInLoadPortSlots[portId] = slots;
        }

        public void AddRobotBuffers(string robotName)
        {
            SubstratesInRobot[robotName] = new ConcurrentDictionary<RobotArmTypes, Substrate>();
            //Dictionary<RobotArmTypes, Substrate> robotArms = new Dictionary<RobotArmTypes, Substrate>
            //{
            //    [RobotArmTypes.UpperArm] = null,
            //    [RobotArmTypes.LowerArm] = null
            //};

            //SubstratesInRobot[robotName] = robotArms;
        }

        public void AddProcessModuleBuffers(string processModuleName)
        {
            SubstratesInProcessModule[processModuleName] = new ConcurrentDictionary<int, Substrate>();
            //Dictionary<string, Substrate> processModuleBuffers = new Dictionary<string, Substrate>();
            //for(int i = 0; i < locations.Length; ++i)
            //{
            //    string location = locations[i];
            //    processModuleBuffers[location] = null;
            //}
            //SubstratesInProcessModule[processModuleName] = processModuleBuffers;
        }

        public void CreateSubstrate(string name, Location location)
        {
            var substrate = new Substrate(name);

            if (location is LoadPortLocation)
            {
                var lpLocation = location as LoadPortLocation;

                substrate.SetSourcePortId(lpLocation.PortId);
                substrate.SetSourceSlot(lpLocation.Slot);
                substrate.SetDestinationPortId(lpLocation.PortId);
                substrate.SetDestinationSlot(lpLocation.Slot);
                substrate.SetLocation(location);
                substrate.InitAttributes();

                SubstratesInLoadPortSlots.TryGetValue(lpLocation.PortId, out ConcurrentDictionary<int, Substrate> substrates);
                substrates[lpLocation.Slot] = substrate;
                SubstratesInLoadPortSlots[lpLocation.PortId] = substrates;
            }
            else if (location is ProcessModuleLocation)
            {
                var pmLocation = location as ProcessModuleLocation;

                if (false == SubstratesInProcessModule.TryGetValue(pmLocation.ProcessModuleName, out ConcurrentDictionary<int, Substrate> substrates))
                    substrates = new ConcurrentDictionary<int, Substrate>();

                if (substrates == null)
                    substrates = new ConcurrentDictionary<int, Substrate>();

                substrate.SetLocation(location);
                substrate.InitAttributes();

                int index = 0;
                if (substrates.Any())
                {
                    List<int> sortedKeys = substrates.Keys.OrderBy(k => k).ToList();
                    index = sortedKeys.Last() + 1;

                }
                substrates[index] = substrate;

                //string substrateName = substrate.GetName();
                //for (int i = 0; i < substrates.Count; ++i)
                //{
                //    if (substrates[i].GetName().Equals(substrateName))
                //        return;
                //}
                //substrates.Add(substrate);
                SubstratesInProcessModule[pmLocation.ProcessModuleName] = substrates;
            }
            else if (location is RobotLocation)
            {
                var robotLocation = location as RobotLocation;

                SubstratesInRobot.TryGetValue(robotLocation.RobotName, out ConcurrentDictionary<RobotArmTypes, Substrate> substrates);
                substrate.SetLocation(location);
                substrate.InitAttributes();

                substrates[robotLocation.Arm] = substrate;
                SubstratesInRobot[robotLocation.RobotName] = substrates;
            }

            SaveRecoveryDataAll();
        }
        public void RemoveSubstrate(string targetName, Location location)
        {
            if (location is LoadPortLocation)
            {
                var lpLocation = location as LoadPortLocation;

                if (SubstratesInLoadPortSlots[lpLocation.PortId].ContainsKey(lpLocation.Slot))
                {
                    SubstratesInLoadPortSlots[lpLocation.PortId][lpLocation.Slot].DeleteRecoveryData();
                    SubstratesInLoadPortSlots[lpLocation.PortId].TryRemove(lpLocation.Slot, out _);
                }
            }
            else if (location is ProcessModuleLocation)
            {
                var pmLocation = location as ProcessModuleLocation;

                int index = -1;
                if (SubstratesInProcessModule.ContainsKey(pmLocation.ProcessModuleName))
                {
                    foreach (var item in SubstratesInProcessModule[pmLocation.ProcessModuleName])
                    {
                        if (item.Value.GetName().Equals(targetName))
                        {
                            index = item.Key;
                            break;
                        }
                    }

                    if (index >= 0)
                    {
                        SubstratesInProcessModule[pmLocation.ProcessModuleName][index].DeleteRecoveryData();
                        SubstratesInProcessModule[pmLocation.ProcessModuleName].TryRemove(index, out _);                        
                    }
                }
                //for (int i = 0; i < SubstratesInProcessModule[pmLocation.ProcessModuleName].Count; ++i)
                //{
                //    string name = SubstratesInProcessModule[pmLocation.ProcessModuleName][i].GetName();
                //    if (name.Equals(targetName))
                //    {
                //        index = i;
                //        SubstratesInProcessModule[pmLocation.ProcessModuleName][i].DeleteRecoveryData();
                //    }
                //}

                //if (index >= 0)
                //{
                //    SubstratesInProcessModule[pmLocation.ProcessModuleName].RemoveAt(index);
                //}

            }
            else if (location is RobotLocation)
            {
                var robotLocation = location as RobotLocation;

                if (SubstratesInRobot[robotLocation.RobotName].ContainsKey(robotLocation.Arm))
                {
                    SubstratesInRobot[robotLocation.RobotName][robotLocation.Arm].DeleteRecoveryData();
                    SubstratesInRobot[robotLocation.RobotName].TryRemove(robotLocation.Arm, out _);
                }
            }

            SaveRecoveryDataAll();
        }
        #endregion </Create, Remove>

        #region <Assign>
        public void AssignSubstrateInLoadPort(int portId, int slot, Substrate substrate)
        {
            SubstratesInLoadPortSlots[portId][slot] = substrate;
        }
        public void BackupAndRemoveSubstrateInLoadPortAll(int portId, string destinationPath)
        {
            if (false == SubstratesInLoadPortSlots.TryGetValue(portId, out ConcurrentDictionary<int, Substrate> value))
                return;

            if (false == Directory.Exists(destinationPath))
                Directory.CreateDirectory(destinationPath);

            foreach (var item in value)
            {
                var fullPath = Path.Combine(destinationPath, string.Format("{0}.{1}", item.Value.GetName(), RecoveryFileDefines.FileExtension));
                if (File.Exists(fullPath))
                    File.Delete(fullPath);

                File.Copy(item.Value.GetSubstrateFilePath(), fullPath);

                item.Value.DeleteRecoveryData();
            }

            SubstratesInLoadPortSlots[portId].Clear();
        }
        public void RemoveSubstrateInLoadPortAll(int portId)
        {
            if (false == SubstratesInLoadPortSlots.TryGetValue(portId, out ConcurrentDictionary<int, Substrate> value))
                return;

            foreach (var item in value)
            {
                item.Value.DeleteRecoveryData();
            }

            SubstratesInLoadPortSlots[portId].Clear();
        }
        public void RemoveSubstrateInLoadPort(int portId, int slot)
        {
            if (SubstratesInLoadPortSlots[portId].ContainsKey(slot))
            {
                SubstratesInLoadPortSlots[portId][slot].DeleteRecoveryData();
                SubstratesInLoadPortSlots[portId].TryRemove(slot, out _);
            }
        }

        public void AssignSubstrateInProcessModule(string moduleName, Substrate substrate)
        {
            if (false == SubstratesInProcessModule.TryGetValue(moduleName, out ConcurrentDictionary<int, Substrate> substrates))
            {
                substrates = new ConcurrentDictionary<int, Substrate>();
            }
            
            if (substrates == null)
                substrates = new ConcurrentDictionary<int, Substrate>();

            int index = 0;
            if (substrates.Any())
            {
                List<int> sortedKeys = substrates.Keys.OrderBy(k => k).ToList();
                index = sortedKeys.Last() + 1;
            }
            
            substrates[index] = substrate;
            SubstratesInProcessModule[moduleName] = substrates;


            //if (SubstratesInProcessModule.ContainsKey(moduleName))
            //{
            //    SubstratesInProcessModule[moduleName].Add(substrate);
            //}
        }

        public void AssignSubstrateInRobot(string robotName, RobotArmTypes armType, Substrate substrate)
        {
            if (SubstratesInRobot.ContainsKey(robotName))
            {
                SubstratesInRobot[robotName][armType] = substrate;
            }
        }
        #endregion </Assign>

        #region <Attributes>
        public void SetSubstrateLotInfoByLocation(Location location, string targetName, string lotId, string recipeId, string carrierId)
        {
            Substrate substrate = new Substrate(string.Empty);
            if (location is LoadPortLocation)
            {
                var lpLocation = location as LoadPortLocation;

                if (false == SubstratesInLoadPortSlots[lpLocation.PortId].TryGetValue(lpLocation.Slot, out substrate))
                    return;
            }
            else if (location is ProcessModuleLocation)
            {
                var pmLocation = location as ProcessModuleLocation;

                if (false == SubstratesInProcessModule.ContainsKey(pmLocation.ProcessModuleName))
                    return;
                
                int index = -1;
                foreach (var item in SubstratesInProcessModule[pmLocation.ProcessModuleName])
                {
                    if (item.Value.GetName().Equals(targetName))
                    {
                        index = item.Key;
                        substrate = item.Value;
                        break;
                    }
                }               

                if (index < 0)
                    return;

                //for (int i = 0; i < SubstratesInProcessModule[pmLocation.ProcessModuleName].Count; ++i)
                //{
                //    string name = SubstratesInProcessModule[pmLocation.ProcessModuleName][i].GetName();
                //    if (name.Equals(targetName))
                //    {
                //        index = i;
                //        break;
                //    }
                //}
                //if (index < 0)
                //    return;

                //substrate = SubstratesInProcessModule[pmLocation.ProcessModuleName][index];
            }
            else if (location is RobotLocation)
            {
                var robotLocation = location as RobotLocation;
                if (false == SubstratesInRobot[robotLocation.RobotName].TryGetValue(robotLocation.Arm, out substrate))
                    return;
            }
            else
                return;

            if (string.IsNullOrEmpty(substrate.GetLotId()))
            {
                substrate.SetLotId(lotId);
            }

            if (string.IsNullOrEmpty(substrate.GetRecipeId()))
            {
                substrate.SetRecipeId(recipeId);
            }

            if (string.IsNullOrEmpty(substrate.GetSourceCarrierId()))
            {
                substrate.SetSourceCarrierId(carrierId);
            }
        }
        public bool GetTransferStatus(Location location, string targetName, ref SubstrateTransferStates transferStatus)
        {
            Substrate substrate = new Substrate("");
            if (false == GetSubstrateByLocation(location, targetName, ref substrate))
                return false;

            transferStatus = substrate.GetTransferStatus();
            return true;
        }
        public bool GetProcessingStatus(Location location, string targetName, ref ProcessingStates processingStatus)
        {
            Substrate substrate = new Substrate("");
            if (false == GetSubstrateByLocation(location, targetName, ref substrate))
                return false;

            processingStatus = substrate.GetProcessingStatus();
            return true;
        }
        public bool IsProcessingCompleted(SubstrateTransferStates transferStatus, ProcessingStates processingStatus)
        {
            switch (processingStatus)
            {
                case ProcessingStates.Rejected:
                case ProcessingStates.Stopped:
                case ProcessingStates.Aborted:
                case ProcessingStates.Skipped:
                case ProcessingStates.Lost:
                    break;
                default:
                    return transferStatus.Equals(SubstrateTransferStates.AtDestination);
            }

            return true;
        }
        #endregion </Attributes>

        #region <Location>
        public bool HasLocation(Location targetLocation)
        {
            if (targetLocation is LoadPortLocation)
            {
                var location = targetLocation as LoadPortLocation;
                return _locationServer.GetLoadPortSlotLocation(location.PortId, location.Slot, ref location);
            }
            else if (targetLocation is ProcessModuleLocation)
            {
                var location = targetLocation as ProcessModuleLocation;
                return _locationServer.GetProcessModuleLocation(location.ProcessModuleName, location.Name, ref location);
            }
            else if (targetLocation is RobotLocation)
            {
                var location = targetLocation as RobotLocation;
                return _locationServer.GetRobotLocation(location.RobotName, location.Arm, ref location);
            }

            return false;
        }
        public bool FindLostSubstrate(string substrateName, ref Substrate substrate)
        {
            bool result = false;

            List<Substrate> substrates = new List<Substrate>();

            // 1. LoadPort
            foreach (var lpSlots in SubstratesInLoadPortSlots)
            {
                foreach (var item in lpSlots.Value)
                {
                    substrates.Add(item.Value);
                }
            }

            // 2. Process Module
            foreach (var pm in SubstratesInProcessModule)
            {
                foreach (var item in pm.Value)
                {
                    substrates.Add(item.Value);
                }
                //for (int i = 0; i < pm.Value.Count; ++i)
                //{
                //    substrates.Add(pm.Value[i]);
                //}
            }


            //private readonly ConcurrentDictionary<int, Dictionary<int, Substrate>> SubstratesInLoadPortSlots = null;
            //private readonly ConcurrentDictionary<string, List<Substrate>> SubstratesInProcessModule = null;

            return result;
        }
        public bool FindSubstrateByLocation(Location targetLocation, string substrateName, ref Substrate substrate)
        {
            if (targetLocation is LoadPortLocation)
            {
                var location = targetLocation as LoadPortLocation;

                return SubstratesInLoadPortSlots[location.PortId].TryGetValue(location.Slot, out substrate);
            }
            else if (targetLocation is ProcessModuleLocation)
            {
                var location = targetLocation as ProcessModuleLocation;

                //if (false == SubstratesInProcessModule.TryGetValue(location.ProcessModuleName, out ConcurrentDictionary<int, Substrate> substrates))
                //    return false;

                foreach (var pm in SubstratesInProcessModule)
                {
                    foreach (var item in pm.Value)
                    {
                        string name = item.Value.GetName();
                        if (name.Equals(substrateName))
                        {
                            substrate = item.Value;
                            return true;
                        }

                    }
                }
                //foreach (var item in substrates)
                //{
                //    string name = item.Value.GetName();
                //    if (name.Equals(substrateName))
                //    {
                //        substrate = item.Value;
                //        return true;
                //    }

                //}
                //for (int i = 0; i < SubstratesInProcessModule[location.ProcessModuleName].Count; ++i)
                //{
                //}

                return false;
            }
            else if (targetLocation is RobotLocation)
            {
                var location = targetLocation as RobotLocation;

                return SubstratesInRobot[location.RobotName].TryGetValue(location.Arm, out substrate);
            }
            else
                return false;
        }
        public bool MoveSubstrateModuleToModule(string substrateName, Location sourceLocation, Location destinationLocation)
        {
            if (false == HasLocation(sourceLocation) ||
                false == HasLocation(destinationLocation))
                return false;

            Substrate substrate = new Substrate("");

            // Source
            if (sourceLocation is LoadPortLocation)
            {
                var location = sourceLocation as LoadPortLocation;
                if (false == GetSubstrateAtLoadPort(location, ref substrate))
                    return false;
            }
            else if (sourceLocation is ProcessModuleLocation)
            {
                var location = sourceLocation as ProcessModuleLocation;
                if (false == GetSubstrateAtProcessModule(substrateName, location, ref substrate))
                    return false;
            }
            else if (sourceLocation is RobotLocation)
            {
                var location = sourceLocation as RobotLocation;
                if (false == GetSubstrateAtRobot(location, ref substrate))
                    return false;
            }

            substrate.SetLocation(destinationLocation);

            // Target
            if (destinationLocation is LoadPortLocation)
            {
                var location = destinationLocation as LoadPortLocation;
                AssignSubstrateInLoadPort(location.PortId, location.Slot, substrate);
            }
            else if (destinationLocation is ProcessModuleLocation)
            {
                var location = destinationLocation as ProcessModuleLocation;
                AssignSubstrateInProcessModule(location.ProcessModuleName, substrate);
                _processModuleGroup.AssignSubstrate(location.Name, substrate);
            }
            else if (destinationLocation is RobotLocation)
            {
                var location = destinationLocation as RobotLocation;
                AssignSubstrateInRobot(location.RobotName, location.Arm, substrate);
            }

            RemoveSubstrate(substrate.GetName(), sourceLocation);
            return true;
        }
        public void MoveMaterialToRobot(Location targetLocation, string robotName, RobotArmTypes arm, Substrate substrate)
        {
            if (substrate == null)
                return;

            if (targetLocation is LoadPortLocation)
            {
                var location = targetLocation as LoadPortLocation;
                int portId = location.PortId;
                if (_carrierServer.GetCarrierAccessingStatus(portId).Equals(CarrierAccessStates.NotAccessed))
                {
                    _carrierServer.SetCarrierAccessingStatus(portId, CarrierAccessStates.InAccessed);
                }

                substrate.SetTransferStatus(SubstrateTransferStates.AtWork);
            }
            else if (targetLocation is ProcessModuleLocation)
            {
                //return _processModuleGroup.GetSubstrate(substrateName, ref substrate);
                _processModuleGroup.RemoveSubstrate(targetLocation.Name, substrate.GetName());
            }

            RobotLocation newLocation = new RobotLocation(arm, robotName);
            if (_locationServer.GetRobotLocation(robotName, arm, ref newLocation))
            {
                MoveSubstrateModuleToModule(substrate.GetName(), targetLocation, newLocation);
                //var loc = newLocation as Location;
                //substrate.SetLocation(loc);
            }
        }
        public void MoveMaterialToModule(Location destinationLocation, Substrate substrate)
        {
            if (destinationLocation is LoadPortLocation)
            {
                if (FrameOfSystem3.Recipe.Recipe.GetInstance().GetValue(FrameOfSystem3.Recipe.EN_RECIPE_TYPE.COMMON, FrameOfSystem3.Recipe.PARAM_COMMON.UseCycleMode.ToString(), false))
                {
                    substrate.SetTransferStatus(SubstrateTransferStates.AtSource);
                    substrate.SetProcessingStatus(ProcessingStates.NeedsProcessing);
                }
                else
                {
                    substrate.SetTransferStatus(SubstrateTransferStates.AtDestination);
                }

                // Destination
                var targetLocation = destinationLocation as LoadPortLocation;
                int destPort = targetLocation.PortId;
                int destSlot = targetLocation.Slot;

                int sourcePort = substrate.GetSourcePortId();
                int sourceSlot = substrate.GetSourceSlot();
                var sourceLocation = new LoadPortLocation(sourcePort, sourceSlot, "");
                _locationServer.GetLoadPortSlotLocation(sourcePort, sourceSlot, ref sourceLocation);

                if (_carrierServer.GetCarrierAccessingStatus(destPort).Equals(CarrierAccessStates.NotAccessed))
                {
                    _carrierServer.SetCarrierAccessingStatus(destPort, CarrierAccessStates.InAccessed);
                }
            }

            MoveSubstrateModuleToModule(substrate.GetName(), substrate.GetLocation(), destinationLocation);

            //else if (location is ProcessModuleLocation)
            //{
            //    var pmLocation = location as ProcessModuleLocation;
            //    _processModuleGroup.AssignSubstrate(pmLocation.Name, substrate);
            //    AssignSubstrateInProcessModule(pmLocation.ProcessModuleName, substrate);
            //}
            //substrate.SetLocation(location);
        }
        #endregion </Location>

        #region <Get Substrate>

        #region <LoadPort>
        public Dictionary<int, Substrate> GetSubstratesAtLoadPort(int portId)
        {
            var substrates = new Dictionary<int, Substrate>(SubstratesInLoadPortSlots[portId]);
            return substrates;
        }
        public bool IsSourceSlotReserved(int portId, int slot)
        {
            foreach (var lp in SubstratesInLoadPortSlots)
            {
                foreach (var item in lp.Value)
                {
                    if (item.Value.GetSourcePortId().Equals(portId) && item.Value.GetSourceSlot().Equals(slot))
                    {
                        return true;
                    }
                }
            }

            foreach (var pm in SubstratesInProcessModule)
            {
                foreach (var item in pm.Value)
                {
                    if (item.Value.GetSourcePortId().Equals(portId) && item.Value.GetSourceSlot().Equals(slot))
                    {
                        return true;
                    }
                }

                //{
                //    for (int i = 0; i < item.Value.Count; ++i)
                //    {
                //        if (item.Value[i].GetSourcePortId().Equals(portId) && item.Value[i].GetSourceSlot().Equals(slot))
                //            return true;
                //    }

                //}
            }

            foreach (var ams in SubstratesInRobot)
            {
                foreach (var item in ams.Value)
                {
                    if (item.Value.GetSourcePortId().Equals(portId))
                    {
                        if (item.Value.GetSourcePortId().Equals(portId) && item.Value.GetSourceSlot().Equals(slot))
                            return true;
                    }
                }
            }

            return false;
        }

        public bool IsSubstrateAtDestination(int portId)
        {
            foreach (var lp in SubstratesInLoadPortSlots)
            {
                foreach (var item in lp.Value)
                {
                    if (item.Value.GetSourcePortId().Equals(portId))
                    {
                        if (false == IsProcessingCompleted(item.Value.GetTransferStatus(), item.Value.GetProcessingStatus()))
                            return false;
                    }
                }
            }

            foreach (var pm in SubstratesInProcessModule)
            {
                foreach (var item in pm.Value)
                {
                    if (false == item.Value.GetSourcePortId().Equals(portId))
                        continue;
                    if (false == IsProcessingCompleted(item.Value.GetTransferStatus(), item.Value.GetProcessingStatus()))
                        return false;
                }
                //{
                //    for (int i = 0; i < item.Value.Count; ++i)
                //    {
                //        if (false == item.Value[i].GetSourcePortId().Equals(portId))
                //            continue;
                //        if (false == IsProcessingCompleted(item.Value[i].GetTransferStatus(), item.Value[i].GetProcessingStatus()))
                //            return false;
                //    }

                //}
            }

            foreach (var arms in SubstratesInRobot)
            {
                foreach (var item in arms.Value)
                {
                    if (item.Value.GetSourcePortId().Equals(portId))
                    {
                        if (false == IsProcessingCompleted(item.Value.GetTransferStatus(), item.Value.GetProcessingStatus()))
                            return false;
                    }
                }
            }

            return true;
        }
        public bool HasAnySubstrateAtLoadPort(int portId)
        {
            return SubstratesInLoadPortSlots[portId].Count > 0;
        }
        public bool HasSubstrateAtLoadPort(int portId, int slot)
        {
            return SubstratesInLoadPortSlots[portId].TryGetValue(slot, out _);
        }
        public string GetSubstrateNameAtLoadPort(int portId, int slot)
        {
            if (portId <= 0 || slot < 0)
                return string.Empty;

            if (false == SubstratesInLoadPortSlots[portId].TryGetValue(slot, out Substrate substrate))
                return string.Empty;

            return substrate.GetName();
        }
        public string GetSubstrateNameByDestinationPortId(int portId, int slot)
        {
            if (portId <= 0 || slot < 0)
                return string.Empty;

            Substrate substrate = new Substrate("");
            if (false == GetSubstrateByDestinationInfo(portId, slot, ref substrate))
                return string.Empty;

            return substrate.GetName();
        }
        public bool GetSubstrateAtLoadPort(LoadPortLocation location, ref Substrate substrate)
        {
            return SubstratesInLoadPortSlots[location.PortId].TryGetValue(location.Slot, out substrate);
        }
        public bool AreAllSubstratesNeedProcessing(int portId)
        {
            if (false == SubstratesInLoadPortSlots.ContainsKey(portId))
                return true;

            foreach (var item in SubstratesInLoadPortSlots[portId])
            {
                if (false == item.Value.GetProcessingStatus().Equals(ProcessingStates.NeedsProcessing))
                    return false;
            }

            return true;
        }
        public bool IsFirstSubstrateAtLoadPort(string sourceCarrierId, int portId, string substrateName)
        {
            foreach (var lp in SubstratesInLoadPortSlots)
            {
                if (lp.Key.Equals(portId))
                {
                    foreach (var item in lp.Value)
                    {
                        // 이름이 다르고, 포트번호가 같으면 같은데서 있다가 나간 자재다.
                        if (item.Value.GetTransferStatus().Equals(SubstrateTransferStates.AtDestination))
                            return false;
                    }
                }
                else
                {
                    foreach (var item in lp.Value)
                    {
                        // 이름이 다르고, 포트번호가 같으면 같은데서 있다가 나간 자재다.
                        if (item.Value.GetSourcePortId().Equals(portId) &&
                            item.Value.GetSourceCarrierId().Equals(sourceCarrierId) &&
                            false == item.Value.GetName().Equals(substrateName))
                        {
                            return false;
                        }
                    }
                }
            }

            foreach (var pm in SubstratesInProcessModule)
            {
                foreach (var item in pm.Value)
                {
                    if (item.Value.GetSourcePortId().Equals(portId) &&
                        item.Value.GetSourceCarrierId().Equals(sourceCarrierId) &&
                        false == item.Value.GetName().Equals(substrateName))
                    {
                        return false;
                    }
                }
                //{
                //    for (int i = 0; i < item.Value.Count; ++i)
                //    {
                //        // 이름이 다르고, 포트번호가 같으면 같은데서 있다가 나간 자재다.
                //        if (item.Value[i].GetSourcePortId().Equals(portId) &&
                //            item.Value[i].GetSourceCarrierId().Equals(sourceCarrierId) &&
                //            false == item.Value[i].GetName().Equals(substrateName))
                //        {
                //            return false;
                //        }
                //    }
                //}
            }

            foreach (var robot in SubstratesInRobot)
            {
                foreach (var item in robot.Value)
                {
                    // 이름이 다르고, 포트번호가 같으면 같은데서 있다가 나간 자재다.
                    if (item.Value.GetSourcePortId().Equals(portId) &&
                        item.Value.GetSourceCarrierId().Equals(sourceCarrierId) &&
                        false == item.Value.GetName().Equals(substrateName))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
        public bool IsLastSubstrateAtLoadPort(int portId, string substrateName)
        {
            foreach (var lp in SubstratesInLoadPortSlots)
            {
                if (false == lp.Key.Equals(portId))
                    continue;

                foreach (var item in lp.Value)
                {
                    if (item.Value.GetName().Equals(substrateName))
                        continue;

                    // 다른놈이 있는거다.
                    if (false == item.Value.GetTransferStatus().Equals(SubstrateTransferStates.AtDestination))
                        return false;
                }

                // 2024.12.31. jhlim [ADD] 한 장인 경우 체크가 안 된다.
                if (lp.Value.Count == 1)
                {
                    return true;
                }
            }

            // 2024.10.16. jhlim [DEL] 아래는 오직 1개 섭만 있는 경우를 찾는거다.
            //foreach (var lp in SubstratesInLoadPortSlots)
            //{
            //    if (false == lp.Key.Equals(portId))
            //        continue;

            //    foreach (var item in lp.Value)
            //    {
            //        if (item.Value.GetName().Equals(substrateName))
            //            continue;

            //        // 다른놈이 있는거다.
            //        if (false == item.Value.GetTransferStatus().Equals(SubstrateTransferStates.AtDestination) ||
            //            item.Value.GetProcessingStatus().Equals(ProcessingStates.Processed))
            //            return false;
            //    }
            //}

            //foreach (var item in SubstratesInProcessModule)
            //{
            //    {
            //        for (int i = 0; i < item.Value.Count; ++i)
            //        {
            //            // 이름이 다르고, 포트번호가 같으면 같은데서 있다가 나간 자재다.
            //            if (item.Value[i].GetSourcePortId().Equals(portId) &&
            //                false == item.Value[i].GetName().Equals(substrateName))
            //            {
            //                return false;
            //            }
            //        }
            //    }
            //}

            //foreach (var robot in SubstratesInRobot)
            //{
            //    foreach (var item in robot.Value)
            //    {
            //        // 이름이 다르고, 포트번호가 같으면 같은데서 있다가 나간 자재다.
            //        if (item.Value.GetSourcePortId().Equals(portId) &&
            //            false == item.Value.GetName().Equals(substrateName))
            //        {
            //            return false;
            //        }
            //    }
            //}

            return true;
        }
        public bool GetSubstrateBySourceCarrierInfo(int portId, int slot, string carrierId, ref Substrate substrate)
        {
            foreach (var pm in SubstratesInProcessModule)
            {
                foreach (var item in pm.Value)
                {
                    if (item.Value.GetSourcePortId().Equals(portId) &&
                        item.Value.GetSourceSlot().Equals(slot) &&
                        item.Value.GetSourceCarrierId().Equals(carrierId))
                    {
                        substrate = item.Value;
                        return true;
                    }

                }
                //for (int i = 0; i < item.Value.Count; ++i)
                //{
                //    if (item.Value[i].GetSourcePortId().Equals(portId) &&
                //        item.Value[i].GetSourceSlot().Equals(slot) &&
                //        item.Value[i].GetSourceCarrierId().Equals(carrierId))
                //    {
                //        substrate = item.Value[i];
                //        return true;
                //    }
                //}
            }

            foreach (var pm in SubstratesInRobot)
            {
                foreach (var item in pm.Value)
                {
                    if (item.Value.GetSourcePortId().Equals(portId) &&
                       item.Value.GetSourceSlot().Equals(slot) &&
                       item.Value.GetSourceCarrierId().Equals(carrierId))
                    {
                        substrate = item.Value;
                        return true;
                    }
                }
            }

            return false;
        }
        public bool GetSubstrateByDestinationInfo(int portId, int slot, ref Substrate substrate)
        {
            foreach (var pm in SubstratesInProcessModule)
            {
                foreach (var item in pm.Value)
                {
                    if (item.Value.GetDestinationPortId().Equals(portId) &&
                        item.Value.GetDestinationSlot().Equals(slot))
                    {
                        substrate = item.Value;
                        return true;
                    }
                }
                //for (int i = 0; i < item.Value.Count; ++i)
                //{
                //    if (item.Value[i].GetDestinationPortId().Equals(portId) &&
                //        item.Value[i].GetDestinationSlot().Equals(slot))
                //    {
                //        substrate = item.Value[i];
                //        return true;
                //    }
                //}
            }

            foreach (var robot in SubstratesInRobot)
            {
                foreach (var item in robot.Value)
                {
                    if (item.Value.GetDestinationPortId().Equals(portId) &&
                       item.Value.GetDestinationSlot().Equals(slot))
                    {
                        substrate = item.Value;
                        return true;
                    }
                }
            }

            return false;
        }
        #endregion </LoadPort>

        #region <ProcessModule>
        public bool GetSubstrateAtProcessModule(string substrateName, ref Substrate substrate)
        {
            foreach (var pm in SubstratesInProcessModule)
            {
                foreach (var item in pm.Value)
                {
                    string name = item.Value.GetName();
                    if (name.Equals(substrateName))
                    {
                        substrate = item.Value;
                        return true;
                    }

                }
                //for (int i = 0; i < item.Value.Count; ++i)
                //{
                //    string name = item.Value[i].GetName();
                //    if (name.Equals(substrateName))
                //    {
                //        substrate = item.Value[i];
                //        return true;
                //    }
                //}
            }

            return false;
        }

        public bool GetSubstrateAtProcessModule(string substrateName, ProcessModuleLocation location, ref Substrate substrate)
        {
            //if (false == SubstratesInProcessModule.TryGetValue(location.ProcessModuleName, out ConcurrentDictionary<int, Substrate> substrates))
            //    return false;

            foreach (var item in SubstratesInProcessModule[location.ProcessModuleName])
            {
                string name = item.Value.GetName();
                if (name.Equals(substrateName))
                {
                    substrate = item.Value;
                    return true;
                }
            }
            //for (int i = 0; i < SubstratesInProcessModule[location.ProcessModuleName].Count; ++i)
            //{
            //    string name = SubstratesInProcessModule[location.ProcessModuleName][i].GetName();
            //    if (name.Equals(substrateName))
            //    {
            //        substrate = SubstratesInProcessModule[location.ProcessModuleName][i];
            //        return true;
            //    }
            //}

            return false;
        }
        public bool GetSubstrateAtProcessModule(string processModuleName, string substrateName, ref Substrate substrate)
        {
            if (false == SubstratesInProcessModule.ContainsKey(processModuleName))
                return false;

            foreach (var item in SubstratesInProcessModule[processModuleName])
            {
                string name = item.Value.GetName();
                if (name.Equals(substrateName))
                {
                    substrate = item.Value;
                    return true;
                }
            //    for (int i = 0; i < SubstratesInProcessModule[processModuleName].Count; ++i)
            //{
            //    string name = SubstratesInProcessModule[processModuleName][i].GetName();
            //    if (name.Equals(substrateName))
            //    {
            //        substrate = SubstratesInProcessModule[processModuleName][i];
            //        return true;
            //    }
            }

            return false;
        }
        public bool GetSubstratesAtProcessModule(string processModuleName, ref List<Substrate> substrates)
        {
            if (false == SubstratesInProcessModule.TryGetValue(processModuleName, out ConcurrentDictionary<int, Substrate> substratesAtProcessModule) ||
                substratesAtProcessModule == null)
                return false;

            substrates.Clear();
            substrates.AddRange(substratesAtProcessModule.Values);
            return true;
            //foreach (var item in SubstratesInProcessModule)
            //{
            //    if (item.Key.Equals(processModuleName))
            //    {
            //        substrates = item.Value;
            //        return true;
            //    }
            //}

            //return false;
        }
        #endregion </ProcessModule>

        #region <Robot>
        public bool GetSubstratesAtRobotAll(string robotName, ref Dictionary<RobotArmTypes, Substrate> substrates)
        {
            if (SubstratesInRobot.TryGetValue(robotName, out ConcurrentDictionary<RobotArmTypes, Substrate> temporarySubstrates))
            {
                substrates = new Dictionary<RobotArmTypes, Substrate>(temporarySubstrates);
                return true;
            }

            return false;
        }
        public bool HasSubstrateAtRobot(RobotLocation location)
        {
            return SubstratesInRobot[location.RobotName].ContainsKey(location.Arm);
        }
        public bool GetSubstrateAtRobot(RobotLocation location, ref Substrate substrate)
        {
            return SubstratesInRobot[location.RobotName].TryGetValue(location.Arm, out substrate);
        }
        public bool GetSubstrateAtRobot(string robotName, RobotArmTypes armType, ref Substrate substrate)
        {
            return SubstratesInRobot[robotName].TryGetValue(armType, out substrate);
        }
        #endregion </Robot>

        #region <Find>
        public bool GetSubstratesAll(ref List<Substrate> substrates)
        {
            substrates.Clear();
            foreach (var lp in SubstratesInLoadPortSlots)
            {
                substrates.AddRange(lp.Value.Values);
                //foreach (var item in lp.Value)
                //{
                //    substrates.Add(item.Value);
                //}
            }

            foreach (var item in SubstratesInProcessModule)
            {
                substrates.AddRange(item.Value.Values);
                //for (int i = 0; i < item.Value.Count; ++i)
                //{
                //    substrates.Add(item.Value[i]);
                //}
            }

            foreach (var arms in SubstratesInRobot)
            {
                substrates.AddRange(arms.Value.Values);
                //foreach (var item in arms.Value)
                //{
                //    substrates.Add(item.Value);
                //}
            }

            return substrates.Count > 0;
        }
        public bool GetSubstrateByAttribute(string attributeName, string attributeValue, ref Substrate substrate)
        {
            foreach (var pm in SubstratesInProcessModule)
            {
                foreach (var item in pm.Value)
                {
                    if (item.Value.GetAttribute(attributeName).Equals(attributeValue))
                    {
                        substrate = item.Value;
                        return true;
                    }
                }
            }
            // 2024.12.29. jhlim [END]

            foreach (var arms in SubstratesInRobot)
            {
                foreach (var item in arms.Value)
                {
                    if (item.Value.GetAttribute(attributeName).Equals(attributeValue))
                    {
                        substrate = item.Value;
                        return true;
                    }

                }
            }

            foreach (var lp in SubstratesInLoadPortSlots)
            {
                foreach (var item in lp.Value)
                {
                    if (item.Value.GetAttribute(attributeName).Equals(attributeValue))
                    {
                        substrate = item.Value;
                        return true;
                    }
                }
            }

            return false;
        }
        public bool GetSubstrateByName(string targetName, ref Substrate substrate)
        {
            // 2024.12.29. jhlim [MOD] 공정 설비부터 검색하도록 순서 변경
            foreach (var pm in SubstratesInProcessModule)
            {
                foreach (var item in pm.Value)
                {
                    if (item.Value.GetName().Equals(targetName))
                    {
                        substrate = item.Value;
                        return true;
                    }
                }                
            }
            // 2024.12.29. jhlim [END]

            foreach (var arms in SubstratesInRobot)
            {
                foreach (var item in arms.Value)
                {
                    if (item.Value.GetName().Equals(targetName))
                    {
                        substrate = item.Value;
                        return true;
                    }

                }
            }

            foreach (var lp in SubstratesInLoadPortSlots)
            {
                foreach (var item in lp.Value)
                {
                    if (item.Value.GetName().Equals(targetName))
                    {
                        substrate = item.Value;
                        return true;
                    }
                }
            }

            return false;
        }
        public bool GetSubstrateByName(string targetName, Location location, ref Substrate substrate)
        {
            if (location is LoadPortLocation)
            {
                foreach (var lp in SubstratesInLoadPortSlots)
                {
                    foreach (var item in lp.Value)
                    {
                        if (item.Value.GetName().Equals(targetName))
                        {
                            substrate = item.Value;
                            return true;
                        }
                    }
                }
            }
            else if (location is ProcessModuleLocation)
            {
                foreach (var pm in SubstratesInProcessModule)
                {
                    foreach (var item in pm.Value)
                    {
                        string name = item.Value.GetName();
                        if (name.Equals(targetName))
                        {
                            substrate = item.Value;
                            return true;
                        }
                    }
                    //for (int i = 0; i < pm.Value.Count; ++i)
                    //{
                    //    string name = pm.Value[i].GetName();
                    //    if (name.Equals(targetName))
                    //    {
                    //        substrate = pm.Value[i];
                    //        return true;
                    //    }
                    //}
                }
            }
            else if (location is RobotLocation)
            {
                foreach (var arms in SubstratesInRobot)
                {
                    foreach (var item in arms.Value)
                    {
                        if (item.Value.GetName().Equals(targetName))
                        {
                            substrate = item.Value;
                            return true;
                        }

                    }
                }
            }
            else
                return false;

            return false;
        }
        public bool GetSubstrate(Location location, string targetName, ref Substrate substrate)
        {
            if (location is LoadPortLocation)
            {
                var lpLocation = location as LoadPortLocation;

                return SubstratesInLoadPortSlots[lpLocation.PortId].TryGetValue(lpLocation.Slot, out substrate);
            }
            else if (location is ProcessModuleLocation)
            {
                var pmLocation = location as ProcessModuleLocation;

                if (false == SubstratesInProcessModule.ContainsKey(pmLocation.ProcessModuleName))
                    return false;

                foreach (var item in SubstratesInProcessModule[pmLocation.ProcessModuleName])
                {
                    string name = item.Value.GetName();
                    if (name.Equals(targetName))
                    {
                        substrate = item.Value;
                        return true;
                    }
                }

                return false;
            }
            else if (location is RobotLocation)
            {
                var robotLocation = location as RobotLocation;

                return SubstratesInRobot[robotLocation.RobotName].TryGetValue(robotLocation.Arm, out substrate);
            }
            else
                return false;
        }
        public bool GetSubstrateByLocation(Location location, string targetName, ref Substrate substrate)
        {
            if (location is LoadPortLocation)
            {
                var lpLocation = location as LoadPortLocation;

                return SubstratesInLoadPortSlots[lpLocation.PortId].TryGetValue(lpLocation.Slot, out substrate);
            }
            else if (location is ProcessModuleLocation)
            {
                var pmLocation = location as ProcessModuleLocation;
                if (false == SubstratesInProcessModule.ContainsKey(pmLocation.ProcessModuleName))
                    return false;

                foreach (var item in SubstratesInProcessModule[pmLocation.ProcessModuleName])
                {
                    string name = item.Value.GetName();
                    if (name.Equals(targetName))
                    {
                        substrate = item.Value;
                        return true;
                    }
                }
            }
            else if (location is RobotLocation)
            {
                var robotLocation = location as RobotLocation;
                return SubstratesInRobot[robotLocation.RobotName].TryGetValue(robotLocation.Arm, out substrate);
            }
            else
                return false;

            return false;
        }
        #endregion </Find>

        #endregion </Get Substrate>

        #endregion </Methods>
    }
}
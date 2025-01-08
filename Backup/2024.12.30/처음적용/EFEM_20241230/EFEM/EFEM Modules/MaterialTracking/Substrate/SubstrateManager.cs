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

            SubstratesInLoadPortSlots = new ConcurrentDictionary<int, Dictionary<int, Substrate>>();
            SubstratesInProcessModule = new ConcurrentDictionary<string, List<Substrate>>();
            SubstratesInRobot = new ConcurrentDictionary<string, Dictionary<RobotArmTypes, Substrate>>();
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

        private readonly ConcurrentDictionary<int, Dictionary<int, Substrate>> SubstratesInLoadPortSlots = null;
        private readonly ConcurrentDictionary<string, List<Substrate>> SubstratesInProcessModule = null;
        private readonly ConcurrentDictionary<string, Dictionary<RobotArmTypes, Substrate>> SubstratesInRobot = null;
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
                EFEMLogger.Instance.WriteLog(string.Format("LoadRecoveryDataAll Exception > {0}, {1}",
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
                            EFEMLogger.Instance.WriteLog(string.Format("SaveRecoveryData failed > {0}", item.Key));
                        }
                    }
                }

                foreach (var item in SubstratesInProcessModule)
                {
                    for (int i = 0; i < item.Value.Count; ++i)
                    {
                        if (false == item.Value[i].SaveRecoveryData())
                        {
                            EFEMLogger.Instance.WriteLog(string.Format("SaveRecoveryData failed > {0}", item.Key));
                        }
                    }
                }

                foreach (var robot in SubstratesInRobot)
                {
                    foreach (var item in robot.Value)
                    {
                        if (false == item.Value.SaveRecoveryData())
                        {
                            EFEMLogger.Instance.WriteLog(string.Format("SaveRecoveryData failed > {0}", item.Key));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                EFEMLogger.Instance.WriteLog(string.Format("SaveRecoveryDataAll Exception > {0}, {1}",
                    ex.Message, ex.StackTrace));

                return false;
            }

            return true;
        }

        //public bool LoadRecoveryData(string fileName, ref Substrate substrate)
        //{
        //    bool created = false;
        //    try
        //    {
        //        string fullPath = string.Format(@"{0}\{1}", RecoveryFileDefines.RecoveryFilePath, fileName);
        //        if (false == File.Exists(fullPath))
        //            return false;

        //        string fileData = String.Empty;
        //        if (false == _fileIOManager.Read(RecoveryFileDefines.RecoveryFilePath, fileName, ref fileData, Define.DefineConstant.FileIO.TIMEOUT_READ))
        //            return false;

        //        string[] data = fileData.Split('\n');
        //        if (false == _fileComposite.CreateRootByString(ref data))
        //        {
        //            _fileComposite.RemoveRoot(RecoveryFileDefines.FileRootName);
        //            return false;
        //        }

        //        created = true;

        //        Dictionary<string, string> attributes = substrate.GetAttributesAll();
        //        Dictionary<string, string> readAttributes = new Dictionary<string, string>(attributes);
        //        foreach (var item in attributes)
        //        {
        //            string key = item.Key;
        //            string value = string.Empty;
        //            if (false == _fileComposite.GetValue(RecoveryFileDefines.FileRootName, key, ref value))
        //                continue;

        //            readAttributes[key] = value;
        //        }

        //        bool result = _fileComposite.RemoveRoot(RecoveryFileDefines.FileRootName);

        //        if (false == substrate.SetAttributesAll(readAttributes))
        //        {
        //            return false;
        //        }

        //        return result;
        //    }
        //    catch (Exception ex)
        //    {
        //        if (created)
        //        {
        //            _fileComposite.RemoveRoot(RecoveryFileDefines.FileRootName);
        //        }

        //        EFEMLogger.Instance.WriteLog(string.Format("LoadRecoveryData Exception > {0}, {1}, {2}", 
        //            fileName, ex.Message, ex.StackTrace));

        //        return false;
        //    }
        //}

        //public bool SaveRecoveryData(Substrate substrate)
        //{
        //    bool created = false;
        //    string fileName = substrate.GetName();

        //    try
        //    {
        //        string fullPath = string.Format(@"{0}\{1}", RecoveryFileDefines.RecoveryFilePath, fileName);
        //        if (File.Exists(fileName))
        //        {
        //            File.Delete(fullPath);
        //        }

        //        _fileComposite.RemoveRoot(RecoveryFileDefines.FileRootName);
        //        _fileComposite.CreateRoot(RecoveryFileDefines.FileRootName);

        //        created = true;

        //        Dictionary<string, string> attributes = substrate.GetAttributesAll();
        //        foreach (var item in attributes)
        //        {
        //            _fileComposite.AddItem(RecoveryFileDefines.FileRootName, item.Key, item.Value);
        //        }

        //        string dataToWrite = String.Empty;
        //        if (false == _fileComposite.GetStructureAsString(RecoveryFileDefines.FileRootName, ref dataToWrite))
        //        {
        //            EFEMLogger.Instance.WriteLog(string.Format("GetStructureAsString Exception > {0}",
        //            fileName));

        //            return _fileComposite.RemoveRoot(RecoveryFileDefines.FileRootName);
        //        }

        //        bool result = _fileIOManager.Write(RecoveryFileDefines.RecoveryFilePath, fileName, dataToWrite, false, true);
        //        result &= _fileComposite.RemoveRoot(RecoveryFileDefines.FileRootName);

        //        return result;
        //    }
        //    catch (Exception ex)
        //    {
        //        if (created)
        //        {
        //            _fileComposite.RemoveRoot(RecoveryFileDefines.FileRootName);
        //        }

        //        EFEMLogger.Instance.WriteLog(string.Format("SaveRecoveryData Exception > {0}, {1}, {2}",
        //            fileName, ex.Message, ex.StackTrace));

        //        return false;
        //    }
        //}

        //public bool SerializeAll()
        //{
        //    bool result = true;
        //    foreach (var item in Substrates)
        //    {
        //        result &= Serialize(item.Value);                
        //    }

        //    return result;
        //}
        //public bool DeSerializeAll()
        //{
        //    if (false == Directory.Exists(RecoveryFilePathDefines.RecoveryFilePath))
        //    {
        //        Directory.CreateDirectory(RecoveryFilePathDefines.RecoveryFilePath);
        //        return false;
        //    }

        //    string[] files = Directory.GetFiles(RecoveryFilePathDefines.RecoveryFilePath);
        //    if (files.Length <= 0)
        //        return false;

        //    for (int i = 0; i < files.Length; ++i)
        //    {
        //        Substrate substrate = new Substrate("");
        //        if (Deserialize(files[i], ref substrate))
        //        {
        //            Substrates[substrate.GetName()] = substrate;
        //        }
        //    }

        //    return true;
        //}
        //public bool Serialize(Substrate substrate)
        //{
        //    try
        //    {
        //        if (false == Directory.Exists(RecoveryFilePathDefines.RecoveryFilePath))
        //            Directory.CreateDirectory(RecoveryFilePathDefines.RecoveryFilePath);

        //        string fileName = substrate.GetName();
        //        if (string.IsNullOrEmpty(substrate.GetName()))
        //        {
        //            fileName = "UnknownSubstrate";
        //        }
        //        string fullPath = string.Format(@"{0}\{1}.xml", RecoveryFilePathDefines.RecoveryFilePath, fileName);

        //        XmlSerializer serializer = new XmlSerializer(typeof(Substrate));
        //        using (TextWriter writer = new StreamWriter(fullPath))
        //        {
        //            serializer.Serialize(writer, substrate);
        //        }

        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        EFEMLogger.Instance.WriteLog(string.Format("Serialize Exception > {0}, {1}, {2}", substrate.GetName(), ex.Message, ex.StackTrace));

        //        return false;
        //    }
        //}

        //public bool Deserialize(string fileName, ref Substrate substrate)
        //{
        //    try
        //    {
        //        if (false == Directory.Exists(RecoveryFilePathDefines.RecoveryFilePath))
        //            Directory.CreateDirectory(RecoveryFilePathDefines.RecoveryFilePath);

        //        XmlSerializer serializer = new XmlSerializer(typeof(Substrate));
        //        using (TextReader reader = new StreamReader(fileName))
        //        {
        //            Substrate sub = serializer.Deserialize(reader);
        //            return true;
        //        }                
        //    }
        //    catch (Exception ex)
        //    {
        //        EFEMLogger.Instance.WriteLog(string.Format("Deserialize Exception > {0}, {1}, {2}", fileName, ex.Message, ex.StackTrace));

        //        return false;
        //    }
        //}
        #endregion <File Control>

        #region <Create, Remove>
        public void AddLoadPortBuffers(int portId, int capacity)
        {
            SubstratesInLoadPortSlots[portId] = new Dictionary<int, Substrate>();
            //Dictionary<int, Substrate> slots = new Dictionary<int, Substrate>();
            //for(int i = 0; i < capacity; ++i)
            //{
            //    slots[i] = null;
            //}
            //SubstratesInLoadPortSlots[portId] = slots;
        }

        public void AddRobotBuffers(string robotName)
        {
            SubstratesInRobot[robotName] = new Dictionary<RobotArmTypes, Substrate>();
            //Dictionary<RobotArmTypes, Substrate> robotArms = new Dictionary<RobotArmTypes, Substrate>
            //{
            //    [RobotArmTypes.UpperArm] = null,
            //    [RobotArmTypes.LowerArm] = null
            //};

            //SubstratesInRobot[robotName] = robotArms;
        }

        public void AddProcessModuleBuffers(string processModuleName)
        {
            SubstratesInProcessModule[processModuleName] = new List<Substrate>();
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

                SubstratesInLoadPortSlots.TryGetValue(lpLocation.PortId, out Dictionary<int, Substrate> substrates);
                substrates[lpLocation.Slot] = substrate;
                SubstratesInLoadPortSlots[lpLocation.PortId] = substrates;
            }
            else if (location is ProcessModuleLocation)
            {
                var pmLocation = location as ProcessModuleLocation;

                if (false == SubstratesInProcessModule.TryGetValue(pmLocation.ProcessModuleName, out List<Substrate> substrates))
                    substrates = new List<Substrate>();

                substrate.SetLocation(location);
                substrate.InitAttributes();

                string substrateName = substrate.GetName();
                for (int i = 0; i < substrates.Count; ++i)
                {
                    if (substrates[i].GetName().Equals(substrateName))
                        return;
                }
                substrates.Add(substrate);
                SubstratesInProcessModule[pmLocation.ProcessModuleName] = substrates;
            }
            else if (location is RobotLocation)
            {
                var robotLocation = location as RobotLocation;

                SubstratesInRobot.TryGetValue(robotLocation.RobotName, out Dictionary<RobotArmTypes, Substrate> substrates);
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
                    SubstratesInLoadPortSlots[lpLocation.PortId].Remove(lpLocation.Slot);
                }
            }
            else if (location is ProcessModuleLocation)
            {
                var pmLocation = location as ProcessModuleLocation;

                int index = -1;
                for (int i = 0; i < SubstratesInProcessModule[pmLocation.ProcessModuleName].Count; ++i)
                {
                    string name = SubstratesInProcessModule[pmLocation.ProcessModuleName][i].GetName();
                    if (name.Equals(targetName))
                    {
                        index = i;
                        SubstratesInProcessModule[pmLocation.ProcessModuleName][i].DeleteRecoveryData();
                    }
                }

                if (index >= 0)
                {
                    SubstratesInProcessModule[pmLocation.ProcessModuleName].RemoveAt(index);
                }

            }
            else if (location is RobotLocation)
            {
                var robotLocation = location as RobotLocation;

                if (SubstratesInRobot[robotLocation.RobotName].ContainsKey(robotLocation.Arm))
                {
                    SubstratesInRobot[robotLocation.RobotName][robotLocation.Arm].DeleteRecoveryData();
                    SubstratesInRobot[robotLocation.RobotName].Remove(robotLocation.Arm);
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
        public void RemoveSubstrateInLoadPortAll(int portId)
        {
            SubstratesInLoadPortSlots.TryGetValue(portId, out Dictionary<int, Substrate> value);
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
                SubstratesInLoadPortSlots[portId].Remove(slot);
            }
        }

        public void AssignSubstrateInProcessModule(string moduleName, Substrate substrate)
        {
            if (SubstratesInProcessModule.ContainsKey(moduleName))
            {
                SubstratesInProcessModule[moduleName].Add(substrate);
            }
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
            Substrate substrate;
            if (location is LoadPortLocation)
            {
                var lpLocation = location as LoadPortLocation;

                if (false == SubstratesInLoadPortSlots[lpLocation.PortId].TryGetValue(lpLocation.Slot, out substrate))
                    return;
            }
            else if (location is ProcessModuleLocation)
            {
                var pmLocation = location as ProcessModuleLocation;

                int index = -1;
                for (int i = 0; i < SubstratesInProcessModule[pmLocation.ProcessModuleName].Count; ++i)
                {
                    string name = SubstratesInProcessModule[pmLocation.ProcessModuleName][i].GetName();
                    if (name.Equals(targetName))
                    {
                        index = i;
                        break;
                    }
                }
                if (index < 0)
                    return;

                substrate = SubstratesInProcessModule[pmLocation.ProcessModuleName][index];
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
            foreach (var item in SubstratesInProcessModule)
            {
                for (int i = 0; i < item.Value.Count; ++i)
                {
                    substrates.Add(item.Value[i]);
                }
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

                for (int i = 0; i < SubstratesInProcessModule[location.ProcessModuleName].Count; ++i)
                {
                    string name = SubstratesInProcessModule[location.ProcessModuleName][i].GetName();
                    if (name.Equals(substrateName))
                    {
                        substrate = SubstratesInProcessModule[location.ProcessModuleName][i];
                        return true;
                    }
                }

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
            return SubstratesInLoadPortSlots[portId];
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

            foreach (var item in SubstratesInProcessModule)
            {
                {
                    for (int i = 0; i < item.Value.Count; ++i)
                    {
                        if (item.Value[i].GetSourcePortId().Equals(portId) && item.Value[i].GetSourceSlot().Equals(slot))
                            return true;
                    }

                }
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

            foreach (var item in SubstratesInProcessModule)
            {
                {
                    for (int i = 0; i < item.Value.Count; ++i)
                    {
                        if (false == item.Value[i].GetSourcePortId().Equals(portId))
                            continue;
                        if (false == IsProcessingCompleted(item.Value[i].GetTransferStatus(), item.Value[i].GetProcessingStatus()))
                            return false;
                    }

                }
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

            foreach (var item in SubstratesInProcessModule)
            {
                {
                    for (int i = 0; i < item.Value.Count; ++i)
                    {
                        // 이름이 다르고, 포트번호가 같으면 같은데서 있다가 나간 자재다.
                        if (item.Value[i].GetSourcePortId().Equals(portId) &&
                            item.Value[i].GetSourceCarrierId().Equals(sourceCarrierId) &&
                            false == item.Value[i].GetName().Equals(substrateName))
                        {
                            return false;
                        }
                    }
                }
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
            foreach (var item in SubstratesInProcessModule)
            {
                for (int i = 0; i < item.Value.Count; ++i)
                {
                    if (item.Value[i].GetSourcePortId().Equals(portId) &&
                        item.Value[i].GetSourceSlot().Equals(slot) &&
                        item.Value[i].GetSourceCarrierId().Equals(carrierId))
                    {
                        substrate = item.Value[i];
                        return true;
                    }
                }
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
            foreach (var item in SubstratesInProcessModule)
            {
                for (int i = 0; i < item.Value.Count; ++i)
                {
                    if (item.Value[i].GetDestinationPortId().Equals(portId) &&
                        item.Value[i].GetDestinationSlot().Equals(slot))
                    {
                        substrate = item.Value[i];
                        return true;
                    }
                }
            }

            foreach (var pm in SubstratesInRobot)
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
            }

            return false;
        }
        #endregion </LoadPort>

        #region <ProcessModule>
        public bool GetSubstrateAtProcessModule(string substrateName, ref Substrate substrate)
        {
            foreach (var item in SubstratesInProcessModule)
            {
                for(int i = 0; i < item.Value.Count; ++i)
                {
                    string name = item.Value[i].GetName();
                    if (name.Equals(substrateName))
                    {
                        substrate = item.Value[i];
                        return true;
                    }
                }
            }

            return false;
        }

        public bool GetSubstrateAtProcessModule(string substrateName, ProcessModuleLocation location, ref Substrate substrate)
        {
            if (false == SubstratesInProcessModule.ContainsKey(location.ProcessModuleName))
                return false;

            for (int i = 0; i < SubstratesInProcessModule[location.ProcessModuleName].Count; ++i)
            {
                string name = SubstratesInProcessModule[location.ProcessModuleName][i].GetName();
                if (name.Equals(substrateName))
                {
                    substrate = SubstratesInProcessModule[location.ProcessModuleName][i];
                    return true;
                }
            }

            return false;
        }
        public bool GetSubstrateAtProcessModule(string processModuleName, string substrateName, ref Substrate substrate)
        {
            if (false == SubstratesInProcessModule.ContainsKey(processModuleName))
                return false;

            for (int i = 0; i < SubstratesInProcessModule[processModuleName].Count; ++i)
            {
                string name = SubstratesInProcessModule[processModuleName][i].GetName();
                if (name.Equals(substrateName))
                {
                    substrate = SubstratesInProcessModule[processModuleName][i];
                    return true;
                }
            }

            return false;
        }
        public bool GetSubstratesAtProcessModule(string processModuleName, ref List<Substrate> substrates)
        {
            foreach (var item in SubstratesInProcessModule)
            {
                if (item.Key.Equals(processModuleName))
                {
                    substrates = item.Value;
                    return true;
                }
            }

            return false;
        }
        #endregion </ProcessModule>

        #region <Robot>
        public bool GetSubstratesAtRobotAll(string robotName, ref Dictionary<RobotArmTypes, Substrate> substrates)
        {
            return SubstratesInRobot.TryGetValue(robotName, out substrates);
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
        public List<Substrate> GetSubstratesAll()
        {
            List<Substrate> substrates = new List<Substrate>();
            foreach (var lp in SubstratesInLoadPortSlots)
            {
                foreach (var item in lp.Value)
                {
                    substrates.Add(item.Value);
                }
            }

            foreach (var item in SubstratesInProcessModule)
            {
                for (int i = 0; i < item.Value.Count; ++i)
                {
                    substrates.Add(item.Value[i]);
                }
            }

            foreach (var arms in SubstratesInRobot)
            {
                foreach (var item in arms.Value)
                {
                    substrates.Add(item.Value);
                }
            }

            return substrates;
        }
        public bool GetSubstrateByName(string targetName, ref Substrate substrate)
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

            foreach (var item in SubstratesInProcessModule)
            {
                {
                    for (int i = 0; i < item.Value.Count; ++i)
                    {
                        if (item.Value[i].GetName().Equals(targetName))
                        {
                            substrate = item.Value[i];
                            return true;
                        }
                    }

                }
            }

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
                    for (int i = 0; i < pm.Value.Count; ++i)
                    {
                        string name = pm.Value[i].GetName();
                        if (name.Equals(targetName))
                        {
                            substrate = pm.Value[i];
                            return true;
                        }
                    }
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

                for (int i = 0; i < SubstratesInProcessModule[pmLocation.ProcessModuleName].Count; ++i)
                {
                    string name = SubstratesInProcessModule[pmLocation.ProcessModuleName][i].GetName();
                    if (name.Equals(targetName))
                    {
                        substrate = SubstratesInProcessModule[pmLocation.ProcessModuleName][i];
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

                foreach (var item in SubstratesInProcessModule)
                {
                    for (int i = 0; i < item.Value.Count; ++i)
                    {
                        string name = SubstratesInProcessModule[pmLocation.ProcessModuleName][i].GetName();
                        if (name.Equals(targetName))
                        {
                            substrate = SubstratesInProcessModule[pmLocation.ProcessModuleName][i];
                            return true;
                        }
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
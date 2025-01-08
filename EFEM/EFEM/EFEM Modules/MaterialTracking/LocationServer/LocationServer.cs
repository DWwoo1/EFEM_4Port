using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

//using EFEM.MaterialTracking.LocationServer.LocationType;
using EFEM.Defines.LoadPort;
using EFEM.Defines.AtmRobot;
using EFEM.Defines.MaterialTracking;
using EFEM.Modules;

namespace EFEM.MaterialTracking.LocationServer
{
    //public class LocationServer
    //{
    //    #region <Constructors>
    //    private LocationServer() 
    //    {
    //        _loadPortManager = LoadPortManager.Instance;
    //    }
    //    #endregion </Constructors>

    //    #region <Fields>
    //    private static LocationServer _instance = null;
    //    private readonly Dictionary<string, Dictionary<LoadPortLoadingMode, string>> LoadPorts = new Dictionary<string, Dictionary<LoadPortLoadingMode, string>>();
    //    private readonly Dictionary<string, string[]> ProcessModules = new Dictionary<string, string[]>();
    //    private readonly Dictionary<string, Dictionary<RobotArmTypes, string>> AtmRobots = new Dictionary<string, Dictionary<RobotArmTypes, string>>();

    //    private static LoadPortManager _loadPortManager = null;
    //    #endregion </Fields>

    //    #region <Properties>
    //    public static LocationServer Instance
    //    {
    //        get
    //        {
    //            if (_instance == null)
    //                _instance = new LocationServer();

    //            return _instance;
    //        }
    //    }
    //    #endregion </Properties>

    //    #region <Methods>
    //    public void AddLoadPortLocation(string name, Dictionary<LoadPortLoadingMode, string> locationNames)
    //    {
    //        if (false == LoadPorts.ContainsKey(name))
    //        {
    //            LoadPorts.Add(name, locationNames);
    //        }
    //    }
    //    public void AddProcessModuleLocation(string name, string[] locationNames)
    //    {
    //        ProcessModules.Add(name, locationNames);
    //    }
    //    public void AddRobotLocation(string name, Dictionary<RobotArmTypes, string> locationNames)
    //    {
    //        AtmRobots.Add(name, locationNames);
    //    }

    //    public bool GetLocationModuleType(string locationName, ref ModuleType locationType)
    //    {
    //        if (LoadPorts.ContainsKey(locationName))
    //        {
    //            locationType = ModuleType.LoadPort;
    //            return true;
    //        }

    //        // Process Module
    //        if (ProcessModules.ContainsKey(locationName))
    //        {
    //            locationType = ModuleType.ProcessModule;
    //            return true;
    //        }

    //        foreach (var item in ProcessModules)
    //        {
    //            for (int i = 0; i < item.Value.Length; ++i)
    //            {
    //                if (item.Value[i].Equals(locationName))
    //                {
    //                    locationType = ModuleType.ProcessModule;
    //                    return true;
    //                }
    //            }
    //        }

    //        // Robot
    //        foreach (var item in AtmRobots)
    //        {
    //            foreach (var kvp in item.Value)
    //            {
    //                if (kvp.Value.Equals(locationName))
    //                {
    //                    locationType = ModuleType.Robot;
    //                    return true;
    //                }
    //            }
    //        }

    //        locationType = ModuleType.UnknownLocation;
    //        return false;
    //    }

    //    public string[] GetLocationsAll()
    //    {
    //        List<string> locations = new List<string>();
    //        foreach (var item in LoadPorts)
    //        {
    //            if (item.Value == null)
    //                continue;

    //            foreach (var kvp in item.Value)
    //            {
    //                locations.Add(kvp.Value);
    //            }
    //        }

    //        foreach (var item in ProcessModules)
    //        {
    //            if (item.Value == null)
    //                continue;

    //            for (int i = 0; i < item.Value.Length; ++i)
    //            {
    //                locations.Add(item.Value[i]);
    //            }
    //        }

    //        foreach(var item in AtmRobots)
    //        {
    //            if (item.Value == null)
    //                continue;

    //            foreach (var kvp in item.Value)
    //            {
    //                locations.Add(kvp.Value);
    //            }
    //        }

    //        return locations.ToArray();
    //    }

    //    public bool FindLoadPortLocation(string lpName, LoadPortLoadingMode lpType, ref string locationName)
    //    {
    //        if (false == LoadPorts.ContainsKey(lpName))
    //            return false;

    //        if (false == LoadPorts[lpName].ContainsKey(lpType))
    //            return false;

    //        return LoadPorts[lpName].TryGetValue(lpType, out locationName);
    //    }

    //    public bool GetRobotLocation(string robotName, RobotArmTypes armType, ref string locationName)
    //    {
    //        if (false == AtmRobots.ContainsKey(robotName))
    //            return false;

    //        if (false == AtmRobots[robotName].ContainsKey(armType))
    //            return false;

    //        locationName = AtmRobots[robotName][armType];
    //        return true;

    //        //if (false == AtmRobots.ContainsKey(robotIndex))
    //        //    return false;

    //        //locationName = AtmRobots[robotIndex][armType];

    //        //return (false == string.IsNullOrEmpty(locationName));
    //    }

    //    //public string[] GetLocationsAll()
    //    //{
    //    //    List<string> locations = new List<string>(LoadPorts.Keys.ToList());

    //    //    locations.AddRange(ProcessModules);

    //    //    return locations.ToArray();
    //    //}

    //    public bool GetActualLocationNameByLocation(string sourceLocation, ref string actualName)
    //    {
    //        // LoadPort
    //        if (LoadPorts.ContainsKey(sourceLocation))
    //        {
    //            actualName = _loadPortManager.GetLoadPortLocationName(sourceLocation);

    //            return true;
    //        }

    //        // Process Module
    //        if (ProcessModules.ContainsKey(sourceLocation))
    //            return true;

    //        foreach (var item in ProcessModules)
    //        {
    //            for (int i = 0; i < item.Value.Length; ++i)
    //            {
    //                if (item.Value[i].Equals(sourceLocation))
    //                {
    //                    actualName = sourceLocation;
    //                    return true;
    //                }
    //            }
    //        }

    //        // Robot
    //        foreach (var item in AtmRobots)
    //        {
    //            foreach (var kvp in item.Value)
    //            {
    //                if (kvp.Value.Equals(sourceLocation))
    //                {
    //                    actualName = sourceLocation;
    //                    return true;
    //                }
    //            }
    //        }

    //        return false;
    //    }

    //    public bool CheckLocationValidity(string location)
    //    {
    //        // LoadPort
    //        if (LoadPorts.ContainsKey(location))
    //            return true;

    //        foreach (var item in LoadPorts)
    //        {
    //            foreach (var kvp in item.Value)
    //            {
    //                if (kvp.Value.Equals(location))
    //                    return true;
    //            }
    //        }

    //        // Process Module
    //        if (ProcessModules.ContainsKey(location))
    //            return true;

    //        foreach (var item in ProcessModules)
    //        {
    //            for (int i = 0; i < item.Value.Length; ++i)
    //            {
    //                if (item.Value[i].Equals(location))
    //                    return true;
    //            }
    //        }

    //        // Robot
    //        foreach (var item in AtmRobots)
    //        {
    //            foreach (var kvp in item.Value)
    //            {
    //                if (kvp.Value.Equals(location))
    //                    return true;
    //            }
    //        }

    //        return false;
    //    }

    //    public string GetLocationNameByCarrierInfo(string carrierId, int slot)
    //    {
    //        return string.Format("{0}.{1}", carrierId, slot);
    //    }
    //    public void GetRobotInfoByLocationName(string locationName, ref string robotName, ref RobotArmTypes armType)
    //    {
    //        string[] splitted = locationName.Split('.');

    //        int length = splitted.Length;
    //        if (length < 2)
    //            return;

    //        robotName = string.Join(".", splitted, 0, length - 1);
    //        Enum.TryParse(splitted[length - 1], out armType);
    //    }
    //    public bool GetCarrierInfoByLocation(string name, ref int portId, ref int slot)
    //    {
    //        string[] splitted = name.Split('.');
    //        int length = splitted.Length;
    //        if (length < 2)
    //            return false;

    //        if (false == int.TryParse(splitted[length - 2], out portId))
    //            return false;

    //        return int.TryParse(splitted[length - 1], out slot);
    //    }
    //    #endregion </Methods>
    //}
    //public class Location
    //{
    //    public Location(string[] locationNames)
    //    {
    //        if (locationNames != null)
    //        {
    //            LocationNames = new string[locationNames.Length];
    //            Array.Copy(locationNames, LocationNames, locationNames.Length);
    //        }
    //    }

    //    public string[] LocationNames { get; private set; }
    //    protected readonly LocationServer LocationServer = LocationServer.Instance;
    //}
    //public class ProcessModuleLocation : Location
    //{
    //    public ProcessModuleLocation(string name, string[] locationNames)
    //        : base(locationNames)
    //    {
    //        LocationServer.AddProcessModuleLocation(name, LocationNames);
    //    }
    //}
    //public class LoadPortLocation : Location
    //{
    //    #region <Constructors>
    //    public LoadPortLocation(string name, Dictionary<string, string> locationNames)
    //        : base(locationNames.Values.ToArray())
    //    {
    //        Dictionary<LoadPortLoadingMode, string> convertedLocationName
    //            = new Dictionary<LoadPortLoadingMode, string>();

    //        foreach (var item in locationNames)
    //        {
    //            if (false == Enum.TryParse(item.Key, out LoadPortLoadingMode lpType))
    //                continue;

    //            convertedLocationName.Add(lpType, item.Value);
    //        }

    //        LocationServer.AddLoadPortLocation(name, convertedLocationName);
    //        LoadingLocations = new ReadOnlyDictionary<LoadPortLoadingMode, string>(convertedLocationName);
    //    }
    //    #endregion </Constructors>

    //    #region <Fields>
    //    private readonly ReadOnlyDictionary<LoadPortLoadingMode, string> LoadingLocations = null;
    //    #endregion </Fields>

    //    #region <Properties>
    //    protected string GetLocationName(LoadPortLoadingMode lpType)
    //    {
    //        if (false == LoadingLocations.ContainsKey(lpType))
    //            return null;

    //        return LoadingLocations[lpType];
    //    }
    //    #endregion </Properties>
    //}
    //public class RobotLocation : Location
    //{
    //    public RobotLocation(string name)
    //        : base(new string[]
    //        {
    //            string.Format("{0}.{1}", name, RobotArmTypes.UpperArm.ToString()),
    //            string.Format("{0}.{1}", name, RobotArmTypes.LowerArm.ToString())
    //        })
    //    {
    //        Dictionary<RobotArmTypes, string> locations = new Dictionary<RobotArmTypes, string>();
    //        for (int i = 0; i < LocationNames.Length; ++i)
    //        {
    //            string[] spliited = LocationNames[i].Split('.');
    //            if (spliited.Length < 2)
    //                continue;

    //            int armPos = spliited.Length - 1;
    //            if (false == Enum.TryParse(spliited[armPos], out RobotArmTypes location))
    //                continue;

    //            locations.Add(location, LocationNames[i]);
    //        }

    //        LocationServer.AddRobotLocation(name, locations);
    //    }
    //}

    public class LocationServer
    {
        #region <Constructors>
        private LocationServer()
        {
            LoadPortLocations = new Dictionary<int, Dictionary<LoadPortLoadingMode, string>>();
            LoadPortSlotLocations = new Dictionary<int, Dictionary<int, LoadPortLocation>>();
            ProcessModuleLocations = new Dictionary<string, Dictionary<string, ProcessModuleLocation>>();
            RobotLocations = new Dictionary<string, Dictionary<RobotArmTypes, RobotLocation>>();
        }
        #endregion </Constructors>

        #region <Fields>
        private static LocationServer _instance = null;

        private readonly Dictionary<int, Dictionary<LoadPortLoadingMode, string>> LoadPortLocations = null;
        private readonly Dictionary<int, Dictionary<int, LoadPortLocation>> LoadPortSlotLocations = null;
        private readonly Dictionary<string, Dictionary<string, ProcessModuleLocation>> ProcessModuleLocations = null;
        private readonly Dictionary<string, Dictionary<RobotArmTypes, RobotLocation>> RobotLocations = null;
        #endregion </Fields>

        #region <Properties>
        public static LocationServer Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new LocationServer();

                return _instance;
            }
        }
        #endregion </Properties>

        #region <Methods>
        
        #region <Add Location>
        public void AddLoadPortLocation(int portId, Dictionary<LoadPortLoadingMode, string> locations, Dictionary<int, LoadPortLocation> slots)
        {
            LoadPortLocations[portId] = locations;
            LoadPortSlotLocations[portId] = slots;
        }
        public void AddProcessModuleLocation(string moduleName, Dictionary<string, ProcessModuleLocation> locations)
        {
            ProcessModuleLocations[moduleName] = locations;
        }
        public void AddRobotLocation(string robotName, Dictionary<RobotArmTypes, RobotLocation> locations)
        {
            RobotLocations[robotName] = locations;
        }
        #endregion </Add Location>

        #region <Get Location>
        public bool GetLocationByName(string locationName, ref Location location)
        {
            foreach (var item in LoadPortSlotLocations)
            {
                foreach (var kvp in item.Value)
                {
                    if (kvp.Value.Name.Equals(locationName))
                    {
                        location = kvp.Value;
                        return true;
                    }
                }
            }

            foreach (var item in ProcessModuleLocations)
            {
                foreach (var kvp in item.Value)
                {
                    if (kvp.Value.Name.Equals(locationName))
                    {
                        location = kvp.Value;
                        return true;
                    }
                }
            }

            foreach (var item in RobotLocations)
            {
                foreach (var kvp in item.Value)
                {
                    if (kvp.Value.Name.Equals(locationName))
                    {
                        location = kvp.Value;
                        return true;
                    }
                }
            }
            
            location = null;
            return false;
        }
        public string GetLoadPortLocationName(int portId)
        {
            if (false == LoadPortSlotLocations.ContainsKey(portId))
                return string.Empty;

            return LoadPortManager.Instance.GetCurrentLocationName(portId);
        }
        public bool GetLoadPortSlotLocation(int portId, int slot, ref LoadPortLocation location)
        {
            if (false == LoadPortSlotLocations.ContainsKey(portId))
                return false;

            return LoadPortSlotLocations[portId].TryGetValue(slot, out location);
        }
        public bool GetProcessModuleLocationByLocationName(string targetLocationName, ref ProcessModuleLocation location)
        {
            foreach (var pmLocations in ProcessModuleLocations)
            {
                foreach (var item in pmLocations.Value)
                {
                    if(item.Key.Equals(targetLocationName))
                    {
                        location = item.Value;
                        return true;
                    }
                }
            }
            return false;
        }
        public bool GetProcessModuleLocation(string moduleName, string targetLocationName, ref ProcessModuleLocation location)
        {
            if (false == ProcessModuleLocations.ContainsKey(moduleName))
                return false;

            return ProcessModuleLocations[moduleName].TryGetValue(targetLocationName, out location);
        }
        public bool GetRobotLocation(string robotName, RobotArmTypes armType, ref RobotLocation location)
        {
            if (false == RobotLocations.ContainsKey(robotName))
                return false;

            if (false == RobotLocations[robotName].ContainsKey(armType))
                return false;

            return RobotLocations[robotName].TryGetValue(armType, out location);
        }
        #endregion </Get Location>

        #region <ETC>
        public bool GetLocationModuleType(string locationName, ref ModuleType locationType)
        {
            foreach (var item in LoadPortSlotLocations)
            {
                foreach (var kvp in item.Value)
                {
                    if (kvp.Value.Name.Equals(locationName))
                    {
                        locationType = ModuleType.LoadPort;
                        return true;
                    }
                }
            }

            foreach (var item in ProcessModuleLocations)
            {
                foreach (var kvp in item.Value)
                {
                    if (kvp.Value.Name.Equals(locationName))
                    {
                        locationType = ModuleType.ProcessModule;
                        return true;
                    }
                }
            }

            foreach (var item in RobotLocations)
            {
                foreach (var kvp in item.Value)
                {
                    if(kvp.Value.Name.Equals(locationName))
                    {
                        locationType = ModuleType.Robot;
                        return true;
                    }
                }
            }
            locationType = ModuleType.UnknownLocation;

            return false;
        }
        #endregion </ETC>
        #endregion </Methods>
    }
    public class Location
    {
        public Location(string locationName)
        {
            Name = locationName;
            SetSubstrateState(string.Empty, SubstrateLocationState.Unoccupied);
        }

        public string Name { get; private set; }
        public SubstrateLocationState Status { get; private set; }
        public string SubstrateName { get; private set; }

        public void SetName(string newName)
        {
            Name = newName;
        }
        public void SetSubstrateState(string substrateName, SubstrateLocationState state)
        {
            Status = state;
            switch (Status)
            {
                //case SubstrateLocationState.Unoccupied:
                //    break;
                case SubstrateLocationState.Occopied:
                    SubstrateName = substrateName;
                    break;
                default:
                    SubstrateName = string.Empty;
                    break;
            }
        }
    }
    public class LoadPortLocation : Location
    {
        public LoadPortLocation(int portId, int slot, string moduleName)
            : base(string.Format("{0}.{1:d2}", moduleName, slot))
        {
            PortId = portId;
            Slot = slot;
            
            LoadPortName = moduleName;
        }
        public string LoadPortName { get; private set; }
        public int PortId { get; private set; }
        public int Slot { get; private set; }
    }
    public class RobotLocation : Location
    {
        public RobotLocation(RobotArmTypes arm, string moduleName)
            : base(string.Format("{0}.{1}", moduleName, arm.ToString()))
        {
            Arm = arm;
            RobotName = moduleName;
        }
        public string RobotName { get; private set; }
        public RobotArmTypes Arm { get; private set; }
    }
    public class ProcessModuleLocation : Location
    {
        public ProcessModuleLocation(string moduleName, string locationName)
            : base(locationName)
        {
            ProcessModuleName = moduleName;
        }

        public string ProcessModuleName { get; private set; }
    }
}

//namespace LocationServerOnly
//{
//    #region <Class>
//    public class LocationName
//    {
//        #region <Constructors>
//        private LocationName()
//        {
//            _locationNames = (string[])Enum.GetValues(GetLocationType());
//        }
//        #endregion </Constructors>

//        #region <Fields>
//        private static LocationName _inatance = null;
//        private readonly string[] _locationNames = null;
//        #endregion </Fields>

//        #region <Properties>
//        public static LocationName Instance
//        {
//            get
//            {
//                if (_inatance == null)
//                    _inatance = new LocationName();

//                return _inatance;
//            }
//        }

//        public string[] LocationNames
//        {
//            get
//            {
//                return _locationNames;
//            }
//        }
//        #endregion </Properties>

//        #region <Methods>
//        private Type GetLocationType()
//        {
//            switch (FrameOfSystem3.AppConfig.AppConfigManager.Instance.ProcessType)
//            {
//                case Define.DefineEnumProject.AppConfig.EN_PROCESS_TYPE.BIN_SORTER:
//                    return typeof(PWA500BINLocations);
//            }

//            return null;
//        }
//        #endregion </Methods>
//    }
//    #endregion </Class>
//}
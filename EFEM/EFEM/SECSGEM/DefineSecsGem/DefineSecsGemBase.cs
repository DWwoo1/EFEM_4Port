using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Concurrent;
using System.Threading;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrameOfSystem3.SECSGEM.DefineSecsGem
{
    #region <Class>
    public static class PATH
    {
        public static readonly string FILE_PATH_CFG = System.Environment.CurrentDirectory + @"\SecsGem\";
        public static readonly string FILEPATH_LOG = Define.DefineConstant.FilePath.FILEPATH_LOG + @"\SecsGem\";

        public static readonly string FILE_NAME_CFG = "Eq.Cfg";
    }

    public static class Contants
    {
        public static readonly int SCENARIO_STEP_END = 1000;
    }

    public class UserDefinedSecsMessage
    {
        #region <Constuctor>
        public UserDefinedSecsMessage(long stream, long function)
        {
            Stream = stream;
            Function = function;
        }
        #endregion </Constuctor>

        #region <Fields>
        private List<SemiObject> _listItemFormat = new List<SemiObject>();
        #endregion </Fields>

        #region <Properties>
        public string Name { get; private set; }
        public long Stream { get; private set; }
        public long Function { get; private set; }

        public List<SemiObject> ListItemFormat { get { return _listItemFormat; } }
        #endregion </Properties>

        #region <Methods>
        public void GetStructure(ref List<SemiObject> item)
        {
            if (item == null || _listItemFormat == null)
                return;

            item.Clear();
            for (int i = 0; i < _listItemFormat.Count; ++i)
                item.Add(_listItemFormat[i]);
        }
        public void SetStructure(List<SemiObject> item)
        {
            if (item == null || _listItemFormat == null)
                return;

            _listItemFormat.Clear();
            for (int i = 0; i < item.Count; ++i)
                _listItemFormat.Add(item[i]);
        }

        public string GetValueAsStringFromStructure(string nameToGet, int nTarget = 0)
        {
            for (int i = 0; i < _listItemFormat.Count; ++i)
            {
                if (_listItemFormat[i].Name.Equals(nameToGet))
                {
                    return _listItemFormat[i].GetTargetValueString(nTarget);
                }
            }

            return String.Empty;
        }
        #endregion </Methods>
    }

    #region <SemiObject>
    public abstract class SemiObject
    {
        #region <Properties>
        public EN_ITEM_FORMAT Format { get; protected set; }

        public string Name { get; protected set; }
        #endregion </Properties>

        #region Method
        public abstract string GetValueStringAll();
        public abstract string GetValueString();
        public abstract string GetTargetValueString(int nTarget);
        #endregion
    }

    public abstract class ObjectValue<T> : SemiObject
    {
        #region <Constuctor>
        protected ObjectValue(EN_ITEM_FORMAT format, string name, params T[] value)
        {
            Format = format;
            Name = name;

            _value = new T[value.Length];
            Array.Copy(value, _value, value.Length);
        }
        #endregion </Constuctor>

        #region <Fields>
        protected T[] _value;
        #endregion </Fields>

        #region <Methods>

        //#region <String Returns>
        //public virtual string GetValueString()
        //{
        //    return _value.ToString();
        //}
        //#endregion </String Returns>

        #region <Value>
        public void SetValue(T newValue)
        {
            _value[0] = newValue;
        }

        public void SetValues(T[] newValue)
        {
            _value = new T[newValue.Length];
            Array.Copy(newValue, _value, newValue.Length);
        }

        public T GetValue()
        {
            return _value[0];
        }
        public override string GetValueStringAll()
        {
            if (_value == null)
                return string.Empty;

            string messageToReturn = string.Empty;
            for (int i = 0; i < _value.Length; ++i)
            {
                if (string.IsNullOrEmpty(messageToReturn))
                {
                    messageToReturn = _value[i].ToString();
                }
                else
                {
                    messageToReturn = string.Format("{0} {1}", messageToReturn, _value[i].ToString());
                }
            }

            return messageToReturn;
        }

        public override string GetValueString()
        {
            if (_value == null)
                return string.Empty;

            return _value[0].ToString();
        }
        public T GetTargetValue(int nTarget)
        {
            return _value[nTarget];
        }
        public override string GetTargetValueString(int nTarget)
        {
            return _value[nTarget].ToString();
        }
        public T[] GetValues()
        {
            return _value;
        }
        #endregion </Value>

        #region <Item Format>
        public void SetItemFormat(EN_ITEM_FORMAT format)
        {
            Format = format;
        }

        public EN_ITEM_FORMAT GetItemFormat()
        {
            return Format;
        }
        #endregion </Item Format>

        #endregion </Methods>
    }

    #region <DataType에 따른 상속>
    public class SemiObjectList : ObjectValue<long>
    {
        public SemiObjectList(long count)
            : base(EN_ITEM_FORMAT.LIST, "List", count)
        {
            //Count = count;
        }

        //public int Count { get; private set; }
    }

    public class SemiObjectAscii : ObjectValue<string>
    {
        public SemiObjectAscii(string name, string value)
            : base(EN_ITEM_FORMAT.ASCII, name, string.IsNullOrEmpty(value) ? string.Empty : value)
        {
        }
    }

    public class SemiObjectBinary : ObjectValue<byte>
    {
        public SemiObjectBinary(string name, params byte[] value)
            : base(EN_ITEM_FORMAT.BINARY, name, value)
        {
        }
    }

    public class SemiObjectBool : ObjectValue<bool>
    {
        public SemiObjectBool(string name, params bool[] value)
            : base(EN_ITEM_FORMAT.BOOL, name, value)
        {
        }
        public SemiObjectBool(string name, params byte[] value)
            : base(EN_ITEM_FORMAT.BOOL, name)
        {
            bool[] convertValue = new bool[value.Length];

            for (int i = 0; i < value.Length; i++)
            {
                convertValue[i] = value[i] == '1';
            }

            SetValues(convertValue);
        }
    }

    public class SemiObjectFloat4 : ObjectValue<float>
    {
        public SemiObjectFloat4(string name, params float[] value)
            : base(EN_ITEM_FORMAT.FLOAT4, name, value)
        {
        }
    }

    public class SemiObjectFloat8 : ObjectValue<double>
    {
        public SemiObjectFloat8(string name, params double[] value)
            : base(EN_ITEM_FORMAT.FLOAT8, name, value)
        {
        }
    }

    public class SemiObjectInt : ObjectValue<sbyte>
    {
        public SemiObjectInt(string name, params sbyte[] value)
            : base(EN_ITEM_FORMAT.INT, name, value)
        {
        }
    }

    public class SemiObjectInt2 : ObjectValue<short>
    {
        public SemiObjectInt2(string name, params short[] value)
            : base(EN_ITEM_FORMAT.INT2, name, value)
        {
        }
    }

    public class SemiObjectInt4 : ObjectValue<int>
    {
        public SemiObjectInt4(string name, params int[] value)
            : base(EN_ITEM_FORMAT.INT4, name, value)
        {
        }
    }

    public class SemiObjectInt8 : ObjectValue<long>
    {
        public SemiObjectInt8(string name, params long[] value)
            : base(EN_ITEM_FORMAT.INT8, name, value)
        {
        }
    }

    public class SemiObjectUInt : ObjectValue<byte>
    {
        public SemiObjectUInt(string name, params byte[] value)
            : base(EN_ITEM_FORMAT.UINT, name, value)
        {
        }
    }

    public class SemiObjectUInt2 : ObjectValue<ushort>
    {
        public SemiObjectUInt2(string name, params ushort[] value)
            : base(EN_ITEM_FORMAT.UINT2, name, value)
        {
        }
    }

    public class SemiObjectUInt4 : ObjectValue<uint>
    {
        public SemiObjectUInt4(string name, params uint[] value)
            : base(EN_ITEM_FORMAT.UINT4, name, value)
        {
        }
    }

    public class SemiObjectUInt8 : ObjectValue<ulong>
    {
        public SemiObjectUInt8(string name, params ulong[] value)
            : base(EN_ITEM_FORMAT.UINT8, name, value)
        {
        }
    }
    #endregion

    #endregion </SemiObject>

    public static class DefinesForClientToClientMessage
    {
        public static readonly string VALUE_MESSAGE_TYPE_SEND = "S";
        public static readonly string VALUE_MESSAGE_TYPE_ACK = "A";
    }

    public class WaferMapData
    {
        #region <Fields>
        private const string AttributeNameOfMaterialId = "MID";
        private const string AttributeNameOfIdType = "IDTYP";
        private const string AttributeNameOfMapFormatType = "MAPFT";
        private const string AttributeNameOfFlatNotchLocation = "FNLOC";
        private const string AttributeNameOfFilmFrameLocation = "FFROT";
        private const string AttributeNameOfOriginLocation = "ORLOC";
        private const string AttributeNameOfProcessAccess = "PRAXI";
        private const string AttributeNameOfBinCodeEquivalents = "BCEQU";
        private const string AttributeNameOfNullBinCode = "NULBC";
        private const string AttributeNameOfReferenceX = "RefX";
        private const string AttributeNameOfReferenceY = "RefY";
        private const string AttributeNameOfStartingX = "StartingX";
        private const string AttributeNameOfStartingY = "StartingY";
        private const string AttributeNameOfCountRow = "CountRow";
        private const string AttributeNameOfCountCol = "CountCol";
        private const string AttributeNameOfXAxisDieSize = "XDIES";
        private const string AttributeNameOfYAxisDieSize = "YDIES";
        private const string AttributeNameOfCountProcessDies = "CountProcessDies";
        private const string AttributeNameOfMapData = "MapData";
        #endregion </Fields>

        #region <Properties>
        public string WaferId { get; set; }
        public double Angle { get; set; }
        public int IndexOfRefX { get; set; }
        public int IndexOfRefY { get; set; }
        public int SizeOfDieX { get; set; }
        public int SizeOfDieY { get; set; }
        public int IndexOfStartingX { get; set; }
        public int IndexOfStartingY { get; set; }
        public int CountOfRow { get; set; }
        public int CountOfCol { get; set; }
        public int CountOfProcessDies { get; set; }
        public string NullBinCode { get; set; }
        public string MapData { get; set; }
        #endregion </Properties>

        public Dictionary<string, string> GetDataAll()
        {
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { AttributeNameOfMaterialId, WaferId },
                { AttributeNameOfFlatNotchLocation, Angle.ToString() },
                { AttributeNameOfReferenceX, IndexOfRefX.ToString() },
                { AttributeNameOfReferenceY, IndexOfRefY.ToString() },
                { AttributeNameOfStartingX, IndexOfStartingX.ToString() },
                { AttributeNameOfStartingY, IndexOfStartingY.ToString() },
                { AttributeNameOfCountRow, CountOfRow.ToString() },
                { AttributeNameOfCountCol, CountOfCol.ToString() },
                { AttributeNameOfXAxisDieSize, SizeOfDieX.ToString() },
                { AttributeNameOfYAxisDieSize, SizeOfDieY.ToString() },
                { AttributeNameOfCountProcessDies, CountOfProcessDies.ToString() },
                { AttributeNameOfNullBinCode, NullBinCode },
                { AttributeNameOfMapData, MapData }
            };

            return data;
        }
    }

    //public class TraceDataInfo
    //{
    //    #region <Constructors>
    //    public TraceDataInfo()
    //    {
    //        DataToUpdate = new ConcurrentDictionary<long, string>();
    //    }
    //    #endregion </Constructors>

    //    #region <Fields>
    //    private ReadOnlyDictionary<string, long> _variableInfo = null;      // Key : Variable Name, Value : Variable Id
    //    private readonly ConcurrentDictionary<long, string> DataToUpdate = null;        // Key : Variable Id, Value : Variable Value
    //    #endregion </Fields>

    //    #region <Properties>
    //    public uint Interval { get; set; }
    //    #endregion </Properties>

    //    #region <Methods>
    //    public void AddDataTable(Dictionary<string, long> variableInfo, uint interval)
    //    {
    //        _variableInfo = new ReadOnlyDictionary<string, long>(variableInfo);
    //        foreach (var item in _variableInfo)
    //        {
    //            DataToUpdate[item.Value] = string.Empty;
    //        }
    //        Interval = interval;
    //    }

    //    public void GetTraceDataValue(ref Dictionary<long, string> variableValues)
    //    {
    //        if (variableValues.Count != DataToUpdate.Count)
    //            variableValues = new Dictionary<long, string>();

    //        foreach (var item in DataToUpdate)
    //        {
    //            variableValues[item.Key] = item.Value;
    //        }
    //    }
    //    public void UpdateTraceData(Dictionary<long, string> variableValues)
    //    {
    //        foreach (var item in variableValues)
    //        {
    //            DataToUpdate[item.Key] = item.Value;
    //        }
    //    }

    //    public void UpdateTraceData(Dictionary<string, string> variableValues)
    //    {
    //        foreach (var item in variableValues)
    //        {
    //            if (_variableInfo.TryGetValue(item.Key, out long variableId))
    //            {
    //                DataToUpdate[variableId] = item.Value;
    //            }
    //        }
    //    }
    //    //public void UpdateTraceData(Dictionary<string, string> variableValues)
    //    //{
    //    //    SlimLock.EnterWriteLock();

    //    //    foreach (var item in variableValues)
    //    //    {
    //    //        if (_variableInfo.TryGetValue(item.Key, out long variableId))
    //    //        {
    //    //            DataToUpdate[variableId] = item.Value;
    //    //        }
    //    //    }

    //    //    SlimLock.ExitWriteLock();
    //    //}

    //    public bool GetValueById(ref Dictionary<long, string> dataToUpdate)
    //    {
    //        bool hasTraceData = true;

    //        if (_variableInfo == null || _variableInfo.Count <= 0 || DataToUpdate.Count <= 0)
    //        {
    //            hasTraceData = false;
    //        }
    //        else
    //        {
    //            dataToUpdate.Clear();
    //            foreach (var item in DataToUpdate)
    //            {
    //                dataToUpdate[item.Key] = item.Value;
    //            }
    //        }

    //        return hasTraceData;
    //    }
    //    public bool GetValueByName(ref Dictionary<string, string> variableInfo)
    //    {
    //        bool hasTraceData = true;

    //        if (_variableInfo == null || _variableInfo.Count <= 0 || DataToUpdate.Count <= 0)
    //        {
    //            hasTraceData = false;
    //        }
    //        else
    //        {
    //            variableInfo.Clear();
    //            foreach (var item in _variableInfo)
    //            {
    //                if (DataToUpdate.ContainsKey(item.Value))
    //                {
    //                    variableInfo[item.Key] = DataToUpdate[item.Value];
    //                }
    //            }
    //        }

    //        return hasTraceData;
    //    }
    //    #endregion </Methods>
    //}

    public class QueuedScenarioInfo
    {
        public Enum Scenario { get; set; }
        public Dictionary<string, string> ScenarioParams { get; set; }
        public Dictionary<string, string> AdditionalParams { get; set; }
    }

    public class StatusVariable
    {
        public StatusVariable(long id, string name)
        {
            Id = id;
            Name = name;
        }
        public long Id { get; set; }
        public string Name { get; set; }
        public EN_ITEM_FORMAT ItemFormat { get; set; }
    }

    public class CollectionEvent
    {
        public CollectionEvent(long id, Dictionary<long, StatusVariable> variables)
        {
            Id = id;

            Variables = new Dictionary<long, StatusVariable>(variables);
        }
        public bool CustomScenario { get; set; }
        public long Id { get; set; }
        public Dictionary<long, StatusVariable> Variables { get; set; }
        public List<long> VariableIds
        {
            get
            {
                if (Variables == null)
                    return null;

                return Variables.Keys.ToList();
            }
        }
    }

    public class EquipmentConstant
    {
        public EquipmentConstant(long id, string name)
        {
            Id = id;
            Name = name;
            Value = string.Empty;
        }
        public long Id { get; set; }
        public string Name { get; set; }
        public EN_ITEM_FORMAT ItemFormat { get; private set; }
        public string Value { get; set; }
        public void SetRange<T>(T min, T max)
        {

        }
    }
    #endregion </Class>

    #region <Enum>
    public enum BaseScenario
    {
        ParameterIsChanged,
    }
    public enum EN_GEM_ALARM_STATE
    {
        CLEARED = 0,
        OCCURED = 1,
    }
    public enum EN_MESSAGE_RESULT
    {
        NG = 0,
        OK = 1,
    }
    public enum EN_SCENARIO_RESULT
    {
        PROCEED,
        COMPLETED,
        ERROR,
        TIMEOUT_ERROR,
    }
    public enum EN_COMM_STATE
    {
        DISABLED = 1,
        WAIT_CR_FROM_HOST,
        WAIT_DELAY,
        WAIT_CRA,
        COMMUNICATING,
    }
    public enum EN_SCENARIO_SEQ
    {
        INIT                = 0,
        SEND_EVENT          = 100,
        WAIT_FOR_PERMISSION = 200,
        AFTER_PERMISSION    = 300,
        FINISH,
    }
    public enum EN_SETTING_CONTROL_STATE
    {
        OFFLINE = 1,
        //HOST_OFFLINE = 3,       // Host Offline은 설정할 수 없다.
        LOCAL = 4,
        REMOTE = 5,
    }
    public enum EN_CONTROL_STATE
    {
        OFFLINE = 1,
        ATTEMP_ONLINE = 2,
        HOST_OFFLINE = 3,
        LOCAL = 4,
        REMOTE = 5
    }
    public enum EN_ITEM_FORMAT
    {
        LIST = 0,
        ASCII,
        BINARY,
        BOOL,
        UINT,
        UINT2,
        UINT4,
        UINT8,
        INT,
        INT2,
        INT4,
        INT8,
        FLOAT4,
        FLOAT8,
    }
    public enum EN_XEIC_DEVICE_SIGNAL_ACK
    {
        NOT_COMPLETE,
        OK,
        NG,
    }
    public enum EN_REMOTE_COMMAND_RESULT
    {
        INIT,
        OK,
        ERROR
    }
    public enum EN_SCENARIO_PERMISSION_RESULT
    {
        OK,
        PROCEED,
        ERROR
    }
    public enum EN_PPGRANT
    {
        OK = 0,
        ALREADY_HAVE,
        NO_SPACE,
        INVALID_PPID,
        BUSY,
    }
    public enum EN_ACK7
    {
        OK = 0,             // Accepted
        PERMISSION = 1,     // Permission not granted
        LENGTH = 2,         // Length error
        OVERFLOW = 3,       // Matrix overflow
        NOT_FOUND = 4,      // PPID not found
        UNSUPPORTED = 5,    // Mode unsupported
        PERFORM_LATER = 6   // Command will be performed with completion signaled later
    }
    public enum EN_OPCALL_LEVEL
    {
        INFO = 1,
        WARNING = 2,
        ERROR = 3,
        DOWN = 4,
        ETC = 5,
    }
    public enum EAC                     // Equipment Acknowledge Code
    {
        OK = 0,
        CONSTANTS_DOES_NOT_EXIST,       // 1 - one or more constants does not exist
        BUSY,                           // 2 - busy
        OUT_OF_RANGE,                   // 3 - one or more values out of range
    }
    public enum EN_CPACK_TYPE
    { 
        OK = 0,
        UNKNOWN_CPNAME = 1,
        ILLEGAL_VALUE_FOR_CPVAL = 2,
        ILLEGAL_FORMAT_FOR_CPVAL = 3,
    }
    public enum CarrierActionAck
    {
        Ok = 0,
        InvalidCommand = 1,
        CannotPerformNow = 2,
        InvalidDataOrArgument = 3,
        InitiatedForAsynchronousCompletion = 4,
        RejectedByInvalidState = 5,
        CommandPerformedWithErrors = 6
    }
    public enum MapTypes : byte
    {
        WaferId = 0,
        WaferCassetteId,
        FilmFrameId
    }
    public enum MapDataFormatTypes : byte
    {
        RowFormat = 0,
        ArrayFormat,
        CoordinateFormat
    }
    public enum OriginLocationTypes : byte
    {
        CenterDieOfWafer = 0,
        UpperRight,
        UpperLeft,
        LowerLeft,
        LowerRight
    }
    public enum ScenarioTypes
    {
        SendingEventScenario,
        ClientToClientCommunicationScenario,
        Custom
    }
    #endregion </Enum>

    #region <Events>
    // Connections
    public delegate void deleHandlerVoid();

    // Terminal Message
    public delegate void deleHandlerString(string message);

    // RemoteCommand
    public delegate bool deleRemoteCommand(string rcmdName, string[] cpNames, string[] cpValues, ref long[] results);

    // Received Signal
    public delegate bool deleRecvClientToClientMessage(string device, string messageName, string sendingType, string scenarioName, string[] contentNames, string[] messages, EN_MESSAGE_RESULT result);

    // Up/Downloading Recipe
    public delegate bool deleReqRecipeControl(string recipeName);

    // Variables
    public delegate void deleChangeEquipmentParameters(string[] ecNames, string[] values);


    public delegate bool deleDisplayOperatorCallForm(EN_OPCALL_LEVEL level, string operatorId, bool usingBuzzer, string message);


    public delegate bool deleSecsMessageReceived(UserDefinedSecsMessage receivedSecsMessage, ref UserDefinedSecsMessage secsMessageToSend);


    public delegate EN_PPGRANT deleRecipeControlGrant(string recipeName);

    #region <Recipe Control>
    public delegate bool deleReqUPloadingUnformattedRecipeControl(string recipeName, ref string recipeFullPath);
    public delegate EN_ACK7 deleReqDownloadingUnformattedRecipeControl(string recipeName, string recipeFullPath);

    //24.09.20 by wdw [ADD] Scenerio S7F4 Ack 확인 
    public delegate void deleReqUPloadingUnformattedRecipeAck(string recipeName, EN_ACK7 Ack);

    // RecipeName, <CCode, PParam>
    public delegate bool deleReqUploadingFormattedRecipe(string recipeName, out Dictionary<string, SemiObject[]> recipeBodies);

    public delegate bool deleReqDownloadingFormattedRecipe(string recipeName, Dictionary<string, string[]> recipeBodies);

    public delegate void deleRecipeFileIsDeleted(string[] recipeFiles);
    #endregion </Recipe Control>

    #endregion </Events>
}
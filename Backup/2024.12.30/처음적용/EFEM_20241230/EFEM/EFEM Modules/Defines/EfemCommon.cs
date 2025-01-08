using System;
using System.Threading;
using System.Linq;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Xml.Linq;
using System.Xml;
using System.Xml.Schema;
using System.IO.Compression;
using System.Threading.Tasks;

namespace EFEM.Defines.Common
{
    #region <Class>
    public class EFEMLogger
    {
        private EFEMLogger()
        {
            _logger = AsyncLoggerForEFEM.Instance;
            TypeOfLogger = LogTypes.Temp;

            FilePath = string.Format(@"EFEM\Main\Main");
        }
        private static EFEMLogger _instance = null;
        private readonly string FilePath = null;
        private static AsyncLoggerForEFEM _logger = null;
        private readonly LogTypes TypeOfLogger;
        public static EFEMLogger Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new EFEMLogger();

                return _instance;
            }
        }

        public void WriteLog(string messageToLog)
        {
            _logger.EnqueueLog(TypeOfLogger, messageToLog);
        }
    }

    public class ModuleLogger
    {
        public ModuleLogger(LogTypes typeOfLog, string filePath)
        {
            _logger = AsyncLoggerForEFEM.Instance;
            TypeOfLog = typeOfLog;
            
            _logger.AddLogTypes(typeOfLog, filePath);
            //FilePath = string.Format(@"EFEM\{0}\Log", name);
        }

        private readonly LogTypes TypeOfLog;
        protected LogTitleTypes _logType;

        //private readonly string FilePath = null;
        private static AsyncLoggerForEFEM _logger = null;
        private string _messageToWrite = string.Empty;

        protected void WriteLog(string message)
        {
            _messageToWrite = string.Format("[{0}] {1}", _logType.ToString(), message);
            _logger.EnqueueLog(TypeOfLog, _messageToWrite);
        }
    }


    //public class EFEMLogger
    //{
    //    private EFEMLogger()
    //    {
    //        _logger = FrameOfSystem3.Log.LogWriter.GetInstance();
    //        FilePath = string.Format(@"EFEM\Main\Main");
    //    }
    //    private static EFEMLogger _instance = null;
    //    private readonly string FilePath = null;
    //    private static FrameOfSystem3.Log.LogWriter _logger = null;

    //    public static EFEMLogger Instance
    //    {
    //        get
    //        {
    //            if (_instance == null)
    //                _instance = new EFEMLogger();

    //            return _instance;
    //        }
    //    }

    //    public void WriteLog(string messageToLog)
    //    {
    //        _logger.MakeLog(FilePath, messageToLog);
    //    }
    //}

    //public class ModuleLogger
    //{
    //    public ModuleLogger(string name)
    //    {
    //        _logger = FrameOfSystem3.Log.LogWriter.GetInstance();
    //        FilePath = string.Format(@"EFEM\{0}\Log", name);
    //    }

    //    protected LogTitleTypes _logType;

    //    private readonly string FilePath = null;
    //    private static FrameOfSystem3.Log.LogWriter _logger = null;
    //    private string _messageToWrite = string.Empty;

    //    protected void WriteLog(string message)
    //    {
    //        _messageToWrite = string.Format("[{0}] {1}", _logType.ToString(), message);
    //        _logger.MakeLog(FilePath, _messageToWrite);
    //    }
    //}

    public class CommandResults
    {
        public CommandResults(string actionName,
            CommandResult result,
            string description = null)
        {
            ActionName = actionName;
            CommandResult = result;
            
            if (description == null)
                Description = string.Empty;
            else
                Description = description;
        }

        public string ActionName { get; set; }
        public CommandResult CommandResult { get; set; }
        public string Description { get; set; }
        public int AlarmCode { get; set; }
    }

    //public class LocationName
    //{
    //    #region <Constructors>
    //    private LocationName()
    //    {
    //        _locationNames = (string[])Enum.GetValues(GetLocationType());
    //    }
    //    #endregion </Constructors>

    //    #region <Fields>
    //    private static LocationName _inatance = null;
    //    private readonly string[] _locationNames = null;
    //    #endregion </Fields>

    //    #region <Properties>
    //    public static LocationName Instance
    //    {
    //        get
    //        {
    //            if (_inatance == null)
    //                _inatance = new LocationName();

    //            return _inatance;
    //        }
    //    }

    //    public string[] LocationNames
    //    {
    //        get
    //        {
    //            return _locationNames;
    //        }
    //    }
    //    #endregion </Properties>

    //    #region <Methods>
    //    private Type GetLocationType()
    //    {
    //        switch (FrameOfSystem3.AppConfig.AppConfigManager.Instance.ProcessType)
    //        {
    //            case Define.DefineEnumProject.AppConfig.EN_PROCESS_TYPE.BIN_SORTER:
    //                return typeof(PWA500BINLocations);
    //        }

    //        return null;
    //    }
    //    #endregion </Methods>
    //}
    public class XmlFileStorage
    {
        #region <Constructors>
        public XmlFileStorage(string elementName)
        {
            RootName = elementName;
        }
        #endregion </Constructors>

        #region <Fields>
        private const string ElementName = "Item";
        private const string AttributeKey = "Key";
        private const string AttributeValue = "Value";
        private readonly string RootName;

        #endregion </Fields>

        #region <Properties>
        #endregion </Properties>

        #region <Methods>
        #endregion </Methods>


        public Dictionary<string, string> LoadDictionaryFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return new Dictionary<string, string>(); // 파일이 없으면 빈 Dictionary 반환
            }

            XElement xml = XElement.Load(filePath);
            Dictionary<string, string> dictionary = new Dictionary<string, string>();

            foreach (var item in xml.Elements(ElementName))
            {
                if (item.Attribute(AttributeKey) == null)
                    continue;
                    
                string key = item.Attribute(AttributeKey).Value;
                string value = string.Empty;
                if (item.Attribute(AttributeValue) != null)
                {
                    value = item.Attribute(AttributeValue).Value;
                }
                dictionary[key] = value;
            }

            return dictionary;
        }
        public void SaveChangedItemsToFile(Dictionary<string, string> dictionary, string filePath)
        {
            XElement xml = new XElement(RootName);

            if (File.Exists(filePath))
            {
                xml = XElement.Load(filePath);
            }

            foreach (var kvp in dictionary)
            {
                var item = xml.Elements(ElementName).FirstOrDefault(e => e.Attribute(AttributeKey).Value == kvp.Key);
                if (item != null && item.Attribute(AttributeValue) != null)
                {
                    // 값이 변경된 경우
                    if (item.Attribute(AttributeValue).Value != kvp.Value)
                    {
                        item.SetAttributeValue(AttributeValue, kvp.Value); // 값 업데이트
                    }
                }
                else
                {
                    // 신규 추가
                    xml.Add(new XElement(ElementName,
                        new XAttribute(AttributeKey, kvp.Key),
                        new XAttribute(AttributeValue, kvp.Value)));
                }
            }

            xml.Save(filePath);
        }

    }

    public class AsyncLoggerForEFEM
    {
        #region <Constructors>
        private AsyncLoggerForEFEM()
        {
            BasePath = string.Format(@"{0}\EFEM", Define.DefineConstant.FilePath.FILEPATH_LOG);

            LogDirectories = new Dictionary<LogTypes, string>();
            LogToWrite = new ConcurrentDictionary<LogTypes, ConcurrentQueue<Tuple<DateTime, string>>>();
            StreamWriters = new Dictionary<LogTypes, StreamWriter>();
            LastBackupDate = new Dictionary<LogTypes, DateTime>();

            // 로그 파일 경로가 존재하지 않으면 생성
            if (false == Directory.Exists(BasePath))
            {
                Directory.CreateDirectory(BasePath);
            }
            
            LogToWrite[LogTypes.Temp] = new ConcurrentQueue<Tuple<DateTime, string>>();
            LogDirectories[LogTypes.Temp] = BasePath;
            LastBackupDate[LogTypes.Temp] = new DateTime();
            //AddLogTypes(LogTypes.Temp, BasePath);

            WriteLogAsync = ProcessLogsAsync();

            //Task.Run(() => ProcessLogsAsync());
        }
        #endregion </Constructors>

        #region <Fields>
        private readonly string BasePath;

        private bool _exiting = false;
        private string _temporaryPath = string.Empty;
        private string _temporaryDir = string.Empty;

        private static AsyncLoggerForEFEM _instance = null;

        private readonly Task WriteLogAsync = null;

        private const int BackUpHour = 22;
        private readonly Dictionary<LogTypes, StreamWriter> StreamWriters;
        private readonly Dictionary<LogTypes, string> LogDirectories;
        private readonly Dictionary<LogTypes, DateTime> LastBackupDate;
        private readonly ConcurrentDictionary<LogTypes, ConcurrentQueue<Tuple<DateTime, string>>> LogToWrite;
        #endregion </Fields>

        #region <Properties>
        public static AsyncLoggerForEFEM Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new AsyncLoggerForEFEM();

                return _instance;
            }
        }
        #endregion </Properties>

        #region <Methods>

        #region <External>
        public void AddLogTypes(LogTypes typeOfLog, string filePath)
        {
            if (false == LogToWrite.ContainsKey(typeOfLog))
            {
                LogToWrite[typeOfLog] = new ConcurrentQueue<Tuple<DateTime, string>>();
            }

            LogDirectories[typeOfLog] = string.Format(@"{0}\{1}", BasePath, filePath);
            LastBackupDate[typeOfLog] = new DateTime();
            
            if (false == Directory.Exists(LogDirectories[typeOfLog]))
                Directory.CreateDirectory(LogDirectories[typeOfLog]);
        }
        public void EnqueueLog(LogTypes typeOfLog, string message)
        {
            var logEntry = Tuple.Create(DateTime.Now, message);

            LogToWrite[typeOfLog].Enqueue(logEntry);
        }
        public async void Exit()
        {
            _exiting = true;

            await WaitForCompletion();

            CloseStreamWritersAll();
        }
        #endregion </External>

        #region <Internal>
        private void CreateLogFilePath(LogTypes typeOfLog, DateTime date, ref string createdPath)
        {
            if (false == LogDirectories.ContainsKey(typeOfLog))            
            {
                typeOfLog = LogTypes.Temp;
            }

            _temporaryDir = LogDirectories[typeOfLog];
            if (false == Directory.Exists(_temporaryDir))
                Directory.CreateDirectory(_temporaryDir);

            createdPath = string.Format(@"{0}\{1:0000}{2:00}{3:00}.txt", _temporaryDir, date.Year, date.Month, date.Day);
        }

        private void CreateStreamWriter(LogTypes typeOfLog, string filePath)
        {
            StreamWriters[typeOfLog] = new StreamWriter(filePath, true) { AutoFlush = true };            
        }
        private bool IsStreamWriterClosed(LogTypes typeOfLog)
        {
            if (false == StreamWriters.ContainsKey(typeOfLog))
                return true;

            return StreamWriters[typeOfLog] == null;
        }
        private void CloseStreamWritersAll()
        {
            foreach (var item in StreamWriters)
            {
                if (item.Value != null)
                {
                    item.Value.Close();
                    item.Value.Dispose();
                    
                    StreamWriters[item.Key] = null;
                }
            }           
        }
        private void CloseStreamWriter(LogTypes typeOfLog)
        {
            if (StreamWriters.ContainsKey(typeOfLog))
            {
                if (StreamWriters[typeOfLog] != null)
                {
                    StreamWriters[typeOfLog].Close();
                    StreamWriters[typeOfLog].Dispose();
                    StreamWriters[typeOfLog] = null;
                }
            }
        }
        private async Task ProcessLogsAsync()
        {
            while (true)
            {
                await Task.Delay(1);

                foreach (var item in LogToWrite)
                {
                    if (item.Value.Count > 0)
                    {
                        if (item.Value.TryDequeue(out Tuple<DateTime, string> logEntry))
                        {
                            WriteLog(item.Key, logEntry.Item1, logEntry.Item2);
                        }
                    }
                    else
                    {
                        CloseStreamWriter(item.Key);

                        // 이전 날짜 로그는 압축 및 제거
                        CleanUpLogsAsync(item.Key);
                    }
                }

                if (_exiting)
                    return;
            }
        }
        private void WriteLineToFile(LogTypes typeOfLog, string path, string message)
        {
            if (IsStreamWriterClosed(typeOfLog))
            {
                CreateStreamWriter(typeOfLog, path);
            }

            StreamWriters[typeOfLog].WriteLine(message);
        }
        private void WriteLog(LogTypes typeOfLog, DateTime logDate, string message)
        {
            try
            {
                // 현재 날짜와 로그 날짜 비교
                CreateLogFilePath(typeOfLog, logDate, ref _temporaryPath);
                if (false == File.Exists(_temporaryPath))
                {
                    // 기존 StreamWriter 닫기
                    CloseStreamWriter(typeOfLog);
                }                

                var logEntry = String.Format("[{0:d2}:{1:d2}:{2:d2}.{3:d3}] {4}",
                    logDate.Hour,
                    logDate.Minute,
                    logDate.Second,
                    logDate.Millisecond,
                    message);

                WriteLineToFile(typeOfLog, _temporaryPath, logEntry);
            }
            catch (Exception ex)
            {
                CreateLogFilePath(LogTypes.Temp, logDate, ref _temporaryPath);
                WriteLineToFile(LogTypes.Temp, _temporaryPath, $"##### { ex.Message } : { ex.StackTrace } #####");                
            }
        }

        private void CleanUpLogsAsync(LogTypes typeOfLog)
        {
            if (false == LogDirectories.ContainsKey(typeOfLog))
                return;

            DateTime today = DateTime.Today;
            // 백업한 날짜와 현재 날짜가 같거나, 현재 시간이 타겟 시간이 아니면 넘긴다.
            if (LastBackupDate[typeOfLog].Date.Equals(today) ||
                DateTime.Now.Hour != BackUpHour)
                return;

            LastBackupDate[typeOfLog] = DateTime.Now;

            try
            {
                string tempFolderPath = Path.Combine(LogDirectories[typeOfLog], "Temp");

                string zipFileName = Path.Combine(LogDirectories[typeOfLog], $"{typeOfLog.ToString()}.zip");
                var logFiles = Directory.GetFiles(LogDirectories[typeOfLog]);
                for (int i = 0; i < logFiles.Length; ++i)
                {
                    string fileNameOnly = Path.GetFileNameWithoutExtension(logFiles[i]);
                    if (DateTime.TryParseExact(fileNameOnly, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
                    {
                        if (parsedDate.Date < today)
                        {
                            if (false == Directory.Exists(tempFolderPath))
                                Directory.CreateDirectory(tempFolderPath);

                            string fileName = Path.GetFileName(logFiles[i]);
                            string tempFilePath = Path.Combine(tempFolderPath, fileName);
                            File.Copy(logFiles[i], tempFilePath, true);

                            if (File.Exists(zipFileName))
                            {
                                using (var zipArchive = ZipFile.Open(zipFileName, ZipArchiveMode.Update))
                                {
                                    ZipArchiveEntry existingEntry = zipArchive.GetEntry(fileName);
                                    if (existingEntry != null)
                                    {
                                        // 기존 엔트리가 존재하면 삭제
                                        existingEntry.Delete();
                                    }

                                    zipArchive.CreateEntryFromFile(tempFilePath, fileName);
                                }
                            }
                            else
                            {
                                using (var zipArchive = ZipFile.Open(zipFileName, ZipArchiveMode.Create))
                                {
                                    zipArchive.CreateEntryFromFile(tempFilePath, fileName);
                                }
                            }

                            // 원본 파일 삭제
                            try
                            {
                                File.Delete(logFiles[i]);
                            }
                            catch (Exception ex)
                            {
                                EnqueueLog(LogTypes.Temp, string.Format("### File Delete Failed : {0}, {1}", ex.Message, ex.StackTrace));
                            }
                        }
                    }
                }

                if (Directory.Exists(tempFolderPath))
                {
                    Directory.Delete(tempFolderPath, true);
                }
            }
            catch (Exception ex)
            {
                EnqueueLog(LogTypes.Temp, string.Format("### Backup Failed : {0}, {1}", ex.Message, ex.StackTrace));
            }
        }

        private async Task WaitForCompletion()
        {
            while (false == WriteLogAsync.IsCompleted)
            {
                await Task.Delay(1);
            }
        }
        #endregion </Internal>
        #endregion </Methods>
    }

    #endregion </Class>

    #region <Enumerations>
    public enum CommandResult
    {
        Proceed,
        Completed,
        Skipped,
        Timeout,
        Error,
        Invalid
    }
    public enum CommunicationResult
    {
        Ack,
        Nack,
        Proceed,
        Error,
    }
    public enum LogTypes
    {
        Temp,
        LoadPort1,
        LoadPort2,
        LoadPort3,
        LoadPort4,
        LoadPort5,
        LoadPort6,
        AtmRobot,
        ProcessModule,
        SecsGem
    }
    public enum LogTitleTypes
    {
        OPER,
        SEND,
        RECV,
    }

    public enum MaterialFormat
    {
        Unknown = 0,
        Wafers = 1,     // 1 - wafers
        Cassettes,      // 2 - cassettes
        Die,            // 3 - die
        Boats,          // 4 - boats
        Ingots,         // 5 - ingots
        LeadFrames,     // 6 - leadframes
        Lots,           // 7 - lots
        Magazines,      // 8 - magazines
        Packages,       // 9 - packages
        Plates,         // 10 - plates
        Tubes,          // 11 - tubes
        WaterFrames,    // 12 - waterframes
        Carrier,        // 13 - carrier (FOUP, SMIF pod, cassette)
        Substrate,      // 14 - substrate
    }
    #region <Location Names>
    public enum PWA500BINLocations
    {
        ProcessModuleCoreInput,
        ProcessModuleSortingInput,
        ProcessModuleCoreOutput,
        ProcessModuleSortingOutput,
    }
    #endregion </Location Names>

    #endregion </Enumerations>
}

//namespace SerializableDictionary
//{
//    [XmlRoot("dictionary")]
//    public class SerializableDictionary<TKey, TValue>
//        : Dictionary<TKey, TValue>, IXmlSerializable
//    {
//        #region <IXmlSerializable Members>
//        public System.Xml.Schema.XmlSchema GetSchema()
//        {
//            return null;
//        }

//        public void ReadXml(System.Xml.XmlReader reader)
//        {
//            XmlSerializer keySerializer = new XmlSerializer(typeof(TKey));
//            XmlSerializer valueSerializer = new XmlSerializer(typeof(TValue));

//            bool wasEmpty = reader.IsEmptyElement;
//            reader.Read();

//            if (wasEmpty)
//                return;

//            while (reader.NodeType != System.Xml.XmlNodeType.EndElement)
//            {
//                reader.ReadStartElement("item");

//                reader.ReadStartElement("key");
//                TKey key = (TKey)keySerializer.Deserialize(reader);
//                reader.ReadEndElement();

//                reader.ReadStartElement("value");
//                TValue value = (TValue)valueSerializer.Deserialize(reader);
//                reader.ReadEndElement();

//                this.Add(key, value);

//                reader.ReadEndElement();
//                reader.MoveToContent();
//            }
//            reader.ReadEndElement();
//        }

//        public void WriteXml(System.Xml.XmlWriter writer)
//        {
//            XmlSerializer keySerializer = new XmlSerializer(typeof(TKey));
//            XmlSerializer valueSerializer = new XmlSerializer(typeof(TValue));

//            foreach (TKey key in this.Keys)
//            {
//                writer.WriteStartElement("item");

//                writer.WriteStartElement("key");
//                keySerializer.Serialize(writer, key);
//                writer.WriteEndElement();

//                writer.WriteStartElement("value");
//                TValue value = this[key];
//                valueSerializer.Serialize(writer, value);
//                writer.WriteEndElement();

//                writer.WriteEndElement();
//            }
//        }
//        #endregion </IXmlSerializable Members>
//    }
//}

//namespace SerializableDictionary
//{
//    public class MyKeyValuePair<TKey, TValue>
//    {
//        public TKey Key { get; set; }
//        public TValue Value { get; set; }
//    }

//    public class SerializableDictionary<TKey, TValue>
//    {
//        public MyKeyValuePair<TKey, TValue>[] NewDictionary { get; set; }

//        public void UpdateKeyValues(Dictionary<TKey, TValue> dictionary)
//        {
//            NewDictionary = dictionary.Select(kvp => new MyKeyValuePair<TKey, TValue> { Key = kvp.Key, Value = kvp.Value }).ToArray();
//        }
//    }
//}

//namespace SerializableDictionary
//{
//    [XmlRoot("dictionary")]
//    public class SerializableDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IXmlSerializable
//    {
//        #region <Fields>
//        #endregion </Fields>
//        private readonly Dictionary<TKey, TValue> MyDictionary = new Dictionary<TKey, TValue>();

//        #region <Properties>
//        public ICollection<TValue> Values => MyDictionary.Values;

//        public TValue this[TKey key]
//        {
//            get => MyDictionary[key];
//            set => MyDictionary[key] = value;
//        }
//        public ICollection<TKey> Keys => MyDictionary.Keys;
//        public int Count => MyDictionary.Count;
//        public bool IsReadOnly => ((ICollection<KeyValuePair<TKey, TValue>>)MyDictionary).IsReadOnly;
//        #endregion </Properties>

//        #region <Methods>
//        public void Add(TKey key, TValue value)
//        {
//            MyDictionary.Add(key, value);
//        }
//        public void Add(KeyValuePair<TKey, TValue> item)
//        {
//            MyDictionary.Add(item.Key, item.Value);
//        }
//        public bool Remove(TKey key)
//        {
//            return MyDictionary.Remove(key);
//        }
//        public bool ContainsKey(TKey key)
//        {
//            return MyDictionary.ContainsKey(key);
//        }
//        public bool TryGetValue(TKey key, out TValue value)
//        {
//            return MyDictionary.TryGetValue(key, out value);
//        }
//        public void Clear()
//        {
//            MyDictionary.Clear();
//        }
//        public bool Contains(KeyValuePair<TKey, TValue> item)
//        {
//            return MyDictionary.Contains(item);
//        }
//        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
//        {
//            ((ICollection<KeyValuePair<TKey, TValue>>)MyDictionary).CopyTo(array, arrayIndex);
//        }
//        public bool Remove(KeyValuePair<TKey, TValue> item)
//        {
//            return MyDictionary.Remove(item.Key);
//        }

//        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
//        {
//            return MyDictionary.GetEnumerator();
//        }

//        IEnumerator IEnumerable.GetEnumerator()
//        {
//            return GetEnumerator();
//        }

//        public XmlSchema GetSchema()
//        {
//            return null;
//        }

//        public void ReadXml(XmlReader reader)
//        {
//            if (reader.IsEmptyElement)
//            {
//                reader.ReadStartElement();
//                return;
//            }

//            reader.ReadStartElement();
//            while (reader.NodeType != XmlNodeType.EndElement)
//            {
//                reader.ReadStartElement("item");
//                TKey key = (TKey)new XmlSerializer(typeof(TKey)).Deserialize(reader);
//                TValue value = (TValue)new XmlSerializer(typeof(TValue)).Deserialize(reader);
//                MyDictionary.Add(key, value);
//                reader.ReadEndElement();
//                reader.MoveToContent();
//            }
//            reader.ReadEndElement();
//        }

//        public void WriteXml(XmlWriter writer)
//        {
//            foreach (var keyValuePair in MyDictionary)
//            {
//                writer.WriteStartElement("item");
//                new XmlSerializer(typeof(TKey)).Serialize(writer, keyValuePair.Key);
//                new XmlSerializer(typeof(TValue)).Serialize(writer, keyValuePair.Value);
//                writer.WriteEndElement();
//            }
//        }
//        #endregion </Methods>
//    }

//}

 namespace Serializabler
{
    [Serializable]
    public class SerializableDictionary
    {
        [XmlElement("Key")]
        public string Key { get; set; }

        [XmlElement("Value")]
        public string Value { get; set; }
    }
}
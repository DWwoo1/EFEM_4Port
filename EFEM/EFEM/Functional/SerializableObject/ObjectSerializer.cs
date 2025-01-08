using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Xml.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;

namespace FrameOfSystem3.Functional.SerializableObject
{
    public enum SerializableType
    {
        XML,
        Binary,
    }

    abstract class ObjectSerializer// <TObject> where TObject : class
    {
        protected ObjectSerializer(string filePath, SerializableType serializableType)
        {
            _serializableType = serializableType;

            FILEPATH = filePath;
            FILE_EXTENSION = _serializableType == SerializableType.XML ? ".xml" : ".bin";
        }

        readonly string FILEPATH;
        readonly string FILE_EXTENSION;
        readonly SerializableType _serializableType;
        ConcurrentQueue<ObjectForAsyncSerialization> _storagesNeedSave = new ConcurrentQueue<ObjectForAsyncSerialization>();

        public bool Load<TObject>(string name, ref TObject deserializedObject) where TObject : class
        {
            string fullFilename = Path.Combine(FILEPATH, name + FILE_EXTENSION);

            if (false == DeserializeFromFile(SerializableType.Binary, fullFilename, ref deserializedObject))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public bool Save<TObject>(string name, TObject serializingObject, bool isAsyncMode = false) where TObject : class
        {
            string fullFilename = string.Empty;
            MemoryStream memoryStreamOfMap = null;
            ObjectForAsyncSerialization asyncObject = null;

            if (isAsyncMode)
            {
                try
                {
                    if (false == DeepCopySerializableObjectToMemoryStream(serializingObject, ref memoryStreamOfMap))
                    {
                        if (memoryStreamOfMap != null)
                        {
                            memoryStreamOfMap.Dispose();
                            memoryStreamOfMap = null;
                        }

                        return false;
                    }
                    asyncObject = new ObjectForAsyncSerialization(name, memoryStreamOfMap);
                    _storagesNeedSave.Enqueue(asyncObject);
                }
                catch
                {
                    if (memoryStreamOfMap != null)
                    {
                        memoryStreamOfMap.Dispose();
                        memoryStreamOfMap = null;
                    }
                    if (asyncObject != null)
                    {
                        asyncObject.Dispose();
                        asyncObject = null;
                    }
                    return false;
                }

                return true;
            }

            fullFilename = Path.Combine(FILEPATH, name + FILE_EXTENSION);

            return SerializeToFile(SerializableType.Binary, fullFilename, serializingObject);
        }

        public void AsyncSave<TObject>()
        {
            string fullFilename = string.Empty;
            ObjectForAsyncSerialization asyncObject = null;

            while (_storagesNeedSave.TryDequeue(out asyncObject))
            {
                try
                {
                    //TObject serializingObject = null;
                    TObject serializingObject = default(TObject);

                    fullFilename = Path.Combine(FILEPATH, asyncObject.ObjectName + FILE_EXTENSION);

                    if (GetSerializableObjectFromMemoryStream(asyncObject.Memory, ref serializingObject))
                    {
                        SerializeToFile(SerializableType.Binary, fullFilename, serializingObject);
                    }
                }
                finally
                {
                    if (asyncObject != null)
                    {
                        asyncObject.Dispose();
                        asyncObject = null;
                    }
                }
            }
        }

        protected static string GetFileExtensionBySerializableType(SerializableType type)
        {
            switch (type)
            {
                case SerializableType.Binary:
                    return ".bin";
                case SerializableType.XML:
                default:
                    return ".xml";
            }
        }

        static bool SerializeToFile<T>(SerializableType storageType, string fullFilename, T targetObject)
        {
            try
            {
                DirectoryInfo dirInfo = new DirectoryInfo(Path.GetDirectoryName(fullFilename));
                if (dirInfo.Exists == false)
                {
                    dirInfo.Create();
                }

                using (FileStream fs = new FileStream(fullFilename, FileMode.Create, FileAccess.Write))
                {
                    switch (storageType)
                    {
                        case SerializableType.Binary:
                            BinaryFormatter binaryFormatter = new BinaryFormatter();
                            binaryFormatter.Serialize(fs, targetObject);
                            break;
                        case SerializableType.XML:
                        default:
                            XmlSerializer serializer = new XmlSerializer(targetObject.GetType());
                            serializer.Serialize(fs, targetObject);
                            break;
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        static bool DeserializeFromFile<T>(SerializableType storageType, string fullFilename, ref T targetObject) where T : class
        {
            bool success = false;
            try
            {
                if (false == File.Exists(fullFilename))
                {
                    return success;
                }

                using (FileStream fs = new FileStream(fullFilename, FileMode.Open, FileAccess.Read))
                {
                    switch (storageType)
                    {
                        case SerializableType.Binary:
                            BinaryFormatter binaryFormatter = new BinaryFormatter();
                            targetObject = binaryFormatter.Deserialize(fs) as T;
                            break;

                        case SerializableType.XML:
                        default:
                            XmlSerializer serializer = new XmlSerializer(typeof(T));
                            targetObject = serializer.Deserialize(fs) as T;
                            break;
                    }

                    success = true;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                success = false;
            }

            return success;
        }

        public static T DeepCopySerializableObject<T>(T source)
        {
            if (false == typeof(T).IsSerializable)
            {
                return source;
            }

            try
            {
                using (var ms = new MemoryStream())
                {
                    var formatter = new BinaryFormatter();
                    formatter.Serialize(ms, source);
                    ms.Position = 0;
                    return (T)formatter.Deserialize(ms);
                }
            }
            catch(Exception ex)
            {
                return source;
            }
        }

        static bool DeepCopySerializableObjectToMemoryStream<T>(T source, ref MemoryStream memoryStream)
        {
            if (false == typeof(T).IsSerializable)
            {
                return false;
            }

            try
            {
                if (memoryStream == null)
                {
                    memoryStream = new MemoryStream();
                }
                var formatter = new BinaryFormatter();
                formatter.Serialize(memoryStream, source);
                memoryStream.Position = 0;
            }
            catch
            {
                if (memoryStream != null)
                {
                    memoryStream.Dispose();
                    memoryStream = null;
                }
                return false;
            }
            return true;
        }

        static bool GetSerializableObjectFromMemoryStream<T>(MemoryStream memoryStream, ref T serializableObject)
        {
            if (false == typeof(T).IsSerializable || memoryStream == null)
            {
                return false;
            }

            try
            {
                var formatter = new BinaryFormatter();
                serializableObject = (T)formatter.Deserialize(memoryStream);
                return true;
            }
            catch
            {
                return false;
            }
        }

        class ObjectForAsyncSerialization : IDisposable
        {
            public string ObjectName
            {
                get
                {
                    return _objectName;
                }
            }
            public MemoryStream Memory
            {
                get
                {
                    return _memoryStream;
                }
            }

            public ObjectForAsyncSerialization(string objectName, MemoryStream memoryStreamOfMap)
            {
                _objectName = objectName;
                _memoryStream = memoryStreamOfMap;
            }

            string _objectName;
            MemoryStream _memoryStream;

            public void Dispose()
            {
                if (_memoryStream != null)
                {
                    _memoryStream.Dispose();
                    _memoryStream = null;
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using Unity.Netcode;
using UnityEngine;
using Unity.Collections;
using System.Threading.Tasks;

namespace TcgEngine
{
    /// <summary>
    /// 网络相关的工具类，提供序列化、反序列化、IP解析等静态方法
    /// </summary>
    public class NetworkTool
    {
        // 将可序列化对象序列化为字节数组
        public static byte[] Serialize<T>(T obj) where T : class
        {
            try
            {
                BinaryFormatter bf = new BinaryFormatter();
                MemoryStream ms = new MemoryStream();
                bf.Serialize(ms, obj);
                byte[] bytes = ms.ToArray();
                ms.Close();
                return bytes;
            }
            catch (Exception e)
            {
                Debug.LogError("Serialization error: " + e.Message);
                return new byte[0];
            }
        }

        // 从字节数组反序列化成对象
        public static T Deserialize<T>(byte[] bytes) where T : class
        {
            try
            {
                BinaryFormatter bf = new BinaryFormatter();
                MemoryStream ms = new MemoryStream();
                ms.Write(bytes, 0, bytes.Length);
                ms.Seek(0, SeekOrigin.Begin);
                T obj = (T)bf.Deserialize(ms);
                ms.Close();
                return obj;
            }
            catch (Exception e)
            {
                Debug.LogError("Deserialization error: " + e.Message);
                return null;
            }
        }

        // 将实现INetworkSerializable接口的对象序列化为字节数组
        public static byte[] NetSerialize<T>(T obj, int size = 128) where T : INetworkSerializable, new()
        {
            if (obj == null)
                return new byte[0];

            try
            {
                FastBufferWriter writer = new FastBufferWriter(size, Allocator.Temp, TcgNetwork.MsgSizeMax);
                writer.WriteNetworkSerializable(obj);
                byte[] bytes = writer.ToArray();
                writer.Dispose();
                return bytes;
            }
            catch (Exception e)
            {
                Debug.LogError("Serialization error: " + e.Message);
                return new byte[0];
            }
        }

        // 从字节数组反序列化INetworkSerializable对象
        public static T NetDeserialize<T>(byte[] bytes) where T : INetworkSerializable, new()
        {
            if (bytes == null || bytes.Length == 0)
                return default(T);

            try
            {
                FastBufferReader reader = new FastBufferReader(bytes, Allocator.Temp);
                reader.ReadNetworkSerializable(out T obj);
                reader.Dispose();
                return obj;
            }
            catch (Exception e)
            {
                Debug.LogError("Deserialization error: " + e.Message);
                return default(T);
            }
        }

        // 使用Netcode序列化字符串数组
        public static void NetSerializeArray<TS>(BufferSerializer<TS> serializer, ref string[] array) where TS : IReaderWriter
        {
            if (serializer.IsReader)
            {
                int size = 0;
                serializer.SerializeValue(ref size);
                array = new string[size];
                for (int i = 0; i < size; i++)
                {
                    string val = "";
                    serializer.SerializeValue(ref val);
                    array[i] = val;
                }
            }

            if (serializer.IsWriter)
            {
                int size = array.Length;
                serializer.SerializeValue(ref size);
                for (int i = 0; i < size; i++)
                    serializer.SerializeValue(ref array[i]);
            }
        }

        // 使用Netcode序列化INetworkSerializable数组
        public static void NetSerializeArray<T, TS>(BufferSerializer<TS> serializer, ref T[] array)
            where T : INetworkSerializable, new() where TS : IReaderWriter
        {
            if (serializer.IsReader)
            {
                int size = 0;
                serializer.SerializeValue(ref size);
                array = new T[size];
                for(int i=0; i<size; i++)
                {
                    T val = new T();
                    serializer.SerializeValue(ref val);
                    array[i] = val;
                }
            }

            if (serializer.IsWriter)
            {
                int size = array.Length;
                serializer.SerializeValue(ref size);
                for (int i = 0; i < size; i++)
                    serializer.SerializeValue(ref array[i]);
            }
        }

        // 将int序列化为字节数组
        public static byte[] SerializeInt32(int data)
        {
            return System.BitConverter.GetBytes(data);
        }

        // 将字节数组反序列化为int
        public static int DeserializeInt32(byte[] bytes)
        {
            if (bytes != null && bytes.Length > 0)
                return System.BitConverter.ToInt32(bytes, 0);
            return 0;
        }

        // 将ulong序列化为字节数组
        public static byte[] SerializeUInt64(ulong data)
        {
            return System.BitConverter.GetBytes(data);
        }

        // 将字节数组反序列化为ulong
        public static ulong DeserializeUInt64(byte[] bytes)
        {
            if (bytes != null && bytes.Length > 0)
                return System.BitConverter.ToUInt64(bytes, 0);
            return 0;
        }

        // 将字符串序列化为字节数组（UTF8）
        public static byte[] SerializeString(string data)
        {
            if(data != null)
                return System.Text.Encoding.UTF8.GetBytes(data);
            return new byte[0];
        }

        // 将字节数组反序列化为字符串（UTF8）
        public static string DeserializeString(byte[] bytes)
        {
            if (bytes != null)
                return System.Text.Encoding.UTF8.GetString(bytes);
            return null;
        }

        // 将对象序列化为Base64字符串
        public static string SerializeToString<T>(T obj) where T : class
        {
            byte[] bytes = Serialize<T>(obj);
            return Convert.ToBase64String(bytes);
        }

        // 从Base64字符串反序列化对象
        public static T DeserializeFromString<T>(string str) where T : class
        {
            byte[] bytes = Convert.FromBase64String(str);
            return Deserialize<T>(bytes);
        }

        // 使用Netcode的BufferSerializer序列化任意对象
        public static void SerializeObject<T, T1>(BufferSerializer<T> serializer, ref T1 data) where T : IReaderWriter where T1 : class
        {
            string sdata = "";
            if (serializer.IsWriter)
            {
                sdata = SerializeToString(data);
            }
            serializer.SerializeValue(ref sdata, true);
            if (serializer.IsReader)
            {
                data = DeserializeFromString<T1>(sdata);
            }
        }

        // 序列化泛型字典，Key和Value均为非托管类型
        public static void SerializeDictionary<T, T1, T2>(BufferSerializer<T> serializer, ref Dictionary<T1, T2> data)
            where T : IReaderWriter where T1 : unmanaged, IComparable, IConvertible, IComparable<T1>, IEquatable<T1> where T2 : unmanaged, IComparable, IConvertible, IComparable<T2>, IEquatable<T2>
        {
            int count = data != null ? data.Count : 0;
            serializer.SerializeValue(ref count);

            if (serializer.IsWriter)
            {
                foreach (KeyValuePair<T1, T2> pair in data)
                {
                    T1 key = pair.Key;
                    T2 val = pair.Value;
                    serializer.SerializeValue(ref key);
                    serializer.SerializeValue(ref val);
                }
            }
            if (serializer.IsReader)
            {
                data = new Dictionary<T1, T2>();
                for (int i = 0; i < count; i++)
                {
                    T1 key = new T1();
                    T2 val = new T2();
                    serializer.SerializeValue(ref key);
                    serializer.SerializeValue(ref val);
                    data.Add(key, val);
                }
            }
        }

        // 序列化Key为枚举，Value为非托管类型的字典
        public static void SerializeDictionaryEnum<T, T1, T2>(BufferSerializer<T> serializer, ref Dictionary<T1, T2> data)
            where T : IReaderWriter where T1 : unmanaged, Enum where T2 : unmanaged, IComparable, IConvertible, IComparable<T2>, IEquatable<T2>
        {
            int count = data != null ? data.Count : 0;
            serializer.SerializeValue(ref count);

            if (serializer.IsWriter)
            {
                foreach (KeyValuePair<T1, T2> pair in data)
                {
                    T1 key = pair.Key;
                    T2 val = pair.Value;
                    serializer.SerializeValue(ref key);
                    serializer.SerializeValue(ref val);
                }
            }
            if (serializer.IsReader)
            {
                data = new Dictionary<T1, T2>();
                for (int i = 0; i < count; i++)
                {
                    T1 key = new T1();
                    T2 val = new T2();
                    serializer.SerializeValue(ref key);
                    serializer.SerializeValue(ref val);
                    data.Add(key, val);
                }
            }
        }

        // 序列化Key为string，Value为非托管类型的字典
        public static void SerializeDictionary<T, T2>(BufferSerializer<T> serializer, ref Dictionary<string, T2> data)
            where T : IReaderWriter where T2 : unmanaged, IComparable, IConvertible, IComparable<T2>, IEquatable<T2>
        {
            int count = data != null ? data.Count : 0;
            serializer.SerializeValue(ref count);

            if (serializer.IsWriter)
            {
                foreach (KeyValuePair<string, T2> pair in data)
                {
                    string key = pair.Key;
                    T2 val = pair.Value;
                    serializer.SerializeValue(ref key);
                    serializer.SerializeValue(ref val);
                }
            }
            if (serializer.IsReader)
            {
                data = new Dictionary<string, T2>();
                for (int i = 0; i < count; i++)
                {
                    string key = "";
                    T2 val = new T2();
                    serializer.SerializeValue(ref key);
                    serializer.SerializeValue(ref val);
                    data.Add(key, val);
                }
            }
        }

        // 序列化Key和Value均为string的字典
        public static void SerializeDictionary<T>(BufferSerializer<T> serializer, ref Dictionary<string, string> data)
            where T : IReaderWriter
        {
            int count = data != null ? data.Count : 0;
            serializer.SerializeValue(ref count);

            if (serializer.IsWriter)
            {
                foreach (KeyValuePair<string, string> pair in data)
                {
                    string key = pair.Key;
                    string val = pair.Value;
                    serializer.SerializeValue(ref key);
                    serializer.SerializeValue(ref val);
                }
            }
            if (serializer.IsReader)
            {
                data = new Dictionary<string, string>();
                for (int i = 0; i < count; i++)
                {
                    string key = "";
                    string val = "";
                    serializer.SerializeValue(ref key);
                    serializer.SerializeValue(ref val);
                    data.Add(key, val);
                }
            }
        }

        // 序列化Key为string，Value为INetworkSerializable对象的字典
        public static void SerializeDictionaryNetObject<T, T2>(BufferSerializer<T> serializer, ref Dictionary<string, T2> data)
            where T : IReaderWriter where T2 : INetworkSerializable, new()
        {
            int count = data != null ? data.Count : 0;
            serializer.SerializeValue(ref count);

            if (serializer.IsWriter)
            {
                foreach (KeyValuePair<string, T2> pair in data)
                {
                    string key = pair.Key;
                    T2 val = pair.Value;
                    serializer.SerializeValue(ref key);
                    serializer.SerializeNetworkSerializable(ref val);
                }
            }
            if (serializer.IsReader)
            {
                data = new Dictionary<string, T2>();
                for (int i = 0; i < count; i++)
                {
                    string key = "";
                    T2 val = new T2();
                    serializer.SerializeValue(ref key);
                    serializer.SerializeNetworkSerializable(ref val);
                    data.Add(key, val);
                }
            }
        }

        // 序列化Key为非托管类型，Value为INetworkSerializable对象的字典
        public static void SerializeDictionaryNetObject<T, T1, T2>(BufferSerializer<T> serializer, ref Dictionary<T1, T2> data)
            where T : IReaderWriter where T1 : unmanaged, IComparable, IConvertible, IComparable<T1>, IEquatable<T1> where T2 : INetworkSerializable, new()
        {
            int count = data != null ? data.Count : 0;
            serializer.SerializeValue(ref count);

            if (serializer.IsWriter)
            {
                foreach (KeyValuePair<T1, T2> pair in data)
                {
                    T1 key = pair.Key;
                    T2 val = pair.Value;
                    serializer.SerializeValue(ref key);
                    serializer.SerializeNetworkSerializable(ref val);
                }
            }
            if (serializer.IsReader)
            {
                data = new Dictionary<T1, T2>();
                for (int i = 0; i < count; i++)
                {
                    T1 key = new T1();
                    T2 val = new T2();
                    serializer.SerializeValue(ref key);
                    serializer.SerializeNetworkSerializable(ref val);
                    data.Add(key, val);
                }
            }
        }

        // 序列化Key为string，Value为类对象的字典
        public static void SerializeDictionaryObject<T, T2>(BufferSerializer<T> serializer, ref Dictionary<string, T2> data)
            where T : IReaderWriter where T2 : class, new()
        {
            int count = data != null ? data.Count : 0;
            serializer.SerializeValue(ref count);

            if (serializer.IsWriter)
            {
                foreach (KeyValuePair<string, T2> pair in data)
                {
                    string key = pair.Key;
                    T2 val = pair.Value;
                    serializer.SerializeValue(ref key);
                    SerializeObject(serializer, ref val);
                }
            }
            if (serializer.IsReader)
            {
                data = new Dictionary<string, T2>();
                for (int i = 0; i < count; i++)
                {
                    string key = "";
                    T2 val = new T2();
                    serializer.SerializeValue(ref key);
                    SerializeObject(serializer, ref val);
                    data.Add(key, val);
                }
            }
        }

        // 序列化Key为非托管类型，Value为类对象的字典
        public static void SerializeDictionaryObject<T, T1, T2>(BufferSerializer<T> serializer, ref Dictionary<T1, T2> data)
            where T : IReaderWriter where T1 : unmanaged, IComparable, IConvertible, IComparable<T1>, IEquatable<T1> where T2 : class, new()
        {
            int count = data != null ? data.Count : 0;
            serializer.SerializeValue(ref count);

            if (serializer.IsWriter)
            {
                foreach (KeyValuePair<T1, T2> pair in data)
                {
                    T1 key = pair.Key;
                    T2 val = pair.Value;
                    serializer.SerializeValue(ref key);
                    SerializeObject(serializer, ref val);
                }
            }
            if (serializer.IsReader)
            {
                data = new Dictionary<T1, T2>();
                for (int i = 0; i < count; i++)
                {
                    T1 key = new T1();
                    T2 val = new T2();
                    serializer.SerializeValue(ref key);
                    SerializeObject(serializer, ref val);
                    data.Add(key, val);
                }
            }
        }

        // 序列化Key为枚举类型，Value为类对象的字典
        public static void SerializeDictionaryEnumObject<T, T1, T2>(BufferSerializer<T> serializer, ref Dictionary<T1, T2> data)
            where T : IReaderWriter where T1 : unmanaged, Enum where T2 : class, new()
        {
            int count = data != null ? data.Count : 0;
            serializer.SerializeValue(ref count);

            if (serializer.IsWriter)
            {
                foreach (KeyValuePair<T1, T2> pair in data)
                {
                    T1 key = pair.Key;
                    T2 val = pair.Value;
                    serializer.SerializeValue(ref key);
                    SerializeObject(serializer, ref val);
                }
            }
            if (serializer.IsReader)
            {
                data = new Dictionary<T1, T2>();
                for (int i = 0; i < count; i++)
                {
                    T1 key = new T1();
                    T2 val = new T2();
                    serializer.SerializeValue(ref key);
                    SerializeObject(serializer, ref val);
                    data.Add(key, val);
                }
            }
        }

        // 对字符串进行16位哈希
        public static ushort Hash16(string string_id)
        {
            return (ushort) string_id.GetHashCode();
        }

        // 对字符串进行32位哈希
        public static uint Hash32(string string_id)
        {
            return (uint) string_id.GetHashCode();
        }

        // 对字符串进行64位哈希
        public static ulong Hash64(string string_id)
        {
            string s1 = string_id.Substring(0, string_id.Length / 2);
            string s2 = string_id.Substring(string_id.Length / 2);
            ulong id = (uint)s1.GetHashCode();
            id = id << 32;
            id = id | (uint)s2.GetHashCode();
            return id;
        }

        // 解析域名为IP地址
        public static IPAddress ResolveDns(string url)
        {
#if !UNITY_WEBGL
            IPAddress[] ips = Dns.GetHostAddresses(url);
            if (ips != null && ips.Length > 0)
                return ips[0];
#else
            Debug.LogWarning("Dns.GetHostAddresses not working on WebGL, try using direct IP address instead of host url");
#endif
            return null;
        }

        // 将host（域名或IP）转换为IP地址
        public static string HostToIP(string host)
        {
            bool success = IPAddress.TryParse(host, out IPAddress address);
            if (success)
                return address.ToString(); // 已经是IP
            IPAddress ip = ResolveDns(host); // 解析域名
            if (ip != null)
                return ip.ToString();
            return "";
        }

        // 获取本机内网IP
        public static string GetLocalIp()
        {
            IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in hostEntry.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return "";
        }

        // 获取外网IP
        public static async Task<string> GetOnlineIp()
        {
            WebResponse res = await WebTool.SendRequest("https://api.ipify.org");
            if (res.success)
                return res.data;
            else
                return null;
        }
    }
}

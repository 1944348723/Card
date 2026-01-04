using UnityEngine;
using System;
using System.Text;
using System.Security.Cryptography;
using UnityEngine.Events;

namespace TcgEngine
{
    /// <summary>
    /// ApiClient 的实用工具静态函数
    /// </summary>
    public class ApiTool : MonoBehaviour
    {
        // ----- 转换相关方法 ------

        /// <summary>
        /// 将 JSON 字符串转换为对象
        /// </summary>
        /// <typeparam name="T">目标对象类型</typeparam>
        /// <param name="json">JSON 字符串</param>
        /// <returns>转换后的对象，如果失败则返回类型默认实例</returns>
        public static T JsonToObject<T>(string json)
        {
            try
            {
                T value = JsonUtility.FromJson<T>(json);
                return value;
            }
            catch (Exception) { }

            // 如果转换失败，返回 T 类型的默认实例
            return (T)Activator.CreateInstance(typeof(T));
        }

        /// <summary>
        /// 将 JSON 数组字符串转换为对象数组
        /// </summary>
        /// <typeparam name="T">数组中元素的类型</typeparam>
        /// <param name="json">JSON 数组字符串</param>
        /// <returns>转换后的对象数组，如果失败则返回空数组</returns>
        public static T[] JsonToArray<T>(string json)
        {
            ListJson<T> list = new ListJson<T>();
            list.list = new T[0];
            try
            {
                // 包装 JSON 为一个对象，以便 JsonUtility 可以解析数组
                string wrap_json = "{ \"list\": " + json + "}";
                list = JsonUtility.FromJson<ListJson<T>>(wrap_json);
                return list.list;
            }
            catch (Exception) { }

            return new T[0];
        }

        /// <summary>
        /// 将对象序列化为 JSON 字符串
        /// </summary>
        /// <param name="data">要序列化的对象</param>
        /// <returns>JSON 字符串</returns>
        public static string ToJson(object data)
        {
            return JsonUtility.ToJson(data);
        }

        /// <summary>
        /// 将字符串解析为整数
        /// </summary>
        /// <param name="int_str">要解析的字符串</param>
        /// <param name="default_val">解析失败时的默认值，默认为 0</param>
        /// <returns>解析后的整数值</returns>
        public static int ParseInt(string int_str, int default_val = 0)
        {
            bool success = int.TryParse(int_str, out int val);
            return success ? val : default_val;
        }

        // 内部类用于 JsonUtility 解析数组
        [Serializable]
        private class ListJson<T>
        {
            public T[] list;
        }
    }
}

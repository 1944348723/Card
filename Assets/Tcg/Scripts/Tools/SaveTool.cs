using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 保存工具类，用于将类写入磁盘或从磁盘读取类文件
    /// 适用于标记了 [System.Serializable] 的类
    /// </summary>
    [System.Serializable]
    public class SaveTool
    {
        /// <summary>
        /// 加载指定文件并反序列化为类对象
        /// 确保类被标记为 [System.Serializable]
        /// </summary>
        /// <typeparam name="T">要反序列化的类类型</typeparam>
        /// <param name="filename">文件名</param>
        /// <returns>返回加载的类对象，如果文件不存在或出错返回null</returns>
        public static T LoadFile<T>(string filename) where T : class
        {
            T data = null;
            string fullpath = Application.persistentDataPath + "/" + filename; //完整文件路径
            if (IsValidFilename(filename) && File.Exists(fullpath))
            {
                FileStream file = null;
                try
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    file = File.Open(fullpath, FileMode.Open);
                    data = (T)bf.Deserialize(file); //反序列化
                    file.Close();
                }
                catch (System.Exception e)
                {
                    Debug.Log("Error Loading Data " + e); //打印加载错误
                    if (file != null) file.Close();
                }
            }
            return data;
        }

        /// <summary>
        /// 将类对象序列化并保存到文件
        /// 确保类被标记为 [System.Serializable]
        /// </summary>
        /// <typeparam name="T">要序列化的类类型</typeparam>
        /// <param name="filename">文件名</param>
        /// <param name="data">类对象</param>
        public static void SaveFile<T>(string filename, T data) where T : class
        {
            if (IsValidFilename(filename))
            {
                FileStream file = null;
                try
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    string fullpath = Application.persistentDataPath + "/" + filename; //完整文件路径
                    file = File.Create(fullpath);
                    bf.Serialize(file, data); //序列化写入文件
                    file.Close();
                }
                catch (System.Exception e)
                {
                    Debug.Log("Error Saving Data " + e); //打印保存错误
                    if (file != null) file.Close();
                }
            }
        }

        /// <summary>
        /// 删除指定文件
        /// </summary>
        /// <param name="filename">文件名</param>
        public static void DeleteFile(string filename)
        {
            string fullpath = Application.persistentDataPath + "/" + filename; //完整路径
            if (File.Exists(fullpath))
                File.Delete(fullpath); //删除文件
        }

        /// <summary>
        /// 获取所有保存文件，可指定扩展名过滤
        /// </summary>
        /// <param name="extension">扩展名过滤，例如 ".save"</param>
        /// <returns>返回文件名列表</returns>
        public static List<string> GetAllSave(string extension = "")
        {
            List<string> saves = new List<string>();
            string[] files = Directory.GetFiles(Application.persistentDataPath); //获取路径下所有文件
            foreach (string file in files)
            {
                if (file.EndsWith(extension))
                {
                    string filename = Path.GetFileName(file); //获取文件名
                    if (!saves.Contains(filename))
                        saves.Add(filename);
                }
            }
            return saves;
        }

        /// <summary>
        /// 检查文件是否存在
        /// </summary>
        /// <param name="filename">文件名</param>
        /// <returns>存在返回true，否则false</returns>
        public static bool DoesFileExist(string filename)
        {
            string fullpath = Application.persistentDataPath + "/" + filename;
            return IsValidFilename(filename) && File.Exists(fullpath);
        }

        /// <summary>
        /// 验证文件名是否合法
        /// 不能为空或包含特殊字符
        /// </summary>
        /// <param name="filename">文件名</param>
        /// <returns>合法返回true，否则false</returns>
        public static bool IsValidFilename(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename))
                return false; //文件名不能为空或仅空格

            foreach (char c in Path.GetInvalidFileNameChars())
            {
                if (filename.Contains(c.ToString()))
                    return false; //不允许包含任何特殊字符
            }
            return true;
        }
    }
}

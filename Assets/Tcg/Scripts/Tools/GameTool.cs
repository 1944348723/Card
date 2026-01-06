using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TcgEngine
{
    /// <summary>
    /// 游戏工具类（静态方法集合）
    /// 提供随机数、UID生成、列表操作以及设备/渲染管线检测等通用方法
    /// </summary>
    public static class GameTool
    {
        private const string uid_chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        private static System.Random random = new System.Random();

        /// <summary>
        /// 生成一个随机字符串，可用作唯一 ID（UID）
        /// </summary>
        /// <param name="min">最小长度</param>
        /// <param name="max">最大长度</param>
        /// <returns>随机生成的字符串</returns>
        public static string GenerateRandomID(int min = 9, int max = 15)
        {
            int length = random.Next(min, max);
            string unique_id = "";
            for (int i = 0; i < length; i++)
            {
                unique_id += uid_chars[random.Next(uid_chars.Length - 1)];
            }
            return unique_id;
        }

        /// <summary>
        /// 生成一个随机整数
        /// </summary>
        public static int GenerateRandomInt()
        {
            return random.Next(int.MinValue, int.MaxValue);
        }

        /// <summary>
        /// 生成一个随机 ulong（64 位无符号整数）
        /// </summary>
        public static ulong GenerateRandomUInt64()
        {
            ulong id = (uint)random.Next(int.MinValue, int.MaxValue); // 先生成低 32 位
            uint bid = (uint)random.Next(int.MinValue, int.MaxValue); // 再生成高 32 位
            id = id << 32;
            id = id | bid;
            return id;
        }

        /// <summary>
        /// 从 source 列表中随机选择 x 个元素放入 dest（不会重复选择，除非 source 中本身有重复元素）
        /// </summary>
        public static List<T> PickXRandom<T>(List<T> source, List<T> dest, int x)
        {
            if (source.Count <= x || x <= 0)
                return source; // 不需要选择，直接返回原列表

            if (dest.Count > 0)
                dest.Clear();

            for (int i = 0; i < x; i++)
            {
                int r = random.Next(source.Count);
                dest.Add(source[r]);
                source.RemoveAt(r);
            }

            return dest;
        }

        /// <summary>
        /// 克隆字符串列表（高效方式，尽量避免不必要的 Add/Remove）
        /// </summary>
        public static void CloneList(List<string> source, List<string> dest)
        {
            for (int i = 0; i < source.Count; i++)
            {
                if (i < dest.Count)
                    dest[i] = source[i];
                else
                    dest.Add(source[i]);
            }

            if (dest.Count > source.Count)
                dest.RemoveRange(source.Count, dest.Count - source.Count);
        }

        /// <summary>
        /// 克隆列表（元素引用保留，不复制元素本身）
        /// </summary>
        public static void CloneListRef<T>(List<T> source, List<T> dest) where T : class
        {
            for (int i = 0; i < source.Count; i++)
            {
                if (i < dest.Count)
                    dest[i] = source[i];
                else
                    dest.Add(source[i]);
            }

            if (dest.Count > source.Count)
                dest.RemoveRange(source.Count, dest.Count - source.Count);
        }

        /// <summary>
        /// 克隆列表（元素可为空）
        /// </summary>
        public static void CloneListRefNull<T>(List<T> source, ref List<T> dest) where T : class
        {
            // source 为 null，则 dest 也置为 null
            if (source == null)
            {
                dest = null;
                return;
            }

            // dest 为 null，创建新列表
            if (dest == null)
                dest = new List<T>();

            // 两者都不为 null，进行克隆
            CloneListRef(source, dest);
        }

        /// <summary>
        /// 检查当前设备是否为移动设备（Android/iOS/Tizen）
        /// </summary>
        public static bool IsMobile()
        {
#if UNITY_ANDROID || UNITY_IOS || UNITY_TIZEN
            return true;
#else
            return UnityEngine.Device.Application.isMobilePlatform;
#endif
        }

        /// <summary>
        /// 检查当前项目是否使用 Universal Render Pipeline (URP)
        /// 如果返回编译错误（因为项目未安装 URP），可以注释掉此函数并返回 false
        /// </summary>
        public static bool IsURP()
        {
            if (GraphicsSettings.renderPipelineAsset is UniversalRenderPipelineAsset)
                return true;
            return false;
        }

    }
}

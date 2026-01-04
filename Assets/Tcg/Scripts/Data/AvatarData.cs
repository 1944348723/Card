using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 定义游戏中所有头像（Avatar）数据
    /// </summary>

    [CreateAssetMenu(fileName = "Avatar", menuName = "TcgEngine/Avatar", order = 10)]
    public class AvatarData : ScriptableObject
    {
        public string id;         // 头像唯一ID
        public Sprite avatar;     // 头像图片资源
        public int sort_order;    // 排序顺序，用于列表显示

        // 存储所有加载的头像数据，方便全局访问
        public static List<AvatarData> avatar_list = new List<AvatarData>();

        /// <summary>
        /// 从资源文件夹加载所有 AvatarData
        /// </summary>
        /// <param name="folder">资源文件夹路径（可选，默认空）</param>
        public static void Load(string folder = "")
        {
            if (avatar_list.Count == 0)
                avatar_list.AddRange(Resources.LoadAll<AvatarData>(folder)); // 加载所有头像数据

            // 根据 sort_order 和 id 排序，保证显示顺序一致
            avatar_list.Sort((AvatarData a, AvatarData b) => { 
                if (a.sort_order == b.sort_order) 
                    return a.id.CompareTo(b.id); // 如果排序相同则按ID字母序排序
                else 
                    return a.sort_order.CompareTo(b.sort_order); // 否则按 sort_order 排序
            });
        }

        /// <summary>
        /// 根据ID获取指定头像
        /// </summary>
        /// <param name="id">头像ID</param>
        /// <returns>对应的 AvatarData，如果不存在返回 null</returns>
        public static AvatarData Get(string id)
        {
            foreach (AvatarData avatar in GetAll())
            {
                if (avatar.id == id)
                    return avatar;
            }
            return null;
        }

        /// <summary>
        /// 获取所有头像数据
        /// </summary>
        /// <returns>头像数据列表</returns>
        public static List<AvatarData> GetAll()
        {
            return avatar_list;
        }
    }
}
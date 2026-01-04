using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 定义所有稀有度数据（例如普通、非凡、稀有、神话）
    /// </summary>
    [CreateAssetMenu(fileName = "RarityData", menuName = "TcgEngine/RarityData", order = 1)]
    public class RarityData : ScriptableObject
    {
        public string id;      // 稀有度唯一ID
        public string title;   // 稀有度名称，例如“普通”“稀有”等
        public Sprite icon;    // 稀有度图标
        public int rank;       // 稀有度等级索引，从1开始（普通）依次递增

        public static List<RarityData> rarity_list = new List<RarityData>(); // 所有稀有度数据列表

        /// <summary>
        /// 加载Resources下的所有RarityData资源
        /// </summary>
        /// <param name="folder">Resources内的子文件夹路径，可选</param>
        public static void Load(string folder = "")
        {
            if (rarity_list.Count == 0)
                rarity_list.AddRange(Resources.LoadAll<RarityData>(folder));
        }

        /// <summary>
        /// 获取最基础的稀有度（rank最低）
        /// </summary>
        /// <returns>返回rank最小的RarityData</returns>
        public static RarityData GetFirst()
        {
            int lowest = 99999;
            RarityData first = null;
            foreach (RarityData rarity in GetAll())
            {
                if (rarity.rank < lowest)
                {
                    first = rarity;
                    lowest = rarity.rank;
                }
            }
            return first;
        }

        /// <summary>
        /// 根据ID获取稀有度数据
        /// </summary>
        /// <param name="id">稀有度ID</param>
        /// <returns>返回对应RarityData对象</returns>
        public static RarityData Get(string id)
        {
            foreach (RarityData rarity in GetAll())
            {
                if (rarity.id == id)
                    return rarity;
            }
            return null;
        }

        /// <summary>
        /// 获取所有稀有度数据
        /// </summary>
        /// <returns>返回所有RarityData列表</returns>
        public static List<RarityData> GetAll()
        {
            return rarity_list;
        }
    }
}

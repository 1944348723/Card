using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 定义卡包数据
    /// 每个卡包包含卡牌数量、稀有度概率、变体概率、显示信息、是否可购买等
    /// </summary>
    [CreateAssetMenu(fileName = "PackData", menuName = "TcgEngine/PackData", order = 5)]
    public class PackData : ScriptableObject
    {
        public string id;  // 卡包唯一ID

        [Header("Content 内容")]
        public PackType type;              // 卡包类型（随机或固定）
        public int cards = 5;              // 卡包内卡牌数量
        public PackRarity[] rarities_1st;  // 第一张卡的稀有度概率
        public PackRarity[] rarities;      // 其余卡牌的稀有度概率
        public PackVariant[] variants;     // 其余卡牌的变体概率

        [Header("Display 显示")]
        public string title;               // 卡包名称
        public Sprite pack_img;            // 卡包图片
        public Sprite cardback_img;        // 卡牌背面图片
        [TextArea(5, 10)]
        public string desc;                // 卡包描述
        public int sort_order;             // 排序用

        [Header("Availability 可用性")]
        public bool available = true;      // 是否可购买
        public int cost = 100;             // 购买费用

        public static List<PackData> pack_list = new List<PackData>(); // 所有卡包列表

        /// <summary>
        /// 加载所有PackData资源
        /// </summary>
        /// <param name="folder">Resources内子文件夹路径，可选</param>
        public static void Load(string folder = "")
        {
            if (pack_list.Count == 0)
                pack_list.AddRange(Resources.LoadAll<PackData>(folder));

            // 按sort_order排序，如果相同按ID排序
            pack_list.Sort((PackData a, PackData b) => {
                if (a.sort_order == b.sort_order)
                    return a.id.CompareTo(b.id);
                else
                    return a.sort_order.CompareTo(b.sort_order);
            });
        }

        /// <summary>
        /// 获取卡包名称
        /// </summary>
        /// <returns>返回卡包标题</returns>
        public string GetTitle()
        {
            return title;
        }

        /// <summary>
        /// 获取卡包描述
        /// </summary>
        /// <returns>返回描述字符串</returns>
        public string GetDesc()
        {
            return desc;
        }

        /// <summary>
        /// 根据ID获取卡包
        /// </summary>
        /// <param name="id">卡包ID</param>
        /// <returns>返回PackData对象</returns>
        public static PackData Get(string id)
        {
            foreach (PackData pack in GetAll())
            {
                if (pack.id == id)
                    return pack;
            }
            return null;
        }

        /// <summary>
        /// 获取所有可购买卡包
        /// </summary>
        /// <returns>返回可购买卡包列表</returns>
        public static List<PackData> GetAllAvailable()
        {
            List<PackData> valid_list = new List<PackData>();
            foreach (PackData apack in GetAll())
            {
                if (apack.available)
                    valid_list.Add(apack);
            }
            return valid_list;
        }

        /// <summary>
        /// 获取所有卡包
        /// </summary>
        /// <returns>返回所有PackData列表</returns>
        public static List<PackData> GetAll()
        {
            return pack_list;
        }
    }

    /// <summary>
    /// 卡包类型枚举
    /// </summary>
    public enum PackType
    {
        Random = 0,  // 随机卡包
        Fixed = 10,  // 固定卡包
    }

    /// <summary>
    /// 卡包中稀有度概率结构体
    /// </summary>
    [System.Serializable]
    public struct PackRarity
    {
        public RarityData rarity;   // 稀有度
        public int probability;     // 概率值
    }

    /// <summary>
    /// 卡包中变体概率结构体
    /// </summary>
    [System.Serializable]
    public struct PackVariant
    {
        public VariantData variant; // 变体
        public int probability;     // 概率值
    }
}

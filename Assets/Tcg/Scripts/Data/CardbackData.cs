using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 定义游戏中所有卡牌背面（Cardback）数据
    /// </summary>

    [CreateAssetMenu(fileName = "Cardback", menuName = "TcgEngine/Cardback", order = 10)]
    public class CardbackData : ScriptableObject
    {
        public string id;         // 卡牌背面唯一ID
        public Sprite cardback;   // 卡牌背面图片资源
        public Sprite deck;       // 对应牌组显示的图标（可选）
        public int sort_order;    // 排序顺序，用于列表显示

        // 存储所有加载的卡牌背面数据，方便全局访问
        public static List<CardbackData> cardback_list = new();

        /// <summary>
        /// 从资源文件夹加载所有 CardbackData
        /// </summary>
        /// <param name="folder">资源文件夹路径（可选，默认空）</param>
        public static void Load(string folder = "")
        {
            if (cardback_list.Count == 0)
                cardback_list.AddRange(Resources.LoadAll<CardbackData>(folder)); // 加载所有卡背数据

            // 根据 sort_order 和 id 排序，保证显示顺序一致
            cardback_list.Sort((CardbackData a, CardbackData b) => {
                if (a.sort_order == b.sort_order)
                    return a.id.CompareTo(b.id); // 如果排序相同则按ID字母序排序
                else
                    return a.sort_order.CompareTo(b.sort_order); // 否则按 sort_order 排序
            });
        }

        /// <summary>
        /// 根据ID获取指定卡背
        /// </summary>
        /// <param name="id">卡背ID</param>
        /// <returns>对应的 CardbackData，如果不存在返回 null</returns>
        public static CardbackData Get(string id)
        {
            foreach (CardbackData cardback in GetAll())
            {
                if (cardback.id == id)
                    return cardback;
            }
            return null;
        }

        /// <summary>
        /// 获取所有卡背数据
        /// </summary>
        /// <returns>卡背数据列表</returns>
        public static List<CardbackData> GetAll()
        {
            return cardback_list;
        }
    }
}
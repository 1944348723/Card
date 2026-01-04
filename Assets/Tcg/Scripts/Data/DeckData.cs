using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 定义固定套牌数据（用于游戏内固定套牌）
    /// 用户自定义套牌请参考 UserData.cs
    /// </summary>
    [CreateAssetMenu(fileName = "DeckData", menuName = "TcgEngine/DeckData", order = 7)]
    public class DeckData : ScriptableObject
    {
        public string id; // 套牌唯一ID

        [Header("Display")]
        public string title; // 套牌显示标题

        [Header("Cards")]
        public CardData hero; // 套牌中的英雄卡
        public CardData[] cards; // 套牌中的其他卡牌列表

        public static List<DeckData> deck_list = new List<DeckData>(); // 静态列表，保存所有加载的套牌数据

        /// <summary>
        /// 从 Resources 文件夹加载所有 DeckData
        /// </summary>
        /// <param name="folder">相对于 Resources 的路径，可为空加载所有</param>
        public static void Load(string folder = "")
        {
            if(deck_list.Count == 0)
                deck_list.AddRange(Resources.LoadAll<DeckData>(folder));
        }

        /// <summary>
        /// 获取套牌中卡牌的数量
        /// </summary>
        public int GetQuantity()
        {
            return cards.Length;
        }

        /// <summary>
        /// 判断套牌是否有效（卡牌数量是否达到规定的 deck_size）
        /// </summary>
        public bool IsValid()
        {
            return cards.Length >= GameplayData.Get().deck_size;
        }

        /// <summary>
        /// 根据ID获取套牌数据
        /// </summary>
        public static DeckData Get(string id)
        {
            foreach (DeckData deck in GetAll())
            {
                if (deck.id == id)
                    return deck;
            }
            return null;
        }

        /// <summary>
        /// 获取所有套牌数据列表
        /// </summary>
        public static List<DeckData> GetAll()
        {
            return deck_list;
        }
    }
}
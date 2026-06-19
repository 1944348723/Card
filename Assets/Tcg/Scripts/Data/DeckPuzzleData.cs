using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 用于套牌中特定卡牌或初始上场卡牌的槽位数据
    /// </summary>
    [System.Serializable]
    public class DeckCardSlot
    {
        public CardData card; // 指定卡牌
        public SlotXY slot;   // 卡牌放置的格子坐标
    }

    /// <summary>
    /// 扩展 DeckData 的套牌数据，增加初始上场卡牌、初始手牌、初始法力等信息
    /// 用于固定关卡或谜题模式
    /// </summary>
    [CreateAssetMenu(fileName = "DeckPuzzleData", menuName = "TcgEngine/DeckPuzzleData", order = 7)]
    public class DeckPuzzleData : DeckData
    {
        public DeckCardSlot[] board_cards; // 初始放置在场上的卡牌槽位
        public int start_cards = 5;        // 初始手牌数量
        public int start_mana = 2;         // 初始法力值
        public int start_hp = 20;          // 初始生命值
        public bool dont_shuffle_deck;     // 是否不洗牌（true = 按顺序抽牌）

        /// <summary>
        /// 根据ID获取 DeckPuzzleData 类型的套牌
        /// </summary>
        public static new DeckPuzzleData Get(string id)
        {
            foreach (DeckData deck in GetAll())
            {
                if (deck.id == id && deck is DeckPuzzleData)
                    return (DeckPuzzleData) deck;
            }
            return null;
        }
    }
}
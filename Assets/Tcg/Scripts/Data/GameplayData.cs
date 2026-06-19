using UnityEngine;
using TcgEngine.AI;

namespace TcgEngine
{
    /// <summary>
    /// 通用游戏玩法数据类
    /// 包含初始生命、法力、水晶、起始手牌、回合时间、AI等级、场景列表等配置
    /// </summary>
    [CreateAssetMenu(fileName = "GameplayData", menuName = "TcgEngine/GameplayData", order = 0)]
    public class GameplayData : ScriptableObject
    {
        [Header("Gameplay 游戏玩法")]
        public int hp_start = 20;              // 玩家初始生命值
        public int mana_start = 1;             // 玩家初始法力值
        public int mana_per_turn = 1;          // 每回合获得的法力值
        public int mana_max = 10;              // 法力上限
        public int cards_start = 5;            // 玩家起始手牌数
        public int cards_per_turn = 1;         // 每回合抽牌数
        public int cards_max = 10;             // 手牌上限
        public float turn_duration = 30f;      // 每回合持续时间（秒）
        public CardData second_bonus;          // 第二玩家的额外卡牌/奖励（可选）
        public bool mulligan;                  // 是否允许重选手牌

        [Header("Deckbuilding 卡组构建")]
        public int deck_size = 30;             // 卡组大小
        public int deck_duplicate_max = 2;     // 每张卡允许最大重复次数

        [Header("Buy/Sell 买卖规则")]
        public float sell_ratio = 0.8f;        // 卖卡时返还比例

        [Header("AI 人工智能")]
        public AIType ai_type;                 // AI 算法类型
        public int ai_level = 10;              // AI 等级，10=最强，1=最弱

        [Header("Decks 卡组")]
        public DeckData[] free_decks;          // 永远可用的卡组（菜单中显示，测试用）
        public DeckData[] starter_decks;       // 玩家初次选择的卡组（API启用时）
        public DeckData[] ai_decks;            // AI 单人模式随机选择的卡组

        [Header("Scenes 场景")]
        public string[] arena_list;            // 游戏场景列表

        [Header("Test 测试用")]
        public DeckData test_deck;             // 直接从Unity场景启动时使用的测试卡组
        public DeckData test_deck_ai;          // AI测试卡组
        public bool ai_vs_ai;                  // 是否AI对战AI

        /// <summary>
        /// 根据经验值计算玩家等级
        /// </summary>
        /// <param name="xp">玩家经验值</param>
        /// <returns>返回玩家等级</returns>
        // TODO: 这个函数放在这是怎么回事，而且明明是工具函数，不依赖于本身数据，还不是静态函数，UserData里也有一样的函数
        // 或者把每级多少经验改成配置项，这样这个函数就合理一些了
        public int GetPlayerLevel(int xp)
        {
            return Mathf.FloorToInt(xp / 1000f) + 1;
        }

        /// <summary>
        /// 随机选择一个游戏场景
        /// </summary>
        /// <returns>返回场景名称</returns>
        public string GetRandomArena()
        {
            if (arena_list.Length > 0)
                return arena_list[Random.Range(0, arena_list.Length)];
            return "Game";
        }

        /// <summary>
        /// 随机选择一个可用免费卡组
        /// </summary>
        /// <returns>返回卡组数据</returns>
        public DeckData GetRandomFreeDeck()
        {
            if (free_decks.Length > 0)
                return free_decks[Random.Range(0, free_decks.Length)];
            return null;
        }

        /// <summary>
        /// 随机选择一个AI卡组
        /// </summary>
        /// <returns>返回卡组数据</returns>
        public DeckData GetRandomAIDeck()
        {
            if (ai_decks.Length > 0)
                return ai_decks[Random.Range(0, ai_decks.Length)];
            return null;
        }

        /// <summary>
        /// 获取全局GameplayData实例
        /// </summary>
        /// <returns>返回DataLoader中的GameplayData</returns>
        public static GameplayData Get()
        {
            return DataLoader.Instance.data;
        }
    }
}

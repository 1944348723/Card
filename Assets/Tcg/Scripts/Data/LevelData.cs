using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 定义关卡数据
    /// 包含关卡ID、关卡等级、场景、玩家卡组、AI卡组、奖励等信息
    /// </summary>
    [CreateAssetMenu(fileName = "LevelData", menuName = "TcgEngine/LevelData", order = 7)]
    public class LevelData : ScriptableObject
    {
        public string id;              // 关卡唯一ID
        public int level;              // 关卡等级或序号

        [Header("Display 显示")]
        public string title;           // 关卡标题/名称

        [Header("Gameplay 游戏玩法")]
        public string scene;           // 关卡场景名
        public DeckData player_deck;   // 玩家卡组
        public DeckData ai_deck;       // AI卡组
        public int ai_level = 10;      // AI等级（1-10）
        public LevelFirst first_player; // 谁先手
        public GameObject tuto_prefab;  // 教学提示Prefab
        public bool mulligan = true;    // 是否允许重选手牌

        [Header("Rewards 奖励")]
        public int reward_xp = 100;       // 完成关卡获得经验
        public int reward_coins = 100;    // 完成关卡获得金币
        public PackData[] reward_packs;   // 奖励卡包
        public CardData[] reward_cards;   // 奖励卡牌
        public DeckData[] reward_decks;   // 奖励卡组

        public static List<LevelData> level_list = new(); // 所有关卡数据列表

        /// <summary>
        /// 加载所有LevelData资源
        /// </summary>
        /// <param name="folder">Resources内的子文件夹路径，可选</param>
        public static void Load(string folder = "")
        {
            if (level_list.Count == 0)
            {
                level_list.AddRange(Resources.LoadAll<LevelData>(folder));
                // 按关卡等级排序
                level_list.Sort((LevelData a, LevelData b) => { return a.level.CompareTo(b.level); });
            }
        }

        /// <summary>
        /// 获取关卡标题
        /// </summary>
        /// <returns>返回关卡名称</returns>
        public string GetTitle()
        {
            return title;
        }

        /// <summary>
        /// 根据ID获取关卡数据
        /// </summary>
        /// <param name="id">关卡ID</param>
        /// <returns>返回LevelData对象</returns>
        public static LevelData Get(string id)
        {
            foreach (LevelData level in GetAll())
            {
                if (level.id == id)
                    return level;
            }
            return null;
        }

        /// <summary>
        /// 获取所有关卡数据
        /// </summary>
        /// <returns>返回LevelData列表</returns>
        public static List<LevelData> GetAll()
        {
            return level_list;
        }
    }

    /// <summary>
    /// 先手类型枚举
    /// </summary>
    public enum LevelFirst
    {
        Random = 0,  // 随机先手
        Player = 10, // 玩家先手
        AI = 20,     // AI先手
    }
}

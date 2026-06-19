using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 定义奖励数据，便于上传到API
    /// </summary>
    [CreateAssetMenu(fileName = "RewardData", menuName = "TcgEngine/RewardData", order = 5)]
    public class RewardData : ScriptableObject
    {
        public string id;         // 奖励唯一ID
        public string group;      // 奖励分组，例如活动、任务等
        public int coins;         // 奖励的金币数量
        public int xp;            // 奖励的经验值

        public PackData[] packs;  // 奖励的卡包
        public CardData[] cards;  // 奖励的卡牌
        public DeckData[] decks;  // 奖励的卡组

        public bool repeat = true; // 是否可重复领取

        public static List<RewardData> reward_list = new(); // 所有奖励数据列表

        /// <summary>
        /// 加载Resources下的所有RewardData资源
        /// </summary>
        /// <param name="folder">Resources内的子文件夹路径，可选</param>
        public static void Load(string folder = "")
        {
            if (reward_list.Count == 0)
                reward_list.AddRange(Resources.LoadAll<RewardData>(folder));
        }

        /// <summary>
        /// 根据ID获取奖励数据
        /// </summary>
        /// <param name="id">奖励ID</param>
        /// <returns>返回对应的RewardData对象</returns>
        public static RewardData Get(string id)
        {
            foreach (RewardData reward in GetAll())
            {
                if (reward.id == id)
                    return reward;
            }
            return null;
        }

        /// <summary>
        /// 获取所有奖励数据
        /// </summary>
        /// <returns>返回RewardData列表</returns>
        public static List<RewardData> GetAll()
        {
            return reward_list;
        }
    }

}
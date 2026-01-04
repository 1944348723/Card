using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 从源列表中选择前 X 个目标
    /// </summary>
    
    [CreateAssetMenu(fileName = "filter", menuName = "TcgEngine/Filter/First", order = 10)]
    public class FilterFirst : FilterData
    {
        public int amount = 1; // 要选择的前 X 个目标的数量

        // 从卡牌列表中过滤目标，取前 amount 张卡牌
        public override List<Card> FilterTargets(Game data, AbilityData ability, Card caster, List<Card> source, List<Card> dest)
        {
            int max = Mathf.Min(source.Count, amount); // 防止数量超过源列表长度
            for (int i = 0; i < max; i++)
                dest.Add(source[i]);  // 将前 amount 张卡牌加入目标列表
            return dest;
        }

        // 从玩家列表中过滤目标，取前 amount 个玩家
        public override List<Player> FilterTargets(Game data, AbilityData ability, Card caster, List<Player> source, List<Player> dest)
        {
            int max = Mathf.Min(source.Count, amount); // 防止数量超过源列表长度
            for (int i = 0; i < max; i++)
                dest.Add(source[i]);  // 将前 amount 个玩家加入目标列表
            return dest;
        }

        // 从格子列表中过滤目标，取前 amount 个格子
        public override List<Slot> FilterTargets(Game data, AbilityData ability, Card caster, List<Slot> source, List<Slot> dest)
        {
            int max = Mathf.Min(source.Count, amount); // 防止数量超过源列表长度
            for (int i = 0; i < max; i++)
                dest.Add(source[i]);  // 将前 amount 个格子加入目标列表
            return dest;
        }
    }
}
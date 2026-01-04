using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 从目标列表中选择最后 X 个元素（卡牌、玩家或格子）
    /// </summary>
    
    [CreateAssetMenu(fileName = "filter", menuName = "TcgEngine/Filter/Last", order = 10)]
    public class FilterLast : FilterData
    {
        public int amount = 1; // 要选择的最后目标数量

        // 从卡牌列表中过滤最后几个目标
        public override List<Card> FilterTargets(Game data, AbilityData ability, Card caster, List<Card> source, List<Card> dest)
        {
            int max = Mathf.Min(source.Count, amount); // 确保不会超过源列表长度
            int min = source.Count - max; // 计算起始索引
            for (int i = source.Count - 1; i >= min; i--) // 从列表末尾向前取
                dest.Add(source[i]);
            return dest;
        }

        // 从玩家列表中过滤最后几个目标
        public override List<Player> FilterTargets(Game data, AbilityData ability, Card caster, List<Player> source, List<Player> dest)
        {
            int max = Mathf.Min(source.Count, amount);
            int min = source.Count - max;
            for (int i = source.Count - 1; i >= min; i--)
                dest.Add(source[i]);
            return dest;
        }

        // 从格子列表中过滤最后几个目标
        public override List<Slot> FilterTargets(Game data, AbilityData ability, Card caster, List<Slot> source, List<Slot> dest)
        {
            int max = Mathf.Min(source.Count, amount);
            int min = source.Count - max;
            for (int i = source.Count - 1; i >= min; i--)
                dest.Add(source[i]);
            return dest;
        }
    }
}
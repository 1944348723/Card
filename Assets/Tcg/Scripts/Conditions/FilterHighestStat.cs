using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 从目标列表中选择具有最高指定属性值的所有目标
    /// </summary>
    
    [CreateAssetMenu(fileName = "filter", menuName = "TcgEngine/Filter/HighestStat", order = 10)]
    public class FilterHighestStat : FilterData
    {
        public ConditionStatType stat; // 要比较的属性类型（攻击、生命、法力）

        // 从卡牌列表中过滤目标，选择属性值最高的卡牌
        public override List<Card> FilterTargets(Game data, AbilityData ability, Card caster, List<Card> source, List<Card> dest)
        {
            // 找到最高属性值
            int highest = -999;
            foreach (Card card in source)
            {
                int statValue = GetStat(card);
                if (statValue > highest)
                    highest = statValue;
            }

            // 将所有属性值等于最高的卡牌加入目标列表
            foreach (Card card in source)
            {
                int statValue = GetStat(card);
                if (statValue == highest)
                    dest.Add(card);
            }

            return dest;
        }

        // 获取卡牌指定的属性值
        private int GetStat(Card card)
        {
            if (stat == ConditionStatType.Attack)
            {
                return card.GetAttack(); // 攻击力
            }
            if (stat == ConditionStatType.HP)
            {
                return card.GetHP(); // 生命值
            }
            if (stat == ConditionStatType.Mana)
            {
                return card.GetMana(); // 法力值
            }
            return 0; // 其他情况返回 0
        }
    }
}
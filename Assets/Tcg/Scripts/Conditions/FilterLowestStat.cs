using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 从目标列表中选择具有最低指定属性值的所有卡牌
    /// </summary>
    
    [CreateAssetMenu(fileName = "filter", menuName = "TcgEngine/Filter/LowestStat", order = 10)]
    public class FilterLowestStat : FilterData
    {
        public ConditionStatType stat; // 指定要比较的属性类型（攻击/生命/法力）

        // 从卡牌列表中过滤出属性值最低的所有卡牌
        public override List<Card> FilterTargets(Game data, AbilityData ability, Card caster, List<Card> source, List<Card> dest)
        {
            // 找到最低属性值
            int lowest = 99999;
            foreach (Card card in source)
            {
                int statValue = GetStat(card);
                if (statValue < lowest)
                    lowest = statValue;
            }

            // 添加所有属性值等于最低值的卡牌
            foreach (Card card in source)
            {
                int statValue = GetStat(card);
                if (statValue == lowest)
                    dest.Add(card);
            }

            return dest;
        }

        // 获取卡牌对应属性值
        private int GetStat(Card card)
        {
            if (stat == ConditionStatType.Attack)
            {
                return card.GetAttack();
            }
            if (stat == ConditionStatType.HP)
            {
                return card.GetHP();
            }
            if (stat == ConditionStatType.Mana)
            {
                return card.GetMana();
            }
            return 0;
        }
    }
}
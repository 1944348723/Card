using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 从目标列表中随机选择指定数量的目标
    /// </summary>
    
    [CreateAssetMenu(fileName = "filter", menuName = "TcgEngine/Filter/Random", order = 10)]
    public class FilterRandom : FilterData
    {
        public int amount = 1; // 要随机选择的目标数量

        // 从卡牌列表中随机选择目标
        public override List<Card> FilterTargets(Game data, AbilityData ability, Card caster, List<Card> source, List<Card> dest)
        {
            return GameTool.PickXRandom(source, dest, amount);
        }

        // 从玩家列表中随机选择目标
        public override List<Player> FilterTargets(Game data, AbilityData ability, Card caster, List<Player> source, List<Player> dest)
        {
            return GameTool.PickXRandom(source, dest, amount);
        }

        // 从格子列表中随机选择目标
        public override List<Slot> FilterTargets(Game data, AbilityData ability, Card caster, List<Slot> source, List<Slot> dest)
        {
            return GameTool.PickXRandom(source, dest, amount);
        }

        // 从卡牌数据列表中随机选择目标
        public override List<CardData> FilterTargets(Game data, AbilityData ability, Card caster, List<CardData> source, List<CardData> dest)
        {
            return GameTool.PickXRandom(source, dest, amount);
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// 效果说明：
    /// 从目标卡牌或玩家身上移除指定的自定义属性或特性（Trait）。
    /// - trait：要移除的特性数据
    /// </summary>
    
    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/RemoveTrait", order = 10)]
    public class EffectRemoveTrait : EffectData
    {
        public TraitData trait;  // 要移除的特性数据

        // 对玩家目标执行效果
        public override void DoEffect(EffectContext logic, AbilityData ability, Card caster, Player target)
        {
            target.RemoveTrait(trait.id);  // 移除玩家身上的指定特性
        }

        // 对卡牌目标执行效果
        public override void DoEffect(EffectContext logic, AbilityData ability, Card caster, Card target)
        {
            target.RemoveTrait(trait.id);  // 移除卡牌身上的指定特性
        }
    }
}

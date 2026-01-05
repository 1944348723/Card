using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// 效果说明：
    /// 将目标卡牌或玩家的自定义属性（Trait）设置为指定值。
    /// - trait：要修改的自定义属性
    /// - ability.value：设置的具体数值
    /// - 支持即时效果和持续效果（Ongoing）
    /// </summary>
    
    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/SetStatCustom", order = 10)]
    public class EffectSetTrait : EffectData
    {
        public TraitData trait;  // 要设置的自定义属性

        // 对玩家目标执行效果
        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Player target)
        {
            target.SetTrait(trait.id, ability.value); // 设置玩家自定义属性
        }

        // 对卡牌目标执行效果
        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            target.SetTrait(trait.id, ability.value); // 设置卡牌自定义属性
        }

        // 对玩家目标执行持续效果（Ongoing）
        public override void DoOngoingEffect(GameLogic logic, AbilityData ability, Card caster, Player target)
        {
            target.SetTrait(trait.id, ability.value); // 设置玩家自定义属性
        }

        // 对卡牌目标执行持续效果（Ongoing）
        public override void DoOngoingEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            target.SetTrait(trait.id, ability.value); // 设置卡牌自定义属性
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// Effect that adds card/player custom stats or traits
    /// 效果说明：为卡牌或玩家添加自定义属性 / 特性（Trait）
    /// </summary>

    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/AddTrait", order = 10)]
    public class EffectAddTrait : EffectData
    {
        public TraitData trait;   // 要添加的特性数据（包含特性ID等）

        // 对“玩家目标”立即生效的效果
        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Player target)
        {
            // 给玩家添加一个 trait.id 的特性，数值为 ability.value
            target.AddTrait(trait.id, ability.value);
        }

        // 对“卡牌目标”立即生效的效果
        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            // 给卡牌添加一个 trait.id 的特性，数值为 ability.value
            target.AddTrait(trait.id, ability.value);
        }

        // 对“卡牌目标”的持续生效效果（例如光环、Buff 持续存在时）
        public override void DoOngoingEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            // 为卡牌添加一个“持续”的特性
            target.AddOngoingTrait(trait.id, ability.value);
        }

        // 对“玩家目标”的持续生效效果
        public override void DoOngoingEffect(GameLogic logic, AbilityData ability, Card caster, Player target)
        {
            // 为玩家添加一个“持续”的特性
            target.AddOngoingTrait(trait.id, ability.value);
        }
    }
}
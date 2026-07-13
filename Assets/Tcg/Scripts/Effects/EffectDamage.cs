using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// 效果说明：
    /// 对玩家或卡牌造成伤害（减少生命值 HP）。
    /// 伤害值 = 基础伤害 + 卡牌特性伤害加成 + 玩家特性伤害加成
    /// </summary>

    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/Damage", order = 10)]
    public class EffectDamage : EffectData
    {
        public TraitData bonus_damage;   // 用于计算额外伤害的 Trait（卡或玩家拥有该 Trait 则提供伤害加成）

        // 对玩家造成伤害
        public override void DoEffect(EffectContext logic, AbilityData ability, Card caster, Player target)
        {
            // 计算最终伤害
            int damage = GetDamage(logic.GameData, caster, ability.value);

            // 执行伤害逻辑
            logic.DamagePlayer(caster, target, damage, DamageType.Spell);
        }

        // 对卡牌造成伤害
        public override void DoEffect(EffectContext logic, AbilityData ability, Card caster, Card target)
        {
            // 计算最终伤害
            int damage = GetDamage(logic.GameData, caster, ability.value);

            // true 通常表示触发伤害动画/事件或可被防御系统识别
            logic.DamageCard(caster, target, damage, DamageType.Spell);
        }

        // 计算伤害值
        private int GetDamage(Game data, Card caster, int value)
        {
            Player player = data.GetPlayer(caster.player_id);

            // 基础伤害 + 卡自身特性加成 + 玩家全局特性加成
            int damage = value 
                         + caster.GetTraitValue(bonus_damage) 
                         + player.GetTraitValue(bonus_damage);

            return damage;
        }

    }
}

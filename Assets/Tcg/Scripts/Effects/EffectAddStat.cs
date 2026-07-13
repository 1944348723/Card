using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// 效果类：为卡牌或玩家增加或减少基础属性（如攻击力、生命值、法力值）
    /// 继承自 EffectData，可用于一次性或持续效果
    /// </summary>

    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/AddStat", order = 10)]
    public class EffectAddStat : EffectData
    {
        [Header("属性类型")]
        public EffectStatType type; // 指定要影响的属性类型（攻击力/生命/法力）

        /// <summary>
        /// 执行效果：影响玩家属性
        /// </summary>
        /// <param name="logic">游戏逻辑管理器</param>
        /// <param name="ability">触发该效果的能力数据</param>
        /// <param name="caster">施放效果的卡牌</param>
        /// <param name="target">目标玩家</param>
        public override void DoEffect(EffectContext logic, AbilityData ability, Card caster, Player target)
        {
            if (type == EffectStatType.HP)
            {
                target.hp += ability.value;       // 增加当前生命
                target.hp_max += ability.value;   // 增加最大生命
            }

            if (type == EffectStatType.Mana)
            {
                target.mana += ability.value;     // 增加当前法力
                target.mana_max += ability.value; // 增加最大法力
                target.mana = Mathf.Max(target.mana, 0); // 保证法力不为负数
                target.mana_max = Mathf.Clamp(target.mana_max, 0, GameplayData.Get().mana_max); // 最大法力不超过设定上限
            }
        }

        /// <summary>
        /// 执行效果：影响卡牌属性
        /// </summary>
        /// <param name="logic">游戏逻辑管理器</param>
        /// <param name="ability">触发该效果的能力数据</param>
        /// <param name="caster">施放效果的卡牌</param>
        /// <param name="target">目标卡牌</param>
        public override void DoEffect(EffectContext logic, AbilityData ability, Card caster, Card target)
        {
            if (type == EffectStatType.Attack)
                target.attack += ability.value; // 增加攻击力
            if (type == EffectStatType.HP)
                target.hp += ability.value;     // 增加生命值
            if (type == EffectStatType.Mana)
                target.mana += ability.value;   // 增加法力
        }

        /// <summary>
        /// 执行持续效果：对卡牌属性产生持续变化
        /// </summary>
        /// <param name="logic">游戏逻辑管理器</param>
        /// <param name="ability">触发该效果的能力数据</param>
        /// <param name="caster">施放效果的卡牌</param>
        /// <param name="target">目标卡牌</param>
        public override void DoOngoingEffect(EffectContext logic, AbilityData ability, Card caster, Card target)
        {
            if (type == EffectStatType.Attack)
                target.attack_ongoing += ability.value; // 持续增加攻击力
            if (type == EffectStatType.HP)
                target.hp_ongoing += ability.value;     // 持续增加生命值
            if (type == EffectStatType.Mana)
                target.mana_ongoing += ability.value;   // 持续增加法力
        }
    }

    /// <summary>
    /// 可被效果影响的基础属性类型
    /// </summary>
    public enum EffectStatType
    {
        None = 0,
        Attack = 10, // 攻击力
        HP = 20,     // 生命值
        Mana = 30,   // 法力值
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// 效果类：为指定的卡牌添加一个能力
    /// 继承自 EffectData，可以被能力系统调用
    /// </summary>

    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/AddAbility", order = 10)]
    public class EffectAddAbility : EffectData
    {
        [Header("能力")]
        public AbilityData gain_ability;  // 需要添加到卡牌的能力

        /// <summary>
        /// 执行效果：为目标卡牌添加能力（一次性效果）
        /// </summary>
        /// <param name="logic">游戏逻辑管理器</param>
        /// <param name="ability">触发该效果的能力数据</param>
        /// <param name="caster">施放效果的卡牌</param>
        /// <param name="target">目标卡牌</param>
        public override void DoEffect(EffectContext logic, AbilityData ability, Card caster, Card target)
        {
            target.AddAbility(gain_ability); // 将指定能力添加到目标卡牌
        }

        /// <summary>
        /// 执行持续效果：为目标卡牌添加持续能力（如持续触发的能力）
        /// </summary>
        /// <param name="logic">游戏逻辑管理器</param>
        /// <param name="ability">触发该效果的能力数据</param>
        /// <param name="caster">施放效果的卡牌</param>
        /// <param name="target">目标卡牌</param>
        public override void DoOngoingEffect(EffectContext logic, AbilityData ability, Card caster, Card target)
        {
            target.AddOngoingAbility(gain_ability); // 将能力添加为持续效果
        }
    }
}

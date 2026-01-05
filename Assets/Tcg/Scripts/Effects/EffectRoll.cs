using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// RollDice 效果说明：
    /// 投掷一个骰子（或生成随机值）。
    /// - dice：骰子的面数（默认 6 面）
    /// - 效果会调用逻辑层生成一个随机值，用于后续技能或效果计算
    /// </summary>

    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/RollDice", order = 10)]
    public class EffectRoll : EffectData
    {
        public int dice = 6;  // 骰子面数，默认 6

        // 对卡牌目标执行投骰子
        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            logic.RollRandomValue(dice);  // 生成随机值
        }

        // 对玩家目标执行投骰子
        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Player target)
        {
            logic.RollRandomValue(dice);  // 生成随机值
        }

        // 对指定槽位执行投骰子（Slot）
        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Slot target)
        {
            logic.RollRandomValue(dice);  // 生成随机值
        }
    }
}
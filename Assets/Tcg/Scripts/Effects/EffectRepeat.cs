using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// 效果说明：
    /// 重复触发指定技能/能力若干次。
    /// - ability：要重复触发的技能
    /// - type：重复次数来源类型（固定值或玩家选择值）
    /// </summary>

    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/Repeat", order = 10)]
    public class EffectRepeat : EffectData
    {
        public AbilityData ability;         // 要重复触发的技能
        public EffectRepeatType type;       // 重复次数的类型（固定值或选择值）

        // 无目标版本
        public override void DoEffect(GameLogic logic, AbilityData iability, Card caster)
        {
            int count = GetRepeatCount(logic.GameData, iability); // 获取重复次数
            for (int i = 0; i < count; i++)
            {
                Card triggerer = logic.GameData.GetCard(logic.GameData.ability_triggerer); // 获取触发技能的来源卡
                logic.TriggerAbilityDelayed(this.ability, caster, triggerer); // 延迟触发技能
            }
        }

        // 玩家目标版本
        public override void DoEffect(GameLogic logic, AbilityData iability, Card caster, Player target)
        {
            int count = GetRepeatCount(logic.GameData, iability); // 获取重复次数
            for (int i = 0; i < count; i++)
            {
                Card triggerer = logic.GameData.GetCard(logic.GameData.ability_triggerer);
                logic.TriggerAbilityDelayed(this.ability, caster, triggerer); // 延迟触发技能
            }
        }

        // 卡牌目标版本
        public override void DoEffect(GameLogic logic, AbilityData iability, Card caster, Card target)
        {
            int count = GetRepeatCount(logic.GameData, iability); // 获取重复次数
            for (int i = 0; i < count; i++)
            {
                Card triggerer = logic.GameData.GetCard(logic.GameData.ability_triggerer);
                logic.TriggerAbilityDelayed(this.ability, caster, triggerer); // 延迟触发技能
            }
        }

        // 根据类型获取重复次数
        public int GetRepeatCount(Game game, AbilityData iability)
        {
            if (type == EffectRepeatType.SelectedValue)
                return game.selected_value;  // 玩家选择的值
            if (type == EffectRepeatType.FixedValue)
                return iability.value;       // 技能自身固定值
            return 0;                        // 默认 0 次
        }
    }

    // 重复次数来源类型枚举
    public enum EffectRepeatType
    {
        FixedValue,      // 固定值
        SelectedValue    // 玩家选择的值
    }
}

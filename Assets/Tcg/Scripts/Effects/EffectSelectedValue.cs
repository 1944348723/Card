using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// 效果说明：
    /// 对游戏中的“已选择值”（selected_value）进行增加或减少操作。
    /// - oper：操作类型（增加、减少、赋值等）
    /// - value：操作的数值
    /// - 可作用于玩家或卡牌目标
    /// </summary>

    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/SelectedValue", order = 10)]
    public class EffectSelectedValue : EffectData
    {
        public EffectOperatorInt oper;  // 操作类型（Add, Subtract, Set 等）
        public int value;               // 操作数值

        // 对玩家目标执行效果
        public override void DoEffect(EffectContext logic, AbilityData ability, Card caster, Player target)
        {
            // 根据操作类型更新 selected_value
            logic.GameData.SetSelectedValue(AddOrSet(logic.GameData.Selection.SelectedValue, oper, value));
        }

        // 对卡牌目标执行效果
        public override void DoEffect(EffectContext logic, AbilityData ability, Card caster, Card target)
        {
            // 根据操作类型更新 selected_value
            logic.GameData.SetSelectedValue(AddOrSet(logic.GameData.Selection.SelectedValue, oper, value));
        }

    }

}

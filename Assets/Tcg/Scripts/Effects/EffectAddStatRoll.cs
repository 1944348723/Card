using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// Effect that adds or removes basic card/player stats such as hp, attack, mana, by the value of the dice roll
    /// 作用效果：根据骰子结果（rolled_value），为玩家或卡牌增加/减少基础属性（生命、攻击、法力）
    /// </summary>

    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/AddStatRoll", order = 10)]
    public class EffectAddStatRoll : EffectData
    {
        public EffectStatType type;   // 要修改的属性类型（HP / Attack / Mana）

        // 对“玩家目标”生效的效果
        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Player target)
        {
            Game data = logic.GetGameData();   // 获取当前游戏数据（里面存储骰子数值 rolled_value）

            // 如果修改生命
            if (type == EffectStatType.HP)
            {
                target.hp += data.rolled_value;       // 当前生命值增加骰子点数
                target.hp_max += data.rolled_value;   // 最大生命值同时也增加
            }

            // 如果修改法力
            if (type == EffectStatType.Mana)
            {
                target.mana += data.rolled_value;         // 当前法力增加
                target.mana_max += data.rolled_value;     // 最大法力也增加

                target.mana = Mathf.Max(target.mana, 0);  // 当前法力不能低于 0
                // 最大法力被限制在 0 ~ 系统允许最大值之间
                target.mana_max = Mathf.Clamp(target.mana_max, 0, GameplayData.Get().mana_max);
            }
        }

        // 对“卡牌目标”生效的效果
        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            Game data = logic.GetGameData();   // 获取骰子结果

            // 增加攻击
            if (type == EffectStatType.Attack)
                target.attack += data.rolled_value;

            // 增加生命
            if (type == EffectStatType.HP)
                target.hp += data.rolled_value;

            // 增加法力
            if (type == EffectStatType.Mana)
                target.mana += data.rolled_value;
        }
    }
}

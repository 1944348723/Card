using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// 效果说明：
    /// 将目标卡牌的所有属性（攻击力、生命值、法力值等）重置为初始值（CardData 中的默认值）。
    /// - 会根据卡牌的 CardData 和 VariantData 恢复原始状态
    /// </summary>

    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/ResetStat", order = 10)]
    public class EffectResetStat : EffectData
    {
        // 对卡牌执行重置效果
        public override void DoEffect(EffectContext logic, AbilityData ability, Card caster, Card target)
        {
            target.SetCard(target.CardData, target.VariantData);  // 重置卡牌属性为原始值
        }
    }
}

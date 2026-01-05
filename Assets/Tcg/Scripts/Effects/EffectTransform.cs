using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// 效果说明：
    /// 将目标卡牌转换为另一张卡牌（替换原有卡牌）。
    /// - transform_to：要转换成的目标卡牌数据
    /// - 调用逻辑层的 TransformCard 方法实现卡牌转换
    /// </summary>

    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/Transform", order = 10)]
    public class EffectTransform : EffectData
    {
        public CardData transform_to; // 要转换成的卡牌数据

        // 对卡牌目标执行转换效果
        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            logic.TransformCard(target, transform_to); // 执行卡牌转换
        }
    }
}
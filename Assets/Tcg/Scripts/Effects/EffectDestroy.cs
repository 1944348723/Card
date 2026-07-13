using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/Destroy", order = 10)]
    public class EffectDestroy : EffectData
    {
        /// <summary>
        /// Destroy 效果说明：
        /// 销毁目标卡牌。
        /// - 如果卡牌当前在战场上（Board），则执行“击杀/破坏”逻辑；
        /// - 如果卡牌不在战场（例如在手牌、牌库、临时区等），则改为丢入弃牌堆。
        /// </summary>

        public override void DoEffect(EffectContext logic, AbilityData ability, Card caster, Card target)
        {
            // 判断目标卡牌是否在场上
            if (logic.GameData.IsOnBoard(target))
                logic.KillCard(caster, target);   // 在战场 → 直接击杀
            else
                logic.DiscardCard(target);        // 不在战场 → 丢弃
        }

    }
}

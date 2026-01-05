using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/DestroyEquip", order = 10)]
    public class EffectDestroyEquip : EffectData
    {
        /// <summary>
        /// DestroyEquip 效果说明：
        /// 销毁目标卡牌的装备。
        /// - 如果目标卡本身是装备卡，则直接丢弃该卡；
        /// - 如果目标卡不是装备卡，则丢弃它所装备的装备卡（根据 equipped_uid 查找）。
        /// </summary>

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            // 如果目标卡是装备卡
            if (target.CardData.IsEquipment())
            {
                logic.DiscardCard(target);  // 直接丢弃
            }
            else
            {
                // 否则获取目标卡所装备的装备卡
                Card etarget = logic.GameData.GetCard(target.equipped_uid);

                // 丢弃该装备卡
                logic.DiscardCard(etarget);
            }
        }

    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// 效果类：根据指定牌堆（手牌/场上/牌库等）中符合条件的卡牌数量，动态计算数值，并设置目标属性
    /// 可用于卡牌或玩家属性（攻击力、生命值、法力值）
    /// </summary>
    
    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/AddStatCount", order = 10)]
    public class EffectAddStatCount : EffectData
    {
        [Header("属性类型")]
        public EffectStatType type; // 目标属性类型（攻击/生命/法力）
        public PileType pile;       // 计算数量的牌堆类型

        [Header("数量条件")]
        public CardType has_type;   // 仅计算指定卡牌类型
        public TeamData has_team;   // 仅计算指定阵营的卡牌
        public TraitData has_trait; // 仅计算拥有指定特性的卡牌

        /// <summary>
        /// 执行效果：作用于玩家属性
        /// </summary>
        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Player target)
        {
            int val = GetCount(logic.GetGameData(), caster) * ability.value; // 计算数量乘以能力值
            if (type == EffectStatType.HP)
            {
                target.hp += val;       // 增加当前生命
                target.hp_max += ability.value; // 增加最大生命
            }

            if (type == EffectStatType.Mana)
            {
                target.mana += val;       // 增加当前法力
                target.mana_max += val;   // 增加最大法力
                target.mana = Mathf.Max(target.mana, 0); // 保证法力不为负数
                target.mana_max = Mathf.Clamp(target.mana_max, 0, GameplayData.Get().mana_max); // 限制最大法力
            }
        }

        /// <summary>
        /// 执行效果：作用于卡牌属性
        /// </summary>
        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            int val = GetCount(logic.GetGameData(), caster) * ability.value;
            if (type == EffectStatType.Attack)
                target.attack += val;
            if (type == EffectStatType.HP)
                target.hp += val;
            if (type == EffectStatType.Mana)
                target.mana += val;
        }

        /// <summary>
        /// 执行持续效果：作用于卡牌属性（持续生效）
        /// </summary>
        public override void DoOngoingEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            int val = GetCount(logic.GetGameData(), caster) * ability.value;
            if (type == EffectStatType.Attack)
                target.attack_ongoing += val;
            if (type == EffectStatType.HP)
                target.hp_ongoing += val;
            if (type == EffectStatType.Mana)
                target.mana_ongoing += val;
        }

        /// <summary>
        /// 获取指定卡牌堆中符合条件的卡牌数量
        /// </summary>
        private int GetCount(Game data, Card caster)
        {
            Player player = data.GetPlayer(caster.player_id);
            return CountPile(player, pile);
        }

        /// <summary>
        /// 统计指定牌堆中符合条件的卡牌数量
        /// </summary>
        private int CountPile(Player player, PileType pile)
        {
            List<Card> card_pile = null;

            // 选择牌堆
            if (pile == PileType.Hand) card_pile = player.cards_hand;
            if (pile == PileType.Board) card_pile = player.cards_board;
            if (pile == PileType.Equipped) card_pile = player.cards_equip;
            if (pile == PileType.Deck) card_pile = player.cards_deck;
            if (pile == PileType.Discard) card_pile = player.cards_discard;
            if (pile == PileType.Secret) card_pile = player.cards_secret;
            if (pile == PileType.Temp) card_pile = player.cards_temp;

            // 统计符合条件的卡牌数量
            if (card_pile != null)
            {
                int count = 0;
                foreach (Card card in card_pile)
                {
                    if (IsTrait(card))
                        count++;
                }
                return count;
            }
            return 0;
        }

        /// <summary>
        /// 判断卡牌是否符合条件（类型/阵营/特性）
        /// </summary>
        private bool IsTrait(Card card)
        {
            bool is_type = card.CardData.type == has_type || has_type == CardType.None;
            bool is_team = card.CardData.team == has_team || has_team == null;
            bool is_trait = card.HasTrait(has_trait) || has_trait == null;
            return (is_type && is_team && is_trait);
        }
    }
}

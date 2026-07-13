using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// 玩家类型枚举
    /// </summary>
    public enum ConditionPlayerType
    {
        Self = 0,       // 自己
        Opponent = 1,   // 对手
        Both = 2,       // 双方
    }

    /// <summary>
    /// 触发条件：统计指定玩家牌堆中的卡牌数量
    /// 可以指定牌堆类型（手牌/场上/牌组/弃牌堆/装备/秘密/临时）  
    /// 也可以只统计符合特定类型/队伍/特性的卡牌
    /// </summary>
    [CreateAssetMenu(fileName = "condition", menuName = "TcgEngine/Condition/Count", order = 10)]
    public class ConditionCount : ConditionData
    {
        [Header("Count cards of type")]
        // 要统计的玩家类型（自己/对手/双方）
        public ConditionPlayerType target;

        // 要统计的牌堆类型
        public CardZone pile;

        // 数值比较运算符（大于/小于/等于等）
        public ConditionOperatorInt oper;

        // 比较值
        public int value;

        [Header("Traits")]
        // 仅统计指定卡牌类型
        public CardType has_type;

        // 仅统计指定队伍
        public TeamData has_team;

        // 仅统计指定特性
        public TraitData has_trait;

        /// <summary>
        /// 判断触发条件是否满足
        /// </summary>
        /// <param name="data">游戏数据上下文</param>
        /// <param name="ability">技能/效果数据</param>
        /// <param name="caster">施法者卡牌</param>
        /// <returns>是否满足触发条件</returns>
        public override bool IsTriggerConditionMet(Game data, AbilityData ability, Card caster)
        {
            int count = 0;

            // 统计自己或双方的牌堆
            if (target == ConditionPlayerType.Self || target == ConditionPlayerType.Both)
            {
                Player player = data.GetPlayer(caster.player_id);
                count += CountPile(player, pile);
            }

            // 统计对手或双方的牌堆
            if (target == ConditionPlayerType.Opponent || target == ConditionPlayerType.Both)
            {
                Player player = data.GetOpponentPlayer(caster.player_id);
                count += CountPile(player, pile);
            }

            // 根据运算符和目标值判断是否满足条件
            return CompareInt(count, oper, value);
        }

        /// <summary>
        /// 统计指定玩家的指定牌堆中符合条件的卡牌数量
        /// </summary>
        private int CountPile(Player player, CardZone pile)
        {
            List<Card> card_pile = null;

            // 获取指定牌堆
            if (pile == CardZone.Hand)
                card_pile = player.cards_hand;
            if (pile == CardZone.Board)
                card_pile = player.cards_board;
            if (pile == CardZone.Equip)
                card_pile = player.cards_equip;
            if (pile == CardZone.Deck)
                card_pile = player.cards_deck;
            if (pile == CardZone.Discard)
                card_pile = player.cards_discard;
            if (pile == CardZone.Secret)
                card_pile = player.cards_secret;
            if (pile == CardZone.Temp)
                card_pile = player.cards_temp;

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
        /// 判断卡牌是否符合指定类型、队伍和特性
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

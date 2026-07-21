using System;

namespace TcgEngine.AI
{
    /// <summary>
    /// 评估一个游戏局面对指定 AI 玩家是否有利。
    /// 分数越高越有利于 AI，越低越有利于对手。
    /// </summary>
    public sealed class AIHeuristic
    {
        private const int WinScore = 100000;
        // 在胜负相同的情况下，优先更快获胜的路线或者必败时拖延失败
        private const int TurnDepthScore = 1000;

        public int BoardCardValue { get; set; } = 20;
        public int SecretCardValue { get; set; } = 10;
        public int HandCardValue { get; set; } = 5;
        public int KillValue { get; set; } = 5;
        public int PlayerHpValue { get; set; } = 4;
        public int CardAttackValue { get; set; } = 3;
        public int CardHpValue { get; set; } = 2;
        public int CardStatusValue { get; set; } = 15;

        private readonly int aiPlayerId;
        private readonly int scoreVariance;
        private readonly Random random;

        public AIHeuristic(int playerId, int level)
        {
            aiPlayerId = playerId;
            scoreVariance = GetScoreVariance(level);
            random = new Random();
        }

        /// <summary>
        /// 计算完整局面的启发式分数。
        /// 常规局面应落在 -10000 到 10000 之间，终局使用更大的绝对值。
        /// </summary>
        public int Calculate(Game game, int turnDepth)
        {
            Player aiPlayer = game.GetPlayer(aiPlayerId);
            Player opponent = game.GetOpponentPlayer(aiPlayerId);
            int score = 0;

            // 终局
            if (aiPlayer.IsDead())  score -= WinScore - turnDepth * TurnDepthScore;
            if (opponent.IsDead())  score += WinScore - turnDepth * TurnDepthScore;

            score += CalculatePlayerScore(aiPlayer);
            score -= CalculatePlayerScore(opponent);

            if (scoreVariance > 0)
                score += random.Next(-scoreVariance, scoreVariance);

            return score;
        }

        private int CalculatePlayerScore(Player player)
        {
            int score = 0;
            score += player.cards_board.Count * BoardCardValue;
            score += player.cards_equip.Count * BoardCardValue;
            score += player.cards_secret.Count * SecretCardValue;
            score += player.cards_hand.Count * HandCardValue;
            score += player.kill_count * KillValue;
            score += player.hp * PlayerHpValue;

            foreach (Card card in player.cards_board)
                score += CalculateBoardCardScore(card);

            return score;
        }

        private int CalculateBoardCardScore(Card card)
        {
            int score = card.GetAttack() * CardAttackValue;
            score += card.GetHP() * CardHpValue;

            foreach (CardStatus status in card.status)
                score += status.StatusData.hvalue * CardStatusValue;
            foreach (CardStatus status in card.ongoing_status)
                score += status.StatusData.hvalue * CardStatusValue;

            return score;
        }

        /// <summary>
        /// AI 等级越低，局面评分的随机波动越大。
        /// </summary>
        private static int GetScoreVariance(int level)
        {
            return level switch
            {
                9 => 5,
                8 => 10,
                7 => 20,
                6 => 30,
                5 => 40,
                4 => 50,
                3 => 75,
                2 => 100,
                _ => level <= 1 ? 200 : 0,
            };

        }
    }

    /// <summary>
    /// 为搜索阶段的候选动作计算优先级和固定顺序。
    /// 这些值只服务于分支筛选与去重，不代表游戏局面的最终价值。
    /// </summary>
    internal sealed class AIActionEvaluator
    {
        private readonly int aiPlayerId;

        public AIActionEvaluator(int playerId)
        {
            aiPlayerId = playerId;
        }

        /// <summary>
        /// 计算候选动作的优先级，候选过多时优先搜索高分动作。
        /// </summary>
        public int CalculatePriority(Game game, AIAction action)
        {
            if (action.type == GameAction.EndTurn || action.type == GameAction.CancelSelect)
                return 0;

            if (action.type == GameAction.CastAbility)
                return 200;

            if (action.type == GameAction.Attack)
            {
                Card attacker = game.GetCard(action.card_uid);
                Card target = game.GetCard(action.target_uid);
                int attackScore = attacker.GetAttack() >= target.GetHP() ? 300 : 100;
                int retaliationScore = target.GetAttack() >= attacker.GetHP() ? -200 : 0;
                return attackScore + retaliationScore + target.GetAttack() * 5;
            }

            if (action.type == GameAction.AttackPlayer)
            {
                Card attacker = game.GetCard(action.card_uid);
                Player target = game.GetPlayer(action.target_player_id);
                int attackScore = attacker.GetAttack() >= target.hp ? 500 : 200;
                return attackScore + attacker.GetAttack() * 10 - target.hp;
            }

            if (action.type == GameAction.PlayCard)
            {
                Player player = game.GetPlayer(aiPlayerId);
                Card card = game.GetCard(action.card_uid);
                int score = 200 + card.GetMana() * 5;

                if (card.CardData.IsBoardCard())
                    return score - 30 * player.cards_board.Count;
                if (card.CardData.IsEquipment())
                    return score - 30 * player.cards_equip.Count;

                return score;
            }

            if (action.type == GameAction.Move)
                return 100;

            return 100;
        }

        /// <summary>
        /// 计算同一回合内动作的固定顺序，避免重复搜索 A→B 与 B→A。
        /// 返回 0 的动作不参与顺序限制。
        /// </summary>
        public int CalculateOrder(Game game, AIAction action)
        {
            if (action.type == GameAction.EndTurn || game.selector != SelectorType.None)
                return 0;

            Card card = game.GetCard(action.card_uid);
            Card target = action.target_uid != null ? game.GetCard(action.target_uid) : null;
            bool isSpell = card != null && !card.CardData.IsBoardCard();

            int typeOrder = GetTypeOrder(action.type, isSpell);
            int cardOrder = card != null ? card.Hash % 100 : 0;
            int targetOrder = target != null ? target.Hash % 100 : 0;
            return typeOrder * 10000 + cardOrder * 100 + targetOrder + 1;
        }

        private static int GetTypeOrder(ushort actionType, bool isSpell)
        {
            if (actionType == GameAction.PlayCard)
                return isSpell ? 1 : 7;
            if (actionType == GameAction.CastAbility)
                return 2;
            if (actionType == GameAction.Move)
                return 3;
            if (actionType == GameAction.Attack)
                return 4;
            if (actionType == GameAction.AttackPlayer)
                return 5;

            return 0;
        }
    }
}

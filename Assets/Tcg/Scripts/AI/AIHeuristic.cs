using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine.AI
{
    /// <summary>
    /// AI 决策用到的数值与计算方式，调整这些参数可以改进AI
    /// Heuristic（启发式）：表示一个棋盘/局面状态的评分，分数高代表有利于 AI，分数低代表有利于对手
    /// Action Score（行动得分）：表示某一个具体行动的评分，如果在某状态下行动太多，将优先选择分高的行动
    /// Action Sort Order（行动排序值）：用于决定一个回合内行动执行的顺序，
    /// 可以避免同一种结果被不同顺序重复搜索；按从小到大顺序执行
    /// </summary>

    public class AIHeuristic
    {
        // ---------- 启发式参数 -------------

        public int board_card_value = 20;      // 场上随从卡的价值分
        public int secret_card_value = 10;     // 秘密卡区域卡牌的价值分
        public int hand_card_value = 5;        // 手牌卡牌的价值分
        public int kill_value = 5;             // 击杀卡牌的价值分

        public int player_hp_value = 4;        // 每一点玩家血量的价值
        public int card_attack_value = 3;      // 每一点随从攻击力价值
        public int card_hp_value = 2;          // 每一点随从生命值价值
        public int card_status_value = 15;     // 每一个状态的价值（乘以 StatusData 的 hvalue）

        // ----------

        private int ai_player_id;          // 当前 AI 的玩家 ID，通常玩家是 0，AI 是 1
        private int ai_level;              // AI 等级（10 最强，1 最弱）
        private int heuristic_modifier;    // 低等级 AI 的启发式随机波动值
        private System.Random random_gen;

        public AIHeuristic(int player_id, int level)
        {
            ai_player_id = player_id;
            ai_level = level;
            heuristic_modifier = GetHeuristicModifier();
            random_gen = new System.Random();
        }

        public int CalculateHeuristic(Game data, NodeState node)
        {
            Player aiplayer = data.GetPlayer(ai_player_id);
            Player oplayer = data.GetOpponentPlayer(ai_player_id);
            return CalculateHeuristic(data, node, aiplayer, oplayer);
        }

        // 计算完整的启发式分值
        // 应当返回 -10000 到 10000 之间（除非胜利）
        public int CalculateHeuristic(Game data, NodeState node, Player aiplayer, Player oplayer)
        {
            int score = 0;

            // 胜负判断
            if (aiplayer.IsDead())
                score += -100000 + node.tdepth * 1000; // AI 死了 → 极低分，并加上深度希望“死得更晚”
            if (oplayer.IsDead())
                score += 100000 - node.tdepth * 1000;  // 对手死了 → 极高分，并减去深度希望“更快赢”

            // 场面状态
            score += aiplayer.cards_board.Count * board_card_value;
            score += aiplayer.cards_equip.Count * board_card_value;
            score += aiplayer.cards_secret.Count * secret_card_value;
            score += aiplayer.cards_hand.Count * hand_card_value;
            score += aiplayer.kill_count * kill_value;
            score += aiplayer.hp * player_hp_value;

            score -= oplayer.cards_board.Count * board_card_value;
            score -= oplayer.cards_equip.Count * board_card_value;
            score -= oplayer.cards_secret.Count * secret_card_value;
            score -= oplayer.cards_hand.Count * hand_card_value;
            score -= oplayer.kill_count * kill_value;
            score -= oplayer.hp * player_hp_value;

            // 随从属性评分
            foreach (Card card in aiplayer.cards_board)
            {
                score += card.GetAttack() * card_attack_value;
                score += card.GetHP() * card_hp_value;

                foreach (CardStatus status in card.status)
                    score += status.StatusData.hvalue * card_status_value;
                foreach (CardStatus status in card.ongoing_status)
                    score += status.StatusData.hvalue * card_status_value;
            }

            foreach (Card card in oplayer.cards_board)
            {
                score -= card.GetAttack() * card_attack_value;
                score -= card.GetHP() * card_hp_value;

                foreach (CardStatus status in card.status)
                    score -= status.StatusData.hvalue * card_status_value;
                foreach (CardStatus status in card.ongoing_status)
                    score -= status.StatusData.hvalue * card_status_value;
            }

            // 低等级 AI 加点随机数
            if (heuristic_modifier > 0)
                score += random_gen.Next(-heuristic_modifier, heuristic_modifier);

            return score;
        }

        // 计算单个行动的评分（而不是整局状态）
        // 当某状态下行动太多时，只会评估得分较高的行动
        // 必须返回正值
        public int CalculateActionScore(Game data, AIAction order)
        {
            if (order.type == GameAction.EndTurn)
                return 0;   // 结束回合通常最差

            if (order.type == GameAction.CancelSelect)
                return 0;   // 取消通常也不优先

            if (order.type == GameAction.CastAbility)
            {
                return 200;
            }

            if (order.type == GameAction.Attack)
            {
                Card card = data.GetCard(order.card_uid);
                Card target = data.GetCard(order.target_uid);
                int ascore = card.GetAttack() >= target.GetHP() ? 300 : 100; // 是否能击杀目标？
                int oscore = target.GetAttack() >= card.GetHP() ? -200 : 0; // 自己会不会被反杀？
                return ascore + oscore + target.GetAttack() * 5;           // 优先清高攻击单位
            }

            if (order.type == GameAction.AttackPlayer)
            {
                Card card = data.GetCard(order.card_uid);
                Player player = data.GetPlayer(order.target_player_id);
                int ascore = card.GetAttack() >= player.hp ? 500 : 200;    // 是否能直接击杀玩家？
                return ascore + (card.GetAttack() * 10) - player.hp;       // 攻击越高越好
            }

            if (order.type == GameAction.PlayCard)
            {
                Player player = data.GetPlayer(ai_player_id);
                Card card = data.GetCard(order.card_uid);
                if (card.CardData.IsBoardCard())
                    return 200 + (card.GetMana() * 5) - (30 * player.cards_board.Count); // 高费用更好，且场上越少越优先打
                else if (card.CardData.IsEquipment())
                    return 200 + (card.GetMana() * 5) - (30 * player.cards_equip.Count);
                else
                    return 200 + (card.GetMana() * 5);
            }

            if (order.type == GameAction.Move)
            {
                return 100;
            }

            return 100; // 其他动作一般也比结束/取消好
        }

        // 同一回合内，动作只能按排序值执行
        // 防止计算 A→B→C / B→C→A / C→A→B 这种重复排列
        // 0 或相同排序值 → AI 会尝试所有排列（变慢）
        public int CalculateActionSort(Game data, AIAction order)
        {
            if (order.type == GameAction.EndTurn)
                return 0; // 结束回合永远可执行
            if (data.selector != SelectorType.None)
                return 0; // 选择器操作不参与排序

            Card card = data.GetCard(order.card_uid);
            Card target = order.target_uid != null ? data.GetCard(order.target_uid) : null;
            bool is_spell = card != null && !card.CardData.IsBoardCard();

            int type_sort = 0;
            if (order.type == GameAction.PlayCard && is_spell)
                type_sort = 1; // 先放法术
            if (order.type == GameAction.CastAbility)
                type_sort = 2; // 再用技能
            if (order.type == GameAction.Move)
                type_sort = 3; // 再移动
            if (order.type == GameAction.Attack)
                type_sort = 4; // 再攻击随从
            if (order.type == GameAction.AttackPlayer)
                type_sort = 5; // 再打玩家
            if (order.type == GameAction.PlayCard && !is_spell)
                type_sort = 7; // 生物最后上

            int card_sort = card != null ? (card.Hash % 100) : 0;
            int target_sort = target != null ? (target.Hash % 100) : 0;
            int sort = type_sort * 10000 + card_sort * 100 + target_sort + 1;
            return sort;
        }

        // 低等级 AI 启发式随机波动范围
        private int GetHeuristicModifier()
        {
            if (ai_level >= 10)
                return 0;
            if (ai_level == 9)
                return 5;
            if (ai_level == 8)
                return 10;
            if (ai_level == 7)
                return 20;
            if (ai_level == 6)
                return 30;
            if (ai_level == 5)
                return 40;
            if (ai_level == 4)
                return 50;
            if (ai_level == 3)
                return 75;
            if (ai_level == 2)
                return 100;
            if (ai_level <= 1)
                return 200;
            return 0;
        }

        // 判断该节点是否代表某方胜利
        public bool IsWin(NodeState node)
        {
            return node.hvalue > 50000 || node.hvalue < -50000;
        }

    }
}

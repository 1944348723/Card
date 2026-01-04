using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine.AI
{
    /// <summary>
    /// 使用 Minimax 算法的 AI 玩家
    /// </summary>
    public class AIPlayerMM : AIPlayer
    {
        private AILogic ai_logic;        // AI 核心逻辑（Minimax + alpha-beta 剪枝）
        private bool is_playing = false; // AI 当前是否正在执行动作

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="gameplay">游戏逻辑对象</param>
        /// <param name="id">AI 控制的玩家 ID</param>
        /// <param name="level">AI 等级（1~10）</param>
        public AIPlayerMM(GameLogic gameplay, int id, int level)
        {
            this.gameplay = gameplay;
            player_id = id;
            ai_level = Mathf.Clamp(level, 1, 10); // 限制 AI 等级在 1~10
            ai_logic = AILogic.Create(id, ai_level); // 创建 Minimax AI 核心逻辑
        }

        /// <summary>
        /// 每帧调用更新 AI
        /// </summary>
        public override void Update()
        {
            Game game_data = gameplay.GetGameData();
            Player player = game_data.GetPlayer(player_id);

            // 如果轮到 AI 行动，并且 AI 当前不在执行动作
            if (!is_playing && game_data.IsPlayerTurn(player))
            {
                is_playing = true;
                TimeTool.StartCoroutine(AiTurn()); // 启动协程执行 AI 回合
            }

            // 如果是选牌阶段（Mulligan）且 AI 不在行动，跳过 Mulligan
            if (!is_playing && game_data.IsPlayerMulliganTurn(player))
            {
                SkipMulligan();
            }

            // 如果轮到其他玩家，且 AI 还在运行，则停止 AI
            if (!game_data.IsPlayerTurn(player) && ai_logic.IsRunning())
                Stop();
        }

        /// <summary>
        /// AI 回合执行协程
        /// </summary>
        private IEnumerator AiTurn()
        {
            yield return new WaitForSeconds(1f); // 等待 1 秒，模拟思考时间

            Game game_data = gameplay.GetGameData();
            ai_logic.RunAI(game_data); // 运行 Minimax 算法

            // 等待 AI 执行完成
            while (ai_logic.IsRunning())
            {
                yield return new WaitForSeconds(0.1f);
            }

            // 获取 AI 认为最优的动作
            AIAction best = ai_logic.GetBestAction();

            if (best != null)
            {
                Debug.Log("执行 AI 动作: " + best.GetText(game_data) + "\n" + ai_logic.GetNodePath());

                ExecuteAction(best); // 执行动作
            }

            ai_logic.ClearMemory(); // 清理 AI 内存

            yield return new WaitForSeconds(0.5f); // 延迟半秒，模拟回合结束
            is_playing = false;
        }

        /// <summary>
        /// 停止 AI 执行
        /// </summary>
        private void Stop()
        {
            ai_logic.Stop();
            is_playing = false;
        }

        // ---------- 执行动作相关方法 ----------

        /// <summary>
        /// 根据 AIAction 执行动作
        /// </summary>
        private void ExecuteAction(AIAction action)
        {
            if (!CanPlay())
                return;

            switch (action.type)
            {
                case GameAction.PlayCard:        PlayCard(action.card_uid, action.slot); break;
                case GameAction.Attack:          AttackCard(action.card_uid, action.target_uid); break;
                case GameAction.AttackPlayer:    AttackPlayer(action.card_uid, action.target_player_id); break;
                case GameAction.Move:            MoveCard(action.card_uid, action.slot); break;
                case GameAction.CastAbility:     CastAbility(action.card_uid, action.ability_id); break;
                case GameAction.SelectCard:      SelectCard(action.target_uid); break;
                case GameAction.SelectPlayer:    SelectPlayer(action.target_player_id); break;
                case GameAction.SelectSlot:      SelectSlot(action.slot); break;
                case GameAction.SelectChoice:    SelectChoice(action.value); break;
                case GameAction.SelectCost:      SelectCost(action.value); break;
                case GameAction.SelectMulligan:  SkipMulligan(); break;
                case GameAction.CancelSelect:    CancelSelect(); break;
                case GameAction.EndTurn:         EndTurn(); break;
                case GameAction.Resign:          Resign(); break;
            }
        }

        /// <summary>
        /// 出牌
        /// </summary>
        private void PlayCard(string card_uid, Slot slot)
        {
            Game game_data = gameplay.GetGameData();
            Card card = game_data.GetCard(card_uid);
            if (card != null)
            {
                gameplay.PlayCard(card, slot);
            }
        }

        /// <summary>
        /// 移动卡牌
        /// </summary>
        private void MoveCard(string card_uid, Slot slot)
        {
            Game game_data = gameplay.GetGameData();
            Card card = game_data.GetCard(card_uid);
            if (card != null)
            {
                gameplay.MoveCard(card, slot); 
            }
        }

        /// <summary>
        /// 攻击目标卡牌
        /// </summary>
        private void AttackCard(string attacker_uid, string target_uid)
        {
            Game game_data = gameplay.GetGameData();
            Card card = game_data.GetCard(attacker_uid);
            Card target = game_data.GetCard(target_uid);
            if (card != null && target != null)
            {
                gameplay.AttackTarget(card, target);
            }
        }

        /// <summary>
        /// 攻击目标玩家
        /// </summary>
        private void AttackPlayer(string attacker_uid, int target_player_id)
        {
            Game game_data = gameplay.GetGameData();
            Card card = game_data.GetCard(attacker_uid);
            if (card != null)
            {
                Player oplayer = game_data.GetPlayer(target_player_id);
                gameplay.AttackPlayer(card, oplayer);
            }
        }

        /// <summary>
        /// 释放卡牌能力
        /// </summary>
        private void CastAbility(string caster_uid, string ability_id)
        {
            Game game_data = gameplay.GetGameData();
            Card caster = game_data.GetCard(caster_uid);
            AbilityData iability = AbilityData.Get(ability_id);
            if (caster != null && iability != null)
            {
                gameplay.CastAbility(caster, iability);
            }
        }

        /// <summary>
        /// 选择目标卡牌
        /// </summary>
        private void SelectCard(string target_uid)
        {
            Game game_data = gameplay.GetGameData();
            Card target = game_data.GetCard(target_uid);
            if (target != null)
            {
                gameplay.SelectCard(target);
            }
        }

        /// <summary>
        /// 选择目标玩家
        /// </summary>
        private void SelectPlayer(int tplayer_id)
        {
            Game game_data = gameplay.GetGameData();
            Player target = game_data.GetPlayer(tplayer_id);
            if (target != null)
            {
                gameplay.SelectPlayer(target);
            }
        }

        /// <summary>
        /// 选择卡牌槽位
        /// </summary>
        private void SelectSlot(Slot slot)
        {
            if (slot != Slot.None)
            {
                gameplay.SelectSlot(slot);
            }
        }

        /// <summary>
        /// 选择能力链选项
        /// </summary>
        private void SelectChoice(int choice)
        {
            gameplay.SelectChoice(choice);
        }

        /// <summary>
        /// 选择支付法力值
        /// </summary>
        private void SelectCost(int cost)
        {
            gameplay.SelectCost(cost);
        }

        /// <summary>
        /// 取消选择
        /// </summary>
        private void CancelSelect()
        {
            if (CanPlay())
            {
                gameplay.CancelSelection();
            }
        }

        /// <summary>
        /// 跳过 Mulligan 阶段（不换牌）
        /// </summary>
        private void SkipMulligan()
        {
            string[] cards = new string[0]; // 不换牌
            SelectMulligan(cards);
        }

        /// <summary>
        /// 执行 Mulligan
        /// </summary>
        private void SelectMulligan(string[] cards)
        {
            Game game_data = gameplay.GetGameData();
            Player player = game_data.GetPlayer(player_id);
            gameplay.Mulligan(player, cards);
        }

        /// <summary>
        /// 结束回合
        /// </summary>
        private void EndTurn()
        {
            if (CanPlay())
            {
                gameplay.EndTurn();
            }
        }

        /// <summary>
        /// 认输，结束游戏
        /// </summary>
        private void Resign()
        {
            int other = player_id == 0 ? 1 : 0;
            gameplay.EndGame(other);
        }

    }

}

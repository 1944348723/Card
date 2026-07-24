using System.Collections;
using System.Collections.Generic;
using TcgEngine.Gameplay;
using UnityEngine;

namespace TcgEngine.AI
{
    /// <summary>
    /// 完全随机决策的 AI 玩家
    /// 非常弱的 AI，但对于测试卡牌或游戏逻辑非常有用
    /// </summary>
    public class AIPlayerRandom : AIPlayer
    {
        private const float FirstDecisionDelay = 1f;
        private const float DecisionDelay = 0.5f;
        private const int MaxActionsPerTurn = 8;
        private const double EndTurnChance = 0.15;

        private readonly System.Random rand = new();
        private readonly List<AIAction> legalActions = new();
        private readonly AIActionGenerator actionGenerator;

        private bool isRunning = false;
        private int activeTurn = -1;
        private int turnActionCount = 0;

        /// <summary>
        /// 构造函数
        /// </summary>
        public AIPlayerRandom(GameLogic gameplay, int id, int level)
            : base(gameplay, id, level)
        {
            actionGenerator = new AIActionGenerator(gameplay, CreateAction);
        }

        /// <summary>
        /// 每帧更新 AI
        /// </summary>
        public override void Update()
        {
            if (isRunning || gameplay.IsResolving())
                return;

            if (!CanMulligan() && !CanResolveSelection() && !CanTakeAction())
                return;

            isRunning = true;
            TimeTool.StartCoroutine(RunDecision());
        }

        /// <summary>
        /// 每次只处理一个当前决策，完成后由下一帧根据最新游戏状态继续。
        /// </summary>
        private IEnumerator RunDecision()
        {
            try
            {
                yield return new WaitForSeconds(GetDecisionDelay());

                if (gameplay.IsResolving())
                    yield break;

                if (CanMulligan())
                {
                    SelectMulligan();
                    yield break;
                }

                if (CanResolveSelection())
                {
                    ResolveCurrentSelection();
                    yield break;
                }

                if (CanTakeAction())
                    ExecuteNextTurnAction();
            }
            finally
            {
                isRunning = false;
            }
        }

        private float GetDecisionDelay()
        {
            Game gameData = gameplay.GetGameData();
            bool isFirstTurnAction = CanTakeAction() && gameData.turn_count != activeTurn;
            return isFirstTurnAction ? FirstDecisionDelay : DecisionDelay;
        }

        private void ExecuteNextTurnAction()
        {
            Game gameData = gameplay.GetGameData();
            if (activeTurn != gameData.turn_count)
            {
                activeTurn = gameData.turn_count;
                turnActionCount = 0;
            }

            CollectLegalActions(gameData, legalActions);

            bool reachedActionLimit = turnActionCount >= MaxActionsPerTurn;
            bool randomlyEndTurn = turnActionCount > 0 && rand.NextDouble() < EndTurnChance;
            if (legalActions.Count == 0 || reachedActionLimit || randomlyEndTurn)
            {
                EndTurn();
                return;
            }

            AIAction action = legalActions[rand.Next(0, legalActions.Count)];
            if (TryExecuteAction(action))
                turnActionCount++;
        }

        private void ResolveCurrentSelection()
        {
            Game gameData = gameplay.GetGameData();
            legalActions.Clear();
            actionGenerator.AddSelectionActions(legalActions, gameData, 0);

            if (legalActions.Count > 0)
            {
                AIAction action = legalActions[rand.Next(0, legalActions.Count)];
                TryExecuteAction(action);
            }
        }

        // ---------- 普通回合动作 ----------

        private void CollectLegalActions(Game gameData, List<AIAction> actions)
        {
            actions.Clear();

            Player player = gameData.GetPlayer(PlayerId);
            if (player == null || !gameplay.Rules.IsPlayerActionTurn(player))
                return;

            foreach (Card card in player.cards_hand)
            {
                actionGenerator.AddPlayCardActions(
                    actions,
                    gameData,
                    card,
                    AIPlayPositionMode.AllLegalSlots);
            }

            foreach (Card card in player.cards_board)
            {
                actionGenerator.AddAttackActions(actions, gameData, card);
                actionGenerator.AddMoveActions(actions, gameData, card);
                actionGenerator.AddActivatedAbilityActions(actions, gameData, card);
            }

            foreach (Card card in player.cards_equip)
                actionGenerator.AddActivatedAbilityActions(actions, gameData, card);

            if (player.hero != null)
                actionGenerator.AddActivatedAbilityActions(actions, gameData, player.hero);
        }

        private static AIAction CreateAction(ushort type, Card card)
        {
            return new AIAction(type)
            {
                card_uid = card?.uid,
                target_player_id = -1,
                slot = Slot.None,
                value = -1,
                valid = true,
            };
        }

        /// <summary>
        /// Mulligan 阶段随机选择（不换牌）
        /// </summary>
        private void SelectMulligan()
        {
            TryExecuteAction(new AIAction(GameAction.SelectMulligan));
        }

        private void EndTurn()
        {
            TryExecuteAction(new AIAction(GameAction.EndTurn));
        }
    }

}

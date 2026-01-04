using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Client;
using TcgEngine.Gameplay;

namespace TcgEngine.AI
{
    /// <summary>
    /// 完全随机决策的 AI 玩家
    /// 非常弱的 AI，但对于测试卡牌或游戏逻辑非常有用
    /// </summary>
    public class AIPlayerRandom : AIPlayer
    {
        private bool is_playing = false;    // AI 是否正在行动
        private bool is_selecting = false;  // AI 是否正在选择目标/卡牌

        private System.Random rand = new System.Random(); // 随机数生成器

        /// <summary>
        /// 构造函数
        /// </summary>
        public AIPlayerRandom(GameLogic gameplay, int id, int level)
        {
            this.gameplay = gameplay;
            player_id = id;
        }

        /// <summary>
        /// 每帧更新 AI
        /// </summary>
        public override void Update()
        {
            if (!CanPlay()) // 如果 AI 不能行动，则返回
                return;

            Game game_data = gameplay.GetGameData();
            Player player = game_data.GetPlayer(player_id);

            // 如果轮到 AI 行动，并且当前没有正在解析的动作
            if (game_data.IsPlayerTurn(player) && !gameplay.IsResolving())
            {
                // 如果 AI 不是在行动，并且没有选择器，轮到 AI
                if(!is_playing && game_data.selector == SelectorType.None && game_data.current_player == player_id)
                {
                    is_playing = true;
                    TimeTool.StartCoroutine(AiTurn()); // 启动 AI 回合协程
                }

                // 如果需要选择目标/卡牌
                if (!is_selecting && game_data.selector != SelectorType.None && game_data.selector_player_id == player_id)
                {
                    if (game_data.selector == SelectorType.SelectTarget)
                    {
                        is_selecting = true;
                        TimeTool.StartCoroutine(AiSelectTarget());
                    }

                    if (game_data.selector == SelectorType.SelectorCard)
                    {
                        is_selecting = true;
                        TimeTool.StartCoroutine(AiSelectCard());
                    }

                    if (game_data.selector == SelectorType.SelectorChoice)
                    {
                        is_selecting = true;
                        TimeTool.StartCoroutine(AiSelectChoice());
                    }

                    if (game_data.selector == SelectorType.SelectorCost)
                    {
                        is_selecting = true;
                        TimeTool.StartCoroutine(AiSelectCost());
                    }
                }
            }

            // 如果是 Mulligan 阶段，AI 自动选择
            if (!is_selecting && game_data.IsPlayerMulliganTurn(player))
            {
                is_selecting = true;
                TimeTool.StartCoroutine(AiSelectMulligan());
            }
        }

        // ---------- AI 回合协程 ----------

        /// <summary>
        /// AI 回合协程，随机出牌、攻击、结束回合
        /// </summary>
        private IEnumerator AiTurn()
        {
            yield return new WaitForSeconds(1f);

            PlayCard(); // 随机出牌
            yield return new WaitForSeconds(0.5f);
            PlayCard();
            yield return new WaitForSeconds(0.5f);
            PlayCard();

            Attack(); // 随机攻击
            yield return new WaitForSeconds(0.5f);
            Attack();
            yield return new WaitForSeconds(0.5f);

            AttackPlayer(); // 随机攻击对方玩家
            yield return new WaitForSeconds(0.5f);

            EndTurn(); // 结束回合

            is_playing = false;
        }

        // ---------- 选择协程 ----------

        private IEnumerator AiSelectCard()
        {
            yield return new WaitForSeconds(0.5f);
            SelectCard();    // 随机选择卡牌
            yield return new WaitForSeconds(0.5f);
            CancelSelect();  // 取消选择（保证流程继续）
            is_selecting = false;
        }

        private IEnumerator AiSelectTarget()
        {
            yield return new WaitForSeconds(0.5f);
            SelectTarget();  
            yield return new WaitForSeconds(0.5f);
            CancelSelect();
            is_selecting = false;
        }

        private IEnumerator AiSelectChoice()
        {
            yield return new WaitForSeconds(0.5f);
            SelectChoice();  
            yield return new WaitForSeconds(0.5f);
            CancelSelect();
            is_selecting = false;
        }

        private IEnumerator AiSelectCost()
        {
            yield return new WaitForSeconds(0.5f);
            SelectCost();    
            yield return new WaitForSeconds(0.5f);
            CancelSelect();
            is_selecting = false;
        }

        private IEnumerator AiSelectMulligan()
        {
            yield return new WaitForSeconds(0.5f);
            SelectMulligan();  
            yield return new WaitForSeconds(0.5f);
            is_selecting = false;
        }

        // ---------- 随机动作方法 ----------

        /// <summary>
        /// 随机出手卡牌
        /// </summary>
        public void PlayCard()
        {
            if (!CanPlay())
                return;

            Game game_data = gameplay.GetGameData();
            Player player = game_data.GetPlayer(player_id);

            if (player.cards_hand.Count > 0 && game_data.IsPlayerActionTurn(player))
            {
                Card random = player.GetRandomCard(player.cards_hand, rand);
                Slot slot = player.GetRandomEmptySlot(rand);

                // 法术卡可以攻击任何槽位
                if (random != null && random.CardData.IsRequireTargetSpell())
                    slot = game_data.GetRandomSlot(rand);

                // 装备卡放置在已占用的槽位
                if(random != null && random.CardData.IsEquipment())
                    slot = player.GetRandomOccupiedSlot(rand);

                if (random != null)
                    gameplay.PlayCard(random, slot);
            }
        }

        /// <summary>
        /// 随机攻击其他卡牌
        /// </summary>
        public void Attack()
        {
            if (!CanPlay())
                return;

            Game game_data = gameplay.GetGameData();
            Player player = game_data.GetPlayer(player_id);

            if (player.cards_board.Count > 0 && game_data.IsPlayerActionTurn(player))
            {
                Card random = player.GetRandomCard(player.cards_board, rand);
                Card rtarget = game_data.GetRandomBoardCard(rand);
                if (random != null && rtarget != null)
                    gameplay.AttackTarget(random, rtarget);
            }
        }

        /// <summary>
        /// 随机攻击对手玩家
        /// </summary>
        public void AttackPlayer()
        {
            if (!CanPlay())
                return;

            Game game_data = gameplay.GetGameData();
            Player player = game_data.GetPlayer(player_id);
            Player oplayer = game_data.GetRandomPlayer(rand);

            if (player.cards_board.Count > 0 && game_data.IsPlayerActionTurn(player))
            {
                Card random = player.GetRandomCard(player.cards_board, rand);
                if (random != null && oplayer != null && oplayer != player)
                    gameplay.AttackPlayer(random, oplayer);
            }
        }

        /// <summary>
        /// 随机选择卡牌作为目标
        /// </summary>
        public void SelectCard()
        {
            if (!CanPlay())
                return;

            Game game_data = gameplay.GetGameData();
            Player player = game_data.GetPlayer(player_id);
            AbilityData ability = AbilityData.Get(game_data.selector_ability_id);
            Card caster = game_data.GetCard(game_data.selector_caster_uid);

            if (player != null && ability != null && caster != null)
            {
                List<Card> card_list = ability.GetCardTargets(game_data, caster);
                if (card_list.Count > 0)
                {
                    Card card = card_list[rand.Next(0, card_list.Count)];
                    gameplay.SelectCard(card);
                }
            }
        }

        /// <summary>
        /// 随机选择目标卡牌（玩家操作时）
        /// </summary>
        public void SelectTarget()
        {
            if (!CanPlay())
                return;

            Game game_data = gameplay.GetGameData();
            if (game_data.selector != SelectorType.None)
            {
                int target_player = (player_id == 0 ? 1 : 0); // 选对手
                Player tplayer = game_data.GetPlayer(target_player);

                if (tplayer.cards_board.Count > 0)
                {
                    Card random = tplayer.GetRandomCard(tplayer.cards_board, rand);
                    if (random != null)
                        gameplay.SelectCard(random);
                }
            }
        }

        /// <summary>
        /// 随机选择能力链选项
        /// </summary>
        public void SelectChoice()
        {
            if (!CanPlay())
                return;

            Game game_data = gameplay.GetGameData();
            AbilityData ability = AbilityData.Get(game_data.selector_ability_id);
            if (ability != null && ability.chain_abilities.Length > 0)
            {
                int choice = rand.Next(0, ability.chain_abilities.Length);
                gameplay.SelectChoice(choice);
            }
        }

        /// <summary>
        /// 随机选择支付的法力值
        /// </summary>
        public void SelectCost()
        {
            if (!CanPlay())
                return;

            Game game_data = gameplay.GetGameData();
            Player player = game_data.GetPlayer(player_id);
            Card card = game_data.GetCard(game_data.selector_caster_uid);

            if (player != null && card != null)
            {
                int max = Mathf.Clamp(player.mana, 0, 9);
                int choice = rand.Next(0, max + 1);
                gameplay.SelectCost(choice);
            }
        }

        /// <summary>
        /// 取消选择
        /// </summary>
        public void CancelSelect()
        {
            if (CanPlay())
                gameplay.CancelSelection();
        }

        /// <summary>
        /// Mulligan 阶段随机选择（不换牌）
        /// </summary>
        public void SelectMulligan()
        {
            if (!CanPlay())
                return;

            Game game_data = gameplay.GetGameData();
            if (game_data.phase == GamePhase.Mulligan)
            {
                Player player = game_data.GetPlayer(player_id);
                string[] cards = new string[0]; // 不换牌
                gameplay.Mulligan(player, cards);
            }
        }

        /// <summary>
        /// 结束回合
        /// </summary>
        public void EndTurn()
        {
            if (CanPlay())
                gameplay.EndTurn();
        }
    }

}

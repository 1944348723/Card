using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Profiling;
using TcgEngine.Gameplay;

namespace TcgEngine.AI
{

    /// <summary>
    /// 该类实现基于 Minimax + AlphaBeta 剪枝 的卡牌游戏 AI
    /// 通过模拟未来数回合所有可能动作，计算“收益评分（heuristic）”，
    /// 选择预期最优的行为
    /// </summary>
    public class AILogic
    {
        private readonly int ai_player_id;
        private readonly AISearchSettings search_settings;
        private readonly GameLogic game_logic;   // 执行动作的逻辑（无动画，纯计算）
        private readonly AIHeuristic heuristic;  // 局面评分系统
        private readonly AIActionEvaluator action_evaluator; // 候选动作排序与筛选

        private Game original_data;     // 进入 AI 计算时的游戏快照
        private NodeState first_node = null; // 根节点
        private NodeState best_move = null;  // 最终最佳决策节点

        private volatile bool running = false;
        private volatile bool cancellation_requested = false;
        private int nb_calculated = 0;       // 计算过的节点数量
        private int reached_depth = 0;       // 实际达到的最大搜索深度

        private System.Random random_gen;

        // 内存池优化（避免 GC 峰值）
        private readonly Pool<NodeState> node_pool = new();
        private readonly Pool<Game> data_pool = new();
        private readonly Pool<AIAction> action_pool = new();
        private readonly Pool<List<AIAction>> list_pool = new();
        private readonly ListSwap<Card> card_array = new();
        private readonly ListSwap<Slot> slot_array = new();

        private AILogic(int playerId, int level, AISearchSettings searchSettings)
        {
            ai_player_id = playerId;
            search_settings = searchSettings;
            heuristic = new AIHeuristic(playerId, level);
            action_evaluator = new AIActionEvaluator(playerId);
            game_logic = new GameLogic(null, true);
        }

        /// <summary>
        /// 创建完整初始化的 AI 搜索器。
        /// </summary>
        public static AILogic Create(int playerId, int level, AISearchSettings searchSettings = null)
        {
            return new AILogic(playerId, level, searchSettings ?? AISearchSettings.Default);
        }

        /// <summary>
        /// 启动 AI 计算（异步线程）
        /// </summary>
        public void RunAI(Game data)
        {
            if (running)
                return;

            original_data = Game.CloneNew(data); // 拷贝游戏数据，防止污染真实游戏
            game_logic.ClearResolve();
            game_logic.SetData(original_data);
            random_gen = new System.Random();

            first_node = null;
            best_move = null;
            reached_depth = 0;
            nb_calculated = 0;
            cancellation_requested = false;
            running = true;

            // 默认：在子线程执行，避免卡 UI
            Thread ai_thread = new(Execute)
            {
                IsBackground = true,
                Name = "TCG AI Search"
            };
            ai_thread.Start();

            // 如需 Debug（断点 / Profiler），可以改为主线程执行：
            // Execute();
        }

        /// <summary>
        /// 请求停止 AI 搜索。搜索线程会在安全点退出，并负责更新运行状态。
        /// </summary>
        public void Stop()
        {
            cancellation_requested = true;
        }

        /// <summary>
        /// AI 主执行函数
        /// </summary>
        private void Execute()
        {
            System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();
            bool profiler_started = false;

            try
            {
                if (cancellation_requested)
                    return;

                // 创建根节点（当前状态）
                first_node = CreateNode(null, null, ai_player_id, 0, 0);
                first_node.hvalue = heuristic.Calculate(original_data, first_node.tdepth);
                first_node.alpha = int.MinValue;
                first_node.beta = int.MaxValue;

                Profiler.BeginSample("AI");
                profiler_started = true;

                // 递归搜索
                CalculateNode(original_data, first_node);

                if (!cancellation_requested)
                    best_move = first_node.best_child;
            }
            catch (Exception exception)
            {
                best_move = null;
                Debug.LogException(exception);
            }
            finally
            {
                if (profiler_started)
                    Profiler.EndSample();

                watch.Stop();
                Debug.Log("AI: Time " + watch.ElapsedMilliseconds + "ms Depth " + reached_depth + " Nodes " + nb_calculated);

                // 最后写入 volatile 状态，确保主线程随后能看到完整结果。
                running = false;
            }
        }

        /// <summary>
        /// 计算当前节点的所有可能行动
        /// </summary>
        private void CalculateNode(Game data, NodeState node)
        {
            if (cancellation_requested)
                return;

            Profiler.BeginSample("Add Actions");
            Player player = data.GetPlayer(data.current_player);

            // 从对象池取 action list
            List<AIAction> action_list = list_pool.Create();

            // 决定允许多少连续动作
            int max_actions = node.tdepth < search_settings.WideSearchDepth
                ? search_settings.WideMaxActionsPerTurn
                : search_settings.MaxActionsPerTurn;

            // 还没达到该回合最大操作数
            if (node.taction < max_actions)
            {
                // 若当前不在选择目标状态
                if (data.selector == SelectorType.None)
                {
                    // 1️⃣ 尝试出手牌
                    for (int c = 0; c < player.cards_hand.Count; c++)
                    {
                        Card card = player.cards_hand[c];
                        AddActions(action_list, data, node, GameAction.PlayCard, card);
                    }

                    // 2️⃣ 尝试操作场上随从
                    for (int c = 0; c < player.cards_board.Count; c++)
                    {
                        Card card = player.cards_board[c];
                        AddActions(action_list, data, node, GameAction.Attack, card);
                        AddActions(action_list, data, node, GameAction.AttackPlayer, card);
                        AddActions(action_list, data, node, GameAction.CastAbility, card);
                        //AddActions(action_list, data, node, GameAction.Move, card); // 可选：移动
                    }

                    // 英雄技能
                    if (player.hero != null)
                        AddActions(action_list, data, node, GameAction.CastAbility, player.hero);
                }
                else
                {
                    // 当前在“选择目标 / 选择费用 / 选择分支”等阶段
                    AddSelectActions(action_list, data, node);
                }
            }

            // ----------- 追加 结束回合 的逻辑 -------------
            bool is_full_mana = HasAction(action_list, GameAction.PlayCard) && player.mana >= player.mana_max;
            bool can_attack_player = HasAction(action_list, GameAction.AttackPlayer);

            // 只有在无法继续攻击，且不是浪费满费时，允许结束回合
            bool can_end = !can_attack_player && !is_full_mana && data.selector == SelectorType.None;

            if (action_list.Count == 0 || can_end)
            {
                AIAction actiont = CreateAction(GameAction.EndTurn);
                action_list.Add(actiont);
            }

            // ----------- 剪枝：去掉低价值行为 -------------
            FilterActions(data, node, action_list);
            Profiler.EndSample();

            // ----------- 遍历有效动作 → 进入子节点 -------------
            for (int o = 0; o < action_list.Count && !cancellation_requested; o++)
            {
                AIAction action = action_list[o];
                if (action.valid && node.alpha < node.beta) // AlphaBeta 剪枝
                {
                    CalculateChildNode(data, node, action);
                }
            }

            // 回收列表
            action_list.Clear();
            list_pool.Dispose(action_list);
        }

        /// <summary>
        /// 过滤动作（评分 / 排序 / 剪枝）
        /// </summary>
        private void FilterActions(Game data, NodeState node, List<AIAction> action_list)
        {
            int count_valid = 0;

            // 先根据 sort 预筛
            for (int o = 0; o < action_list.Count; o++)
            {
                AIAction action = action_list[o];
                action.sort = action_evaluator.CalculateOrder(data, action);
                action.valid = action.sort <= 0 || action.sort >= node.sort_min;

                if (action.valid)
                    count_valid++;
            }

            int max_actions = node.tdepth < search_settings.WideSearchDepth
                ? search_settings.WideMaxBranchesPerAction
                : search_settings.MaxBranchesPerAction;
            int max_actions_skip = max_actions + 2;

            if (count_valid <= max_actions_skip)
                return;

            // 计算每个行动的分数
            for (int o = 0; o < action_list.Count; o++)
            {
                AIAction action = action_list[o];
                if (action.valid)
                {
                    action.score = action_evaluator.CalculatePriority(data, action);
                }
            }

            // 排序（高分优先），多余的标记为 invalid
            action_list.Sort((AIAction a, AIAction b) => { return b.score.CompareTo(a.score); });

            for (int o = 0; o < action_list.Count; o++)
            {
                AIAction action = action_list[o];
                action.valid = action.valid && o < max_actions;
            }
        }

        // 为父节点创建一个子节点，并继续递归计算它
        private void CalculateChildNode(Game data, NodeState parent, AIAction action)
        {
            // 无效动作直接返回
            if (action.type == GameAction.None)
                return;

            int player_id = data.current_player;

            // 克隆游戏数据，避免影响原始节点数据
            Profiler.BeginSample("Clone Data");
            Game ndata = data_pool.Create();
            Game.Clone(data, ndata);        // 复制数据状态
            game_logic.ClearResolve();      // 清理解析状态
            game_logic.SetData(ndata);      // 设置新的游戏数据
            Profiler.EndSample();

            // 执行动作，模拟结果
            Profiler.BeginSample("Execute AIAction");
            DoAIAction(ndata, action, player_id);
            Profiler.EndSample();

            if (cancellation_requested)
            {
                data_pool.Dispose(ndata);
                return;
            }

            // ----------- 更新“执行深度” ----------
            bool new_turn = action.type == GameAction.EndTurn;
            int next_tdepth = parent.tdepth;
            int next_taction = parent.taction + 1;

            // 如果这个动作导致回合结束，进入下一层深度
            if (new_turn)
            {
                next_tdepth = parent.tdepth + 1;
                next_taction = 0;
            }

            // ----------- 创建子节点 ----------
            Profiler.BeginSample("Create Node");
            NodeState child_node = CreateNode(parent, action, player_id, next_tdepth, next_taction);
            parent.childs.Add(child_node);
            Profiler.EndSample();

            // 继续继承 Sort 限制值（若进入新回合则清零）
            child_node.sort_min = new_turn ? 0 : Mathf.Max(action.sort, child_node.sort_min);

            // ----------- 递归继续搜索 ----------
            // 若：游戏尚未结束 且 没达到最大搜索层级，继续
            if (!ndata.HasEnded() && child_node.tdepth < search_settings.MaxTurnDepth)
            {
                CalculateNode(ndata, child_node);
            }
            else
            {
                // 否则为叶子节点，计算最终启发式得分
                child_node.hvalue = heuristic.Calculate(ndata, child_node.tdepth);
            }

            if (cancellation_requested)
            {
                data_pool.Dispose(ndata);
                return;
            }

            // ----------- 回溯：更新父节点评价 ----------
            if (player_id == ai_player_id)
            {
                // 当前节点是 AI 玩家 → 取最大值（Max 节点）
                if (parent.best_child == null || child_node.hvalue > parent.hvalue)
                {
                    parent.best_child = child_node;
                    parent.hvalue = child_node.hvalue;
                    parent.alpha = Mathf.Max(parent.alpha, parent.hvalue);
                }
            }
            else
            {
                // 当前节点是对手 → 取最小值（Min 节点）
                if (parent.best_child == null || child_node.hvalue < parent.hvalue)
                {
                    parent.best_child = child_node;
                    parent.hvalue = child_node.hvalue;
                    parent.beta = Mathf.Min(parent.beta, parent.hvalue);
                }
            }

            // ----------- Debug 统计 ----------
            nb_calculated++;
            if (child_node.tdepth > reached_depth)
                reached_depth = child_node.tdepth;

            // 这个分支的 Game 数据已用完，回收
            // 注意：NodeState 不回收，因为之后要拿完整决策路径
            data_pool.Dispose(ndata);
        }

        // 创建一个新的节点（用于搜索树）
        private NodeState CreateNode(NodeState parent, AIAction action, int player_id, int turn_depth, int turn_action)
        {
            NodeState nnode = node_pool.Create();
            nnode.current_player = player_id;                  // 当前执行玩家
            nnode.tdepth = turn_depth;                         // 当前搜索深度（第几回合层）
            nnode.taction = turn_action;                       // 当前回合第几个动作
            nnode.parent = parent;                             // 父节点
            nnode.last_action = action;                        // 导致该节点的动作
            nnode.alpha = parent != null ? parent.alpha : int.MinValue;
            nnode.beta = parent != null ? parent.beta : int.MaxValue;
            nnode.hvalue = 0;                                  // 启发式分数
            nnode.sort_min = 0;                                // 行动最小 sort 限制
            return nnode;
        }

        // 为某张卡片添加所有可能的动作到 actions 列表中
        private void AddActions(List<AIAction> actions, Game data, NodeState node, ushort type, Card card)
        {
            Player player = data.GetPlayer(data.current_player);

            // 如果当前在选择目标状态，不允许普通行为
            if (data.selector != SelectorType.None)
                return;

            // 麻痹状态不能行动
            if (card.HasStatus(StatusType.Paralysed))
                return;

            // ----------------- 出牌逻辑 -----------------
            if (type == GameAction.PlayCard)
            {
                // 随从类卡片
                if (card.CardData.IsBoardCard())
                {
                    Slot slot = player.GetRandomEmptySlot(data.Board, random_gen, slot_array.Get());

                    if (game_logic.Rules.CanPlayCard(card, slot))
                    {
                        AIAction action = CreateAction(type, card);
                        action.slot = slot;
                        actions.Add(action);
                    }
                }
                // 装备类卡片
                else if (card.CardData.IsEquipment())
                {
                    Player tplayer = data.GetPlayer(card.player_id);
                    for (int c = 0; c < tplayer.cards_board.Count; c++)
                    {
                        Card tcard = tplayer.cards_board[c];
                        if (game_logic.Rules.CanPlayCard(card, tcard.slot))
                        {
                            AIAction action = CreateAction(type, card);
                            action.slot = tcard.slot;
                            action.target_player_id = tplayer.player_id;
                            actions.Add(action);
                        }
                    }
                }
                // 需要目标的法术
                else if (card.CardData.IsRequireTargetSpell())
                {
                    // 目标是玩家
                    for (int p = 0; p < data.players.Length; p++)
                    {
                        Player tplayer = data.players[p];
                        Slot tslot = new Slot(tplayer.player_id);
                        if (game_logic.Rules.CanPlayCard(card, tslot))
                        {
                            AIAction action = CreateAction(type, card);
                            action.slot = tslot;
                            action.target_player_id = tplayer.player_id;
                            actions.Add(action);
                        }
                    }

                    // 目标是随从
                    foreach (Slot slot in data.Board.GetAll())
                    {
                        if (game_logic.Rules.CanPlayCard(card, slot))
                        {
                            Card slot_card = data.GetSlotCard(slot);
                            AIAction action = CreateAction(type, card);
                            action.slot = slot;
                            action.target_uid = slot_card != null ? slot_card.uid : null;
                            actions.Add(action);
                        }
                    }
                }
                // 无目标法术
                else if (game_logic.Rules.CanPlayCard(card, Slot.None))
                {
                    AIAction action = CreateAction(type, card);
                    actions.Add(action);
                }
            }

            // ----------------- 攻击随从 -----------------
            if (type == GameAction.Attack)
            {
                if (card.CanAttack())
                {
                    for (int p = 0; p < data.players.Length; p++)
                    {
                        if (p != player.player_id)
                        {
                            Player oplayer = data.players[p];
                            for (int tc = 0; tc < oplayer.cards_board.Count; tc++)
                            {
                                Card target = oplayer.cards_board[tc];
                                if (game_logic.Rules.CanAttackTarget(card, target))
                                {
                                    AIAction action = CreateAction(type, card);
                                    action.target_uid = target.uid;
                                    actions.Add(action);
                                }
                            }
                        }
                    }
                }
            }

            // ----------------- 攻击玩家 -----------------
            if (type == GameAction.AttackPlayer)
            {
                if (card.CanAttack())
                {
                    for (int p = 0; p < data.players.Length; p++)
                    {
                        if (p != player.player_id)
                        {
                            Player oplayer = data.players[p];
                            if (game_logic.Rules.CanAttackTarget(card, oplayer))
                            {
                                AIAction action = CreateAction(type, card);
                                action.target_player_id = oplayer.player_id;
                                actions.Add(action);
                            }
                        }
                    }
                }
            }

            // ----------------- 主动技能 -----------------
            if (type == GameAction.CastAbility)
            {
                List<AbilityData> abilities = card.GetAbilities();
                for (int a = 0; a < abilities.Count; a++)
                {
                    AbilityData ability = abilities[a];

                    // 必须是可主动释放 + 有合法目标
                    if (ability.trigger == AbilityTrigger.Activate &&
                        game_logic.Rules.CanCastAbility(card, ability) &&
                        ability.HasValidSelectTarget(data, card))
                    {
                        AIAction action = CreateAction(type, card);
                        action.ability_id = ability.id;
                        actions.Add(action);
                    }
                }
            }

            // ----------------- 移动卡片 -----------------
            if (type == GameAction.Move)
            {
                foreach (Slot slot in data.Board.GetAll(player.player_id))
                {
                    if (game_logic.Rules.CanMoveCard(card, slot))
                    {
                        AIAction action = CreateAction(type, card);
                        action.slot = slot;
                        actions.Add(action);
                    }
                }
            }
        }

        // 添加所有“选择阶段”的可能行为
        private void AddSelectActions(List<AIAction> actions, Game data, NodeState node)
        {
            if (data.selector == SelectorType.None)
                return;

            Player player = data.GetPlayer(data.selector_player_id);
            Card caster = data.GetCard(data.selector_caster_uid);
            AbilityData ability = AbilityData.Get(data.selector_ability_id);
            if (player == null || caster == null)
                return;

            // 选择目标（随从 / 玩家 / 空槽）
            if (data.selector == SelectorType.SelectTarget && ability != null)
            {
                // 选择玩家
                for (int p = 0; p < data.players.Length; p++)
                {
                    Player tplayer = data.players[p];
                    if (ability.CanTarget(data, caster, tplayer))
                    {
                        AIAction action = CreateAction(GameAction.SelectPlayer, caster);
                        action.target_player_id = tplayer.player_id;
                        actions.Add(action);
                    }
                }

                // 选择随从 or 空槽
                foreach (Slot slot in data.Board.GetAll())
                {
                    Card tcard = data.GetSlotCard(slot);

                    if (tcard != null && ability.CanTarget(data, caster, tcard))
                    {
                        AIAction action = CreateAction(GameAction.SelectCard, caster);
                        action.target_uid = tcard.uid;
                        actions.Add(action);
                    }
                    else if (tcard == null && ability.CanTarget(data, caster, slot))
                    {
                        AIAction action = CreateAction(GameAction.SelectSlot, caster);
                        action.slot = slot;
                        actions.Add(action);
                    }
                }
            }

            // 选择卡片
            if (data.selector == SelectorType.SelectorCard && ability != null)
            {
                for (int p = 0; p < data.players.Length; p++)
                {
                    List<Card> cards = ability.GetCardTargets(data, caster, card_array);
                    foreach (Card tcard in cards)
                    {
                        AIAction action = CreateAction(GameAction.SelectCard, caster);
                        action.target_uid = tcard.uid;
                        actions.Add(action);
                    }
                }
            }

            // 选择能力分支
            if (data.selector == SelectorType.SelectorChoice && ability != null)
            {
                for (int i = 0; i < ability.chain_abilities.Length; i++)
                {
                    AbilityData choice = ability.chain_abilities[i];
                    if (choice != null && game_logic.Rules.CanSelectAbility(caster, choice))
                    {
                        AIAction action = CreateAction(GameAction.SelectChoice, caster);
                        action.value = i;
                        actions.Add(action);
                    }
                }
            }

            // 选择法力值
            if (data.selector == SelectorType.SelectorCost)
            {
                for (int i = 1; i <= player.mana; i++)
                {
                    AIAction action = CreateAction(GameAction.SelectCost, caster);
                    action.value = i;
                    actions.Add(action);
                }
            }

            // 如果没有任何选择，加入取消选项
            if (actions.Count == 0)
            {
                AIAction caction = CreateAction(GameAction.CancelSelect, caster);
                actions.Add(caction);
            }
        }


        // 创建一个 AIAction（只指定类型）
        private AIAction CreateAction(ushort type)
        {
            AIAction action = action_pool.Create();   // 从对象池创建一个 AIAction，避免频繁 GC
            action.Clear();                           // 重置状态
            action.type = type;                       // 设置动作类型
            action.valid = true;                      // 标记为有效
            return action;
        }

        // 创建一个 AIAction（指定类型 + 关联的卡牌）
        private AIAction CreateAction(ushort type, Card card)
        {
            AIAction action = action_pool.Create();
            action.Clear();
            action.type = type;
            action.card_uid = card.uid;               // 绑定这张卡牌
            action.valid = true;
            return action;
        }


        //--------------------- AI 执行动作模拟 ---------------------
        // 在 AI 预测搜索时，不是真实游戏执行，而是模拟执行动作
        private void DoAIAction(Game data, AIAction action, int player_id)
        {
            Player player = data.GetPlayer(player_id);

            // 打出手牌
            if (action.type == GameAction.PlayCard)
            {
                Card card = player.GetHandCard(action.card_uid);
                game_logic.PlayCard(card, action.slot);
            }

            // 移动场上单位
            if (action.type == GameAction.Move)
            {
                Card card = player.GetBoardCard(action.card_uid);
                game_logic.MoveCard(card, action.slot);
            }

            // 攻击卡牌
            if (action.type == GameAction.Attack)
            {
                Card card = player.GetBoardCard(action.card_uid);
                Card target = data.GetBoardCard(action.target_uid);
                game_logic.AttackTarget(card, target);
            }

            // 直接攻击玩家
            if (action.type == GameAction.AttackPlayer)
            {
                Card card = player.GetBoardCard(action.card_uid);
                Player tplayer = data.GetPlayer(action.target_player_id);
                game_logic.AttackPlayer(card, tplayer);
            }

            // 释放技能 / 法术
            if (action.type == GameAction.CastAbility)
            {
                Card card = player.GetCard(action.card_uid);
                AbilityData ability = AbilityData.Get(action.ability_id);
                game_logic.CastAbility(card, ability);
            }

            // 选择卡牌（用于需要选择目标的技能 / 法术）
            if (action.type == GameAction.SelectCard)
            {
                Card target = data.GetCard(action.target_uid);
                game_logic.SelectCard(target);
            }

            // 选择玩家
            if (action.type == GameAction.SelectPlayer)
            {
                Player target = data.GetPlayer(action.target_player_id);
                game_logic.SelectPlayer(target);
            }

            // 选择位置（棋盘格）
            if (action.type == GameAction.SelectSlot)
            {
                game_logic.SelectSlot(action.slot);
            }

            // 选择一个选项（某些卡牌会弹出选择界面）
            if (action.type == GameAction.SelectChoice)
            {
                game_logic.SelectChoice(action.value);
            }

            // 取消当前选择
            if (action.type == GameAction.CancelSelect)
            {
                game_logic.CancelSelection();
            }

            // 结束回合
            if (action.type == GameAction.EndTurn)
            {
                game_logic.EndTurn();
            }
        }


        // 判断是否存在某种 Action（避免重复动作）
        private bool HasAction(List<AIAction> list, ushort type)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].type == type)
                    return true;
            }
            return false;
        }


        //---------------- AI 运行状态与调试 ----------------

        // AI 是否正在运行
        public bool IsRunning()
        {
            return running;
        }

        // 获取 AI 预测路径（默认从根节点）
        public string GetNodePath()
        {
            return GetNodePath(first_node);
        }

        // 获取从某个节点开始的行动路径（调试用）
        private string GetNodePath(NodeState node)
        {
            if (node == null)
                return "Prediction: unavailable";

            string path = "Prediction: HValue: " + node.hvalue + "\n";
            NodeState current = node;
            AIAction move;

            // 一直顺着 best_child 找下去，相当于打印 AI 决策链
            while (current != null)
            {
                move = current.last_action;
                if (move != null)
                    path += "Player " + current.current_player + ": " 
                            + move.GetText(original_data) + "\n";

                current = current.best_child;
            }
            return path;
        }


        //---------------- 内存清理 ----------------

        // 释放本次搜索持有的状态，并保留池容量供下一次搜索复用
        public void ClearMemory()
        {
            if (running)
                throw new InvalidOperationException("AI 搜索仍在运行，不能清理搜索状态");

            // 先断开树和列表中的对象引用，再归还池对象。
            foreach (NodeState node in node_pool.GetAllActive())
                node.Clear();
            foreach (AIAction action in action_pool.GetAllActive())
                action.Clear();
            foreach (List<AIAction> actions in list_pool.GetAllActive())
                actions.Clear();

            data_pool.DisposeAll();
            node_pool.DisposeAll();
            action_pool.DisposeAll();
            list_pool.DisposeAll();

            card_array.Clear();
            slot_array.Clear();

            game_logic.ClearResolve();
            game_logic.SetData(null);

            original_data = null;
            first_node = null;
            best_move = null;
            random_gen = null;
        }

        // 获取最佳行动
        public AIAction GetBestAction()
        {
            return best_move != null ? best_move.last_action : null;
        }

        //---------------------- 节点状态（用于 Minimax 搜索树） ----------------------
        private sealed class NodeState
        {
            public int tdepth;      // 当前节点所在“回合深度”（不是单纯步数，而是轮到谁 + 多少轮）
            public int taction;     // 当前回合已经执行了多少个动作（因为一回合可以多个操作）
            public int sort_min;    // 行为排序下限：低于该值的行为不再计算（避免 A→B 与 B→A 重复搜索）

            public int hvalue;      // 启发值（AI 想要最大化，敌人想要最小化）

            public int alpha;       // Alpha 值：AI（最大化方）当前已知最大收益
            public int beta;        // Beta 值：对手（最小化方）当前已知最小收益
                                    // alpha-beta 剪枝用，减少搜索分支

            public AIAction last_action = null;   // 导致到达该节点的动作
            public int current_player;            // 当前轮到哪个玩家行动

            public NodeState parent;              // 父节点
            public NodeState best_child = null;   // 当前节点计算出的最佳子节点
            public List<NodeState> childs = new List<NodeState>();  // 所有子节点（搜索树分支）

            public NodeState() { }

            // 清理节点（用于对象池复用）
            public void Clear()
            {
                last_action = null;
                best_child = null;
                parent = null;
                childs.Clear();
            }
        }
    }
}

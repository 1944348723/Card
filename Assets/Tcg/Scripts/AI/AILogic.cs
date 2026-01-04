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
        //-------- AI 配置参数 ------------------

        public int ai_depth = 3;                // 预测多少回合（越大越智能，但计算量指数增长）
        public int ai_depth_wide = 1;           // 前多少层采用“宽搜索”（更多分支）
        public int actions_per_turn = 2;        // 每个回合最多连续预测多少动作
        public int actions_per_turn_wide = 3;   // 宽搜索下的动作限制
        public int nodes_per_action = 4;        // 每个动作最多保留多少子节点（超过将剪枝）
        public int nodes_per_action_wide = 7;   // 宽搜索版本

        /*
         * 举例：
         * 第一层（AI 当前回合）→ 最多考虑 3 步连续动作 → 每一步最多 7 个候选分支
         * 第二层（对手回合）→ 最多 2 步动作 → 每步最多 4 个分支
         * 第三层（AI 下回合）→ 同上
         * 
         * 在达到最大深度或游戏结束时计算 heuristic，
         * heuristic 会从叶子往上回溯，得出当前应执行的最优路径。
         */

        //-----

        public int ai_player_id;   // AI 对应的玩家 id（通常是 1）
        public int ai_level;       // AI 难度等级

        private GameLogic game_logic;   // 执行动作的逻辑（无动画，纯计算）
        private Game original_data;     // 进入 AI 计算时的游戏快照
        private AIHeuristic heuristic;  // 启发式评分系统
        private Thread ai_thread;       // AI 线程

        private NodeState first_node = null; // 根节点
        private NodeState best_move = null;  // 最终最佳决策节点

        private bool running = false;
        private int nb_calculated = 0;       // 计算过的节点数量
        private int reached_depth = 0;       // 实际达到的最大搜索深度

        private System.Random random_gen;

        // 内存池优化（避免 GC 峰值）
        private Pool<NodeState> node_pool = new Pool<NodeState>();
        private Pool<Game> data_pool = new Pool<Game>();
        private Pool<AIAction> action_pool = new Pool<AIAction>();
        private Pool<List<AIAction>> list_pool = new Pool<List<AIAction>>();
        private ListSwap<Card> card_array = new ListSwap<Card>();
        private ListSwap<Slot> slot_array = new ListSwap<Slot>();

        /// <summary>
        /// 工厂方法：创建 AI
        /// </summary>
        public static AILogic Create(int player_id, int level)
        {
            AILogic job = new AILogic();
            job.ai_player_id = player_id;
            job.ai_level = level;

            job.heuristic = new AIHeuristic(player_id, level);
            job.game_logic = new GameLogic(true); // true = 禁用动画，纯逻辑

            return job;
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
            reached_depth = 0;
            nb_calculated = 0;
            running = true;

            // 默认：在子线程执行，避免卡 UI
            ai_thread = new Thread(Execute);
            ai_thread.Start();

            // 如需 Debug（断点 / Profiler），可以改为主线程执行：
            // Execute();
        }

        /// <summary>
        /// 停止 AI（终止线程）
        /// </summary>
        public void Stop()
        {
            running = false;
            if (ai_thread != null && ai_thread.IsAlive)
                ai_thread.Abort();
        }

        /// <summary>
        /// AI 主执行函数
        /// </summary>
        private void Execute()
        {
            // 创建根节点（当前状态）
            first_node = CreateNode(null, null, ai_player_id, 0, 0);
            first_node.hvalue = heuristic.CalculateHeuristic(original_data, first_node);
            first_node.alpha = int.MinValue;
            first_node.beta = int.MaxValue;

            Profiler.BeginSample("AI");
            System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();

            // 递归搜索
            CalculateNode(original_data, first_node);

            Debug.Log("AI: Time " + watch.ElapsedMilliseconds + "ms Depth " + reached_depth + " Nodes " + nb_calculated);
            Profiler.EndSample();

            // 记录最佳路径
            best_move = first_node.best_child;
            running = false;
        }

        /// <summary>
        /// 计算当前节点的所有可能行动
        /// </summary>
        private void CalculateNode(Game data, NodeState node)
        {
            Profiler.BeginSample("Add Actions");
            Player player = data.GetPlayer(data.current_player);

            // 从对象池取 action list
            List<AIAction> action_list = list_pool.Create();

            // 决定允许多少连续动作
            int max_actions = node.tdepth < ai_depth_wide ? actions_per_turn_wide : actions_per_turn;

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
            for (int o = 0; o < action_list.Count; o++)
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
                action.sort = heuristic.CalculateActionSort(data, action);
                action.valid = action.sort <= 0 || action.sort >= node.sort_min;

                if (action.valid)
                    count_valid++;
            }

            int max_actions = node.tdepth < ai_depth_wide ? nodes_per_action_wide : nodes_per_action;
            int max_actions_skip = max_actions + 2;

            if (count_valid <= max_actions_skip)
                return;

            // 计算每个行动的分数
            for (int o = 0; o < action_list.Count; o++)
            {
                AIAction action = action_list[o];
                if (action.valid)
                {
                    action.score = heuristic.CalculateActionScore(data, action);
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
            if (!ndata.HasEnded() && child_node.tdepth < ai_depth)
            {
                CalculateNode(ndata, child_node);
            }
            else
            {
                // 否则为叶子节点，计算最终启发式得分
                child_node.hvalue = heuristic.CalculateHeuristic(ndata, child_node);
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
                    Slot slot = player.GetRandomEmptySlot(random_gen, slot_array.Get());

                    if (data.CanPlayCard(card, slot))
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
                        if (data.CanPlayCard(card, tcard.slot))
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
                        if (data.CanPlayCard(card, tslot))
                        {
                            AIAction action = CreateAction(type, card);
                            action.slot = tslot;
                            action.target_player_id = tplayer.player_id;
                            actions.Add(action);
                        }
                    }

                    // 目标是随从
                    foreach (Slot slot in Slot.GetAll())
                    {
                        if (data.CanPlayCard(card, slot))
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
                else if (data.CanPlayCard(card, Slot.None))
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
                                if (data.CanAttackTarget(card, target))
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
                            if (data.CanAttackTarget(card, oplayer))
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
                        data.CanCastAbility(card, ability) &&
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
                foreach (Slot slot in Slot.GetAll(player.player_id))
                {
                    if (data.CanMoveCard(card, slot))
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
                foreach (Slot slot in Slot.GetAll())
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
                    if (choice != null && data.CanSelectAbility(caster, choice))
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
        public string GetNodePath(NodeState node)
        {
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

        // 清空 AI 内存，释放所有池对象
        public void ClearMemory()
        {
            original_data = null;
            first_node = null;
            best_move = null;

            // 清空所有节点和动作
            foreach (NodeState node in node_pool.GetAllActive())
                node.Clear();
            foreach (AIAction order in action_pool.GetAllActive())
                order.Clear();

            data_pool.DisposeAll();
            node_pool.DisposeAll();
            action_pool.DisposeAll();
            list_pool.DisposeAll();

            System.GC.Collect();   // 强制 GC，彻底释放 AI 内存
        }


        //---------------- 数据获取接口 ----------------

        // 已计算的节点数量（性能统计）
        public int GetNbNodesCalculated()
        {
            return nb_calculated;
        }

        // 搜索达到的最大深度
        public int GetDepthReached()
        {
            return reached_depth;
        }

        // 获取最佳节点（最终选择）
        public NodeState GetBest()
        {
            return best_move;
        }

        // 获取根节点
        public NodeState GetFirst()
        {
            return first_node;
        }

        // 获取最佳行动
        public AIAction GetBestAction()
        {
            return best_move != null ? best_move.last_action : null;
        }

        // 是否已经找到最优解
        public bool IsBestFound()
        {
            return best_move != null;
        }

    }

        //---------------------- 节点状态（用于 Minimax 搜索树） ----------------------
        public class NodeState
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

            // 构造函数：创建一个新的节点状态
            public NodeState(NodeState parent, int player_id, int turn_depth, int turn_action, int turn_sort)
            {
                this.parent = parent;
                this.current_player = player_id;
                this.tdepth = turn_depth;
                this.taction = turn_action;
                this.sort_min = turn_sort;
            }

            // 清理节点（用于对象池复用）
            public void Clear()
            {
                last_action = null;
                best_child = null;
                parent = null;
                childs.Clear();
            }
        }



        //---------------------- AI 行为对象 ----------------------
        public class AIAction
        {
            public ushort type;          // 行为类型（参见 GameAction 枚举）

            // 行为涉及到的数据
            public string card_uid;      // 操作者卡牌
            public string target_uid;    // 目标卡牌（如果有）
            public int target_player_id; // 目标玩家（攻击玩家 / 选择目标）
            public string ability_id;    // 技能 ID
            public Slot slot;            // 槽位（板位 / 站位）
            public int value;            // 额外数值（用于选择类操作）

            public int score;            // 行为评分：用于过滤不重要行为（只模拟分数高的）
            public int sort;             // 行为执行顺序排序值（用于避免顺序不同导致重复搜索）
            public bool valid;           // 行为是否有效（false 则直接忽略）

            public AIAction() { }
            public AIAction(ushort t) { type = t; }

            // 调试文本（方便输出 AI 决策路径）
            public string GetText(Game data)
            {
                string txt = GameAction.GetString(type);

                Card card = data.GetCard(card_uid);
                Card target = data.GetCard(target_uid);

                if (card != null)
                    txt += " card " + card.card_id;

                if (target != null)
                    txt += " target " + target.card_id;

                if (slot != Slot.None)
                    txt += " slot " + slot.x + "-" + slot.p;

                if (ability_id != null)
                    txt += " ability " + ability_id;

                if (value > 0)
                    txt += " value " + value;

                return txt;
            }

            // 清除数据（对象池复用）
            public void Clear()
            {
                type = 0;
                valid = false;

                card_uid = null;
                target_uid = null;
                ability_id = null;

                target_player_id = -1;
                slot = Slot.None;
                value = -1;

                score = 0;
                sort = 0;
            }

            // 一个空动作对象
            public static AIAction None
            {
                get
                {
                    AIAction a = new AIAction();
                    a.type = 0;
                    return a;
                }
            }
        }

}

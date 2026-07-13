using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Client;
using TcgEngine.UI;

namespace TcgEngine.Client
{
    /// <summary>
    /// Board 上 Slot 的可视化表现（对应 Slot.cs 的视觉层）
    /// 会在可以交互时高亮显示
    /// </summary>
    public class BoardSlot : BSlot
    {
        public BoardSlotType type; // 该 Slot 的类型（是否需要翻转 / 属于哪一侧玩家）
        public int x;              // Slot 在棋盘上的 X 坐标
        public int y;              // Slot 在棋盘上的 Y 坐标

        private static List<BoardSlot> slot_list = new List<BoardSlot>(); // 场景中所有 BoardSlot 的静态列表

        protected override void Awake()
        {
            base.Awake();
            slot_list.Add(this); // 创建时加入列表
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            slot_list.Remove(this); // 销毁时移除
        }

        private void Start()
        {
            // 检查 Slot 坐标是否在 Slot 系统允许的范围内
            BoardLayout board = GetBoardLayout();
            if (x < board.MinX || x > board.MaxX || y < board.MinY || y > board.MaxY)
                Debug.LogError("Board Slot X 和 Y 必须在 Slot.cs 设定的最小值和最大值之间，否则无效。");
        }

        protected override void Update()
        {
            base.Update();

            // 游戏还没准备好，不处理逻辑
            if (!GameClient.Get().IsReady())
                return;

            BoardCard bcard_selected = PlayerControls.Get().GetSelected();  // 当前被选中的 Board 卡牌
            HandCard drag_card = HandCard.GetDrag();                       // 当前是否正在拖拽手牌

            Game gdata = GameClient.Get().GetGameData();   // 游戏数据
            Player player = GameClient.Get().GetPlayer();  // 本地玩家
            Slot slot = GetSlot();                         // 当前 Slot 逻辑坐标
            Card dcard = drag_card?.GetCard();             // 被拖拽的卡
            Card slot_card = gdata.GetSlotCard(GetSlot()); // 当前格子上是否已有卡
            bool your_turn = GameClient.Get().IsYourTurn();// 是否轮到自己回合

            collide.enabled = slot_card == null;  // 如果 Slot 上已有卡牌，则禁用 Collider，无法点击

            // ============================
            // 计算这个 Slot 是否需要高亮
            // ============================
            target_alpha = 0f;

            // 你的回合 + 正在拖拽可放置到棋盘的卡 + 该位置允许放置 → 高亮
            if (your_turn && dcard != null && dcard.CardData.IsBoardCard() && GameClient.Get().Rules.CanPlayCard(dcard, slot))
            {
                target_alpha = 1f; // 高亮：拖生物 / 装备
            }

            // 你的回合 + 拖拽需要目标的卡牌（例如法术）+ 该格子是合法目标 → 高亮
            if (your_turn && dcard != null && dcard.CardData.IsRequireTarget() && GameClient.Get().Rules.CanPlayCard(dcard, slot))
            {
                target_alpha = 1f; // 高亮：拖目标法术
            }

            // ============================
            // 选择器模式（例如技能选目标）
            // ============================
            if (gdata.selector == SelectorType.SelectTarget && player.player_id == gdata.selector_player_id)
            {
                Card caster = gdata.GetCard(gdata.selector_caster_uid); // 施放者
                AbilityData ability = AbilityData.Get(gdata.selector_ability_id);

                // 目标为空 slot
                if(ability != null && slot_card == null && ability.CanTarget(gdata, caster, slot))
                    target_alpha = 1f;

                // 目标为 slot 上的卡
                if (ability != null && slot_card != null && ability.CanTarget(gdata, caster, slot_card))
                    target_alpha = 1f;
            }

            Card select_card = bcard_selected?.GetCard();

            // 是否可以移动
            bool can_do_move = your_turn && select_card != null && slot_card == null && GameClient.Get().Rules.CanMoveCard(select_card, slot);

            // 是否可以攻击
            bool can_do_attack = your_turn && select_card != null && slot_card != null && GameClient.Get().Rules.CanAttackTarget(select_card, slot_card);

            // 只要可以移动或攻击 → 高亮
            if (can_do_attack || can_do_move)
            {
                target_alpha = 1f;
            }
        }

        /// <summary>
        /// 计算该 BoardSlot 在游戏逻辑中的 Slot 坐标
        /// 处理玩家镜像翻转（因为双方棋盘视角是对称的）
        /// </summary>
        public override Slot GetSlot()
        {
            int p = 0;

            // X 翻转（对手视角需要镜像）
            if (type == BoardSlotType.FlipX)
            {
                int pid = GameClient.Get().GetPlayerID();
                int px = x;
                if ((pid % 2) == 1)
                    px = GetBoardLayout().MirrorX(x); // 如果不是先手 → 镜像翻转 X
                return new Slot(px, y, p);
            }

            // Y 翻转
            if (type == BoardSlotType.FlipY)
            {
                int pid = GameClient.Get().GetPlayerID();
                int py = y;
                if ((pid % 2) == 1)
                    py = GetBoardLayout().MirrorY(y); // 镜像翻转 Y
                return new Slot(x, py, p);
            }

            // 指定为“自己玩家的区域”
            if (type == BoardSlotType.PlayerSelf)
                p = GameClient.Get().GetPlayerID();

            // 指定为“对手区域”
            if(type == BoardSlotType.PlayerOpponent)
                p = GameClient.Get().GetOpponentPlayerID();
           
            return new Slot(x, y, p); // 默认 Slot
        }

        private BoardLayout GetBoardLayout()
        {
            Game game = GameClient.Get()?.GetGameData();
            return game?.Board ?? BoardLayout.Default;
        }

        /// <summary>
        /// 鼠标点击 Slot 时触发
        /// </summary>
        public void OnMouseDown()
        {
            // 如果点到 UI（按钮 / 面板等），则忽略棋盘点击
            if (GameUI.IsOverUI())
                return;

            Game gdata = GameClient.Get().GetGameData();
            int player_id = GameClient.Get().GetPlayerID();

            // 当前处于“选择目标模式” + 轮到自己
            if (gdata.selector == SelectorType.SelectTarget && player_id == gdata.selector_player_id)
            {
                Slot slot = GetSlot();
                Card slot_card = gdata.GetSlotCard(slot);

                // Slot 必须为空，才可以选择 Slot 作为目标
                if (slot_card == null)
                {
                    GameClient.Get().SelectSlot(slot);
                }
            }
        }
    }
}

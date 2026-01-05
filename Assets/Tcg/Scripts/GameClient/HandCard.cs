using TcgEngine.Client;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine.UI;

namespace TcgEngine.Client
{
    /// <summary>
    /// 代表手牌中的卡牌的视觉效果
    /// 将从 Card.cs 获取数据并显示
    /// </summary>
    public class HandCard : MonoBehaviour
    {
        public Image card_glow;               // 卡牌高亮显示
        public float move_speed = 10f;        // 移动速度
        public float move_rotate_speed = 4f;  // 旋转速度
        public float move_max_rotate = 10f;   // 最大旋转角度

        [HideInInspector]
        public Vector2 deck_position;         // 卡牌在手牌区域的位置
        [HideInInspector]
        public float deck_angle;              // 卡牌在手牌区域的角度

        private string card_uid = "";         // 卡牌唯一ID

        private CardUI card_ui;               // 卡牌UI组件
        private RectTransform hand_transform; // 手牌父节点
        private RectTransform card_transform; // 当前卡牌节点
        private Vector3 start_scale;          // 初始缩放
        private Vector3 current_rotate;       // 当前旋转
        private Vector3 target_rotate;        // 目标旋转
        private Vector3 prev_pos;             // 上一帧位置

        private bool destroyed = false;       // 是否已销毁
        private float focus_timer = 0f;       // 高亮计时器

        private bool focus = false;           // 是否高亮
        private bool drag = false;            // 是否拖拽中
        private bool selected = false;        // 是否被选中

        private static List<HandCard> card_list = new List<HandCard>(); // 所有手牌实例列表

        void Awake()
        {
            card_list.Add(this); // 添加到静态列表
            card_ui = GetComponent<CardUI>();
            card_transform = transform.GetComponent<RectTransform>();
            hand_transform = transform.parent.GetComponent<RectTransform>();
            start_scale = transform.localScale; // 保存初始缩放
        }

        private void Start()
        {
            // 可以用于初始化
        }

        private void OnDestroy()
        {
            card_list.Remove(this); // 从静态列表移除
        }

        void Update()
        {
            if (!GameClient.Get().IsReady())
                return; // 游戏未准备好时跳过

            Card card = GetCard();
            Vector2 target_position = deck_position;
            Vector3 target_size = start_scale;

            focus_timer += Time.deltaTime;

            // 高亮状态
            if (IsFocus())
            {
                target_position = deck_position + Vector2.up * 40f;
            }

            // 拖拽状态
            if (IsDrag())
            {
                target_position = GetTargetPosition();
                target_size = start_scale * 0.75f;

                // 旋转效果
                Vector3 dir = card_transform.position - prev_pos;
                Vector3 addrot = new Vector3(dir.y * 90f, -dir.x * 90f, 0f);
                target_rotate += addrot * move_rotate_speed * Time.deltaTime;
                target_rotate = new Vector3(Mathf.Clamp(target_rotate.x, -move_max_rotate, move_max_rotate),
                                            Mathf.Clamp(target_rotate.y, -move_max_rotate, move_max_rotate), 0f);
                current_rotate = Vector3.Lerp(current_rotate, target_rotate, move_rotate_speed * Time.deltaTime);
            }
            else
            {
                target_rotate = new Vector3(0f, 0f, deck_angle);
                current_rotate = new Vector3(0f, 0f, deck_angle);
            }

            // 平滑移动、旋转和缩放
            card_transform.anchoredPosition = Vector2.Lerp(card_transform.anchoredPosition, target_position, Time.deltaTime * move_speed);
            card_transform.localRotation = Quaternion.Slerp(card_transform.localRotation, Quaternion.Euler(current_rotate), Time.deltaTime * move_speed);
            card_transform.localScale = Vector3.Lerp(card_transform.localScale, target_size, 5f * Time.deltaTime);

            // 更新卡牌UI
            card_ui.SetCard(card);

            // 高亮显示
            card_glow.enabled = IsFocus() || IsDrag();

            prev_pos = Vector3.Lerp(prev_pos, card_transform.position, 1f * Time.deltaTime);

            // 鼠标点击取消选择
            if (!drag && selected && Input.GetMouseButtonDown(0))
                selected = false;
        }

        /// <summary>
        /// 获取拖拽时的目标位置
        /// </summary>
        private Vector2 GetTargetPosition()
        {
            Card card = GetCard();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(hand_transform, Input.mousePosition, Camera.main, out Vector2 tpos);

            if (card.CardData.IsRequireTarget())
            {
                tpos = deck_position + Vector2.up * 150f + Vector2.right * tpos.x / 10f;
            }

            return tpos;
        }

        /// <summary>
        /// 设置卡牌
        /// </summary>
        public void SetCard(Card card)
        {
            this.card_uid = card.uid;
            card_ui.SetCard(card);
        }

        /// <summary>
        /// 销毁卡牌对象
        /// </summary>
        public void Kill()
        {
            if (!destroyed)
            {
                destroyed = true;
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 是否高亮
        /// </summary>
        public bool IsFocus()
        {
            if (GameTool.IsMobile())
                return selected && !drag;
            return focus && !drag && focus_timer > 0f;
        }

        /// <summary>
        /// 是否正在拖拽
        /// </summary>
        public bool IsDrag()
        {
            return drag;
        }

        /// <summary>
        /// 获取对应Card对象
        /// </summary>
        public Card GetCard()
        {
            Game gdata = GameClient.Get().GetGameData();
            return gdata.GetCard(card_uid);
        }

        /// <summary>
        /// 获取对应CardData
        /// </summary>
        public CardData GetCardData()
        {
            Card card = GetCard();
            if (card != null)
                return CardData.Get(card.card_id);
            return null;
        }

        /// <summary>
        /// 获取卡牌UID
        /// </summary>
        public string GetCardUID()
        {
            return card_uid;
        }

        /// <summary>
        /// 鼠标进入卡牌触发
        /// </summary>
        public void OnMouseEnterCard()
        {
            if (GameUI.IsUIOpened())
                return;

            focus = true;
        }

        /// <summary>
        /// 鼠标离开卡牌触发
        /// </summary>
        public void OnMouseExitCard()
        {
            focus = false;
            focus_timer = -0.2f;
        }

        /// <summary>
        /// 鼠标按下卡牌触发
        /// </summary>
        public void OnMouseDownCard()
        {
            if (GameUI.IsOverUILayer("UI"))
                return;

            UnselectAll();
            drag = true;
            selected = true;
            PlayerControls.Get().UnselectAll();
            AudioTool.Get().PlaySFX("hand_card", AssetData.Get().hand_card_click_audio);
        }

        /// <summary>
        /// 鼠标松开卡牌触发
        /// </summary>
        public void OnMouseUpCard()
        {
            Vector2 mpos = GameCamera.Get().MouseToPercent(Input.mousePosition);
            Vector3 board_pos = GameBoard.Get().RaycastMouseBoard();

            if (drag && mpos.y > 0.25f)
                TryPlayCard(board_pos);
            else if (!GameTool.IsMobile())
                HandCardArea.Get().SortCards();

            drag = false;
        }

        /// <summary>
        /// 尝试在棋盘上使用卡牌
        /// </summary>
        public void TryPlayCard(Vector3 board_pos)
        {
            if (!GameClient.Get().IsYourTurn())
            {
                WarningText.ShowNotYourTurn();
                return;
            }

            BSlot bslot = BSlot.GetNearest(board_pos);
            int player_id = GameClient.Get().GetPlayerID();
            Game gdata = GameClient.Get().GetGameData();
            Player player = gdata.GetPlayer(player_id);
            Card card = GetCard();

            Slot slot = Slot.None;
            if (bslot != null)
                slot = bslot.GetEmptySlot(board_pos);

            if (bslot != null && card.CardData.IsRequireTarget())
                slot = bslot.GetSlot(board_pos);

            if (!Tutorial.Get().CanDo(TutoEndTrigger.PlayCard, card))
                return;

            Card slot_card = bslot?.GetSlotCard(board_pos);
            if (bslot != null && card.CardData.IsRequireTargetSpell() && slot_card != null && slot_card.HasStatus(StatusType.SpellImmunity))
            {
                WarningText.ShowSpellImmune();
                return;
            }

            if (!player.CanPayMana(card))
            {
                WarningText.ShowNoMana();
                return;
            }

            if (gdata.CanPlayCard(card, slot, true))
            {
                PlayCard(slot);
            }
        }

        /// <summary>
        /// 在指定槽位上使用卡牌
        /// </summary>
        public void PlayCard(Slot slot)
        {
            GameClient.Get().PlayCard(GetCard(), slot);
            HandCardArea.Get().DelayRefresh(GetCard());
            Destroy(gameObject);

            if (GameTool.IsMobile())
                BoardCard.UnfocusAll();
        }

        public CardData CardData { get { return GetCardData(); } }

        // 静态方法，获取拖拽卡牌
        public static HandCard GetDrag()
        {
            foreach (HandCard card in card_list)
            {
                if (card.IsDrag())
                    return card;
            }
            return null;
        }

        // 静态方法，获取高亮卡牌
        public static HandCard GetFocus()
        {
            foreach (HandCard card in card_list)
            {
                if (card.IsFocus())
                    return card;
            }
            return null;
        }

        // 静态方法，通过UID获取卡牌
        public static HandCard Get(string uid)
        {
            foreach (HandCard card in card_list)
            {
                if (card && card.GetCardUID() == uid)
                    return card;
            }
            return null;
        }

        // 静态方法，取消所有选择
        public static void UnselectAll()
        {
            foreach (HandCard card in card_list)
                card.selected = false;
        }

        // 静态方法，获取所有手牌
        public static List<HandCard> GetAll()
        {
            return card_list;
        }
    }
}

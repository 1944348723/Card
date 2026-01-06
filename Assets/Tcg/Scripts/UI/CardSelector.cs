using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine.Client;
using TcgEngine;

namespace TcgEngine.UI
{
    /// <summary>
    /// 卡牌选择器界面
    /// 当具有 CardSelector 目标的技能被触发时显示
    /// </summary>
    public class CardSelector : SelectorPanel
    {
        public GameObject card_prefab;              // 卡牌预制体
        public RectTransform content;               // 卡牌内容容器
        public Text title;                           // 面板标题
        public Text subtitle;                        // 面板副标题
        public Button select_button;                 // 确认选择按钮
        public Text select_button_text;              // 确认按钮文本
        public float card_spacing = 100f;            // 卡牌间距

        private AbilityData iability;                // 当前技能
        private List<Card> card_list = new List<Card>();               // 可选择的卡牌列表
        private List<CardSelectorCard> selector_list = new List<CardSelectorCard>(); // 卡牌选择 UI 列表

        private Vector2 mouse_start;                 // 鼠标按下位置
        private int mouse_start_index;               // 鼠标按下时的卡牌索引
        private int selection_index = 0;             // 当前选择卡牌索引
        private bool drag = false;                   // 是否正在拖动卡牌
        private bool force_show = false;             // 是否强制显示面板（用于查看牌堆/弃牌堆）
        private float mouse_scroll = 0f;             // 鼠标滚轮累计值
        private float timer = 0f;                    // 计时器

        private static CardSelector instance;        // 单例实例

        protected override void Awake()
        {
            base.Awake();
            instance = this;
            Hide();                                   // 初始化时隐藏面板
        }

        protected override void Update()
        {
            base.Update();

            timer += Time.deltaTime;

            // 拖动卡牌逻辑
            Vector2 mouse_pos = GetMouseRectPosition();
            Vector2 move = mouse_pos - mouse_start;
            if (drag && move.magnitude > 0.1f)
            {
                selection_index = mouse_start_index - Mathf.RoundToInt(move.x / card_spacing);
                selection_index = Mathf.Clamp(selection_index, 0, selector_list.Count - 1);
            }

            // 鼠标滚轮控制选择卡牌
            mouse_scroll += -Input.mouseScrollDelta.y;
            if (mouse_scroll > 0.5f)
            {
                OnClickNext();
                mouse_scroll -= 1f;
            }
            else if (mouse_scroll < -0.5f)
            {
                OnClickPrev();
                mouse_scroll += 1f;
            }

            // 更新每张卡牌的位置和缩放
            foreach (CardSelectorCard card in selector_list)
            {
                bool is_selected = card.GetIndex() == selection_index;
                Vector3 pos = GetCardPosition(card);
                Vector3 scale = is_selected ? Vector3.one : Vector3.one / 2f;
                card.SetTargetPos(pos);
                card.SetTargetScale(scale);
            }

            // 如果不是选择技能，则右键关闭面板
            if (iability == null && Input.GetMouseButtonDown(1) && timer > 1f)
                Hide();

            Game game = GameClient.Get().GetGameData();
            if (game != null && iability != null && game.selector == SelectorType.None)
                Hide(); // 技能已选择，关闭面板
        }

        // 刷新面板，重新生成卡牌 UI
        public void RefreshPanel()
        {
            foreach (CardSelectorCard card in selector_list)
                Destroy(card.gameObject);
            selector_list.Clear();
            drag = false;
            mouse_scroll = 0f;

            select_button_text.text = (iability != null) ? "Select" : "OK";
            select_button.gameObject.SetActive(iability != null);

            int index = 0;
            foreach (Card card in card_list)
            {
                CardData icard = CardData.Get(card.card_id);
                if (icard != null)
                {
                    GameObject obj = Instantiate(card_prefab, content.transform);

                    RectTransform rect = obj.GetComponent<RectTransform>();
                    CardSelectorCard selector_card = obj.GetComponent<CardSelectorCard>();
                    selector_card.SetCard(card);
                    selector_card.SetIndex(index);

                    Vector3 pos = GetCardPosition(selector_card);
                    Vector3 scale = (index == selection_index ? 1 : 0.5f) * Vector3.one;
                    selector_card.SetTargetPos(pos);
                    selector_card.SetTargetScale(scale);
                    rect.anchoredPosition = pos;
                    selector_list.Add(selector_card);

                    index++;
                }
            }
        }

        // 显示技能相关的卡牌选择
        public override void Show(AbilityData iability, Card caster)
        {
            Game data = GameClient.Get().GetGameData();
            this.card_list = iability.GetCardTargets(data, caster);
            this.iability = iability;
            force_show = false;
            title.text = iability.title;
            subtitle.text = iability.desc;
            selection_index = 0;
            timer = 0f;
            Show();
        }

        // 显示牌堆或弃牌堆的卡牌
        public void Show(List<Card> card_list, string title)
        {
            this.card_list.Clear();
            this.card_list.AddRange(card_list);
            this.card_list.Sort((Card a, Card b) => { return a.CardData.title.CompareTo(b.CardData.title); }); // 按名称排序显示
            this.iability = null;
            force_show = true;
            this.title.text = title;
            subtitle.text = "";
            selection_index = 0;
            timer = 0f;
            Show();
        }

        // 点击确认按钮
        public void OnClickOK()
        {
            Game data = GameClient.Get().GetGameData();
            if (iability != null && data.selector == SelectorType.SelectorCard)
            {
                CardSelectorCard selector_card = null;
                if (selection_index >= 0 && selection_index < selector_list.Count)
                    selector_card = selector_list[selection_index];

                if (selector_card != null)
                {
                    Card selected_card = selector_card.GetCard();
                    Card caster = data.GetCard(data.selector_caster_uid);
                    if (selected_card != null && iability.AreTargetConditionsMet(data, caster, selected_card))
                    {
                        GameClient.Get().SelectCard(selected_card);
                        Hide();
                    }
                }
            }
            else
            {
                Hide();
            }
        }

        // 鼠标按下事件
        public void OnClickMouseDown()
        {
            mouse_start = GetMouseRectPosition();
            mouse_start_index = selection_index;
            drag = true;
        }

        // 鼠标抬起事件
        public void OnClickMouseUp()
        {
            drag = false;
        }

        // 点击取消按钮
        public void OnClickCancel()
        {
            GameClient.Get().CancelSelection();
            Hide();
        }

        // 点击下一个卡牌
        public void OnClickNext()
        {
            selection_index += 1;
            selection_index = Mathf.Clamp(selection_index, 0, selector_list.Count - 1);
        }

        // 点击上一个卡牌
        public void OnClickPrev()
        {
            selection_index -= 1;
            selection_index = Mathf.Clamp(selection_index, 0, selector_list.Count - 1);
        }

        // 根据索引计算卡牌目标位置
        private Vector2 GetCardPosition(CardSelectorCard card)
        {
            int index_offset = card.GetIndex() - selection_index;
            Vector2 pos = new Vector2(index_offset * card_spacing, (index_offset != 0) ? 50f : 0f);
            float center_offset = (index_offset != 0) ? (Mathf.Sign(index_offset) * 140f) : 0;
            pos += Vector2.right * center_offset;
            return pos;
        }

        // 获取鼠标在内容容器内的局部坐标
        private Vector2 GetMouseRectPosition()
        {
            Vector2 localpoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(content, Input.mousePosition, GetComponentInParent<Canvas>().worldCamera, out localpoint);
            return localpoint;
        }

        // 判断当前面板是否是技能选择
        public bool IsAbility()
        {
            return IsVisible() && iability != null;
        }

        // 显示面板并刷新卡牌
        public override void Show(bool instant = false)
        {
            base.Show(instant);
            RefreshPanel();
        }

        // 隐藏面板
        public override void Hide(bool instant = false)
        {
            base.Hide(instant);
            force_show = false;
        }

        // 判断面板是否应该显示
        public override bool ShouldShow()
        {
            Game data = GameClient.Get().GetGameData();
            int player_id = GameClient.Get().GetPlayerID();
            return force_show || (data.selector == SelectorType.SelectorCard && data.selector_player_id == player_id);
        }

        // 获取单例实例
        public static CardSelector Get()
        {
            return instance;
        }
    }
}

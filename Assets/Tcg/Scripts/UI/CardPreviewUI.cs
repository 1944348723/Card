using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine.Client;
using TcgEngine;

namespace TcgEngine.UI
{
    /// <summary>
    /// 游戏场景中的卡牌预览界面
    /// 当鼠标悬停在卡牌上时显示该卡牌的大图和额外信息
    /// </summary>
    public class CardPreviewUI : MonoBehaviour
    {
        public UIPanel ui_panel;                   // 卡牌预览面板
        public CardUI card_ui;                     // 卡牌 UI 显示组件
        public Text desc;                          // 卡牌描述文本
        public float hover_delay_board = 0.7f;     // 在场上悬停显示预览的延迟时间
        public float hover_delay_hand = 0.4f;      // 在手牌悬停显示预览的延迟时间
        public float hover_delay_mobile = 0.1f;    // 移动端悬停显示预览的延迟时间

        public RectTransform[] side_rows;          // 侧边状态行的 RectTransform
        public StatusLine[] status_lines;          // 状态行显示组件

        private float preview_timer = 0f;          // 预览计时器
        private Vector2[] start_pos;               // 侧边状态行初始位置

        private void Start()
        {
            start_pos = new Vector2[side_rows.Length];
            for (int i = 0; i < side_rows.Length; i++)
            {
                start_pos[i] = side_rows[i].anchoredPosition; // 记录初始位置
            }
        }

        void Update()
        {
            if (!GameClient.Get().IsReady())
                return;

            // 隐藏所有状态行
            foreach (StatusLine line in status_lines)
                line.Hide();

            PlayerControls controls = PlayerControls.Get();
            HandCard hcard = HandCard.GetFocus();       // 获取手牌焦点
            BoardCard bcard = BoardCard.GetFocus();     // 获取场上卡牌焦点
            HeroUI hero_ui = HeroUI.GetFocus();         // 获取英雄焦点
            Card histcard = TurnHistoryLine.GetHoverCard(); // 获取历史回合悬停卡牌
            Card secret_card = SecretIconUI.GetHoverCard(); // 获取悬停的秘法卡

            float delay = hcard != null ? hover_delay_hand : hover_delay_board;
            if (GameTool.IsMobile())
                delay = hover_delay_mobile;             // 移动端使用短延迟

            // 确定需要显示预览的卡牌
            Card pcard = hcard != null ? hcard?.GetCard() : bcard?.GetFocusCard();
            if (pcard == null)
                pcard = histcard;
            if (pcard == null)
                pcard = secret_card;
            if (pcard == null)
                pcard = hero_ui?.GetCard();

            // 仅当没有鼠标按下且手牌未拖动时才显示预览
            bool hover_only = !Input.GetMouseButton(0) && !HandCardArea.Get().IsDragging();
            bool should_show_preview = hover_only && pcard != null && !GameUI.IsUIOpened();

            if (should_show_preview)
                preview_timer += Time.deltaTime;       // 增加计时器
            else
                preview_timer = 0f;                    // 重置计时器

            bool show_preview = should_show_preview && preview_timer >= delay;
            ui_panel.SetVisible(show_preview);         // 设置面板显示状态

            if (show_preview)
            {
                CardData icard = pcard.CardData;
                card_ui.SetCard(icard, pcard.VariantData); // 更新卡牌 UI

                // 设置描述文本
                string cdesc = icard.GetDesc();              // 卡牌基础描述
                string adesc = icard.GetAbilitiesDesc();     // 卡牌技能描述
                if (!string.IsNullOrWhiteSpace(cdesc))
                    this.desc.text = cdesc + "\n\n" + adesc;
                else
                    this.desc.text = adesc;

                // 显示卡牌技能状态
                int index = 0;
                foreach (AbilityData ability in pcard.GetAbilities())
                {
                    if (index < status_lines.Length)
                    {
                        // 不显示默认技能（GetAbilitiesDesc 已显示）
                        if (!pcard.CardData.HasAbility(ability) && !string.IsNullOrWhiteSpace(ability.desc))
                        {
                            status_lines[index].SetLine(pcard.CardData, ability);
                            index++;
                        }
                    }
                }

                // 显示卡牌状态效果
                foreach (CardStatus status in pcard.GetAllStatus())
                {
                    if (index < status_lines.Length)
                    {
                        StatusData istatus = StatusData.Get(status.type);
                        if (istatus != null && !string.IsNullOrWhiteSpace(istatus.desc))
                        {
                            int ival = Mathf.Max(status.value, Mathf.CeilToInt(status.duration / 2f));
                            status_lines[index].SetLine(istatus, ival);
                            index++;
                        }
                    }
                }
            }

        }
    }
}

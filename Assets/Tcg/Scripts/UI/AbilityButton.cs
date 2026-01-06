using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine.Client;

namespace TcgEngine.UI
{
    /// <summary>
    /// BoardCard 上的技能按钮，用于激活技能
    /// </summary>

    public class AbilityButton : MonoBehaviour
    {
        public Text text; // 按钮文本
        public Image focus_highlight; // 高亮显示选中状态

        private Card card; // 按钮对应的卡牌
        private AbilityData iability; // 按钮对应的技能数据

        private CanvasGroup canvas_group; // 控制透明度和交互
        private float target_alpha = 0f; // 目标透明度
        private bool focus = false; // 当前是否获得焦点
        private bool nextfocus = false; // 下一帧是否获得焦点
        private bool interactable = false; // 是否可交互

        private static List<AbilityButton> button_list = new List<AbilityButton>(); // 所有技能按钮列表

        void Awake()
        {
            button_list.Add(this); // 添加到按钮列表
            canvas_group = GetComponent<CanvasGroup>();
            canvas_group.alpha = 0f; // 初始透明
            if (focus_highlight != null)
                focus_highlight.enabled = false; // 初始不高亮
        }

        private void OnDestroy()
        {
            button_list.Remove(this); // 从列表移除
        }

        void Update()
        {
            // 平滑更新透明度
            canvas_group.alpha = Mathf.MoveTowards(canvas_group.alpha, target_alpha, 5f * Time.deltaTime);
            focus = nextfocus;

            // 高亮显示控制
            if (focus_highlight != null && IsVisible())
                focus_highlight.enabled = focus && interactable;
        }

        // 设置按钮对应技能和卡牌
        public void SetAbility(Card card, AbilityData iability)
        {
            this.card = card;
            this.iability = iability;
            text.text = iability.title;
            if (this.iability.mana_cost > 0)
                text.text += " (" + this.iability.mana_cost + ")";
            canvas_group.interactable = true;
            canvas_group.blocksRaycasts = true;
            target_alpha = 1f;
        }

        // 设置按钮是否可交互
        public void SetInteractable(bool interact)
        {
            interactable = interact;
        }

        // 隐藏按钮
        public void Hide()
        {
            if (canvas_group == null)
                canvas_group = GetComponent<CanvasGroup>();

            this.card = null;
            this.iability = null;
            canvas_group.interactable = false;
            canvas_group.blocksRaycasts = false;
            target_alpha = 0f;
        }

        // 点击按钮时触发技能
        public void OnClick()
        {
            if (card != null && iability != null)
            {
                if (!Tutorial.Get().CanDo(TutoEndTrigger.CastAbility, card))
                    return;

                GameClient.Get().CastAbility(card, iability);
                PlayerControls.Get().UnselectAll();
            }
        }

        // 获取当前按钮对应技能
        public AbilityData GetAbility()
        {
            return iability;
        }

        // 判断按钮是否可见
        public bool IsVisible()
        {
            return canvas_group.alpha > 0.5f;
        }

        // 判断按钮是否可交互
        public bool IsInteractable()
        {
            return interactable && IsVisible();
        }

        // 鼠标进入时焦点控制
        public void MouseEnter()
        {
            focus = true;
            nextfocus = true;
        }

        // 鼠标离开时焦点控制（移动端延迟一帧）
        public void MouseExit()
        {
            nextfocus = false; 
        }

        // 获取离指定位置最近且有焦点的按钮
        public static AbilityButton GetFocus(Vector3 pos, float range = 999f)
        {
            AbilityButton nearest = null;
            float min_dist = range;
            foreach (AbilityButton button in button_list)
            {
                float dist = (button.transform.position - pos).magnitude;
                if (button.focus && button.IsVisible() && dist < min_dist)
                {
                    min_dist = dist;
                    nearest = button;
                }
            }
            return nearest;
        }

        // 获取离指定位置最近的按钮（不管是否有焦点）
        public static AbilityButton GetNearest(Vector3 pos, float range = 999f)
        {
            AbilityButton nearest = null;
            float min_dist = range;
            foreach (AbilityButton button in button_list)
            {
                float dist = (button.transform.position - pos).magnitude;
                if (dist < min_dist)
                {
                    min_dist = dist;
                    nearest = button;
                }
            }
            return nearest;
        }

    }
}

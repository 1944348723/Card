using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TcgEngine.UI
{
    /// <summary>
    /// 选择面板中的单个选项
    /// 是一个可以点击的按钮
    /// </summary>
    public class ChoiceSelectorChoice : MonoBehaviour
    {
        public Text title;                 // 显示技能标题
        public Text subtitle;              // 显示技能描述
        public Image highlight;            // 高亮显示选中状态

        public UnityAction<int> onClick;  // 点击事件回调，参数为选项索引

        private Button button;             // 按钮组件
        private int choice;                // 当前选项索引
        private bool focus = false;        // 鼠标是否悬停焦点状态

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(OnClick); // 注册按钮点击事件
        }

        private void Update()
        {
            if (highlight != null)
                highlight.enabled = focus; // 根据焦点显示高亮
        }

        // 设置选项数据
        public void SetChoice(int choice, AbilityData ability)
        {
            this.choice = choice;
            this.title.text = ability.title;
            this.subtitle.text = ability.desc;
            button.interactable = true;     // 可点击
            gameObject.SetActive(true);     // 显示选项

            // 如果技能有法力消耗，显示在标题后
            if (ability.mana_cost > 0)
                this.title.text += " (" + ability.mana_cost + ")";
        }

        // 设置按钮是否可交互
        public void SetInteractable(bool interact)
        {
            button.interactable = interact;
        }

        // 隐藏选项
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        // 点击事件触发
        public void OnClick()
        {
            onClick?.Invoke(choice);
        }

        // 鼠标悬停进入
        public void MouseEnter()
        {
            if (button.interactable)
                focus = true;
        }

        // 鼠标悬停退出
        public void MouseExit()
        {
            focus = false;
        }
    }
}

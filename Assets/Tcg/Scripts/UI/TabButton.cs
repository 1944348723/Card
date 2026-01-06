using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace TcgEngine.UI
{
    /// <summary>
    /// 标签按钮（TabButton），用于切换不同的面板（Tab Panel）
    /// </summary>
    public class TabButton : MonoBehaviour
    {
        public string group;             // 所属的按钮组（同组内只能有一个激活）
        public bool active;              // 当前按钮是否被激活
        public GameObject highlight;     // 高亮显示的对象
        public UIPanel ui_panel;         // 对应的面板，当按钮激活时显示

        public UnityAction onClick;                      // 单独点击事件
        public static UnityAction<TabButton> onClickAny; // 全局点击事件（任意Tab按钮点击都会触发）

        private static List<TabButton> tab_list = new List<TabButton>(); // 所有Tab按钮列表

        private void Awake()
        {
            // 添加到全局Tab列表
            tab_list.Add(this);
        }

        private void OnDestroy()
        {
            // 从全局Tab列表移除
            tab_list.Remove(this);
        }

        void Start()
        {
            // 获取Button组件，绑定点击事件
            Button button = GetComponent<Button>();
            if (button != null)
                button.onClick.AddListener(OnClick);

            // 如果按钮默认激活，显示对应面板
            if (active && ui_panel != null)
                ui_panel.Show();
        }

        void Update()
        {
            // 更新高亮显示
            if (highlight != null)
                highlight.SetActive(active);
        }

        /// <summary>
        /// 点击按钮时调用
        /// </summary>
        private void OnClick()
        {
            Activate();          // 激活此按钮
            onClick?.Invoke();   // 调用单独点击事件
            onClickAny?.Invoke(this); // 调用全局点击事件
        }

        /// <summary>
        /// 激活当前按钮，并隐藏同组其他按钮
        /// </summary>
        public void Activate()
        {
            SetAll(group, false); // 同组其他按钮设置为不激活
            active = true;
            if (ui_panel != null)
                ui_panel.Show();
        }

        /// <summary>
        /// 禁用当前按钮
        /// </summary>
        public void Deactivate()
        {
            active = false;
            if (ui_panel != null)
                ui_panel.Hide();
        }

        /// <summary>
        /// 返回按钮是否激活
        /// </summary>
        public bool IsActive()
        {
            return active;
        }

        /// <summary>
        /// 设置指定组的所有Tab按钮为激活或不激活
        /// </summary>
        public static void SetAll(string group, bool act)
        {
            foreach (TabButton btn in tab_list)
            {
                if (btn.group == group)
                {
                    btn.active = act;
                    if(btn.ui_panel != null)
                        btn.ui_panel.SetVisible(act);
                }
            }
        }

        /// <summary>
        /// 获取指定组的所有Tab按钮
        /// </summary>
        public static List<TabButton> GetAll(string group)
        {
            List<TabButton> glist = new List<TabButton>();
            foreach (TabButton btn in tab_list)
            {
                if (btn.group == group)
                    glist.Add(btn);
            }
            return glist;
        }

        /// <summary>
        /// 获取所有Tab按钮
        /// </summary>
        public static List<TabButton> GetAll()
        {
            return tab_list;
        }
    }
}

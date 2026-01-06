using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace TcgEngine.UI
{
    /// <summary>
    /// 图标按钮（IconButton）
    /// 当被点击时，会禁用同一组内的其他按钮，实现类似单选按钮组的效果
    /// </summary>
    public class IconButton : MonoBehaviour
    {
        public string group;     // 按钮所属组，用于管理同组按钮的互斥
        public string value;     // 按钮的值，可用于识别不同按钮

        public Image active_img;   // 激活状态下显示的图标
        public Image disabled_img; // 禁用状态下显示的图标（可选）
        public bool on_if_all_off; // 如果组内所有按钮都关闭，则保持自身激活

        public UnityAction<IconButton> onClick; // 点击事件回调

        private bool active = false;        // 当前按钮是否处于激活状态
        private Button button;              // Button 组件引用
        private static List<IconButton> toggle_list = new List<IconButton>(); // 所有 IconButton 列表，用于组管理

        void Awake()
        {
            toggle_list.Add(this);                  // 添加到静态列表中
            button = GetComponent<Button>();        // 获取 Button 组件
            button.onClick.AddListener(OnClick);   // 注册点击事件

            if(!on_if_all_off && active_img != null)
                active_img.enabled = false;        // 初始状态禁用激活图标
        }

        private void OnDestroy()
        {
            toggle_list.Remove(this);               // 从列表中移除
        }

        void Start()
        {
            // 可扩展初始化逻辑
        }

        private void Update()
        {
            // 如果开启 on_if_all_off 功能，当组内所有按钮都关闭时保持自身激活
            if (on_if_all_off)
            {
                if (active_img != null && IsAllOff(group))
                {
                    active_img.enabled = true;
                }
            }
        }

        /// <summary>
        /// 点击事件处理
        /// </summary>
        void OnClick()
        {
            bool was_active = active;   // 记录点击前的状态

            DeactivateAll(group);       // 关闭同组所有按钮

            if (!was_active)            // 如果之前未激活，则激活自身
                Activate();

            if (onClick != null)        // 触发回调
                onClick.Invoke(this);
        }

        /// <summary>
        /// 设置按钮状态
        /// </summary>
        public void SetActive(bool act)
        {
            if (act) Activate();
            else Deactivate();
        }

        /// <summary>
        /// 激活按钮
        /// </summary>
        public void Activate()
        {
            active = true;
            if (active_img != null)
                active_img.enabled = true;
        }

        /// <summary>
        /// 禁用按钮
        /// </summary>
        public void Deactivate()
        {
            active = false;
            if (active_img != null)
                active_img.enabled = false;
        }

        /// <summary>
        /// 获取按钮是否处于激活状态
        /// </summary>
        public bool IsActive()
        {
            return active;
        }

        /// <summary>
        /// 检查组内是否所有按钮都处于关闭状态
        /// </summary>
        public static bool IsAllOff(string group)
        {
            bool all_off = true;
            foreach (IconButton toggle in toggle_list)
            {
                if (toggle.group == group && toggle.IsActive())
                    all_off = false;
            }
            return all_off;
        }

        /// <summary>
        /// 禁用组内所有按钮
        /// </summary>
        public static void DeactivateAll(string group)
        {
            foreach (IconButton toggle in toggle_list)
            {
                if (toggle.group == group)
                    toggle.Deactivate();
            }
        }

        /// <summary>
        /// 获取组内所有按钮
        /// </summary>
        public static List<IconButton> GetAll(string group)
        {
            List<IconButton> toggles = new List<IconButton>();
            foreach (IconButton toggle in toggle_list)
            {
                if (toggle.group == group)
                    toggles.Add(toggle);
            }
            return toggles;
        }
    }
}

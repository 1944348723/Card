using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TcgEngine.UI
{
    /// <summary>
    /// 悬停文本框
    /// 当鼠标悬停在 HoverTarget 或 HoverTargetUI 上时显示的文本框
    /// </summary>
    public class HoverTextBox : MonoBehaviour
    {
        public UIPanel panel_left;   // 左侧显示的文本面板
        public UIPanel panel_right;  // 右侧显示的文本面板
        public Text text1;           // 左侧文本内容
        public Text text2;           // 右侧文本内容

        private HoverTarget current;      // 当前悬停的场景目标
        private HoverTargetUI current_ui; // 当前悬停的UI目标

        private RectTransform rect_left;  // 左侧面板的RectTransform
        private RectTransform rect_right; // 右侧面板的RectTransform

        private static HoverTextBox instance; // 单例实例

        void Awake()
        {
            instance = this;
            rect_left = panel_left.GetComponent<RectTransform>();
            rect_right = panel_right.GetComponent<RectTransform>();
        }

        void Update()
        {
            // 如果有悬停目标，则跟随鼠标移动
            if (current != null || current_ui != null)
            {
                transform.position = GameUI.MouseToWorld(Input.mousePosition); // 将文本框移动到鼠标位置
                transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward, Vector2.up); // 面向摄像机

                // 根据鼠标在屏幕的位置选择显示左侧还是右侧面板
                panel_left.SetVisible(transform.position.x > 0f);
                panel_right.SetVisible(transform.position.x <= 0f);

                // 如果悬停目标不再悬停，则隐藏文本框
                if (current != null && !current.IsHover())
                    Hide();
                if (current_ui != null && !current_ui.IsHover())
                    Hide();
            }
        }

        /// <summary>
        /// 显示场景悬停目标的文本框
        /// </summary>
        public void Show(HoverTarget hover)
        {
            current = hover;
            current_ui = null;
            text1.text = hover.GetText();
            text2.text = hover.GetText();
            text1.fontSize = hover.text_size;
            text2.fontSize = hover.text_size;
            rect_left.sizeDelta = new Vector2(hover.width, hover.height);
            rect_right.sizeDelta = new Vector2(hover.width, hover.height);
        }

        /// <summary>
        /// 显示UI悬停目标的文本框
        /// </summary>
        public void Show(HoverTargetUI hover)
        {
            current = null;
            current_ui = hover;
            text1.text = hover.GetText();
            text2.text = hover.GetText();
            text1.fontSize = hover.text_size;
            text2.fontSize = hover.text_size;
            rect_left.sizeDelta = new Vector2(hover.width, hover.height);
            rect_right.sizeDelta = new Vector2(hover.width, hover.height);
        }

        /// <summary>
        /// 隐藏悬停文本框
        /// </summary>
        public void Hide()
        {
            current = null;
            current_ui = null;
            panel_left.Hide();
            panel_right.Hide();
        }

        /// <summary>
        /// 获取单例实例
        /// </summary>
        public static HoverTextBox Get()
        {
            return instance;
        }
    }
}

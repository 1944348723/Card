using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine;

namespace TcgEngine.UI
{
    /// <summary>
    /// 改变Unity UI Toggle按钮的文字颜色
    /// 根据Toggle的开关状态，显示不同颜色
    /// </summary>
    public class ToggleText : MonoBehaviour
    {
        public Color on_color = Color.yellow;   // Toggle开启时文字颜色
        public Color off_color = Color.white;   // Toggle关闭时文字颜色

        private Toggle toggle;      // 按钮Toggle组件
        private Text toggle_txt;    // Toggle下的文字组件

        private bool previous = false; // 上一次Toggle状态，用于检测变化

        void Awake()
        {
            // 获取Toggle组件和子文字组件
            toggle = GetComponent<Toggle>();
            toggle_txt = GetComponentInChildren<Text>();
        }

        private void Start()
        {
            // 初始化刷新文字颜色
            Refresh();
        }

        void Update()
        {
            // 如果Toggle状态发生变化，刷新文字颜色
            if (previous != toggle.isOn)
                Refresh();
        }

        /// <summary>
        /// 刷新文字颜色，根据Toggle状态切换颜色
        /// </summary>
        private void Refresh()
        {
            toggle_txt.color = toggle.isOn ? on_color : off_color;
            previous = toggle.isOn;
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine;

namespace TcgEngine.UI
{
    /// <summary>
    /// OptionString 类，用于存储选项的值和显示标题
    /// </summary>
    [System.Serializable]
    public class OptionString
    {
        public string value; // 选项对应的实际值
        public string title; // 选项显示在UI上的标题
    }

    /// <summary>
    /// OptionSelector 选项选择器
    /// UI 元素，带有左右两个箭头，可以从预设选项中选择一个字符串
    /// </summary>
    public class OptionSelector : MonoBehaviour
    {
        [Header("Options")]
        public OptionString[] options; // 预设的选项数组

        [Header("Display")]
        public Text select_text; // 显示当前选项标题的文本组件

        private int position = 0; // 当前选中的选项索引

        void Start()
        {
            SetIndex(0); // 初始化默认选择第一个选项
        }

        void Update()
        {
            // 当前不需要每帧更新逻辑
        }

        /// <summary>
        /// 值改变后的处理
        /// 更新显示文本
        /// </summary>
        private void AfterChangeOption()
        {
            if (select_text != null)
                select_text.text = GetSelectedTitle();
        }

        /// <summary>
        /// 点击左箭头，选择上一个选项
        /// </summary>
        public void OnClickLeft()
        {
            position = (position + options.Length - 1) % options.Length; // 循环选择
            AfterChangeOption();
        }

        /// <summary>
        /// 点击右箭头，选择下一个选项
        /// </summary>
        public void OnClickRight()
        {
            position = (position + options.Length + 1) % options.Length; // 循环选择
            AfterChangeOption();
        }

        /// <summary>
        /// 设置选中索引
        /// </summary>
        public void SetIndex(int index)
        {
            position = index;
            AfterChangeOption();
        }

        /// <summary>
        /// 获取当前选中的 OptionString 对象
        /// </summary>
        public OptionString GetSelected()
        {
            return options[position];
        }

        /// <summary>
        /// 获取当前选中的值（value）
        /// </summary>
        public string GetSelectedValue()
        {
            return options[position].value;
        }

        /// <summary>
        /// 获取当前选中的显示标题（title）
        /// </summary>
        public string GetSelectedTitle()
        {
            return options[position].title;
        }
    }
}

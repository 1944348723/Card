using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TcgEngine;

namespace TcgEngine.UI
{
    /// <summary>
    /// OptionInt 类，用于存储整数选项及其显示标题
    /// </summary>
    [System.Serializable]
    public class OptionInt
    {
        public int value;   // 选项的整数值
        public string title; // 选项显示的文本标题
    }

    /// <summary>
    /// OptionSelectorInt 选项选择器（整数版）
    /// UI 元素，带有左右两个箭头，可以从预设的整数选项中选择一个
    /// </summary>
    public class OptionSelectorInt : MonoBehaviour
    {
        [Header("Options")]
        public OptionInt[] options; // 预设的整数选项数组

        [Header("Display")]
        public Text select_text; // 显示当前选中选项的文本UI

        public UnityAction onChange; // 值改变时的回调事件

        private int position = 0; // 当前选中选项的索引
        private bool is_locked = false; // 是否锁定选择器（禁止左右切换）

        void Start()
        {
            SetIndex(0); // 默认选择第一个选项
        }

        void Update()
        {
            // 当前不需要每帧更新逻辑
        }

        /// <summary>
        /// 选项改变后的处理
        /// 更新显示文本并触发回调
        /// </summary>
        private void AfterChangeOption()
        {
            if (select_text != null)
                select_text.text = GetSelectedTitle();
            onChange?.Invoke();
        }

        /// <summary>
        /// 点击左箭头，选择上一个整数选项
        /// </summary>
        public void OnClickLeft()
        {
            if (is_locked)
                return;

            position = (position + options.Length - 1) % options.Length; // 循环选择
            AfterChangeOption();
        }

        /// <summary>
        /// 点击右箭头，选择下一个整数选项
        /// </summary>
        public void OnClickRight()
        {
            if (is_locked)
                return;

            position = (position + options.Length + 1) % options.Length; // 循环选择
            AfterChangeOption();
        }

        /// <summary>
        /// 设置当前选中索引
        /// </summary>
        public void SetIndex(int index)
        {
            position = index;
            if (select_text != null)
                select_text.text = GetSelectedTitle();
        }

        /// <summary>
        /// 根据整数值设置选中项
        /// </summary>
        public void SetValue(int value)
        {
            for (int i = 0; i < options.Length; i++)
            {
                if (options[i].value == value)
                    position = i;
            }

            if (select_text != null)
                select_text.text = GetSelectedTitle();
        }

        /// <summary>
        /// 设置选择器是否锁定（禁止切换选项）
        /// </summary>
        public void SetLocked(bool locked)
        {
            is_locked = locked;
        }

        /// <summary>
        /// 获取当前选中的 OptionInt 对象
        /// </summary>
        public OptionInt GetSelected()
        {
            return options[position];
        }

        /// <summary>
        /// 获取当前选中的整数值
        /// </summary>
        public int GetSelectedValue()
        {
            return options[position].value;
        }

        /// <summary>
        /// 获取当前选中的标题文本，如果标题为空，则返回整数值的字符串
        /// </summary>
        public string GetSelectedTitle()
        {
            if (!string.IsNullOrWhiteSpace(options[position].title))
                return options[position].title;
            return options[position].value.ToString();
        }
    }
}

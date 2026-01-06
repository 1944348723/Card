using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TcgEngine;

namespace TcgEngine.UI
{
    /// <summary>
    /// NumberSelector 数字选择器
    /// 允许在最小值和最大值之间选择一个整数
    /// </summary>
    public class NumberSelector : MonoBehaviour
    {
        [Header("Options")]
        public int value;        // 当前选中的值
        public int value_min;    // 最小值
        public int value_max;    // 最大值

        [Header("Display")]
        public Text select_text; // 显示当前值的文本组件

        public UnityAction onChange; // 值改变时触发的回调事件

        private bool is_locked = false; // 是否锁定选择器（禁止更改值）

        void Start()
        {
            SetValue(0); // 初始化值为0（会自动限制在min-max范围内）
        }

        void Update()
        {
            // 当前不需要每帧更新逻辑
        }

        /// <summary>
        /// 值改变后的处理
        /// 更新显示文本并触发回调
        /// </summary>
        private void AfterChangeOption()
        {
            if (select_text != null)
                select_text.text = value.ToString();
            onChange?.Invoke();
        }

        /// <summary>
        /// 点击左箭头，值减一
        /// </summary>
        public void OnClickLeft()
        {
            if (is_locked)
                return;

            value--;
            value = Mathf.Clamp(value, value_min, value_max); // 限制在范围内
            AfterChangeOption();
        }

        /// <summary>
        /// 点击右箭头，值加一
        /// </summary>
        public void OnClickRight()
        {
            if (is_locked)
                return;

            value++;
            value = Mathf.Clamp(value, value_min, value_max); // 限制在范围内
            AfterChangeOption();
        }

        /// <summary>
        /// 设置当前值
        /// </summary>
        public void SetValue(int val)
        {
            value = Mathf.Clamp(val, value_min, value_max); // 限制范围

            if (select_text != null)
                select_text.text = value.ToString(); // 更新显示文本
        }

        /// <summary>
        /// 设置最小值
        /// </summary>
        public void SetMin(int min)
        {
            value_min = min;
        }

        /// <summary>
        /// 设置最大值
        /// </summary>
        public void SetMax(int max)
        {
            value_max = max;
        }

        /// <summary>
        /// 设置选择器是否锁定
        /// 锁定时不能改变值
        /// </summary>
        public void SetLocked(bool locked)
        {
            is_locked = locked;
        }

    }
}

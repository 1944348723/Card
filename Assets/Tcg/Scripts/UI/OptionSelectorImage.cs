using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TcgEngine;

namespace TcgEngine.UI
{
    /// <summary>
    /// OptionImage 类，用于存储选项的值和对应的图片
    /// </summary>
    [System.Serializable]
    public class OptionImage
    {
        public string value; // 选项对应的实际值
        public Sprite image; // 选项对应的图片
    }

    /// <summary>
    /// OptionSelectorImage 选项选择器（图片版）
    /// UI 元素，带有左右两个箭头，可以从预设的图片选项中选择一个
    /// </summary>
    public class OptionSelectorImage : MonoBehaviour
    {
        [Header("Options")]
        public OptionImage[] options; // 预设的图片选项数组

        [Header("Display")]
        public Image select_img; // 显示当前选中图片的UI组件

        public UnityAction onChange; // 值改变时的回调事件

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
        /// 选项改变后的处理
        /// 更新显示图片，并触发回调
        /// </summary>
        private void AfterChangeOption()
        {
            if (select_img != null)
                select_img.sprite = GetSelectedImage();
            onChange?.Invoke();
        }

        /// <summary>
        /// 点击左箭头，选择上一个图片选项
        /// </summary>
        public void OnClickLeft()
        {
            position = (position + options.Length - 1) % options.Length; // 循环选择
            AfterChangeOption();
        }

        /// <summary>
        /// 点击右箭头，选择下一个图片选项
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
        /// 根据值设置选中项
        /// </summary>
        public void SetValue(string value)
        {
            for (int i = 0; i < options.Length; i++)
            {
                if (options[i].value == value)
                    position = i;
            }

            if (select_img != null)
                select_img.sprite = GetSelectedImage();
        }

        /// <summary>
        /// 获取当前选中的 OptionImage 对象
        /// </summary>
        public OptionImage GetSelected()
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
        /// 获取当前选中的图片（Sprite）
        /// </summary>
        public Sprite GetSelectedImage()
        {
            return options[position].image;
        }
    }
}

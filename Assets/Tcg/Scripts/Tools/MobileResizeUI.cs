using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine.UI
{
    /// <summary>
    /// 移动端 UI 调整工具
    /// 添加到任意 UI 元素上，在移动设备上可以调整位置和缩放
    /// 一般用于按钮在移动端需要更大或位置偏移的情况
    /// </summary>
    public class MobileResizeUI : MonoBehaviour
    {
        /// <summary>
        /// 在移动端相对于原始位置的偏移量
        /// </summary>
        public Vector2 position_offset;

        /// <summary>
        /// 在移动端的缩放比例（1 = 原始大小）
        /// </summary>
        public float size = 1f;

        void Start()
        {
            // 如果当前设备是移动端
            if (GameTool.IsMobile())
            {
                RectTransform rect = GetComponent<RectTransform>();
                
                // 调整位置
                rect.anchoredPosition += position_offset;

                // 调整缩放
                transform.localScale = transform.localScale * size;
            }
        }
    }
}
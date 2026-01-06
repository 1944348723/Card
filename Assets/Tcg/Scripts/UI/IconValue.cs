using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TcgEngine.UI
{
    /// <summary>
    /// IconValue 图标组件
    /// 根据设置的 value 值，显示对应的图标
    /// </summary>
    public class IconValue : MonoBehaviour
    {
        public int value;                  // 当前值，用于选择显示哪一个图标
        public bool auto_refresh = true;   // 是否每帧自动刷新图标

        public Sprite[] values;            // 可选图标数组，每个索引对应一个 value 值

        private Image image;               // 当前图标的 Image 组件

        void Awake()
        {
            image = GetComponent<Image>(); // 获取 Image 组件
        }

        void Update()
        {
            if (auto_refresh)
                Refresh();                 // 自动刷新图标显示
        }

        /// <summary>
        /// 根据 value 刷新图标显示
        /// </summary>
        public void Refresh()
        {
            if (image == null)
                image = GetComponent<Image>();

            if (value >= 0 && value < values.Length)
            {
                image.sprite = values[value];         // 设置对应索引的图标
                image.enabled = image.sprite != null; // 如果图标为空则隐藏
            }
        }

        /// <summary>
        /// 设置图标材质
        /// </summary>
        public void SetMat(Material mat)
        {
            if (image == null)
                image = GetComponent<Image>();

            image.material = mat;
        }
    }
}
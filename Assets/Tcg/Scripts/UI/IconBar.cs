using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TcgEngine.UI
{
    /// <summary>
    /// 图标条（IconBar）
    /// 显示多个图标来表示某个数值，例如游戏中的法力值条
    /// </summary>
    public class IconBar : MonoBehaviour
    {
        public int value = 0;        // 当前数值
        public int max_value = 4;    // 最大数值
        public bool auto_refresh = true; // 是否自动刷新显示

        public Image[] icons;        // 图标数组
        public Sprite sprite_full;   // 图标满时的显示图片
        public Sprite sprite_empty;  // 图标空时的显示图片

        void Awake()
        {
            // 初始化逻辑，可扩展
        }

        void Update()
        {
            // 如果开启自动刷新，每帧刷新图标显示
            if (auto_refresh)
                Refresh();
        }

        /// <summary>
        /// 刷新图标显示
        /// 根据当前数值 value 设置满图标或空图标
        /// </summary>
        public void Refresh()
        {
            int index = 0;
            foreach (Image icon in icons)
            {
                // 如果索引小于当前值或最大值，则显示图标
                icon.gameObject.SetActive(index < value || index < max_value);
                // 设置图标图片：满图标或空图标
                icon.sprite = (index < value) ? sprite_full : sprite_empty;
                index++;
            }
        }

        /// <summary>
        /// 设置所有图标的材质
        /// </summary>
        public void SetMat(Material mat)
        {
            foreach (Image icon in icons)
            {
                icon.material = mat;
            }
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine.FX
{
    /// <summary>
    /// 鼠标悬停时显示的特效（FX）
    /// </summary>
    public class HoverFX : MonoBehaviour
    {
        // 悬停时显示的特效对象
        public GameObject fx;

        // 当前是否处于悬停状态
        private bool hover = false;
        
        void Update()
        {
            // 根据 hover 状态控制特效的显示或隐藏
            if (hover != fx.activeSelf)
                fx.SetActive(hover);
        }

        /// <summary>
        /// 鼠标指针进入对象时调用
        /// </summary>
        public void PointerEnter()
        {
            hover = true;  // 设置悬停状态为 true
        }

        /// <summary>
        /// 鼠标指针离开对象时调用
        /// </summary>
        public void PointerExit()
        {
            hover = false; // 设置悬停状态为 false
        }
    }
}
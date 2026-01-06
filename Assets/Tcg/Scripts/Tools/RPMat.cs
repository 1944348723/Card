using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TcgEngine
{
    /// <summary>
    /// 根据渲染管线(Render Pipeline)切换材质
    /// 在URP(通用渲染管线)下使用指定材质
    /// </summary>
    public class RPMat : MonoBehaviour
    {
        // URP专用材质
        public Material mat_urp;

        // 可能存在的SpriteRenderer组件
        private SpriteRenderer render;

        // 可能存在的UI Image组件
        private Image image;
       
        /// <summary>
        /// 初始化
        /// </summary>
        void Start()
        {
            // 获取SpriteRenderer组件
            render = GetComponent<SpriteRenderer>();

            // 获取Image组件
            image = GetComponent<Image>();

            // 如果是SpriteRenderer且使用URP，替换材质
            if (render != null && GameTool.IsURP())
                render.material = mat_urp;

            // 如果是UI Image且使用URP，替换材质
            if (image != null && GameTool.IsURP())
                image.material = mat_urp;
        }
    }
}
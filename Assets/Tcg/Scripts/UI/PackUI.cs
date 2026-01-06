using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TcgEngine;
using UnityEngine.EventSystems;

namespace TcgEngine.UI
{
    /// <summary>
    /// PackUI 类用于显示一个卡包及其相关信息
    /// </summary>
    public class PackUI : MonoBehaviour, IPointerClickHandler
    {
        public Image pack_img;          // 卡包的图像显示
        public Text pack_title;         // 卡包标题文本
        public Text pack_quantity;      // 卡包数量文本
        public Image quantity_bar;      // 卡包数量进度条显示

        public UnityAction<PackUI> onClick;       // 左键点击事件回调
        public UnityAction<PackUI> onClickRight;  // 右键点击事件回调

        private PackData pack;          // 当前显示的卡包数据

        void Awake()
        {
            // Awake 中没有逻辑
        }

        /// <summary>
        /// 设置要显示的卡包（不带数量）
        /// </summary>
        public void SetPack(PackData pack)
        {
            this.pack = pack;

            if (pack != null)
            {
                if (pack_title != null)
                {
                    pack_title.enabled = true;
                    pack_title.text = pack.title; // 设置标题文本
                }
                pack_img.enabled = true;
                pack_img.sprite = pack.pack_img; // 设置卡包图片
            }

            if (pack_quantity != null)
                pack_quantity.enabled = false; // 默认隐藏数量
            if (quantity_bar != null)
                quantity_bar.enabled = false;   // 默认隐藏数量条
        }

        /// <summary>
        /// 设置要显示的卡包及其数量
        /// </summary>
        public void SetPack(PackData pack, int quantity)
        {
            SetPack(pack);

            if (pack_quantity != null)
            {
                pack_quantity.enabled = quantity > 0;  // 有数量时显示
                pack_quantity.text = quantity.ToString(); // 设置数量文本
            }

            if (quantity_bar != null)
                quantity_bar.enabled = quantity > 0;  // 有数量时显示数量条
        }

        /// <summary>
        /// 隐藏当前显示的卡包
        /// </summary>
        public void Hide()
        {
            this.pack = null;
            pack_img.enabled = false;
            if(pack_title != null)
                pack_title.enabled = false;
            if (pack_quantity != null)
                pack_quantity.enabled = false;
            if (quantity_bar != null)
                quantity_bar.enabled = false;
        }

        /// <summary>
        /// 响应鼠标点击事件
        /// 左键触发 onClick，右键触发 onClickRight
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                if (onClick != null)
                    onClick.Invoke(this);
            }

            if (eventData.button == PointerEventData.InputButton.Right)
            {
                if (onClickRight != null)
                    onClickRight.Invoke(this);
            }
        }

        /// <summary>
        /// 获取当前显示的卡包数据
        /// </summary>
        public PackData GetPack()
        {
            return pack;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TcgEngine;

namespace TcgEngine.UI
{
    /// <summary>
    /// 显示卡背的 UI 组件
    /// </summary>
    
    public class CardbackUI : MonoBehaviour
    {
        public UnityAction<CardbackData> onClick; // 点击卡背时触发的事件

        private Image cardback_img; // 显示卡背的 Image 组件
        private Button cardback_button; // 卡背的 Button 组件
        private Sprite default_icon; // 默认卡背图标

        private CardbackData cardback; // 当前卡背数据

        void Awake()
        {
            cardback_img = GetComponent<Image>(); // 获取 Image 组件
            cardback_button = GetComponent<Button>(); // 获取 Button 组件
            default_icon = cardback_img.sprite; // 保存默认卡背

            if (cardback_button != null)
                cardback_button.onClick.AddListener(OnClick); // 注册点击事件
        }

        // 设置卡背数据
        public void SetCardback(CardbackData cardback)
        {
            this.cardback = cardback;
            cardback_img.enabled = true;
            cardback_img.sprite = default_icon;

            if (cardback != null)
            {
                cardback_img.sprite = cardback.cardback; // 显示卡背
            }
        }

        // 设置默认卡背
        public void SetDefaultCardback()
        {
            this.cardback = null;
            cardback_img.enabled = true;
            cardback_img.sprite = default_icon;
        }

        // 隐藏卡背
        public void Hide()
        {
            this.cardback = null;
            cardback_img.enabled = false;
        }

        // 获取当前卡背数据
        public CardbackData GetCardback()
        {
            return cardback;
        }

        // 点击卡背时调用
        private void OnClick()
        {
            if (cardback != null)
                onClick?.Invoke(cardback); // 触发点击事件
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TcgEngine.UI
{
    /// <summary>
    /// 手牌弃牌阶段的卡牌界面控制
    /// </summary>
    public class CardMulligan : MonoBehaviour
    {
        public CardUI card_ui;          // 卡牌 UI 组件
        public Image x_img;             // 标记弃牌的 X 图标

        private Card card;              // 当前关联的卡牌对象

        public UnityAction<CardMulligan> onClick; // 点击回调事件

        private void Awake()
        {
            if (x_img != null)
                x_img.enabled = false;  // 初始化时隐藏 X 图标

            card_ui.onClick += OnClick; // 绑定卡牌点击事件
        }

        // 设置当前卡牌
        public void SetCard(Card card)
        {
            this.card = card;
            card_ui.SetCard(card.CardData, card.VariantData); // 更新 UI 显示
            gameObject.SetActive(true);                       // 显示该卡牌对象
        }

        // 设置卡牌是否被选择弃掉
        public void SetSelected(bool discard)
        {
            if (x_img != null)
                x_img.enabled = discard; // 根据 discard 显示或隐藏 X 图标
        }

        // 隐藏卡牌 UI
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        // 判断卡牌是否被选择弃掉
        public bool IsSelected()
        {
            if (x_img != null)
                return x_img.enabled;
            return false;
        }

        // 获取当前关联的卡牌
        public Card GetCard()
        {
            return card;
        }

        // 卡牌点击事件触发
        private void OnClick(CardUI card_ui)
        {
            onClick?.Invoke(this); // 调用回调
        }

    }
}
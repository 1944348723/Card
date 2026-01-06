using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine;

namespace TcgEngine.UI
{
    /// <summary>
    /// 卡牌选择器中的单张卡牌
    /// </summary>
    public class CardSelectorCard : MonoBehaviour
    {
        public CardUI card_ui;                // 显示卡牌信息的 UI 组件

        private int index;                     // 卡牌在选择器中的索引
        private Vector2 target_pos;            // 卡牌目标位置
        private Vector3 target_scale;          // 卡牌目标缩放

        private Card card;                     // 对应的卡牌数据

        private RectTransform rect;            // 卡牌 RectTransform 组件

        private void Awake()
        {
            rect = GetComponent<RectTransform>(); // 获取 RectTransform
        }

        private void Start()
        {
            transform.localScale = target_scale; // 初始化缩放
        }

        private void Update()
        {
            // 平滑移动到目标位置
            rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, target_pos, 5f * Time.deltaTime);
            // 平滑缩放到目标缩放
            transform.localScale = Vector2.Lerp(transform.localScale, target_scale, 2f * Time.deltaTime);
        }

        // 设置卡牌数据
        public void SetCard(Card card)
        {
            this.card = card;
            CardData icard = CardData.Get(card.card_id);
            card_ui.SetCard(icard, card.VariantData); // 更新 UI
        }

        // 设置索引
        public void SetIndex(int index)
        {
            this.index = index;
        }

        // 设置目标位置
        public void SetTargetPos(Vector3 pos)
        {
            target_pos = pos;
        }

        // 设置目标缩放
        public void SetTargetScale(Vector3 scale)
        {
            target_scale = scale;
        }

        // 获取卡牌数据
        public Card GetCard()
        {
            return card;
        }

        // 获取索引
        public int GetIndex()
        {
            return index;
        }

        // 获取目标位置
        public Vector3 GetTargetPos()
        {
            return target_pos;
        }

        // 获取目标缩放
        public Vector3 GetTargetScale()
        {
            return target_scale;
        }
    }
}

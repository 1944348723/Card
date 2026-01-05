using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TcgEngine.Client
{
    /// <summary>
    /// 对手手牌的简化版显示
    /// 功能类似 HandCard，但只显示背面，不显示详细信息
    /// </summary>
    public class HandCardBack : MonoBehaviour
    {
        public Image card_sprite;  // 卡牌背面显示图片

        private RectTransform rect; // 卡牌RectTransform引用

        private static List<HandCardBack> card_list = new List<HandCardBack>(); // 当前对手手牌列表

        void Awake()
        {
            card_list.Add(this); // 添加到列表中
            rect = GetComponent<RectTransform>();
            SetCardback(null); // 初始化卡背为默认状态
        }

        private void OnDestroy()
        {
            card_list.Remove(this); // 从列表中移除
        }

        /// <summary>
        /// 设置卡背图片
        /// </summary>
        /// <param name="cb">卡背数据</param>
        public void SetCardback(CardbackData cb)
        {
            if (cb != null && cb.cardback != null)
                card_sprite.sprite = cb.cardback;
        }

        /// <summary>
        /// 获取RectTransform引用
        /// </summary>
        public RectTransform GetRect()
        {
            if (rect == null)
                return GetComponent<RectTransform>();
            return rect;
        }

    }
}
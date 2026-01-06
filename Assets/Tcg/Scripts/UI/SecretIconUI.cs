using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TcgEngine.UI
{
    /// <summary>
    /// 显示玩家密技（秘密）的图标UI
    /// 可以检测鼠标悬停以显示对应的卡牌信息
    /// </summary>
    public class SecretIconUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private Card card = null;               // 绑定的密技卡牌
        private bool is_hover = false;          // 是否鼠标悬停在图标上

        private static List<SecretIconUI> icon_list = new List<SecretIconUI>(); // 所有密技图标实例列表

        void Awake()
        {
            icon_list.Add(this);                // 添加到列表
        }

        void OnDestroy()
        {
            icon_list.Remove(this);             // 从列表移除
        }

        /// <summary>
        /// 绑定密技卡牌
        /// </summary>
        public void SetCard(Card card)
        {
            this.card = card;
        }

        /// <summary>
        /// 鼠标进入图标区域
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            is_hover = true;
        }

        /// <summary>
        /// 鼠标离开图标区域
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            is_hover = false;
        }

        void OnDisable()
        {
            is_hover = false;                   // 禁用时重置悬停状态
        }

        /// <summary>
        /// 获取绑定的卡牌
        /// </summary>
        public Card GetCard()
        {
            return card;
        }

        /// <summary>
        /// 获取当前鼠标悬停的密技卡牌
        /// </summary>
        public static Card GetHoverCard()
        {
            foreach (SecretIconUI line in icon_list)
            {
                if (line.card != null && line.is_hover)
                    return line.card;           // 返回悬停的卡牌
            }
            return null;
        }
    }
}
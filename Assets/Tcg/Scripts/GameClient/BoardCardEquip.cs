using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TcgEngine.UI;

namespace TcgEngine.Client
{
    /// <summary>
    /// 棋盘上卡牌的装备显示组件
    /// 用于显示装备的图标、生命值和高亮效果
    /// </summary>
    public class BoardCardEquip : MonoBehaviour
    {
        public Image equip_sprite;       // 装备卡牌的图像
        public Image equip_glow;         // 装备高亮效果图像
        public Text equip_hp;            // 装备的生命值显示文本

        public Color glow_ally;          // 友方高亮颜色
        public Color glow_enemy;         // 敌方高亮颜色

        private Canvas canvas;           // 所在Canvas
        private RectTransform rect;      // UI矩形组件

        private Card equip;              // 当前显示的装备卡牌
        private bool focus;              // 是否处于焦点状态（鼠标悬停或选中）
        private float target_alpha = 0f; // 高亮透明度目标值

        void Awake()
        {
            canvas = GetComponentInParent<Canvas>(); // 获取父Canvas
            rect = GetComponent<RectTransform>();   // 获取自身RectTransform
        }

        private void Update()
        {
            if (equip != null)
            {
                // 根据焦点状态设置目标透明度
                target_alpha = focus ? 1f : 0f;
                // 判断鼠标是否悬停在该UI上
                focus = GameUI.IsOverRectTransform(canvas, rect);
            }
            else
            {
                target_alpha = 0f;
                focus = false;
            }

            if (equip_glow != null)
            {
                // 根据玩家身份选择高亮颜色
                int player_id = GameClient.Get().GetPlayerID();
                Color ccolor = player_id == equip.player_id ? glow_ally : glow_enemy;
                // 平滑过渡高亮透明度
                float calpha = Mathf.MoveTowards(equip_glow.color.a, target_alpha * ccolor.a, 4f * Time.deltaTime);
                equip_glow.color = new Color(ccolor.r, ccolor.g, ccolor.b, calpha);
            }
        }

        /// <summary>
        /// 设置装备卡牌并显示
        /// </summary>
        /// <param name="equip">要显示的装备卡牌</param>
        public void SetEquip(Card equip)
        {
            if (equip != null)
            {
                this.equip = equip;
                equip_sprite.sprite = equip.CardData.GetBoardArt(equip.VariantData); // 设置装备图像
                equip_hp.text = equip.GetHP().ToString(); // 设置装备生命值文本

                if (!gameObject.activeSelf)
                    gameObject.SetActive(true); // 激活UI
            }
            else
            {
                Hide(); // 没有装备则隐藏
            }
        }

        /// <summary>
        /// 隐藏装备UI
        /// </summary>
        public void Hide()
        {
            this.equip = null;
            focus = false;
            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }

        /// <summary>
        /// 判断装备是否处于焦点状态
        /// </summary>
        public bool IsFocus()
        {
            return equip != null && focus;
        }

        /// <summary>
        /// 获取当前装备卡牌
        /// </summary>
        public Card GetCard()
        {
            return equip;
        }

        /// <summary>
        /// 鼠标指针进入UI时触发
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            focus = true;
        }

        /// <summary>
        /// 鼠标指针离开UI时触发
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            focus = false;
        }

        /// <summary>
        /// 当UI被禁用时，重置焦点状态
        /// </summary>
        void OnDisable()
        {
            focus = false;
        }
    }
}

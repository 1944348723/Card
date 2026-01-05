using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine.UI;

namespace TcgEngine.Client
{
    /// <summary>
    /// 棋盘上的卡组显示组件
    /// 显示玩家或对手的卡组数量和弃牌堆数量
    /// 鼠标悬停时显示详细信息
    /// </summary>
    public class BoardDeck : MonoBehaviour
    {
        public bool opponent;          // 是否为对手的卡组
        public UIPanel hover_panel;     // 悬停显示的面板
        public SpriteRenderer deck_render; // 卡组背面图像显示
        public Text deck_value;         // 卡组数量文本
        public Text discard_value;      // 弃牌堆数量文本

        private bool hover = false;     // 是否悬停状态
        
        void Start()
        {
            // 如果是移动端，默认显示悬停面板
            if (GameTool.IsMobile())
            {
                hover_panel?.SetVisible(true);
            }
        }

        void Update()
        {
            Refresh(); // 每帧刷新卡组显示
        }

        /// <summary>
        /// 刷新卡组和弃牌堆的显示
        /// </summary>
        private void Refresh()
        {
            if (!GameClient.Get().IsReady())
                return;

            // 获取玩家或对手
            Player player = opponent ? GameClient.Get().GetOpponentPlayer() : GameClient.Get().GetPlayer();
            if (player == null)
                return;

            // 获取卡牌背面数据并显示
            CardbackData cb = CardbackData.Get(player.cardback);
            if (deck_render != null && cb != null)
                deck_render.sprite = cb.deck;

            // 显示卡组数量
            if (deck_value != null)
                deck_value.text = player.cards_deck.Count.ToString();
            // 显示弃牌堆数量
            if (discard_value != null)
                discard_value.text = player.cards_discard.Count.ToString();
        }

        /// <summary>
        /// 显示玩家卡组的所有卡牌
        /// </summary>
        public void ShowDeckCards()
        {
            Player player = GameClient.Get().GetPlayer();
            CardSelector.Get().Show(player.cards_deck, "DECK");
        }

        /// <summary>
        /// 显示玩家或对手弃牌堆的所有卡牌
        /// </summary>
        public void ShowDiscardCards()
        {
            Player player = opponent ? GameClient.Get().GetOpponentPlayer() : GameClient.Get().GetPlayer();
            CardSelector.Get().Show(player.cards_discard, "DISCARD");
        }

        /// <summary>
        /// 控制悬停面板的显示
        /// </summary>
        private void ShowHover(bool hover)
        {
            if(!GameTool.IsMobile())
                hover_panel?.SetVisible(hover);
        }

        /// <summary>
        /// 鼠标进入卡组区域时触发
        /// </summary>
        private void OnMouseEnter()
        {
            hover = true;
            ShowHover(hover);
            Refresh();
        }

        /// <summary>
        /// 鼠标离开卡组区域时触发
        /// </summary>
        private void OnMouseExit()
        {
            hover = false;
            ShowHover(hover);
        }

        /// <summary>
        /// UI被禁用时触发
        /// </summary>
        private void OnDisable()
        {
            hover = false;
            ShowHover(hover);
        }

        /// <summary>
        /// 鼠标在卡组区域停留时触发
        /// 左键点击显示玩家卡组，右键点击显示弃牌堆
        /// 对手卡组不可查看
        /// </summary>
        private void OnMouseOver()
        {
            if (!opponent && Input.GetMouseButtonDown(0))
                ShowDeckCards(); // 左键显示玩家卡组
            else if(Input.GetMouseButtonDown(1))
                ShowDiscardCards(); // 右键显示弃牌堆
        }
    }
}

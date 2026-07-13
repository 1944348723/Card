using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Client;

namespace TcgEngine.UI
{
    /// <summary>
    /// MulliganSelector 选择重置手牌面板
    /// 玩家在游戏开始时可以选择重置部分手牌（Mulligan）
    /// </summary>
    public class MulliganSelector : SelectorPanel
    {
        public CardMulligan[] cards;     // 用于显示手牌的UI元素数组

        private static MulliganSelector instance;  // 单例实例

        protected override void Awake()
        {
            base.Awake();
            instance = this;              // 设置单例

            // 为每张手牌绑定点击事件
            foreach (CardMulligan card in cards)
            {
                card.onClick += OnClickCard;
            }
        }

        /// <summary>
        /// 刷新手牌显示
        /// </summary>
        private void RefreshMulligan()
        {
            Player player = GameClient.Get().GetPlayer();

            int index = 0;
            foreach (Card card in player.cards_hand)
            {
                string bonus_id = GameplayData.Get().second_bonus != null ? GameplayData.Get().second_bonus.id : "";
                if (index < cards.Length && card.card_id != bonus_id)
                {
                    CardMulligan card_ui = cards[index];
                    card_ui.SetCard(card);   // 设置UI显示手牌
                    index++;
                }
            }
        }

        /// <summary>
        /// 点击手牌时切换选择状态
        /// </summary>
        private void OnClickCard(CardMulligan card_ui)
        {
            card_ui.SetSelected(!card_ui.IsSelected());
        }

        /// <summary>
        /// 点击确认按钮时，提交选择的重置手牌
        /// </summary>
        public void OnClickOK()
        {
            List<string> selected_cards = new List<string>();

            foreach (CardMulligan acard in cards)
            {
                if (acard.IsSelected())
                    selected_cards.Add(acard.GetCard().uid);  // 收集已选手牌的UID
            }

            GameClient.Get().Mulligan(selected_cards.ToArray());  // 发送重置请求
            Hide();                                               // 隐藏面板
        }

        /// <summary>
        /// 显示面板时刷新手牌
        /// </summary>
        public override void Show(bool instant = false)
        {
            base.Show(instant);
            RefreshMulligan();
        }

        /// <summary>
        /// 判断是否需要显示Mulligan面板
        /// </summary>
        public override bool ShouldShow()
        {
            Game gdata = GameClient.Get().GetGameData();
            Player player = GameClient.Get().GetPlayer();
            return GameClient.Get().Rules.IsPlayerMulliganTurn(player); // 当前玩家是否处于Mulligan阶段
        }

        /// <summary>
        /// 获取单例实例
        /// </summary>
        public static MulliganSelector Get()
        {
            return instance;
        }
    }
}

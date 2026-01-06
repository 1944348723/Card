using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TcgEngine.UI
{
    /// <summary>
    /// 用于在界面中显示卡组信息的UI组件
    /// 只显示部分卡牌以及卡组总数量
    /// </summary>
    public class DeckDisplay : MonoBehaviour
    {
        public Text deck_title;      // 显示卡组名称的文本
        public Text card_count;      // 显示卡组总数的文本
        public CardUI[] ui_cards;    // 卡牌显示控件数组

        private string deck_id;      // 当前显示的卡组ID

        void Awake()
        {
            Clear(); // 初始化时清空显示
        }

        void Update()
        {

        }

        // 清空UI显示
        public void Clear()
        {
            if (deck_title != null)
                deck_title.text = "";
            if (card_count != null)
                card_count.text = "";
            foreach (CardUI card in ui_cards)
                card.Hide(); // 隐藏每张卡牌
        }

        // 根据卡组ID显示卡组
        public void SetDeck(string tid)
        {
            UserData user = Authenticator.Get().UserData;
            UserDeckData udeck = user.GetDeck(tid);
            DeckData ddeck = DeckData.Get(tid);
            if (udeck != null)
                SetDeck(udeck); // 显示用户自定义卡组
            else if (ddeck != null)
                SetDeck(ddeck);  // 显示系统卡组
            else
                Clear();         // 卡组不存在则清空显示
        }

        // 显示用户自定义卡组
        public void SetDeck(UserDeckData deck)
        {
            Clear();

            if (deck != null)
            {
                deck_id = deck.tid;

                if (deck_title != null)
                    deck_title.text = deck.title;

                if (card_count != null)
                {
                    card_count.text = deck.GetQuantity().ToString() + " / " + GameplayData.Get().deck_size.ToString();
                    // 若卡组数量不足则显示红色，否则白色
                    card_count.color = deck.GetQuantity() >= GameplayData.Get().deck_size ? Color.white : Color.red;
                }

                // 转换为可显示的卡牌列表
                List<CardDataQ> cards = new List<CardDataQ>();
                foreach (UserCardData ucard in deck.cards)
                {
                    CardDataQ card = new CardDataQ();
                    card.card = CardData.Get(ucard.tid);
                    card.variant = VariantData.Get(ucard.variant);
                    card.quantity = ucard.quantity;
                    if (card.card != null)
                        cards.Add(card);
                }

                ShowCards(cards); // 显示卡牌
            }

            gameObject.SetActive(deck != null);
        }

        // 显示系统卡组
        public void SetDeck(DeckData deck)
        {
            Clear();

            if (deck != null)
            {
                deck_id = deck.id;

                if (deck_title != null)
                    deck_title.text = deck.title;

                if (card_count != null)
                {
                    card_count.text = deck.GetQuantity().ToString() + " / " + GameplayData.Get().deck_size.ToString();
                    card_count.color = deck.GetQuantity() >= GameplayData.Get().deck_size ? Color.white : Color.red;
                }

                List<CardDataQ> dcards = new List<CardDataQ>();
                VariantData variant = VariantData.GetDefault();
                foreach (CardData icard in deck.cards)
                {
                    if (icard != null)
                    {
                        CardDataQ card = new CardDataQ();
                        card.card = icard;
                        card.variant = variant;
                        card.quantity = 1;
                        dcards.Add(card);
                    }
                }

                // 如果是拼图卡组，还要显示板上的卡牌
                if (deck is DeckPuzzleData)
                {
                    DeckPuzzleData pdeck = (DeckPuzzleData)deck;
                    foreach (DeckCardSlot slot in pdeck.board_cards)
                    {
                        if (slot.card != null)
                        {
                            CardDataQ card = new CardDataQ();
                            card.card = slot.card;
                            card.variant = variant;
                            card.quantity = 1;
                            dcards.Add(card);
                        }
                    }
                }

                ShowCards(dcards);
            }

            gameObject.SetActive(deck != null);
        }

        // 显示卡牌列表
        public void ShowCards(List<CardDataQ> cards)
        {
            // 按法力值从大到小排序
            cards.Sort((CardDataQ a, CardDataQ b) => { return b.card.mana.CompareTo(a.card.mana); });

            int index = 0;
            foreach (CardDataQ icard in cards)
            {
                for (int i = 0; i < icard.quantity; i++)
                {
                    if (index < ui_cards.Length)
                    {
                        CardUI card_ui = ui_cards[index];
                        card_ui.SetCard(icard.card, icard.variant); // 设置卡牌UI
                        index++;
                    }
                }
            }
        }

        // 隐藏整个面板
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        // 获取当前显示的卡组ID
        public string GetDeck()
        {
            return deck_id;
        }
    }
}

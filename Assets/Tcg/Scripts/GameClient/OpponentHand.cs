using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Client;

namespace TcgEngine.Client
{
    /// <summary>
    /// 对手手牌区域
    /// 类似 HandCardArea，但只用于显示对手手牌
    /// 简化版，不支持拖拽，只负责显示卡牌背面
    /// </summary>
    public class OpponentHand : MonoBehaviour
    {
        public GameObject card_prefab;      // 卡牌预制体，用于显示卡牌背面
        public RectTransform card_area;     // 对手手牌区域的 RectTransform
        public float card_spacing = 100f;   // 卡牌间距
        public float card_angle = 10f;      // 卡牌旋转角度
        public float card_offset_y = 10f;   // Y方向偏移，用于扇形排列

        private List<HandCardBack> cards = new List<HandCardBack>(); // 当前显示的对手手牌列表

        void Start()
        {
            // 当前无逻辑，保留空方法
        }

        void Update()
        {
            // 如果游戏客户端未准备好，则不更新
            if (!GameClient.Get().IsReady())
                return;

            Game gdata = GameClient.Get().GetGameData();
            Player player = gdata.GetPlayer(GameClient.Get().GetOpponentPlayerID()); // 获取对手玩家数据

            // 如果当前显示的卡牌数量少于对手手牌数量，生成新卡牌
            if (cards.Count < player.cards_hand.Count)
            {
                GameObject new_card = Instantiate(card_prefab, card_area);
                HandCardBack hand_card = new_card.GetComponent<HandCardBack>();
                CardbackData cbdata = CardbackData.Get(player.cardback); // 获取对手卡背数据
                hand_card.SetCardback(cbdata);
                RectTransform card_rect = new_card.GetComponent<RectTransform>();
                card_rect.anchoredPosition = new Vector2(0f, 100f); // 初始位置稍偏上
                cards.Add(hand_card);
            }

            // 如果当前显示的卡牌数量多于对手手牌数量，移除多余卡牌
            if (cards.Count > player.cards_hand.Count)
            {
                HandCardBack card = cards[cards.Count - 1];
                cards.RemoveAt(cards.Count - 1);
                Destroy(card.gameObject);
            }

            // 更新卡牌位置和旋转，实现扇形排列
            int nb_cards = Mathf.Min(cards.Count, player.cards_hand.Count);

            for (int i = 0; i < nb_cards; i++)
            {
                HandCardBack card = cards[i];
                RectTransform crect = card.GetRect();
                float half = nb_cards / 2f;
                Vector3 tpos = new Vector3(
                    (i - half) * card_spacing,              // X轴位置
                    (i - half) * (i - half) * card_offset_y // Y轴偏移，实现扇形效果
                );
                float tangle = (i - half) * card_angle;    // 卡牌旋转角度
                crect.anchoredPosition = Vector3.Lerp(crect.anchoredPosition, tpos, 4f * Time.deltaTime);
                card.transform.localRotation = Quaternion.Slerp(card.transform.localRotation, Quaternion.Euler(0f, 0f, tangle), 4f * Time.deltaTime);
            }

        }
    }
}

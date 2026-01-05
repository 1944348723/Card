using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine.Client;
using TcgEngine.UI;

namespace TcgEngine.Client
{
    /// <summary>
    /// 手牌区域
    /// 管理手牌的生成、销毁和排列
    /// 根据服务器刷新数据更新手牌显示
    /// </summary>
    public class HandCardArea : MonoBehaviour
    {
        public GameObject card_prefab;       // 手牌预制体
        public RectTransform card_area;      // 手牌父节点
        public float card_spacing = 100f;    // 卡牌间距
        public float card_angle = 10f;       // 卡牌旋转角度
        public float card_offset_y = 10f;    // 卡牌纵向偏移量

        private List<HandCard> cards = new List<HandCard>(); // 当前手牌列表

        private bool is_dragging;            // 是否正在拖拽手牌

        private string last_destroyed;       // 最近销毁的卡牌UID
        private float last_destroyed_timer = 0f; // 最近销毁计时器

        private static HandCardArea _instance; // 单例引用

        void Awake()
        {
            _instance = this; // 初始化单例
        }

        void Update()
        {
            if (!GameClient.Get().IsReady())
                return; // 游戏未准备好时跳过

            int player_id = GameClient.Get().GetPlayerID();
            Game data = GameClient.Get().GetGameData();
            Player player = data.GetPlayer(player_id);

            last_destroyed_timer += Time.deltaTime;

            // 添加新卡牌
            foreach (Card card in player.cards_hand)
            {
                if (!HasCard(card.uid))
                    SpawnNewCard(card);
            }

            // 移除已经销毁的卡牌
            for (int i = cards.Count - 1; i >= 0; i--)
            {
                HandCard card = cards[i];
                if (card == null || player.GetHandCard(card.GetCard().uid) == null)
                {
                    cards.RemoveAt(i);
                    if(card != null)
                        card.Kill(); // 销毁卡牌对象
                }
            }

            // 设置卡牌位置和旋转
            int index = 0;
            float count_half = cards.Count / 2f;
            foreach (HandCard card in cards)
            {
                card.deck_position = new Vector2(
                    (index - count_half) * card_spacing, 
                    (index - count_half) * (index - count_half) * -card_offset_y
                );
                card.deck_angle = (index - count_half) * -card_angle;
                index++;
            }

            // 检测是否有手牌正在拖拽
            HandCard drag_card = HandCard.GetDrag();
            is_dragging = drag_card != null;
        }

        /// <summary>
        /// 生成新卡牌对象
        /// </summary>
        public void SpawnNewCard(Card card)
        {
            GameObject card_obj = Instantiate(card_prefab, card_area.transform);
            card_obj.GetComponent<HandCard>().SetCard(card);
            card_obj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -100f); // 初始位置在底部
            cards.Add(card_obj.GetComponent<HandCard>());
        }

        /// <summary>
        /// 延迟刷新，用于手牌销毁后的短时间保护
        /// </summary>
        public void DelayRefresh(Card card)
        {
            last_destroyed_timer = 0f;
            last_destroyed = card.uid;
        }

        /// <summary>
        /// 根据X轴位置对卡牌进行排序（用于手牌层级管理）
        /// </summary>
        public void SortCards()
        {
            cards.Sort(SortFunc);

            int i = 0;
            foreach (HandCard acard in cards)
            {
                acard.transform.SetSiblingIndex(i);
                i++;
            }
        }

        /// <summary>
        /// 排序比较函数，按X轴位置排序
        /// </summary>
        private int SortFunc(HandCard a, HandCard b)
        {
            return a.transform.position.x.CompareTo(b.transform.position.x);
        }

        /// <summary>
        /// 检查当前手牌区域是否包含指定UID的卡牌
        /// </summary>
        public bool HasCard(string card_uid)
        {
            HandCard card = HandCard.Get(card_uid);
            bool just_destroyed = card_uid == last_destroyed && last_destroyed_timer < 0.7f; // 保护期内已销毁的卡牌
            return card != null || just_destroyed;
        }

        /// <summary>
        /// 当前是否正在拖拽卡牌
        /// </summary>
        public bool IsDragging()
        {
            return is_dragging;
        }

        /// <summary>
        /// 获取HandCardArea单例
        /// </summary>
        public static HandCardArea Get()
        {
            return _instance;
        }
    }
}

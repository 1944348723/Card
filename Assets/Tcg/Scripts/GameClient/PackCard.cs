using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine.UI;
using TcgEngine.FX;

namespace TcgEngine.Client
{
    /// <summary>
    /// 打开的卡包中显示的卡牌（视觉效果）
    /// 玩家可以通过点击卡牌翻面
    /// </summary>
    public class PackCard : MonoBehaviour
    {
        public float move_speed = 5f;       // 卡牌移动速度
        public float flip_speed = 10f;      // 卡牌翻转速度

        public SpriteRenderer cardback;     // 卡牌背面显示的 SpriteRenderer
        public CardUI card_ui;              // 卡牌正面 UI 显示组件

        public GameObject new_card;         // 标记新卡的 UI 对象

        [Header("FX")]
        public GameObject card_flip_fx;      // 普通翻牌特效
        public GameObject card_rare_flip_fx; // 稀有卡翻牌特效
        public AudioClip card_flip_audio;    // 普通翻牌音效
        public AudioClip card_rare_flip_audio; // 稀有卡翻牌音效

        private CardData icard;             // 当前显示的卡牌数据
        private VariantData variant;        // 当前卡牌的变体数据

        private Vector3 target;             // 卡牌目标位置
        private Quaternion rtarget;         // 卡牌目标旋转
        private bool revealed = false;      // 是否已翻开
        private bool removed = false;       // 是否标记为移除
        private bool is_new = false;        // 是否为新卡
        private float timer = 0f;           // 计时器，用于翻牌或销毁延迟

        private static List<PackCard> card_list = new List<PackCard>(); // 所有 PackCard 实例列表

        void Awake()
        {
            card_list.Add(this);             // 添加到静态列表
        }

        private void OnDestroy()
        {
            card_list.Remove(this);          // 从静态列表移除
        }

        void Update()
        {
            // 平滑移动到目标位置
            transform.position = Vector3.MoveTowards(transform.position, target, move_speed * Time.deltaTime);

            // 翻牌逻辑
            if (revealed)
            {
                timer += Time.deltaTime;
                transform.rotation = Quaternion.Slerp(transform.rotation, rtarget, flip_speed * Time.deltaTime);
            }

            // 移除延迟销毁
            if (removed && timer > 4f)
                Destroy(gameObject);
        }

        /// <summary>
        /// 设置卡牌显示
        /// </summary>
        public void SetCard(PackData pack, CardData card, VariantData variant)
        {
            this.icard = card;
            this.variant = variant;

            // 设置卡背
            if (cardback != null)
                cardback.sprite = pack.cardback_img;

            card_ui.SetCard(card, variant);  // 设置卡牌正面显示
            new_card?.SetActive(false);      // 默认隐藏新卡标记

            // 判断是否为新卡
            UserData udata = Authenticator.Get().GetUserData();
            is_new = !udata.HasCard(icard.id, variant.id);
        }

        /// <summary>
        /// 设置卡牌目标位置和翻转方向
        /// </summary>
        public void SetTarget(Vector3 pos)
        {
            target = pos;
            rtarget = Quaternion.Euler(0f, 180f, 0f); // 初始背面朝上
            transform.rotation = rtarget;
        }

        /// <summary>
        /// 翻开卡牌
        /// </summary>
        public void Reveal()
        {
            if (revealed)
                return;

            revealed = true;
            rtarget = Quaternion.Euler(0f, 0f, 0f); // 正面朝上
            new_card?.SetActive(is_new);           // 显示新卡标记

            // 播放特效与音效，根据稀有度区分
            if (icard != null && icard.rarity.rank >= 3)
            {
                FXTool.DoFX(card_rare_flip_fx, transform.position);
                AudioTool.Get().PlaySFX("pack_open", card_rare_flip_audio);
            }
            else
            {
                FXTool.DoFX(card_flip_fx, transform.position);
                AudioTool.Get().PlaySFX("pack_open", card_flip_audio);
            }
        }

        /// <summary>
        /// 标记卡牌移除（用于动画或延迟销毁）
        /// </summary>
        public void Remove()
        {
            if (removed)
                return;

            removed = true;
            timer = 0f;
            target = Vector3.up * 10f; // 向上飞出一点位置
        }

        /// <summary>
        /// 鼠标点击事件，翻牌
        /// </summary>
        public void OnMouseDown()
        {
            if (!GameUI.IsOverUILayer("UI"))
            {
                Reveal();
            }
        }

        /// <summary>
        /// 判断卡牌是否已经翻开
        /// </summary>
        public bool IsRevealed()
        {
            return revealed && timer > 0.5f;
        }

        /// <summary>
        /// 获取当前场景所有 PackCard
        /// </summary>
        public static List<PackCard> GetAll()
        {
            return card_list;
        }
    }
}

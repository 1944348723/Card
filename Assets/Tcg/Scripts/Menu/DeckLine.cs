using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TcgEngine.UI
{
    /// <summary>
    /// 卡组编辑面板中的一行（可以表示一张卡牌或一个卡组标题）
    /// </summary>
    public class DeckLine : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        // UI组件
        public Image image;          // 卡牌或卡组的主图片
        public Image frame;          // 卡牌边框
        public Text title;           // 卡牌或卡组标题
        public Text value;           // 卡牌数量或卡组数量显示
        public IconValue cost;       // 卡牌消耗的法力值图标
        public UIPanel delete_btn;   // 删除按钮面板

        public AudioClip click_audio;    // 点击音效
        public Material disabled_mat;    // 禁用材质
        public Material default_mat;     // 默认材质

        // 点击事件回调
        public UnityAction<DeckLine> onClick;         // 左键点击事件
        public UnityAction<DeckLine> onClickRight;    // 右键点击事件
        public UnityAction<DeckLine> onClickDelete;   // 删除按钮点击事件

        // 内部数据
        private CardData card;         // 当前行的卡牌数据
        private VariantData variant;   // 卡牌变体数据
        private DeckData deck;         // 当前行的卡组数据
        private UserDeckData udeck;    // 用户卡组数据
        private bool hidden = false;   // 是否隐藏
        private bool hover = false;    // 鼠标是否悬停

        void Awake()
        {
            // 初始化逻辑可留空
        }

        void Update()
        {
            // 根据鼠标悬停或移动端显示删除按钮
            if (delete_btn != null)
            {
                bool visi = hover || GameTool.IsMobile();
                delete_btn.SetVisible(visi && !hidden && udeck != null);
            }
        }

        // 设置卡牌行显示
        public void SetLine(CardData card, VariantData variant, int quantity, bool invalid = false)
        {
            this.card = card;
            this.variant = variant;
            this.deck = null;
            this.udeck = null;
            hidden = false;

            // 设置标题与颜色
            if (title != null)
                title.text = card.title;
            if (title != null)
                title.color = variant.color;

            // 显示数量
            if (value != null)
                value.text = quantity.ToString();
            if (value != null)
                value.enabled = quantity > 1;

            // 显示法力消耗
            if (cost != null)
                cost.value = card.mana;

            // 高亮或禁用显示
            if (this.value != null)
                this.value.color = invalid ? Color.red : Color.white;
            if (invalid)
                title.color = Color.gray;

            // 设置图片与边框
            if (image != null)
            {
                image.sprite = card.GetFullArt(variant);
                image.enabled = true;
                image.material = invalid ? disabled_mat : default_mat;
            }

            if (frame != null)
            {
                frame.sprite = variant.frame;
                frame.enabled = true;
                frame.material = invalid ? disabled_mat : default_mat;
            }

            gameObject.SetActive(true);
        }

        // 设置卡组数据行显示（DeckData）
        public void SetLine(DeckData deck)
        {
            this.card = null;
            this.deck = deck;
            this.udeck = null;
            hidden = false;

            if (this.title != null)
                this.title.text = deck.title;
            if (this.title != null)
                this.title.color = Color.white;
            if (this.value != null)
                this.value.text = deck.GetQuantity().ToString();
            if (this.value != null)
                this.value.enabled = deck.GetQuantity() > 0;

            gameObject.SetActive(true);
        }

        // 设置用户卡组数据行显示
        public void SetLine(UserData udata, UserDeckData deck)
        {
            this.card = null;
            this.deck = null;
            this.udeck = deck;
            hidden = false;

            if (this.title != null)
                this.title.text = deck.title;
            if (this.title != null)
                this.title.color = Color.white;
            if (this.value != null)
                this.value.text = deck.GetQuantity().ToString() + "/" + GameplayData.Get().deck_size;
            if (this.value != null)
                this.value.enabled = deck.GetQuantity() > 0;
            if (this.value != null)
                this.value.color = udata.IsDeckValid(deck) ? Color.white : Color.red;

            gameObject.SetActive(true);
        }

        // 设置标题行（自定义字符串）
        public void SetLine(string title)
        {
            this.card = null;
            this.deck = null;
            this.udeck = null;
            hidden = false;

            if (this.title != null)
                this.title.text = title;
            if (this.title != null)
                this.title.color = Color.white;

            if (this.value != null)
                this.value.enabled = false;

            gameObject.SetActive(true);
        }

        // 隐藏该行
        public void Hide()
        {
            this.card = null;
            this.deck = null;
            this.udeck = null;
            hidden = true;
            hover = false;

            if (title != null)
                title.text = "";
            if (this.title != null)
                this.title.color = Color.white;
            if (value != null)
                value.text = "";
            if (value != null)
                value.enabled = true;
            if (cost != null)
                cost.value = 0;
            if (image != null)
                image.enabled = false;
            if (frame != null)
                frame.enabled = false;
            if (delete_btn != null)
                delete_btn.SetVisible(false);

            gameObject.SetActive(false);
        }

        // ---- 获取当前行数据 ----
        public CardData GetCard() { return card; }
        public VariantData GetVariant() { return variant; }
        public DeckData GetDeck() { return deck; }
        public UserDeckData GetUserDeck() { return udeck; }
        public bool IsHidden() { return hidden; }

        // ---- 鼠标事件接口实现 ----
        public void OnPointerClick(PointerEventData eventData)
        {
            if (hidden) return;

            // 左键点击
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                onClick?.Invoke(this);
                AudioTool.Get().PlaySFX("ui", click_audio);
            }

            // 右键点击
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                onClickRight?.Invoke(this);
                AudioTool.Get().PlaySFX("ui", click_audio);
            }
        }

        // 点击删除按钮
        public void OnClickDelete()
        {
            onClickDelete?.Invoke(this);
            AudioTool.Get().PlaySFX("ui", click_audio);
        }

        // 鼠标悬停进入
        public void OnPointerEnter(PointerEventData eventData)
        {
            hover = true;
        }

        // 鼠标悬停离开
        public void OnPointerExit(PointerEventData eventData)
        {
            hover = false;
        }
    }
}

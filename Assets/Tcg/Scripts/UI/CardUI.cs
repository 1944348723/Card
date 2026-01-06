using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using TcgEngine.Client;

namespace TcgEngine.UI
{
    /// <summary>
    /// 显示卡牌的所有属性的 UI 脚本
    /// 用于其他显示卡牌的脚本，如 BoardCard、HandCard、CollectionCard 等
    /// </summary>
    public class CardUI : MonoBehaviour, IPointerClickHandler
    {
        public Image card_image;           // 卡牌主图
        public Image frame_image;          // 卡牌边框
        public Image team_icon;            // 队伍图标
        public Image rarity_icon;          // 稀有度图标
        public Image attack_icon;          // 攻击图标
        public Image hp_icon;              // 生命图标
        public Image cost_icon;            // 消耗图标
        public Text attack;                // 攻击数值
        public Text hp;                    // 生命数值
        public Text cost;                  // 法力消耗数值

        public Text card_title;            // 卡牌名称
        public Text card_text;             // 卡牌描述文本

        public TraitUI[] stats;            // 卡牌特性显示列表

        public UnityAction<CardUI> onClick;       // 左键点击事件
        public UnityAction<CardUI> onClickRight;  // 右键点击事件

        private CardData card;             // 卡牌数据
        private VariantData variant;       // 卡牌变体数据

        void Awake()
        {

        }

        // 设置卡牌对象
        public void SetCard(Card card)
        {
            if (card == null)
                return;

            SetCard(card.CardData, card.VariantData);

            if (cost != null)
                cost.text = card.GetMana().ToString();
            if (cost != null && card.CardData.IsDynamicManaCost())
                cost.text = "X";
            if (attack != null)
                attack.text = card.GetAttack().ToString();
            if (hp != null)
                hp.text = card.GetHP().ToString();

            foreach (TraitUI stat in stats)
                stat.SetCard(card);
        }

        // 设置卡牌数据和变体数据
        public void SetCard(CardData card, VariantData variant)
        {
            if (card == null)
                return;

            this.card = card;
            this.variant = variant;

            if(card_image != null)
                card_image.sprite = card.GetFullArt(variant);   // 设置卡牌图像
            if (frame_image != null)
                frame_image.sprite = variant.frame;             // 设置卡牌边框
            if (card_title != null)
                card_title.text = card.GetTitle().ToUpper();    // 设置卡牌标题
            if (card_text != null)
                card_text.text = card.GetText();                // 设置卡牌描述文本

            if (attack_icon != null)
                attack_icon.enabled = card.IsCharacter();       // 是否显示攻击图标
            if (attack != null)
                attack.enabled = card.IsCharacter();           // 是否显示攻击值
            if (hp_icon != null)
                hp_icon.enabled = card.IsBoardCard() || card.IsEquipment(); // 是否显示生命图标
            if (hp != null)
                hp.enabled = card.IsBoardCard() || card.IsEquipment();      // 是否显示生命值
            if (cost_icon != null)
                cost_icon.enabled = card.type != CardType.Hero; // 英雄不显示消耗
            if (cost != null)
                cost.enabled = card.type != CardType.Hero;      // 英雄不显示消耗

            if (cost != null)
                cost.text = card.mana.ToString();              // 设置法力值
            if (cost != null && card.IsDynamicManaCost())
                cost.text = "X";                               // 动态消耗显示 X
            if (attack != null)
                attack.text = card.attack.ToString();         // 设置攻击数值
            if (hp != null)
                hp.text = card.hp.ToString();                 // 设置生命数值

            if (team_icon != null)
            {
                team_icon.sprite = card.team.icon;           // 设置队伍图标
                team_icon.enabled = team_icon.sprite != null;
            }

            if (rarity_icon != null)
            {
                rarity_icon.sprite = card.rarity.icon;       // 设置稀有度图标
                rarity_icon.enabled = rarity_icon.sprite != null && card.type != CardType.Hero;
            }

            foreach (TraitUI stat in stats)
                stat.SetCard(card);                            // 更新卡牌特性显示

            if (!gameObject.activeSelf)
                gameObject.SetActive(true);                  // 激活 UI 对象
        }

        // 设置生命值显示
        public void SetHP(int hp_value)
        {
            if (hp != null)
                hp.text = hp_value.ToString();
        }

        // 设置材质
        public void SetMaterial(Material mat)
        {
            if (card_image != null)
                card_image.material = mat;
            if (frame_image != null)
                frame_image.material = mat;
            if (team_icon != null)
                team_icon.material = mat;
            if (rarity_icon != null)
                rarity_icon.material = mat;
            if (attack_icon != null)
                attack_icon.material = mat;
            if (hp_icon != null)
                hp_icon.material = mat;
            if (cost_icon != null)
                cost_icon.material = mat;
        }

        // 设置透明度
        public void SetOpacity(float opacity)
        {
            if (card_image != null)
                card_image.color = new Color(card_image.color.r, card_image.color.g, card_image.color.b, opacity);
            if (frame_image != null)
                frame_image.color = new Color(frame_image.color.r, frame_image.color.g, frame_image.color.b, opacity);
            if (team_icon != null)
                team_icon.color = new Color(team_icon.color.r, team_icon.color.g, team_icon.color.b, opacity);
            if (rarity_icon != null)
                rarity_icon.color = new Color(rarity_icon.color.r, rarity_icon.color.g, rarity_icon.color.b, opacity);
            if (attack_icon != null)
                attack_icon.color = new Color(attack_icon.color.r, attack_icon.color.g, attack_icon.color.b, opacity);
            if (hp_icon != null)
                hp_icon.color = new Color(hp_icon.color.r, hp_icon.color.g, hp_icon.color.b, opacity);
            if (cost_icon != null)
                cost_icon.color = new Color(cost_icon.color.r, cost_icon.color.g, cost_icon.color.b, opacity);
            if (attack != null)
                attack.color = new Color(attack.color.r, attack.color.g, attack.color.b, opacity);
            if (hp != null)
                hp.color = new Color(hp.color.r, hp.color.g, hp.color.b, opacity);
            if (cost != null)
                cost.color = new Color(cost.color.r, cost.color.g, cost.color.b, opacity);
            if (card_title != null)
                card_title.color = new Color(card_title.color.r, card_title.color.g, card_title.color.b, opacity);
            if (card_text != null)
                card_text.color = new Color(card_text.color.r, card_text.color.g, card_text.color.b, opacity);
        }

        // 隐藏卡牌 UI
        public void Hide()
        {
            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }

        // 处理鼠标点击事件
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                if (onClick != null)
                    onClick.Invoke(this);
            }

            if (eventData.button == PointerEventData.InputButton.Right)
            {
                if (onClickRight != null)
                    onClickRight.Invoke(this);
            }
        }

        // 获取卡牌数据
        public CardData GetCard()
        {
            return card;
        }

        // 获取卡牌变体数据
        public VariantData GetVariant()
        {
            return variant;
        }
    }
}

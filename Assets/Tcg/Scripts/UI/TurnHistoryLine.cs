using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine.Client;
using UnityEngine.EventSystems;

namespace TcgEngine.UI
{
    /// <summary>
    /// 回合历史栏中的一条记录格子（TurnHistoryLine）
    /// 每个格子显示玩家在回合中执行的一个动作，并支持鼠标悬停显示提示信息
    /// </summary>
    public class TurnHistoryLine : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public HoverTargetUI hover;   // 鼠标悬停时显示的提示文本
        public Image card_img;        // 显示操作对应的卡牌图片

        private Card card;            // 当前格子对应的卡牌
        private float timer = 0f;     // 计时器，用于延迟隐藏
        private bool is_hover = false;// 是否被鼠标悬停

        private static List<TurnHistoryLine> line_list = new List<TurnHistoryLine>(); // 所有历史格子的列表

        void Awake()
        {
            line_list.Add(this);
        }

        void OnDestroy()
        {
            // 销毁时从列表中移除
            line_list.Remove(this);
        }

        void Start()
        {
            // 初始隐藏
            gameObject.SetActive(false);
        }

        private void Update()
        {
            // 更新计时器
            timer += Time.deltaTime;
        }

        /// <summary>
        /// 设置历史格子内容
        /// 根据ActionHistory的类型，显示不同的文字和卡牌
        /// </summary>
        public void SetLine(ActionHistory history)
        {
            Game gdata = GameClient.Get().GetGameData();
            Card acard = gdata.GetCard(history.card_uid);        // 获取执行操作的卡牌
            Card target = gdata.GetCard(history.target_uid);     // 获取目标卡牌
            Player ptarget = gdata.GetPlayer(history.target_id);// 获取目标玩家
            CardData icard = CardData.Get(history.card_id);      // 获取卡牌数据
            CardData itarget = CardData.Get(target?.card_id);   // 获取目标卡牌数据
            VariantData variant = acard.VariantData;             // 获取卡牌变体数据
            AbilityData iability = AbilityData.Get(history.ability_id); // 获取技能数据
            card = acard;

            if (icard == null)
                return;

            // 根据不同动作类型设置显示文本
            if (history.type == GameAction.PlayCard)
            {
                string text = icard.title + " was played";
                SetLine(icard, variant, text);
            }

            if (history.type == GameAction.Move)
            {
                string text = icard.title + " moved";
                SetLine(icard, variant, text);
            }

            if (history.type == GameAction.Attack && itarget != null)
            {
                string text = icard.title + " attacked " + itarget.title;
                SetLine(icard, variant, text);
            }

            if (history.type == GameAction.AttackPlayer && ptarget != null)
            {
                string text = icard.title + " attacked " + ptarget.username;
                SetLine(icard, variant, text);
            }

            if (history.type == GameAction.CastAbility && iability != null)
            {
                if (iability.target == AbilityTarget.SelectTarget && itarget != null)
                {
                    string text = icard.title + " casted " + iability.GetTitle() + " on " + itarget.title;
                    SetLine(icard, variant, text);
                }
                else
                {
                    string text = icard.title + " casted " + iability.GetTitle();
                    SetLine(icard, variant, text);
                }
            }

            if (history.type == GameAction.SecretTriggered)
            {
                string text = icard.title + " was triggered";
                SetLine(icard, variant, text);
            }
        }

        /// <summary>
        /// 设置格子显示的卡牌和提示文字
        /// </summary>
        public void SetLine(CardData icard, VariantData variant, string text)
        {
            card_img.sprite = icard.GetFullArt(variant); // 设置卡牌图片
            hover.text = text;                            // 设置悬停提示文本
            gameObject.SetActive(true);                   // 显示格子
            timer = 0f;                                   // 重置计时器
        }

        /// <summary>
        /// 隐藏历史格子
        /// </summary>
        public void Hide()
        {
            card = null;
            if (timer > 0.05f)
                gameObject.SetActive(false);
        }

        /// <summary>
        /// 鼠标悬停事件
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            timer = 0f;
            is_hover = true;
        }

        /// <summary>
        /// 鼠标离开事件
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            timer = 0f;
            is_hover = false;
        }

        void OnDisable()
        {
            is_hover = false;
        }

        /// <summary>
        /// 获取当前被鼠标悬停的卡牌
        /// </summary>
        public static Card GetHoverCard()
        {
            foreach (TurnHistoryLine line in line_list)
            {
                if (line.card != null && line.is_hover)
                    return line.card;
            }
            return null;
        }
    }
}

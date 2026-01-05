using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Client;
using UnityEngine.Events;
using TcgEngine.UI;

namespace TcgEngine.Client
{
    /// <summary>
    /// 玩家操作控制脚本
    /// 负责点击卡牌、攻击、激活技能等操作
    /// 保存当前选中的卡牌，并在鼠标释放时将动作发送给 GameClient
    /// </summary>
    public class PlayerControls : MonoBehaviour
    {
        // 当前选中的卡牌（BoardCard类型）
        private BoardCard selected_card = null;

        // 静态实例，方便全局访问
        private static PlayerControls instance;

        void Awake()
        {
            // 保存静态实例
            instance = this;
        }

        void Update()
        {
            // 如果游戏客户端未准备好，则不处理输入
            if (!GameClient.Get().IsReady())
                return;

            // 右键点击取消所有选择
            if (Input.GetMouseButtonDown(1))
                UnselectAll();

            // 如果有选中的卡牌
            if (selected_card != null)
            {
                // 鼠标左键释放时执行操作
                if (Input.GetMouseButtonUp(0))
                {
                    ReleaseClick(); // 根据鼠标位置执行操作（攻击/技能/移动）
                    UnselectAll();  // 释放后取消选中
                }
            }
        }

        /// <summary>
        /// 选中卡牌
        /// </summary>
        /// <param name="bcard">被选中的BoardCard</param>
        public void SelectCard(BoardCard bcard)
        {
            Game gdata = GameClient.Get().GetGameData();
            Player player = GameClient.Get().GetPlayer();
            Card card = bcard.GetFocusCard();

            // 如果当前是选择目标阶段，并且玩家正在操作
            if (gdata.IsPlayerSelectorTurn(player) && gdata.selector == SelectorType.SelectTarget)
            {
                if (!Tutorial.Get().CanDo(TutoEndTrigger.SelectTarget, card))
                    return;

                // 目标选择器，选择这张卡
                GameClient.Get().SelectCard(card);
            }
            // 如果是行动阶段，并且卡牌属于玩家
            else if (gdata.IsPlayerActionTurn(player) && card.player_id == player.player_id)
            {
                // 开始拖动卡牌
                selected_card = bcard;
            }
        }

        /// <summary>
        /// 右键选择卡牌（目前右键没有功能）
        /// </summary>
        /// <param name="card"></param>
        public void SelectCardRight(BoardCard card)
        {
            if (!Input.GetMouseButton(0))
            {
                // 右键暂无操作
            }
        }

        /// <summary>
        /// 鼠标释放时执行的操作
        /// 根据鼠标位置决定攻击、施放技能或移动
        /// </summary>
        private void ReleaseClick()
        {
            bool yourturn = GameClient.Get().IsYourTurn();

            if (yourturn && selected_card != null)
            {
                Card card = selected_card.GetCard();
                Vector3 wpos = GameBoard.Get().RaycastMouseBoard(); // 获取鼠标在游戏板上的世界位置
                BSlot tslot = BSlot.GetNearest(wpos);               // 获取最近的卡槽
                Card target = tslot?.GetSlotCard(wpos);            // 获取目标卡牌
                AbilityButton ability = AbilityButton.GetFocus(wpos, 1f); // 检查鼠标是否悬停在技能按钮上

                // 如果鼠标在技能按钮上并且可操作
                if (ability != null && ability.IsInteractable())
                {
                    if (!Tutorial.Get().CanDo(TutoEndTrigger.CastAbility, card))
                        return;

                    // 施放技能
                    GameClient.Get().CastAbility(card, ability.GetAbility());
                }
                // 如果目标是玩家攻击槽
                else if (tslot is BoardSlotPlayer)
                {
                    if (!Tutorial.Get().CanDo(TutoEndTrigger.AttackPlayer, card))
                        return;

                    if (card.exhausted) // 如果卡牌疲劳
                        WarningText.ShowExhausted();
                    else
                        GameClient.Get().AttackPlayer(card, tslot.GetPlayer());
                }
                // 如果目标是其他卡牌（敌方卡牌）
                else if (target != null && target.uid != card.uid && target.player_id != card.player_id)
                {
                    if (!Tutorial.Get().CanDo(TutoEndTrigger.Attack, card) && !Tutorial.Get().CanDo(TutoEndTrigger.Attack, target))
                        return;

                    if (card.exhausted)
                        WarningText.ShowExhausted();
                    else
                        GameClient.Get().AttackTarget(card, target);
                }
                // 如果目标是普通卡槽
                else if (tslot != null && tslot is BoardSlot)
                {
                    if (!Tutorial.Get().CanDo(TutoEndTrigger.Move, tslot.GetSlot()))
                        return;

                    // 移动卡牌到指定槽
                    GameClient.Get().Move(card, tslot.GetSlot());
                }
            }
        }

        /// <summary>
        /// 取消当前选中的卡牌
        /// </summary>
        public void UnselectAll()
        {
            selected_card = null;
        }

        /// <summary>
        /// 获取当前选中的卡牌
        /// </summary>
        /// <returns></returns>
        public BoardCard GetSelected()
        {
            return selected_card;
        }

        /// <summary>
        /// 获取全局实例
        /// </summary>
        /// <returns></returns>
        public static PlayerControls Get()
        {
            return instance;
        }
    }
}

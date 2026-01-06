using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine.Client;

namespace TcgEngine.UI
{
    /// <summary>
    /// 当使用目标为 ChoiceSelector 的技能时显示的消耗选择面板
    /// 允许玩家选择不同的数值（例如法力消耗）
    /// </summary>
    public class CostSelector : SelectorPanel
    {
        public NumberSelector selector; // 数值选择器组件

        private Card caster; // 技能施放者卡牌

        private static CostSelector instance; // 单例实例

        protected override void Awake()
        {
            base.Awake();
            instance = this; // 初始化单例
        }

        protected override void Start()
        {
            base.Start();
        }

        protected override void Update()
        {
            base.Update();

            Game game = GameClient.Get().GetGameData();
            if (game != null && game.selector == SelectorType.None)
                Hide(); // 如果当前没有选择器，则隐藏面板
        }

        // 刷新面板显示内容
        public void RefreshPanel()
        {
            if (caster == null)
                return;

            Game game = GameClient.Get().GetGameData();
            Player player = game.GetPlayer(caster.player_id);
            selector.SetMax(player.mana); // 设置最大可选数值为玩家当前法力
            selector.SetValue(0);         // 默认选中 0
        }

        // 点击确认按钮
        public void OnClickOK()
        {
            Game data = GameClient.Get().GetGameData();
            if (data.selector == SelectorType.SelectorCost)
            {
                GameClient.Get().SelectCost(selector.value); // 发送选择的数值给服务器
            }

            Hide(); // 隐藏面板
        }

        // 点击取消按钮
        public void OnClickCancel()
        {
            GameClient.Get().CancelSelection(); // 取消选择
            Hide();                              // 隐藏面板
        }

        // 显示面板并指定施法者卡牌
        public override void Show(AbilityData iability, Card caster)
        {
            this.caster = caster;
            Show();
        }

        public override void Show(bool instant = false)
        {
            base.Show(instant);
            RefreshPanel(); // 显示时刷新面板
        }

        // 判断是否应该显示面板
        public override bool ShouldShow()
        {
            Game data = GameClient.Get().GetGameData();
            int player_id = GameClient.Get().GetPlayerID();
            return data.selector == SelectorType.SelectorCost && data.selector_player_id == player_id;
        }

        // 获取单例实例
        public static CostSelector Get()
        {
            return instance;
        }
    }
}

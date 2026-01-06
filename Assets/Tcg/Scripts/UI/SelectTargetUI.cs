using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine.Client;
using TcgEngine;

namespace TcgEngine.UI
{
    /// <summary>
    /// 当使用技能选择目标（SelectTarget）时显示的UI面板
    /// 用于展示技能的目标选择界面
    /// </summary>
    public class SelectTargetUI : SelectorPanel
    {
        public Text title; // 技能标题文本
        public Text desc;  // 技能描述文本（暂未使用）

        private static SelectTargetUI _instance; // 单例实例

        protected override void Awake()
        {
            _instance = this; // 设置单例
            base.Awake();
        }

        protected override void Update()
        {
            base.Update();

            // 获取游戏数据
            Game game = GameClient.Get().GetGameData();
            // 如果当前没有选择器类型，则隐藏面板
            if (game != null && game.selector == SelectorType.None)
                Hide();
        }

        /// <summary>
        /// 显示选择目标面板
        /// </summary>
        /// <param name="ability">技能数据</param>
        /// <param name="caster">施法卡牌</param>
        public override void Show(AbilityData ability, Card caster)
        {
            this.title.text = ability.title;
            //this.desc.text = ability.desc; // 暂未启用技能描述
            Show();
        }

        /// <summary>
        /// 点击关闭按钮时调用，取消选择
        /// </summary>
        public void OnClickClose()
        {
            GameClient.Get().CancelSelection();
        }

        /// <summary>
        /// 判断该面板是否应显示
        /// </summary>
        /// <returns>是否显示</returns>
        public override bool ShouldShow()
        {
            Game data = GameClient.Get().GetGameData();
            int player_id = GameClient.Get().GetPlayerID();
            return data.selector == SelectorType.SelectTarget && data.selector_player_id == player_id;
        }

        /// <summary>
        /// 获取单例实例
        /// </summary>
        public static SelectTargetUI Get()
        {
            return _instance;
        }
    }
}
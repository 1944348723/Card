using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine.Client;

namespace TcgEngine.UI
{
    /// <summary>
    /// 选择面板
    /// 当使用目标为 ChoiceSelector 的技能时显示
    /// 允许玩家在多个技能选项中进行选择
    /// </summary>
    public class ChoiceSelector : SelectorPanel
    {
        public ChoiceSelectorChoice[] choices;     // 所有可选择的选项

        private Card caster;                        // 施法者
        private AbilityData ability;                // 当前技能

        private static ChoiceSelector instance;    // 单例

        protected override void Awake()
        {
            base.Awake();
            instance = this;                        // 设置单例
        }

        protected override void Start()
        {
            base.Start();

            // 为每个选项注册点击事件
            foreach (ChoiceSelectorChoice choice in choices)
                choice.onClick += OnClickChoice;
        }

        protected override void Update()
        {
            base.Update();

            // 如果当前选择类型已清空，则隐藏面板
            Game game = GameClient.Get().GetGameData();
            if (game != null && game.selector == SelectorType.None)
                Hide();
        }

        // 刷新选择面板显示
        public void RefreshPanel()
        {
            if (ability == null)
                return;

            // 隐藏所有选项
            foreach (ChoiceSelectorChoice choice in choices)
                choice.Hide();

            Game gdata = GameClient.Get().GetGameData();
            Player player = GameClient.Get().GetPlayer();

            int index = 0;
            foreach (AbilityData choice in ability.chain_abilities)
            {
                if (choice != null && index < choices.Length)
                {
                    ChoiceSelectorChoice achoice = choices[index];
                    achoice.SetChoice(index, choice);                       // 设置选项
                    achoice.SetInteractable(GameClient.Get().Rules.CanSelectAbility(caster, choice)); // 是否可点击
                    index++;
                }
            }
        }

        // 点击某个选项
        public void OnClickChoice(int index)
        {
            Game data = GameClient.Get().GetGameData();
            if (data.selector == SelectorType.SelectorChoice)
            {
                GameClient.Get().SelectChoice(index); // 选择选项
                Hide();
            }
            else
            {
                Hide();
            }
        }

        // 点击取消
        public void OnClickCancel()
        {
            GameClient.Get().CancelSelection(); // 取消选择
            Hide();
        }

        // 显示面板（技能选择）
        public override void Show(AbilityData iability, Card caster)
        {
            this.caster = caster;
            this.ability = iability;
            Show();
        }

        public override void Show(bool instant = false)
        {
            base.Show(instant);
            RefreshPanel();                        // 刷新显示
        }

        // 判断是否应该显示面板
        public override bool ShouldShow()
        {
            Game data = GameClient.Get().GetGameData();
            int player_id = GameClient.Get().GetPlayerID();
            return data.selector == SelectorType.SelectorChoice && data.selector_player_id == player_id;
        }

        // 获取单例
        public static ChoiceSelector Get()
        {
            return instance;
        }
    }
}

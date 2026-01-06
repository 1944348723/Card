using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TcgEngine.Client;

namespace TcgEngine.UI
{
    /// <summary>
    /// 单人游戏面板
    /// 用于选择玩家与 AI 的套牌并开始单人游戏
    /// </summary>
    public class SoloPanel : UIPanel
    {
        public Text username;                  // 显示当前用户名
        public DeckSelector selector_player;   // 玩家套牌选择器
        public DeckSelector selector_ai;       // AI 套牌选择器

        public DeckDisplay display_player;     // 玩家套牌预览
        public DeckDisplay display_ai;         // AI 套牌预览

        private static SoloPanel instance;     // 单例实例

        protected override void Awake()
        {
            base.Awake();
            instance = this;
        }

        protected override void Start()
        {
            base.Start();

            // 注册套牌变化事件
            selector_player.onChange += OnChangeDeck;
            selector_ai.onChange += OnChangeDeck;
        }

        /// <summary>
        /// 刷新玩家与 AI 的套牌列表
        /// </summary>
        private void RefreshDecks()
        {
            if(username != null)
                username.text = Authenticator.Get().Username; // 显示用户名

            string selected_id = MainMenu.Get().deck_selector.GetDeckID();
            selector_player.SetupUserDeckList(); // 设置玩家套牌列表
            selector_player.SelectDeck(selected_id); // 默认选择主菜单中选中的套牌
            selector_ai.SetupAIDeckList();          // 设置 AI 套牌列表
            selector_ai.SelectDeck(0);              // 默认选择第一套 AI 套牌

            RefreshDeckDisplay(); // 刷新套牌显示
        }

        /// <summary>
        /// 刷新套牌预览显示
        /// </summary>
        private void RefreshDeckDisplay()
        {
            display_player.SetDeck(selector_player.GetDeckID());
            display_ai.SetDeck(selector_ai.GetDeckID());
        }

        /// <summary>
        /// 当套牌选择变化时刷新显示
        /// </summary>
        private void OnChangeDeck(string id)
        {
            RefreshDeckDisplay();
        }

        public override void Show(bool instant = false)
        {
            base.Show(instant);
            RefreshDecks(); // 显示面板时刷新套牌
        }

        /// <summary>
        /// 点击“开始游戏”按钮
        /// </summary>
        public void OnClickPlay()
        {
            UserDeckData deck = selector_player.GetDeck();
            if (deck == null || !deck.IsValid())
                return;

            UserDeckData aideck = selector_ai.GetDeck();
            if (aideck == null || !aideck.IsValid())
                return;

            // 设置玩家与 AI 套牌
            GameClient.player_settings.deck = deck;
            GameClient.ai_settings.deck = aideck;
            GameClient.ai_settings.ai_level = GameplayData.Get().ai_level;
            GameClient.game_settings.scene = GameplayData.Get().GetRandomArena();

            // 开始单人游戏
            MainMenu.Get().StartGame(GameType.Solo, GameMode.Casual);
        }

        public static SoloPanel Get()
        {
            return instance; // 获取单例
        }
    }
}

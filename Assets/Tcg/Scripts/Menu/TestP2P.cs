using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine.Client;
using TcgEngine.UI;

namespace TcgEngine
{
    /// <summary>
    /// P2P 菜单场景的主脚本
    /// 用于测试玩家与玩家之间的联网对战（点对点/本地服务器）
    /// </summary>
    public class TestP2P : MonoBehaviour
    {
        public UIPanel deck_panel;       // 套牌选择面板
        public UIPanel join_panel;       // 加入游戏面板
        public InputField username;      // 用户名输入框
        public InputField password;      // 密码输入框
        public DeckSelector deck_selector; // 套牌选择器
        public DeckDisplay deck_preview; // 套牌预览显示
        public InputField join_ip;       // 加入游戏的服务器 IP 输入框
        public Text error;               // 错误提示文本

        private bool starting = false;   // 游戏是否已开始标记

        void Start()
        {
            // 初始化游戏设置
            GameClient.game_settings = GameSettings.Default;
            GameClient.player_settings = PlayerSettings.Default;
            GameClient.game_settings.game_uid = "test_p2p";

            deck_selector.onChange += OnChangeDeck; // 套牌选择变化事件
            error.text = "";
        }

        void Update()
        {
            // 此处保留空的更新逻辑
        }

        /// <summary>
        /// 登录方法（异步）
        /// 使用用户名和密码进行 API 登录，并加载用户数据
        /// </summary>
        private async void Login()
        {
            error.text = "";
            bool success = await Authenticator.Get().Login(username.text, password.text);
            if (success)
            {
                UserData udata = await Authenticator.Get().LoadUserData();
                GameClient.player_settings.avatar = udata.GetAvatar();     // 设置玩家头像
                GameClient.player_settings.cardback = udata.GetCardback(); // 设置玩家卡背
                deck_panel.Show();                                         // 显示套牌选择面板
                RefreshDeckList();                                         // 刷新套牌列表
            }
            else
            {
                error.text = Authenticator.Get().GetError();              // 显示错误信息
            }
        }

        /// <summary>
        /// 开始游戏
        /// </summary>
        public void StartGame()
        {
            if (!starting)
            {
                starting = true;
                SceneNav.GoTo(GameClient.game_settings.GetScene()); // 加载游戏场景
            }
        }

        /// <summary>
        /// 刷新玩家套牌列表
        /// </summary>
        public void RefreshDeckList()
        {
            deck_selector.SetupUserDeckList();                  // 设置用户套牌列表
            deck_selector.SelectDeck(GameClient.player_settings.deck.tid); // 选择当前套牌
            RefreshDeck(deck_selector.GetDeckID());             // 刷新套牌预览
        }

        /// <summary>
        /// 刷新套牌显示
        /// </summary>
        private void RefreshDeck(string tid)
        {
            UserData user = Authenticator.Get().UserData;
            UserDeckData udeck = user.GetDeck(tid);
            DeckData ddeck = DeckData.Get(tid);
            if (udeck != null)
                deck_preview.SetDeck(udeck); // 显示用户套牌
            else if (ddeck != null)
                deck_preview.SetDeck(ddeck); // 显示基础套牌
            else
                deck_preview.Clear();        // 清空预览
        }

        /// <summary>
        /// 当选择套牌变化时调用
        /// </summary>
        public void OnChangeDeck(string tid)
        {
            GameClient.player_settings.deck = deck_selector.GetDeck(); // 更新玩家套牌
            PlayerPrefs.SetString("tcg_deck", tid);                    // 保存选择的套牌 ID
            RefreshDeck(tid);                                          // 刷新预览
        }

        /// <summary>
        /// 登录按钮点击事件
        /// </summary>
        public void OnClickLogin()
        {
            if (username.text.Length == 0)
                return;

            Login();
        }

        /// <summary>
        /// 主机游戏按钮点击事件（本地 P2P 主机）
        /// </summary>
        public void OnClickHost()
        {
            GameClient.game_settings.game_type = GameType.HostP2P; // 设置为 P2P 主机模式
            GameClient.game_settings.server_url = "127.0.0.1";     // 本地服务器
            GameClient.player_settings.deck = deck_selector.GetDeck();
            StartGame();
        }

        /// <summary>
        /// 点击“加入游戏”按钮
        /// </summary>
        public void OnClickGoJoin()
        {
            GameClient.player_settings.deck = deck_selector.GetDeck();
            deck_panel.Hide();
            join_panel.Show();
        }

        /// <summary>
        /// 加入远程游戏按钮点击事件
        /// </summary>
        public void OnClickJoin()
        {
            if (join_ip.text.Length == 0)
                return;

            GameClient.game_settings.game_type = GameType.Multiplayer; // 设置为多人模式
            GameClient.game_settings.server_url = join_ip.text;        // 服务器 IP
            StartGame();
        }

        /// <summary>
        /// 返回按钮点击事件
        /// </summary>
        public void OnClickBack()
        {
            if (join_panel.IsVisible())
            {
                join_panel.Hide();
                deck_panel.Show();
            }
            else
            {
                deck_panel.Hide();
            }
        }
    }
}

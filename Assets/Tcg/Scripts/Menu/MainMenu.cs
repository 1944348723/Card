using System.Collections;
using System.Collections.Generic;
using TcgEngine.Client;
using UnityEngine;
using UnityEngine.UI;

namespace TcgEngine.UI
{
    /// <summary>
    /// 主菜单场景主脚本
    /// 控制主菜单 UI 显示、玩家信息、牌组选择、多种游戏模式和匹配功能
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        public AudioClip music;           // 背景音乐
        public AudioClip ambience;        // 环境音效

        [Header("Player UI")]
        public Text username_txt;         // 显示用户名
        public Text credits_txt;          // 显示金币/积分
        public AvatarUI avatar;           // 玩家头像 UI
        public GameObject loader;         // 匹配加载提示

        [Header("UI")]
        public Text version_text;         // 游戏版本文本
        public DeckSelector deck_selector; // 牌组选择器
        public DeckDisplay deck_preview;  // 牌组预览

        private bool starting = false;    // 防止重复启动游戏

        private static MainMenu instance; // 单例

        void Awake()
        {
            instance = this;

            // 设置默认游戏设置
            Application.targetFrameRate = 120;
            GameClient.game_settings = GameSettings.Default;
        }

        private void Start()
        {
            BlackPanel.Get().Show(true);
            AudioTool.Get().PlayMusic("music", music);
            AudioTool.Get().PlaySFX("ambience", ambience, 0.5f, true, true);

            username_txt.text = "";
            credits_txt.text = "";
            version_text.text = "Version " + Application.version;

            deck_selector.onChange += OnChangeDeck;

            // 检查是否已登录
            if (Authenticator.Get().IsConnected())
                AfterLogin();
            else
                RefreshLogin();
        }

        void Update()
        {
            UserData udata = Authenticator.Get().UserData;
            if (udata != null)
            {
                credits_txt.text = GameUI.FormatNumber(udata.coins); // 更新金币显示
            }

            // 更新匹配状态显示
            bool matchmaking = GameClientMatchmaker.Get().IsMatchmaking();
            if (loader.activeSelf != matchmaking)
                loader.SetActive(matchmaking);
            if (MatchmakingPanel.Get().IsVisible() != matchmaking)
                MatchmakingPanel.Get().SetVisible(matchmaking);
        }

        /// <summary>
        /// 尝试刷新登录状态，如果已登录则进入主菜单，否则跳转登录界面
        /// </summary>
        private async void RefreshLogin()
        {
            bool success = await Authenticator.Get().RefreshLogin();
            if (success)
                AfterLogin();
            else
                SceneNav.GoTo("LoginMenu");
        }

        /// <summary>
        /// 登录后初始化玩家数据和事件
        /// </summary>
        private void AfterLogin()
        {
            BlackPanel.Get().Hide();

            // 注册匹配事件
            GameClientMatchmaker matchmaker = GameClientMatchmaker.Get();
            matchmaker.onMatchmaking += OnMatchmakingDone;
            matchmaker.onMatchList += OnReceiveObserver;

            // 读取玩家默认牌组
            GameClient.player_settings.deck.tid = PlayerPrefs.GetString("tcg_deck_" + Authenticator.Get().Username, "");

            // 刷新玩家信息
            RefreshUserData();

            // 好友列表（可选显示）
            //FriendPanel.Get().Show();
        }

        /// <summary>
        /// 刷新玩家数据
        /// </summary>
        public async void RefreshUserData()
        {
            UserData user = await Authenticator.Get().LoadUserData();
            if (user != null)
            {
                username_txt.text = user.username;
                credits_txt.text = GameUI.FormatNumber(user.coins);

                AvatarData avatar = AvatarData.Get(user.avatar);
                this.avatar.SetAvatar(avatar);

                // 刷新牌组列表
                RefreshDeckList();
            }
        }

        /// <summary>
        /// 刷新牌组列表 UI
        /// </summary>
        public void RefreshDeckList()
        {
            deck_selector.SetupUserDeckList();
            deck_selector.SelectDeck(GameClient.player_settings.deck.tid);
            RefreshDeck(deck_selector.GetDeckID());
        }

        /// <summary>
        /// 刷新牌组预览
        /// </summary>
        private void RefreshDeck(string tid)
        {
            if (deck_preview != null)
            {
                deck_preview.SetDeck(tid);
            }
        }

        /// <summary>
        /// 牌组切换回调
        /// </summary>
        private void OnChangeDeck(string tid)
        {
            GameClient.player_settings.deck = deck_selector.GetDeck();
            PlayerPrefs.SetString("tcg_deck_" + Authenticator.Get().Username, tid);
            RefreshDeck(tid);
        }

        /// <summary>
        /// 匹配完成回调
        /// </summary>
        private void OnMatchmakingDone(MatchmakingResult result)
        {
            if (result == null)
                return;

            if (result.success)
            {
                Debug.Log("Matchmaking found: " + result.success + " " + result.server_url + "/" + result.game_uid);
                StartGame(GameType.Multiplayer, result.game_uid, result.server_url);
            }
            else
            {
                MatchmakingPanel.Get().SetCount(result.players);
            }
        }

        /// <summary>
        /// 接收到观察者匹配列表
        /// </summary>
        private void OnReceiveObserver(MatchList list)
        {
            MatchListItem target = null;
            foreach (MatchListItem item in list.items)
            {
                if (item.username == GameClient.observe_user)
                    target = item;
            }

            if (target != null)
            {
                StartGame(GameType.Observer, target.game_uid, target.game_url);
            }
        }

        /// <summary>
        /// 启动游戏（根据游戏类型和模式）
        /// </summary>
        public void StartGame(GameType type, GameMode mode)
        {
            string uid = GameTool.GenerateRandomID();
            GameClient.game_settings.game_type = type;
            GameClient.game_settings.game_mode = mode;
            StartGame(uid); 
        }

        /// <summary>
        /// 启动游戏（指定游戏 UID 和服务器）
        /// </summary>
        public void StartGame(GameType type, string game_uid, string server_url = "")
        {
            GameClient.game_settings.game_type = type;
            StartGame(game_uid, server_url);
        }

        /// <summary>
        /// 启动游戏（仅指定游戏 UID 和服务器 URL）
        /// </summary>
        public void StartGame(string game_uid, string server_url = "")
        {
            if (!starting)
            {
                starting = true;
                GameClient.game_settings.server_url = server_url;
                GameClient.game_settings.game_uid = game_uid;
                GameClientMatchmaker.Get().Disconnect();
                FadeToScene(GameClient.game_settings.GetScene());
            }
        }

        /// <summary>
        /// 开始观察指定玩家
        /// </summary>
        public void StartObserve(string user)
        {
            GameClient.observe_user = user;
            GameClientMatchmaker.Get().StopMatchmaking();
            GameClientMatchmaker.Get().RefreshMatchList(user);
        }

        /// <summary>
        /// 发起挑战指定玩家
        /// </summary>
        public void StartChallenge(string user)
        {
            string self = Authenticator.Get().Username;
            if (self == user)
                return; // 不能挑战自己

            string key;
            if (self.CompareTo(user) > 0)
                key = self + "-" + user;
            else
                key = user + "-" + self;

            StartMathmaking(GameMode.Casual, key);
        }

        /// <summary>
        /// 开始匹配
        /// </summary>
        public void StartMathmaking(GameMode mode, string group)
        {
            UserDeckData deck = deck_selector.GetDeck();
            if (deck != null)
            {
                GameClient.game_settings.game_type = GameType.Multiplayer;
                GameClient.game_settings.game_mode = mode;
                GameClient.player_settings.deck = deck;
                GameClient.game_settings.scene = GameplayData.Get().GetRandomArena();
                GameClientMatchmaker.Get().StartMatchmaking(group, GameClient.game_settings.nb_players);
            }
        }

        /// <summary>
        /// 点击单人模式
        /// </summary>
        public void OnClickSolo()
        {
            if (!Authenticator.Get().IsConnected())
            {
                FadeToScene("LoginMenu");
                return;
            }

            SoloPanel.Get().Show();
        }

        /// <summary>
        /// 点击 PvP（玩家对战）模式
        /// </summary>
        public void OnClickPvP()
        {
            if (!Authenticator.Get().IsConnected())
            {
                FadeToScene("LoginMenu");
                return;
            }

            UserDeckData deck = deck_selector.GetDeck();
            if (deck == null || !deck.IsValid())
                return;

            StartMathmaking(GameMode.Ranked, "");
        }

        /// <summary>
        /// 点击冒险模式
        /// </summary>
        public void OnClickAdventure()
        {
            AdventurePanel.Get().Show();
        }

        /// <summary>
        /// 点击加入游戏代码模式
        /// </summary>
        public void OnClickPlayCode()
        {
            JoinCodePanel.Get().Show();
        }
        
        /// <summary>
        /// 取消匹配
        /// </summary>
        public void OnClickCancelMatch()
        {
            GameClientMatchmaker.Get().StopMatchmaking();
        }

        /// <summary>
        /// 打开设置面板
        /// </summary>
        public void OnClickSettings()
        {
            SettingsPanel.Get().Show();
        }

        /// <summary>
        /// 场景淡入淡出跳转
        /// </summary>
        public void FadeToScene(string scene)
        {
            StartCoroutine(FadeToRun(scene));
        }

        private IEnumerator FadeToRun(string scene)
        {
            BlackPanel.Get().Show();
            AudioTool.Get().FadeOutMusic("music");
            yield return new WaitForSeconds(1f);
            SceneNav.GoTo(scene);
        }

        /// <summary>
        /// 点击注销
        /// </summary>
        public void OnClickLogout()
        {
            TcgNetwork.Get().Disconnect();
            Authenticator.Get().Logout();
            FadeToScene("LoginMenu");
        }

        /// <summary>
        /// 点击退出游戏
        /// </summary>
        public void OnClickQuit()
        {
            Application.Quit();
        }

        /// <summary>
        /// 获取 MainMenu 单例
        /// </summary>
        public static MainMenu Get()
        {
            return instance;
        }
    }
}

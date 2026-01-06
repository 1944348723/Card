using System.Collections;
using System.Collections.Generic;
using TcgEngine.Client;
using UnityEngine;
using UnityEngine.UI;

namespace TcgEngine.UI
{
    /// <summary>
    /// LevelUI 类
    /// 游戏关卡面板中的单个关卡显示控件
    /// 显示关卡标题、等级、玩家使用的卡组，以及是否完成状态
    /// </summary>
    public class LevelUI : MonoBehaviour
    {
        [Header("Level")]
        /// <summary>
        /// 当前关卡数据
        /// </summary>
        public LevelData level;

        [Header("UI")]
        /// <summary>
        /// 关卡标题文本
        /// </summary>
        public Text title;

        /// <summary>
        /// 关卡子标题文本（显示 LEVEL 编号）
        /// </summary>
        public Text subtitle;

        /// <summary>
        /// 显示关卡使用的卡组
        /// </summary>
        public DeckDisplay deck;

        /// <summary>
        /// 关卡完成标识（完成奖励后显示）
        /// </summary>
        public GameObject completed;

        /// <summary>
        /// Start 生命周期
        /// 初始化按钮点击事件，隐藏完成标识，刷新 UI
        /// </summary>
        void Start()
        {
            Button btn = GetComponent<Button>();
            btn.onClick.AddListener(OnClick);
            completed.SetActive(false);

            if (level != null)
                SetLevel(level); // 刷新 UI
            else
                Hide(); // 隐藏 UI
        }

        /// <summary>
        /// 设置关卡数据并刷新显示
        /// </summary>
        /// <param name="level">关卡数据对象</param>
        public void SetLevel(LevelData level)
        {
            this.level = level;
            RefreshLevel();
        }

        /// <summary>
        /// 根据关卡数据刷新显示
        /// 更新标题、子标题、卡组和完成状态
        /// </summary>
        public void RefreshLevel()
        {
            if (level != null)
            {
                title.text = level.title;
                subtitle.text = "LEVEL " + level.level;
                deck.SetDeck(level.player_deck); // 设置玩家卡组
                gameObject.SetActive(true);

                // 显示是否完成
                UserData udata = Authenticator.Get().GetUserData();
                if(udata != null)
                    completed.SetActive(udata.HasReward(level.id));
            }
        }

        /// <summary>
        /// 隐藏 UI
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 点击关卡 UI 时触发
        /// 设置游戏客户端关卡信息，并开始游戏
        /// </summary>
        public void OnClick()
        {
            if (level != null)
            {
                GameClient.game_settings.level = level.id;
                GameClient.game_settings.scene = level.scene;
                GameClient.player_settings.deck = new UserDeckData(level.player_deck);
                GameClient.ai_settings.deck = new UserDeckData(level.ai_deck);
                GameClient.ai_settings.ai_level = level.ai_level;
                MainMenu.Get().StartGame(GameType.Adventure, GameMode.Casual);
            }
        }
    }
}

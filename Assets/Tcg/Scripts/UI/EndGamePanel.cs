using TcgEngine.Client;
using UnityEngine;
using UnityEngine.UI;

namespace TcgEngine.UI
{
    /// <summary>
    /// 游戏结束面板
    /// 在游戏结束时显示，用于显示胜利者和获得的奖励
    /// </summary>
    public class EndGamePanel : UIPanel
    {
        public Text winner_text;       // 显示胜利/失败/平局
        public Image winner_glow;      // 胜利者头像高亮光效

        public Text player_name;       // 玩家名字
        public Text other_name;        // 对手名字
        public Image player_avatar;    // 玩家头像
        public Image other_avatar;     // 对手头像

        public Text coins_text;        // 奖励金币文本
        public Text xp_text;           // 奖励经验文本

        private bool reward_loaded = false; // 奖励是否已加载
        private float timer = 0f;           // 延时计时器

        private int target_coins = 0; // 目标金币数
        private int target_xp = 0;    // 目标经验数
        private float coins = 0;      // 当前显示金币数
        private float xp = 0;         // 当前显示经验数

        private static EndGamePanel _instance; // 单例

        protected override void Awake()
        {
            base.Awake();
            _instance = this;
        }

        protected override void Start()
        {
            base.Start();

            coins_text.text = "";
            xp_text.text = "";
        }

        protected override void Update()
        {
            base.Update();

            // 延时加载奖励
            if (!reward_loaded && IsVisible())
            {
                timer += Time.deltaTime;
                if (timer > 1f)
                {
                    timer = 0f;
                    RefreshRewards();
                }
            }

            // 显示金币和经验逐渐增加的动画
            if (reward_loaded)
            {
                coins = Mathf.MoveTowards(coins, target_coins, 2000f * Time.deltaTime);
                xp = Mathf.MoveTowards(xp, target_xp, 500f * Time.deltaTime);

                coins_text.text = "+ " + Mathf.RoundToInt(coins) + " coins";
                xp_text.text = "+ " + Mathf.RoundToInt(xp) + " xp";

                if (Mathf.RoundToInt(coins) == 0)
                    coins_text.text = "";
                if (Mathf.RoundToInt(xp) == 0)
                    xp_text.text = "";
            }
        }

        /// <summary>
        /// 刷新面板信息，包括胜利者、玩家和对手信息
        /// </summary>
        private void RefreshPanel(int winner)
        {
            Game data = GameClient.Get().GetGameData();
            Player pwinner = data.GetPlayer(winner); // 胜利者
            Player player = GameClient.Get().GetPlayer(); // 当前玩家
            Player oplayer = GameClient.Get().GetOpponentPlayer(); // 对手

            player_name.text = player.username;
            other_name.text = oplayer.username;

            AvatarData avat1 = AvatarData.Get(player.avatar);
            AvatarData avat2 = AvatarData.Get(oplayer.avatar);
            if(avat1 != null)
                player_avatar.sprite = avat1.avatar;
            if (avat2 != null)
                other_avatar.sprite = avat2.avatar;

            if (pwinner != null && pwinner == player)
                winner_text.text = "Victory";  // 胜利
            else if (pwinner != null)
                winner_text.text = "Defeat";   // 失败
            else
                winner_text.text = "Tie";      // 平局

            // 胜利者头像光效位置
            if (pwinner == player)
                winner_glow.rectTransform.anchoredPosition = player_avatar.rectTransform.anchoredPosition;
            if (pwinner == oplayer)
                winner_glow.rectTransform.anchoredPosition = other_avatar.rectTransform.anchoredPosition;
            winner_glow.gameObject.SetActive(pwinner != null);
        }

        /// <summary>
        /// 刷新奖励（金币和经验）
        /// 根据在线或冒险模式获取奖励
        /// </summary>
        private async void RefreshRewards()
        {
            // 在线奖励
            if (GameClient.game_settings.IsOnline())
            {
                string url = ApiClient.ServerURL + "/matches/" + GameClient.game_settings.game_uid;
                WebResponse res = await ApiClient.Get().SendGetRequest(url);
                if (res.success)
                {
                    reward_loaded = true;
                    MatchResponse match = ApiTool.JsonToObject<MatchResponse>(res.data);
                    string username = ApiClient.Get().Username.ToLower();
                    foreach (MatchDataResponse data in match.udata)
                    {
                        if (data.username.ToLower() == username)
                        {
                            target_coins = data.reward.coins;
                            target_xp = data.reward.xp;
                        }
                    }
                }
            }

            // 冒险模式奖励
            if (GameClient.game_settings.game_type == GameType.Adventure)
            {
                LevelData lvl = LevelData.Get(GameClient.game_settings.level);
                if (lvl != null && RewardManager.Get().IsRewardGained())
                {
                    target_coins = lvl.reward_coins;
                    target_xp = lvl.reward_xp;
                    reward_loaded = true;
                }
            }
        }

        /// <summary>
        /// 显示游戏结束面板，并刷新信息
        /// </summary>
        public void ShowEnd(int winner)
        {
            reward_loaded = false;
            RefreshPanel(winner);
            RefreshRewards();
            Show();
        }

        /// <summary>
        /// 点击退出按钮
        /// </summary>
        public void OnClickQuit()
        {
            GameUI.Get().OnClickQuit();
        }

        /// <summary>
        /// 获取单例
        /// </summary>
        public static EndGamePanel Get()
        {
            return _instance;
        }
    }
}

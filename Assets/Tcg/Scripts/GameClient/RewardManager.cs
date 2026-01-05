using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace TcgEngine.Client
{
    // 冒险模式奖励管理器
    // 负责在玩家完成冒险关卡后发放奖励（金币、经验、卡牌、卡包等）
    public class RewardManager : MonoBehaviour
    {
        // 是否已经领取过奖励
        private bool reward_gained = false;

        // 静态实例，方便全局访问
        private static RewardManager instance;

        void Awake()
        {
            // 保存静态实例
            instance = this;
        }

        private void Start()
        {
            // 订阅游戏结束事件，当游戏结束时触发 OnGameEnd
            GameClient.Get().onGameEnd += OnGameEnd;
        }

        /// <summary>
        /// 游戏结束时触发
        /// </summary>
        /// <param name="winner">胜利玩家ID</param>
        void OnGameEnd(int winner)
        {
            int player_id = GameClient.Get().GetPlayerID();

            // 如果当前是冒险模式，并且玩家胜利
            if (GameClient.game_settings.game_type == GameType.Adventure && winner == player_id)
            {
                UserData udata = Authenticator.Get().UserData;
                LevelData level = LevelData.Get(GameClient.game_settings.level);

                // 如果关卡存在且玩家尚未领取奖励，并且本局奖励未发放
                if (level != null && !udata.HasReward(level.id) && !reward_gained)
                {
                    // 测试模式奖励处理
                    if (Authenticator.Get().IsTest())
                        GainRewardTest(level);

                    // API模式奖励处理
                    if (Authenticator.Get().IsApi())
                        GainRewardAPI(level);
                }
            }
        }

        /// <summary>
        /// 测试模式下直接发放奖励
        /// </summary>
        /// <param name="level">关卡数据</param>
        private async void GainRewardTest(LevelData level)
        {
            VariantData variant = VariantData.GetDefault(); // 默认卡牌变体
            UserData udata = Authenticator.Get().UserData;

            // 发放金币和经验
            udata.coins += level.reward_coins;
            udata.xp += level.reward_xp;

            // 标记奖励已领取
            udata.AddReward(level.id);

            // 发放卡牌
            foreach (CardData card in level.reward_cards)
            {
                udata.AddCard(card.id, variant.id, 1);
            }

            // 发放卡包
            foreach (PackData pack in level.reward_packs)
            {
                udata.AddPack(pack.id, 1);
            }

            reward_gained = true;

            // 保存用户数据
            await Authenticator.Get().SaveUserData();
        }

        /// <summary>
        /// API模式下发放奖励（向服务器请求）
        /// </summary>
        /// <param name="level">关卡数据</param>
        private async void GainRewardAPI(LevelData level)
        {
            bool success = await GainRewardAPI(level.id); // 调用API
            reward_gained = success; // 如果成功则标记奖励已领取
        }

        /// <summary>
        /// 向服务器请求领取奖励
        /// </summary>
        /// <param name="reward_id">奖励ID</param>
        /// <returns>是否领取成功</returns>
        public async Task<bool> GainRewardAPI(string reward_id)
        {
            RewardGainRequest req = new RewardGainRequest();
            req.reward = reward_id;

            // 构造请求URL
            string url = ApiClient.ServerURL + "/users/rewards/gain/" + ApiClient.Get().UserID;

            // 转换请求为JSON
            string json = ApiTool.ToJson(req);

            // 发送POST请求
            WebResponse res = await ApiClient.Get().SendPostRequest(url, json);

            Debug.Log("Gain Reward: " + reward_id + " " + res.success);

            return res.success;
        }

        /// <summary>
        /// 检查奖励是否已领取
        /// </summary>
        /// <returns></returns>
        public bool IsRewardGained()
        {
            return reward_gained;
        }

        /// <summary>
        /// 获取全局实例
        /// </summary>
        /// <returns></returns>
        public static RewardManager Get()
        {
            return instance;
        }
    } 
}

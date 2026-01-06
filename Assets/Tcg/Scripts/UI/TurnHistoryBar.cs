using System.Collections;
using System.Collections.Generic;
using TcgEngine.Client;
using UnityEngine;

namespace TcgEngine.UI
{
    /// <summary>
    /// 回合历史栏（TurnHistoryBar）
    /// 显示当前回合玩家执行过的所有操作记录
    /// </summary>
    public class TurnHistoryBar : MonoBehaviour
    {
        public bool is_opponent;                 // 是否显示对手的回合历史
        public TurnHistoryLine[] history_lines;  // UI上用于显示每条操作的行

        void Start()
        {
            // 初始无需操作
        }

        void Update()
        {
            // 如果客户端尚未准备好，则不更新
            if (!GameClient.Get().IsReady())
                return;

            // 获取当前玩家ID或对手ID
            int player_id = is_opponent ? GameClient.Get().GetOpponentPlayerID() : GameClient.Get().GetPlayerID();
            // 获取游戏数据
            Game data = GameClient.Get().GetGameData();
            // 获取玩家数据
            Player player = data.GetPlayer(player_id);

            if (player != null && player.history_list != null)
            {
                int index = 0;
                // 遍历玩家本回合的操作历史
                foreach (ActionHistory order in player.history_list)
                {
                    if (index < history_lines.Length)
                    {
                        // 更新对应的UI行显示操作
                        history_lines[index].SetLine(order);
                        index++;
                    }
                }

                // 对于剩余未使用的行，隐藏它们
                while (index < history_lines.Length)
                {
                    history_lines[index].Hide();
                    index++;
                }
            }
        }
    }
}
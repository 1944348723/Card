using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Client;
using UnityEngine.Events;
using TcgEngine.UI;

namespace TcgEngine.Client
{
    /// <summary>
    /// GameBoard 负责：
    /// 1根据服务器最新同步的数据，动态生成和销毁 BoardCard（场上的卡牌实例）
    /// 2当服务器发送游戏结束状态时，负责触发游戏结束流程（结算、音效、动画等）
    /// </summary>
    public class GameBoard : MonoBehaviour
    {
        public GameObject card_prefab;   // 场上卡牌的预制体

        public UnityAction<Card> onCardSpawned; // 当有卡被生成时触发
        public UnityAction<Card> onCardKilled;  // 当有卡被移除时触发

        private bool game_ended = false; // 游戏是否已经结束，防止重复触发

        private static GameBoard _instance; // 单例

        void Awake()
        {
            _instance = this;   // 记录唯一实例
        }

        private void Start()
        {
            
        }

        void Update()
        {
            // 如果 GameClient 还没准备好（未连接或未收到游戏数据），不执行逻辑
            if (!GameClient.Get().IsReady())
                return;

            Game data = GameClient.Get().GetGameData();

            // ----------- 场上卡牌管理 -------------

            List<BoardCard> cards = BoardCard.GetAll();   // 当前已存在的场上卡牌对象

            // 遍历服务器数据中的所有玩家与他们的场上卡牌
            // 找出服务器存在但本地未生成的 → 生成
            foreach (Player p in data.players)
            {
                foreach (Card card in p.cards_board)
                {
                    BoardCard bcard = BoardCard.Get(card.uid);
                    if (card != null && bcard == null)
                    {
                        SpawnNewCard(card); // 创建新卡
                    }
                }
            }

            // 遍历现有场上卡，找出：
            // 服务器已经删除的 或 标记死亡但动画结束 → 销毁
            for (int i = cards.Count - 1; i >= 0; i--)
            {
                BoardCard bcard = cards[i];
                if (bcard != null && HasBoardCard(bcard) && !bcard.IsDamagedDelayed())
                {
                    KillCard(bcard);
                }
            }

            // ----------- 游戏结束检测 --------------
            if (!game_ended && data.state == GameState.GameEnded)
            {
                game_ended = true;
                EndGame();
            }
        }

        /// <summary>
        /// 生成新的场上卡牌对象
        /// </summary>
        private void SpawnNewCard(Card card)
        {
            GameObject card_obj = Instantiate(card_prefab, Vector3.zero, Quaternion.identity);
            card_obj.SetActive(true);
            card_obj.GetComponent<BoardCard>().SetCard(card); // 绑定服务器数据
            onCardSpawned?.Invoke(card); // 触发回调
        }

        /// <summary>
        /// 销毁一张 BoardCard
        /// </summary>
        private void KillCard(BoardCard card)
        {
            card.Destroy(); // 播放死亡动画 + 销毁
            onCardKilled?.Invoke(card.GetCard());
        }

        /// <summary>
        /// 判断某个 BoardCard 是否已经不存在于服务器数据中
        /// 返回 true 表示应该删除
        /// </summary>
        private bool HasBoardCard(BoardCard bcard)
        {
            Game data = GameClient.Get().GetGameData();
            Card card = data.GetBoardCard(bcard.GetCardUID());
            return card == null && !bcard.IsDead();
        }

        /// <summary>
        /// 触发游戏结束流程
        /// </summary>
        public void EndGame()
        {
            StartCoroutine(EndGameRun());
        }

        /// <summary>
        /// 游戏结束协程：负责动画、音效、胜负 UI、特效播放
        /// </summary>
        private IEnumerator EndGameRun()
        {
            Game data = GameClient.Get().GetGameData();
            Player pwinner = data.GetPlayer(data.current_player);   // 获胜玩家
            Player player = GameClient.Get().GetPlayer();
            bool win = pwinner != null && player.player_id == pwinner.player_id;
            bool tied = pwinner == null;   // 平局

            AudioTool.Get().FadeOutMusic("music");  // 音乐淡出

            yield return new WaitForSeconds(1f);

            // UI 杀死一方头像血条动画
            if (win)
                PlayerUI.Get(true).Kill();
            if (!win && !tied)
                PlayerUI.Get(false).Kill();

            // 播放胜利/失败/平局特效
            if (win && AssetData.Get().win_fx != null)
                Instantiate(AssetData.Get().win_fx, Vector3.zero, Quaternion.identity);
            else if (tied && AssetData.Get().tied_fx != null)
                Instantiate(AssetData.Get().tied_fx, Vector3.zero, Quaternion.identity);
            else if (!tied && AssetData.Get().lose_fx != null)
                Instantiate(AssetData.Get().lose_fx, Vector3.zero, Quaternion.identity);

            // 音效
            if (win)
                AudioTool.Get().PlaySFX("ending_sfx", AssetData.Get().win_audio);
            else
                AudioTool.Get().PlaySFX("ending_sfx", AssetData.Get().defeat_audio);

            // 背景音乐
            if (win)
                AudioTool.Get().PlayMusic("music", AssetData.Get().win_music, 0.4f, false);
            else
                AudioTool.Get().PlayMusic("music", AssetData.Get().defeat_music, 0.4f, false);

            yield return new WaitForSeconds(2f);

            // 打开结算界面
            EndGamePanel.Get().ShowEnd(data.current_player);
        }

        /// <summary>
        /// 将鼠标屏幕射线投射到游戏棋盘平面，返回落点世界坐标
        /// </summary>
        public Vector3 RaycastMouseBoard()
        {
            Ray ray = GameCamera.Get().MouseToRay(Input.mousePosition);
            Plane plane = new Plane(transform.forward, 0f);
            bool success = plane.Raycast(ray, out float dist);
            if (success)
                return ray.GetPoint(dist);
            return Vector3.zero;
        }

        /// <summary>
        /// 获取棋盘旋转角度
        /// </summary>
        public Vector3 GetAngles()
        {
            return transform.rotation.eulerAngles;
        }

        /// <summary>
        /// 单例获取
        /// </summary>
        public static GameBoard Get()
        {
            return _instance;
        }
    }
}

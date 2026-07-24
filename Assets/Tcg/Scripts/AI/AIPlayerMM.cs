using System.Collections;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine.AI
{
    /// <summary>
    /// 使用 Minimax 算法的 AI 玩家。
    /// </summary>
    public class AIPlayerMM : AIPlayer
    {
        private readonly AILogic aiLogic;
        private bool isPlaying = false;

        public AIPlayerMM(GameLogic gameplay, int playerId, int level)
            : base(gameplay, playerId, Mathf.Clamp(level, 1, 10))
        {
            aiLogic = AILogic.Create(PlayerId, Level);
        }

        public override void Update()
        {
            Player player = GetPlayer();
            bool isPlayerTurn = gameplay.Rules.IsPlayerTurn(player);

            if (!isPlaying && isPlayerTurn)
            {
                isPlaying = true;
                TimeTool.StartCoroutine(AiTurn());
            }

            if (!isPlaying && gameplay.Rules.IsPlayerMulliganTurn(player))
                TryExecuteAction(new AIAction(GameAction.SelectMulligan));

            if (!isPlayerTurn && aiLogic.IsRunning())
                aiLogic.Stop();
        }

        private IEnumerator AiTurn()
        {
            yield return new WaitForSeconds(1f);

            if (!IsPlayerTurn())
            {
                isPlaying = false;
                yield break;
            }

            Game gameData = gameplay.GetGameData();
            aiLogic.RunAI(gameData);

            while (aiLogic.IsRunning())
                yield return new WaitForSeconds(0.1f);

            AIAction best = aiLogic.GetBestAction();
            if (best != null && IsPlayerTurn())
            {
                Debug.Log("执行 AI 动作: " + best.GetText(gameData) + "\n" + aiLogic.GetNodePath());
                TryExecuteAction(best);
            }

            aiLogic.ClearMemory();

            yield return new WaitForSeconds(0.5f);
            isPlaying = false;
        }

        private bool IsPlayerTurn()
        {
            return gameplay.Rules.IsPlayerTurn(GetPlayer());
        }
    }
}

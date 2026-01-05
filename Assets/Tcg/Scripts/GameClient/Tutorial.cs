using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine.Client
{
    /// <summary>
    /// 教程系统组件
    /// 管理单人冒险模式下的游戏教程逻辑
    /// 控制卡牌点击、攻击、技能释放等操作的引导
    /// </summary>
    public class Tutorial : MonoBehaviour
    {
        // 是否处于教程模式
        private bool is_tuto;

        // 当前教程步骤组
        private TutoStepGroup current_group;

        // 当前教程步骤
        private TutoStep current_step;

        // 锁定状态，用于延迟切换步骤
        private bool locked = false;

        // 静态实例，方便全局访问
        private static Tutorial instance;

        void Awake()
        {
            instance = this;
        }

        void Start()
        {
            // 仅在冒险模式下启用教程
            if (GameClient.game_settings.game_type == GameType.Adventure)
            {
                LevelData level = GameClient.game_settings.GetLevel();
                if (level.tuto_prefab != null)
                {
                    is_tuto = true;

                    // 实例化教程 UI 预制件
                    GameObject tuto_obj = Instantiate(level.tuto_prefab);
                    tuto_obj.GetComponent<Canvas>().worldCamera = GameCamera.GetCamera();

                    // 注册游戏事件回调，用于触发教程逻辑
                    GameClient.Get().onNewTurn += OnNewTurn;
                    GameClient.Get().onCardPlayed += OnCardPlayed;
                    GameClient.Get().onAttackStart += OnAttack;
                    GameClient.Get().onAttackPlayerStart += OnAttackPlayer;
                    GameClient.Get().onAbilityStart += OnCastAbility;
                    GameClient.Get().onAbilityTargetCard += OnTargetCard;
                    GameClient.Get().onAbilityTargetPlayer += OnTargetPlayer;
                }

                // 隐藏所有教程 UI
                HideAll();
            }
        }

        void Update()
        {
            if (GameClient.game_settings.game_type != GameType.Adventure)
                return;

            Game data = GameClient.Get().GetGameData();
            if (data == null)
                return;

            // 可以在此处增加每帧教程逻辑，例如动画、提示显示等
        }

        #region 游戏事件回调

        // 每当新回合开始
        private void OnNewTurn(int player_id)
        {
            Game data = GameClient.Get().GetGameData();
            if (data == null)
                return;

            EndGroup(); // 结束当前步骤组

            // 仅在玩家回合触发
            if (player_id != GameClient.Get().GetPlayerID())
                return;

            TutoStepGroup group = TutoStepGroup.Get(TutoStartTrigger.StartTurn, data.turn_count);
            ShowGroup(group);
        }

        // 玩家打出卡牌
        private void OnCardPlayed(Card card, Slot slot)
        {
            Hide(); // 隐藏当前步骤提示

            if (card.player_id == GameClient.Get().GetPlayerID())
            {
                TriggerEndStep(TutoEndTrigger.PlayCard);
                TriggerStartGroup(TutoStartTrigger.PlayCard, card);
            }
        }

        // 玩家攻击目标卡牌
        private void OnAttack(Card card, Card target)
        {
            Hide();

            if (card.player_id == GameClient.Get().GetPlayerID())
            {
                TriggerEndStep(TutoEndTrigger.Attack, 2f);
                TriggerStartGroup(TutoStartTrigger.Attack, card);
                TriggerStartGroup(TutoStartTrigger.Attack, target);
            }
        }

        // 玩家攻击敌方玩家
        private void OnAttackPlayer(Card card, Player target)
        {
            Hide();

            if (card.player_id == GameClient.Get().GetPlayerID())
            {
                TriggerEndStep(TutoEndTrigger.AttackPlayer, 2f);
                TriggerStartGroup(TutoStartTrigger.Attack, card);
            }
        }

        // 玩家施放技能
        private void OnCastAbility(AbilityData ability, Card card)
        {
            if (card.player_id == GameClient.Get().GetPlayerID())
            {
                TriggerEndStep(TutoEndTrigger.CastAbility);
                TriggerStartGroup(TutoStartTrigger.CastAbility, card);
            }
        }

        // 技能目标为卡牌
        private void OnTargetCard(AbilityData ability, Card card, Card target)
        {
            if (card.player_id == GameClient.Get().GetPlayerID())
            {
                TriggerEndStep(TutoEndTrigger.SelectTarget);
            }
        }

        // 技能目标为玩家
        private void OnTargetPlayer(AbilityData ability, Card card, Player target)
        {
            if (card.player_id == GameClient.Get().GetPlayerID())
            {
                TriggerEndStep(TutoEndTrigger.SelectTarget);
            }
        }

        #endregion

        #region 教程控制方法

        /// <summary>
        /// 根据触发器结束当前步骤
        /// </summary>
        public void TriggerEndStep(TutoEndTrigger trigger, float time = 1f)
        {
            if (current_step != null && current_step.end_trigger == trigger)
            {
                Hide();
                locked = true;
                TimeTool.WaitFor(time, () =>
                {
                    locked = false;
                    ShowNext();
                });
            }
        }

        /// <summary>
        /// 根据触发器开始新的步骤组
        /// </summary>
        public void TriggerStartGroup(TutoStartTrigger trigger, Card card)
        {
            if (current_group == null || !current_group.forced)
            {
                if (current_step == null || !current_step.forced)
                {
                    CardData target = card != null ? card.CardData : null;
                    ShowGroup(TutoStartTrigger.PlayCard, target);
                }
            }
        }

        /// <summary>
        /// 显示指定触发器对应的教程步骤组
        /// </summary>
        public void ShowGroup(TutoStartTrigger trigger, CardData target)
        {
            Game data = GameClient.Get().GetGameData();
            TutoStepGroup group = TutoStepGroup.Get(trigger, target, data.turn_count);
            ShowGroup(group);
        }

        /// <summary>
        /// 显示教程步骤组
        /// </summary>
        public void ShowGroup(TutoStepGroup group)
        {
            if (group != null)
            {
                current_group = group;
                group.SetTriggered(); // 标记已触发
                TutoStep step = TutoStep.Get(group, 0);
                Show(step);
            }
        }

        /// <summary>
        /// 显示下一步教程
        /// </summary>
        public void ShowNext()
        {
            if (current_group != null)
            {
                int index = GetNextIndex();
                TutoStep step = TutoStep.Get(current_group, index);
                if (step != null)
                    Show(step);
                else
                    EndGroup();
            }
        }

        /// <summary>
        /// 显示单个教程步骤
        /// </summary>
        public void Show(TutoStep step)
        {
            HideAll();
            current_step = step;
            if (step != null)
                step.Show();
        }

        /// <summary>
        /// 结束当前教程步骤组
        /// </summary>
        public void EndGroup()
        {
            HideAll();
            current_group = null;
            current_step = null;
        }

        /// <summary>
        /// 隐藏指定步骤
        /// </summary>
        public void Hide(TutoStep step)
        {
            if (step != null)
                step.Hide();
        }

        /// <summary>
        /// 隐藏当前步骤
        /// </summary>
        public void Hide()
        {
            Hide(current_step);
        }

        /// <summary>
        /// 检查当前触发是否可以执行
        /// </summary>
        public bool CanDo(TutoEndTrigger trigger)
        {
            return CanDo(trigger, null);
        }

        public bool CanDo(TutoEndTrigger trigger, Slot slot)
        {
            Game data = GameClient.Get().GetGameData();
            Card card = data.GetSlotCard(slot);
            return CanDo(trigger, card);
        }

        public bool CanDo(TutoEndTrigger trigger, Card target)
        {
            if (!is_tuto)
                return true; // 非教程模式直接允许

            if (locked)
                return false; // 锁定时禁止操作

            if (current_step != null && current_step.forced)
            {
                if (trigger == TutoEndTrigger.CastAbility && current_step.end_trigger == TutoEndTrigger.SelectTarget)
                    return true; // 取消选择目标时允许施法

                if (current_step.end_trigger != trigger)
                    return false; // 触发器不匹配

                CardData target_data = target != null ? target.CardData : null;
                if (current_step.trigger_target != null && current_step.trigger_target != target_data)
                    return false; // 目标不匹配
            }

            return true;
        }

        /// <summary>
        /// 获取下一步索引
        /// </summary>
        public int GetNextIndex()
        {
            if (current_step != null)
                return current_step.GetStepIndex() + 1;
            return 0;
        }

        public bool IsTuto()
        {
            return is_tuto;
        }

        public TutoEndTrigger GetEndTrigger()
        {
            if (current_step != null)
                return current_step.end_trigger;
            return TutoEndTrigger.Click;
        }

        public void HideAll()
        {
            TutoStep.HideAll();
        }

        public static Tutorial Get()
        {
            return instance;
        }

        #endregion
    }

    /// <summary>
    /// 教程步骤开始触发类型
    /// </summary>
    public enum TutoStartTrigger
    {
        StartTurn = 0,    // 回合开始
        PlayCard = 10,    // 打出卡牌
        Attack = 20,      // 攻击
        CastAbility = 30, // 施放技能
    }

    /// <summary>
    /// 教程步骤结束触发类型
    /// </summary>
    public enum TutoEndTrigger
    {
        Click = 0,           // 点击结束
        EndTurn = 5,         // 回合结束
        PlayCard = 10,       // 打出卡牌结束
        Move = 15,           // 移动结束
        Attack = 20,         // 攻击结束
        AttackPlayer = 25,   // 攻击玩家结束
        CastAbility = 30,    // 施放技能结束
        SelectTarget = 35,   // 选择目标结束
    }
}

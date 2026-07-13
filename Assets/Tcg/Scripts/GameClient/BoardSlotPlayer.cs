using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.UI;
using TcgEngine.FX;

namespace TcgEngine.Client
{
    /// <summary>
    /// 这个类代表“玩家区域”的一个可被攻击的可视化槽位
    /// 敌方卡牌可以以此为目标来对玩家造成伤害（即攻击玩家 HP）
    /// </summary>
    public class BoardSlotPlayer : BSlot
    {
        public bool opponent;          // 是否是对手玩家的区域（true 表示对手，false 表示自己）

        public float range_x = 3f;     // X 方向的可交互/选中范围
        public float range_y = 1f;     // Y 方向的可交互/选中范围

        private static BoardSlotPlayer instance_self;   // 本地玩家的实例
        private static BoardSlotPlayer instance_other;  // 对手玩家的实例

        private static List<BoardSlotPlayer> zone_list = new List<BoardSlotPlayer>();  // 所有玩家槽位（自己 + 对手）

        protected override void Awake()
        {
            base.Awake();
            zone_list.Add(this);

            // 根据 opponent 标记来区分我方与对方实例
            if (opponent)
                instance_other = this;
            else
                instance_self = this;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            zone_list.Remove(this);
        }

        private void Start()
        {
            // 监听伤害事件
            GameClient.Get().onPlayerDamaged += OnPlayerDamaged;

            // 监听技能开始事件（播施法 FX）
            GameClient.Get().onAbilityStart += OnAbilityStart;

            // 监听技能作用到玩家事件（投射物/目标 FX）
            GameClient.Get().onAbilityTargetPlayer += OnAbilityEffect;
        }

        protected override void Update()
        {
            base.Update();

            // 游戏未就绪就不处理
            if (!GameClient.Get().IsReady())
                return;

            // 只有“对手玩家区域”才需要被高亮为攻击目标
            if (!opponent)
                return;

            BoardCard bcard_selected = PlayerControls.Get().GetSelected(); // 当前选中的战场卡
            HandCard drag_card = HandCard.GetDrag();                       // 当前正在拖拽的手牌
            bool your_turn = GameClient.Get().IsYourTurn();                // 是否轮到你操作

            Game gdata = GameClient.Get().GetGameData();
            Player player = GameClient.Get().GetPlayer();
            Player oplayer = GameClient.Get().GetOpponentPlayer();

            // 默认不高亮
            target_alpha = 0f;

            // 正在选择战场卡牌攻击
            Card select_card = bcard_selected?.GetCard();
            if (select_card != null)
            {
                bool can_do_attack = GameClient.Get().Rules.IsPlayerActionTurn(player) && select_card.CanAttack(); // 当前卡是否能攻击
                bool can_be_attacked = GameClient.Get().Rules.CanAttackTarget(select_card, oplayer); // 对手是否可以成为攻击目标

                if (can_do_attack && can_be_attacked)
                {
                    target_alpha = 1f;   // 可攻击 → 高亮
                }
            }

            // 拖拽需要目标的法术并且玩家可作为合法目标
            if (your_turn && drag_card != null && drag_card.CardData.IsRequireTargetSpell()
                && GameClient.Get().Rules.IsPlayTargetValid(drag_card.GetCard(), GetPlayer()))
            {
                target_alpha = 1f;
            }

            // 选择目标阶段（例如技能指定攻击目标）
            if (gdata.selector == SelectorType.SelectTarget && player.player_id == gdata.selector_player_id)
            {
                Card caster = gdata.GetCard(gdata.selector_caster_uid);
                AbilityData ability = AbilityData.Get(gdata.selector_ability_id);
                if (ability != null && ability.AreTargetConditionsMet(gdata, caster, GetPlayer()))
                    target_alpha = 1f;
            }

        }

        // 当技能开始时（表现施法特效）
        private void OnAbilityStart(AbilityData iability, Card caster)
        {
            if (iability != null && caster != null)
            {
                int player_id = opponent ? GameClient.Get().GetOpponentPlayerID() : GameClient.Get().GetPlayerID();

                // 如果施法者是该玩家，并且是法术卡
                if (caster.CardData.type == CardType.Spell && caster.player_id == player_id)
                {
                    FXTool.DoFX(iability.caster_fx, transform.position);
                    AudioTool.Get().PlaySFX("fx", iability.cast_audio);
                }
            }
        }

        // 当技能真正作用到玩家（表现目标 FX + 投射物）
        private void OnAbilityEffect(AbilityData iability, Card caster, Player target)
        {
            if (iability != null && caster != null && target != null)
            {
                int player_id = opponent ? GameClient.Get().GetOpponentPlayerID() : GameClient.Get().GetPlayerID();
                if (target.player_id == player_id)
                {
                    FXTool.DoFX(iability.target_fx, transform.position);
                    FXTool.DoProjectileFX(iability.projectile_fx, GetFXSource(caster), transform, iability.GetDamage());
                    AudioTool.Get().PlaySFX("fx", iability.target_audio);
                }
            }
        }

        // 玩家受伤时触发伤害特效
        private void OnPlayerDamaged(Player target, int damage)
        {
            if (GetPlayerID() == target.player_id && damage > 0)
            {
                DamageFX(transform, damage);
            }
        }

        // 伤害数字飘字 + 特效
        private void DamageFX(Transform target, int value, float delay = 0.5f)
        {
            TimeTool.WaitFor(delay, () =>
            {
                GameObject fx = FXTool.DoFX(AssetData.Get().damage_fx, target.position);
                fx.GetComponent<DamageFX>().SetValue(value);
            });
        }

        // 获取 FX 发射源（卡牌或玩家）
        private Transform GetFXSource(Card caster)
        {
            if (caster.CardData.IsBoardCard())
            {
                BoardCard bcard = BoardCard.Get(caster.uid);
                if (bcard != null)
                    return bcard.transform;
            }
            else
            {
                BoardSlotPlayer slot = BoardSlotPlayer.Get(caster.player_id);
                if (slot != null)
                    return slot.transform;
            }
            return null;
        }

        // 鼠标点击（用于选择目标玩家）
        public void OnMouseDown()
        {
            if (GameUI.IsUIOpened() || GameUI.IsOverUILayer("UI"))
                return;

            Game gdata = GameClient.Get().GetGameData();
            int player_id = GameClient.Get().GetPlayerID();

            // 当前在“选择目标阶段”且是你的选择回合
            if (gdata.selector == SelectorType.SelectTarget && player_id == gdata.selector_player_id)
            {
                GameClient.Get().SelectPlayer(GetPlayer());
            }
        }

        // 获取玩家 ID（根据是否 opponent 判断）
        public int GetPlayerID()
        {
            return opponent ? GameClient.Get().GetOpponentPlayerID() : GameClient.Get().GetPlayerID();
        }

        // 获取 Player 数据对象
        public override Player GetPlayer()
        {
            return opponent ? GameClient.Get().GetOpponentPlayer() : GameClient.Get().GetPlayer();
        }

        // 转换为游戏逻辑层的 Slot
        public override Slot GetSlot()
        {
            return new Slot(GetPlayerID());
        }

        // 根据是否为对手获取对应实例
        public static BoardSlotPlayer Get(bool opponent)
        {
            if(opponent)
                return instance_other;
            return instance_self;
        }

        // 根据 player_id 获取实例
        public static BoardSlotPlayer Get(int player_id)
        {
            bool opponent = player_id != GameClient.Get().GetPlayerID();
            return Get(opponent);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// Defines all ability data
    /// </summary>

    [CreateAssetMenu(fileName = "ability", menuName = "TcgEngine/AbilityData", order = 5)]
    public class AbilityData : ScriptableObject
    {
        public string id; 
        // 技能唯一标识符

        [Header("触发条件")]
        public AbilityTrigger trigger;             
        // 技能触发时机（如上场、回合开始、主动等）
        public ConditionData[] conditions_trigger; 
        // 触发技能时需要满足的条件（通常检查施法者自身）

        [Header("目标")]
        public AbilityTarget target;               
        // 技能目标类型（如自身、对手、全场卡牌等）
        public ConditionData[] conditions_target;  
        // 检查目标是否有效的条件
        public FilterData[] filters_target;  
        // 对目标进行进一步筛选的过滤器（如随机、最高攻击等）

        [Header("效果")]
        public EffectData[] effects;              
        // 技能效果列表（具体执行逻辑在EffectData里）
        public StatusData[] status;               
        // 技能附加状态列表（如中毒、护盾等）
        public int value;                         
        // 技能效果值（例如造成X伤害或回复X生命）
        public int duration;                      
        // 技能状态持续时间，0表示永久

        [Header("链式/选项技能")]
        public AbilityData[] chain_abilities;    
        // 该技能触发后会执行的连锁技能

        [Header("主动技能")]
        public int mana_cost;                   
        // 主动技能法力消耗
        public bool exhaust;                    
        // 主动技能是否消耗行动

        [Header("特效/音效")]
        public GameObject board_fx;             
        // 技能棋盘特效
        public GameObject caster_fx;            
        // 施法者特效
        public GameObject target_fx;            
        // 目标特效
        public GameObject projectile_fx;        
        // 弹道特效
        public AudioClip cast_audio;            
        // 施放音效
        public AudioClip target_audio;          
        // 目标音效
        public bool charge_target;              
        // 是否需要选定目标

        [Header("文本信息")]
        public string title;
        // 技能名称
        [TextArea(5, 7)]
        public string desc;
        // 技能描述文本，可包含占位符 <name>、<value>、<duration>

        public static List<AbilityData> ability_list = new();                             
        // 静态列表缓存所有技能，用于循环快速访问
        public static Dictionary<string, AbilityData> ability_dict = new(); 
        // 静态字典缓存技能ID到技能对象映射，加快按ID查找

        public static void Load(string folder = "")
        {
            // 从Resources文件夹加载所有技能数据，仅加载一次
            if (ability_list.Count == 0)
            {
                ability_list.AddRange(Resources.LoadAll<AbilityData>(folder));

                foreach (AbilityData ability in ability_list)
                    ability_dict.Add(ability.id, ability);
            }
        }

        public string GetTitle()
        {
            return title;
            // 获取技能名称
        }

        public string GetDesc()
        {
            return desc;
            // 获取技能描述
        }

        public string GetDesc(CardData card)
        {
            string dsc = desc;
            // 替换描述中的占位符
            dsc = dsc.Replace("<name>", card.title);
            dsc = dsc.Replace("<value>", value.ToString());
            dsc = dsc.Replace("<duration>", duration.ToString());
            return dsc;
        }

        // 判断技能触发条件是否满足（施法者为触发者）
        public bool AreTriggerConditionsMet(Game data, Card caster)
        {
            return AreTriggerConditionsMet(data, caster, caster);
        }

        // 判断技能触发条件是否满足（由其他卡触发）
        public bool AreTriggerConditionsMet(Game data, Card caster, Card trigger_card)
        {
            foreach (ConditionData cond in conditions_trigger)
            {
                if (cond != null)
                {
                    if (!cond.IsTriggerConditionMet(data, this, caster))
                        return false;
                    if (!cond.IsTargetConditionMet(data, this, caster, trigger_card))
                        return false;
                }
            }
            return true;
        }

        // 判断技能触发条件是否满足（由玩家操作触发）
        public bool AreTriggerConditionsMet(Game data, Card caster, Player trigger_player)
        {
            foreach (ConditionData cond in conditions_trigger)
            {
                if (cond != null)
                {
                    if (!cond.IsTriggerConditionMet(data, this, caster))
                        return false;
                    if (!cond.IsTargetConditionMet(data, this, caster, trigger_player))
                        return false;
                }
            }
            return true;
        }

        // 检查卡牌目标是否满足条件
        public bool AreTargetConditionsMet(Game data, Card caster, Card target_card)
        {
            foreach (ConditionData cond in conditions_target)
            {
                if (cond != null && !cond.IsTargetConditionMet(data, this, caster, target_card))
                    return false;
            }
            return true;
        }

        // 检查玩家目标是否满足条件
        public bool AreTargetConditionsMet(Game data, Card caster, Player target_player)
        {
            foreach (ConditionData cond in conditions_target)
            {
                if (cond != null && !cond.IsTargetConditionMet(data, this, caster, target_player))
                    return false;
            }
            return true;
        }

        // 检查格子目标是否满足条件
        public bool AreTargetConditionsMet(Game data, Card caster, Slot target_slot)
        {
            foreach (ConditionData cond in conditions_target)
            {
                if (cond != null && !cond.IsTargetConditionMet(data, this, caster, target_slot))
                    return false;
            }
            return true;
        }

        // 检查卡牌数据目标是否满足条件（用于创建卡牌）
        public bool AreTargetConditionsMet(Game data, Card caster, CardData target_card)
        {
            foreach (ConditionData cond in conditions_target)
            {
                if (cond != null && !cond.IsTargetConditionMet(data, this, caster, target_card))
                    return false;
            }
            return true;
        }

        // 判断施法是否可以对卡牌目标生效（额外判断隐身与法术免疫）
        public bool CanTarget(Game data, Card caster, Card target)
        {
            if (target.HasStatus(StatusType.Stealth))
                return false; // 隐身不可选

            if (target.HasStatus(StatusType.SpellImmunity))
                return false; // 法术免疫不可选

            return AreTargetConditionsMet(data, caster, target);
        }

        // 判断施法是否可以对玩家目标生效
        public bool CanTarget(Game data, Card caster, Player target)
        {
            return AreTargetConditionsMet(data, caster, target);
        }

        // 判断施法是否可以对格子目标生效
        public bool CanTarget(Game data, Card caster, Slot target)
        {
            return AreTargetConditionsMet(data, caster, target);
        }

        // 检查经过筛选后的目标数组中是否仍包含该卡牌（用于CardSelector）
        public bool IsCardSelectionValid(Game data, Card caster, Card target, ListSwap<Card> card_array = null)
        {
            List<Card> targets = GetCardTargets(data, caster, card_array);
            return targets.Contains(target); // 卡牌仍在数组中
        }

        // 执行技能效果（无特定目标）
        public void DoEffects(GameLogic logic, Card caster)
        {
            foreach(EffectData effect in effects)
                effect?.DoEffect(logic, this, caster);
        }

        // 执行技能效果（卡牌目标）
        public void DoEffects(GameLogic logic, Card caster, Card target)
        {
            foreach (EffectData effect in effects)
                effect?.DoEffect(logic, this, caster, target);
            foreach(StatusData stat in status)
                target.AddStatus(stat, value, duration);
        }

        // 执行技能效果（玩家目标）
        public void DoEffects(GameLogic logic, Card caster, Player target)
        {
            foreach (EffectData effect in effects)
                effect?.DoEffect(logic, this, caster, target);
            foreach (StatusData stat in status)
                target.AddStatus(stat, value, duration);
        }

        // 执行技能效果（格子目标）
        public void DoEffects(GameLogic logic, Card caster, Slot target)
        {
            foreach (EffectData effect in effects)
                effect?.DoEffect(logic, this, caster, target);
        }

        // 执行技能效果（卡牌数据目标）
        public void DoEffects(GameLogic logic, Card caster, CardData target)
        {
            foreach (EffectData effect in effects)
                effect?.DoEffect(logic, this, caster, target);
        }

        // 执行持续技能效果（卡牌目标）
        public void DoOngoingEffects(GameLogic logic, Card caster, Card target)
        {
            foreach (EffectData effect in effects)
                effect?.DoOngoingEffect(logic, this, caster, target);
            foreach (StatusData stat in status)
                target.AddOngoingStatus(stat, value);
        }

        // 执行持续技能效果（玩家目标）
        public void DoOngoingEffects(GameLogic logic, Card caster, Player target)
        {
            foreach (EffectData effect in effects)
                effect?.DoOngoingEffect(logic, this, caster, target);
            foreach (StatusData stat in status)
                target.AddOngoingStatus(stat, value);
        }

        // 检查技能是否包含某种类型的效果
        public bool HasEffect<T>() where T : EffectData
        {
            foreach (EffectData eff in effects)
            {
                if (eff != null && eff is T)
                    return true;
            }
            return false;
        }


        public bool HasStatus(StatusType type)
        {
            // 检查技能是否包含指定类型的状态
            foreach (StatusData sta in status)
            {
                if (sta != null && sta.effect == type)
                    return true;
            }
            return false;
        }

        public int GetDamage()
        {
            // 计算技能造成的总伤害值（只统计EffectDamage类型的效果）
            int damage = 0;
            foreach (EffectData eff in effects)
            {
                if (eff != null && eff is EffectDamage)
                {
                    damage += this.value;
                }
            }
            return damage;
        }

        private void AddValidCards(Game data, Card caster, List<Card> source, List<Card> targets)
        {
            // 从指定卡牌列表中添加符合条件的卡牌到目标列表
            foreach (Card card in source)
            {
                if (AreTargetConditionsMet(data, caster, card))
                    targets.Add(card);
            }
        }

        // 返回卡牌目标列表，memory_array用于内存优化，避免重复分配
        public List<Card> GetCardTargets(Game data, Card caster, ListSwap<Card> memory_array = null)
        {
            if (memory_array == null)
                memory_array = new ListSwap<Card>(); // 如果未传入，创建新的ListSwap对象（较慢）

            List<Card> targets = memory_array.Get();

            // 目标为自身
            if (target == AbilityTarget.Self)
            {
                if (AreTargetConditionsMet(data, caster, caster))
                    targets.Add(caster);
            }

            // 目标为全场卡牌或选择目标
            if (target == AbilityTarget.AllCardsBoard || target == AbilityTarget.SelectTarget)
            {
                foreach (Player player in data.players)
                {
                    foreach (Card card in player.cards_board)
                    {
                        if (AreTargetConditionsMet(data, caster, card))
                            targets.Add(card);
                    }
                }
            }

            // 目标为手牌中的所有卡
            if (target == AbilityTarget.AllCardsHand)
            {
                foreach (Player player in data.players)
                {
                    foreach (Card card in player.cards_hand)
                    {
                        if (AreTargetConditionsMet(data, caster, card))
                            targets.Add(card);
                    }
                }
            }

            // 目标为所有牌堆的卡牌（包括CardSelector）
            if (target == AbilityTarget.AllCardsAllPiles || target == AbilityTarget.CardSelector)
            {
                foreach (Player player in data.players)
                {
                    AddValidCards(data, caster, player.cards_deck, targets);
                    AddValidCards(data, caster, player.cards_discard, targets);
                    AddValidCards(data, caster, player.cards_hand, targets);
                    AddValidCards(data, caster, player.cards_secret, targets);
                    AddValidCards(data, caster, player.cards_board, targets);
                    AddValidCards(data, caster, player.cards_equip, targets);
                    AddValidCards(data, caster, player.cards_temp, targets);
                }
            }

            // 特殊目标：最近上场的卡牌
            if (target == AbilityTarget.LastPlayed)
            {
                Card target = data.GetCard(data.last_played);
                if (target != null && AreTargetConditionsMet(data, caster, target))
                    targets.Add(target);
            }

            // 特殊目标：最近被摧毁的卡牌
            if (target == AbilityTarget.LastDestroyed)
            {
                Card target = data.GetCard(data.last_destroyed);
                if (target != null && AreTargetConditionsMet(data, caster, target))
                    targets.Add(target);
            }

            // 特殊目标：最近被选中的目标
            if (target == AbilityTarget.LastTargeted)
            {
                Card target = data.GetCard(data.last_target);
                if (target != null && AreTargetConditionsMet(data, caster, target))
                    targets.Add(target);
            }

            // 特殊目标：最近召唤或创建的卡牌
            if (target == AbilityTarget.LastSummoned)
            {
                Card target = data.GetCard(data.last_summoned);
                if (target != null && AreTargetConditionsMet(data, caster, target))
                    targets.Add(target);
            }

            // 特殊目标：触发技能的卡牌
            if (target == AbilityTarget.AbilityTriggerer)
            {
                Card target = data.GetCard(data.ability_triggerer);
                if (target != null && AreTargetConditionsMet(data, caster, target))
                    targets.Add(target);
            }

            // 特殊目标：装备或被装备卡牌
            if (target == AbilityTarget.EquippedCard)
            {
                if (caster.CardData.IsEquipment())
                {
                    // 如果当前卡是装备，获取持有者
                    Player player = data.GetPlayer(caster.player_id);
                    Card target = player.GetBearerCard(caster);
                    if (target != null && AreTargetConditionsMet(data, caster, target))
                        targets.Add(target);
                }
                else if(caster.equipped_uid != null)
                {
                    // 获取装备的目标卡
                    Card target = data.GetCard(caster.equipped_uid);
                    if (target != null && AreTargetConditionsMet(data, caster, target))
                        targets.Add(target);
                }
            }

            // 对目标应用过滤器（如随机、条件筛选等）
            if (filters_target != null && targets.Count > 0)
            {
                foreach (FilterData filter in filters_target)
                {
                    if (filter != null)
                        targets = filter.FilterTargets(data, this, caster, targets, memory_array.GetOther(targets));
                }
            }

            return targets;
        }

        // 返回玩家目标列表
        public List<Player> GetPlayerTargets(Game data, Card caster, ListSwap<Player> memory_array = null)
        {
            if (memory_array == null)
                memory_array = new ListSwap<Player>();

            List<Player> targets = memory_array.Get();

            // 自己
            if (target == AbilityTarget.PlayerSelf)
            {
                Player player = data.GetPlayer(caster.player_id);
                targets.Add(player);
            }
            // 对手
            else if (target == AbilityTarget.PlayerOpponent)
            {
                for (int tp = 0; tp < data.players.Length; tp++)
                {
                    if (tp != caster.player_id)
                    {
                        Player oplayer = data.players[tp];
                        targets.Add(oplayer);
                    }
                }
            }
            // 全体玩家
            else if (target == AbilityTarget.AllPlayers)
            {
                foreach (Player player in data.players)
                {
                    if (AreTargetConditionsMet(data, caster, player))
                        targets.Add(player);
                }
            }

            // 应用过滤器
            if (filters_target != null && targets.Count > 0)
            {
                foreach (FilterData filter in filters_target)
                {
                    if (filter != null)
                        targets = filter.FilterTargets(data, this, caster, targets, memory_array.GetOther(targets));
                }
            }

            return targets;
        }

        // 返回格子目标列表
        public List<Slot> GetSlotTargets(Game data, Card caster, ListSwap<Slot> memory_array = null)
        {
            if (memory_array == null)
                memory_array = new ListSwap<Slot>();

            List<Slot> targets = memory_array.Get();

            if (target == AbilityTarget.AllSlots)
            {
                List<Slot> slots = Slot.GetAll();
                foreach (Slot slot in slots)
                {
                    if (AreTargetConditionsMet(data, caster, slot))
                        targets.Add(slot);
                }
            }

            // 应用过滤器
            if (filters_target != null && targets.Count > 0)
            {
                foreach (FilterData filter in filters_target)
                {
                    if (filter != null)
                        targets = filter.FilterTargets(data, this, caster, targets, memory_array.GetOther(targets));
                }
            }

            return targets;
        }

        // 返回卡牌数据目标列表（用于创建卡牌或全局操作）
        public List<CardData> GetCardDataTargets(Game data, Card caster, ListSwap<CardData> memory_array = null)
        {
            if (memory_array == null)
                memory_array = new ListSwap<CardData>();

            List<CardData> targets = memory_array.Get();

            if (target == AbilityTarget.AllCardData)
            {
                foreach (CardData card in CardData.GetAll())
                {
                    if (AreTargetConditionsMet(data, caster, card))
                        targets.Add(card);
                }
            }

            // 应用过滤器
            if (filters_target != null && targets.Count > 0)
            {
                foreach (FilterData filter in filters_target)
                {
                    if (filter != null)
                        targets = filter.FilterTargets(data, this, caster, targets, memory_array.GetOther(targets));
                }
            }

            return targets;
        }


        // 检查是否存在有效目标，如果没有，AI将不会尝试施放激活技能
        public bool HasValidSelectTarget(Game game_data, Card caster)
        {
            // 如果目标类型是需要玩家选择的卡牌
            if (target == AbilityTarget.SelectTarget)
            {
                if (HasValidBoardCardTarget(game_data, caster))
                    return true;
                if (HasValidPlayerTarget(game_data, caster))
                    return true;
                if (HasValidSlotTarget(game_data, caster))
                    return true;
                return false; // 没有任何有效目标
            }

            // 如果目标类型是卡牌选择器（CardSelector），检查全牌堆卡牌是否有有效目标
            if (target == AbilityTarget.CardSelector)
            {
                if (HasValidCardTarget(game_data, caster))
                    return true;
                return false;
            }

            // 如果目标类型是选择链（ChoiceSelector），检查连锁技能是否有触发条件满足
            if (target == AbilityTarget.ChoiceSelector)
            {
                foreach (AbilityData choice in chain_abilities)
                {
                    if(choice.AreTriggerConditionsMet(game_data, caster))
                        return true;
                }
                return false;
            }

            return true; // 如果不是选择类型，默认视为有效
        }

        // 检查是否存在有效的场上卡牌目标
        public bool HasValidBoardCardTarget(Game game_data, Card caster)
        {
            for (int p = 0; p < game_data.players.Length; p++)
            {
                Player player = game_data.players[p];
                for (int c = 0; c < player.cards_board.Count; c++)
                {
                    Card card = player.cards_board[c];
                    if (CanTarget(game_data, caster, card))
                        return true;
                }
            }
            return false;
        }

        // 检查是否存在有效的所有牌堆中的卡牌目标
        public bool HasValidCardTarget(Game game_data, Card caster)
        {
            for (int p = 0; p < game_data.players.Length; p++)
            {
                Player player = game_data.players[p];
                bool v1 = HasValidCardTarget(game_data, caster, player.cards_deck);
                bool v2 = HasValidCardTarget(game_data, caster, player.cards_discard);
                bool v3 = HasValidCardTarget(game_data, caster, player.cards_hand);
                bool v4 = HasValidCardTarget(game_data, caster, player.cards_board);
                bool v5 = HasValidCardTarget(game_data, caster, player.cards_equip);
                bool v6 = HasValidCardTarget(game_data, caster, player.cards_secret);
                bool v7 = HasValidCardTarget(game_data, caster, player.cards_temp);
                if (v1 || v2 || v3 || v4 || v5 || v6 || v7)
                    return true;
            }
            return false;
        }

        // 检查指定卡牌列表中是否存在符合目标条件的卡牌
        public bool HasValidCardTarget(Game game_data, Card caster, List<Card> list)
        {
            for (int c = 0; c < list.Count; c++)
            {
                Card card = list[c];
                if (AreTargetConditionsMet(game_data, caster, card))
                    return true;
            }
            return false;
        }

        // 检查是否存在有效的玩家目标
        public bool HasValidPlayerTarget(Game game_data, Card caster)
        {
            for (int p = 0; p < game_data.players.Length; p++)
            {
                Player player = game_data.players[p];
                if (CanTarget(game_data, caster, player))
                    return true;
            }
            return false;
        }

        // 检查是否存在有效的格子目标
        public bool HasValidSlotTarget(Game game_data, Card caster)
        {
            foreach (Slot slot in Slot.GetAll())
            {
                if (CanTarget(game_data, caster, slot))
                    return true;
            }
            return false;
        }

        // 判断技能是否是需要选择目标的类型
        public bool IsSelector()
        {
            return target == AbilityTarget.SelectTarget || 
                   target == AbilityTarget.CardSelector || 
                   target == AbilityTarget.ChoiceSelector;
        }

        // 根据ID获取AbilityData对象
        public static AbilityData Get(string id)
        {
            if (id == null)
                return null;
            bool success = ability_dict.TryGetValue(id, out AbilityData ability);
            if (success)
                return ability;
            return null;
        }

        // 获取所有AbilityData列表
        public static List<AbilityData> GetAll()
        {
            return ability_list;
        }

    }


    // 技能触发时机枚举
    public enum AbilityTrigger
    {
        None = 0,           // 无触发

        Ongoing = 2,        // 持续生效（始终激活，但并非所有效果都适用）
        Activate = 5,       // 主动技能（需要玩家操作或消耗行动）

        OnPlay = 10,        // 当该卡牌被使用时触发
        OnPlayOther = 12,   // 当其他卡牌被使用时触发

        StartOfTurn = 20,   // 回合开始时触发
        EndOfTurn = 22,     // 回合结束时触发

        OnBeforeAttack = 30, // 攻击前触发（在造成伤害前）
        OnAfterAttack = 31,  // 攻击后触发（造成伤害后，如果目标还存活）
        OnBeforeDefend = 32, // 防御前触发（被攻击前，计算伤害前）
        OnAfterDefend = 33,  // 防御后触发（被攻击后，如果自身仍存活）
        OnKill = 35,         // 在攻击过程中击杀其他卡牌时触发

        OnDeath = 40,        // 卡牌死亡时触发
        OnDeathOther = 42,   // 其他卡牌死亡时触发
    }

    // 技能目标类型枚举
    public enum AbilityTarget
    {
        None = 0,           // 无目标
        Self = 1,           // 自己

        PlayerSelf = 4,     // 自己的玩家对象
        PlayerOpponent = 5, // 对手的玩家对象
        AllPlayers = 7,     // 所有玩家对象

        AllCardsBoard = 10,     // 所有场上卡牌
        AllCardsHand = 11,      // 所有手牌
        AllCardsAllPiles = 12,  // 所有牌堆中的卡牌（手牌、牌库、弃牌堆、场上等）
        AllSlots = 15,          // 所有格子（Board Slot）
        AllCardData = 17,       // 所有卡牌数据，仅用于创建卡牌效果

        PlayTarget = 20,        // 在施放技能时选择的目标（仅限法术类）
        AbilityTriggerer = 25,  // 触发技能的卡牌（如陷阱触发者）
        EquippedCard = 27,      // 装备目标：如果是装备卡，获取携带者；如果是角色卡，获取装备的道具

        SelectTarget = 30,      // 玩家选择目标：可选择场上卡牌、玩家或格子
        CardSelector = 40,      // 卡牌选择器菜单（可从多个牌堆选择）
        ChoiceSelector = 50,    // 选择链菜单（技能连锁选择）

        LastPlayed = 70,        // 最近被使用的卡牌
        LastTargeted = 72,      // 最近被技能指定的卡牌
        LastDestroyed = 74,     // 最近被消灭的卡牌
        LastSummoned = 77,      // 最近被召唤或创建的卡牌
    }


}

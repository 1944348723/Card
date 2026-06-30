using System.Collections.Generic;

namespace TcgEngine
{
    // 包含游戏玩法的所有状态数据（在网络中同步）
    [System.Serializable]
    public class Game
    {
        public string game_uid;           // 游戏唯一ID
        public GameSettings settings;     // 游戏设置（规则、玩家数量等）

        // 游戏状态
        public int first_player = 0;      // 先手玩家ID
        public int current_player = 0;    // 当前行动玩家ID
        public int turn_count = 0;        // 当前回合数
        public float turn_timer = 0f;     // 回合计时器

        public GameState state = GameState.Connecting; // 当前游戏状态
        public GamePhase phase = GamePhase.None;       // 当前游戏阶段

        // 玩家
        public Player[] players;          // 玩家数组

        // 选择器相关（用于技能/法术选择等）
        public SelectorType selector = SelectorType.None;  // 当前选择器类型
        public int selector_player_id = 0;                 // 当前选择玩家ID
        public string selector_ability_id;                // 当前选择的技能ID
        public string selector_caster_uid;                // 当前施法卡牌UID

        // 其他引用值
        public string last_played;         // 最近打出的卡牌UID
        public string last_target;         // 最近目标UID
        public string last_destroyed;      // 最近被摧毁的卡牌UID
        public string last_summoned;       // 最近召唤的卡牌UID
        public string ability_triggerer;   // 技能触发者UID
        public int rolled_value;           // 骰子结果
        public int selected_value;         // 玩家选择的值（比如选择器）

        // 其他引用集合
        public HashSet<string> ability_played = new(); // 已触发技能集合
        public HashSet<string> cards_attacked = new(); // 已攻击卡牌集合

        public Game() { }

        // 构造函数：初始化游戏UID和玩家
        public Game(string uid, int nb_players)
        {
            this.game_uid = uid;
            players = new Player[nb_players];
            for (int i = 0; i < nb_players; i++)
            {
                players[i] = new Player(i);
            }
            settings = GameSettings.Default;
        }

        // 判断是否所有玩家准备就绪
        public virtual bool AreAllPlayersReady()
        {
            foreach (Player player in players)
            {
                if (!player.IsReady())
                {
                    return false;
                }
            }
            return true;
        }

        // 判断是否所有玩家已连接
        public virtual bool AreAllPlayersConnected()
        {
            foreach (Player player in players)
            {
                if (!player.IsConnected())
                {
                    return false;
                }
            }
            return true;
        }

        // 检查是否轮到玩家行动（包括常规操作和选择器）
        public virtual bool IsPlayerTurn(Player player)
        {
            return IsPlayerActionTurn(player) || IsPlayerSelectorTurn(player);
        }

        // 检查玩家是否轮到常规操作（打牌、攻击、技能）
        public virtual bool IsPlayerActionTurn(Player player)
        {
            return player != null && current_player == player.player_id 
                && state == GameState.Play && phase == GamePhase.Main && selector == SelectorType.None;
        }

        // 检查玩家是否轮到选择器操作（技能目标选择等）
        public virtual bool IsPlayerSelectorTurn(Player player)
        {
            return player != null && selector_player_id == player.player_id 
                && state == GameState.Play && phase == GamePhase.Main && selector != SelectorType.None;
        }

        // 检查玩家是否轮到换牌（Mulligan）
        public virtual bool IsPlayerMulliganTurn(Player player)
        {
            return phase == GamePhase.Mulligan && !player.ready;
        }

        // 检查玩家是否可以打出卡牌到指定槽位
        public virtual bool CanPlayCard(Card card, Slot slot, bool skip_cost = false)
        {
            if (card == null) return false;

            Player player = GetPlayer(card.player_id);

            if (!player.HasCardInHand(card))    return false;
            // 法力不足
            if (!skip_cost && !player.CanPayMana(card)) return false;
            // AI不能在0法力打X-cost卡
            if (player.is_ai && card.CardData.IsDynamicManaCost() && player.mana == 0)  return false; 

            // 根据卡牌类型判断槽位是否合法
            if (card.CardData.IsBoardCard())
            {
                return slot.IsBoardSlot() && !HasCardOnSlot(slot) && slot.BelongsToPlayer(card.player_id);
            }
            if (card.CardData.IsEquipment())
            {
                if (!slot.IsBoardSlot())     return false;

                Card target = GetSlotCard(slot);
                bool isSameTeamCharacter = target != null
                                        && target.CardData.type == CardType.Character
                                        && target.player_id == card.player_id;
                return isSameTeamCharacter;
            }
            if (card.CardData.IsRequireTargetSpell())
            {
                return IsPlayTargetValid(card, slot);
            }
            if (card.CardData.type == CardType.Spell)
            {
                return CanAnyPlayAbilityTrigger(card); // 检查法术的OnPlay技能触发条件
            }
            return true;
        }

        // 检查卡牌是否允许移动到指定槽位
        public virtual bool CanMoveCard(Card card, Slot slot, bool skip_cost = false)
        {
            if (card == null || !slot.IsBoardSlot())
                return false;

            if (!IsOnBoard(card))
                return false; // 只能移动已在场上的卡牌

            if (!card.CanMove(skip_cost))
                return false; // 卡牌不能移动

            if (!slot.BelongsToPlayer(card.player_id))
                return false; // 不能移动到敌方槽位

            if (card.slot == slot)
                return false; // 不能移动到原位置

            Card slot_card = GetSlotCard(slot);
            if (slot_card != null)
                return false; // 目标槽已被占用

            return true;
        }

        // 检查卡牌是否允许攻击玩家
        public virtual bool CanAttackTarget(Card attacker, Player target, bool skip_cost = false)
        {
            if(attacker == null || target == null)
                return false;

            if (!attacker.CanAttack(skip_cost))
                return false; // 卡牌不能攻击

            if (attacker.player_id == target.player_id)
                return false; // 不能攻击己方玩家

            if (!IsOnBoard(attacker) || !attacker.CardData.IsCharacter())
                return false; // 攻击者必须是场上角色

            if (target.HasStatus(StatusType.Protected) && !attacker.HasStatus(StatusType.Flying))
                return false; // 受保护状态阻挡攻击

            return true;
        }

        // 检查卡牌是否允许攻击另一张卡
        public virtual bool CanAttackTarget(Card attacker, Card target, bool skip_cost = false)
        {
            if (attacker == null || target == null)
                return false;

            if (!attacker.CanAttack(skip_cost))
                return false; // 卡牌不能攻击

            if (attacker.player_id == target.player_id)
                return false; // 不能攻击己方卡牌

            if (!IsOnBoard(attacker) || !IsOnBoard(target))
                return false; // 攻击双方必须在场

            if (!attacker.CardData.IsCharacter() || !target.CardData.IsBoardCard())
                return false; // 只有角色可以攻击卡牌

            if (target.HasStatus(StatusType.Stealth))
                return false; // 潜行状态不可被攻击

            if (target.HasStatus(StatusType.Protected) && !attacker.HasStatus(StatusType.Flying))
                return false; // 邻近保护状态阻挡攻击

            return true;
        }

        // 检查卡牌是否可以施放技能（主动技能）
        public virtual bool CanCastAbility(Card card, AbilityData ability)
        {
            if (ability == null || card == null || !card.CanDoActivatedAbilities())
                return false; // 卡牌不能施放

            if (ability.trigger != AbilityTrigger.Activate)
                return false; // 非主动技能

            Player player = GetPlayer(card.player_id);
            if (!player.CanPayAbility(card, ability))
                return false; // 法力不足

            if (!ability.AreTriggerConditionsMet(this, card))
                return false; // 条件未满足

            return true;
        }

        // 检查玩家选择技能是否可用（选择器）
        public virtual bool CanSelectAbility(Card card, AbilityData ability)
        {
            if (ability == null || card == null || !card.CanDoAbilities())
                return false; // 卡牌不能施放

            Player player = GetPlayer(card.player_id);
            if (!player.CanPayAbility(card, ability))
                return false; // 法力不足

            if (!ability.AreTriggerConditionsMet(this, card))
                return false; // 条件未满足

            return true;
        }

        // 检查卡牌的OnPlay技能是否触发
        public virtual bool CanAnyPlayAbilityTrigger(Card card)
        {
            if (card == null)
                return false;
            if (card.CardData.IsDynamicManaCost())
                return true; // X-cost卡牌不受限制

            foreach (AbilityData ability in card.GetAbilities())
            {
                if (ability.trigger == AbilityTrigger.OnPlay && ability.AreTriggerConditionsMet(this, card))
                    return true;
            }
            return false;
        }

        // 检查法术或技能的目标是否合法（拖动到玩家）
        public virtual bool IsPlayTargetValid(Card caster, Player target)
        {
            if (caster == null || target == null)
                return false;

            foreach (AbilityData ability in caster.GetAbilities())
            {
                if (ability && ability.trigger == AbilityTrigger.OnPlay && ability.target == AbilityTarget.PlayTarget)
                {
                    if (!ability.CanTarget(this, caster, target))
                        return false;
                }
            }
            return true;
        }

        // 检查卡牌是否可以作为目标被施法（用于需要拖到另一张卡的法术）
        public virtual bool IsPlayTargetValid(Card caster, Card target)
        {
            if (caster == null || target == null)
                return false;

            // 遍历卡牌的OnPlay技能，检查目标是否合法
            foreach (AbilityData ability in caster.GetAbilities())
            {
                if (ability && ability.trigger == AbilityTrigger.OnPlay && ability.target == AbilityTarget.PlayTarget)
                {
                    if (!ability.CanTarget(this, caster, target))
                        return false;
                }
            }
            return true;
        }

        // 检查槽位是否可以作为目标被施法（用于需要拖到槽位的法术）
        public virtual bool IsPlayTargetValid(Card caster, Slot target)
        {
            if (caster == null)
                return false;

            if (target.IsPlayerSlot())
                return IsPlayTargetValid(caster, GetPlayer(target.p)); // 如果槽位指向玩家，检查目标玩家

            Card slot_card = GetSlotCard(target);
            if (slot_card != null)
                return IsPlayTargetValid(caster, slot_card); // 槽位上有卡牌，则检查该卡牌是否可作为目标

            // 遍历卡牌的OnPlay技能，检查槽位目标是否合法
            foreach (AbilityData ability in caster.GetAbilities())
            {
                if (ability && ability.trigger == AbilityTrigger.OnPlay && ability.target == AbilityTarget.PlayTarget)
                {
                    if (!ability.CanTarget(this, caster, target))
                        return false;
                }
            }
            return true;
        }

        // 根据玩家ID获取玩家对象
        public Player GetPlayer(int id)
        {
            if (id >= 0 && id < players.Length)
                return players[id];
            return null;
        }

        // 获取当前行动的玩家
        public Player GetActivePlayer()
        {
            return GetPlayer(current_player);
        }

        // 获取指定玩家的对手玩家
        public Player GetOpponentPlayer(int id)
        {
            int oid = id == 0 ? 1 : 0;
            return GetPlayer(oid);
        }

        // 根据UID获取玩家拥有的任意卡牌
        public Card GetCard(string card_uid)
        {
            foreach (Player player in players)
            {
                Card acard = player.GetCard(card_uid);
                if (acard != null)
                    return acard;
            }
            return null;
        }

        // 根据UID获取场上的卡牌
        public Card GetBoardCard(string card_uid)
        {
            foreach (Player player in players)
            {
                foreach (Card card in player.cards_board)
                {
                    if (card != null && card.uid == card_uid)
                        return card;
                }
            }
            return null;
        }

        // 根据UID获取装备卡
        public Card GetEquipCard(string card_uid)
        {
            foreach (Player player in players)
            {
                foreach (Card card in player.cards_equip)
                {
                    if (card != null && card.uid == card_uid)
                        return card;
                }
            }
            return null;
        }

        // 根据UID获取手牌
        public Card GetHandCard(string card_uid)
        {
            foreach (Player player in players)
            {
                foreach (Card card in player.cards_hand)
                {
                    if (card != null && card.uid == card_uid)
                        return card;
                }
            }
            return null;
        }

        // 根据UID获取牌库中的卡牌
        public Card GetDeckCard(string card_uid)
        {
            foreach (Player player in players)
            {
                foreach (Card card in player.cards_deck)
                {
                    if (card != null && card.uid == card_uid)
                        return card;
                }
            }
            return null;
        }

        // 根据UID获取弃牌堆中的卡牌
        public Card GetDiscardCard(string card_uid)
        {
            foreach (Player player in players)
            {
                foreach (Card card in player.cards_discard)
                {
                    if (card != null && card.uid == card_uid)
                        return card;
                }
            }
            return null;
        }

        // 根据UID获取秘密卡
        public Card GetSecretCard(string card_uid)
        {
            foreach (Player player in players)
            {
                foreach (Card card in player.cards_secret)
                {
                    if (card != null && card.uid == card_uid)
                        return card;
                }
            }
            return null;
        }

        // 根据UID获取临时卡
        public Card GetTempCard(string card_uid)
        {
            foreach (Player player in players)
            {
                foreach (Card card in player.cards_temp)
                {
                    if (card != null && card.uid == card_uid)
                        return card;
                }
            }
            return null;
        }

        // 根据槽位获取该槽上的卡牌
        public Card GetSlotCard(Slot slot)
        {
            foreach (Player player in players)
            {
                foreach (Card card in player.cards_board)
                {
                    if (card != null && card.slot == slot)
                        return card;
                }
            }
            return null;
        }

        
        // 随机获取一个玩家
        public virtual Player GetRandomPlayer(System.Random rand)
        {
            Player player = GetPlayer(rand.NextDouble() < 0.5 ? 1 : 0); // 以50%概率选择玩家0或1
            return player;
        }

        // 随机获取场上的一张卡牌
        public virtual Card GetRandomBoardCard(System.Random rand)
        {
            Player player = GetRandomPlayer(rand);
            return player.GetRandomCard(player.cards_board, rand); // 从该玩家场上的卡牌中随机选择
        }

        // 随机获取一个槽位
        public virtual Slot GetRandomSlot(System.Random rand)
        {
            Player player = GetRandomPlayer(rand);
            return player.GetRandomSlot(rand); // 从该玩家可用槽位中随机选择
        }

        // 判断卡牌是否在手牌中
        public bool IsInHand(Card card)
        {
            return card != null && GetHandCard(card.uid) != null;
        }

        // 判断卡牌是否在场上
        public bool IsOnBoard(Card card)
        {
            return card != null && GetBoardCard(card.uid) != null;
        }

        // 判断卡牌是否被装备
        public bool IsEquipped(Card card)
        {
            return card != null && GetEquipCard(card.uid) != null;
        }

        // 判断卡牌是否在牌库中
        public bool IsInDeck(Card card)
        {
            return card != null && GetDeckCard(card.uid) != null;
        }

        // 判断卡牌是否在弃牌堆中
        public bool IsInDiscard(Card card)
        {
            return card != null && GetDiscardCard(card.uid) != null;
        }

        // 判断卡牌是否是秘密卡
        public bool IsInSecret(Card card)
        {
            return card != null && GetSecretCard(card.uid) != null;
        }

        // 判断卡牌是否是临时卡
        public bool IsInTemp(Card card)
        {
            return card != null && GetTempCard(card.uid) != null;
        }

        // 判断槽位上是否有卡牌
        public bool HasCardOnSlot(Slot slot)
        {
            return GetSlotCard(slot) != null;
        }

        // 判断游戏是否已经开始
        public bool HasStarted()
        {
            return state != GameState.Connecting;
        }

        // 判断游戏是否已经结束
        public bool HasEnded()
        {
            return state == GameState.GameEnded;
        }

        // 克隆游戏对象并生成新实例（速度较慢）
        public static Game CloneNew(Game source)
        {
            Game game = new Game();
            Clone(source, game); // 将source的数据复制到新对象
            return game;
        }

        // 将一个游戏对象的所有变量克隆到另一个对象，主要用于AI预测树
        public static void Clone(Game source, Game dest)
        {
            dest.game_uid = source.game_uid;
            dest.settings = source.settings;

            dest.first_player = source.first_player;
            dest.current_player = source.current_player;
            dest.turn_count = source.turn_count;
            dest.turn_timer = source.turn_timer;
            dest.state = source.state;
            dest.phase = source.phase;

            // 初始化玩家数组
            if (dest.players == null)
            {
                dest.players = new Player[source.players.Length];
                for(int i=0; i< source.players.Length; i++)
                    dest.players[i] = new Player(i);
            }

            // 克隆玩家对象
            for (int i = 0; i < source.players.Length; i++)
                Player.Clone(source.players[i], dest.players[i]);

            dest.selector = source.selector;
            dest.selector_player_id = source.selector_player_id;
            dest.selector_caster_uid = source.selector_caster_uid;
            dest.selector_ability_id = source.selector_ability_id;

            dest.last_destroyed = source.last_destroyed;
            dest.last_played = source.last_played;
            dest.last_target = source.last_target;
            dest.last_summoned = source.last_summoned;
            dest.ability_triggerer = source.ability_triggerer;
            dest.rolled_value = source.rolled_value;
            dest.selected_value = source.selected_value;

            // 克隆HashSet
            CloneHash(source.ability_played, dest.ability_played);
            CloneHash(source.cards_attacked, dest.cards_attacked);
        }

        // 克隆HashSet内容
        public static void CloneHash(HashSet<string> source, HashSet<string> dest)
        {
            dest.Clear();
            foreach (string str in source)
                dest.Add(str);
        }
    }

    // 游戏状态枚举
    [System.Serializable]
    public enum GameState
    {
        Connecting = 0, // 玩家尚未连接
        Play = 20,      // 游戏进行中
        GameEnded = 99, // 游戏结束
    }

    // 游戏阶段枚举
    [System.Serializable]
    public enum GamePhase
    {
        None = 0,
        Mulligan = 5,   // 握手阶段/换牌阶段
        StartTurn = 10, // 回合开始阶段
        Main = 20,      // 主游戏阶段
        EndTurn = 30,   // 回合结束阶段
    }

    // 选择器类型枚举（用于选择目标、卡牌或其他操作）
    [System.Serializable]
    public enum SelectorType
    {
        None = 0,          // 无选择器
        SelectTarget = 10, // 选择目标
        SelectorCard = 20, // 选择卡牌
        SelectorChoice = 30,// 选择选项
        SelectorCost = 40, // 选择花费
    }
}
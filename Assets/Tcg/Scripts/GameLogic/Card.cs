using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    // 表示游戏中卡牌的当前状态（仅数据部分）
    [System.Serializable]
    public class Card
    {
        public string card_id; // 卡牌的类型 ID
        public string uid; // 卡牌的唯一实例 ID
        public int player_id; // 所属玩家 ID
        public string variant_id; // 卡牌变体 ID

        public Slot slot; // 卡牌所在槽位
        public bool exhausted; // 是否已疲惫（已行动）
        public int damage = 0; // 已受伤害

        public int mana = 0; // 基础法力值
        public int attack = 0; // 基础攻击力
        public int hp = 0; // 基础生命值

        public int mana_ongoing = 0; // 临时法力值增益/减益
        public int attack_ongoing = 0; // 临时攻击力增益/减益
        public int hp_ongoing = 0; // 临时生命值增益/减益

        public string equipped_uid = null; // 装备的卡牌 UID

        public List<CardTrait> traits = new List<CardTrait>(); // 卡牌基础特性列表
        public List<CardTrait> ongoing_traits = new List<CardTrait>(); // 临时特性列表

        public List<CardStatus> status = new List<CardStatus>(); // 基础状态效果列表
        public List<CardStatus> ongoing_status = new List<CardStatus>(); // 临时状态效果列表

        public List<string> abilities = new List<string>(); // 卡牌基础技能 ID 列表
        public List<string> abilities_ongoing = new List<string>(); // 临时技能 ID 列表

        // 非序列化字段，不会保存到数据中
        [System.NonSerialized] private int hash = 0;
        [System.NonSerialized] private CardData data = null; // 卡牌静态数据
        [System.NonSerialized] private VariantData vdata = null; // 卡牌变体数据
        [System.NonSerialized] private List<AbilityData> abilities_data = null; // 技能数据列表

        // 构造函数
        public Card(string card_id, string uid, int player_id) { this.card_id = card_id; this.uid = uid; this.player_id = player_id; }

        // 刷新卡牌状态，默认重置疲惫状态
        public virtual void Refresh() { exhausted = false; }

        // 清空临时效果
        public virtual void ClearOngoing() 
        { 
            ongoing_status.Clear(); 
            ongoing_traits.Clear(); 
            ClearOngoingAbility(); 
            attack_ongoing = 0; 
            hp_ongoing = 0; 
            mana_ongoing = 0; 
        }

        // 清空所有状态并重置卡牌为初始状态
        public virtual void Clear()
        {
            ClearOngoing(); 
            Refresh(); 
            damage = 0; 
            status.Clear(); 
            SetCard(CardData, VariantData); // 重置为初始数据
            equipped_uid = null;
        }

        // 获取总攻击力（基础 + 临时）
        public virtual int GetAttack() { return Mathf.Max(attack + attack_ongoing, 0); }

        // 获取当前生命值（基础 + 临时 - 伤害）
        public virtual int GetHP() { return Mathf.Max(hp + hp_ongoing - damage, 0); }

        // 获取当前生命值，可加偏移值
        public virtual int GetHP(int offset) { return Mathf.Max(hp + hp_ongoing - damage + offset, 0); }

        // 获取最大生命值（基础 + 临时）
        public virtual int GetHPMax() { return Mathf.Max(hp + hp_ongoing, 0); }

        // 获取总法力值（基础 + 临时）
        public virtual int GetMana() { return Mathf.Max(mana + mana_ongoing, 0); }

        // 设置卡牌数据和变体
        public virtual void SetCard(CardData icard, VariantData cvariant)
        {
            data = icard;
            card_id = icard.id;
            variant_id = cvariant.id;
            attack = icard.attack;
            hp = icard.hp;
            mana = icard.mana;
            SetTraits(icard); // 设置特性
            SetAbilities(icard); // 设置技能
        }

        // 初始化卡牌特性
        public void SetTraits(CardData icard)
        {
            traits.Clear();
            foreach (TraitData trait in icard.traits)
                SetTrait(trait.id, 0);
            if (icard.stats != null)
            {
                foreach (TraitStat stat in icard.stats)
                    SetTrait(stat.trait.id, stat.value);
            }
        }

        // 初始化卡牌技能
        public void SetAbilities(CardData icard)
        {
            abilities.Clear();
            abilities_ongoing.Clear();
            if (abilities_data != null)
                abilities_data.Clear();
            foreach (AbilityData ability in icard.abilities)
                AddAbility(ability);
        }
        
        //------ 自定义特性/状态方法 ---------

        // 设置特性值
        public void SetTrait(string id, int value)
        {
            CardTrait trait = GetTrait(id);
            if (trait != null)
            {
                trait.value = value;
            }
            else
            {
                trait = new CardTrait(id, value);
                traits.Add(trait);
            }
        }

        // 增加特性值
        public void AddTrait(string id, int value)
        {
            CardTrait trait = GetTrait(id);
            if (trait != null)
                trait.value += value;
            else
                SetTrait(id, value);
        }

        // 增加临时特性值
        public void AddOngoingTrait(string id, int value)
        {
            CardTrait trait = GetOngoingTrait(id);
            if (trait != null)
            {
                trait.value += value;
            }
            else
            {
                trait = new CardTrait(id, value);
                ongoing_traits.Add(trait);
            }
        }

        // 移除特性
        public void RemoveTrait(string id)
        {
            for (int i = traits.Count - 1; i >= 0; i--)
            {
                if (traits[i].id == id)
                    traits.RemoveAt(i);
            }
        }

        // 获取基础特性
        public CardTrait GetTrait(string id)
        {
            foreach (CardTrait trait in traits)
            {
                if (trait.id == id)
                    return trait;
            }
            return null;
        }

        // 获取临时特性
        public CardTrait GetOngoingTrait(string id)
        {
            foreach (CardTrait trait in ongoing_traits)
            {
                if (trait.id == id)
                    return trait;
            }
            return null;
        }

        // 获取特性值
        public int GetTraitValue(TraitData trait)
        {
            if (trait != null)
                return GetTraitValue(trait.id);
            return 0;
        }

        // 获取特性值（按 ID）
        public virtual int GetTraitValue(string id)
        {
            int val = 0;
            CardTrait stat1 = GetTrait(id);
            CardTrait stat2 = GetOngoingTrait(id);
            if (stat1 != null)
                val += stat1.value;
            if (stat2 != null)
                val += stat2.value;
            return val;
        }

        // 是否拥有特性
        public bool HasTrait(TraitData trait)
        {
            if (trait != null)
                return HasTrait(trait.id);
            return false;
        }

        public bool HasTrait(string id)
        {
            return GetTrait(id) != null || GetOngoingTrait(id) != null;
        }

        // 获取所有特性
        public List<CardTrait> GetAllTraits()
        {
            List<CardTrait> all_traits = new List<CardTrait>();
            all_traits.AddRange(traits);
            all_traits.AddRange(ongoing_traits);
            return all_traits;
        }
        
        // 兼容方法，将 traits 作为 stats 使用
        public void SetStat(string id, int value) => SetTrait(id, value);
        public void AddStat(string id, int value) => AddTrait(id, value);
        public void AddOngoingStat(string id, int value) => AddOngoingTrait(id, value);
        public void RemoveStat(string id) => RemoveTrait(id);
        public int GetStatValue(TraitData trait) => GetTraitValue(trait);
        public int GetStatValue(string id) => GetTraitValue(id);
        public bool HasStat(TraitData trait) => HasTrait(trait);
        public bool HasStat(string id) => HasTrait(id);
        public List<CardTrait> GetAllStats() => GetAllTraits();

        //------ 状态效果方法 ---------

        // 添加状态效果
        public void AddStatus(StatusData status, int value, int duration)
        {
            if (status != null)
                AddStatus(status.effect, value, duration);
        }

        // 添加持续状态效果
        public void AddOngoingStatus(StatusData status, int value)
        {
            if (status != null)
                AddOngoingStatus(status.effect, value);
        }

        // 添加状态效果（按类型）
        public void AddStatus(StatusType type, int value, int duration)
        {
            if (type != StatusType.None)
            {
                CardStatus status = GetStatus(type);
                if (status == null)
                {
                    status = new CardStatus(type, value, duration);
                    this.status.Add(status);
                }
                else
                {
                    status.value += value;
                    status.duration = Mathf.Max(status.duration, duration);
                    status.permanent = status.permanent || duration == 0;
                }
            }
        }


                // 添加临时状态效果
        public void AddOngoingStatus(StatusType type, int value)
        {
            if (type != StatusType.None)
            {
                CardStatus status = GetOngoingStatus(type);
                if (status == null)
                {
                    status = new CardStatus(type, value, 0); // 持续时间为0表示临时
                    ongoing_status.Add(status);
                }
                else
                {
                    status.value += value; // 已存在则增加值
                }
            }
        }

        // 移除指定类型的状态效果
        public void RemoveStatus(StatusType type)
        {
            for (int i = status.Count - 1; i >= 0; i--)
            {
                if (status[i].type == type)
                    status.RemoveAt(i);
            }
        }

        // 获取所有状态效果（基础 + 临时）
        public List<CardStatus> GetAllStatus()
        {
            List<CardStatus> all_status = new List<CardStatus>();
            all_status.AddRange(status);
            all_status.AddRange(ongoing_status);
            return all_status;
        }

        // 判断是否拥有某种状态效果
        public bool HasStatus(StatusType type)
        {
            return GetStatus(type) != null || GetOngoingStatus(type) != null;
        }

        // 获取基础状态效果
        public CardStatus GetStatus(StatusType type)
        {
            foreach (CardStatus status in status)
            {
                if (status.type == type)
                    return status;
            }
            return null;
        }

        // 获取临时状态效果
        public CardStatus GetOngoingStatus(StatusType type)
        {
            foreach (CardStatus status in ongoing_status)
            {
                if (status.type == type)
                    return status;
            }
            return null;
        }

        // 获取状态值（基础 + 临时）
        public virtual int GetStatusValue(StatusType type)
        {
            CardStatus status1 = GetStatus(type);
            CardStatus status2 = GetOngoingStatus(type);
            int v1 = status1 != null ? status1.value : 0;
            int v2 = status2 != null ? status2.value : 0;
            return v1 + v2;
        }

        // 减少状态持续时间，每回合调用
        public virtual void ReduceStatusDurations()
        {
            for (int i = status.Count - 1; i >= 0; i--)
            {
                if (!status[i].permanent) // 永久状态不减少
                {
                    status[i].duration -= 1;
                    if (status[i].duration <= 0)
                        status.RemoveAt(i); // 时间到则移除
                }
            }
        }

        //----- 技能方法 ------------

        // 添加基础技能
        public void AddAbility(AbilityData ability)
        {
            abilities.Add(ability.id);
            if (abilities_data != null)
                abilities_data.Add(ability);
        }

        // 移除基础技能
        public void RemoveAbility(AbilityData ability)
        {
            abilities.Remove(ability.id);
            if (abilities_data != null)
                abilities_data.Remove(ability);
        }

        // 添加临时技能
        public void AddOngoingAbility(AbilityData ability)
        {
            if (!abilities_ongoing.Contains(ability.id) && !abilities.Contains(ability.id))
            {
                abilities_ongoing.Add(ability.id);
                if (abilities_data != null)
                    abilities_data.Add(ability);
            }
        }

        // 清除所有临时技能
        public void ClearOngoingAbility()
        {
            if (abilities_data != null)
            {
                for (int i = abilities_data.Count - 1; i >= 0; i--)
                {
                    AbilityData ability = abilities_data[i];
                    if (abilities_ongoing.Contains(ability.id))
                        abilities_data.RemoveAt(i);
                }
            }

            abilities_ongoing.Clear();
        }

        // 按触发条件获取技能
        public AbilityData GetAbility(AbilityTrigger trigger)
        {
            foreach (AbilityData iability in GetAbilities())
            {
                if (iability.trigger == trigger)
                    return iability;
            }
            return null;
        }

        // 判断是否拥有指定技能
        public bool HasAbility(AbilityData ability)
        {
            foreach (AbilityData iability in GetAbilities())
            {
                if (iability.id == ability.id)
                    return true;
            }
            return false;
        }

        // 判断是否拥有指定触发条件的技能
        public bool HasAbility(AbilityTrigger trigger)
        {
            AbilityData iability = GetAbility(trigger);
            return iability != null;
        }

        // 判断是否拥有指定触发条件和目标的技能
        public bool HasAbility(AbilityTrigger trigger, AbilityTarget target)
        {
            foreach (AbilityData iability in GetAbilities())
            {
                if (iability.trigger == trigger && iability.target == target)
                    return true;
            }
            return false;
        }

        // 判断当前技能是否可用（满足触发条件）
        public bool HasActiveAbility(Game data, AbilityTrigger trigger)
        {
            AbilityData iability = GetAbility(trigger);
            if (iability != null && CanDoAbilities() && iability.AreTriggerConditionsMet(data, this))
                return true;
            return false;
        }

        // 判断技能条件是否满足
        public bool AreAbilityConditionsMet(AbilityTrigger ability_trigger, Game data, Card caster, Card triggerer)
        {
            foreach (AbilityData ability in GetAbilities())
            {
                if (ability && ability.trigger == ability_trigger && ability.AreTriggerConditionsMet(data, caster, triggerer))
                    return true;
            }
            return false;
        }

        // 获取所有技能数据（基础 + 临时）
        public List<AbilityData> GetAbilities()
        {
            // 初始化 abilities_data，因为网络传输后可能为空（无法序列化）
            if (abilities_data == null)
            {
                abilities_data = new List<AbilityData>(abilities.Count + abilities_ongoing.Count);
                for (int i = 0; i < abilities.Count; i++)
                    abilities_data.Add(AbilityData.Get(abilities[i]));
                for (int i = 0; i < abilities_ongoing.Count; i++)
                    abilities_data.Add(AbilityData.Get(abilities_ongoing[i]));
            }

            return abilities_data;
        }

        //---- 行动判定 ---------

        // 判断卡牌是否可以攻击
        public virtual bool CanAttack(bool skip_cost = false)
        {
            if (HasStatus(StatusType.Paralysed)) // 麻痹状态无法攻击
                return false;
            if (!skip_cost && exhausted) // 已疲惫无法攻击
                return false;
            return true;
        }

        // 判断卡牌是否可以移动
        public virtual bool CanMove(bool skip_cost = false)
        {
            // 在示例中，移动没有任何限制
            // if (HasStatusEffect(StatusEffect.Paralysed))
            //    return false; // 麻痹状态无法移动
            // if (!skip_cost && exhausted)
            //    return false; // 已疲惫无法行动
            return true; 
        }

        // 判断是否可以使用激活技能（主动技能）
        public virtual bool CanDoActivatedAbilities()
        {
            if (HasStatus(StatusType.Paralysed)) // 麻痹状态不可用
                return false;
            if (HasStatus(StatusType.Silenced)) // 沉默状态不可用
                return false;

            return true;
        }

        // 判断是否可以使用技能（包括被动技能）
        public virtual bool CanDoAbilities()
        {
            if (HasStatus(StatusType.Silenced)) // 沉默状态不可用
                return false;
            return true;
        }

        // 判断是否可以执行任何行动（攻击/移动/技能）
        public virtual bool CanDoAnyAction()
        {
            return CanAttack() || CanMove() || CanDoActivatedAbilities();
        }

        //---------------- 属性快捷访问 ----------------

        // 获取卡牌数据对象（优化缓存）
        public CardData CardData 
        { 
            get { 
                if(data == null || data.id != card_id)
                    data = CardData.Get(card_id); // 优化，存储以备将来使用
                return data;
            } 
        }

        // 获取卡牌变体数据对象（优化缓存）
        public VariantData VariantData
        {
            get
            {
                if (vdata == null || vdata.id != variant_id)
                    vdata = VariantData.Get(variant_id); // 优化，存储以备将来使用
                return vdata;
            }
        }

        public CardData Data => CardData; // 另一种访问方式

        // 获取卡牌唯一哈希值
        public int Hash
        {
            get {
                if (hash == 0)
                    hash = Mathf.Abs(uid.GetHashCode()); // 优化缓存
                return hash;
            }
        }

        //---------------- 静态创建/克隆方法 ----------------

        // 创建卡牌并自动生成 UID
        public static Card Create(CardData icard, VariantData ivariant, Player player)
        {
            return Create(icard, ivariant, player, GameTool.GenerateRandomID(11, 15));
        }

        // 创建卡牌并指定 UID
        public static Card Create(CardData icard, VariantData ivariant, Player player, string uid)
        {
            Card card = new Card(icard.id, uid, player.player_id);
            card.SetCard(icard, ivariant);
            player.cards_all[card.uid] = card; // 添加到玩家卡牌字典
            return card;
        }

        // 克隆一张卡牌，返回新对象
        public static Card CloneNew(Card source)
        {
            Card card = new Card(source.card_id, source.uid, source.player_id);
            Clone(source, card);
            return card;
        }

        // 克隆所有变量到另一张卡牌（主要用于 AI 预测）
        public static void Clone(Card source, Card dest)
        {
            dest.card_id = source.card_id;
            dest.uid = source.uid;
            dest.player_id = source.player_id;

            dest.variant_id = source.variant_id;
            dest.slot = source.slot;
            dest.exhausted = source.exhausted;
            dest.damage = source.damage;

            dest.attack = source.attack;
            dest.hp = source.hp;
            dest.mana = source.mana;

            dest.mana_ongoing = source.mana_ongoing;
            dest.attack_ongoing = source.attack_ongoing;
            dest.hp_ongoing = source.hp_ongoing;

            dest.equipped_uid = source.equipped_uid;

            // 克隆特性/状态/技能列表
            CardTrait.CloneList(source.traits, dest.traits);
            CardTrait.CloneList(source.ongoing_traits, dest.ongoing_traits);
            CardStatus.CloneList(source.status, dest.status);
            CardStatus.CloneList(source.ongoing_status, dest.ongoing_status);
            GameTool.CloneList(source.abilities, dest.abilities); 
            GameTool.CloneList(source.abilities_ongoing, dest.abilities_ongoing); 
            GameTool.CloneListRefNull(source.abilities_data, ref dest.abilities_data); 
            // AbilityData 仅引用，无需深拷贝
        }

        // 克隆可能为 null 的卡牌变量
        public static void CloneNull(Card source, ref Card dest)
        {
            if (source == null)
            {
                dest = null;
                return;
            }
            if (dest == null)
            {
                dest = CloneNew(source);
                return;
            }
            Clone(source, dest);
        }

        // 克隆字典（深度克隆每张卡牌）
        public static void CloneDict(Dictionary<string, Card> source, Dictionary<string, Card> dest)
        {
            foreach (KeyValuePair<string, Card> pair in source)
            {
                bool valid = dest.TryGetValue(pair.Key, out Card val);
                if (valid)
                    Clone(pair.Value, val);
                else
                    dest[pair.Key] = CloneNew(pair.Value);
            }
        }

        // 克隆列表并保持引用一致（从引用字典中获取）
        public static void CloneListRef(Dictionary<string, Card> ref_dict, List<Card> source, List<Card> dest)
        {
            for (int i = 0; i < source.Count; i++)
            {
                Card scard = source[i];
                bool valid = ref_dict.TryGetValue(scard.uid, out Card rcard);
                if (valid)
                {
                    if (i < dest.Count)
                        dest[i] = rcard;
                    else
                        dest.Add(rcard);
                }
            }

            if(dest.Count > source.Count)
                dest.RemoveRange(source.Count, dest.Count - source.Count);
        }
    }

    //-------------------- CardStatus 类 --------------------

    [System.Serializable]
    public class CardStatus
    {
        public StatusType type;       // 状态类型
        public int value;             // 状态值
        public int duration = 1;      // 状态持续回合数
        public bool permanent = true; // 是否永久状态

        [System.NonSerialized]
        private StatusData data = null; // 状态数据引用

        public CardStatus() { }

        public CardStatus(StatusType type, int value, int duration)
        {
            this.type = type;
            this.value = value;
            this.duration = duration;
            this.permanent = (duration == 0); // 持续时间为0表示永久状态
        }

        // 获取状态数据对象
        public StatusData StatusData { 
            get
            {
                if (data == null || data.effect != type)
                    data = StatusData.Get(type);
                return data;
            }
        }

        public StatusData Data => StatusData; // 别名访问

        // 克隆新对象
        public static CardStatus CloneNew(CardStatus copy)
        {
            CardStatus status = new CardStatus(copy.type, copy.value, copy.duration);
            status.permanent = copy.permanent;
            return status;
        }

        // 克隆到现有对象
        public static void Clone(CardStatus source, CardStatus dest)
        {
            dest.type = source.type;
            dest.value = source.value;
            dest.duration = source.duration;
            dest.permanent = source.permanent;
        }

        // 克隆列表
        public static void CloneList(List<CardStatus> source, List<CardStatus> dest)
        {
            for (int i = 0; i < source.Count; i++)
            {
                if (i < dest.Count)
                    Clone(source[i], dest[i]);
                else
                    dest.Add(CloneNew(source[i]));
            }

            if (dest.Count > source.Count)
                dest.RemoveRange(source.Count, dest.Count - source.Count);
        }
    }


    [System.Serializable]
    public class CardTrait
    {
        public string id;   // 特性/属性的唯一ID
        public int value;   // 特性值（数值型，可正可负）

        [System.NonSerialized]
        private TraitData data = null; // 缓存对应的 TraitData 对象，非序列化

        // 构造函数：使用 id 和 value 创建特性
        public CardTrait(string id, int value)
        {
            this.id = id;
            this.value = value;
        }

        // 构造函数：使用 TraitData 对象和 value 创建特性
        public CardTrait(TraitData trait, int value)
        {
            this.id = trait.id;
            this.value = value;
        }

        // 获取 TraitData 对象（带缓存）
        public TraitData TraitData
        {
            get
            {
                if (data == null || data.id != id)
                    data = TraitData.Get(id); // 如果缓存为空或 id 不一致，则重新获取
                return data;
            }
        }

        public TraitData Data => TraitData; // 别名访问

        //-------------------- 克隆方法 --------------------

        // 克隆一个新的 CardTrait 对象
        public static CardTrait CloneNew(CardTrait copy)
        {
            CardTrait trait = new CardTrait(copy.id, copy.value);
            return trait;
        }

        // 克隆到已有对象
        public static void Clone(CardTrait source, CardTrait dest)
        {
            dest.id = source.id;
            dest.value = source.value;
        }

        // 克隆列表
        public static void CloneList(List<CardTrait> source, List<CardTrait> dest)
        {
            for (int i = 0; i < source.Count; i++)
            {
                if (i < dest.Count)
                    Clone(source[i], dest[i]); // 目标列表已有元素，直接克隆覆盖
                else
                    dest.Add(CloneNew(source[i])); // 目标列表没有该元素，新增
            }

            if (dest.Count > source.Count)
                dest.RemoveRange(source.Count, dest.Count - source.Count); // 删除多余元素
        }
    }

}

using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    // 状态模型：表示玩家在游戏中的当前数据。
    [System.Serializable]
    public class Player
    {
        public int player_id; // 玩家ID
        public string username; // 玩家用户名
        public string avatar; // 玩家头像
        public string cardback; // 卡背
        public string deck; // 使用的牌组ID

        public bool is_ai = false; // 是否为AI玩家
        public int ai_level; // AI等级

        public bool connected = false; // 是否已连接服务器和游戏
        public bool ready = false; // 是否已准备好（发送完所有玩家数据）

        public int hp; // 当前生命值
        public int hp_max; // 最大生命值
        public int mana = 0; // 当前法力值
        public int mana_max = 0; // 最大法力值
        public int kill_count = 0; // 击杀计数

        public Dictionary<string, Card> cards_all = new(); // 所有卡片字典，便于通过UID快速访问
        public Card hero = null; // 英雄卡

        // 所有卡牌区域，具有互斥性，同时只可能在一个区域中
        public List<Card> cards_deck = new(); // 玩家牌库
        public List<Card> cards_hand = new(); // 玩家手牌
        public List<Card> cards_board = new(); // 玩家场上卡牌
        public List<Card> cards_equip = new(); // 装备卡牌
        public List<Card> cards_discard = new(); // 弃牌区
        public List<Card> cards_secret = new(); // 秘密区（法术陷阱等）
        public List<Card> cards_temp = new(); // 临时生成的卡牌，尚未分配到任何区域

        public List<CardTrait> traits = new(); // 当前持久性特性
        public List<CardTrait> ongoing_traits = new(); // 当前持续性特性

        public List<CardStatus> status = new(); // 当前持久状态/带持续时间的状态
        public List<CardStatus> ongoing_status = new(); // 当前持续状态

        public List<ActionHistory> history_list = new(); // 玩家执行的动作历史记录

        public Player(int id)
        {
            this.player_id = id;
        }

        // 玩家是否准备好（已准备且拥有卡牌数据）
        public bool IsReady()
        {
            return ready && cards_all.Count > 0;
        }

        // 玩家是否已连接（或是AI玩家）
        public bool IsConnected()
        {
            return connected || is_ai;
        }

        // 清除持续状态和持续特性
        public virtual void ClearOngoing()
        {
            ongoing_status.Clear();
            ongoing_traits.Clear();
        }

        //---- 卡牌操作 -----

        // 从所有卡组中移除卡牌
        public virtual void RemoveCardFromAllGroups(Card card)
        {
            cards_deck.Remove(card);
            cards_hand.Remove(card);
            cards_board.Remove(card);
            cards_equip.Remove(card);
            cards_discard.Remove(card);
            cards_secret.Remove(card);
            cards_temp.Remove(card);
            UnequipFromAllCards(card);
        }

        // 从所有卡牌中解除装备关系
        public virtual void UnequipFromAllCards(Card equip)
        {
            foreach (Card card in cards_board)
            {
                if (card.equipped_uid == equip.uid)
                    card.equipped_uid = null;
            }
        }

        // 从列表随机获取一张卡牌
        public virtual Card GetRandomCard(List<Card> card_list, System.Random rand)
        {
            if (card_list.Count > 0)
                return card_list[rand.Next(0, card_list.Count)];
            return null;
        }

        public bool HasCardInHand(Card card)
        {
            return cards_hand.Contains(card);
        }

        public bool HasCardInDeck(Card card)
        {
            return cards_deck.Contains(card);
        }

        // 根据UID获取手牌
        public Card GetHandCard(string uid)
        {
            foreach (Card card in cards_hand)
            {
                if (card.uid == uid)
                    return card;
            }

            return null;
        }

        // 根据UID获取场上卡牌
        public Card GetBoardCard(string uid)
        {
            foreach (Card card in cards_board)
            {
                if (card.uid == uid)
                    return card;
            }

            return null;
        }

        // 根据UID获取装备卡牌
        public Card GetEquipCard(string uid)
        {
            foreach (Card card in cards_equip)
            {
                if (card.uid == uid)
                    return card;
            }

            return null;
        }

        // 根据UID获取牌库卡牌
        public Card GetDeckCard(string uid)
        {
            foreach (Card card in cards_deck)
            {
                if (card.uid == uid)
                    return card;
            }

            return null;
        }

        // 根据UID获取弃牌卡牌
        public Card GetDiscardCard(string uid)
        {
            foreach (Card card in cards_discard)
            {
                if (card.uid == uid)
                    return card;
            }

            return null;
        }

        // 获取某装备卡的携带者（即装备到谁身上）
        public Card GetBearerCard(Card equipment)
        {
            foreach (Card card in cards_board)
            {
                if (card != null && card.equipped_uid == equipment.uid)
                    return card;
            }

            return null;
        }

        // 获取指定槽位上的卡牌
        public Card GetSlotCard(Slot slot)
        {
            foreach (Card card in cards_board)
            {
                if (card != null && card.slot == slot)
                    return card;
            }

            return null;
        }

        // 根据UID获取任意卡牌（从字典中快速查找）
        public Card GetCard(string uid)
        {
            if (uid != null)
            {
                bool valid = cards_all.TryGetValue(uid, out Card card);
                if (valid)
                    return card;
            }

            return null;
        }

        // 判断卡牌是否在场上
        public bool IsOnBoard(Card card)
        {
            return card != null && GetBoardCard(card.uid) != null;
        }

        //---- 槽位操作 -----

        // 获取随机槽位
        public Slot GetRandomSlot(System.Random rand)
        {
            return Slot.GetRandom(player_id, rand);
        }

        // 获取随机空槽位
        public virtual Slot GetRandomEmptySlot(System.Random rand, List<Slot> list_mem = null)
        {
            List<Slot> valid = GetEmptySlots(list_mem);
            if (valid.Count > 0)
                return valid[rand.Next(0, valid.Count)];
            return Slot.None;
        }

        // 获取随机已占用槽位
        public virtual Slot GetRandomOccupiedSlot(System.Random rand, List<Slot> list_mem = null)
        {
            List<Slot> valid = GetOccupiedSlots(list_mem);
            if (valid.Count > 0)
                return valid[rand.Next(0, valid.Count)];
            return Slot.None;
        }

        // 获取所有空槽位
        public List<Slot> GetEmptySlots(List<Slot> list_mem = null)
        {
            List<Slot> valid = list_mem != null ? list_mem : new List<Slot>();
            foreach (Slot slot in Slot.GetAll(player_id))
            {
                Card slot_card = GetSlotCard(slot);
                if (slot_card == null)
                    valid.Add(slot);
            }

            return valid;
        }

        // 获取所有已占用槽位
        public List<Slot> GetOccupiedSlots(List<Slot> list_mem = null)
        {
            List<Slot> valid = list_mem != null ? list_mem : new List<Slot>();
            foreach (Slot slot in Slot.GetAll(player_id))
            {
                Card slot_card = GetSlotCard(slot);
                if (slot_card != null)
                    valid.Add(slot);
            }

            return valid;
        }

        //------ 自定义特性/状态操作 ---------

        // 设置或覆盖特性
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

        // 增加特性值，如果不存在则创建
        public void AddTrait(string id, int value)
        {
            CardTrait trait = GetTrait(id);
            if (trait != null)
                trait.value += value;
            else
                SetTrait(id, value);
        }

        // 增加持续性特性值，如果不存在则创建
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

        // 获取特性
        public CardTrait GetTrait(string id)
        {
            foreach (CardTrait trait in traits)
            {
                if (trait.id == id)
                    return trait;
            }

            return null;
        }

        // 获取持续性特性
        public CardTrait GetOngoingTrait(string id)
        {
            foreach (CardTrait trait in ongoing_traits)
            {
                if (trait.id == id)
                    return trait;
            }

            return null;
        }

        //---- 特性（Traits）操作 -----

        // 获取玩家的所有特性，包括持久性和持续性
        public List<CardTrait> GetAllTraits()
        {
            List<CardTrait> all_traits = new List<CardTrait>();
            all_traits.AddRange(traits); // 添加持久性特性
            all_traits.AddRange(ongoing_traits); // 添加持续性特性
            return all_traits;
        }

        // 获取指定 TraitData 对象的特性值
        public int GetTraitValue(TraitData trait)
        {
            if (trait != null)
                return GetTraitValue(trait.id);
            return 0;
        }

        // 获取指定ID的特性总值（持久+持续）
        public virtual int GetTraitValue(string id)
        {
            int val = 0;
            CardTrait stat1 = GetTrait(id); // 持久性特性
            CardTrait stat2 = GetOngoingTrait(id); // 持续性特性
            if (stat1 != null)
                val += stat1.value;
            if (stat2 != null)
                val += stat2.value;
            return val;
        }

        // 判断是否存在指定 TraitData 对象
        public bool HasTrait(TraitData trait)
        {
            if (trait != null)
                return HasTrait(trait.id);
            return false;
        }

        // 判断是否存在指定ID的特性
        public bool HasTrait(string id)
        {
            foreach (CardTrait trait in traits)
            {
                if (trait.id == id)
                    return true;
            }

            return false;
        }

        //---- 状态（Status）操作 -----

        // 添加状态（带持续时间）
        public void AddStatus(StatusData status, int value, int duration)
        {
            if (status != null)
                AddStatus(status.effect, value, duration);
        }

        // 添加持续状态（无持续时间）
        public void AddOngoingStatus(StatusData status, int value)
        {
            if (status != null)
                AddOngoingStatus(status.effect, value);
        }

        // 添加状态（指定 StatusType、数值和持续时间）
        public void AddStatus(StatusType effect, int value, int duration)
        {
            if (effect != StatusType.None)
            {
                CardStatus status = GetStatus(effect);
                if (status == null)
                {
                    status = new CardStatus(effect, value, duration);
                    this.status.Add(status);
                }
                else
                {
                    status.value += value;
                    status.duration = Mathf.Max(status.duration, duration);
                    status.permanent = status.permanent || duration == 0; // 持续时间为0则标记为永久
                }
            }
        }

        // 添加持续状态（仅数值，无持续时间）
        public void AddOngoingStatus(StatusType effect, int value)
        {
            if (effect != StatusType.None)
            {
                CardStatus status = GetOngoingStatus(effect);
                if (status == null)
                {
                    status = new CardStatus(effect, value, 0);
                    ongoing_status.Add(status);
                }
                else
                {
                    status.value += value;
                }
            }
        }

        // 移除指定状态
        public void RemoveStatus(StatusType effect)
        {
            for (int i = status.Count - 1; i >= 0; i--)
            {
                if (status[i].type == effect)
                    status.RemoveAt(i);
            }
        }

        // 获取指定状态（持久性）
        public CardStatus GetStatus(StatusType effect)
        {
            foreach (CardStatus status in status)
            {
                if (status.type == effect)
                    return status;
            }

            return null;
        }

        // 获取指定状态（持续性）
        public CardStatus GetOngoingStatus(StatusType effect)
        {
            foreach (CardStatus status in ongoing_status)
            {
                if (status.type == effect)
                    return status;
            }

            return null;
        }

        // 获取所有状态（持久+持续）
        public List<CardStatus> GetAllStatus()
        {
            List<CardStatus> all_status = new List<CardStatus>();
            all_status.AddRange(status);
            all_status.AddRange(ongoing_status);
            return all_status;
        }

        // 判断是否存在指定状态
        public bool HasStatus(StatusType effect)
        {
            return GetStatus(effect) != null || GetOngoingStatus(effect) != null;
        }

        // 获取指定状态的总值（持久+持续）
        public virtual int GetStatusValue(StatusType type)
        {
            CardStatus status1 = GetStatus(type);
            CardStatus status2 = GetOngoingStatus(type);
            int v1 = status1 != null ? status1.value : 0;
            int v2 = status2 != null ? status2.value : 0;
            return v1 + v2;
        }

        // 减少所有非永久状态的持续时间
        public virtual void ReduceStatusDurations()
        {
            for (int i = status.Count - 1; i >= 0; i--)
            {
                if (!status[i].permanent)
                {
                    status[i].duration -= 1;
                    if (status[i].duration <= 0)
                        status.RemoveAt(i); // 持续时间为0则移除
                }
            }
        }

        //---- 历史记录（History）操作 -----

        // 添加动作历史（只涉及卡牌本身）
        public void AddHistory(ushort type, Card card)
        {
            ActionHistory order = new ActionHistory();
            order.type = type;
            order.card_id = card.card_id;
            order.card_uid = card.uid;
            history_list.Add(order);
        }

        // 添加动作历史（涉及卡牌和目标卡牌）
        public void AddHistory(ushort type, Card card, Card target)
        {
            ActionHistory order = new ActionHistory();
            order.type = type;
            order.card_id = card.card_id;
            order.card_uid = card.uid;
            order.target_uid = target.uid;
            history_list.Add(order);
        }

        // 添加动作历史（涉及卡牌和目标玩家）
        public void AddHistory(ushort type, Card card, Player target)
        {
            ActionHistory order = new ActionHistory();
            order.type = type;
            order.card_id = card.card_id;
            order.card_uid = card.uid;
            order.target_id = target.player_id;
            history_list.Add(order);
        }


        //---- 历史记录（History）操作：Ability 相关 -----

        // 添加动作历史（涉及卡牌和能力本身）
        public void AddHistory(ushort type, Card card, AbilityData ability)
        {
            ActionHistory order = new ActionHistory();
            order.type = type; // 动作类型
            order.card_id = card.card_id; // 卡牌ID
            order.card_uid = card.uid; // 卡牌唯一UID
            order.ability_id = ability.id; // 能力ID
            history_list.Add(order);
        }

        // 添加动作历史（涉及卡牌、能力和目标卡牌）
        public void AddHistory(ushort type, Card card, AbilityData ability, Card target)
        {
            ActionHistory order = new ActionHistory();
            order.type = type;
            order.card_id = card.card_id;
            order.card_uid = card.uid;
            order.ability_id = ability.id;
            order.target_uid = target.uid; // 目标卡牌的UID
            history_list.Add(order);
        }

        // 添加动作历史（涉及卡牌、能力和目标玩家）
        public void AddHistory(ushort type, Card card, AbilityData ability, Player target)
        {
            ActionHistory order = new ActionHistory();
            order.type = type;
            order.card_id = card.card_id;
            order.card_uid = card.uid;
            order.ability_id = ability.id;
            order.target_id = target.player_id; // 目标玩家ID
            history_list.Add(order);
        }

        // 添加动作历史（涉及卡牌、能力和目标槽位）
        public void AddHistory(ushort type, Card card, AbilityData ability, Slot target)
        {
            ActionHistory order = new ActionHistory();
            order.type = type;
            order.card_id = card.card_id;
            order.card_uid = card.uid;
            order.ability_id = ability.id;
            order.slot = target; // 目标槽位
            history_list.Add(order);
        }


        //---- 行动检查（Action Check） -----

        // 判断玩家是否有足够法力施放卡牌
        public virtual bool CanPayMana(Card card)
        {
            if (card.CardData.IsDynamicManaCost()) // 动态费用卡牌不受当前法力限制
                return true;
            return mana >= card.GetMana();
        }

        // 消耗玩家法力施放卡牌
        public virtual void PayMana(Card card)
        {
            if (!card.CardData.IsDynamicManaCost())
                mana -= card.GetMana();
        }

        // 判断玩家是否能施放某个卡牌的能力
        public virtual bool CanPayAbility(Card card, AbilityData ability)
        {
            bool exhaust = !card.exhausted || !ability.exhaust; // 判断卡牌是否未疲劳或能力不消耗疲劳
            return exhaust && mana >= ability.mana_cost; // 法力是否足够
        }

        // 判断玩家是否已死亡（手牌、场上、牌库为空 或 HP ≤ 0）
        public virtual bool IsDead()
        {
            if (cards_hand.Count == 0 && cards_board.Count == 0 && cards_deck.Count == 0)
                return true;
            if (hp <= 0)
                return true;
            return false;
        }


        //--------------------

        // 克隆玩家数据到另一个 Player 对象，通常用于 AI 预测
        public static void Clone(Player source, Player dest)
        {
            dest.player_id = source.player_id;
            dest.is_ai = source.is_ai;
            dest.ai_level = source.ai_level;

            // 注释掉的变量不用于 AI 预测
            //dest.username = source.username;
            //dest.avatar = source.avatar;
            //dest.deck = source.deck;
            //dest.connected = source.connected;
            //dest.ready = source.ready;

            dest.hp = source.hp;
            dest.hp_max = source.hp_max;
            dest.mana = source.mana;
            dest.mana_max = source.mana_max;
            dest.kill_count = source.kill_count;

            Card.CloneNull(source.hero, ref dest.hero); // 克隆英雄卡
            Card.CloneDict(source.cards_all, dest.cards_all); // 克隆卡牌字典
            Card.CloneListRef(dest.cards_all, source.cards_board, dest.cards_board);
            Card.CloneListRef(dest.cards_all, source.cards_equip, dest.cards_equip);
            Card.CloneListRef(dest.cards_all, source.cards_hand, dest.cards_hand);
            Card.CloneListRef(dest.cards_all, source.cards_deck, dest.cards_deck);
            Card.CloneListRef(dest.cards_all, source.cards_discard, dest.cards_discard);
            Card.CloneListRef(dest.cards_all, source.cards_secret, dest.cards_secret);
            Card.CloneListRef(dest.cards_all, source.cards_temp, dest.cards_temp);

            CardStatus.CloneList(source.status, dest.status); // 克隆状态
            CardStatus.CloneList(source.ongoing_status, dest.ongoing_status);
        }
    }
    //---- 动作历史结构体 -----
    [System.Serializable]
    public class ActionHistory
    {
        public ushort type; // 动作类型
        public string card_id; // 卡牌ID
        public string card_uid; // 卡牌唯一UID
        public string target_uid; // 目标卡牌UID
        public string ability_id; // 能力ID
        public int target_id; // 目标玩家ID
        public Slot slot; // 目标槽位
    }
}

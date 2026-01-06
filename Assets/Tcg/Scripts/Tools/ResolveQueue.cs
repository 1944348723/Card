using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TcgEngine
{
    /// <summary>
    /// 处理技能、攻击、奥秘等队列，按顺序逐个执行
    /// 可以在每个执行间设置延迟
    /// </summary>
    public class ResolveQueue 
    {
        // 对象池：技能队列元素
        private Pool<AbilityQueueElement> ability_elem_pool = new Pool<AbilityQueueElement>();

        // 对象池：奥秘队列元素
        private Pool<SecretQueueElement> secret_elem_pool = new Pool<SecretQueueElement>();

        // 对象池：攻击队列元素
        private Pool<AttackQueueElement> attack_elem_pool = new Pool<AttackQueueElement>();

        // 对象池：回调队列元素
        private Pool<CallbackQueueElement> callback_elem_pool = new Pool<CallbackQueueElement>();

        // 技能队列
        private Queue<AbilityQueueElement> ability_queue = new Queue<AbilityQueueElement>();

        // 奥秘队列
        private Queue<SecretQueueElement> secret_queue = new Queue<SecretQueueElement>();

        // 攻击队列
        private Queue<AttackQueueElement> attack_queue = new Queue<AttackQueueElement>();

        // 回调队列
        private Queue<CallbackQueueElement> callback_queue = new Queue<CallbackQueueElement>();

        // 当前游戏数据
        private Game game_data;

        // 是否正在执行队列
        private bool is_resolving = false;

        // 执行延迟
        private float resolve_delay = 0f;

        // 是否跳过延迟
        private bool skip_delay = false;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="data">游戏数据</param>
        /// <param name="skip">是否跳过延迟</param>
        public ResolveQueue(Game data, bool skip)
        {
            game_data = data;
            skip_delay = skip;
        }

        /// <summary>
        /// 设置游戏数据
        /// </summary>
        public void SetData(Game data)
        {
            game_data = data;
        }

        /// <summary>
        /// 每帧更新，处理延迟逻辑
        /// </summary>
        public virtual void Update(float delta)
        {
            if (resolve_delay > 0f)
            {
                resolve_delay -= delta;
                if (resolve_delay <= 0f)
                    ResolveAll(); // 延迟结束后执行所有队列
            }
        }

        /// <summary>
        /// 将技能加入队列
        /// </summary>
        public virtual void AddAbility(AbilityData ability, Card caster, Card triggerer, Action<AbilityData, Card, Card> callback)
        {
            if (ability != null && caster != null)
            {
                AbilityQueueElement elem = ability_elem_pool.Create();
                elem.caster = caster;
                elem.triggerer = triggerer;
                elem.ability = ability;
                elem.callback = callback;
                ability_queue.Enqueue(elem);
            }
        }

        /// <summary>
        /// 将攻击加入队列（目标为卡牌）
        /// </summary>
        public virtual void AddAttack(Card attacker, Card target, Action<Card, Card, bool> callback, bool skip_cost = false)
        {
            if (attacker != null && target != null)
            {
                AttackQueueElement elem = attack_elem_pool.Create();
                elem.attacker = attacker;
                elem.target = target;
                elem.ptarget = null;
                elem.skip_cost = skip_cost;
                elem.callback = callback;
                attack_queue.Enqueue(elem);
            }
        }

        /// <summary>
        /// 将攻击加入队列（目标为玩家）
        /// </summary>
        public virtual void AddAttack(Card attacker, Player target, Action<Card, Player, bool> callback, bool skip_cost = false)
        {
            if (attacker != null && target != null)
            {
                AttackQueueElement elem = attack_elem_pool.Create();
                elem.attacker = attacker;
                elem.target = null;
                elem.ptarget = target;
                elem.skip_cost = skip_cost;
                elem.pcallback = callback;
                attack_queue.Enqueue(elem);
            }
        }

        /// <summary>
        /// 将奥秘触发加入队列
        /// </summary>
        public virtual void AddSecret(AbilityTrigger secret_trigger, Card secret, Card trigger, Action<AbilityTrigger, Card, Card> callback)
        {
            if (secret != null && trigger != null)
            {
                SecretQueueElement elem = secret_elem_pool.Create();
                elem.secret_trigger = secret_trigger;
                elem.secret = secret;
                elem.triggerer = trigger;
                elem.callback = callback;
                secret_queue.Enqueue(elem);
            }
        }

        /// <summary>
        /// 将回调加入队列
        /// </summary>
        public virtual void AddCallback(Action callback)
        {
            if (callback != null)
            {
                CallbackQueueElement elem = callback_elem_pool.Create();
                elem.callback = callback;
                callback_queue.Enqueue(elem);
            }
        }

        /// <summary>
        /// 执行队列中的一个元素
        /// </summary>
        public virtual void Resolve()
        {
            if (ability_queue.Count > 0)
            {
                // 执行技能
                AbilityQueueElement elem = ability_queue.Dequeue();
                ability_elem_pool.Dispose(elem);
                elem.callback?.Invoke(elem.ability, elem.caster, elem.triggerer);
            }
            else if (secret_queue.Count > 0)
            {
                // 执行奥秘
                SecretQueueElement elem = secret_queue.Dequeue();
                secret_elem_pool.Dispose(elem);
                elem.callback?.Invoke(elem.secret_trigger, elem.secret, elem.triggerer);
            }
            else if (attack_queue.Count > 0)
            {
                // 执行攻击
                AttackQueueElement elem = attack_queue.Dequeue();
                attack_elem_pool.Dispose(elem);
                if (elem.ptarget != null)
                    elem.pcallback?.Invoke(elem.attacker, elem.ptarget, elem.skip_cost);
                else
                    elem.callback?.Invoke(elem.attacker, elem.target, elem.skip_cost);
            }
            else if (callback_queue.Count > 0)
            {
                // 执行回调
                CallbackQueueElement elem = callback_queue.Dequeue();
                callback_elem_pool.Dispose(elem);
                elem.callback.Invoke();
            }
        }

        /// <summary>
        /// 设置延迟后执行所有队列
        /// </summary>
        public virtual void ResolveAll(float delay)
        {
            SetDelay(delay);
            ResolveAll();  // 如果没有延迟则立即执行
        }

        /// <summary>
        /// 执行所有队列
        /// </summary>
        public virtual void ResolveAll()
        {
            if (is_resolving)
                return;

            is_resolving = true;
            while (CanResolve())
            {
                Resolve();
            }
            is_resolving = false;
        }

        /// <summary>
        /// 设置队列执行延迟
        /// </summary>
        public virtual void SetDelay(float delay)
        {
            if (!skip_delay)
            {
                resolve_delay = Mathf.Max(resolve_delay, delay);
            }
        }

        /// <summary>
        /// 是否可以继续执行队列
        /// </summary>
        public virtual bool CanResolve()
        {
            if (resolve_delay > 0f)
                return false;   // 延迟中
            if (game_data.state == GameState.GameEnded)
                return false; // 游戏结束无法执行
            if (game_data.selector != SelectorType.None)
                return false; // 玩家选择中，暂停执行
            return attack_queue.Count > 0 || ability_queue.Count > 0 || secret_queue.Count > 0 || callback_queue.Count > 0;
        }

        /// <summary>
        /// 是否正在执行队列
        /// </summary>
        public virtual bool IsResolving()
        {
            return is_resolving || resolve_delay > 0f;
        }

        /// <summary>
        /// 清空所有队列和对象池
        /// </summary>
        public virtual void Clear()
        {
            attack_elem_pool.DisposeAll();
            ability_elem_pool.DisposeAll();
            secret_elem_pool.DisposeAll();
            callback_elem_pool.DisposeAll();
            attack_queue.Clear();
            ability_queue.Clear();
            secret_queue.Clear();
            callback_queue.Clear();
        }

        /// <summary>
        /// 获取攻击队列
        /// </summary>
        public Queue<AttackQueueElement> GetAttackQueue()
        {
            return attack_queue;
        }

        /// <summary>
        /// 获取技能队列
        /// </summary>
        public Queue<AbilityQueueElement> GetAbilityQueue()
        {
            return ability_queue;
        }

        /// <summary>
        /// 获取奥秘队列
        /// </summary>
        public Queue<SecretQueueElement> GetSecretQueue()
        {
            return secret_queue;
        }

        /// <summary>
        /// 获取回调队列
        /// </summary>
        public Queue<CallbackQueueElement> GetCallbackQueue()
        {
            return callback_queue;
        }
    }

    /// <summary>
    /// 技能队列元素
    /// </summary>
    public class AbilityQueueElement
    {
        public AbilityData ability;                                     // 技能数据
        public Card caster;                                             // 技能施放者
        public Card triggerer;                                          // 技能触发者
        public Action<AbilityData, Card, Card> callback;               // 回调
    }

    /// <summary>
    /// 攻击队列元素
    /// </summary>
    public class AttackQueueElement
    {
        public Card attacker;                                          // 攻击者
        public Card target;                                            // 攻击目标卡牌
        public Player ptarget;                                         // 攻击目标玩家
        public bool skip_cost;                                         // 是否跳过消耗
        public Action<Card, Card, bool> callback;                     // 回调（卡牌目标）
        public Action<Card, Player, bool> pcallback;                  // 回调（玩家目标）
    }

    /// <summary>
    /// 奥秘队列元素
    /// </summary>
    public class SecretQueueElement
    {
        public AbilityTrigger secret_trigger;                          // 奥秘触发事件
        public Card secret;                                            // 奥秘卡牌
        public Card triggerer;                                         // 奥秘触发者
        public Action<AbilityTrigger, Card, Card> callback;           // 回调
    }

    /// <summary>
    /// 普通回调队列元素
    /// </summary>
    public class CallbackQueueElement
    {
        public Action callback;                                        // 回调
    }
}

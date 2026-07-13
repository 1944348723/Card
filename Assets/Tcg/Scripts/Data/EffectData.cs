using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// 所有技能效果的基类
    /// 子类可以重写不同的 DoEffect 方法来实现具体的效果逻辑
    /// </summary>
    public class EffectData : ScriptableObject
    {
        /// <summary>
        /// 对服务器端游戏逻辑应用效果（无指定目标）
        /// </summary>
        public virtual void DoEffect(EffectContext context, AbilityData ability, Card caster)
        {
            // 服务器端的游戏逻辑
        }

        /// <summary>
        /// 对指定卡牌目标施加效果
        /// </summary>
        public virtual void DoEffect(EffectContext context, AbilityData ability, Card caster, Card target)
        {
            // 服务器端的游戏逻辑
        }

        /// <summary>
        /// 对指定玩家目标施加效果
        /// </summary>
        public virtual void DoEffect(EffectContext context, AbilityData ability, Card caster, Player target)
        {
            // 服务器端的游戏逻辑
        }

        /// <summary>
        /// 对指定格子目标施加效果
        /// </summary>
        public virtual void DoEffect(EffectContext context, AbilityData ability, Card caster, Slot target)
        {
            // 服务器端的游戏逻辑
        }

        /// <summary>
        /// 对指定卡牌数据目标施加效果（主要用于生成新卡效果）
        /// </summary>
        public virtual void DoEffect(EffectContext context, AbilityData ability, Card caster, CardData target)
        {
            // 服务器端的游戏逻辑
        }

        /// <summary>
        /// 持续效果，仅作用于卡牌目标
        /// </summary>
        public virtual void DoOngoingEffect(EffectContext context, AbilityData ability, Card caster, Card target)
        {
            // 持续效果逻辑
        }

        /// <summary>
        /// 持续效果，仅作用于玩家目标
        /// </summary>
        public virtual void DoOngoingEffect(EffectContext context, AbilityData ability, Card caster, Player target)
        {
            // 持续效果逻辑
        }

        /// <summary>
        /// 根据运算类型对整数值进行加法或赋值操作
        /// </summary>
        /// <param name="original_val">原始值</param>
        /// <param name="oper">操作类型（Add 或 Set）</param>
        /// <param name="add_value">要加或赋的新值</param>
        /// <returns>返回修改后的值</returns>
        public int AddOrSet(int original_val, EffectOperatorInt oper, int add_value)
        {
            if (oper == EffectOperatorInt.Add)
                return original_val + add_value;
            if (oper == EffectOperatorInt.Set)
                return add_value;
            return original_val;
        }
    }

    /// <summary>
    /// 整数效果运算类型
    /// Add = 加法，Set = 直接赋值
    /// </summary>
    public enum EffectOperatorInt
    {
        Add, // 加法运算
        Set, // 直接赋值
    }
}

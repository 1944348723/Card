using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 所有目标过滤器的基类
    /// 可以在目标条件筛选后、效果应用前，对目标进行进一步过滤
    /// </summary>
    public class FilterData : ScriptableObject
    {
        /// <summary>
        /// 对卡牌列表进行过滤
        /// </summary>
        /// <param name="data">游戏数据对象</param>
        /// <param name="ability">技能数据对象</param>
        /// <param name="caster">施法卡牌</param>
        /// <param name="source">原始目标列表</param>
        /// <param name="dest">可复用的目标列表（优化用）</param>
        /// <returns>返回过滤后的目标列表</returns>
        public virtual List<Card> FilterTargets(Game data, AbilityData ability, Card caster, List<Card> source, List<Card> dest)
        {
            return source; // 可重写此方法，针对卡牌目标进行过滤
        }

        /// <summary>
        /// 对玩家列表进行过滤
        /// </summary>
        /// <param name="data">游戏数据对象</param>
        /// <param name="ability">技能数据对象</param>
        /// <param name="caster">施法卡牌</param>
        /// <param name="source">原始目标列表</param>
        /// <param name="dest">可复用的目标列表（优化用）</param>
        /// <returns>返回过滤后的目标列表</returns>
        public virtual List<Player> FilterTargets(Game data, AbilityData ability, Card caster, List<Player> source, List<Player> dest)
        {
            return source; // 可重写此方法，针对玩家目标进行过滤
        }

        /// <summary>
        /// 对格子列表进行过滤
        /// </summary>
        /// <param name="data">游戏数据对象</param>
        /// <param name="ability">技能数据对象</param>
        /// <param name="caster">施法卡牌</param>
        /// <param name="source">原始目标列表</param>
        /// <param name="dest">可复用的目标列表（优化用）</param>
        /// <returns>返回过滤后的目标列表</returns>
        public virtual List<Slot> FilterTargets(Game data, AbilityData ability, Card caster, List<Slot> source, List<Slot> dest)
        {
            return source; // 可重写此方法，针对格子目标进行过滤
        }

        /// <summary>
        /// 对卡牌数据列表进行过滤（主要用于生成新卡效果）
        /// </summary>
        /// <param name="data">游戏数据对象</param>
        /// <param name="ability">技能数据对象</param>
        /// <param name="caster">施法卡牌</param>
        /// <param name="source">原始目标列表</param>
        /// <param name="dest">可复用的目标列表（优化用）</param>
        /// <returns>返回过滤后的目标列表</returns>
        public virtual List<CardData> FilterTargets(Game data, AbilityData ability, Card caster, List<CardData> source, List<CardData> dest)
        {
            return source; // 可重写此方法，用于过滤生成新卡的目标
        }
    }
}

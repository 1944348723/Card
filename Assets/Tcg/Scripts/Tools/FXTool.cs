using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.FX;
using TcgEngine.Client;

namespace TcgEngine
{
    /// <summary>
    /// 特效工具类
    /// 提供静态方法用于生成各种 FX（特效）Prefab，包括瞬间特效、绑定特效、投射特效等
    /// </summary>
    public class FXTool : MonoBehaviour
    {
        /// <summary>
        /// 在指定位置生成一个特效 Prefab，并在 duration 秒后自动销毁
        /// </summary>
        /// <param name="fx_prefab">要生成的特效预制体</param>
        /// <param name="pos">生成位置</param>
        /// <param name="duration">持续时间，秒</param>
        /// <returns>生成的特效对象</returns>
        public static GameObject DoFX(GameObject fx_prefab, Vector3 pos, float duration = 5f)
        {
            if (fx_prefab != null)
            {
                GameObject fx = Instantiate(fx_prefab, pos, GetFXRotation());
                Destroy(fx, duration); // duration 秒后销毁
                return fx;
            }
            return null;
        }

        /// <summary>
        /// 生成一个绑定到目标 Transform 的特效，偏移量为 Vector3.zero
        /// </summary>
        public static GameObject DoSnapFX(GameObject fx_prefab, Transform snap_target)
        {
            return DoSnapFX(fx_prefab, snap_target, Vector3.zero);
        }

        /// <summary>
        /// 生成一个绑定到目标 Transform 的特效，并指定偏移量
        /// 特效会跟随目标移动，并在 5 秒后自动销毁
        /// </summary>
        public static GameObject DoSnapFX(GameObject fx_prefab, Transform snap_target, Vector3 offset)
        {
            if (fx_prefab != null && snap_target != null)
            {
                GameObject fx = Instantiate(fx_prefab, snap_target.transform.position + offset, GetFXRotation());
                SnapFX snap = fx.AddComponent<SnapFX>();
                snap.target = snap_target; // 设置绑定目标
                snap.offset = offset;      // 设置偏移量
                Destroy(fx, 5f);           // 5 秒后销毁
                return fx;
            }
            return null;
        }

        /// <summary>
        /// 生成一个投射特效（Projectile FX），从 source 发射到 target，并造成 damage 伤害
        /// 特效会在飞行结束后销毁
        /// </summary>
        public static GameObject DoProjectileFX(GameObject fx_prefab, Transform source, Transform target, int damage)
        {
            if (fx_prefab != null && source != null && target != null)
            {
                GameObject fx = Instantiate(fx_prefab, source.position, GetFXRotation());
                Projectile projectile = fx.GetComponent<Projectile>();
                if (projectile == null)
                    projectile = fx.AddComponent<Projectile>();

                projectile.SetSource(source); // 设置来源
                projectile.SetTarget(target); // 设置目标
                projectile.damage = damage;   // 设置伤害
                projectile.DelayDamage();     // 延迟处理伤害

                Destroy(fx, projectile.duration); // 特效持续时间后销毁
                return fx;
            }
            return null;
        }

        /// <summary>
        /// 获取 FX 的默认旋转方向
        /// 默认面向游戏棋盘的 forward 方向，如果不存在棋盘则朝向 Vector3.forward
        /// </summary>
        private static Quaternion GetFXRotation()
        {
            GameBoard board = GameBoard.Get();
            Vector3 facing = board != null ? board.transform.forward : Vector3.forward;
            return Quaternion.LookRotation(facing, Vector3.up);
        }
    }
}

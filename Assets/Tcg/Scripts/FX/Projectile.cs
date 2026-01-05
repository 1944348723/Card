using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Client;
using TcgEngine.UI;

namespace TcgEngine.FX
{
    /// <summary>
    /// 投射物特效（Projectile FX）
    /// 用于显示卡片/技能发射的飞行物，命中目标时触发爆炸特效和音效
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        // 投射物飞行速度
        public float speed = 10f;

        // 投射物最大存在时间（秒），超过后自动销毁
        public float duration = 4f;

        // 命中后触发的特效预制体
        public GameObject explode_fx;

        // 命中后播放的音效
        public AudioClip explode_audio;

        [HideInInspector]
        public int damage; // 投射物造成的伤害，用于延迟显示 HP 变化

        // 飞行起点和目标
        private Transform source;
        private Transform target;

        // 起点和目标的偏移量，用于微调飞行路径
        private Vector3 source_offset;
        private Vector3 target_offset;

        // 飞行计时器
        private float timer = 0f;

        /// <summary>
        /// 延迟显示伤害值
        /// 主要作用是让飞行物命中前 HP 不会提前变化
        /// </summary>
        public void DelayDamage()
        {
            BoardCard tcard = target?.GetComponent<BoardCard>();
            if (tcard != null)
            {
                // 延迟显示板上卡片受到的伤害
                tcard.DelayDamage(damage, 8f / speed);
            }

            BoardSlotPlayer pslot = target?.GetComponent<BoardSlotPlayer>();
            if (pslot != null)
            {
                // 延迟显示玩家受到的伤害
                PlayerUI player_ui = PlayerUI.Get(pslot.GetPlayerID() != GameClient.Get().GetPlayerID());
                player_ui.DelayDamage(damage, 8f / speed);
            }
        }

        void Update()
        {
            timer += Time.deltaTime;

            // 如果起点或目标为空，则销毁投射物
            if (source == null || target == null)
            {
                Destroy(gameObject);
                return;
            }

            // 超过存在时间后销毁
            if (timer > duration)
            {
                Destroy(gameObject);
                return;
            }

            // 当前投射物位置
            Vector3 spos = transform.position;

            // 目标位置（考虑偏移量）
            Vector3 tpos = target.position + target_offset;

            // 飞行方向向量
            Vector3 dir = (tpos - spos);

            // 按速度移动投射物
            transform.position += dir.normalized * Mathf.Min(dir.magnitude, 1f) * speed * Time.deltaTime;

            // 设置投射物旋转方向，使其朝向目标
            transform.rotation = GetFXRotation(dir.normalized);

            // 当投射物接近目标时触发爆炸特效和音效，并销毁投射物
            if (dir.magnitude < 0.2f)
            {
                FXTool.DoFX(explode_fx, target.position);
                AudioTool.Get().PlaySFX("fx", explode_audio);
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 设置投射物起点
        /// </summary>
        public void SetSource(Transform source)
        {
            this.source = source;
            transform.position = source.position;
        }

        /// <summary>
        /// 设置投射物起点及偏移量
        /// </summary>
        public void SetSource(Transform source, Vector3 offset)
        {
            this.source = source;
            source_offset = offset;
            transform.position = source.position + source_offset;
        }

        /// <summary>
        /// 设置投射物目标
        /// </summary>
        public void SetTarget(Transform target)
        {
            this.target = target;
        }

        /// <summary>
        /// 设置投射物目标及偏移量
        /// </summary>
        public void SetTarget(Transform target, Vector3 offset)
        {
            this.target = target;
            target_offset = offset;
        }

        /// <summary>
        /// 获取投射物旋转朝向
        /// 投射物始终面向游戏板前方，并沿 dir 方向旋转
        /// </summary>
        private static Quaternion GetFXRotation(Vector3 dir)
        {
            GameBoard board = GameBoard.Get();
            Vector3 facing = board != null ? board.transform.forward : Vector3.forward;
            return Quaternion.LookRotation(facing, dir);
        }
    }
}

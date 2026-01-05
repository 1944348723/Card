using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine.FX
{
    /// <summary>
    /// 骰子掷骰特效（Dice Roll FX）
    /// 仅针对6面骰子设计
    /// </summary>
    public class DiceRollFX : MonoBehaviour
    {
        public int value;  // 骰子最终点数（1~6）

        [Header("Anim")]
        public Transform dice;           // 骰子模型的Transform
        public float roll_speed = 20f;   // 骰子旋转速度
        public float roll_duration = 1f; // 掷骰动画持续时间
        public AudioClip start_audio;    // 掷骰开始音效
        public AudioClip end_audio;      // 掷骰结束音效

        private Vector3[] dir;           // 每一面的方向向量
        private bool ended = false;      // 掷骰是否结束
        private float timer = 0f;        // 计时器
        private float x = 0f;            // X轴旋转累加
        private float y = 0f;            // Y轴旋转累加
        private float z = 0f;            // Z轴旋转累加

        void Start()
        {
            // 初始化每个面的方向向量
            dir = new Vector3[6];
            dir[0] = Vector3.forward;  // 1点
            dir[1] = Vector3.up;       // 2点
            dir[2] = Vector3.right;    // 3点
            dir[3] = Vector3.left;     // 4点
            dir[4] = Vector3.down;     // 5点
            dir[5] = Vector3.back;     // 6点

            // 播放掷骰开始音效
            AudioTool.Get().PlaySFX("dice", start_audio);
        }

        void Update()
        {
            timer += Time.deltaTime;

            // 掷骰动画阶段
            if (!ended)
            {
                if (timer < roll_duration)
                {
                    // 累加旋转角度
                    x += 5f * Time.deltaTime;
                    y += 7f * Time.deltaTime;

                    // 旋转骰子
                    dice.Rotate(x * roll_speed, y * roll_speed, z * roll_speed, Space.Self);
                }
                else
                {
                    // 掷骰结束
                    ended = true;
                    timer = 0f;

                    // 播放掷骰结束音效
                    AudioTool.Get().PlaySFX("dice", end_audio);
                }
            }

            // 骰子停止后，将骰子旋转到最终点数
            if (ended)
            {
                if (value >= 1 && value <= dir.Length)
                {
                    Vector3 target = dir[value - 1];  // 目标面的方向
                    Vector3 up = target.y > target.z ? Vector3.back : Vector3.up;  // 上方向向量
                    Quaternion trot = Quaternion.LookRotation(target, up);        // 计算目标旋转
                    dice.localRotation = Quaternion.Slerp(dice.localRotation, trot, roll_speed * Time.deltaTime);
                }

                // 停留一段时间后销毁骰子对象
                if (timer > 1f)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}

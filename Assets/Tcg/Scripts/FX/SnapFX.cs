using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine.FX
{
    /// <summary>
    /// 跟随特效（Snap FX）
    /// 将特效对象“贴附”到目标对象上，并跟随其移动
    /// </summary>
    public class SnapFX : MonoBehaviour
    {
        // 目标对象，特效将跟随该对象移动
        public Transform target;

        // 与目标的偏移量，可调整特效显示位置
        public Vector3 offset = Vector3.zero;

        void Update()
        {
            // 如果目标为空，则销毁特效
            if (target == null)
            {
                Destroy(gameObject);
                return;
            }

            // 将特效位置设置为目标位置加上偏移量
            transform.position = target.position + offset;
        }
    }
}
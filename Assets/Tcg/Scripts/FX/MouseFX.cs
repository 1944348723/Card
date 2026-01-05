using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine.FX
{
    /// <summary>
    /// 随鼠标移动的特效（FX）
    /// </summary>
    public class MouseFX : MonoBehaviour
    {
        // 跟随鼠标移动的速度
        public float speed = 20f;

        // 每帧更新鼠标位置并移动特效
        void Update()
        {
            // 从主摄像机发出一条射线，经过鼠标屏幕位置
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            // 定义一个平面（这里是 XY 平面，即 z=0）
            Plane plane = new Plane(Vector3.forward, 0f);

            // 计算射线与平面的交点距离
            plane.Raycast(ray, out float dist);

            // 获取射线与平面的交点位置
            Vector3 tpos = ray.GetPoint(dist);

            // 平滑移动特效到目标位置
            transform.position = Vector3.Lerp(transform.position, tpos, speed * Time.deltaTime);
        }
    }
}
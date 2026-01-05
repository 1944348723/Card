using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Client;

namespace TcgEngine.FX
{
    /// <summary>
    /// 使特效面向某个方向（通常是相机或游戏板）
    /// </summary>
    public class FaceFX : MonoBehaviour
    {
        public FaceType type;  // 特效面向类型

        void Start()
        {
            Vector3 up = GameBoard.Get().transform.up;  // 上方向向量，通常为Board的Y轴方向

            // 面向相机方向（与相机旋转保持一致）
            if (type == FaceType.FaceCamera)
            {
                GameCamera cam = GameCamera.Get();
                if (cam != null)
                {
                    Vector3 forward = cam.transform.forward;  // 相机的前方向
                    transform.rotation = Quaternion.LookRotation(forward, up);  // 设置特效旋转
                }
            }

            // 面向相机中心（特效指向相机位置）
            if (type == FaceType.FaceCameraCenter)
            {
                GameCamera cam = GameCamera.Get();
                if (cam != null)
                {
                    Vector3 forward = transform.position - cam.transform.position; // 指向相机的向量
                    transform.rotation = Quaternion.LookRotation(forward.normalized, up);  // 设置旋转
                }
            }

            // 面向游戏板（与Board的前方向一致）
            if (type == FaceType.FaceBoard)
            {
                GameBoard board = GameBoard.Get();
                if (board != null)
                {
                    Vector3 forward = board.transform.forward;  // Board的前方向
                    transform.rotation = Quaternion.LookRotation(forward, up);  // 设置旋转
                }
            }
        }
    }

    /// <summary>
    /// 特效面向类型枚举
    /// </summary>
    public enum FaceType
    {
        FaceCamera = 0,         // 与相机旋转保持一致
        FaceCameraCenter = 5,   // 面向相机位置
        FaceBoard = 10          // 与游戏板旋转保持一致
    }
}
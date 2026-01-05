using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Client;

namespace TcgEngine.FX
{
    /// <summary>
    /// 当施法需要选择目标时，显示的准心特效（十字准星）
    /// </summary>
    public class AimTargetFX : MonoBehaviour
    {
        public GameObject fx;  // 准心特效对象

        void Start()
        {
            // 初始化，可留空，当前无需逻辑
        }

        void Update()
        {
            bool visible = false; // 是否显示准心
            HandCard hcard = HandCard.GetDrag(); // 获取当前正在拖拽的手牌
            if (hcard != null)
            {
                Card caster = hcard.GetCard(); // 获取手牌对应的卡牌
                if (caster.CardData.IsRequireTarget()) // 判断卡牌是否需要目标
                    visible = true; // 如果需要目标，则显示准心
            }

            // 根据是否需要显示准心来切换特效显示状态
            if (fx.activeSelf != visible)
                fx.SetActive(visible);

            // 如果需要显示准心，将其位置更新到鼠标射线碰撞的棋盘位置
            if (visible)
            {
                Vector3 dest = GameBoard.Get().RaycastMouseBoard(); // 获取鼠标在棋盘上的世界位置
                transform.position = dest; // 更新准心位置
            }
        }
    }
}
using TcgEngine.Client;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine;

namespace TcgEngine.FX
{
    /// <summary>
    /// 鼠标拖动攻击时显示的连线特效（Line FX）
    /// 用于显示从选中的卡片到鼠标指向位置的攻击线路
    /// </summary>
    public class MouseLineFX : MonoBehaviour
    {
        // 小点模板，用于构建连线
        public GameObject dot_template;

        // 点之间的间距
        public float dot_spacing = 0.2f;

        // 当前渲染的点列表
        private List<GameObject> dot_list = new List<GameObject>();

        // 连线的点位置列表
        private List<Vector3> points = new List<Vector3>();

        void Start()
        {
            // 初始模板隐藏
            dot_template.SetActive(false);
        }

        void Update()
        {
            // 游戏未准备好时不显示
            if (!GameClient.Get().IsReady())
                return;

            // 更新连线路径和渲染
            RefreshLine();
            RefreshRender();
        }

        /// <summary>
        /// 刷新连线的点位置
        /// </summary>
        private void RefreshLine()
        {
            points.Clear();

            Game gdata = GameClient.Get().GetGameData();
            PlayerControls controls = PlayerControls.Get();
            BoardCard bcard = controls.GetSelected();

            bool visible = false;
            Vector3 source = Vector3.zero;

            // 获取选中的板上卡片位置
            if (bcard != null)
            {
                source = bcard.transform.position;
                visible = true;
            }

            // 获取拖动手牌的起点
            HandCard drag = HandCard.GetDrag();
            if (drag != null)
            {
                source = drag.transform.position;
                visible = drag.GetCardData().IsRequireTarget(); // 仅当卡片需要目标时显示
            }

            // 当正在选择目标时更新起点
            if (gdata.selector == SelectorType.SelectTarget && gdata.selector_player_id == GameClient.Get().GetPlayerID())
            {
                BoardCard caster = BoardCard.Get(gdata.selector_caster_uid);
                if (caster != null)
                {
                    source = caster.transform.position;
                    visible = true;
                }
            }

            // 如果连线可见，计算点位置
            if (visible)
            {
                Vector3 dest = GameBoard.Get().RaycastMouseBoard();
                Vector3 dir = (dest - source).normalized; // 方向向量
                float dist = (dest - source).magnitude;   // 距离

                float value = 0f;
                while (value < dist)
                {
                    Vector3 pos = source + dir * value;
                    points.Add(pos); // 添加点位置
                    value += dot_spacing;
                }
            }
        }

        /// <summary>
        /// 根据点列表刷新连线渲染
        /// </summary>
        private void RefreshRender()
        {
            // 如果点不足，新增点
            while (dot_list.Count < points.Count)
            {
                AddDot();
            }

            int index = 0;
            foreach (GameObject dot in dot_list)
            {
                bool active = false;
                if (index < points.Count)
                {
                    Vector3 pos = points[index];
                    dot.transform.position = pos; // 设置点位置
                    active = true;
                }

                // 激活或隐藏点对象
                if (dot.activeSelf != active)
                    dot.SetActive(active);

                index++;
            }
        }

        /// <summary>
        /// 添加一个新的点对象到连线
        /// </summary>
        public void AddDot()
        {
            GameObject dot = Instantiate(dot_template, transform);
            dot.SetActive(true);
            dot_list.Add(dot);
        }
    }
}

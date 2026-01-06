using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine;

namespace TcgEngine.UI
{
    /// <summary>
    /// 集合面板中的卡牌网格
    /// </summary>

    public class CardGrid : MonoBehaviour
    {
        private GridLayoutGroup grid; // 网格布局组件
        private RectTransform rect;   // 网格的 RectTransform

        void Awake()
        {
            grid = GetComponent<GridLayoutGroup>(); // 获取 GridLayoutGroup 组件
            rect = GetComponent<RectTransform>();   // 获取 RectTransform 组件
        }

        // 计算网格的行数和列数
        public void GetColumnAndRow(out int rows, out int columns)
        {
            rows = 0;
            columns = 0;

            if (grid.transform.childCount == 0)
                return;

            // 获取第一个子对象的 RectTransform
            RectTransform firstChildObj = grid.transform.GetChild(0).GetComponent<RectTransform>();
            Vector2 firstChildPos = firstChildObj.anchoredPosition;
            bool stopCountingCol = false;

            if (firstChildPos.x == 0 && firstChildPos.y == 0)
                return;

            // 初始化行列为 1
            rows = 1;
            columns = 1;

            // 遍历剩余的子对象
            for (int i = 1; i < grid.transform.childCount; i++)
            {
                // 获取当前子对象
                RectTransform currentChildObj = grid.transform.GetChild(i).GetComponent<RectTransform>();
                Vector2 currentChildPos = currentChildObj.anchoredPosition;

                // 判断是列还是行
                if (Mathf.Abs(firstChildPos.x - currentChildPos.x) < 0.1f)
                {
                    rows++;               // 行数增加
                    stopCountingCol = true;
                }
                else
                {
                    if (!stopCountingCol)
                        columns++;       // 列数增加
                }
            }
        }

        // 获取网格布局组件
        public GridLayoutGroup GetGrid()
        {
            return grid;
        }

        // 获取网格的 RectTransform
        public RectTransform GetRect()
        {
            return rect;
        }
    }
}

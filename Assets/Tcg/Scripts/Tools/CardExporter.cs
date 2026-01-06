using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine.UI;

namespace TcgEngine
{
    /// <summary>
    /// 卡牌导出工具脚本，将所有卡牌导出为 PNG 图片
    /// 导出的图片包含卡牌 UI、属性等信息
    /// </summary>
    public class CardExporter : MonoBehaviour
    {
        public string export_path = "C:/CardsExport"; // 导出路径
        public int width = 856;                        // 图片宽度
        public int height = 1200;                      // 图片高度
        public VariantData variant;                     // 卡牌显示的变体数据（皮肤/样式）

        [Header("引用")]
        public Camera render_cam;                       // 用于渲染卡牌的摄像机
        public CardUI card_ui;                          // 卡牌 UI 脚本引用

        private RenderTexture texture;                 // 渲染用 RenderTexture
        private Texture2D export_texture;             // 导出用 Texture2D

        void Start()
        {
            if (variant == null)
                variant = VariantData.GetDefault();   // 如果未设置变体，使用默认变体

            GenerateAll();                             // 开始生成所有卡牌图片
        }

        /// <summary>
        /// 生成并导出所有可构建卡牌
        /// </summary>
        private async void GenerateAll()
        {
            QualitySettings.SetQualityLevel(QualitySettings.names.Length -1); // 设置最高画质

            // 初始化渲染纹理和导出纹理
            texture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
            export_texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
            texture.filterMode = FilterMode.Point;
            export_texture.filterMode = FilterMode.Point;
            render_cam.targetTexture = texture;
            render_cam.orthographicSize = height / 2;

            List<CardData> cards = CardData.GetAll(); // 获取所有卡牌数据
            for (int i = 0; i < cards.Count; i++)
            {
                CardData card = cards[i];
                if (card.deckbuilding) // 仅导出可构建卡牌
                {
                    ShowText("导出中: " + card.id);
                    GenerateCard(card);        // 渲染卡牌
                    await TimeTool.Delay(1);   // 延迟以确保渲染完成
                    ExportCard(card);          // 导出为 PNG
                    await TimeTool.Delay(2);   // 稍作延迟，防止阻塞
                }
            }

            ShowText("导出完成!");
        }

        /// <summary>
        /// 渲染单张卡牌
        /// </summary>
        /// <param name="card">卡牌数据</param>
        private void GenerateCard(CardData card)
        {
            card_ui.SetCard(card, variant); // 将卡牌数据设置到 UI
            render_cam.Render();            // 渲染摄像机画面
        }

        /// <summary>
        /// 将渲染好的卡牌导出为 PNG 图片
        /// </summary>
        /// <param name="card">卡牌数据</param>
        private void ExportCard(CardData card)
        {
            RenderTexture.active = texture; // 设置渲染纹理为当前活动纹理
            export_texture.ReadPixels(new Rect(0, 0, width, height), 0, 0); // 读取像素
            byte[] bytes = export_texture.EncodeToPNG(); // 编码为 PNG
            string file = card.id + ".png";             // 文件名
            File.WriteAllBytes(export_path + "/" + file, bytes); // 保存文件
            RenderTexture.active = null;                // 重置活动纹理
        }

        /// <summary>
        /// 打印导出信息到控制台
        /// </summary>
        /// <param name="txt">信息文本</param>
        private void ShowText(string txt)
        {
            Debug.Log(txt);
        }
    }
}

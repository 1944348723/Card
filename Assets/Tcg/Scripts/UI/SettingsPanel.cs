using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TcgEngine.UI
{
    /// <summary>
    /// 游戏设置面板
    /// 用于调整音量、画质、分辨率、窗口模式等游戏设置
    /// </summary>
    public class SettingsPanel : UIPanel
    {
        public string tab_group; // 面板所属的标签组
        public SliderDrag master_vol; // 总音量滑条
        public SliderDrag music_vol; // 音乐音量滑条
        public SliderDrag sfx_vol;   // 音效音量滑条
        public SliderDrag quality;   // 画质滑条
        public SliderDrag resolution; // 分辨率滑条
        public Toggle windowed;      // 窗口模式开关

        public Text master_vol_txt;  // 显示总音量数值
        public Text music_vol_txt;   // 显示音乐音量数值
        public Text sfx_vol_txt;     // 显示音效音量数值
        public Text quality_txt;     // 显示画质文本
        public Text resolution_txt;  // 显示分辨率文本

        public static HashSet<string> reso_hash = new HashSet<string>(); // 记录已存在的分辨率标签，避免重复
        public static List<Resolution> resolutions = new List<Resolution>(); // 可用分辨率列表
        private bool refreshing = false; // 是否正在刷新面板，防止触发回调

        private static SettingsPanel instance; // 单例实例

        protected override void Awake()
        {
            base.Awake();
            instance = this; // 设置单例
        }

        protected override void Start()
        {
            base.Start();

            // 设置滑条默认最小值和最大值
            master_vol.minValue = 0;
            master_vol.maxValue = 100;
            music_vol.minValue = 0;
            music_vol.maxValue = 100;
            sfx_vol.minValue = 0;
            sfx_vol.maxValue = 100;
            quality.minValue = 0;
            resolution.minValue = 0;

            // 注册滑条值变化事件
            master_vol.onValueChanged += RefreshText;
            music_vol.onValueChanged += RefreshText;
            sfx_vol.onValueChanged += RefreshText;
            quality.onValueChanged += RefreshText;
            resolution.onValueChanged += RefreshText;

            // 注册滑条拖动结束事件
            master_vol.onEndDrag += OnChangeAudio;
            music_vol.onEndDrag += OnChangeAudio;
            sfx_vol.onEndDrag += OnChangeAudio;
            quality.onEndDrag += OnChangeQuality;
            resolution.onEndDrag += OnChangeResolution;
            windowed.onValueChanged.AddListener(OnChangeWindowed);

            // 获取屏幕可用分辨率，去重后加入列表
            foreach (Resolution reso in Screen.resolutions)
            {
                string reso_tag = reso.width + "x" + reso.height;
                if (!reso_hash.Contains(reso_tag))
                {
                    resolutions.Add(reso);
                    reso_hash.Add(reso_tag);
                }
            }

            // 设置滑条最大值
            quality.maxValue = QualitySettings.names.Length - 1;
            resolution.maxValue = resolutions.Count - 1;

            // 注册标签按钮点击事件
            foreach (TabButton btn in TabButton.GetAll(tab_group))
                btn.onClick += OnClickTab;
        }

        /// <summary>
        /// 刷新面板显示的值，将游戏设置值同步到UI
        /// </summary>
        private void RefreshPanel()
        {
            refreshing = true; // 防止触发回调

            master_vol.value = AudioTool.Get().master_vol * 100f;
            music_vol.value = AudioTool.Get().music_vol * 100f;
            sfx_vol.value = AudioTool.Get().sfx_vol * 100f;

            int quality_value = QualitySettings.GetQualityLevel();
            int reso_value = GetResolutionIndex();
            bool windowed_value = !Screen.fullScreen;

            quality.value = quality_value;
            resolution.value = reso_value;
            windowed.isOn = windowed_value;

            refreshing = false;

            RefreshText(); // 更新显示文本
        }

        /// <summary>
        /// 刷新面板中显示的文字
        /// </summary>
        private void RefreshText()
        {
            master_vol_txt.text = master_vol.value.ToString();
            music_vol_txt.text = music_vol.value.ToString();
            sfx_vol_txt.text = sfx_vol.value.ToString();

            int quality_value = Mathf.RoundToInt(quality.value);
            quality_txt.text = QualitySettings.names[quality_value];

            int reso_value = Mathf.RoundToInt(resolution.value);
            if (resolutions.Count > 0)
            {
                Resolution resolu = resolutions[reso_value];
                string reso_tag = resolu.width + "x" + resolu.height + " " + Screen.currentResolution.refreshRate + "Hz";
                resolution_txt.text = reso_tag;
            }
        }

        /// <summary>
        /// 音量滑条拖动结束后调用，修改游戏音量
        /// </summary>
        private void OnChangeAudio()
        {
            if (!refreshing)
            {
                AudioTool.Get().master_vol = master_vol.value / 100f;
                AudioTool.Get().sfx_vol = sfx_vol.value / 100f;
                AudioTool.Get().music_vol = music_vol.value / 100f;
                AudioTool.Get().RefreshVolume();
                AudioTool.Get().SavePrefs();
                RefreshText();
            }
        }

        /// <summary>
        /// 画质滑条拖动结束后调用，修改画质
        /// </summary>
        private void OnChangeQuality()
        {
            if (!refreshing)
            {
                int quality_value = Mathf.RoundToInt(quality.value);
                QualitySettings.SetQualityLevel(quality_value);
                RefreshText();
            }
        }

        /// <summary>
        /// 分辨率滑条拖动结束后调用，修改屏幕分辨率
        /// </summary>
        private void OnChangeResolution()
        {
            if (!refreshing && resolutions.Count > 0)
            {
                int reso_value = Mathf.RoundToInt(resolution.value);
                Resolution resolu = resolutions[reso_value];
                Screen.SetResolution(resolu.width, resolu.height, !windowed.isOn);
                RefreshText();
            }
        }

        /// <summary>
        /// 窗口模式开关变化时调用
        /// </summary>
        private void OnChangeWindowed(bool val)
        {
            OnChangeResolution();
        }

        private void OnClickTab()
        {
            Hide();
        }

        public void OnClickOK()
        {
            Hide();
        }

        /// <summary>
        /// 获取当前屏幕分辨率在列表中的索引，选择最接近当前屏幕分辨率的值
        /// </summary>
        private int GetResolutionIndex()
        {
            int dist_min = 99999;
            int closest = 0;
            for (int i = 0; i < resolutions.Count; i++)
            {
                Resolution res = resolutions[i];
                int dist = Mathf.Abs(res.height - Screen.height) + Mathf.Abs(res.width - Screen.width);
                if (dist < dist_min)
                {
                    dist_min = dist;
                    closest = i;
                }
            }
            return closest;
        }

        public override void Show(bool instant = false)
        {
            base.Show(instant);
            RefreshPanel(); // 显示面板时刷新数据
        }

        public override void Hide(bool instant = false)
        {
            base.Hide(instant);
        }

        /// <summary>
        /// 获取单例实例
        /// </summary>
        public static SettingsPanel Get()
        {
            return instance;
        }
    }
}

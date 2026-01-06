using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine.Client;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

namespace TcgEngine.UI
{
    /// <summary>
    /// 游戏场景主UI脚本
    /// 管理游戏内的所有UI元素
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        public Canvas game_canvas;       // 游戏主画布
        public Canvas panel_canvas;      // 面板画布
        public Canvas top_canvas;        // 顶层画布
        public UIPanel menu_panel;       // 菜单面板
        public Text quit_btn;            // 退出按钮文本

        [Header("回合区域")]
        public Text turn_count;          // 回合数显示
        public Text turn_timer;          // 回合计时显示
        public Button end_turn_button;   // 结束回合按钮
        public Animator timeout_animator; // 超时动画
        public AudioClip timeout_audio;   // 超时音效

        private float selector_timer = 0f;   // 选择器刷新计时
        private float end_turn_timer = 0f;   // 结束回合按钮冷却计时
        private int prev_time_val = 0;       // 上一帧的时间值

        private static GameUI instance;      // 单例

        void Awake()
        {
            instance = this;

            // 设置画布摄像机
            if (game_canvas.worldCamera == null)
                game_canvas.worldCamera = Camera.main;
            if (panel_canvas.worldCamera == null)
                panel_canvas.worldCamera = Camera.main;
            if (top_canvas.worldCamera == null)
                top_canvas.worldCamera = Camera.main;
        }

        private void Start()
        {
            GameClient.Get().onGameStart += OnGameStart;
            GameClient.Get().onNewTurn += OnNewTurn;
            LoadPanel.Get().Show(true);
            BlackPanel.Get().Show(true);
            BlackPanel.Get().Hide();

            if (quit_btn != null)
                quit_btn.text = GameClient.game_settings.IsOnlinePlayer() ? "Resign" : "Quit";
        }

        void Update()
        {
            Game data = GameClient.Get().GetGameData();
            bool is_connecting = data == null || data.state == GameState.Connecting;
            bool connection_lost = !is_connecting && !GameClient.Get().IsReady();
            ConnectionPanel.Get().SetVisible(connection_lost);

            // 菜单切换
            if (Input.GetKeyDown(KeyCode.Escape))
                menu_panel.Toggle();

            if (!GameClient.Get().IsReady())
                return;

            bool yourturn = GameClient.Get().IsYourTurn();
            LoadPanel.Get().SetVisible(is_connecting && !data.HasStarted());
            end_turn_button.interactable = yourturn && end_turn_timer > 1f;
            end_turn_timer += Time.deltaTime;
            selector_timer += Time.deltaTime;

            // 回合计时显示
            turn_count.text = "Turn " + data.turn_count.ToString();
            turn_timer.enabled = data.turn_timer > 0f;
            turn_timer.text = Mathf.RoundToInt(data.turn_timer).ToString();
            turn_timer.enabled = data.turn_timer < 999f;

            // 模拟回合计时
            if (data.state == GameState.Play && data.turn_timer > 0f)
                data.turn_timer -= Time.deltaTime;

            // 回合剩余时间警告
            if (data.state == GameState.Play)
            {
                int val = Mathf.RoundToInt(data.turn_timer);
                int tick_val = 10;
                if (val < prev_time_val && val <= tick_val)
                    PulseFX();
                prev_time_val = val;
            }

            // 显示选择器面板
            foreach (SelectorPanel panel in SelectorPanel.GetAll())
            {
                bool should_show = panel.ShouldShow();
                if (should_show != panel.IsVisible() && selector_timer > 1f)
                {
                    selector_timer = 0f;
                    panel.SetVisible(should_show);

                    if (should_show)
                    {
                        AbilityData ability = AbilityData.Get(data.selector_ability_id);
                        Card caster = data.GetCard(data.selector_caster_uid);
                        panel.Show(ability, caster);
                    }
                }
            }

            // 隐藏选择器
            if (!yourturn && data.phase != GamePhase.Mulligan)
            {
                SelectorPanel.HideAll();
            }
        }

        /// <summary>
        /// 回合计时闪烁提示效果
        /// </summary>
        private void PulseFX()
        {
            timeout_animator?.SetTrigger("pulse");
            AudioTool.Get().PlaySFX("time", timeout_audio, 1f);
        }

        private void OnGameStart()
        {
            // 游戏开始时的逻辑
        }

        private void OnNewTurn(int player_id)
        {
            // 新回合时隐藏选择器
            CardSelector.Get().Hide();
            SelectTargetUI.Get().Hide();
        }

        public void OnClickNextTurn()
        {
            if (!Tutorial.Get().CanDo(TutoEndTrigger.EndTurn))
                return;

            GameClient.Get().EndTurn();
            end_turn_timer = 0f; // 立即禁用按钮
        }

        public void OnClickRestart()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void OnClickMenu()
        {
            menu_panel.Show();
        }

        public void OnClickBack()
        {
            menu_panel.Hide();
        }

        public void OnClickQuit()
        {
            bool online = GameClient.game_settings.IsOnlinePlayer();
            bool ended = GameClient.Get().HasEnded();
            if (online && !ended)
                GameClient.Get().Resign();
            else
                StartCoroutine(QuitRoutine("Menu"));
            menu_panel.Hide();
        }

        private IEnumerator QuitRoutine(string scene)
        {
            BlackPanel.Get().Show();
            AudioTool.Get().FadeOutMusic("music");
            AudioTool.Get().FadeOutSFX("ambience");
            AudioTool.Get().FadeOutSFX("ending_sfx");

            yield return new WaitForSeconds(1f);

            GameClient.Get().Disconnect();
            SceneNav.GoTo(scene);
        }

        public void OnClickSwapObserve()
        {
            int other = GameClient.Get().GetPlayerID() == 0 ? 1 : 0;
            GameClient.Get().SetObserverMode(other);
        }

        /// <summary>
        /// 判断是否有UI打开
        /// </summary>
        public static bool IsUIOpened()
        {
            return CardSelector.Get().IsVisible() || EndGamePanel.Get().IsVisible();
        }

        /// <summary>
        /// 判断鼠标是否悬停在UI上
        /// </summary>
        public static bool IsOverUI()
        {
            PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
            eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
            return results.Count > 0;
        }

        /// <summary>
        /// 判断鼠标是否悬停在指定Sorting Layer的UI上
        /// </summary>
        public static bool IsOverUILayer(string sorting_layer)
        {
            return IsOverUILayer(SortingLayer.NameToID(sorting_layer));
        }

        public static bool IsOverUILayer(int sorting_layer)
        {
            PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
            eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
            int count = 0;
            foreach (RaycastResult result in results)
            {
                if (result.sortingLayer == sorting_layer)
                    count++;
            }
            return count > 0;
        }

        /// <summary>
        /// 判断鼠标是否悬停在指定RectTransform区域
        /// </summary>
        public static bool IsOverRectTransform(Canvas canvas, RectTransform rect)
        {
            PointerEventData pevent = new PointerEventData(EventSystem.current);
            pevent.position = Input.mousePosition;

            List<RaycastResult> results = new List<RaycastResult>();
            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            raycaster.Raycast(pevent, results);

            foreach (RaycastResult result in results)
            {
                if (result.gameObject.transform == rect || result.gameObject.transform.IsChildOf(rect))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 将鼠标屏幕坐标转换为RectTransform本地坐标
        /// </summary>
        public static Vector2 MouseToRectPos(Canvas canvas, RectTransform rect, Vector2 screen_pos)
        {
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay && canvas.worldCamera != null)
            {
                Vector2 anchor_pos;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screen_pos, canvas.worldCamera, out anchor_pos);
                return anchor_pos;
            }
            else
            {
                Vector2 anchor_pos = screen_pos - new Vector2(rect.position.x, rect.position.y);
                anchor_pos = new Vector2(anchor_pos.x / rect.lossyScale.x, anchor_pos.y / rect.lossyScale.y);
                return anchor_pos;
            }
        }

        /// <summary>
        /// 将鼠标屏幕坐标转换为世界坐标
        /// </summary>
        public static Vector3 MouseToWorld(Vector2 mouse_pos, float distance = 10f)
        {
            Camera cam = GameCamera.Get() != null ? GameCamera.GetCamera() : Camera.main;
            Vector3 wpos = cam.ScreenToWorldPoint(new Vector3(mouse_pos.x, mouse_pos.y, distance));
            return wpos;
        }

        /// <summary>
        /// 格式化数字，添加千位分隔符
        /// </summary>
        public static string FormatNumber(int value)
        {
            return string.Format("{0:#,0}", value);
        }

        /// <summary>
        /// 获取单例
        /// </summary>
        public static GameUI Get()
        {
            return instance;
        }
    }
}

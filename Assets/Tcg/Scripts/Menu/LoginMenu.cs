using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TcgEngine.UI
{
    /// <summary>
    /// 登录菜单主脚本
    /// 控制登录界面、注册界面以及其他按钮操作
    /// </summary>
    public class LoginMenu : MonoBehaviour
    {
        [Header("Login")]
        public UIPanel login_panel;           // 登录面板
        public InputField login_user;         // 用户名输入框
        public InputField login_password;     // 密码输入框
        public Button login_button;           // 登录按钮
        public GameObject login_bottom;       // 登录界面底部 UI
        public Text error_msg;                // 错误提示文本

        [Header("Register")]
        public UIPanel register_panel;        // 注册面板
        public InputField register_username;  // 注册用户名输入框
        public InputField register_email;     // 注册邮箱输入框
        public InputField register_password;  // 注册密码输入框
        public InputField register_password_confirm; // 注册确认密码输入框
        public Button register_button;        // 注册按钮

        [Header("Other")]
        public GameObject test_area;          // 测试区域，用于测试模式下显示

        [Header("Music")]
        public AudioClip music;               // 登录界面背景音乐

        private bool clicked = false;         // 防止重复点击

        private static LoginMenu instance;    // 单例

        void Awake()
        {
            instance = this;
        }

        private void Start()
        {
            // 播放背景音乐
            AudioTool.Get().PlayMusic("music", music);
            BlackPanel.Get().Show(true);
            error_msg.text = "";
            test_area.SetActive(Authenticator.Get().IsTest());

            // 读取上次登录用户名
            string user = PlayerPrefs.GetString("tcg_last_user", "");
            login_user.text = user;

            // 测试模式下隐藏密码输入和底部 UI
            if (Authenticator.Get().IsTest())
            {
                login_password.gameObject.SetActive(false);
                login_bottom.SetActive(false);
            }
            else if (!string.IsNullOrEmpty(user))
            {
                SelectField(login_password); // 自动选中密码输入框
            }

            RefreshLogin(); // 尝试自动登录
        }

        void Update()
        {
            // 按钮可交互性检测
            login_button.interactable = !clicked && !string.IsNullOrWhiteSpace(login_user.text);
            register_button.interactable = !clicked && !string.IsNullOrWhiteSpace(register_username.text)
                && !string.IsNullOrWhiteSpace(register_email.text)
                && !string.IsNullOrWhiteSpace(register_password.text)
                && register_password.text == register_password_confirm.text;

            // 登录面板 Tab 和 Enter 键切换
            if (login_panel.IsVisible())
            {
                if (Input.GetKeyDown(KeyCode.Tab))
                {
                    if (login_user.isFocused)
                        SelectField(login_password);
                    else
                        SelectField(login_user);
                }

                if (Input.GetKeyDown(KeyCode.Return))
                {
                    if (login_button.interactable)
                        OnClickLogin();
                }
            }

            // 注册面板 Tab 和 Enter 键切换
            if (register_panel.IsVisible())
            {
                if (Input.GetKeyDown(KeyCode.Tab))
                {
                    if (register_username.isFocused)
                        SelectField(register_email);
                    else if (register_email.isFocused)
                        SelectField(register_password);
                    else if (register_password.isFocused)
                        SelectField(register_password_confirm);
                    else
                        SelectField(register_username);
                }

                if (Input.GetKeyDown(KeyCode.Return))
                {
                    if (register_button.interactable)
                        OnClickRegister();
                }
            }
        }

        /// <summary>
        /// 尝试刷新登录状态（自动登录）
        /// </summary>
        private async void RefreshLogin()
        {
            bool success = await Authenticator.Get().RefreshLogin();
            if (success)
            {
                SceneNav.GoTo("Menu"); // 自动跳转到主菜单
            }
            else
            {
                login_panel.Show();
                BlackPanel.Get().Hide();
            }
        }

        /// <summary>
        /// 登录操作
        /// </summary>
        private async void Login(string user, string password)
        {
            clicked = true;
            error_msg.text = "";

            bool success = await Authenticator.Get().Login(user, password);
            if (success)
            {
                PlayerPrefs.SetString("tcg_last_user", login_user.text);
                FadeToScene("Menu");
            }
            else
            {
                clicked = false;
                error_msg.text = Authenticator.Get().GetError();
            }
        }

        /// <summary>
        /// 注册操作
        /// </summary>
        private async void Register(string email, string user, string password)
        {
            clicked = true;
            error_msg.text = "";

            bool success = await Authenticator.Get().Register(register_email.text, register_username.text, register_password.text);
            if (success)
            {
                login_user.text = register_username.text;
                login_password.text = register_password.text;
                login_panel.Show();
                register_panel.Hide();
            }
            else
            {
                error_msg.text = Authenticator.Get().GetError();
            }
            clicked = false;
        }

        /// <summary>
        /// 点击登录按钮
        /// </summary>
        public void OnClickLogin()
        {
            if (string.IsNullOrWhiteSpace(login_user.text))
                return;
            if (clicked)
                return;

            Login(login_user.text, login_password.text);
        }

        /// <summary>
        /// 点击注册按钮
        /// </summary>
        public void OnClickRegister()
        {
            if (string.IsNullOrWhiteSpace(register_username.text))
                return;
            if (string.IsNullOrWhiteSpace(register_email.text))
                return;

            if (register_password.text != register_password_confirm.text)
                return;

            if (clicked)
                return;

            Register(register_email.text, register_username.text, register_password.text);
        }

        /// <summary>
        /// 切换到登录面板
        /// </summary>
        public void OnClickSwitchLogin()
        {
            login_panel.Show();
            register_panel.Hide();
            login_user.text = "";
            login_password.text = "";
            error_msg.text = "";
            SelectField(login_user);
        }

        /// <summary>
        /// 切换到注册面板
        /// </summary>
        public void OnClickSwitchRegister()
        {
            login_panel.Hide();
            register_panel.Show();
            error_msg.text = "";
            SelectField(register_username);
        }

        /// <summary>
        /// 打开密码重置面板
        /// </summary>
        public void OnClickSwitchReset()
        {
            RecoveryPanel.Get().Show();
        }

        /// <summary>
        /// 直接进入菜单场景
        /// </summary>
        public void OnClickGo()
        {
            FadeToScene("Menu");
        }

        /// <summary>
        /// 退出游戏
        /// </summary>
        public void OnClickQuit()
        {
            Application.Quit();
        }

        /// <summary>
        /// 选中输入框
        /// </summary>
        private void SelectField(InputField field)
        {
            if (!GameTool.IsMobile())
                field.Select();
        }

        /// <summary>
        /// 场景淡入淡出跳转
        /// </summary>
        public void FadeToScene(string scene)
        {
            StartCoroutine(FadeToRun(scene));
        }

        private IEnumerator FadeToRun(string scene)
        {
            BlackPanel.Get().Show();
            AudioTool.Get().FadeOutMusic("music");
            yield return new WaitForSeconds(1f);
            SceneNav.GoTo(scene);
        }

        /// <summary>
        /// 获取 LoginMenu 单例
        /// </summary>
        public static LoginMenu Get()
        {
            return instance;
        }
    }
}

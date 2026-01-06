using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TcgEngine.UI
{
    /// <summary>
    /// 登录菜单中的密码找回面板
    /// 仅在 API 模式下可用
    /// </summary>
    public class RecoveryPanel : UIPanel
    {
        public InputField reset_email;          // 输入用于重置密码的邮箱
        public Text reset_error;                // 显示重置密码错误信息

        public UIPanel confirm_panel;           // 确认密码重置面板
        public InputField confirm_code;         // 输入收到的验证码
        public InputField confirm_password;     // 输入新密码
        public InputField confirm_pass_confirm; // 确认新密码
        public Text confirm_error;              // 显示确认错误信息

        private static RecoveryPanel instance;  // 单例实例

        protected override void Awake()
        {
            base.Awake();
            instance = this;
        }

        /// <summary>
        /// 刷新面板状态，清空输入与错误信息
        /// </summary>
        public virtual void RefreshPanel()
        {
            confirm_panel.Hide(true);
            reset_email.text = "";
            confirm_code.text = "";
            confirm_password.text = "";
            confirm_pass_confirm.text = "";
            reset_error.text = "";
            confirm_error.text = "";
        }

        /// <summary>
        /// 发送密码重置请求
        /// </summary>
        public async void ResetPassword()
        {
            if (ApiClient.Get().IsBusy())  // 如果 API 正在忙，直接返回
                return;

            reset_error.text = "";

            if (reset_email.text.Length == 0)
                return;

            ResetPasswordRequest req = new ResetPasswordRequest();
            req.email = reset_email.text;

            string url = ApiClient.ServerURL + "/users/password/reset";
            string json = ApiTool.ToJson(req);
            WebResponse res = await ApiClient.Get().SendPostRequest(url, json);
            if (!res.success)
            {
                reset_error.text = res.error; // 显示错误信息
            }
            else
            {
                confirm_panel.Show(); // 显示确认面板
            }
        }

        /// <summary>
        /// 确认密码重置请求
        /// </summary>
        public async void ResetPasswordConfirm()
        {
            if (ApiClient.Get().IsBusy())
                return;

            confirm_error.text = "";

            // 检查输入完整性
            if (confirm_code.text.Length == 0 || confirm_password.text.Length == 0 || confirm_pass_confirm.text.Length == 0)
                return;

            // 检查两次密码是否一致
            if (confirm_password.text != confirm_pass_confirm.text)
            {
                confirm_error.text = "Passwords don't match"; // 密码不匹配
                return;
            }

            ResetConfirmPasswordRequest req = new ResetConfirmPasswordRequest();
            req.email = reset_email.text;
            req.code = confirm_code.text;
            req.password = confirm_password.text;

            string url = ApiClient.ServerURL + "/users/password/reset/confirm";
            string json = ApiTool.ToJson(req);
            WebResponse res = await ApiClient.Get().SendPostRequest(url, json);
            if (!res.success)
            {
                confirm_error.text = res.error; // 显示错误信息
            }
            else
            {
                // 成功后回到登录界面，并填写邮箱
                LoginMenu.Get().login_user.text = req.email;
                LoginMenu.Get().login_password.text = "";
                Hide();
            }
        }

        public override void Show(bool instant = false)
        {
            base.Show(instant);
            RefreshPanel(); // 显示时刷新面板
        }

        public override void Hide(bool instant = false)
        {
            base.Hide(instant);
            confirm_panel.Hide();
        }

        public void OnClickReset()
        {
            ResetPassword(); // 点击“重置密码”按钮
        }

        public void OnClickResetConfirm()
        {
            ResetPasswordConfirm(); // 点击“确认重置密码”按钮
        }

        public void OnClickBack()
        {
            Hide(); // 点击返回按钮
        }

        public static RecoveryPanel Get()
        {
            return instance; // 获取单例
        }
    }

    /// <summary>
    /// 密码重置请求数据
    /// </summary>
    [Serializable]
    public class ResetPasswordRequest
    {
        public string email; // 用户邮箱
    }

    /// <summary>
    /// 确认密码重置请求数据
    /// </summary>
    [Serializable]
    public class ResetConfirmPasswordRequest
    {
        public string email;    // 用户邮箱
        public string code;     // 验证码
        public string password; // 新密码
    } 
}

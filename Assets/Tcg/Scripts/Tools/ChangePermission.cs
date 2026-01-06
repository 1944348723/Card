using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine.UI;

namespace TcgEngine
{
    /// <summary>
    /// 用户权限修改工具脚本
    /// 用于登录管理员账号并修改指定用户的权限等级（会覆盖原有权限）
    /// </summary>
    public class ChangePermission : MonoBehaviour
    {
        public string username = "admin"; // 默认管理员用户名

        [Header("登录")]
        public InputField username_txt;  // 用户名输入框
        public InputField password_txt;  // 密码输入框

        [Header("修改权限")]
        public UIPanel permission_panel; // 权限修改面板
        public InputField target_user_txt; // 目标用户名输入框
        public InputField target_perm_txt; // 目标权限等级输入框
        public Text error;                // 错误或提示信息显示

        private string logged_user;       // 当前登录的管理员用户名

        void Start()
        {
            username_txt.text = username; // 初始化用户名输入框
            error.text = "";
        }

        /// <summary>
        /// 管理员登录
        /// </summary>
        private async void Login(string user, string pass)
        {
            LoginResponse res = await ApiClient.Get().Login(user, pass);
            if (res.success && res.permission_level >= 10)
            {
                logged_user = user;
                permission_panel.Show(); // 显示权限修改面板
            }
            else if (res.success)
            {
                error.text = "非管理员用户"; // 登录成功但权限不足
            }
            else
            {
                error.text = res.error; // 登录失败显示错误
            }
        }

        /// <summary>
        /// 获取指定用户名的用户ID
        /// </summary>
        private async Task<string> GetUserID(string tuser)
        {
            string url = ApiClient.ServerURL + "/users/" + tuser;
            WebResponse res = await ApiClient.Get().SendGetRequest(url);
            UserData udata = ApiTool.JsonToObject<UserData>(res.data);
            if (!res.success)
                error.text = res.error;

            return res.success ? udata.id : null;
        }

        /// <summary>
        /// 修改指定用户权限
        /// </summary>
        private async void SetPermission(string tuser, int permission)
        {
            string user_id = await GetUserID(tuser);
            if (user_id == null)
                return;

            ChangePermissionRequest req = new ChangePermissionRequest();
            req.permission_level = permission;

            string url = ApiClient.ServerURL + "/users/permission/edit/" + user_id;
            string json = ApiTool.ToJson(req);
            WebResponse res = await ApiClient.Get().SendPostRequest(url, json);

            if (!res.success)
                error.text = res.error;

            if (res.success)
            {
                error.text = "修改成功！";
                error.color = Color.green;
            }
        }

        /// <summary>
        /// 点击登录按钮事件
        /// </summary>
        public void OnClickLogin()
        {
            if (string.IsNullOrEmpty(username_txt.text))
                return;

            if (string.IsNullOrEmpty(password_txt.text))
                return;

            error.text = "";
            error.color = Color.red;
            Login(username_txt.text, password_txt.text);
        }

        /// <summary>
        /// 点击更新权限按钮事件
        /// </summary>
        public void OnClickUpdate()
        {
            if (string.IsNullOrEmpty(target_user_txt.text))
                return;

            bool success = int.TryParse(target_perm_txt.text, out int perm);
            if (!success)
                return;

            if (logged_user == target_user_txt.text)
                return; // 防止修改自己权限

            error.text = "";
            error.color = Color.red;
            SetPermission(target_user_txt.text, perm);
        }
    }

    #region 请求类
    [System.Serializable]
    public class ChangePermissionRequest
    {
        public int permission_level; // 设置的权限等级
    }
    #endregion
}

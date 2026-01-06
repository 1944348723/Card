using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 该认证器需要依赖外部的 UserLogin API 资源
    /// 它通过真实的 Web API 与服务器数据库交互
    /// 用于进行真正的在线账号登录 / 注册 / 刷新 等操作
    /// </summary>
    public class AuthenticatorApi : Authenticator
    {
        private int permission = 0; // 当前用户权限等级（由服务器返回）

        public override async Task Initialize()
        {
            await base.Initialize();   // 仅调用父类初始化
        }

        /// <summary>
        /// 使用用户名 + 密码登录（真实线上认证）
        /// 登录成功后：
        /// - 标记 logged_in = true
        /// - 保存 user_id
        /// - 保存 username
        /// - 保存权限等级
        /// </summary>
        public override async Task<bool> Login(string username, string password)
        {
            LoginResponse res = await Client.Login(username, password);
            if (res.success)
            {
                this.logged_in = true;
                this.user_id = res.id;
                this.username = res.username;
                permission = res.permission_level;
            }
            return res.success;
        }

        /// <summary>
        /// 刷新登录状态（基于服务器 Token 刷新）
        /// 如果成功，会重新更新用户信息
        /// </summary>
        public override async Task<bool> RefreshLogin()
        {
            LoginResponse res = await Client.RefreshLogin();
            if (res.success)
            {
                this.logged_in = true;
                this.user_id = res.id;
                this.username = res.username;
            }
            return res.success;
        }

        /// <summary>
        /// 注册新账号（用户名 + 邮箱 + 密码）
        /// 注册成功后会自动执行一次登录
        /// </summary>
        public override async Task<bool> Register(string username, string email, string password)
        {
            RegisterResponse res = await Client.Register(username, email, password);

            if (res.success)
                await Login(username, password);

            return res.success;
        }

        /// <summary>
        /// 从服务器加载用户数据（如金币、背包、进度等）
        /// </summary>
        public override async Task<UserData> LoadUserData()
        {
            UserData res = await Client.LoadUserData();
            return res;
        }

        /// <summary>
        /// 保存用户数据
        /// 说明：
        /// API 模式下，数据通常在每次请求时就已经持久化
        /// 因此这里不需要额外保存到本地
        /// </summary>
        public override async Task<bool> SaveUserData()
        {
            // 不做任何处理，数据已由 API 自动持久化，无需保存到磁盘
            await Task.Yield();
            return false;
        }

        /// <summary>
        /// 登出
        /// 调用父类清理本地数据
        /// 并通知服务器注销会话
        /// </summary>
        public override void Logout()
        {
            base.Logout();
            Client.Logout();
            permission = 0;
        }

        /// <summary>
        /// 获取当前用户数据
        /// 直接从 ApiClient 读取
        /// </summary>
        public override UserData GetUserData()
        {
            return Client.UserData;
        }

        /// <summary>
        /// 是否已登录（以 API 客户端状态为准）
        /// </summary>
        public override bool IsSignedIn()
        {
            return Client.IsLoggedIn();
        }

        /// <summary>
        /// 登录是否已过期（服务器 Token 判断）
        /// </summary>
        public override bool IsExpired()
        {
            return Client.IsExpired();
        }

        /// <summary>
        /// 获取用户权限等级
        /// </summary>
        public override int GetPermission()
        {
            return permission;
        }

        /// <summary>
        /// 获取最近一次 API 错误信息
        /// </summary>
        public override string GetError()
        {
            return Client.GetLastError();
        }

        /// <summary>
        /// 快捷访问 ApiClient
        /// </summary>
        public ApiClient Client { get { return ApiClient.Get(); } }

    }
}

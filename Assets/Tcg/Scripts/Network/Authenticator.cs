using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 所有认证器（Authenticator）的基类，必须被继承使用
    /// 用于统一处理用户登录 / 注册 / 会话刷新 / 用户数据加载 等操作
    /// 不直接实例化，而是由子类实现具体逻辑（例如本地测试登录或API登录）
    /// </summary>
    public abstract class Authenticator
    {
        protected string user_id = null;      // 用户唯一ID
        protected string username = null;     // 用户名
        protected bool logged_in = false;     // 是否已登录
        protected bool inited = false;        // 是否已初始化

        // 初始化认证器（默认什么都不做，仅标记为已初始化）
        public virtual async Task Initialize()
        {
            inited = true;
            await Task.Yield(); // 什么也不做，仅用于保持异步结构
        }

        // 登录（只提供用户名的版本，默认返回失败，子类必须重写）
        public virtual async Task<bool> Login(string username)
        {
            await Task.Yield(); // 什么也不做
            return false;
        }

        // 登录（用户名 + token 版本，默认调用上面的 Login(username)
        // 某些认证器不需要 token，因此复用逻辑
        public virtual async Task<bool> Login(string username, string token)
        {
            return await Login(username); 
        }

        // 刷新登录状态（默认与 Login(username) 行为一致）
        public virtual async Task<bool> RefreshLogin()
        {
            return await Login(username);
        }

        // 绕过真实登录流程，直接指定用户（用于测试）
        public virtual void LoginTest(string username)
        {
            this.user_id = username;
            this.username = username;
            logged_in = true;
        }

        // 注册（用户名 + 邮箱 + token）
        // 默认行为：注册完成后执行登录
        public virtual async Task<bool> Register(string username, string email, string token)
        {
            return await Login(username, token);
        }

        // 加载用户数据（默认不做任何事，返回 null，需子类实现）
        public virtual async Task<UserData> LoadUserData()
        {
            await Task.Yield();
            return null;
        }

        // 保存用户数据（默认不做任何操作，返回 false）
        public virtual async Task<bool> SaveUserData()
        {
            await Task.Yield();
            return false;
        }

        // 登出：清空所有用户信息与状态
        public virtual void Logout()
        {
            logged_in = false;
            user_id = null;
            username = null;
        }

        // 是否已经初始化
        public virtual bool IsInited()
        {
            return inited;
        }

        // 是否仍然处于有效连接状态（登录且未过期）
        public virtual bool IsConnected()
        {
            return IsSignedIn() && !IsExpired();
        }

        // 是否已登录（注意：即使登录过期也可能仍为 true）
        public virtual bool IsSignedIn()
        {
            return logged_in;
        }

        // 登录是否已过期（默认不过期，子类自行实现）
        public virtual bool IsExpired()
        {
            return false;
        }

        // 获取用户ID
        public virtual string GetUserId()
        {
            return user_id;
        }

        // 获取用户名
        public virtual string GetUsername()
        {
            return username;
        }

        // 返回权限等级（默认：已登录=1，未登录=0；可被子类扩展为更复杂权限系统）
        public virtual int GetPermission()
        {
            return logged_in ? 1 : 0;
        }

        // 获取用户数据（默认无数据）
        public virtual UserData GetUserData()
        {
            return null;
        }

        // 获取最新错误消息（默认返回空字符串）
        public virtual string GetError()
        {
            return "";
        }

        // 是否是本地测试登录模式（不走真实服务器）
        public bool IsTest()
        {
            return NetworkData.Get().auth_type == AuthenticatorType.LocalSave;
        }

        // 是否是API在线认证模式
        public bool IsApi()
        {
            return NetworkData.Get().auth_type == AuthenticatorType.Api;
        }

        // 一些便捷访问属性
        public string UserID{ get{ return GetUserId(); }}
        public string Username{ get { return GetUsername(); } }
        public UserData UserData{ get { return GetUserData(); } }

        // 工厂方法：根据认证方式创建不同的 Authenticator 实例
        public static Authenticator Create(AuthenticatorType type)
        {
            if (type == AuthenticatorType.Api)
                return new AuthenticatorApi();     // 在线认证
            else
                return new AuthenticatorLocal();   // 本地测试认证
        }

        // 获取当前网络系统正在使用的认证器
        public static Authenticator Get()
        {
            return TcgNetwork.Get().Auth;
        }
    }

    /// <summary>
    /// 认证器类型
    /// LocalSave：本地假登录，主要用于快速测试，无需联网
    /// Api：真实在线登录方式，依赖服务器认证
    /// </summary>
    public enum AuthenticatorType
    {
        LocalSave = 0,   // 测试模式，本地假登录，不需要每次输入账号
        Api = 10,        // 实际在线登录
    }
}

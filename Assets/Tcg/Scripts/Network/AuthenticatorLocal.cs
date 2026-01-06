using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 测试用认证器（本地模式）
    /// - 只生成一个本地用户 ID 作为登录身份
    /// - 非常适合多人联机测试，无需每次输入账号密码
    /// - 但 Unity Services 功能在本地测试模式下不可用（Relay、Cloud Save 等）
    /// - 如果需要测试 Unity Services，请使用 Anonymous 匿名模式（需先在 Services 窗口连接项目 ID）
    /// </summary>
    public class AuthenticatorLocal : Authenticator
    {
        private UserData udata = null;   // 本地缓存的用户数据

        /// <summary>
        /// 本地登录
        /// 直接使用传入的 username 作为：
        /// - user_id
        /// - username
        /// 登录必定成功
        /// 并把用户名写入 PlayerPrefs 作为下次自动登录的依据
        /// </summary>
        public override async Task<bool> Login(string username)
        {
            this.user_id = username;  // 使用用户名作为唯一ID，确保测试时存档一致
            this.username = username;
            logged_in = true;

            await Task.Yield(); // 占位，让接口保持异步结构

            PlayerPrefs.SetString("tcg_user", username); // 记录上次登录的用户名
            return true;
        }

        /// <summary>
        /// 刷新登录
        /// - 从 PlayerPrefs 读取上次登录用户
        /// - 如果存在，则自动重新登录
        /// </summary>
        public override async Task<bool> RefreshLogin()
        {
            string username = PlayerPrefs.GetString("tcg_user", "");
            if (!string.IsNullOrEmpty(username))
            {
                bool success = await Login(username);
                return success;
            }
            return false;
        }

        /// <summary>
        /// 从本地加载用户数据
        /// - 先读取 PlayerPrefs 用户名
        /// - 判断是否存在对应存档文件：username.user
        /// - 如果存在则读取
        /// - 若不存在则创建一个新的 UserData
        /// </summary>
        public override async Task<UserData> LoadUserData()
        {
            string user = PlayerPrefs.GetString("tcg_user", "");
            string file = username + ".user";

            // 如果存档存在，则读取
            if (!string.IsNullOrEmpty(user) && SaveTool.DoesFileExist(file))
            {
                udata = SaveTool.LoadFile<UserData>(file);
            }

            // 如果没有存档，则创建默认数据
            if (udata == null)
            {
                udata = new UserData();
                udata.username = username;
                udata.id = username;
            }

            await Task.Yield(); // 保持异步
            return udata;
        }

        /// <summary>
        /// 保存用户数据到本地文件
        /// - 存档文件名：username.user
        /// - 仅当 udata 存在且用户名合法时保存
        /// </summary>
        public override async Task<bool> SaveUserData()
        {
            if (udata != null && SaveTool.IsValidFilename(username))
            {
                string file = username + ".user";
                SaveTool.SaveFile<UserData>(file, udata);

                await Task.Yield(); // 保持异步
                return true;
            }
            return false;
        }

        /// <summary>
        /// 本地登出
        /// - 调用父类 Logout 清理基础信息
        /// - 清空本地用户数据
        /// - 删除 PlayerPrefs 里的保存用户名
        /// </summary>
        public override void Logout()
        {
            base.Logout();
            udata = null;
            PlayerPrefs.DeleteKey("tcg_user");
        }

        /// <summary>
        /// 获取当前本地用户数据
        /// </summary>
        public override UserData GetUserData()
        {
            return udata;
        }
    }
}

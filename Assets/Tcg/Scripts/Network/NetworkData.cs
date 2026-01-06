using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 所有网络相关设置的主配置文件（ScriptableObject）
    /// 
    /// 说明：
    /// - 用于配置游戏服务器地址、端口、API 地址、认证方式等网络参数
    /// - 注意：服务器 API 的密码不会存储在这里！
    ///   它被放在 Server 场景中，以避免被打包到客户端导致泄露
    /// </summary>
    [CreateAssetMenu(fileName = "NetworkData", menuName = "TcgEngine/NetworkData", order = 0)]
    public class NetworkData : ScriptableObject
    {
        [Header("Game Server")]
        public string url;                      // 游戏服务器地址（域名或 IP）
        public ushort port;                     // 游戏服务器监听或连接端口

        [Header("API")]
        public string api_url;                  // NodeJS API 服务器地址（可与游戏服务器相同）
        public bool api_https;                  // 是否启用 HTTPS（true = https，false = http）
        // http 默认使用 80 端口
        // https 默认使用 443 端口

        [Header("Settings")]
        public SoloType solo_type;              // 单人模式是否启用 Netcode
        // - 启用 Netcode：单机行为更接近联机模式
        // - 关闭 Netcode：完全离线运行（WebGL 必须使用该模式）
        public AuthenticatorType auth_type;     // 登录认证模式
        // - LocalSave：本地测试模式
        // - Api：真实在线登录模式
        
        /// <summary>
        /// 获取当前全局 NetworkData 配置实例
        /// </summary>
        public static NetworkData Get()
        {
            return TcgNetwork.Get().data;
        }
    }

    /// <summary>
    /// 单人模式网络行为类型
    /// </summary>
    public enum SoloType
    {
        UseNetcode = 0,     // 单机模式依然使用 Netcode 消息系统
        // 目的：让单机和多人行为尽可能一致，推荐
        Offline = 10        // 单机完全离线模式（无 Netcode）
        // 注意：可能与联机模式行为存在差异
        // 并且 WebGL 必须使用该模式（因为 WebGL 不支持 StartHost）
    }
}
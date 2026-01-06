using System.Collections;
using System.Collections.Generic;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace TcgEngine
{
    // UnityTransport 封装类
    // 方便以后替换为 WebSocketTransport（例如构建 WebGL 时）
    public class TcgTransport : MonoBehaviour
    {
        // UnityTransport 组件
        private UnityTransport transport;

        // 监听所有地址
        private const string listen_all = "0.0.0.0";

        // 初始化 Transport
        public virtual void Init()
        {
            transport = GetComponent<UnityTransport>();
        }

        // 设置为服务器模式
        public virtual void SetServer(ushort port)
        {
            transport.ConnectionData.ServerListenAddress = listen_all;
            transport.SetConnectionData(listen_all, port);
            // 如果需要证书，可在此设置
            // transport.SetServerSecrets(cert, key);
        }

        // 设置为客户端模式
        public virtual void SetClient(string address, ushort port)
        {
            string ip = NetworkTool.HostToIP(address);
            transport.SetConnectionData(ip, port);
            // 如果需要证书，可在此设置
            // transport.SetClientSecrets(address, chain);
        }

        // 获取当前地址
        public virtual string GetAddress() { return transport.ConnectionData.Address; }

        // 获取当前端口
        public virtual ushort GetPort() { return transport.ConnectionData.Port; }
    }
}
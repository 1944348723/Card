using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 网络消息发送与接收的基础类
    /// 负责：
    /// 1. 注册/取消网络消息监听
    /// 2. 接收来自客户端或服务器的消息
    /// 3. 提供多种发送接口（字符串、字节、基本类型、自定义结构体）
    /// 4. 支持单播与多播
    /// </summary>
    public class NetworkMessaging
    {
        // 网络系统核心对象
        private TcgNetwork network;

        // 保存消息类型与回调函数的映射表
        // key = 消息类型字符串
        // value = 当该类型消息到达时触发的回调
        private Dictionary<string, System.Action<ulong, FastBufferReader>> msg_dict = new Dictionary<string, System.Action<ulong, FastBufferReader>>();

        public NetworkMessaging(TcgNetwork network)
        {
            this.network = network;

            // 当网络连接建立时，自动重新注册所有消息监听
            network.onConnect += OnConnect;
        }

        // 当网络成功连接时调用
        // 重新注册之前已经监听过的所有消息类型
        private void OnConnect()
        {
            foreach (KeyValuePair<string, System.Action<ulong, FastBufferReader>> pair in msg_dict)
            {
                RegisterNetMsg(pair.Key, pair.Value);
            }
        }

        /// <summary>
        /// 监听某种消息类型
        /// type = 消息标识字符串
        /// callback = 收到消息时调用
        /// </summary>
        public void ListenMsg(string type, System.Action<ulong, FastBufferReader> callback)
        {
            msg_dict[type] = callback;
            RegisterNetMsg(type, callback);
        }

        /// <summary>
        /// 取消监听某种消息类型
        /// </summary>
        public void UnListenMsg(string type)
        {
            msg_dict.Remove(type);

            if (network.NetworkManager.CustomMessagingManager != null)
                network.NetworkManager.CustomMessagingManager.UnregisterNamedMessageHandler(type);
        }

        /// <summary>
        /// 真正向 Netcode 注册监听
        /// 只有在网络在线时才允许注册
        /// </summary>
        private void RegisterNetMsg(string type, System.Action<ulong, FastBufferReader> callback)
        {
            if (IsOnline)
            {
                network.NetworkManager.CustomMessagingManager.RegisterNamedMessageHandler(type, (ulong client_id, FastBufferReader reader) =>
                {
                    ReceiveNetMessage(type, client_id, reader);
                });
            }
        }

        /// <summary>
        /// 接收到网络消息时触发
        /// 会根据消息类型在字典中查找对应回调
        /// </summary>
        private void ReceiveNetMessage(string type, ulong client_id, FastBufferReader reader)
        {
            bool valid = msg_dict.TryGetValue(type, out System.Action<ulong, FastBufferReader> callback);
            if (valid && IsOnline)
            {
                callback(client_id, reader);
            }
        }

        //---------------- 单个目标发送 ----------------

        /// <summary>
        /// 发送空消息（不带内容）
        /// </summary>
        public void SendEmpty(string type, ulong target, NetworkDelivery delivery)
        {
            using FastBufferWriter writer = new(0, Allocator.Temp);
            Send(type, target, writer, delivery);
        }

        /// <summary>
        /// 发送字节数组
        /// </summary>
        public void SendBytes(string type, ulong target, byte[] msg, NetworkDelivery delivery)
        {
            using FastBufferWriter writer = new(msg.Length, Allocator.Temp);
            writer.WriteBytesSafe(msg, msg.Length);
            Send(type, target, writer, delivery);
        }

        /// <summary>
        /// 发送字符串消息
        /// </summary>
        public void SendString(string type, ulong target, string msg, NetworkDelivery delivery)
        {
            using FastBufferWriter writer = new(msg.Length, Allocator.Temp, TcgNetwork.MsgSizeMax);
            writer.WriteValueSafe(msg);
            Send(type, target, writer, delivery);
        }

        /// <summary>
        /// 发送 int
        /// </summary>
        public void SendInt(string type, ulong target, int data, NetworkDelivery delivery)
        {
            using FastBufferWriter writer = new(4, Allocator.Temp);
            writer.WriteValueSafe(data);
            Send(type, target, writer, delivery);
        }

        /// <summary>
        /// 发送 ulong
        /// </summary>
        public void SendUInt64(string type, ulong target, ulong data, NetworkDelivery delivery)
        {
            using FastBufferWriter writer = new(8, Allocator.Temp);
            writer.WriteValueSafe(data);
            Send(type, target, writer, delivery);
        }

        /// <summary>
        /// 发送 float
        /// </summary>
        public void SendFloat(string type, ulong target, float data, NetworkDelivery delivery)
        {
            using FastBufferWriter writer = new(4, Allocator.Temp);
            writer.WriteValueSafe(data);
            Send(type, target, writer, delivery);
        }

        /// <summary>
        /// 发送自定义可序列化对象
        /// 必须实现 INetworkSerializable
        /// </summary>
        public void SendObject<T>(string type, ulong target, T data, NetworkDelivery delivery) where T : INetworkSerializable
        {
            using FastBufferWriter writer = new(256, Allocator.Temp, TcgNetwork.MsgSizeMax);
            writer.WriteNetworkSerializable(data);
            Send(type, target, writer, delivery);
        }

        /// <summary>
        /// 发送只包含协议标签的消息。
        /// </summary>
        public void SendTagged(string type, ulong target, ushort tag, NetworkDelivery delivery)
        {
            using FastBufferWriter writer = new(128, Allocator.Temp, TcgNetwork.MsgSizeMax);
            writer.WriteValueSafe(tag);
            Send(type, target, writer, delivery);
        }

        /// <summary>
        /// 发送协议标签和可网络序列化的载荷。
        /// </summary>
        public void SendTagged<T>(string type, ulong target, ushort tag, T data, NetworkDelivery delivery)
            where T : INetworkSerializable
        {
            using FastBufferWriter writer = new(128, Allocator.Temp, TcgNetwork.MsgSizeMax);
            writer.WriteValueSafe(tag);
            writer.WriteNetworkSerializable(data);
            Send(type, target, writer, delivery);
        }

        /// <summary>
        /// 发送协议标签和整数载荷。
        /// </summary>
        public void SendTagged(string type, ulong target, ushort tag, int data, NetworkDelivery delivery)
        {
            using FastBufferWriter writer = new(128, Allocator.Temp, TcgNetwork.MsgSizeMax);
            writer.WriteValueSafe(tag);
            writer.WriteValueSafe(data);
            Send(type, target, writer, delivery);
        }

        //---------------- 多个目标（群发 / 广播，仅服务器可用） ----------------

        /// <summary>
        /// 向多个目标发送只包含协议标签的消息。
        /// </summary>
        public void SendTagged(string type, IReadOnlyList<ulong> targets, ushort tag, NetworkDelivery delivery)
        {
            if (!IsServer || targets == null || targets.Count == 0)
                return;

            using FastBufferWriter writer = new(128, Allocator.Temp, TcgNetwork.MsgSizeMax);
            writer.WriteValueSafe(tag);
            Send(type, targets, writer, delivery);
        }

        /// <summary>
        /// 向多个目标发送协议标签和可网络序列化的载荷。
        /// </summary>
        public void SendTagged<T>(string type, IReadOnlyList<ulong> targets, ushort tag, T data, NetworkDelivery delivery)
            where T : INetworkSerializable
        {
            if (!IsServer || targets == null || targets.Count == 0)
                return;

            using FastBufferWriter writer = new(128, Allocator.Temp, TcgNetwork.MsgSizeMax);
            writer.WriteValueSafe(tag);
            writer.WriteNetworkSerializable(data);
            Send(type, targets, writer, delivery);
        }

        /// <summary>
        /// 群发空消息
        /// </summary>
        public void SendEmpty(string type, IReadOnlyList<ulong> targets, NetworkDelivery delivery)
        {
            if (IsServer)
            {
                using FastBufferWriter writer = new(0, Allocator.Temp);
                Send(type, targets, writer, delivery);
            }
        }

        /// <summary>
        /// 群发字节数组
        /// </summary>
        public void SendBytes(string type, IReadOnlyList<ulong> targets, byte[] msg, NetworkDelivery delivery)
        {
            if (IsServer)
            {
                using FastBufferWriter writer = new(msg.Length, Allocator.Temp);
                writer.WriteBytesSafe(msg, msg.Length);
                Send(type, targets, writer, delivery);
            }
        }

        /// <summary>
        /// 群发字符串
        /// </summary>
        public void SendString(string type, IReadOnlyList<ulong> targets, string msg, NetworkDelivery delivery)
        {
            if (IsServer)
            {
                using FastBufferWriter writer = new(msg.Length, Allocator.Temp, TcgNetwork.MsgSizeMax);
                writer.WriteValueSafe(msg);
                Send(type, targets, writer, delivery);
            }
        }

        /// <summary>
        /// 群发 int
        /// </summary>
        public void SendInt(string type, IReadOnlyList<ulong> targets, int data, NetworkDelivery delivery)
        {
            if (IsServer)
            {
                using FastBufferWriter writer = new(4, Allocator.Temp);
                writer.WriteValueSafe(data);
                Send(type, targets, writer, delivery);
            }
        }

        /// <summary>
        /// 群发 ulong
        /// </summary>
        public void SendUInt64(string type, IReadOnlyList<ulong> targets, ulong data, NetworkDelivery delivery)
        {
            if (IsServer)
            {
                using FastBufferWriter writer = new(8, Allocator.Temp);
                writer.WriteValueSafe(data);
                Send(type, targets, writer, delivery);
            }
        }

        /// <summary>
        /// 群发 float
        /// </summary>
        public void SendFloat(string type, IReadOnlyList<ulong> targets, float data, NetworkDelivery delivery)
        {
            if (IsServer)
            {
                using FastBufferWriter writer = new(4, Allocator.Temp);
                writer.WriteValueSafe(data);
                Send(type, targets, writer, delivery);
            }
        }
        
        public void SendObject<T>(string type, IReadOnlyList<ulong> targets, T data, NetworkDelivery delivery) where T : INetworkSerializable
        {
            // 群发自定义对象（必须实现 INetworkSerializable）
            // 仅服务器允许发送
            if (IsServer)
            {
                using FastBufferWriter writer = new(256, Allocator.Temp, TcgNetwork.MsgSizeMax);
                writer.WriteNetworkSerializable(data);
                Send(type, targets, writer, delivery);
            }
        }

        //--------- Send All ----------
        // 发送给所有客户端（广播）

        public void SendEmptyAll(string type, NetworkDelivery delivery)
        {
            // 发送一个没有内容的空消息
            if (IsServer)
            {
                using FastBufferWriter writer = new(0, Allocator.Temp);
                SendAll(type, writer, delivery);
            }
        }

        public void SendStringAll(string type, string msg, NetworkDelivery delivery)
        {
            // 广播字符串
            if (IsServer)
            {
                using FastBufferWriter writer = new(msg.Length, Allocator.Temp, TcgNetwork.MsgSizeMax);
                writer.WriteValueSafe(msg);
                SendAll(type, writer, delivery);
            }
        }

        public void SendIntAll(string type, int data, NetworkDelivery delivery)
        {
            // 广播 int
            if (IsServer)
            {
                using FastBufferWriter writer = new(4, Allocator.Temp);
                writer.WriteValueSafe(data);
                SendAll(type, writer, delivery);
            }
        }

        public void SendUInt64All(string type, ulong data, NetworkDelivery delivery)
        {
            // 广播 ulong
            if (IsServer)
            {
                using FastBufferWriter writer = new(8, Allocator.Temp);
                writer.WriteValueSafe(data);
                SendAll(type, writer, delivery);
            }
        }

        public void SendFloatAll(string type, float data, NetworkDelivery delivery)
        {
            // 广播 float
            if (IsServer)
            {
                using FastBufferWriter writer = new(4, Allocator.Temp);
                writer.WriteValueSafe(data);
                SendAll(type, writer, delivery);
            }
        }

        public void SendBytesAll(string type, byte[] msg, NetworkDelivery delivery)
        {
            // 广播字节数组
            if (IsServer)
            {
                using FastBufferWriter writer = new(msg.Length, Allocator.Temp);
                writer.WriteBytesSafe(msg, msg.Length);
                SendAll(type, writer, delivery);
            }
        }

        public void SendObjectAll<T>(string type, T data, NetworkDelivery delivery) where T : INetworkSerializable
        {
            // 广播自定义对象（结构体 / 数据对象）
            if (IsServer)
            {
                using FastBufferWriter writer = new(256, Allocator.Temp, TcgNetwork.MsgSizeMax);
                writer.WriteNetworkSerializable(data);
                SendAll(type, writer, delivery);
            }
        }

        //-------- Generic Send ----------
        // 以下为通用发送接口（内部使用）
        // 根据在线 / 离线模式决定走网络还是本地模拟

        public void Send(string type, ulong target, FastBufferWriter writer, NetworkDelivery delivery)
        {
            // 在线模式：真正通过网络发送
            // 离线模式：如果目标是本地客户端，则直接本地触发回调
            if (IsOnline)
                SendOnline(type, target, writer, delivery);
            else if(target == ClientID)
                SendOffline(type, writer);
        }

        public void Send(string type, IReadOnlyList<ulong> targets, FastBufferWriter writer, NetworkDelivery delivery)
        {
            // 群发版本
            if (IsOnline)
                SendOnline(type, targets, writer, delivery);
            else if (Contains(targets, ClientID))
                SendOffline(type, writer);
        }

        // 广播（内部调用群发）
        public void SendAll(string type, FastBufferWriter writer, NetworkDelivery delivery)
        {
            Send(type, ClientList, writer, delivery);
        }

        // 实际在线发送（单播）
        private void SendOnline(string type, ulong target, FastBufferWriter writer, NetworkDelivery delivery)
        {
            network.NetworkManager.CustomMessagingManager.SendNamedMessage(type, target, writer, delivery);
        }

        // 实际在线发送（群发）
        private void SendOnline(string type, IReadOnlyList<ulong> targets, FastBufferWriter writer, NetworkDelivery delivery)
        {
            network.NetworkManager.CustomMessagingManager.SendNamedMessage(type, targets, writer, delivery);
        }

        // 离线模式发送
        // 本质上是：把 writer 内容复制到 reader，然后直接触发本地 callback
        private void SendOffline(string type, FastBufferWriter writer)
        {
            bool found = msg_dict.TryGetValue(type, out System.Action<ulong, FastBufferReader> callback);
            if (found)
            {
                using FastBufferReader reader = new(writer, Allocator.Temp);
                callback?.Invoke(ClientID, reader);
            }
        }


        //--------- Forward msgs ----------
        //--------- 消息转发相关 ----------

        // 将某个客户端发来的消息转发给指定单个客户端
        // 注意：在转发前，必须确保你已经完成了对 reader 的读取
        public void Forward(string type, ulong target, FastBufferReader reader, NetworkDelivery delivery)
        {
            if (IsServer && IsOnline)
            {
                reader.Seek(0); //重置读取位置到起点
                reader.ReadValueSafe(out ulong header); //读取并忽略消息头（通常包含发送方等信息）
                
                // 读取剩余有效消息体字节
                byte[] bytes = new byte[reader.Length - reader.Position];
                reader.ReadBytesSafe(ref bytes, reader.Length - reader.Position);

                // 使用新的 Writer 重新组包并发送
                using FastBufferWriter writer = new(bytes.Length, Allocator.Temp);
                writer.WriteBytesSafe(bytes, bytes.Length);

                network.NetworkManager.CustomMessagingManager.SendNamedMessage(type, target, writer, delivery);
            }
        }

        // 将某个客户端发来的消息转发给多个指定客户端（列表）
        // 注意：在转发前，必须确保你已经完成了对 reader 的读取
        public void Forward(string type, IReadOnlyList<ulong> targets, FastBufferReader reader, NetworkDelivery delivery)
        {
            if (IsServer && IsOnline)
            {
                reader.Seek(0); //重置 reader
                reader.ReadValueSafe(out ulong header); //忽略消息头

                byte[] bytes = new byte[reader.Length - reader.Position];
                reader.ReadBytesSafe(ref bytes, reader.Length - reader.Position);

                using FastBufferWriter writer = new(bytes.Length, Allocator.Temp);
                writer.WriteBytesSafe(bytes, bytes.Length);

                network.NetworkManager.CustomMessagingManager.SendNamedMessage(type, targets, writer, delivery);
            }
        }

        // 将某个客户端消息转发给所有其他客户端（除了消息来源客户端）
        // 注意：在转发前，必须确保你已经完成了 reader 的读取
        public void ForwardAll(string type, ulong source_client, FastBufferReader reader, NetworkDelivery delivery)
        {
            if (IsServer && IsOnline)
            {
                reader.Seek(0); //重置 reader
                reader.ReadValueSafe(out ulong header); //忽略消息头

                byte[] bytes = new byte[reader.Length - reader.Position];
                reader.ReadBytesSafe(ref bytes, reader.Length - reader.Position);

                using FastBufferWriter writer = new(bytes.Length, Allocator.Temp);
                writer.WriteBytesSafe(bytes, bytes.Length);

                // 遍历所有客户端，除了来源客户端与服务器本身
                foreach (ulong client in ClientList)
                {
                    if(client != source_client && client != ClientID)
                        network.NetworkManager.CustomMessagingManager.SendNamedMessage(type, client, writer, delivery);
                }
            }
        }

        // 判断目标客户端列表中是否包含某个 client_id
        private bool Contains(IReadOnlyList<ulong> list, ulong client_id)
        {
            foreach (ulong cid in list)
            {
                if (cid == client_id)
                    return true;
            }
            return false;
        }

        // 客户端列表（只读）
        // 由 network 统一维护
        public IReadOnlyList<ulong> ClientList { get { return network.GetClientsIds(); } }

        // 是否在线模式（联机/离线判定）
        public bool IsOnline { get { return network.IsOnline; } }

        // 是否服务器
        public bool IsServer { get { return network.IsServer; } }

        // 服务器 ClientID
        public ulong ServerID { get { return network.ServerID; } }

        // 当前客户端 ClientID
        public ulong ClientID { get { return network.ClientID; } }

        // 获取单例 NetworkMessaging
        public static NetworkMessaging Get()
        {
            return TcgNetwork.Get().Messaging;
        }

    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine.Client;
using TcgEngine;

namespace TcgEngine.UI
{
    /// <summary>
    /// UI 聊天区域
    /// 显示玩家可以输入消息的框，同时显示从服务器接收到的聊天消息
    /// </summary>
    public class ChatUI : MonoBehaviour
    {
        public bool is_opponent;              // 是否为对手的聊天框

        [Header("显示区域")]
        public ChatBubble chat_bubble;        // 消息气泡
        public AudioClip chat_audio;          // 聊天音效

        [Header("输入区域")]
        public UIPanel chat_field_area;       // 输入框面板
        public InputField chat_field;         // 输入框

        private string chat_msg;              // 当前显示的消息
        private float chat_timer = 0f;        // 消息计时器

        private static List<ChatUI> ui_list = new List<ChatUI>(); // 所有聊天 UI 列表

        private void Awake()
        {
            ui_list.Add(this);                // 添加到聊天 UI 列表
        }

        private void OnDestroy()
        {
            ui_list.Remove(this);             // 从列表移除
        }

        void Start()
        {
            GameClient.Get().onChatMsg += OnChat; // 注册接收消息回调
            RefreshChat();                        // 初始化显示
        }

        void Update()
        {
            if (!GameClient.Get().IsReady())
                return;

            int player_id = is_opponent ? GameClient.Get().GetOpponentPlayerID() : GameClient.Get().GetPlayerID();
            Game data = GameClient.Get().GetGameData();
            Player player = data.GetPlayer(player_id);

            if (player != null)
            {
                // 发送聊天消息
                if (chat_field_area != null && !is_opponent && Input.GetKeyDown(KeyCode.Return))
                {
                    if (chat_field_area.IsVisible())
                    {
                        if (!string.IsNullOrWhiteSpace(chat_field.text))
                            SendChat(chat_field.text); // 发送消息
                        chat_field.text = "";
                        chat_field_area.Hide();       // 隐藏输入框
                        GUI.FocusControl(null);
                    }
                    else
                    {
                        chat_field_area.Show();       // 显示输入框
                    }

                    chat_field.ActivateInputField();
                    chat_field.Select();
                }

                // 聊天消息消失计时
                chat_timer += Time.deltaTime;
                if (chat_timer > 5f)
                    chat_msg = null;                // 超时清空消息
            }
        }

        // 发送聊天消息到服务器
        private void SendChat(string msg)
        {
            GameClient.Get().SendChatMsg(msg);
        }

        // 刷新聊天显示
        private void RefreshChat()
        {
            chat_bubble.Hide();

            if(!string.IsNullOrWhiteSpace(chat_msg))
                chat_bubble.SetLine(chat_msg, 5f); // 显示消息，持续 5 秒
        }

        // 接收到聊天消息的回调
        private void OnChat(int chat_player_id, string msg)
        {
            int player_id = is_opponent ? GameClient.Get().GetOpponentPlayerID() : GameClient.Get().GetPlayerID();
            if (player_id == chat_player_id)
            {
                chat_msg = msg;                     // 保存消息
                chat_timer = 0f;                     // 重置计时器
                AudioTool.Get().PlaySFX("chat", chat_audio); // 播放聊天音效
                RefreshChat();                       // 刷新显示
            }
        }

        // 点击发送按钮
        public void OnClickSend()
        {
            if (chat_field_area != null && !string.IsNullOrWhiteSpace(chat_field.text))
            {
                SendChat(chat_field.text);           // 发送消息
                chat_field.text = "";
                chat_field_area.Hide();              // 隐藏输入框
                GUI.FocusControl(null);
            }
        }

        // 获取指定玩家的 ChatUI
        public static ChatUI Get(bool opponent)
        {
            foreach (ChatUI ui in ui_list)
            {
                if (ui.is_opponent == opponent)
                    return ui;
            }
            return null;
        }

    }
}

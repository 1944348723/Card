using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TcgEngine.UI
{
    /// <summary>
    /// 好友面板
    /// 包含所有好友列表，并可以邀请新的好友
    /// </summary>
    public class FriendPanel : UIPanel
    {
        // UI组件
        public ScrollRect friend_scroll;          // 滚动区域
        public RectTransform friend_content;      // 滚动内容容器
        public FriendLine line_prefab;            // 好友行预制体
        public InputField friend_input;           // 添加好友输入框
        public TabButton friends_tab;             // 好友列表Tab
        public TabButton requests_tab;            // 好友请求Tab
        public int online_duration = 10;          // 在线判定时间（分钟）
        public Text test_msg;                     // 测试消息文本
        public Text error;                        // 错误消息文本

        private List<FriendLine> friend_lines = new List<FriendLine>(); // 好友行缓存

        private static FriendPanel instance;      // 单例引用

        protected override void Awake()
        {
            base.Awake();
            instance = this;
            InitLines();  // 初始化好友行
        }

        protected override void Start()
        {
            base.Start();

            // Tab点击事件绑定
            friends_tab.onClick += RefreshPanel;
            requests_tab.onClick += RefreshPanel;
        }

        /// <summary>
        /// 初始化好友行
        /// </summary>
        private void InitLines()
        {
            int nlines = 100; // 预先创建100行
            for (int i = 0; i < nlines; i++)
            {
                FriendLine line = AddLine(line_prefab, i);
                line.Hide(); // 初始隐藏
                friend_lines.Add(line);
            }

            friend_scroll.verticalNormalizedPosition = 1f;
        }

        /// <summary>
        /// 实例化好友行并绑定事件
        /// </summary>
        private FriendLine AddLine(FriendLine template, int index)
        {
            GameObject line = Instantiate(template.gameObject, friend_content);
            RectTransform rtrans = line.GetComponent<RectTransform>();
            FriendLine rline = line.GetComponent<FriendLine>();
            rline.onClick += OnClickFriendLine;
            rline.onClickAccept += OnClickFriendAccept;
            rline.onClickReject += OnClickFriendReject;
            rline.onClickWatch += OnClickFriendWatch;
            rline.onClickChallenge += OnClickFriendChallenge;
            return rline;
        }

        /// <summary>
        /// 刷新面板显示内容
        /// </summary>
        private async void RefreshPanel()
        {
            foreach (FriendLine line in friend_lines)
                line.Hide();

            if(test_msg != null)
                test_msg.enabled = Authenticator.Get().IsTest();

            if (!Authenticator.Get().IsApi())
                return;

            // 请求好友列表数据
            string url = ApiClient.ServerURL + "/users/friends/list";
            WebResponse res = await ApiClient.Get().SendGetRequest(url);
            if (res.success)
            {
                FriendResponse contract_list = ApiTool.JsonToObject<FriendResponse>(res.data);
                if (friends_tab.active)
                    SetFriends(contract_list);
                else if (requests_tab.active)
                    SetRequests(contract_list);
            }
        }

        /// <summary>
        /// 设置好友列表
        /// </summary>
        private void SetFriends(FriendResponse contract_list)
        {
            DateTime server_time = DateTime.Parse(contract_list.server_time);
            DateTime login_time = server_time.AddMinutes(-online_duration);

            int index = 0;
            foreach (FriendData user in contract_list.friends)
            {
                if (index < friend_lines.Count)
                {
                    FriendLine line = friend_lines[index];
                    bool valid = DateTime.TryParse(user.last_online_time, out DateTime last_login);
                    bool online = valid && last_login > login_time;
                    line.SetLine(user, online); // 设置好友行显示
                }
                index++;
            }
        }

        /// <summary>
        /// 设置好友请求列表
        /// </summary>
        private void SetRequests(FriendResponse contract_list)
        {
            DateTime server_time = DateTime.Parse(contract_list.server_time);
            DateTime login_time = server_time.AddMinutes(-10);

            int index = 0;
            foreach (FriendData user in contract_list.friends_requests)
            {
                if (index < friend_lines.Count)
                {
                    FriendLine line = friend_lines[index];
                    bool valid = DateTime.TryParse(user.last_online_time, out DateTime last_login);
                    bool online = valid && last_login > login_time;
                    line.SetLine(user, online, true); // is_request为true
                }
                index++;
            }
        }

        /// <summary>
        /// 添加好友请求
        /// </summary>
        private async void AddFriend(string fuser)
        {
            FriendAddRequest req = new FriendAddRequest();
            req.username = fuser;

            string url = ApiClient.ServerURL + "/users/friends/add";
            string json = ApiTool.ToJson(req);

            WebResponse res = await ApiClient.Get().SendPostRequest(url, json);
            if (res.success)
            {
                RefreshPanel();
            }
            else
            {
                error.text = res.error;
            }
        }

        /// <summary>
        /// 删除好友
        /// </summary>
        private async void RemoveFriend(string fuser)
        {
            FriendAddRequest req = new FriendAddRequest();
            req.username = fuser;

            string url = ApiClient.ServerURL + "/users/friends/remove";
            string json = ApiTool.ToJson(req);

            WebResponse res = await ApiClient.Get().SendPostRequest(url, json);
            if (res.success)
            {
                RefreshPanel();
            }
            else
            {
                error.text = res.error;
            }
        }

        public void OnClickBack()
        {
            Hide();
        }

        private void OnClickFriendLine(FriendLine user)
        {
            // 点击好友行，可扩展功能
        }

        private void OnClickFriendAccept(FriendLine user)
        {
            FriendData friend = user.GetFriend();
            AddFriend(friend.username);
        }

        private void OnClickFriendReject(FriendLine user)
        {
            FriendData friend = user.GetFriend();
            RemoveFriend(friend.username);
        }

        private void OnClickFriendWatch(FriendLine user)
        {
            FriendData friend = user.GetFriend();
            MainMenu.Get().StartObserve(friend.username);
        }

        private void OnClickFriendChallenge(FriendLine user)
        {
            FriendData friend = user.GetFriend();
            MainMenu.Get().StartChallenge(friend.username);
        }

        public void OnClickAddFriend()
        {
            string fuser = friend_input.text;
            if (string.IsNullOrWhiteSpace(fuser))
                return;

            error.text = "";
            AddFriend(fuser);
        }

        public void OnClickRemoveFriend()
        {
            string fuser = friend_input.text;
            if (string.IsNullOrWhiteSpace(fuser))
                return;

            error.text = "";
            RemoveFriend(fuser);
        }

        public override void Show(bool instant = false)
        {
            base.Show(instant);
            error.text = "";
            friend_input.text = "";
            friends_tab.Activate(); // 默认显示好友Tab
            RefreshPanel();
        }

        public static FriendPanel Get()
        {
            return instance;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine.Client;

namespace TcgEngine.UI
{
    /// <summary>
    /// 玩家面板（PlayerPanel）
    /// 当点击菜单中的头像时显示，展示玩家账户相关的所有信息
    /// 并允许玩家更换头像或卡背
    /// </summary>

    public class PlayerPanel : UIPanel
    {
        [Header("Player")]
        public Text player_name;      // 玩家昵称
        public Text player_level;     // 玩家等级
        public AvatarUI avatar;       // 玩家头像UI
        public CardbackUI cardback;   // 玩家卡背UI
        public Text elo;              // Elo值
        public Text winrate;          // 胜率
        public Text cards_all;        // 拥有卡牌总数
        public Text victories;        // 胜场
        public Text defeats;          // 败场

        [Header("Bottom bar")]
        public GameObject buttons_area;      // 底部按钮区域
        public GameObject account_button;    // 账号按钮
        public GameObject sell_button;       // 出售按钮

        [Header("Avatars")]
        public UIPanel avatar_panel;  // 头像选择面板
        public AvatarUI[] avatars;    // 所有头像UI

        [Header("Cardbacks")]
        public UIPanel cardback_panel; // 卡背选择面板
        public CardbackUI[] cardbacks; // 所有卡背UI

        [Header("Edit Panel")]
        public UIPanel edit_panel;          // 编辑信息面板
        public InputField user_email;       // 用户邮箱输入框
        public InputField user_password_prev;   // 旧密码输入框
        public InputField user_password_new;    // 新密码输入框
        public InputField user_password_confirm; // 确认密码输入框
        public Button edit_change_email;    // 修改邮箱按钮
        public Button edit_change_password; // 修改密码按钮
        public Button resend_button;        // 重发验证邮件按钮
        public Button confirm_button;       // 确认修改按钮
        public Text edit_error;             // 编辑错误提示

        private string username;            // 当前显示的用户名
        private UserData user_data;         // 当前显示的玩家数据

        private static PlayerPanel instance; // 单例

        protected override void Awake()
        {
            base.Awake();
            instance = this;

            // 注册头像点击事件
            foreach (AvatarUI icon in avatars)
                icon.onClick += OnClickAvatar;

            // 注册卡背点击事件
            foreach (CardbackUI icon in cardbacks)
                icon.onClick += OnClickCardback;
        }

        protected override void Update()
        {
            base.Update();
            // 可在此添加动态UI刷新逻辑
        }

        protected override void Start()
        {
            base.Start();
        }

        /// <summary>
        /// 加载玩家数据
        /// 如果是自己则从本地获取，否则从服务器获取
        /// </summary>
        private async void LoadData()
        {
            if (IsYou())
                user_data = Authenticator.Get().UserData;
            else
                user_data = await ApiClient.Get().LoadUserData(username);

            RefreshPanel();
        }

        /// <summary>
        /// 清空面板显示内容
        /// </summary>
        private void ClearPanel()
        {
            player_name.text = "";
            elo.text = "";
            winrate.text = "";
            player_level.text = "";
            avatar.Hide();
            cardback.Hide();
        }

        /// <summary>
        /// 刷新面板显示
        /// </summary>
        private void RefreshPanel()
        {
            avatar_panel.Hide();
            //cardback_panel.Hide();

            if (user_data != null)
            {
                UserData user = user_data;
                player_name.text = user.username;
                player_level.text = GameplayData.Get().GetPlayerLevel(user.xp).ToString();

                AvatarData avatar = AvatarData.Get(user.avatar);
                this.avatar.SetAvatar(avatar);

                CardbackData cb = CardbackData.Get(user.cardback);
                this.cardback.SetCardback(cb);

                int winrate_val = user.matches > 0 ? Mathf.RoundToInt(user.victories * 100f / user.matches) : 0;
                winrate.text = winrate_val + "%";
                elo.text = user.elo.ToString();
                victories.text = user.victories.ToString();
                defeats.text = user.defeats.ToString();
                cards_all.text = user.CountUniqueCards() + " / " + CardData.GetAllDeckbuilding().Count;

                // 仅显示自己账户相关按钮
                buttons_area?.SetActive(IsYou());
                account_button?.SetActive(Authenticator.Get().IsApi());
                sell_button?.SetActive(Authenticator.Get().IsApi());
            }
        }

        /// <summary>
        /// 刷新头像列表
        /// </summary>
        private void RefreshAvatarList()
        {
            foreach (AvatarUI icon in avatars)
                icon.SetDefaultAvatar();

            int index = 0;
            foreach (AvatarData adata in AvatarData.GetAll())
            {
                if (index < avatars.Length)
                {
                    AvatarUI line = avatars[index];
                    if (adata != null)
                    {
                        line.SetAvatar(adata);
                        index++;
                    }
                }
            }
        }

        /// <summary>
        /// 刷新卡背列表
        /// </summary>
        private void RefreshCardBackList()
        {
            foreach (CardbackUI line in cardbacks)
                line.Hide();

            int index = 0;
            foreach (CardbackData cbdata in CardbackData.GetAll())
            {
                if (index < cardbacks.Length)
                {
                    CardbackUI line = cardbacks[index];
                    if (cbdata != null)
                    {
                        line.SetCardback(cbdata);
                        index++;
                    }
                }
            }
        }

        /// <summary>
        /// 点击头像时更换玩家头像
        /// </summary>
        private void OnClickAvatar(AvatarData avatar)
        {
            user_data = Authenticator.Get().UserData;
            if (avatar != null && user_data != null && IsYou())
            {
                user_data.avatar = avatar.id;
                RefreshPanel();
                SaveUserAvatar(avatar);
                avatar_panel.Hide();
            }
        }

        /// <summary>
        /// 点击卡背时更换玩家卡背
        /// </summary>
        private void OnClickCardback(CardbackData cb)
        {
            user_data = Authenticator.Get().UserData;
            if (cb != null && user_data != null && IsYou())
            {
                user_data.cardback = cb.id;
                RefreshPanel();
                SaveUserCardback(cb);
                cardback_panel.Hide();
            }
        }

        /// <summary>
        /// 保存头像到服务器及本地
        /// </summary>
        private async void SaveUserAvatar(AvatarData avatar)
        {
            if (ApiClient.Get().IsConnected())
            {
                string url = ApiClient.ServerURL + "/users/edit/" + ApiClient.Get().UserID;
                EditUserRequest req = new EditUserRequest();
                req.avatar = avatar.id;
                string json_data = ApiTool.ToJson(req);
                await ApiClient.Get().SendRequest(url, "POST", json_data);
            }
            await Authenticator.Get().SaveUserData();
            MainMenu.Get().RefreshUserData();
            RefreshPanel();
        }

        /// <summary>
        /// 保存卡背到服务器及本地
        /// </summary>
        private async void SaveUserCardback(CardbackData cardback)
        {
            if (ApiClient.Get().IsConnected())
            {
                string url = ApiClient.ServerURL + "/users/edit/" + ApiClient.Get().UserID;
                EditUserRequest req = new EditUserRequest();
                req.cardback = cardback.id;
                string json_data = ApiTool.ToJson(req);
                await ApiClient.Get().SendRequest(url, "POST", json_data);
            }
            await Authenticator.Get().SaveUserData();
            MainMenu.Get().RefreshUserData();
            RefreshPanel();
        }

        /// <summary>
        /// 点击头像按钮显示头像选择面板
        /// </summary>
        public void OnClickAvatar()
        {
            if (!IsYou())
                return;

            RefreshAvatarList();
            avatar_panel.Show();
        }

        /// <summary>
        /// 点击卡背按钮显示卡背选择面板
        /// </summary>
        public void OnClickCardBack()
        {
            if (!IsYou())
                return;

            RefreshCardBackList();
            cardback_panel.Show();
        }

        /// <summary>
        /// 点击好友按钮
        /// </summary>
        public void OnClickFriends()
        {
            FriendPanel.Get().Show();
        }

        /// <summary>
        /// 点击出售重复卡牌按钮
        /// </summary>
        public void OnClickDuplicates()
        {
            SellDuplicatePanel.Get().Show();
        }

        /// <summary>
        /// 点击编辑按钮，显示编辑面板
        /// </summary>
        public void OnClickEdit()
        {
            user_email.readOnly = true;
            user_password_prev.readOnly = true;
            user_password_new.readOnly = true;
            user_password_confirm.readOnly = true;
            user_password_new.gameObject.SetActive(false);
            user_password_confirm.gameObject.SetActive(false);

            UserData udata = Authenticator.Get().UserData;
            user_email.text = udata.email;
            user_password_prev.text = "password";
            user_password_new.text = "password";
            user_password_confirm.text = "password";
            edit_change_email.gameObject.SetActive(true);
            edit_change_password.gameObject.SetActive(true);
            resend_button.gameObject.SetActive(udata.validation_level == 0);
            confirm_button.gameObject.SetActive(false);
            edit_error.text = "";
            edit_panel.Show();
        }

        /// <summary>
        /// 点击更改密码按钮
        /// </summary>
        public void OnClickChangePass()
        {
            OnClickEdit();
            user_password_prev.readOnly = false;
            user_password_new.readOnly = false;
            user_password_confirm.readOnly = false;
            user_password_prev.text = "";
            user_password_new.text = "";
            user_password_confirm.text = "";
            user_password_new.gameObject.SetActive(true);
            user_password_confirm.gameObject.SetActive(true);
            edit_change_email.gameObject.SetActive(false);
            edit_change_password.gameObject.SetActive(false);
            resend_button.gameObject.SetActive(false);
            confirm_button.gameObject.SetActive(true);
            user_password_prev.Select();
        }

        /// <summary>
        /// 点击更改邮箱按钮
        /// </summary>
        public void OnClickChangeEmail()
        {
            OnClickEdit();
            user_email.readOnly = false;
            edit_change_email.gameObject.SetActive(false);
            edit_change_password.gameObject.SetActive(false);
            resend_button.gameObject.SetActive(false);
            confirm_button.gameObject.SetActive(true);
            user_email.Select();
        }

        /// <summary>
        /// 点击重发验证邮件按钮
        /// </summary>
        public async void OnClickResendConfirm()
        {
            edit_error.text = "";
            string url = ApiClient.ServerURL + "/users/email/resend";
            WebResponse res = await ApiClient.Get().SendPostRequest(url, "");
            if (res.success)
            {
                edit_panel.Hide();
            }
            else
            {
                edit_error.text = res.error;
            }
        }

        /// <summary>
        /// 点击编辑面板的确认按钮
        /// 修改邮箱或密码
        /// </summary>
        public async void OnClickEditConfirm()
        {
            edit_error.text = "";

            if (!user_email.readOnly && user_email.text.Length > 0)
            {
                EditEmailRequest req = new EditEmailRequest();
                req.email = user_email.text;
                string url = ApiClient.ServerURL + "/users/email/edit/";
                string json = ApiTool.ToJson(req);
                WebResponse res = await ApiClient.Get().SendPostRequest(url, json);
                if (res.success)
                {
                    edit_panel.Hide();
                    MainMenu.Get().RefreshUserData();
                }
                else
                {
                    edit_error.text = res.error;
                }
            }
            else if (!user_password_new.readOnly && user_password_new.text.Length > 0)
            {
                if (user_password_new.text == user_password_confirm.text)
                {
                    EditPasswordRequest req = new EditPasswordRequest();
                    req.password_previous = user_password_prev.text;
                    req.password_new = user_password_new.text;
                    string url = ApiClient.ServerURL + "/users/password/edit/";
                    string json = ApiTool.ToJson(req);
                    WebResponse res = await ApiClient.Get().SendPostRequest(url, json);
                    if (res.success)
                    {
                        edit_panel.Hide();
                    }
                    else
                    {
                        edit_error.text = res.error;
                    }
                }
            }
        }

        /// <summary>
        /// 判断当前面板显示的是否为自己
        /// </summary>
        public bool IsYou()
        {
            return username == ApiClient.Get().Username;
        }

        /// <summary>
        /// 显示自己玩家信息
        /// </summary>
        public void ShowPlayer()
        {
            string user = ApiClient.Get().Username;
            ShowPlayer(user);
        }

        /// <summary>
        /// 显示指定玩家信息
        /// </summary>
        public void ShowPlayer(string user)
        {
            if (username != user)
                ClearPanel();
            username = user;
            LoadData();
        }

        public override void Show(bool instant = false)
        {
            base.Show(instant);
            ShowPlayer();
        }

        public override void Hide(bool instant = false)
        {
            base.Hide(instant);
            edit_panel.Hide();
        }

        /// <summary>
        /// 获取单例
        /// </summary>
        public static PlayerPanel Get()
        {
            return instance;
        }
    }
}

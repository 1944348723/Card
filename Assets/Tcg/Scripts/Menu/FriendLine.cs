using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TcgEngine.UI
{
    /// <summary>
    /// 好友面板中的一行
    /// 显示好友头像、用户名，并包含可与好友互动的按钮（接受、拒绝、观看、挑战）
    /// </summary>
    public class FriendLine : MonoBehaviour
    {
        // UI组件
        public Text username;          // 显示用户名
        public Image avatar;           // 显示头像

        public Image online_img;       // 在线状态图片
        public Sprite online_sprite;   // 在线状态精灵
        public Sprite offline_sprite;  // 离线状态精灵

        public Button accept_btn;      // 接受好友请求按钮
        public Button reject_btn;      // 拒绝好友请求按钮
        public Button watch_btn;       // 观看好友按钮
        public Button challenge_btn;   // 挑战好友按钮

        // 点击事件回调
        public UnityAction<FriendLine> onClick;           // 左键点击好友行
        public UnityAction<FriendLine> onClickAccept;     // 点击接受按钮
        public UnityAction<FriendLine> onClickReject;     // 点击拒绝按钮
        public UnityAction<FriendLine> onClickWatch;      // 点击观看按钮
        public UnityAction<FriendLine> onClickChallenge;  // 点击挑战按钮

        // 内部数据
        private FriendData fdata;       // 当前好友数据
        private Sprite default_avat;    // 默认头像，用于回退显示

        private void Awake()
        {
            // 保存默认头像
            default_avat = avatar.sprite;

            // 按钮绑定事件
            if (accept_btn != null)
                accept_btn.onClick.AddListener(() => { onClickAccept?.Invoke(this); });
            if (reject_btn != null)
                reject_btn.onClick.AddListener(() => { onClickReject?.Invoke(this); });
            if (watch_btn != null)
                watch_btn.onClick.AddListener(() => { onClickWatch?.Invoke(this); });
            if (challenge_btn != null)
                challenge_btn.onClick.AddListener(() => { onClickChallenge?.Invoke(this); });
        }

        /// <summary>
        /// 设置好友行显示内容
        /// </summary>
        /// <param name="user">好友数据</param>
        /// <param name="online">是否在线</param>
        /// <param name="is_request">是否为好友请求</param>
        public void SetLine(FriendData user, bool online, bool is_request = false)
        {
            fdata = user;
            username.text = user.username;
            avatar.sprite = default_avat;

            // 设置头像
            if (avatar != null)
            {
                AvatarData avat = AvatarData.Get(user.avatar);
                if (avat != null)
                    avatar.sprite = avat.avatar;
            }

            // 设置在线状态
            if (online_img != null)
            {
                online_img.sprite = online ? online_sprite : offline_sprite;
            }

            // 设置按钮显示
            if (watch_btn != null)
                watch_btn.gameObject.SetActive(online && !is_request);
            if (challenge_btn != null)
                challenge_btn.gameObject.SetActive(online && !is_request);

            if (accept_btn != null)
                accept_btn.gameObject.SetActive(is_request);
            if (reject_btn != null)
                reject_btn.gameObject.SetActive(is_request);

            gameObject.SetActive(true);
        }

        /// <summary>
        /// 隐藏好友行
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 点击好友行
        /// </summary>
        public void OnClick()
        {
            onClick?.Invoke(this);
        }

        /// <summary>
        /// 获取当前好友数据
        /// </summary>
        /// <returns>好友数据对象</returns>
        public FriendData GetFriend()
        {
            return fdata;
        }
    }
}

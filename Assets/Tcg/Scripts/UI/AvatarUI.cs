using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TcgEngine;

namespace TcgEngine.UI
{
    /// <summary>
    /// 显示头像的 UI 组件
    /// </summary>

    public class AvatarUI : MonoBehaviour
    {
        public UnityAction<AvatarData> onClick; // 点击头像时触发的事件

        private Image avatar_img; // 显示头像的 Image 组件
        private Button avatar_button; // 头像的 Button 组件
        private Sprite default_icon; // 默认头像图标

        private AvatarData avatar; // 当前头像数据

        void Awake()
        {
            avatar_img = GetComponent<Image>(); // 获取 Image 组件
            avatar_button = GetComponent<Button>(); // 获取 Button 组件
            default_icon = avatar_img.sprite; // 保存默认头像

            if (avatar_button != null)
                avatar_button.onClick.AddListener(OnClick); // 注册点击事件
        }

        // 设置头像数据
        public void SetAvatar(AvatarData avatar)
        {
            this.avatar = avatar;
            avatar_img.enabled = true;
            avatar_img.sprite = default_icon;

            if (avatar != null)
            {
                avatar_img.sprite = avatar.avatar; // 显示头像
            }
        }

        // 设置默认头像
        public void SetDefaultAvatar()
        {
            this.avatar = null;
            avatar_img.enabled = true;
            avatar_img.sprite = default_icon;
        }

        // 设置 Image 为指定精灵
        public void SetImage(Sprite sprite)
        {
            avatar_img.sprite = sprite;
        }

        // 隐藏头像
        public void Hide()
        {
            this.avatar = null;
            avatar_img.enabled = false;
        }

        // 获取当前头像数据
        public AvatarData GetAvatar()
        {
            return avatar;
        }

        // 点击头像时调用
        private void OnClick()
        {
            onClick?.Invoke(avatar); // 触发点击事件
        }
    }
}
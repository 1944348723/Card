using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine.Client;
using TcgEngine;

namespace TcgEngine.UI
{
    /// <summary>
    /// PlayerUI 类用于显示游戏场景中每个玩家的主要信息
    /// 游戏中每个玩家都有一个对应的 PlayerUI
    /// </summary>
    public class PlayerUI : MonoBehaviour
    {
        public bool is_opponent;          // 是否为对手
        public Text pname;                // 玩家名字显示文本
        public AvatarUI avatar;           // 玩家头像 UI
        public IconBar mana_bar;          // 法力值条
        public Text hp_txt;               // 当前生命值文本
        public Text hp_max_txt;           // 最大生命值文本

        public Animator[] secrets;        // 玩家密技（秘密）图标动画

        public GameObject dead_fx;        // 玩家死亡特效
        public AudioClip dead_audio;      // 玩家死亡音效
        public Sprite avatar_dead;        // 玩家死亡头像显示

        private bool killed = false;      // 是否已死亡
        private float timer = 0f;         // 定时器，用于慢速更新

        private int prev_hp = 0;          // 上一次显示的生命值（用于动画延迟）
        private float delayed_damage_timer = 0f; // 延迟显示伤害的计时器

        private static List<PlayerUI> ui_list = new List<PlayerUI>(); // 所有 PlayerUI 的列表

        private void Awake()
        {
            ui_list.Add(this);             // 将当前 PlayerUI 添加到列表
        }

        private void OnDestroy()
        {
            ui_list.Remove(this);          // 移除当前 PlayerUI
        }

        void Start()
        {
            pname.text = "";
            hp_txt.text = "";
            hp_max_txt.text = "";

            for (int i = 0; i < secrets.Length; i++)
                secrets[i].gameObject.SetActive(false); // 隐藏所有密技图标

            avatar.onClick += OnClickAvatar;           // 点击头像事件绑定
            GameClient.Get().onSecretTrigger += OnSecretTrigger; // 密技触发事件绑定
        }

        void Update()
        {
            if (!GameClient.Get().IsReady())
                return;

            Player player = GetPlayer();

            if (player != null)
            {
                pname.text = player.username;          // 显示玩家名字
                mana_bar.value = player.mana;          // 更新法力值
                mana_bar.max_value = player.mana_max;  // 更新最大法力值
                hp_txt.text = prev_hp.ToString();      // 显示延迟生命值
                hp_max_txt.text = "/" + player.hp_max.ToString(); // 显示最大生命值

                AvatarData adata = AvatarData.Get(player.avatar);
                if (avatar != null && adata != null && !killed)
                    avatar.SetAvatar(adata);          // 更新头像显示

                delayed_damage_timer -= Time.deltaTime; // 更新伤害延迟计时
                if (!IsDamagedDelayed())
                    prev_hp = player.hp;               // 未延迟伤害时直接更新
            }

            timer += Time.deltaTime;
            if (timer > 0.4f)
            {
                timer = 0f;
                SlowUpdate();                           // 慢速更新（用于密技图标显示）
            }
        }

        /// <summary>
        /// 慢速更新密技图标显示
        /// </summary>
        void SlowUpdate()
        {
            Player player = GetPlayer();
            if (player == null)
                return;

            for (int i = 0; i < secrets.Length; i++)
            {
                bool active = i < player.cards_secret.Count;  // 是否有密技需要显示
                bool was_active = secrets[i].gameObject.activeSelf;
                if (active != was_active)
                    secrets[i].gameObject.SetActive(active);
                if (active && !was_active)
                    secrets[i].SetTrigger("appear");          // 播放密技出现动画
                if (active && !was_active && !is_opponent)
                    secrets[i].GetComponent<SecretIconUI>().SetCard(player.cards_secret[i]);
                if (!active && was_active)
                    secrets[i].Rebind();                      // 重置动画
            }
        }

        /// <summary>
        /// 玩家死亡显示
        /// </summary>
        public void Kill()
        {
            killed = true;
            avatar.SetImage(avatar_dead);                  // 显示死亡头像
            AudioTool.Get().PlaySFX("fx", dead_audio);    // 播放死亡音效
            FXTool.DoFX(dead_fx, avatar.transform.position); // 播放死亡特效
        }

        /// <summary>
        /// 延迟显示伤害效果
        /// </summary>
        public void DelayDamage(int damage, float duration = 1f)
        {
            if (damage != 0)
            {
                delayed_damage_timer = duration;
            }
        }

        /// <summary>
        /// 是否还有延迟伤害未显示
        /// </summary>
        public bool IsDamagedDelayed()
        {
            return delayed_damage_timer > 0f;
        }

        /// <summary>
        /// 点击头像事件
        /// 如果当前选择器是选择目标并且是玩家操作回合，则选择该玩家
        /// </summary>
        private void OnClickAvatar(AvatarData avatar)
        {
            Game gdata = GameClient.Get().GetGameData();
            int player_id = GameClient.Get().GetPlayerID();
            if (gdata.selector == SelectorType.SelectTarget && player_id == gdata.selector_player_id)
            {
                GameClient.Get().SelectPlayer(GetPlayer());
            }
        }

        /// <summary>
        /// 密技触发事件
        /// </summary>
        private void OnSecretTrigger(Card secret, Card triggerer)
        {
            Player player = GetPlayer();
            int index = player.cards_secret.Count - 1;
            if (player.player_id == secret.player_id && index >= 0 && index < secrets.Length)
            {
                secrets[index].SetTrigger("reveal"); // 播放密技触发动画
            }
        }

        /// <summary>
        /// 获取对应玩家数据
        /// </summary>
        public Player GetPlayer()
        {
            int player_id = is_opponent ? GameClient.Get().GetOpponentPlayerID() : GameClient.Get().GetPlayerID();
            Game data = GameClient.Get().GetGameData();
            return data.GetPlayer(player_id);
        }

        /// <summary>
        /// 根据是否为对手获取 PlayerUI 实例
        /// </summary>
        public static PlayerUI Get(bool opponent)
        {
            foreach (PlayerUI ui in ui_list)
            {
                if (ui.is_opponent == opponent)
                    return ui;
            }
            return null;
        }

    }
}

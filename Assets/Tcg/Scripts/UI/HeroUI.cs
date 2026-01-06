using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine.Client;
using UnityEngine.EventSystems;

namespace TcgEngine.UI
{
    /// <summary>
    /// 英雄UI脚本
    /// 显示玩家或对手的英雄，并管理英雄能力按钮
    /// </summary>
    public class HeroUI : MonoBehaviour
    {
        public bool opponent;                  // 是否是对手英雄
        public GameObject power_area;           // 能力区域UI
        public Button power_button;             // 英雄能力按钮
        public Image power_image;               // 英雄能力图片
        public GameObject power_mana_slot;      // 能力法力槽
        public Text power_mana;                 // 能力法力消耗显示

        public Material active_mat;             // 可用状态材质
        public Material inactive_mat;           // 不可用状态材质

        private bool focus = false;             // 鼠标是否悬停在英雄上

        private static List<HeroUI> ui_list = new List<HeroUI>(); // 所有英雄UI列表

        private void Awake()
        {
            ui_list.Add(this);
        }

        private void OnDestroy()
        {
            ui_list.Remove(this);
        }

        void Start()
        {
            power_area.SetActive(false);

            if (power_button != null)
                power_button.onClick.AddListener(OnClickPower);

            // 鼠标悬停事件
            EventTrigger trigger = power_area.GetComponent<EventTrigger>();
            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerEnter;
            entry.callback.AddListener((eventData) => { OnEnterMouse(); });
            EventTrigger.Entry exit = new EventTrigger.Entry();
            exit.eventID = EventTriggerType.PointerExit;
            exit.callback.AddListener((eventData) => { OnExitMouse(); });
            trigger.triggers.Add(entry);
            trigger.triggers.Add(exit);
        }

        private void Update()
        {
            if (!GameClient.Get().IsReady())
                return;

            Game gdata = GameClient.Get().GetGameData();
            Player player = GetPlayer();
            Card hero = player.hero;
            if (hero == null)
                return;

            // 获取激活型能力
            AbilityData ability = hero.GetAbility(AbilityTrigger.Activate);
            if (ability != null)
            {
                // 更新英雄能力UI
                power_image.sprite = hero.CardData.GetBoardArt(hero.VariantData);
                power_image.material = !hero.exhausted ? active_mat : inactive_mat;
                power_mana_slot?.SetActive(gdata.IsPlayerTurn(player) && !hero.exhausted);
                power_mana.text = ability.mana_cost.ToString();
            }

            if (power_button != null)
                power_button.interactable = ability != null && !hero.exhausted && gdata.IsPlayerTurn(player);

            if (hero != null && !power_area.activeSelf)
                power_area.SetActive(true);
        }

        /// <summary>
        /// 点击英雄能力按钮
        /// </summary>
        public void OnClickPower()
        {
            Game gdata = GameClient.Get().GetGameData();
            Player player = GameClient.Get().GetPlayer();
            Card hero = player.hero;
            AbilityData ability = hero?.GetAbility(AbilityTrigger.Activate);

            if (ability != null && !opponent)
            {
                // 检查法力是否足够
                if (!hero.exhausted && !player.CanPayAbility(hero, ability))
                {
                    WarningText.ShowNoMana();
                    return;
                }

                // 检查教程限制
                if (!Tutorial.Get().CanDo(TutoEndTrigger.CastAbility, hero))
                    return;

                // 验证是否可以施放能力
                bool valid = gdata.IsPlayerActionTurn(player) && gdata.CanCastAbility(hero, ability);
                if (valid)
                {
                    GameClient.Get().CastAbility(hero, ability);
                }
            }
        }

        private void OnEnterMouse()
        {
            focus = true;
        }

        private void OnExitMouse()
        {
            focus = false;
        }

        private void OnDisable()
        {
            focus = false;
        }

        /// <summary>
        /// 英雄是否被鼠标悬停
        /// </summary>
        public bool IsFocus()
        {
            return focus;
        }

        /// <summary>
        /// 获取英雄所属玩家ID
        /// </summary>
        public int GetPlayerID()
        {
            return opponent ? GameClient.Get().GetOpponentPlayerID() : GameClient.Get().GetPlayerID();
        }

        /// <summary>
        /// 获取英雄所属玩家
        /// </summary>
        public Player GetPlayer()
        {
            Game gdata = GameClient.Get().GetGameData();
            return gdata.GetPlayer(GetPlayerID());
        }

        /// <summary>
        /// 获取英雄卡牌
        /// </summary>
        public Card GetCard()
        {
            Player player = GetPlayer();
            return player.hero;
        }

        /// <summary>
        /// 获取当前被鼠标悬停的英雄UI
        /// </summary>
        public static HeroUI GetFocus()
        {
            foreach (HeroUI ui in ui_list)
            {
                if (ui.IsFocus())
                    return ui;
            }
            return null;
        }

        /// <summary>
        /// 根据是否为对手获取英雄UI
        /// </summary>
        public static HeroUI Get(bool opponent)
        {
            foreach (HeroUI ui in ui_list)
            {
                if (ui.opponent == opponent)
                    return ui;
            }
            return null;
        }

        /// <summary>
        /// 根据玩家ID获取英雄UI
        /// </summary>
        public static HeroUI Get(int player_id)
        {
            bool opponent = player_id != GameClient.Get().GetPlayerID();
            return Get(opponent);
        }
    }
}

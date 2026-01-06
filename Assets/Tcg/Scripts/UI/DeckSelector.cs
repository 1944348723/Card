using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TcgEngine;

namespace TcgEngine.UI
{
    /// <summary>
    /// 卡组选择器，用于在比赛前让玩家选择一个卡组
    /// </summary>
    public class DeckSelector : MonoBehaviour
    {
        public DropdownValue deck_dropdown;   // 下拉菜单组件，用于显示卡组列表
        public UnityAction<string> onChange;  // 当选择改变时触发的事件

        void Start()
        {
            deck_dropdown.onValueChanged += OnChange; // 绑定下拉菜单值改变事件
        }

        void Update()
        {

        }

        // 设置玩家可用卡组列表
        public void SetupUserDeckList()
        {
            deck_dropdown.ClearOptions();           // 清空原有选项

            deck_dropdown.AddOption("random", "Random"); // 添加随机卡组选项

            // 添加系统标准卡组
            foreach (DeckData deck in GameplayData.Get().free_decks)
            {
                deck_dropdown.AddOption(deck.id, deck.title);
            }

            // 添加玩家自定义卡组
            UserData udata = Authenticator.Get().UserData;
            if (udata != null)
            {
                foreach (UserDeckData deck in udata.decks)
                {
                    if (udata.IsDeckValid(deck))
                    {
                        deck_dropdown.AddOption(deck.tid, deck.title);
                    }
                }
            }
        }

        // 设置AI可用卡组列表
        public void SetupAIDeckList()
        {
            deck_dropdown.ClearOptions(); // 清空原有选项

            deck_dropdown.AddOption("random_ai", "Random"); // 添加随机AI卡组选项

            // 添加系统AI卡组
            foreach (DeckData deck in GameplayData.Get().ai_decks)
            {
                deck_dropdown.AddOption(deck.id, deck.title);
            }

            // 同样添加玩家自定义卡组
            UserData udata = Authenticator.Get().UserData;
            if (udata != null)
            {
                foreach (UserDeckData deck in udata.decks)
                {
                    if (udata.IsDeckValid(deck))
                    {
                        deck_dropdown.AddOption(deck.tid, deck.title);
                    }
                }
            }
        }

        // 通过用户卡组对象选择卡组
        private void SelectDeck(UserDeckData deck)
        {
            if (deck != null)
            {
                deck_dropdown.SetValue(deck.tid);
            }
        }

        // 通过系统卡组对象选择卡组
        private void SelectDeck(DeckData deck)
        {
            if (deck != null)
            {
                deck_dropdown.SetValue(deck.id);
            }
        }

        // 通过卡组ID选择卡组
        public void SelectDeck(string deck)
        {
            // 确保卡组存在，防止选择无效卡组
            UserData udata = Authenticator.Get().UserData;
            UserDeckData udeck = udata?.GetDeck(deck);
            if (udeck != null)
            {
                SelectDeck(udeck);
                return;
            }

            DeckData adeck = DeckData.Get(deck);
            if(adeck != null)
                SelectDeck(adeck);
        }

        // 通过下拉菜单索引选择卡组
        public void SelectDeck(int index)
        {
            deck_dropdown.SetValue(index);
        }

        // 锁定下拉菜单，禁止操作
        public void Lock()
        {
            deck_dropdown.interactable = false;
        }

        // 解锁下拉菜单
        public void Unlock()
        {
            deck_dropdown.interactable = true;
        }

        // 设置锁定状态
        public void SetLocked(bool locked)
        {
            deck_dropdown.interactable = !locked;
        }

        // 当下拉菜单值改变时触发
        private void OnChange(int i, string val)
        {
            string value = deck_dropdown.GetSelectedValue();
            onChange?.Invoke(value);
        }

        // 获取当前选择的卡组ID
        public string GetDeckID()
        {
            return deck_dropdown.GetSelectedValue();
        }

        // 获取当前选择的卡组名称
        public string GetDeckTitle()
        {
            return deck_dropdown.GetSelectedText();
        }

        // 根据ID获取卡组数据
        public UserDeckData GetDeckById(string deck_id)
        {
            UserData user = Authenticator.Get().UserData;
            UserDeckData udeck = user.GetDeck(deck_id); // 检查用户自定义卡组
            DeckData deck = DeckData.Get(deck_id);       // 检查系统卡组

            // 返回用户自定义卡组
            if (udeck != null)
                return udeck;
            // 返回系统卡组
            else if (deck != null)
                return new UserDeckData(deck);
            return null;
        }

        // 获取当前选择的卡组对象
        public UserDeckData GetDeck()
        {
            string deck_id = GetDeckID();

            // 随机卡组处理
            if (deck_id == "random")
                return GetRandomDeck();
            if (deck_id == "random_ai")
                return GetRandomDeckAI();

            return GetDeckById(deck_id);
        }

        // 获取随机玩家卡组
        public UserDeckData GetRandomDeck()
        {
            List<UserDeckData> random_decks = new List<UserDeckData>();
            foreach (DropdownValueItem item in deck_dropdown.Items)
            {
                UserDeckData deck = GetDeckById(item.id);
                if (deck != null)
                    random_decks.Add(deck);
            }

            if (random_decks.Count > 0)
                return random_decks[Random.Range(0, random_decks.Count)];
            return null;
        }

        // 获取随机AI卡组
        public UserDeckData GetRandomDeckAI()
        {
            return new UserDeckData(GameplayData.Get().GetRandomAIDeck());
        }
    }
}

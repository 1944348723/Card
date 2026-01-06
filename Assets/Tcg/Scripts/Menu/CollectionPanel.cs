using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TcgEngine.UI
{
    /// <summary>
    /// CollectionPanel 显示玩家拥有的所有卡牌的面板
    /// 也是玩家进行卡组编辑（Deckbuilder）的面板
    /// </summary>
    public class CollectionPanel : UIPanel
    {
        [Header("Cards")] public ScrollRect scroll_rect; // 卡牌滚动区域
        public RectTransform scroll_content; // 滚动内容容器
        public CardGrid grid_content; // 卡牌网格布局
        public GameObject card_prefab; // 卡牌预制件

        [Header("Left Side")] public IconButton[] team_filters; // 队伍过滤按钮
        public Toggle toggle_owned; // 已拥有卡牌开关
        public Toggle toggle_not_owned; // 未拥有卡牌开关

        public Toggle toggle_character; // 角色卡类型开关
        public Toggle toggle_spell; // 法术卡类型开关
        public Toggle toggle_artifact; // 遗物卡类型开关
        public Toggle toggle_equipment; // 装备卡类型开关
        public Toggle toggle_secret; // 秘密卡类型开关

        public Toggle toggle_common; // 普通稀有度开关
        public Toggle toggle_uncommon; // 稀有稀有度开关
        public Toggle toggle_rare; // 史诗稀有度开关
        public Toggle toggle_mythic; // 传说稀有度开关

        public Toggle toggle_foil; // 金卡/特殊卡开关

        public Dropdown sort_dropdown; // 排序下拉菜单
        public InputField search; // 搜索框

        [Header("Right Side")] public UIPanel deck_list_panel; // 卡组列表面板
        public UIPanel card_list_panel; // 卡牌列表面板
        public DeckLine[] deck_lines; // 卡组条目

        [Header("Deckbuilding")] public InputField deck_title; // 当前编辑卡组名称
        public Text deck_quantity; // 卡组卡牌总数显示
        public GameObject deck_cards_prefab; // 卡组卡牌预制件
        public RectTransform deck_content; // 卡组卡牌容器
        public GridLayoutGroup deck_grid; // 卡组网格布局
        public IconButton[] hero_powers; // 英雄技能按钮

        private TeamData filter_team = null; // 队伍过滤条件
        private int filter_dropdown = 0; // 排序过滤条件
        private string filter_search = ""; // 搜索过滤条件

        private List<CollectionCard> card_list = new List<CollectionCard>(); // 当前显示的卡牌列表
        private List<CollectionCard> all_list = new List<CollectionCard>(); // 所有卡牌对象列表
        private List<DeckLine> deck_card_lines = new List<DeckLine>(); // 当前显示的卡组条目

        private string current_deck_tid; // 当前编辑的卡组 ID
        private bool editing_deck = false; // 是否处于编辑卡组状态
        private bool saving = false; // 是否正在保存卡组
        private bool spawned = false; // 是否已经生成卡牌对象
        private bool update_grid = false; // 是否需要更新网格布局
        private float update_grid_timer = 0f; // 网格布局更新计时器

        private List<UserCardData> deck_cards = new List<UserCardData>(); // 当前卡组中的卡牌数据

        private static CollectionPanel instance; // 单例引用

        protected override void Awake()
        {
            base.Awake();
            instance = this;

            // 删除网格内容（防止重复生成）
            for (int i = 0; i < grid_content.transform.childCount; i++)
                Destroy(grid_content.transform.GetChild(i).gameObject);
            for (int i = 0; i < deck_grid.transform.childCount; i++)
                Destroy(deck_grid.transform.GetChild(i).gameObject);

            // 绑定卡组条目点击事件
            foreach (DeckLine line in deck_lines)
                line.onClick += OnClickDeckLine;
            foreach (DeckLine line in deck_lines)
                line.onClickDelete += OnClickDeckDelete;

            // 绑定队伍筛选按钮点击事件
            foreach (IconButton button in team_filters)
                button.onClick += OnClickTeam;
        }

        protected override void Start()
        {
            base.Start();

            // 设置英雄技能按钮的悬停文字
            foreach (IconButton btn in hero_powers)
            {
                CardData icard = CardData.Get(btn.value); // 获取对应卡牌
                HoverTargetUI hover = btn.GetComponent<HoverTargetUI>();
                AbilityData iability = icard?.GetAbility(AbilityTrigger.Activate); // 获取英雄技能
                if (icard != null && hover != null && iability != null)
                {
                    string color = ColorUtility.ToHtmlStringRGBA(icard.team.color); // 获取队伍颜色
                    hover.text = "<b><color=#" + color + ">Hero Power: </color>";
                    hover.text += icard.title + "</b>\n " + iability.GetDesc(icard); // 技能描述
                    if (iability.mana_cost > 0)
                        hover.text += " <size=16>Mana: " + iability.mana_cost + "</size>"; // 消耗显示
                }
            }
        }

        protected override void Update()
        {
            base.Update();
            // 可以在这里添加实时刷新或动画逻辑
        }

        private void LateUpdate()
        {
            // 定期更新网格布局
            update_grid_timer += Time.deltaTime;
            if (update_grid && update_grid_timer > 0.2f)
            {
                grid_content.GetColumnAndRow(out int rows, out int cols); // 获取行列数
                if (cols > 0)
                {
                    float row_height = grid_content.GetGrid().cellSize.y + grid_content.GetGrid().spacing.y; // 单行高度
                    float height = rows * row_height;
                    scroll_content.sizeDelta = new Vector2(scroll_content.sizeDelta.x, height + 100); // 调整滚动容器高度
                    update_grid = false;
                }
            }
        }

        private void SpawnCards()
        {
            spawned = true;

            // 清空已有卡牌对象
            foreach (CollectionCard card in all_list)
                Destroy(card.gameObject);
            all_list.Clear();

            // 遍历所有卡牌和变体生成 CollectionCard
            foreach (VariantData variant in VariantData.GetAll())
            {
                foreach (CardData card in CardData.GetAll())
                {
                    GameObject nCard = Instantiate(card_prefab, grid_content.transform); // 实例化卡牌对象
                    CollectionCard dCard = nCard.GetComponent<CollectionCard>();
                    dCard.SetCard(card, variant, 0); // 初始化卡牌数量为 0
                    dCard.onClick += OnClickCard; // 左键点击事件
                    dCard.onClickRight += OnClickCardRight; // 右键点击事件
                    all_list.Add(dCard);
                    nCard.SetActive(false); // 默认隐藏
                }
            }
        }

        //----- 重新加载用户数据 -----
        public async void ReloadUser()
        {
            await Authenticator.Get().LoadUserData(); // 加载用户数据
            MainMenu.Get().RefreshDeckList(); // 刷新主界面卡组列表
            RefreshCardsQuantities(); // 刷新卡牌数量显示

            if (!editing_deck)
                RefreshDeckList(); // 非编辑状态下刷新卡组列表
        }

        public async void ReloadUserCards()
        {
            await Authenticator.Get().LoadUserData();
            RefreshCardsQuantities(); // 刷新卡牌数量
        }

        public async void ReloadUserDecks()
        {
            await Authenticator.Get().LoadUserData();
            MainMenu.Get().RefreshDeckList();
            RefreshDeckList(); // 刷新卡组列表
        }

        //----- 刷新 UI -----
        private void RefreshAll()
        {
            RefreshFilters(); // 刷新筛选条件
            RefreshCards(); // 刷新卡牌显示
            RefreshDeckList(); // 刷新卡组列表
            RefreshStarterDeck(); // 刷新初始卡组
        }

        private void RefreshFilters()
        {
            search.text = ""; // 清空搜索框
            sort_dropdown.value = 0; // 重置排序
            foreach (IconButton button in team_filters)
                button.Deactivate(); // 重置队伍筛选按钮

            filter_team = null;
            filter_dropdown = 0;
            filter_search = "";
        }

        private void ShowDeckList()
        {
            deck_list_panel.Show(); // 显示卡组列表
            card_list_panel.Hide(); // 隐藏卡牌列表
            editing_deck = false; // 设置非编辑状态
        }

        private void ShowDeckCards()
        {
            deck_list_panel.Hide(); // 隐藏卡组列表
            card_list_panel.Show(); // 显示卡牌列表
        }

        /// <summary>
        /// 刷新卡牌显示，包括数量、筛选和排序
        /// </summary>
        public void RefreshCards()
        {
            if (!spawned)
                SpawnCards(); // 首次生成卡牌对象

            card_list.Clear(); // 清空当前显示列表

            UserData udata = Authenticator.Get().UserData; // 获取用户数据
            if (udata == null)
                return;

            VariantData variant = VariantData.GetDefault(); // 默认卡牌变体
            VariantData special = VariantData.GetSpecial(); // 特殊变体
            if (toggle_foil.isOn && special != null)
                variant = special; // 如果金卡开关打开，使用特殊变体

            List<CardDataQ> all_cards = new List<CardDataQ>(); // 所有卡牌数据
            List<CardDataQ> shown_cards = new List<CardDataQ>(); // 符合筛选条件的卡牌

            // 遍历所有卡牌生成卡牌数据对象
            foreach (CardData icard in CardData.GetAll())
            {
                CardDataQ card = new CardDataQ();
                card.card = icard;
                card.variant = variant;
                card.quantity = udata.GetCardQuantity(icard, variant); // 用户拥有数量
                all_cards.Add(card);
            }

            // 根据下拉菜单排序
            if (filter_dropdown == 0) // 按名称
                all_cards.Sort((CardDataQ a, CardDataQ b) => { return a.card.title.CompareTo(b.card.title); });
            if (filter_dropdown == 1) // 按攻击力
                all_cards.Sort((CardDataQ a, CardDataQ b) =>
                {
                    return b.card.attack == a.card.attack
                        ? b.card.hp.CompareTo(a.card.hp)
                        : b.card.attack.CompareTo(a.card.attack);
                });
            if (filter_dropdown == 2) // 按生命值
                all_cards.Sort((CardDataQ a, CardDataQ b) =>
                {
                    return b.card.hp == a.card.hp
                        ? b.card.attack.CompareTo(a.card.attack)
                        : b.card.hp.CompareTo(a.card.hp);
                });
            if (filter_dropdown == 3) // 按法力值
                all_cards.Sort((CardDataQ a, CardDataQ b) =>
                {
                    return b.card.mana == a.card.mana
                        ? a.card.title.CompareTo(b.card.title)
                        : a.card.mana.CompareTo(b.card.mana);
                });

            // 遍历所有卡牌并应用筛选条件
            foreach (CardDataQ card in all_cards)
            {
                if (card.card.deckbuilding) // 仅显示可用于卡组编辑的卡牌
                {
                    CardData icard = card.card;
                    if (filter_team == null || filter_team == icard.team)
                    {
                        bool owned = card.quantity > 0; // 是否拥有
                        RarityData rarity = icard.rarity;
                        CardType type = icard.type;

                        // 已拥有/未拥有筛选
                        bool owned_check = (owned && toggle_owned.isOn)
                                           || (!owned && toggle_not_owned.isOn)
                                           || toggle_owned.isOn == toggle_not_owned.isOn;

                        // 卡牌类型筛选
                        bool type_check = (type == CardType.Character && toggle_character.isOn)
                                          || (type == CardType.Spell && toggle_spell.isOn)
                                          || (type == CardType.Artifact && toggle_artifact.isOn)
                                          || (type == CardType.Equipment && toggle_equipment.isOn)
                                          || (type == CardType.Secret && toggle_secret.isOn)
                                          || (!toggle_character.isOn && !toggle_spell.isOn && !toggle_artifact.isOn &&
                                              !toggle_equipment.isOn && !toggle_secret.isOn);

                        // 稀有度筛选
                        bool rarity_check = (rarity.rank == 1 && toggle_common.isOn)
                                            || (rarity.rank == 2 && toggle_uncommon.isOn)
                                            || (rarity.rank == 3 && toggle_rare.isOn)
                                            || (rarity.rank == 4 && toggle_mythic.isOn)
                                            || (!toggle_common.isOn && !toggle_uncommon.isOn && !toggle_rare.isOn &&
                                                !toggle_mythic.isOn);

                        // 搜索筛选
                        string search = filter_search.ToLower();
                        bool search_check = string.IsNullOrWhiteSpace(search)
                                            || icard.id.Contains(search)
                                            || icard.title.ToLower().Contains(search)
                                            || icard.GetText().ToLower().Contains(search);

                        // 如果全部条件通过，添加到显示列表
                        if (owned_check && type_check && rarity_check && search_check)
                        {
                            shown_cards.Add(card);
                        }
                    }
                }
            }

            // 将符合条件的卡牌绑定到 CollectionCard 对象并显示
            int index = 0;
            foreach (CardDataQ qcard in shown_cards)
            {
                if (index < all_list.Count)
                {
                    CollectionCard dcard = all_list[index];
                    dcard.SetCard(qcard.card, qcard.variant, 0); // 初始化数量为0
                    card_list.Add(dcard); // 添加到显示列表
                    if (!dcard.gameObject.activeSelf)
                        dcard.gameObject.SetActive(true); // 激活显示
                    index++;
                }
            }

            // 隐藏多余的卡牌对象
            for (int i = index; i < all_list.Count; i++)
                all_list[i].gameObject.SetActive(false);

            // 设置标志刷新网格布局
            update_grid = true;
            update_grid_timer = 0f;
            scroll_rect.verticalNormalizedPosition = 1f; // 滚动到顶部
            RefreshCardsQuantities(); // 刷新数量条显示
        }

        // 刷新显示卡牌的数量和灰度状态
        private void RefreshCardsQuantities()
        {
            UserData udata = Authenticator.Get().UserData; // 获取用户数据
            foreach (CollectionCard card in card_list)
            {
                CardData icard = card.GetCard(); // 当前卡牌
                VariantData ivariant = card.GetVariant(); // 当前卡牌的变体
                bool owned = IsCardOwned(udata, icard, ivariant, 1); // 判断是否拥有至少一张
                int quantity = udata.GetCardQuantity(icard, ivariant); // 获取用户拥有数量
                card.SetQuantity(quantity); // 更新数量显示
                card.SetGrayscale(!owned); // 如果没有拥有，则设置为灰色
            }
        }

        // 刷新卡组列表显示
        private void RefreshDeckList()
        {
            // 先隐藏所有卡组条目
            foreach (DeckLine line in deck_lines)
                line.Hide();
            deck_cards.Clear(); // 清空当前编辑卡组的数据
            editing_deck = false; // 设置非编辑状态
            saving = false; // 重置保存状态

            UserData udata = Authenticator.Get().UserData; // 获取用户数据
            if (udata == null)
                return;

            int index = 0;
            foreach (UserDeckData deck in udata.decks)
            {
                if (index < deck_lines.Length)
                {
                    DeckLine line = deck_lines[index];
                    line.SetLine(udata, deck); // 显示已有卡组信息
                }

                index++;
            }

            // 如果还有空余行，则显示添加新卡组按钮
            if (index < deck_lines.Length)
            {
                DeckLine line = deck_lines[index];
                line.SetLine("+");
            }

            RefreshCardsQuantities(); // 同时刷新卡牌数量显示
        }

        // 刷新当前编辑卡组
        private void RefreshDeck(UserDeckData deck)
        {
            deck_title.text = "Deck Name"; // 默认名称
            current_deck_tid = GameTool.GenerateRandomID(7); // 生成随机ID
            deck_cards.Clear(); // 清空卡组数据
            saving = false;
            editing_deck = true; // 进入编辑状态

            // 禁用所有英雄技能按钮
            foreach (IconButton btn in hero_powers)
                btn.Deactivate();

            if (deck != null)
            {
                deck_title.text = deck.title; // 设置卡组名称
                current_deck_tid = deck.tid; // 设置当前卡组ID

                // 激活已选择的英雄技能
                foreach (IconButton btn in hero_powers)
                {
                    if (deck.hero != null && btn.value == deck.hero.tid)
                        btn.Activate();
                }

                // 遍历卡组中的卡牌，添加到编辑列表
                for (int i = 0; i < deck.cards.Length; i++)
                {
                    CardData card = CardData.Get(deck.cards[i].tid);
                    VariantData variant = VariantData.Get(deck.cards[i].variant);
                    if (card != null && variant != null)
                    {
                        AddDeckCard(card, variant, deck.cards[i].quantity);
                    }
                }
            }

            RefreshDeckCards(); // 刷新卡组显示
        }

        // 刷新卡组内卡牌列表
        private void RefreshDeckCards()
        {
            foreach (DeckLine line in deck_card_lines)
                line.Hide(); // 先隐藏所有卡牌行

            List<CardDataQ> list = new List<CardDataQ>();
            foreach (UserCardData card in deck_cards)
            {
                CardDataQ acard = new CardDataQ();
                acard.card = CardData.Get(card.tid);
                acard.variant = VariantData.Get(card.variant);
                acard.quantity = card.quantity;
                list.Add(acard);
            }

            // 按名称排序
            list.Sort((CardDataQ a, CardDataQ b) => { return a.card.title.CompareTo(b.card.title); });

            UserData udata = Authenticator.Get().UserData;
            int index = 0;
            int count = 0;

            foreach (CardDataQ card in list)
            {
                if (index >= deck_card_lines.Count)
                    CreateDeckCard(); // 动态生成卡牌行

                if (index < deck_card_lines.Count)
                {
                    DeckLine line = deck_card_lines[index];
                    if (line != null)
                    {
                        // 更新每一行显示，包括数量和是否灰色
                        line.SetLine(card.card, card.variant, card.quantity,
                            !IsCardOwned(udata, card.card, card.variant, card.quantity));
                        count += card.quantity; // 累计总卡牌数
                    }
                }

                index++;
            }

            // 显示卡组总数，并根据是否达到上限改变颜色
            deck_quantity.text = count + "/" + GameplayData.Get().deck_size;
            deck_quantity.color = count >= GameplayData.Get().deck_size ? Color.white : Color.red;

            RefreshCardsQuantities(); // 同步刷新卡牌数量
        }

        // 检查并显示初始卡组面板
        private void RefreshStarterDeck()
        {
            UserData udata = Authenticator.Get().UserData;
            if (udata != null && (udata.cards.Length == 0 || udata.rewards.Length == 0))
            {
                if (GameplayData.Get().starter_decks.Length > 0)
                {
                    StarterDeckPanel.Get().Show(); // 显示初始卡组面板
                }
            }
        }

        //-------- 卡组编辑操作 ----------

        // 创建新的卡组行
        private void CreateDeckCard()
        {
            GameObject deck_line = Instantiate(deck_cards_prefab, deck_grid.transform);
            DeckLine line = deck_line.GetComponent<DeckLine>();
            deck_card_lines.Add(line);
            float height = deck_card_lines.Count * 70f + 20f;
            deck_content.sizeDelta = new Vector2(deck_content.sizeDelta.x, height); // 调整容器高度
            line.onClick += OnClickCardLine; // 左键点击事件
            line.onClickRight += OnRightClickCardLine; // 右键点击事件
        }

        // 添加卡牌到当前卡组
        private void AddDeckCard(CardData card, VariantData variant, int quantity = 1)
        {
            AddDeckCard(card.id, variant.id, quantity);
        }

        // 从当前卡组移除卡牌
        private void RemoveDeckCard(CardData card, VariantData variant)
        {
            RemoveDeckCard(card.id, variant.id);
        }

        // 添加卡牌到当前卡组（使用ID和变体）
        private void AddDeckCard(string tid, string variant, int quantity = 1)
        {
            UserCardData ucard = GetDeckCard(tid, variant); // 查找是否已存在
            if (ucard != null)
            {
                ucard.quantity += quantity; // 已存在则增加数量
            }
            else
            {
                ucard = new UserCardData(tid, variant);
                ucard.quantity = quantity; // 新增卡牌
                deck_cards.Add(ucard);
            }
        }

        // 从当前卡组移除指定数量卡牌
        private void RemoveDeckCard(string tid, string variant)
        {
            for (int i = deck_cards.Count - 1; i >= 0; i--)
            {
                UserCardData ucard = deck_cards[i];
                if (ucard.tid == tid && ucard.variant == variant)
                {
                    ucard.quantity--;

                    if (ucard.quantity <= 0)
                        deck_cards.RemoveAt(i); // 数量为0则删除
                }
            }
        }

        // 根据ID和变体查找卡组内卡牌
        private UserCardData GetDeckCard(string tid, string variant)
        {
            foreach (UserCardData ucard in deck_cards)
            {
                if (ucard.tid == tid && ucard.variant == variant)
                    return ucard;
            }

            return null;
        }

        // 保存当前卡组
        private void SaveDeck()
        {
            UserData udata = Authenticator.Get().UserData;
            UserDeckData udeck = new UserDeckData();
            udeck.tid = current_deck_tid;
            udeck.title = deck_title.text; // 设置卡组名称
            udeck.hero = new UserCardData();
            udeck.hero.tid = GetSelectedHeroId(); // 记录英雄技能
            udeck.hero.variant = VariantData.GetDefault().id;
            udeck.cards = deck_cards.ToArray(); // 设置卡牌数组
            saving = true;

            if (Authenticator.Get().IsTest())
                SaveDeckTest(udata, udeck); // 测试模式保存

            if (Authenticator.Get().IsApi())
                SaveDeckAPI(udata, udeck); // API模式保存

            ShowDeckList(); // 返回卡组列表界面
        }

        // 测试模式保存卡组
        private async void SaveDeckTest(UserData udata, UserDeckData udeck)
        {
            udata.SetDeck(udeck);
            await Authenticator.Get().SaveUserData(); // 保存到本地
            ReloadUserDecks(); // 刷新卡组列表
        }

        // API模式保存卡组
        private async void SaveDeckAPI(UserData udata, UserDeckData udeck)
        {
            string url = ApiClient.ServerURL + "/users/deck/" + udeck.tid;
            string jdata = ApiTool.ToJson(udeck);
            WebResponse res = await ApiClient.Get().SendPostRequest(url, jdata); // 发送POST请求保存
            UserDeckData[] decks = ApiTool.JsonToArray<UserDeckData>(res.data);
            saving = res.success;

            if (res.success && decks != null)
            {
                udata.decks = decks;
                await Authenticator.Get().SaveUserData();
                ReloadUserDecks(); // 刷新卡组列表
            }
        }

        // 删除指定卡组
        private async void DeleteDeck(string deck_tid)
        {
            UserData udata = Authenticator.Get().UserData;
            UserDeckData udeck = udata.GetDeck(deck_tid);
            List<UserDeckData> decks = new List<UserDeckData>(udata.decks);
            decks.Remove(udeck); // 从数组移除
            udata.decks = decks.ToArray();

            if (Authenticator.Get().IsApi())
            {
                string url = ApiClient.ServerURL + "/users/deck/" + deck_tid;
                await ApiClient.Get().SendRequest(url, "DELETE", ""); // API删除请求
            }

            await Authenticator.Get().SaveUserData();
            ReloadUserDecks(); // 刷新卡组列表
        }

        //---- 左侧面板筛选点击事件 -----------

        // 点击队伍筛选按钮
        public void OnClickTeam(IconButton button)
        {
            filter_team = null;
            if (button.IsActive())
            {
                foreach (TeamData team in TeamData.GetAll())
                {
                    if (button.value == team.id)
                        filter_team = team; // 设置当前队伍筛选条件
                }
            }

            RefreshCards(); // 刷新卡牌列表
        }


        // 当切换左侧 Toggle（复选框）时刷新卡牌列表
        public void OnChangeToggle()
        {
            RefreshCards();
        }

        // 当选择下拉框（排序方式）时刷新卡牌列表
        public void OnChangeDropdown()
        {
            filter_dropdown = sort_dropdown.value; // 设置排序方式
            RefreshCards();
        }

        // 当搜索框文字变化时刷新卡牌列表
        public void OnChangeSearch()
        {
            filter_search = search.text; // 设置搜索条件
            RefreshCards();
        }

        // ---- 卡牌网格点击事件 ----------

        // 左键点击卡牌
        public void OnClickCard(CardUI card)
        {
            if (!editing_deck)
            {
                // 如果不在编辑卡组，显示卡牌详情面板
                CardZoomPanel.Get().ShowCard(card.GetCard(), card.GetVariant());
                return;
            }

            // 编辑卡组时，尝试将卡牌加入当前卡组
            CardData icard = card.GetCard();
            VariantData variant = card.GetVariant();
            if (icard != null)
            {
                int in_deck = CountDeckCards(icard, variant); // 当前卡组该卡数量
                int in_deck_same = CountDeckCards(icard); // 当前卡组该卡所有变体总数
                UserData udata = Authenticator.Get().UserData;

                bool owner = IsCardOwned(udata, card.GetCard(), card.GetVariant(), in_deck + 1); // 是否拥有足够数量
                bool deck_limit = in_deck_same < GameplayData.Get().deck_duplicate_max; // 是否未超过重复上限

                if (owner && deck_limit)
                {
                    AddDeckCard(icard, variant); // 添加卡牌到卡组
                    RefreshDeckCards(); // 刷新卡组显示
                }
            }
        }

        // 右键点击卡牌，显示卡牌详情
        public void OnClickCardRight(CardUI card)
        {
            CardZoomPanel.Get().ShowCard(card.GetCard(), card.GetVariant());
        }

        // ---- 右侧面板点击事件 --------

        // 点击卡组行
        public void OnClickDeckLine(DeckLine line)
        {
            if (line.IsHidden() || saving)
                return; // 行隐藏或正在保存则忽略

            UserDeckData deck = line.GetUserDeck();
            RefreshDeck(deck); // 刷新卡组编辑面板
            ShowDeckCards(); // 切换显示右侧卡组编辑
        }

        // 点击右侧卡牌行左键：从卡组移除卡牌
        private void OnClickCardLine(DeckLine line)
        {
            CardData card = line.GetCard();
            VariantData variant = line.GetVariant();
            if (card != null)
            {
                RemoveDeckCard(card, variant);
            }

            RefreshDeckCards(); // 刷新卡组显示
        }

        // 点击右侧卡牌行右键：显示卡牌详情
        private void OnRightClickCardLine(DeckLine line)
        {
            CardData icard = line.GetCard();
            if (icard != null)
                CardZoomPanel.Get().ShowCard(icard, line.GetVariant());
        }

        // ---- 卡组编辑按钮点击事件 -----

        // 点击保存卡组
        public void OnClickSaveDeck()
        {
            if (!saving)
            {
                SaveDeck(); // 保存卡组
            }
        }

        // 点击返回按钮，显示卡组列表
        public void OnClickDeckBack()
        {
            ShowDeckList();
        }

        // 点击删除当前编辑卡组
        public void OnClickDeleteDeck()
        {
            if (editing_deck && !string.IsNullOrEmpty(current_deck_tid))
            {
                DeleteDeck(current_deck_tid);
            }
        }

        // 点击卡组列表右侧删除按钮
        public void OnClickDeckDelete(DeckLine line)
        {
            if (line.IsHidden())
                return;

            UserDeckData deck = line.GetUserDeck();
            if (deck != null)
            {
                DeleteDeck(deck.tid);
            }
        }

        // ---- 获取卡组信息的方法 -----

        // 统计当前卡组中指定卡牌和变体的数量
        public int CountDeckCards(CardData card, VariantData cvariant)
        {
            int count = 0;
            foreach (UserCardData ucard in deck_cards)
            {
                if (ucard.tid == card.id && ucard.variant == cvariant.id)
                    count += ucard.quantity;
            }

            return count;
        }

        // 统计当前卡组中指定卡牌（所有变体）的数量
        public int CountDeckCards(CardData card)
        {
            int count = 0;
            foreach (UserCardData ucard in deck_cards)
            {
                if (ucard.tid == card.id)
                    count += ucard.quantity;
            }

            return count;
        }

        // 判断用户是否拥有指定数量的卡牌
        private bool IsCardOwned(UserData udata, CardData card, VariantData variant, int quantity)
        {
            return udata.GetCardQuantity(card, variant) >= quantity;
        }

        // 获取当前选中的英雄技能ID
        private string GetSelectedHeroId()
        {
            foreach (IconButton btn in hero_powers)
            {
                if (btn.IsActive())
                    return btn.value;
            }

            return "";
        }

        // ---- 面板显示相关 -----

        // 显示CollectionPanel
        public override void Show(bool instant = false)
        {
            base.Show(instant);
            RefreshAll(); // 刷新全部内容
            ShowDeckList(); // 显示卡组列表界面
        }

        // 获取当前CollectionPanel单例
        public static CollectionPanel Get()
        {
            return instance;
        }

        // ---- 辅助数据结构 -----

        // 用于存储卡牌+变体+数量信息，便于排序和显示
        public struct CardDataQ
        {
            public CardData card;
            public VariantData variant;
            public int quantity;
        }
    }
}
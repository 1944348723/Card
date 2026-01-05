using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine.Client;
using UnityEngine.Events;
using TcgEngine.UI;
using TcgEngine.FX;

namespace TcgEngine.Client
{
    /// <summary>
    /// 表示棋盘上卡牌的视觉表现。
    /// 会读取 Card.cs 中的数据，并在界面上显示卡牌信息。
    /// </summary>

    public class BoardCard : MonoBehaviour
    {
        public SpriteRenderer card_sprite;    // 卡牌主体精灵
        public SpriteRenderer card_glow;      // 卡牌发光效果
        public SpriteRenderer card_shadow;    // 卡牌阴影效果

        public Image armor_icon;              // 护甲图标
        public Text armor;                    // 护甲数值文本

        public CanvasGroup status_group;      // 状态栏 CanvasGroup，用于控制透明度
        public Text status_text;              // 状态栏文本

        public BoardCardEquip equipment;      // 卡牌装备显示对象

        public AbilityButton[] buttons;       // 卡牌技能按钮数组

        public Color glow_ally;               // 友方卡牌发光颜色
        public Color glow_enemy;              // 敌方卡牌发光颜色

        public UnityAction onKill;            // 卡牌死亡回调事件

        private CardUI card_ui;               // 卡牌UI组件
        private BoardCardFX card_fx;          // 卡牌特效组件
        private Canvas canvas;                // 卡牌Canvas组件

        private string card_uid = "";         // 卡牌唯一ID
        private bool destroyed = false;       // 是否已被销毁
        private bool focus = false;           // 是否获得焦点或鼠标悬停
        private float timer = 0f;             // 内部计时器
        private float status_alpha_target = 0f;// 状态栏目标透明度
        private float delayed_damage_timer = 0f;// 延迟显示伤害计时器
        private int prev_hp = 0;              // 上一帧HP，用于延迟显示伤害

        private bool back_to_hand;            // 是否回手动画
        private Vector3 back_to_hand_target;  // 回手目标位置

        private static List<BoardCard> card_list = new List<BoardCard>(); // 所有BoardCard实例列表

        void Awake()
        {
            card_list.Add(this);  // 添加到全局卡牌列表
            card_ui = GetComponent<CardUI>();      // 获取卡牌UI组件
            card_fx = GetComponent<BoardCardFX>(); // 获取卡牌特效组件
            canvas = GetComponentInChildren<Canvas>(); // 获取子Canvas
            card_glow.color = new Color(card_glow.color.r, card_glow.color.g, card_glow.color.b, 0f); // 初始发光透明
            canvas.gameObject.SetActive(false);    // 初始隐藏Canvas
            status_alpha_target = 0f;

            if (equipment != null)
                equipment.Hide();  // 隐藏装备显示

            if (status_group != null)
                status_group.alpha = 0f; // 隐藏状态栏
        }

        void OnDestroy()
        {
            card_list.Remove(this); // 移除全局卡牌列表
        }

        private void Start()
        {
            // 随机微小旋转，让卡牌排列更自然
            Vector3 board_rot = GameBoard.Get().GetAngles();
            transform.rotation = Quaternion.Euler(board_rot.x, board_rot.y, board_rot.z + Random.Range(-1f, 1f));
        }

        void Update()
        {
            if (!GameClient.Get().IsReady())
                return;

            delayed_damage_timer -= Time.deltaTime;  // 更新延迟伤害计时
            timer += Time.deltaTime;                  // 更新通用计时器
            if (timer > 0.15f && !destroyed && !canvas.gameObject.activeSelf)
                canvas.gameObject.SetActive(true);   // 延迟显示Canvas

            PlayerControls controls = PlayerControls.Get(); // 获取玩家操作
            Game data = GameClient.Get().GetGameData();     // 获取游戏数据
            Player player = GameClient.Get().GetPlayer();   // 获取自己玩家信息
            Card card = data.GetCard(card_uid);            // 获取当前卡牌数据

            if (!destroyed)
            {
                card_ui.SetCard(card);        // 更新卡牌UI
                card_ui.SetHP(prev_hp);       // 显示上一帧HP，实现延迟伤害效果
            }

            // 保存上一帧HP值，如果没有延迟伤害
            if (!IsDamagedDelayed())
                prev_hp = card.GetHP();

            bool selected = controls.GetSelected() == this;    // 是否被选中
            Vector3 targ_pos = GetTargetPos();                // 获取卡牌目标位置
            float speed = 12f;                                // 平滑移动速度

            transform.position = Vector3.MoveTowards(transform.position, targ_pos, speed * Time.deltaTime); // 平滑移动到目标位置

            // 计算发光目标透明度
            float target_alpha = IsFocus() || selected ? 1f : 0f;
            if (destroyed || timer < 1f)
                target_alpha = 0f;
            if (equipment != null && equipment.IsFocus())
                target_alpha = 0f;

            // 设置发光颜色
            Color ccolor = player.player_id == card.player_id ? glow_ally : glow_enemy;
            float calpha = Mathf.MoveTowards(card_glow.color.a, target_alpha * ccolor.a, 4f * Time.deltaTime);
            card_glow.color = new Color(ccolor.r, ccolor.g, ccolor.b, calpha);

            card_shadow.enabled = !destroyed && timer > 0.4f; // 阴影显示
            card_sprite.color = card.HasStatus(StatusType.Stealth) ? Color.gray : Color.white; // 潜行状态显示灰色
            card_ui.hp.color = (destroyed || card.damage > 0) ? Color.yellow : Color.white;   // 受伤显示黄色

            // 护甲显示
            int armor_val = card.GetStatusValue(StatusType.Armor);
            armor.text = armor_val.ToString();
            armor.enabled = armor_val > 0;
            armor_icon.enabled = armor_val > 0;

            // 更新卡牌图片
            Sprite sprite = card.CardData.GetBoardArt(card.VariantData);
            if (sprite != card_sprite.sprite)
                card_sprite.sprite = sprite;

            // 更新卡牌框架图片
            Sprite frame = card.VariantData.frame_board;
            if (frame != null && card_ui.frame_image != null)
                card_ui.frame_image.sprite = frame;

            // 装备显示
            if (equipment != null)
            {
                Card equip = data.GetEquipCard(card.equipped_uid);
                equipment.SetEquip(equip);
            }

            // 隐藏技能按钮
            foreach (AbilityButton button in buttons)
                button.Hide();

            // 显示选中卡牌的技能按钮
            if (selected && card.player_id == player.player_id)
            {
                int index = 0;
                List<AbilityData> abilities = card.GetAbilities();
                foreach (AbilityData iability in abilities)
                {
                    if (iability != null && iability.trigger == AbilityTrigger.Activate)
                    {
                        if (index < buttons.Length)
                        {
                            AbilityButton button = buttons[index];
                            button.SetAbility(card, iability);                          // 设置技能按钮数据
                            button.SetInteractable(data.CanCastAbility(card, iability)); // 设置按钮是否可用
                        }
                        index++;
                    }
                }

                // 显示装备技能
                Card equip = data.GetEquipCard(card.equipped_uid);
                if (equip != null)
                {
                    List<AbilityData> equip_abilities = equip.GetAbilities();
                    foreach (AbilityData iability in equip_abilities)
                    {
                        if (iability != null && iability.trigger == AbilityTrigger.Activate)
                        {
                            if (index < buttons.Length)
                            {
                                AbilityButton button = buttons[index];
                                button.SetAbility(equip, iability);
                                button.SetInteractable(data.CanCastAbility(equip, iability));
                            }
                            index++;
                        }
                    }
                }
            }

            // 更新状态栏透明度
            if (status_group != null)
                status_group.alpha = Mathf.MoveTowards(status_group.alpha, status_alpha_target, 5f * Time.deltaTime);
        }

        // 获取卡牌目标位置（槽位或回手动画目标）
        private Vector3 GetTargetPos()
        {
            Game data = GameClient.Get().GetGameData();
            Card card = data.GetCard(card_uid);

            if (destroyed && back_to_hand && timer > 0.5f)
                return back_to_hand_target;

            BSlot slot = BSlot.Get(card.slot);
            if (slot != null)
            {
                Vector3 targ_pos = slot.GetPosition(card.slot);
                return targ_pos;
            }

            return transform.position;
        }

        // 设置卡牌数据
        public void SetCard(Card card)
        {
            this.card_uid = card.uid;

            transform.position = GetTargetPos();
            prev_hp = card.GetHP();

            CardData icard = CardData.Get(card.card_id);
            if (icard)
            {
                card_ui.SetCard(card);                             // 设置UI显示
                card_sprite.sprite = icard.GetBoardArt(card.VariantData); // 设置卡牌图片
                armor.enabled = false;                              // 隐藏护甲
                armor_icon.enabled = false;
                status_alpha_target = 0f;                           // 隐藏状态条
            }
        }

        // 设置渲染顺序
        public void SetOrder(int order)
        {
            card_sprite.sortingOrder = order;
            canvas.sortingOrder = order + 1;
        }

        // 销毁卡牌
        public void Destroy()
        {
            if (!destroyed)
            {
                Game data = GameClient.Get().GetGameData();
                Card card = data.GetCard(card_uid);
                Player player = data.GetPlayer(card.player_id);

                destroyed = true;
                timer = 0f;
                status_alpha_target = 0f;
                card_glow.enabled = false;
                card_shadow.enabled = false;

                SetOrder(card_sprite.sortingOrder - 2);  // 调整渲染顺序
                Destroy(gameObject, 1.3f);              // 延迟销毁

                TimeTool.WaitFor(0.8f, () =>
                {
                    canvas.gameObject.SetActive(false); // 隐藏UI
                });

                GameBoard board = GameBoard.Get();
                if (player.HasCard(player.cards_hand, card) || player.HasCard(player.cards_deck, card))
                {
                    back_to_hand = true;
                    back_to_hand_target = player.player_id == GameClient.Get().GetPlayerID() ? -board.transform.up : board.transform.up;
                    back_to_hand_target = back_to_hand_target * 10f; // 设置回手动画方向和距离
                }

                if (!back_to_hand)
                {
                    card.hp = 0;
                    card_ui.SetCard(card);
                }

                if (onKill != null)
                    onKill.Invoke(); // 调用死亡回调
            }
        }

        // 延迟显示伤害
        public void DelayDamage(int damage, float duration = 1f)
        {
            if (damage != 0)
            {
                delayed_damage_timer = duration;
            }
        }

        // 判断是否存在延迟伤害
        public bool IsDamagedDelayed()
        {
            return delayed_damage_timer > 0f;
        }

        // 显示状态栏文字
        private void ShowStatusBar()
        {
            Card card = GetCard();
            if (card != null && status_text != null && !destroyed)
            {
                string stxt = GetStatusText();  // 状态文本
                string ttxt = GetTraitText();   // 特质文本

                if (stxt.Length > 0 && ttxt.Length > 0)
                    status_text.text = ttxt + ", " + stxt;
                else
                    status_text.text = ttxt + stxt;
            }

            bool show_status = status_text != null && status_text.text.Length > 0;
            status_alpha_target = show_status ? 1f : 0f; // 控制状态栏显示
        }


        /// <summary>
        /// 获取卡牌的状态文本（如：护甲、沉默、冻结等）
        /// 将所有状态拼接成一个字符串，用于显示在状态栏
        /// </summary>
        public string GetStatusText()
        {
            Card card = GetCard();
            string txt = "";
            foreach (CardStatus astatus in card.GetAllStatus())
            {
                StatusData istats = StatusData.Get(astatus.type);
                if (istats != null && !string.IsNullOrEmpty(istats.title))
                {
                    int ival = Mathf.Max(astatus.value, Mathf.CeilToInt(astatus.duration / 2f));
                    string sval = ival > 1 ? " " + ival : "";
                    txt += istats.GetTitle() + sval + ", ";
                }
            }
            if (txt.Length > 2)
                txt = txt.Substring(0, txt.Length - 2);
            return txt;
        }

        /// <summary>
        /// 获取卡牌的特质文本（如：嘲讽、吸血等）
        /// 将所有特质拼接成一个字符串，用于显示在状态栏
        /// </summary>
        public string GetTraitText()
        {
            Card card = GetCard();
            string txt = "";
            foreach (CardTrait atrait in card.GetAllTraits())
            {
                TraitData itrait = TraitData.Get(atrait.id);
                if (itrait != null && !string.IsNullOrEmpty(itrait.title))
                {
                    int ival = atrait.value;
                    string sval = ival > 1 ? " " + ival : "";
                    txt += itrait.GetTitle() + sval + ", ";
                }
            }
            if (txt.Length > 2)
                txt = txt.Substring(0, txt.Length - 2);
            return txt;
        }

        /// <summary>
        /// 判断卡牌是否已死亡（已销毁）
        /// </summary>
        public bool IsDead()
        {
            return destroyed;
        }

        /// <summary>
        /// 判断该卡牌是否当前处于焦点状态（鼠标悬停或选中）
        /// </summary>
        public bool IsFocus()
        {
            return focus;
        }

        /// <summary>
        /// 判断装备卡是否处于焦点状态
        /// </summary>
        public bool IsEquipFocus()
        {
            return equipment != null && equipment.IsFocus();
        }

        /// <summary>
        /// 鼠标进入卡牌时触发
        /// 显示状态栏，如果在移动端或UI被打开则不处理
        /// </summary>
        public void OnMouseEnter()
        {
            if (GameUI.IsUIOpened())
                return;

            if (GameTool.IsMobile())
                return;

            focus = true;
            ShowStatusBar();
        }

        /// <summary>
        /// 鼠标离开卡牌时触发
        /// 隐藏状态栏
        /// </summary>
        public void OnMouseExit()
        {
            focus = false;
            status_alpha_target = 0f;
        }

        /// <summary>
        /// 鼠标按下卡牌时触发
        /// 选中卡牌，如果在移动端则同时显示状态栏
        /// </summary>
        public void OnMouseDown()
        {
            if (GameUI.IsOverUILayer("UI"))
                return;

            PlayerControls.Get().SelectCard(this);

            if (GameTool.IsMobile())
            {
                focus = true;
                ShowStatusBar();
            }
        }

        /// <summary>
        /// 鼠标松开卡牌时触发（目前未实现功能）
        /// </summary>
        public void OnMouseUp()
        {

        }

        /// <summary>
        /// 鼠标悬停卡牌时触发
        /// 右键点击时选中卡牌右键操作
        /// </summary>
        public void OnMouseOver()
        {
            if (Input.GetMouseButtonDown(1))
            {
                PlayerControls.Get().SelectCardRight(this);
            }
        }

        /// <summary>
        /// 获取卡牌唯一ID
        /// </summary>
        public string GetCardUID()
        {
            return card_uid;
        }

        /// <summary>
        /// 获取主卡（非装备卡）
        /// </summary>
        public Card GetCard()
        {
            Game data = GameClient.Get().GetGameData();
            Card card = data.GetCard(card_uid);
            return card;
        }

        /// <summary>
        /// 获取装备卡
        /// </summary>
        public Card GetEquipCard()
        {
            Game data = GameClient.Get().GetGameData();
            Card card = GetCard();
            Card equip = data?.GetEquipCard(card.equipped_uid);
            return equip;
        }

        /// <summary>
        /// 根据焦点返回当前焦点卡牌（主卡或装备卡）
        /// </summary>
        public Card GetFocusCard()
        {
            if (IsEquipFocus())
                return GetEquipCard();
            return GetCard();
        }

        /// <summary>
        /// 获取卡牌数据（CardData）
        /// </summary>
        public CardData GetCardData()
        {
            Card card = GetCard();
            if (card != null)
                return CardData.Get(card.card_id);
            return null;
        }

        /// <summary>
        /// 获取卡牌所在格子
        /// </summary>
        public Slot GetSlot()
        {
            return GetCard().slot;
        }

        /// <summary>
        /// 获取卡牌的FX组件
        /// </summary>
        public BoardCardFX GetCardFX()
        {
            return card_fx;
        }

        /// <summary>
        /// 获取卡牌的CardData属性
        /// </summary>
        public CardData CardData { get { return GetCardData(); } }

        /// <summary>
        /// 获取指定玩家在棋盘上的卡牌数量
        /// </summary>
        public static int GetNbCardsBoardPlayer(int player_id)
        {
            int nb = 0;
            foreach (BoardCard acard in card_list)
            {
                if (acard != null && acard.GetCard().player_id == player_id)
                    nb++;
            }
            return nb;
        }

        /// <summary>
        /// 获取离指定位置最近的敌方卡牌
        /// </summary>
        public static BoardCard GetNearestPlayer(Vector3 pos, int skip_player_id, BoardCard skip, float range = 2f)
        {
            BoardCard nearest = null;
            float min_dist = range;
            foreach (BoardCard card in card_list)
            {
                float dist = (card.transform.position - pos).magnitude;
                if (dist < min_dist && card != skip && skip_player_id != card.GetCard().player_id)
                {
                    min_dist = dist;
                    nearest = card;
                }
            }
            return nearest;
        }

        /// <summary>
        /// 获取离指定位置最近的卡牌（可包括己方）
        /// </summary>
        public static BoardCard GetNearest(Vector3 pos, BoardCard skip, float range = 2f)
        {
            BoardCard nearest = null;
            float min_dist = range;
            foreach (BoardCard card in card_list)
            {
                float dist = (card.transform.position - pos).magnitude;
                if (dist < min_dist && card != skip)
                {
                    min_dist = dist;
                    nearest = card;
                }
            }
            return nearest;
        }

        /// <summary>
        /// 获取当前被鼠标或装备焦点选中的卡牌
        /// </summary>
        public static BoardCard GetFocus()
        {
            if (GameUI.IsOverUI())
                return null;

            foreach (BoardCard card in card_list)
            {
                if (card.IsFocus() || card.IsEquipFocus())
                    return card;
            }
            return null;
        }

        /// <summary>
        /// 取消所有卡牌的焦点状态
        /// </summary>
        public static void UnfocusAll()
        {
            foreach (BoardCard card in card_list)
            {
                card.focus = false;
                card.status_alpha_target = 0f;
            }
        }

        /// <summary>
        /// 根据卡牌UID获取BoardCard实例
        /// </summary>
        public static BoardCard Get(string uid)
        {
            foreach (BoardCard card in card_list)
            {
                if (card.card_uid == uid)
                    return card;
            }
            return null;
        }

        /// <summary>
        /// 获取所有BoardCard实例
        /// </summary>
        public static List<BoardCard> GetAll()
        {
            return card_list;
        }

    }
}
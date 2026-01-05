using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine.Client;
using TcgEngine.UI;

namespace TcgEngine.Client
{
    /// <summary>
    /// 手牌卡包区域类
    /// 根据玩家数据生成/销毁手牌卡包的视觉对象
    /// </summary>
    public class HandPackArea : MonoBehaviour
    {
        public RectTransform hand_area;    // 手牌区域的RectTransform
        public GameObject pack_template;   // 卡包预制体模板
        public float card_spacing = 100f;  // 卡包间距
        public float card_angle = 10f;     // 卡包旋转角度
        public float card_offset_y = 10f;  // Y方向偏移，用于卡包扇形排列

        private List<HandPack> packs = new List<HandPack>(); // 当前手牌区域中的所有卡包对象列表

        private Vector3 start_pos;        // 手牌区域初始位置
        private bool is_dragging;         // 当前是否有卡包被拖拽
        private bool is_locked;           // 区域是否被锁定（锁定时位置下移）
        private string last_destroyed;    // 上一个被销毁的卡包ID
        private float last_destroyed_timer = 0f; // 上一个销毁的计时器，用于延迟刷新

        private static HandPackArea _instance; // 单例

        void Awake()
        {
            _instance = this;
        }

        private void Start()
        {
            pack_template.SetActive(false);   // 模板隐藏
            start_pos = hand_area.anchoredPosition; // 记录初始位置

            if (Authenticator.Get().IsConnected())
                LoadPacks();   // 如果已登录，加载卡包
            else
                RefreshLogin(); // 否则刷新登录状态
        }

        /// <summary>
        /// 刷新登录状态，如果成功则加载卡包，否则跳转登录界面
        /// </summary>
        private async void RefreshLogin()
        {
            bool success = await Authenticator.Get().RefreshLogin();
            if (success)
                LoadPacks();
            else
                SceneNav.GoTo("LoginMenu");
        }

        /// <summary>
        /// 异步加载玩家的卡包数据
        /// </summary>
        public async void LoadPacks()
        {
            UserData udata = await Authenticator.Get().LoadUserData();
            if (udata != null)
            {
                RefreshPacks(); // 刷新视觉卡包
            }
        }

        /// <summary>
        /// 根据玩家数据刷新手牌区域，生成新卡包并移除不存在的卡包
        /// </summary>
        public void RefreshPacks()
        {
            UserData udata = Authenticator.Get().UserData;

            // 添加新的卡包
            foreach (UserCardData pack in udata.packs)
            {
                PackData dpack = PackData.Get(pack.tid);
                if (dpack != null && !HasPack(pack.tid))
                    SpawnNewPack(pack);
            }

            // 移除玩家已没有的卡包
            for (int i = packs.Count - 1; i >= 0; i--)
            {
                HandPack pack = packs[i];
                if (pack == null || !udata.HasPack(pack.GetPackTid()))
                {
                    packs.RemoveAt(i);
                    if (pack)
                        pack.Remove();
                }
            }
        }

        void Update()
        {
            last_destroyed_timer += Time.deltaTime;

            // 根据锁定状态调整手牌区域位置
            Vector3 tpos = is_locked ? (start_pos + Vector3.down * 200f) : start_pos;
            hand_area.anchoredPosition = Vector3.MoveTowards(hand_area.anchoredPosition, tpos, 200f * Time.deltaTime);

            // 设置卡包索引和扇形排列
            int index = 0;
            float count_half = packs.Count / 2f;
            foreach (HandPack card in packs)
            {
                card.deck_position = new Vector2((index - count_half) * card_spacing, 
                                                 (index - count_half) * (index - count_half) * -card_offset_y);
                card.deck_angle = (index - count_half) * -card_angle;
                index++;
            }

            // 更新是否有卡包被拖拽状态
            HandPack drag_pack = HandPack.GetDrag();
            is_dragging = drag_pack != null;
        }

        /// <summary>
        /// 生成新的视觉卡包对象
        /// </summary>
        public void SpawnNewPack(UserCardData pack)
        {
            GameObject card_obj = Instantiate(pack_template, hand_area.transform);
            card_obj.SetActive(true);
            card_obj.GetComponent<HandPack>().SetPack(pack);
            card_obj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -100f);
            packs.Add(card_obj.GetComponent<HandPack>());
        }

        /// <summary>
        /// 延迟刷新，用于处理刚销毁的卡包的过渡效果
        /// </summary>
        public void DelayRefresh(Card card)
        {
            last_destroyed_timer = 0f;
            last_destroyed = card.uid;
        }

        /// <summary>
        /// 锁定手牌区域（锁定时区域下移）
        /// </summary>
        public void Lock(bool locked)
        {
            is_locked = locked;
        }

        /// <summary>
        /// 根据X轴排序卡包的显示顺序
        /// </summary>
        public void SortCards()
        {
            packs.Sort(SortFunc);

            int i = 0;
            foreach (HandPack acard in packs)
            {
                acard.transform.SetSiblingIndex(i);
                i++;
            }
        }

        /// <summary>
        /// 排序比较函数，根据X坐标升序排列
        /// </summary>
        private int SortFunc(HandPack a, HandPack b)
        {
            return a.transform.position.x.CompareTo(b.transform.position.x);
        }

        /// <summary>
        /// 判断是否已存在指定tid的卡包
        /// </summary>
        public bool HasPack(string pack_tid)
        {
            HandPack card = HandPack.Get(pack_tid);
            bool just_destroyed = pack_tid == last_destroyed && last_destroyed_timer < 0.5f;
            return card != null || just_destroyed;
        }

        /// <summary>
        /// 是否有卡包正在拖拽
        /// </summary>
        public bool IsDragging()
        {
            return is_dragging;
        }

        /// <summary>
        /// 区域是否锁定
        /// </summary>
        public bool IsLocked()
        {
            return is_locked;
        }

        /// <summary>
        /// 单例获取
        /// </summary>
        public static HandPackArea Get()
        {
            return _instance;
        }
    }
}

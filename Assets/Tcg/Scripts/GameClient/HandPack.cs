using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine.FX;
using TcgEngine.Client;

namespace TcgEngine.Client
{
    /// <summary>
    /// 手中卡包的视觉显示类，用于 OpenPack 场景
    /// 负责显示卡包、拖拽效果、焦点高亮和打开动画
    /// </summary>
    public class HandPack : MonoBehaviour
    {
        public Image pack_sprite;       // 卡包的主图像
        public Image pack_glow;         // 卡包高亮效果
        public Text pack_quantity;      // 卡包数量显示文本

        public float move_speed = 10f;         // 移动速度
        public float move_rotate_speed = 4f;   // 移动旋转速度
        public float move_max_rotate = 10f;    // 最大旋转角度

        [HideInInspector]
        public Vector2 deck_position;  // 卡包在手牌区域的目标位置
        [HideInInspector]
        public float deck_angle;       // 卡包在手牌区域的旋转角度

        [Header("FX")]
        public GameObject pack_open_fx;       // 打开卡包的特效
        public AudioClip pack_open_audio;     // 打开卡包的音效

        private string pack_tid = "";         // 卡包唯一ID
        private int quantity = 0;             // 当前卡包数量

        private RectTransform hand_transform; // 手牌区域RectTransform
        private RectTransform card_transform; // 卡包自身RectTransform
        private Vector3 start_scale;          // 初始缩放
        private float current_alpha = 0f;     // 当前透明度
        private Vector3 current_rotate;       // 当前旋转角
        private Vector3 target_rotate;        // 目标旋转角
        private Vector3 prev_pos;             // 上一帧的位置，用于计算旋转

        private bool destroyed = false;       // 是否已经销毁
        private float focus_timer = 0f;       // 焦点计时器

        private bool focus = false;           // 鼠标悬停焦点状态
        private bool drag = false;            // 是否正在拖拽

        private static List<HandPack> pack_list = new List<HandPack>(); // 当前场景中所有手牌卡包列表

        void Awake()
        {
            pack_list.Add(this); // 添加到列表
            card_transform = transform.GetComponent<RectTransform>();
            hand_transform = transform.parent.GetComponent<RectTransform>();
            start_scale = transform.localScale;
        }

        private void OnDestroy()
        {
            pack_list.Remove(this); // 从列表移除
        }

        void Update()
        {
            focus_timer += Time.deltaTime;

            Vector2 target_position = deck_position;
            Vector3 target_size = start_scale;
            float target_alpha = 1f;

            bool player_dragging = HandPackArea.Get().IsDragging();

            // 鼠标悬停时提升位置
            if (focus && focus_timer > 0.5f)
            {
                target_position = deck_position + Vector2.up * 40f;
            }

            // 拖拽逻辑
            if (drag)
            {
                target_position = GetTargetPosition();
                target_size = start_scale * 0.8f;
                Vector3 dir = card_transform.position - prev_pos;
                Vector3 addrot = new Vector3(dir.y * 90f, -dir.x * 90f, 0f);
                target_rotate += addrot * move_rotate_speed * Time.deltaTime;
                target_rotate = new Vector3(Mathf.Clamp(target_rotate.x, -move_max_rotate, move_max_rotate),
                                            Mathf.Clamp(target_rotate.y, -move_max_rotate, move_max_rotate), 0f);
                current_rotate = Vector3.Lerp(current_rotate, target_rotate, move_rotate_speed * Time.deltaTime);
                move_speed = 9f;
                target_alpha = 0.8f;
            }
            else
            {
                target_rotate = new Vector3(0f, 0f, deck_angle);
                current_rotate = new Vector3(0f, 0f, deck_angle);
            }

            // 更新位置、旋转和缩放
            card_transform.anchoredPosition = Vector2.Lerp(card_transform.anchoredPosition, target_position, Time.deltaTime * move_speed);
            card_transform.rotation = Quaternion.Slerp(card_transform.rotation, Quaternion.Euler(current_rotate), Time.deltaTime * move_speed);
            card_transform.localScale = Vector3.Lerp(card_transform.localScale, target_size, 4f * Time.deltaTime);

            // 更新高亮和透明度
            pack_glow.enabled = (focus && !player_dragging) || drag;
            current_alpha = Mathf.MoveTowards(current_alpha, target_alpha, 2f * Time.deltaTime);
            pack_sprite.color = new Color(1f, 1f, 1f, current_alpha);
            pack_glow.color = new Color(pack_glow.color.r, pack_glow.color.g, pack_glow.color.b, current_alpha * 0.8f);
            pack_quantity.text = quantity.ToString();

            prev_pos = Vector3.Lerp(prev_pos, card_transform.position, 1f * Time.deltaTime);
        }

        /// <summary>
        /// 获取拖拽时的目标位置
        /// </summary>
        private Vector2 GetTargetPosition()
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(hand_transform, Input.mousePosition, Camera.main, out Vector2 tpos);
            return tpos;
        }

        /// <summary>
        /// 设置卡包显示数据
        /// </summary>
        public void SetPack(UserCardData pack)
        {
            this.pack_tid = pack.tid;
            this.quantity = pack.quantity;

            PackData ipack = PackData.Get(pack.tid);
            if (ipack)
            {
                pack_sprite.sprite = ipack.pack_img;
            }
        }

        /// <summary>
        /// 打开卡包，播放特效和音效
        /// </summary>
        public void OpenPack()
        {
            FXTool.DoFX(pack_open_fx, transform.position);
            AudioTool.Get().PlaySFX("pack_open", pack_open_audio);
            Destroy(gameObject);
            OpenPackMenu.Get().OpenPack(pack_tid);
        }

        /// <summary>
        /// 移除卡包数量，数量为0时销毁
        /// </summary>
        public void Remove()
        {
            quantity--;
            if (quantity <= 0)
                Kill();
        }

        /// <summary>
        /// 销毁卡包
        /// </summary>
        public void Kill()
        {
            if (!destroyed)
            {
                destroyed = true;
                Destroy(gameObject);
            }
        }

        public bool IsFocus() { return focus && !drag; }
        public bool IsDrag() { return drag; }

        public PackData GetPackData() { return PackData.Get(pack_tid); }
        public string GetPackTid() { return pack_tid; }

        public int GetPackQuantity()
        {
            UserData udata = Authenticator.Get().UserData;
            return udata.GetPackQuantity(pack_tid);
        }

        // 鼠标悬停事件
        public void OnMouseEnterCard()
        {
            if (HandPackArea.Get().IsLocked()) return;
            focus = true;
        }

        public void OnMouseExitCard()
        {
            focus = false;
            focus_timer = 0f;
        }

        // 鼠标按下事件
        public void OnMouseDownCard()
        {
            if (HandPackArea.Get().IsLocked()) return;
            drag = true;
            AudioTool.Get().PlaySFX("hand_card", AssetData.Get().hand_card_click_audio);
        }

        // 鼠标抬起事件
        public void OnMouseUpCard()
        {
            Vector3 world_pos = MouseToWorld(Input.mousePosition);
            if (drag && world_pos.y > -2.5f)
                OpenPack(); // 打开卡包
            else
                HandPackArea.Get().SortCards();
            drag = false;
        }

        // 将鼠标屏幕坐标转换为世界坐标
        public Vector3 MouseToWorld(Vector3 mouse_pos)
        {
            Vector3 wpos = Camera.main.ScreenToWorldPoint(mouse_pos);
            wpos.z = 0f;
            return wpos;
        }

        public PackData PackData { get { return GetPackData(); } }

        // 静态工具方法：获取当前拖拽的卡包
        public static HandPack GetDrag()
        {
            foreach (HandPack card in pack_list)
            {
                if (card.IsDrag()) return card;
            }
            return null;
        }

        // 静态工具方法：获取当前焦点卡包
        public static HandPack GetFocus()
        {
            foreach (HandPack card in pack_list)
            {
                if (card.IsFocus()) return card;
            }
            return null;
        }

        // 静态工具方法：通过tid获取卡包
        public static HandPack Get(string uid)
        {
            foreach (HandPack card in pack_list)
            {
                if (card && card.GetPackTid() == uid) return card;
            }
            return null;
        }

        // 静态工具方法：获取所有卡包
        public static List<HandPack> GetAll() { return pack_list; }
    }
}

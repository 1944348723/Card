using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TcgEngine.UI
{

    /// <summary>
    /// JoinCodePanel 类
    /// 面板类，用于玩家输入房间/游戏加入码进行匹配，或生成随机加入码。
    /// 继承自 UIPanel。
    /// </summary>
    public class JoinCodePanel : UIPanel
    {
        /// <summary>
        /// 输入框，用于玩家输入加入码
        /// </summary>
        public InputField code_field;

        /// <summary>
        /// 当前的游戏/房间代码
        /// </summary>
        private string game_code = "";

        /// <summary>
        /// 单例实例
        /// </summary>
        private static JoinCodePanel instance;

        /// <summary>
        /// Awake 生命周期，初始化单例
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            instance = this;
        }

        /// <summary>
        /// 每帧更新（继承自 UIPanel，可扩展）
        /// </summary>
        protected override void Update()
        {
            base.Update();
        }

        /// <summary>
        /// 点击“随机生成”按钮
        /// 随机生成一个长度在4到6之间的大写游戏码，并显示到输入框
        /// </summary>
        public void OnClickRandomize()
        {
            game_code = GameTool.GenerateRandomID(4,6).ToUpper();
            code_field.text = game_code;
        }

        /// <summary>
        /// 点击“加入游戏”按钮
        /// 读取输入框的内容，如果长度>=3，则开始匹配并隐藏面板
        /// </summary>
        public void OnClickJoinCode()
        {
            if (code_field.text.Length < 3)
                return;

            game_code = code_field.text.ToUpper();
            MainMenu.Get().StartMathmaking(GameMode.Casual, "code_" + game_code);
            Hide();
        }

        /// <summary>
        /// 显示面板
        /// 重写父类 Show 方法，每次显示时清空输入框
        /// </summary>
        public override void Show(bool instant = false)
        {
            base.Show(instant);
            code_field.text = "";
        }

        /// <summary>
        /// 获取当前游戏码
        /// </summary>
        /// <returns>字符串类型的游戏码</returns>
        public string GetCode()
        {
            return game_code;
        }

        /// <summary>
        /// 获取单例实例
        /// </summary>
        /// <returns>JoinCodePanel 单例</returns>
        public static JoinCodePanel Get()
        {
            return instance;
        }

    }
}

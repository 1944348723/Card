namespace TcgEngine.AI
{
    /// <summary>
    /// AI 决策层与执行层之间传递的动作描述。
    /// 搜索过程还会使用 score、sort 和 valid 保存候选筛选状态。
    /// </summary>
    public class AIAction
    {
        public ushort type;

        public string card_uid;
        public string target_uid;
        public int target_player_id;
        public string ability_id;
        public Slot slot;
        public int value;

        public int score;
        public int sort;
        public bool valid;

        public AIAction()
        {
        }

        public AIAction(ushort type)
        {
            this.type = type;
        }

        public string GetText(Game game)
        {
            string text = GameAction.GetString(type);
            Card card = game.GetCard(card_uid);
            Card target = game.GetCard(target_uid);

            if (card != null)
                text += " card " + card.card_id;
            if (target != null)
                text += " target " + target.card_id;
            if (slot != Slot.None)
                text += " slot " + slot.x + "-" + slot.p;
            if (ability_id != null)
                text += " ability " + ability_id;
            if (value > 0)
                text += " value " + value;

            return text;
        }

        /// <summary>
        /// 恢复对象池要求的默认状态。
        /// </summary>
        public void Clear()
        {
            type = GameAction.None;
            card_uid = null;
            target_uid = null;
            target_player_id = -1;
            ability_id = null;
            slot = Slot.None;
            value = -1;
            score = 0;
            sort = 0;
            valid = false;
        }
    }
}

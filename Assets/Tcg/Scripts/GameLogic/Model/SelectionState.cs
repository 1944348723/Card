namespace TcgEngine
{
    /// <summary>对当前交互选择上下文的一致只读快照。</summary>
    public readonly struct SelectionState
    {
        public SelectorType Type { get; }
        public int PlayerId { get; }
        public string AbilityId { get; }
        public string CasterUid { get; }
        public int SelectedValue { get; }
        public bool IsActive => Type != SelectorType.None;

        public SelectionState(SelectorType type, int playerId, string abilityId, string casterUid, int selectedValue)
        {
            Type = type;
            PlayerId = playerId;
            AbilityId = abilityId;
            CasterUid = casterUid;
            SelectedValue = selectedValue;
        }
    }
}

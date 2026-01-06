using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TcgEngine.UI
{
    /// <summary>
    /// 表示下拉菜单中的一项，可以存储一个ID和显示文本
    /// </summary>
    [System.Serializable]
    public class DropdownValueItem
    {
        public string id;   // 唯一标识ID
        public string text; // 显示在下拉菜单上的文本
    }

    /// <summary>
    /// 可绑定ID的下拉菜单组件
    /// 让每个下拉菜单项可以关联一个ID，而不仅仅是显示文本
    /// </summary>
    [RequireComponent(typeof(Dropdown))]
    public class DropdownValue : MonoBehaviour
    {
        public UnityAction<int, string> onValueChanged; // 下拉值改变时的回调，参数为索引和ID

        private List<DropdownValueItem> values = new List<DropdownValueItem>(); // 存储下拉菜单项及其ID
        private Dropdown dropdown; // Unity的Dropdown组件

        void Awake()
        {
            dropdown = GetComponent<Dropdown>();
            dropdown.onValueChanged.AddListener(OnChangeValue); // 绑定Unity Dropdown的值改变事件
        }

        private void Start()
        {

        }

        /// <summary>
        /// 添加一个下拉菜单选项
        /// </summary>
        public void AddOption(string id, string text)
        {
            Dropdown.OptionData option = new Dropdown.OptionData(text);
            dropdown.options.Add(option); // 添加到Unity Dropdown显示选项
            DropdownValueItem item = new DropdownValueItem();
            item.id = id;
            item.text = text;
            values.Add(item); // 添加到内部ID列表
            dropdown.RefreshShownValue();
        }

        /// <summary>
        /// 清空所有下拉菜单选项
        /// </summary>
        public void ClearOptions()
        {
            values.Clear();
            dropdown.ClearOptions();
        }

        /// <summary>
        /// 通过ID设置当前选中的下拉项
        /// </summary>
        public void SetValue(string value)
        {
            int index = 0;
            foreach (DropdownValueItem item in values)
            {
                if (item.id == value)
                    dropdown.value = index;
                index++;
            }
        }

        /// <summary>
        /// 通过索引设置当前选中的下拉项
        /// </summary>
        public void SetValue(int index)
        {
            if (index >= 0 && index < dropdown.options.Count)
                dropdown.value = index;
        }

        /// <summary>
        /// 当Unity Dropdown值改变时触发
        /// </summary>
        private void OnChangeValue(int selected_index)
        {
            if (selected_index >= 0 && selected_index < values.Count)
            {
                DropdownValueItem value = values[selected_index];
                onValueChanged?.Invoke(selected_index, value.id);
            }
        }

        /// <summary>
        /// 获取当前选中的下拉项对象
        /// </summary>
        public DropdownValueItem GetSelected()
        {
            if (dropdown.value >= 0 && dropdown.value < values.Count)
            {
                DropdownValueItem item = values[dropdown.value];
                return item;
            }
            return null;
        }

        /// <summary>
        /// 获取当前选中的下拉项ID
        /// </summary>
        public string GetSelectedValue()
        {
            DropdownValueItem item = GetSelected();
            if (item != null)
                return item.id;
            return "";
        }

        /// <summary>
        /// 获取当前选中的下拉项文本
        /// </summary>
        public string GetSelectedText()
        {
            DropdownValueItem item = GetSelected();
            if (item != null)
                return item.text;
            return "";
        }

        /// <summary>
        /// 获取当前选中的下拉项索引
        /// </summary>
        public int GetSelectedIndex()
        {
            return dropdown.value;
        }

        /// <summary>
        /// 获取下拉菜单项数量
        /// </summary>
        public int Count
        {
            get { return dropdown.options.Count; }
        }

        /// <summary>
        /// 获取或设置下拉菜单是否可交互
        /// </summary>
        public bool interactable
        {
            get { return dropdown.interactable; }
            set { dropdown.interactable = value; }
        }

        /// <summary>
        /// 获取或设置下拉菜单当前索引值
        /// </summary>
        public int value
        {
            get { return dropdown.value; }
            set { dropdown.value = value; dropdown.RefreshShownValue(); }
        }

        /// <summary>
        /// 获取下拉菜单所有项的ID和文本列表
        /// </summary>
        public List<DropdownValueItem> Items
        {
            get { return values; }
        }
    }
}

using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using tContentPatch;
using tContentPatch.Content.UI;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace BetterPrefix
{
    public class PresetPrefixesSetting : ModSetting
    {
        public override string Name => "预设前缀";
        public override string Title => "更好的前缀: 预设前缀";
        public override string FilePath => "presetPrefixes.json";
        public override Type DataType => typeof(List<int>);

        private List<int> _presetIds = new List<int> { 65, 72, 81, 82, 83, 84, 85 };
        private Action<List<int>> _updateUI;

        public static List<int> CurrentPresetIds { get; private set; } = new List<int>();

        public override void Load(object v)
        {
            if (v == null)
            {
                SetDefault();
                Save();
            }
            else
            {
                _presetIds = ((List<int>)v).ToList();
                _presetIds.Sort();
            }
            CurrentPresetIds = new List<int>(_presetIds);
        }

        public override object GetSaveData() => _presetIds;

        public override void SetDefault()
        {
            _presetIds = new List<int> { 65, 72, 81, 82, 83, 84, 85 };
            _presetIds.Sort();
            NeedSave = true;
            _updateUI?.Invoke(_presetIds);
        }

        public override UIElement GetUI()
        {
            // 垂直堆叠面板
            var stack = new UIStackPanel();
            stack.Width.Set(0, 1f);
            stack.Height.Set(0, 1f);
            stack.Horizontal = false;
            stack.ItemMargin = 5;
            stack.SetPadding(10);

            // 显示标签
            var displayLabel = new UIText("当前启用的前缀：");
            stack.Append(displayLabel);

            // 显示前缀列表（自动换行，自动高度）
            var prefixListText = new UIText(FormatPrefixList(_presetIds));
            prefixListText.Width.Set(0, 1f);
            prefixListText.IsWrapped = true;
            stack.Append(prefixListText);

            // 输入面板（水平排列）
            var inputPanel = new UIStackPanel();
            inputPanel.Horizontal = true;
            inputPanel.ItemMargin = 5;
            inputPanel.Width.Set(0, 1f);
            inputPanel.Height.Set(40, 0);

            var idInput = new UITextBox("");
            idInput.Width.Set(-150, 1f);
            idInput.Height.Set(30, 0);
            idInput.OnTextChanged += s =>
            {
                // 只允许数字
                string newText = Regex.Replace(s, @"[^\d]", "");
                if (newText != s) idInput.SetText(newText);
            };
            inputPanel.Append(idInput);

            var addButton = new UIButton1("添加");
            addButton.Width.Set(80, 0);
            addButton.Height.Set(30, 0);
            addButton.OnLeftClick += (evt, element) =>
            {
                if (int.TryParse(idInput.Text, out int id) && id >= 0)
                {
                    if (!_presetIds.Contains(id))
                    {
                        _presetIds.Add(id);
                        _presetIds.Sort();
                        RefreshUI(prefixListText);
                        NeedSave = true;
                    }
                    idInput.SetText("");
                }
            };
            inputPanel.Append(addButton);

            var deleteButton = new UIButton1("删除");
            deleteButton.Width.Set(80, 0);
            deleteButton.Height.Set(30, 0);
            deleteButton.OnLeftClick += (evt, element) =>
            {
                if (int.TryParse(idInput.Text, out int id))
                {
                    if (_presetIds.Remove(id))
                    {
                        _presetIds.Sort();
                        RefreshUI(prefixListText);
                        NeedSave = true;
                    }
                    idInput.SetText("");
                }
            };
            inputPanel.Append(deleteButton);

            stack.Append(inputPanel);

            // 重置按钮
            var resetButton = new UIButton1("重置为默认");
            resetButton.Width.Set(120, 0);
            resetButton.Height.Set(30, 0);
            resetButton.OnLeftClick += (evt, element) =>
            {
                SetDefault();
                RefreshUI(prefixListText);
            };
            stack.Append(resetButton);

            // 提示文字
            var hint = new UIText("修改后需要重新加载模组生效");
            hint.TextColor = Color.Gray;
            stack.Append(hint);

            // 保存刷新回调
            _updateUI = list =>
            {
                _presetIds = list;
                RefreshUI(prefixListText);
            };

            return stack;
        }

        private void RefreshUI(UIText target)
        {
            target.SetText(FormatPrefixList(_presetIds));
        }

        private string FormatPrefixList(List<int> ids)
        {
            if (ids == null || ids.Count == 0) return "无";
            var parts = new List<string>();
            foreach (int id in ids)
            {
                string name = GetPrefixName(id);
                parts.Add($"[{name}({id})]");
            }
            return string.Join(" ", parts);
        }

        private string GetPrefixName(int prefixId)
        {
            if (prefixId >= 0 && prefixId < Lang.prefix.Length && Lang.prefix[prefixId] != null)
                return Lang.prefix[prefixId].Value;
            return "未知";
        }
    }
}
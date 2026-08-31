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

namespace BetterBuffGet
{
    public class AvailableBuffsSetting : ModSetting
    {
        public override string Name => "可用增益";
        public override string Title => "更好的增益获取: 可用增益";
        public override string FilePath => "availableBuffs.json";
        public override Type DataType => typeof(List<int>);

        private List<int> _availableBuffIds = new List<int>
        {
            1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,48,76,
            104,105,106,107,108,109,110,111,112,113,114,115,116,117,
            121,122,123,124,207,257
        };

        private Action<List<int>> _updateUI;

        public static List<int> CurrentAvailableBuffs { get; private set; } = new List<int>();

        public override void Load(object v)
        {
            if (v == null)
            {
                SetDefault();
                Save();
            }
            else
            {
                _availableBuffIds = ((List<int>)v).ToList();
                _availableBuffIds.Sort();
            }
            CurrentAvailableBuffs = new List<int>(_availableBuffIds);
        }

        public override object GetSaveData() => _availableBuffIds;

        public override void SetDefault()
        {
            _availableBuffIds = new List<int>
            {
                1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,48,76,
                104,105,106,107,108,109,110,111,112,113,114,115,116,117,
                121,122,123,124,207,257
            };
            _availableBuffIds.Sort();
            NeedSave = true;
            _updateUI?.Invoke(_availableBuffIds);
            CurrentAvailableBuffs = new List<int>(_availableBuffIds);
        }

        public override void Save()
        {
            base.Save();
            CurrentAvailableBuffs = new List<int>(_availableBuffIds);
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
            var displayLabel = new UIText("当前启用的增益：");
            stack.Append(displayLabel);

            // 显示增益列表
            var buffListText = new UIText(FormatBuffList(_availableBuffIds));
            buffListText.Width.Set(0, 1f);
            buffListText.IsWrapped = true;
            stack.Append(buffListText);

            // 输入面板
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
                string newText = Regex.Replace(s, @"[^\d]", "");
                if (newText != s) idInput.SetText(newText);
            };
            inputPanel.Append(idInput);

            var addButton = new UIButton1("添加");
            addButton.Width.Set(80, 0);
            addButton.Height.Set(30, 0);
            addButton.OnLeftClick += (evt, element) =>
            {
                if (int.TryParse(idInput.Text, out int id) && id >= 0 && id < Terraria.ID.BuffID.Count)
                {
                    if (!_availableBuffIds.Contains(id))
                    {
                        _availableBuffIds.Add(id);
                        _availableBuffIds.Sort();
                        RefreshUI(buffListText);
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
                    if (_availableBuffIds.Remove(id))
                    {
                        _availableBuffIds.Sort();
                        RefreshUI(buffListText);
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
                RefreshUI(buffListText);
            };
            stack.Append(resetButton);

            // 提示文字
            var hint = new UIText("修改后需要点击保存并重载模组生效");
            hint.TextColor = Color.Gray;
            stack.Append(hint);

            // 保存刷新回调
            _updateUI = list =>
            {
                _availableBuffIds = list;
                RefreshUI(buffListText);
            };

            return stack;
        }

        private void RefreshUI(UIText target)
        {
            target.SetText(FormatBuffList(_availableBuffIds));
        }

        private string FormatBuffList(List<int> ids)
        {
            if (ids == null || ids.Count == 0) return "无";
            var parts = new List<string>();
            foreach (int id in ids)
            {
                string name = Lang.GetBuffName(id);
                if (string.IsNullOrEmpty(name)) name = "未知";
                parts.Add($"[{name}({id})]");
            }
            return string.Join(" ", parts);
        }
    }
}
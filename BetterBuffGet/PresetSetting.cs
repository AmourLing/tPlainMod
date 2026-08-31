using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using tContentPatch;
using tContentPatch.Content.UI;
using tContentPatch.Content.UI.ModSet;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace BetterBuffGet
{
    [Serializable]
    public class PresetEntry
    {
        public string name;
        public List<int> ids = new List<int>();
    }

    /// <summary>
    /// 预设方案: 把当前已选增益保存为命名方案, 可一键应用
    /// </summary>
    public class PresetSetting : ModSetting
    {
        public override string Name => "预设方案";
        public override string Title => "更好的增益获取: 预设方案";
        public override string FilePath => "presets.json";
        public override Type DataType => typeof(List<PresetEntry>);

        public static List<PresetEntry> CurrentPresets { get; private set; } = new List<PresetEntry>();

        public override void Load(object v)
        {
            if (v == null)
            {
                SetDefault();
                Save();
                return;
            }
            CurrentPresets = ((List<PresetEntry>)v)
                .Where(e => e != null && !string.IsNullOrEmpty(e.name) && e.ids != null)
                .ToList();
        }

        public override object GetSaveData() => CurrentPresets;

        public override void SetDefault()
        {
            CurrentPresets = new List<PresetEntry>();
            NeedSave = true;
        }

        /// <summary>把当前已选增益保存为新预设并落盘</summary>
        public static void Add(string name, IEnumerable<int> ids)
        {
            name = (name ?? "").Trim();
            if (name.Length == 0) return;

            // 重名则覆盖
            int idx = CurrentPresets.FindIndex(e => e.name == name);
            var entry = new PresetEntry { name = name, ids = ids.Distinct().OrderBy(i => i).ToList() };
            if (idx >= 0) CurrentPresets[idx] = entry;
            else CurrentPresets.Add(entry);

            var s = new PresetSetting();
            s.NeedSave = true;
            s.Save();
        }

        /// <summary>删除预设并落盘</summary>
        public static void Delete(string name)
        {
            int idx = CurrentPresets.FindIndex(e => e.name == name);
            if (idx < 0) return;
            CurrentPresets.RemoveAt(idx);
            var s = new PresetSetting();
            s.NeedSave = true;
            s.Save();
        }

        /// <summary>应用预设: 把已选增益设为该方案的集合 (收藏不变)</summary>
        public static void Apply(string name)
        {
            var e = CurrentPresets.FirstOrDefault(p => p.name == name);
            if (e == null) return;
            BuffSetting.ApplyPreset(e.ids);
        }

        public override UIElement GetUI()
        {
            var stack = new UIStackPanel();
            stack.Width.Set(0, 1f);
            stack.ItemMargin = 4;
            stack.IsAutoUpdateSize = true;

            var desc = new UIText("把当前已选增益保存为命名方案, 可一键应用", 0.8f);
            desc.TextColor = Color.Gray;
            stack.Append(desc);

            // 预设列表 (先建, 供下方保存按钮的回调引用)
            var scroll = new UIScrollViewer();
            scroll.Width.Set(0, 1f);
            scroll.Height.Set(220, 0);
            stack.Append(scroll);

            var list = new UIStackPanel();
            list.Width.Set(0, 1f);
            list.ItemMargin = 2;
            list.IsAutoUpdateSize = true;
            scroll.SetChild(list);
            list.OnUpdate += _ => list.UpdateContainer_Height();

            Action refreshList = null;
            refreshList = () =>
            {
                list.RemoveAllChildren();
                if (CurrentPresets.Count == 0)
                {
                    var empty = new UIText("暂无预设, 在上方输入名称后保存", 0.8f);
                    empty.TextColor = Color.Gray;
                    list.Append(empty);
                }
                else
                {
                    foreach (PresetEntry p in CurrentPresets)
                        list.Append(CreatePresetRow(p, refreshList));
                }
                list.UpdateContainer_Height();
            };

            // 命名 + 存为预设
            var addRow = new UIStackPanel();
            addRow.Horizontal = true;
            addRow.Width.Set(0, 1f);
            addRow.Height.Set(40, 0);
            addRow.ItemMargin = 6;
            addRow.Top.Set(0, 0);
            stack.Append(addRow);

            var nameBox = new UITextBox("预设名称");
            nameBox.Width.Set(-96, 1f);
            nameBox.Height.Set(30, 0);
            addRow.Append(nameBox);

            var saveBtn = new UIButton1("存为预设");
            saveBtn.Width.Set(80, 0);
            saveBtn.Height.Set(30, 0);
            saveBtn.OnLeftClick += (evt, element) =>
            {
                PresetSetting.Add(nameBox.Text, GetSelectedIds());
                nameBox.SetText("");
                refreshList();
            };
            addRow.Append(saveBtn);

            refreshList();
            return stack;
        }

        private static UIElement CreatePresetRow(PresetEntry preset, Action onDeleted)
        {
            var btn = new UIButton1($"{preset.name}  ({preset.ids.Count} 项)", 0.85f);
            btn.Width.Set(0, 1f);
            btn.Height.Set(28, 0);
            btn.EnableColorBack = new Color(58, 90, 130) * 0.85f;
            btn.MouseOverColorBack = new Color(74, 112, 160);
            btn.OnUpdate += _ =>
            {
                if (btn.IsMouseHovering)
                    Main.instance.MouseText($"{preset.name}  左键: 应用  右键: 删除");
            };
            string name = preset.name;
            btn.OnLeftClick += (evt, element) => Apply(name);
            btn.OnRightClick += (evt, element) =>
            {
                Delete(name);
                onDeleted?.Invoke();
            };
            return btn;
        }

        private static IEnumerable<int> GetSelectedIds() =>
            BuffSetting.CurrentSelectedBuffs.Where(kv => kv.Value).Select(kv => kv.Key);
    }
}

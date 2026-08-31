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
            // 每帧刷新换行容器的高度, 否则 UIList 拿不到正确的内容高度, 滚动条会失效
            Action<UIWrapPanel> keepHeight = wrap => wrap.UpdateContainer_Height();

            var stack = new UIStackPanel();
            stack.Width.Set(0, 1f);
            stack.ItemMargin = 4;
            stack.IsAutoUpdateSize = true;

            var desc = new UIText("点击格子切换启用: 绿=已启用, 红=未启用, 金=搜索匹配", 0.8f);
            desc.TextColor = Color.Gray;
            stack.Append(desc);

            // ===== 已启用前缀摘要 (点击标签可定位到表格中的格子) =====
            var enabledHeader = new UIText("已启用 (0)", 0.8f);
            stack.Append(enabledHeader);

            var enabledScroll = new UIScrollViewer();
            enabledScroll.Width.Set(0, 1f);
            enabledScroll.Height.Set(52, 0);
            stack.Append(enabledScroll);

            var enabledWrap = new UIWrapPanel();
            enabledWrap.Width.Set(0, 1f);
            enabledWrap.ItemMargin = 2;
            enabledScroll.SetChild(enabledWrap);

            // ===== 搜索框 (ID 或名字) =====
            var searchItem = new UIItemTextBox("", -1, null, "搜索 ID/名字");
            searchItem.TextBoxWidth = new StyleDimension(160, 0);
            stack.Append(searchItem);

            // ===== 全部前缀表格 =====
            var gridScroll = new UIScrollViewer();
            gridScroll.Width.Set(0, 1f);
            gridScroll.Height.Set(140, 0);
            stack.Append(gridScroll);

            var gridWrap = new UIWrapPanel();
            gridWrap.Width.Set(0, 1f);
            gridWrap.ItemMargin = 2;
            gridScroll.SetChild(gridWrap);

            // ===== 反馈行 =====
            var preview = new UIText("", 0.8f);
            stack.Append(preview);

            // ===== 按钮行 =====
            var btnStack = new UIStackPanel();
            btnStack.Horizontal = true;
            btnStack.ItemMargin = 6;
            btnStack.Width.Set(0, 1f);
            btnStack.Height.Set(30, 0);
            stack.Append(btnStack);

            var addButton = new UIButton1("添加");
            addButton.Width.Set(100, 0);
            addButton.Height.Set(28, 0);
            btnStack.Append(addButton);

            var resetButton = new UIButton1("恢复默认");
            resetButton.Width.Set(100, 0);
            resetButton.Height.Set(28, 0);
            btnStack.Append(resetButton);

            var hint = new UIText("修改后需要重新加载模组生效", 0.8f);
            hint.TextColor = Color.Gray;
            stack.Append(hint);

            // ===== 逻辑部分 =====
            List<int> allIds = GetAllPrefixIds().ToList();
            Dictionary<int, UIButton1> cells = new Dictionary<int, UIButton1>();
            HashSet<int> matches = new HashSet<int>();

            Action refreshCellColors = () =>
            {
                foreach (KeyValuePair<int, UIButton1> kv in cells)
                    ApplyCellColor(kv.Value, _presetIds.Contains(kv.Key), matches.Contains(kv.Key));
            };

            Action locateFirstMatch = () =>
            {
                if (matches.Count == 0) return;
                if (!cells.TryGetValue(matches.Min(), out UIButton1 target)) return;

                // UIScrollViewer 的子元素: [0]=UIList, [1]=UIScrollbar
                UIScrollbar scrollbar = gridScroll.Children.ElementAtOrDefault(1) as UIScrollbar;
                if (scrollbar != null)
                    scrollbar.ViewPosition = Math.Max(0, target.Top.Pixels - 8);
            };

            Action rebuildSummary = () =>
            {
                enabledWrap.RemoveAllChildren();
                foreach (int id in _presetIds)
                {
                    var chip = new UIText(GetPrefixName(id), 0.8f);
                    chip.TextColor = new Color(110, 210, 110);
                    chip.MarginRight = 8;
                    chip.MarginTop = 1;
                    chip.MarginBottom = 1;
                    chip.OnUpdate += _ =>
                    {
                        if (chip.IsMouseHovering)
                            Main.instance.MouseText("点击定位到表格");
                    };
                    int capturedId = id;
                    chip.OnLeftClick += (evt, element) => searchItem.SetText(capturedId.ToString());
                    enabledWrap.Append(chip);
                }
                enabledHeader.SetText($"已启用 ({_presetIds.Count})");
                enabledWrap.UpdateContainer_Height();
            };

            Action rebuildGrid = () =>
            {
                gridWrap.RemoveAllChildren();
                cells.Clear();
                foreach (int id in allIds)
                {
                    UIButton1 btn = CreatePrefixCell(id, matches, rebuildSummary);
                    cells[id] = btn;
                    gridWrap.Append(btn);
                }
                gridWrap.UpdateContainer_Height();
            };

            searchItem.OnTextChanged += s =>
            {
                string q = (s ?? "").Trim();
                matches.Clear();

                if (q.Length > 0)
                {
                    bool isNumeric = q.All(char.IsDigit);
                    foreach (int id in allIds)
                    {
                        bool hit = isNumeric && id.ToString().StartsWith(q);
                        if (!hit)
                            hit = GetPrefixName(id).ToLower().Contains(q.ToLower());
                        if (hit)
                            matches.Add(id);
                    }
                }

                refreshCellColors();

                if (q.Length == 0)
                    preview.SetText("");
                else if (matches.Count == 0)
                {
                    preview.SetText("没有匹配的前缀");
                    preview.TextColor = Color.OrangeRed;
                }
                else if (matches.Count == 1)
                {
                    int id = matches.First();
                    preview.SetText($"匹配: {GetPrefixName(id)} (ID {id}){( _presetIds.Contains(id) ? ", 已启用" : "")}");
                    preview.TextColor = new Color(255, 215, 0);
                }
                else
                {
                    preview.SetText($"匹配 {matches.Count} 个前缀, 已在表格中标金");
                    preview.TextColor = new Color(255, 215, 0);
                }

                locateFirstMatch();
            };

            addButton.OnLeftClick += (evt, element) =>
            {
                string q = searchItem.GetText().Trim();
                if (!int.TryParse(q, out int id) || id < 0)
                {
                    preview.SetText("添加需输入纯数字 ID, 或直接点击表格中的格子");
                    preview.TextColor = Color.OrangeRed;
                    return;
                }

                if (id == 0 || id >= Lang.prefix.Length || Lang.prefix[id] == null || string.IsNullOrWhiteSpace(Lang.prefix[id].Value))
                {
                    preview.SetText($"前缀 {id} 不存在");
                    preview.TextColor = Color.OrangeRed;
                    return;
                }

                if (_presetIds.Contains(id))
                {
                    preview.SetText($"前缀 {id} 已启用: {GetPrefixName(id)}");
                    preview.TextColor = Color.OrangeRed;
                }
                else
                {
                    _presetIds.Add(id);
                    _presetIds.Sort();
                    NeedSave = true;
                    refreshCellColors();
                    rebuildSummary();
                    preview.SetText($"已添加: {GetPrefixName(id)}");
                    preview.TextColor = new Color(255, 215, 0);
                }
            };

            resetButton.OnLeftClick += (evt, element) =>
            {
                preview.SetText("已恢复默认预设");
                preview.TextColor = Color.Gray;
                SetDefault();
            };

            stack.OnUpdate += _ =>
            {
                keepHeight(gridWrap);
                keepHeight(enabledWrap);
            };

            _updateUI = list =>
            {
                _presetIds = list;
                refreshCellColors();
                rebuildSummary();
            };

            rebuildGrid();
            rebuildSummary();
            return stack;
        }

        /// <summary>
        /// 枚举游戏中所有可用的前缀 ID (跳过空项)
        /// </summary>
        private IEnumerable<int> GetAllPrefixIds()
        {
            for (int i = 1; i < Lang.prefix.Length; i++)
            {
                if (Lang.prefix[i] != null && !string.IsNullOrWhiteSpace(Lang.prefix[i].Value))
                    yield return i;
            }
        }

        private UIButton1 CreatePrefixCell(int id, HashSet<int> matches, Action onChanged)
        {
            var btn = new UIButton1(GetPrefixName(id), 0.8f);
            btn.Width.Set(62, 0);
            btn.Height.Set(24, 0);
            btn.MarginLeft = 2;
            btn.MarginRight = 2;
            btn.MarginTop = 2;
            btn.MarginBottom = 2;
            ApplyCellColor(btn, _presetIds.Contains(id), matches.Contains(id));
            btn.OnUpdate += _ =>
            {
                if (btn.IsMouseHovering)
                {
                    bool added = _presetIds.Contains(id);
                    Main.instance.MouseText($"{GetPrefixName(id)} (ID: {id}) - {(added ? "已启用, 点击移除" : "未启用, 点击添加")}");
                }
            };
            btn.OnLeftClick += (evt, element) =>
            {
                bool added = _presetIds.Contains(id);
                if (added)
                    _presetIds.Remove(id);
                else
                    _presetIds.Add(id);
                _presetIds.Sort();
                NeedSave = true;

                // 表格顺序固定, 只需刷新颜色并同步摘要
                ApplyCellColor(btn, !added, matches.Contains(id));
                onChanged?.Invoke();
            };
            return btn;
        }

        private void ApplyCellColor(UIButton1 btn, bool added, bool highlighted)
        {
            if (highlighted)
            {
                btn.EnableColorBack = new Color(190, 150, 40) * 0.95f;
                btn.MouseOverColorBack = new Color(220, 180, 55);
            }
            else if (added)
            {
                btn.EnableColorBack = new Color(56, 132, 56) * 0.9f;
                btn.MouseOverColorBack = new Color(78, 168, 78);
            }
            else
            {
                btn.EnableColorBack = new Color(150, 62, 62) * 0.9f;
                btn.MouseOverColorBack = new Color(188, 80, 80);
            }
        }

        private string GetPrefixName(int prefixId)
        {
            if (prefixId <= 0) return "无";
            if (prefixId < Lang.prefix.Length && Lang.prefix[prefixId] != null)
                return Lang.prefix[prefixId].Value;
            return "未知";
        }
    }
}

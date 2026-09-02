using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using tContentPatch.Content.UI;
using tContentPatch.Content.UI.ModSet;
using Terraria;
using Terraria.Achievements;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace BetterAchievementUnlocker
{
    /// <summary>
    /// 成就解锁器窗口: 全部成就图标网格, 每个成就独立选择/解锁
    /// 左键: 选择/取消 (待解锁)  右键: 立即解锁单个
    /// </summary>
    internal class UIAchievementUnlocker : UIWindow
    {
        private UIItemTextBox _searchBox;
        private ToggleCell _hideDoneToggle;
        private UIButton1 _selectAllButton;
        private UIButton1 _deselectAllButton;
        private UIButton1 _unlockSelectedButton;
        private UIButton1 _unlockAllButton;
        private UIText _countText;
        private UIScrollViewer _scrollViewer;
        private UIWrapPanel _wrapPanel;

        private List<Achievement> _all = new List<Achievement>();
        private readonly Dictionary<string, UIAchievementCell> _cells = new Dictionary<string, UIAchievementCell>();
        private List<Achievement> _visible = new List<Achievement>();
        private string _filter = "";
        private bool _hideDone;
        private bool _built;
        private string _result = "";

        public UIAchievementUnlocker(string title, int width, int height) : base(title, width, height)
        {
            HAlign = 0.5f;
            VAlign = 0.5f;
            MinWidth.Pixels = 480;
            MinHeight.Pixels = 420;

            // 搜索框
            _searchBox = new UIItemTextBox("", -1, null, "搜索 名称/ID");
            _searchBox.Width.Set(0, 1f);
            _searchBox.Top.Set(0, 0);
            _searchBox.TextBoxWidth = new StyleDimension(-90, 1f);
            _searchBox.OnTextChanged += s =>
            {
                _filter = (s ?? "").Trim();
                ApplyFilter();
            };
            Child.Append(_searchBox);

            // 过滤开关行
            var toggleRow = new UIStackPanel();
            toggleRow.Horizontal = true;
            toggleRow.Width.Set(0, 1f);
            toggleRow.Height.Set(30, 0);
            toggleRow.Top.Set(44, 0);
            toggleRow.ItemMargin = 4;
            Child.Append(toggleRow);

            _hideDoneToggle = new ToggleCell("隐藏已完成", "仅显示未完成的成就");
            _hideDoneToggle.Width.Set(150, 0);
            _hideDoneToggle.OnValUpdate += v =>
            {
                _hideDone = v;
                ApplyFilter();
            };
            toggleRow.Append(_hideDoneToggle);

            // 操作按钮行: 全选 / 全不选 / 解锁选中 / 全部解锁
            _selectAllButton = MakeButton("全选", 0f, SelectVisible);
            _deselectAllButton = MakeButton("全不选", 1 / 3f, () =>
            {
                AchievementSetting.ClearAll();
                RefreshCellColors();
                UpdateCounts();
            });
            _unlockSelectedButton = MakeButton("解锁选中", 2 / 3f, UnlockSelected);
            _unlockAllButton = MakeButton("全部解锁", 1f, UnlockAllNotCompleted);
            Child.Append(_selectAllButton);
            Child.Append(_deselectAllButton);
            Child.Append(_unlockSelectedButton);
            Child.Append(_unlockAllButton);

            // 状态行 (替代聊天提示)
            _countText = new UIText("", 0.8f);
            _countText.Top.Set(110, 0);
            _countText.HAlign = 0.5f;
            _countText.TextColor = Color.Gray;
            Child.Append(_countText);

            // 成就图标网格
            _scrollViewer = new UIScrollViewer();
            _scrollViewer.Width.Set(0, 1f);
            _scrollViewer.Height.Set(-140, 1f);
            _scrollViewer.Top.Set(132, 0);
            Child.Append(_scrollViewer);

            _wrapPanel = new UIWrapPanel();
            _wrapPanel.Width.Set(0, 1f);
            _wrapPanel.ItemMargin = 2;
            _scrollViewer.SetChild(_wrapPanel);

            // 每帧刷新换行容器高度, 保证 UIList 滚动范围正确
            OnUpdate += _ => _wrapPanel.UpdateContainer_Height();
        }

        private static UIButton1 MakeButton(string text, float halign, Action onClick)
        {
            UIButton1 btn = new UIButton1(text, 0.8f);
            btn.Width.Set(-12, 1 / 4f);
            btn.Height.Set(28, 0);
            btn.Top.Set(78, 0);
            btn.HAlign = halign;
            btn.VAlign = 0f;
            btn.OnLeftClick += (evt, element) => onClick();
            return btn;
        }

        public override void OnActivate()
        {
            base.OnActivate();
            BuildGrid();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (!_built) BuildGrid();
        }

        private void BuildGrid()
        {
            _all = AchievementUnlocker.GetAll();

            _wrapPanel.RemoveAllChildren();
            _cells.Clear();
            foreach (Achievement ach in _all)
            {
                UIAchievementCell cell = new UIAchievementCell(ach, RefreshCellColorsAndCounts,
                    s => { _result = s; UpdateCounts(); });
                _cells[ach.Name] = cell;
            }
            _built = _all.Count > 0;
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            string q = _filter.ToLower();

            _visible = _all.Where(a =>
            {
                if (_hideDone && a.IsCompleted) return false;
                if (q.Length == 0) return true;
                if (q.All(char.IsDigit) && a.Id.ToString().StartsWith(q)) return true;
                string name = a.FriendlyName?.Value ?? a.Name;
                return name.ToLower().Contains(q);
            }).ToList();

            _wrapPanel.RemoveAllChildren();
            foreach (Achievement a in _visible)
                _wrapPanel.Append(_cells[a.Name]);
            _wrapPanel.UpdateContainer_Height();

            UpdateCounts();
        }

        private void RefreshCellColorsAndCounts()
        {
            RefreshCellColors();
            UpdateCounts();
        }

        private void RefreshCellColors()
        {
            foreach (KeyValuePair<string, UIAchievementCell> kv in _cells)
                kv.Value.RefreshState();
        }

        private void UpdateCounts()
        {
            int done = _all.Count(AchievementUnlocker.IsUnlocked);
            int selected = _all.Count(a => !a.IsCompleted && AchievementSetting.IsSelected(a.Name));
            string text = $"已解锁 {done}/{_all.Count} · 选中 {selected}";
            if (_result.Length > 0)
                text += $" · {_result}";
            _countText.SetText(text);
        }

        private void SelectVisible()
        {
            foreach (Achievement a in _visible)
            {
                if (!a.IsCompleted)
                    AchievementSetting.SetSelected(a.Name, true);
            }
            AchievementSetting.SaveNow();
            RefreshCellColorsAndCounts();
        }

        private bool TryUnlockMany(IEnumerable<string> names)
        {
            if (Main.gameMenu)
            {
                _result = "请先进入世界";
                UpdateCounts();
                return false;
            }

            List<string> list = names.Where(n => !string.IsNullOrEmpty(n)).ToList();
            int count = AchievementUnlocker.UnlockMany(list);
            AchievementSetting.SaveNow();
            RefreshAchievementsMenu();
            _result = count > 0 ? $"刚解锁 {count} 个" : "";
            RefreshCellColorsAndCounts();
            return true;
        }

        private void UnlockSelected()
        {
            List<string> names = _all
                .Where(a => !a.IsCompleted && AchievementSetting.IsSelected(a.Name))
                .Select(a => a.Name)
                .ToList();
            TryUnlockMany(names);
        }

        private void UnlockAllNotCompleted()
        {
            List<string> names = _all
                .Where(a => !a.IsCompleted)
                .Select(a => a.Name)
                .ToList();
            TryUnlockMany(names);
        }

        private static void RefreshAchievementsMenu()
        {
            try
            {
                Type menuType = Type.GetType("Terraria.UI.UIAchievementsMenu, Terraria");
                object menu = menuType?.GetConstructor(Type.EmptyTypes)?.Invoke(null);
                if (menu != null)
                    typeof(Main).GetField("AchievementsMenu")?.SetValue(null, menu);

                if (Main.menuMode == 888)
                {
                    Main.menuMode = 0;
                    Main.menuMode = 888;
                }
            }
            catch { }
        }

        /// <summary>
        /// 单个成就格子: 图标 + 选中/完成/悬停状态
        /// 右键: 未完成 → 立即解锁; 已完成 → 取消解锁 (本地清除进度)
        /// </summary>
        private class UIAchievementCell : UIElement
        {
            private readonly Achievement _ach;
            private readonly Action _onChanged;
            private readonly Action<string> _setResult;
            private Texture2D _pixel;
            private static Microsoft.Xna.Framework.Graphics.Texture2D _iconSheet;

            public UIAchievementCell(Achievement ach, Action onChanged, Action<string> setResult)
            {
                _ach = ach;
                _onChanged = onChanged;
                _setResult = setResult;
                Width.Set(50, 0);
                Height.Set(50, 0);
                MarginLeft = 2;
                MarginRight = 2;
                MarginTop = 2;
                MarginBottom = 2;

                OnLeftClick += (evt, element) => ToggleSelect();
                OnRightClick += (evt, element) => RightClickAction();
            }

            private bool Done => AchievementUnlocker.IsUnlocked(_ach);
            private bool Selected => AchievementSetting.IsSelected(_ach.Name);

            private void ToggleSelect()
            {
                if (Done) return; // 已完成不可选
                AchievementSetting.SetSelected(_ach.Name, !Selected);
                AchievementSetting.SaveNow();
                _onChanged?.Invoke();
            }

            private void RightClickAction()
            {
                if (Done)
                {
                    // 取消解锁: 本地清除进度
                    string name = _ach.FriendlyName?.Value ?? _ach.Name;
                    AchievementUnlocker.ClearProgress(_ach);
                    _setResult?.Invoke($"已取消: {name}");
                    _onChanged?.Invoke();
                    return;
                }

                if (Main.gameMenu) return;
                AchievementUnlocker.Unlock(_ach);
                AchievementSetting.SaveNow();
                _onChanged?.Invoke();
            }

            public void RefreshState()
            {
                // 颜色在 DrawSelf 里按状态实时取, 这里仅为触发重绘
            }

            private Texture2D Pixel
            {
                get
                {
                    if (_pixel == null)
                    {
                        _pixel = new Texture2D(Main.graphics.GraphicsDevice, 1, 1);
                        _pixel.SetData(new[] { Color.White });
                    }
                    return _pixel;
                }
            }

            private static Texture2D IconSheet
            {
                get
                {
                    if (_iconSheet == null)
                        _iconSheet = Main.Assets.Request<Texture2D>("Images/UI/Achievements", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
                    return _iconSheet;
                }
            }

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                base.DrawSelf(spriteBatch);

                CalculatedStyle dim = GetDimensions();
                if (dim.Width <= 0) return;

                bool done = Done;
                bool selected = Selected;

                // 图标: 未完成用锁定帧, 已完成用正常帧 (与原版成就界面一致)
                int idx = AchievementUnlocker.GetIconIndex(_ach);
                Rectangle frame = new Rectangle(idx % 8 * 66, idx / 8 * 66, 64, 64);
                if (!done) frame.X += 528;

                Rectangle dest = new Rectangle((int)dim.X + 1, (int)dim.Y + 1, (int)dim.Width - 2, (int)dim.Height - 2);
                spriteBatch.Draw(IconSheet, dest, frame, Color.White);

                // 选中底色
                if (selected)
                    spriteBatch.Draw(Pixel, dest, new Color(60, 160, 60) * 0.35f);

                // 边框: 选中绿 / 完成 金 / 悬停 白
                if (IsMouseHovering || selected || done)
                {
                    Color bc = selected ? Color.LimeGreen : done ? new Color(220, 180, 60) : Color.White;
                    int x = (int)dim.X, y = (int)dim.Y, w = (int)dim.Width, h = (int)dim.Height, t = 1;
                    spriteBatch.Draw(Pixel, new Rectangle(x, y, w, t), bc);
                    spriteBatch.Draw(Pixel, new Rectangle(x, y + h - t, w, t), bc);
                    spriteBatch.Draw(Pixel, new Rectangle(x, y, t, h), bc);
                    spriteBatch.Draw(Pixel, new Rectangle(x + w - t, y, t, h), bc);
                }

                if (IsMouseHovering)
                {
                    Main.LocalPlayer.mouseInterface = true;
                    string name = _ach.FriendlyName?.Value ?? _ach.Name;
                    string desc = _ach.Description?.Value ?? "";
                    string text;
                    if (done)
                        text = $"{name} (ID:{_ach.Id}) [已完成] {desc} | 右键: 取消解锁(仅本地)";
                    else
                        text = $"{name} (ID:{_ach.Id}) [{(selected ? "已选中待解锁" : "未解锁")}] {desc} | 左键:选择 右键:立即解锁";
                    Main.instance.MouseText(text);
                }
            }
        }
    }

    /// <summary>
    /// 紧凑开关: 标签左对齐, "关"/"开" 右对齐紧贴右侧, 点击切换, 悬停显示说明
    /// </summary>
    internal class ToggleCell : UIElement
    {
        public Action<bool> OnValUpdate;

        private readonly string _label;
        private readonly string _hint;
        private bool _on;
        private readonly UIText _labelText;
        private readonly UIText _stateText;
        private Texture2D _pixel;

        public ToggleCell(string label, string hint)
        {
            _label = label ?? "";
            _hint = hint ?? "";
            Height.Set(30, 0);

            // 标签: 左对齐
            _labelText = new UIText(_label, 0.8f);
            _labelText.Left.Set(2, 0);
            _labelText.Top.Set(0, 0);
            _labelText.Width.Set(0, 0);
            _labelText.HAlign = 0f;
            _labelText.VAlign = 0.5f;
            Append(_labelText);

            // 状态: 右对齐 (内容宽度, 靠右, 紧贴单元格右缘)
            _stateText = new UIText("关", 0.8f);
            _stateText.Left.Set(0, 0);
            _stateText.Top.Set(0, 0);
            _stateText.Width.Set(0, 0);
            _stateText.HAlign = 1f;
            _stateText.VAlign = 0.5f;
            _stateText.TextColor = new Color(180, 120, 120);
            Append(_stateText);

            OnLeftClick += (evt, element) =>
            {
                _on = !_on;
                RefreshState();
                OnValUpdate?.Invoke(_on);
            };
        }

        private void RefreshState()
        {
            _stateText.SetText(_on ? "开" : "关");
            _stateText.TextColor = _on ? new Color(120, 200, 120) : new Color(180, 120, 120);
        }

        private Texture2D Pixel
        {
            get
            {
                if (_pixel == null)
                {
                    _pixel = new Texture2D(Main.graphics.GraphicsDevice, 1, 1);
                    _pixel.SetData(new[] { Color.White });
                }
                return _pixel;
            }
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);

            CalculatedStyle dim = GetDimensions();
            if (dim.Width <= 0) return;

            if (IsMouseHovering)
            {
                Main.LocalPlayer.mouseInterface = true;
                int x = (int)dim.X, y = (int)dim.Y, w = (int)dim.Width, h = (int)dim.Height;
                spriteBatch.Draw(Pixel, new Rectangle(x, y, w, 1), Color.White);
                spriteBatch.Draw(Pixel, new Rectangle(x, y + h - 1, w, 1), Color.White);
                spriteBatch.Draw(Pixel, new Rectangle(x, y, 1, h), Color.White);
                spriteBatch.Draw(Pixel, new Rectangle(x + w - 1, y, 1, h), Color.White);

                if (_hint.Length > 0)
                    Main.instance.MouseText(_hint);
            }
        }
    }
}

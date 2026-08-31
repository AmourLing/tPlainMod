using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using tContentPatch.Content.UI;
using tContentPatch.Content.UI.ModSet;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;

namespace BetterBuffGet
{
    /// <summary>
    /// 增益列表面板: 图标网格 + 搜索 + 左键开关 + 右键收藏置顶 (参考 ImproveGame 无限药水界面)
    /// </summary>
    internal class UIBuffList : UIElement
    {
        private const float CellSize = 44f;

        private UIText _titleText;
        private UIItemTextBox _searchBox;
        private UIButton1 _selectAllButton;
        private UIButton1 _deselectAllButton;
        private UIButton1 _saveButton;
        private UIScrollViewer _scrollViewer;
        private UIWrapPanel _wrapPanel;
        private UIScrollViewer _presetScroll;
        private UIStackPanel _presetList;
        private UITextBox _presetNameBox;

        private readonly List<int> _allBuffIds = new List<int>();
        private readonly Dictionary<int, UIBuffCell> _cells = new Dictionary<int, UIBuffCell>();
        private List<int> _visibleIds = new List<int>();
        private string _filter = "";
        private bool _gridBuilt = false;

        // 两个过滤器: 仅背包已有 / 仅药水食物可获得
        private bool _filterInventory;
        private bool _filterConsumable;
        private readonly HashSet<int> _inventoryBuffs = new HashSet<int>();
        private int _invRefreshTimer;
        private bool _wasReady;

        public UIBuffList()
        {
            Width.Set(0, 1f);
            Height.Set(620, 0);

            _titleText = new UIText("增益列表", 0.9f);
            _titleText.Top.Set(2, 0);
            _titleText.HAlign = 0.5f;
            Append(_titleText);

            // 搜索框: ID 或名称
            _searchBox = new UIItemTextBox("", -1, null, "搜索 ID/名字");
            _searchBox.Width.Set(0, 1f);
            _searchBox.Top.Set(28, 0);
            _searchBox.TextBoxWidth = new StyleDimension(-90, 1f);
            _searchBox.OnTextChanged += s => ApplyFilter(s);
            Append(_searchBox);

            // 过滤器开关行: 仅背包已有 / 仅药水食物 / 自动获得
            // 使用自绘 ToggleCell: 标签左对齐, "关"/"开" 右对齐紧贴切换点, 悬停提示
            var toggleRow = new UIStackPanel();
            toggleRow.Horizontal = true;
            toggleRow.Width.Set(0, 1f);
            toggleRow.Height.Set(30, 0);
            toggleRow.Top.Set(72, 0);
            toggleRow.ItemMargin = 4;
            Append(toggleRow);

            var invToggle = new ToggleCell("仅背包", "仅显示背包中已有物品的增益");
            invToggle.Width.Set(-6, 1 / 3f);
            invToggle.OnValUpdate += v =>
            {
                _filterInventory = v;
                if (v) { _inventoryBuffs.Clear(); RefreshInventoryBuffs(); }
                ApplyCurrentFilter();
            };
            toggleRow.Append(invToggle);

            var consumableToggle = new ToggleCell("仅可获", "仅显示药水/食物/增益站等可获得的增益");
            consumableToggle.Width.Set(-6, 1 / 3f);
            consumableToggle.OnValUpdate += v =>
            {
                _filterConsumable = v;
                ApplyCurrentFilter();
            };
            toggleRow.Append(consumableToggle);

            var autoToggle = new ToggleCell("自动", "每 10s 检查一次, 增益不足 12s 时补一次");
            autoToggle.Width.Set(-6, 1 / 3f);
            autoToggle.OnValUpdate += v => BetterBuffGetMod.AutoApply = v;
            toggleRow.Append(autoToggle);

            // 预设方案区
            var presetLabel = new UIText("预设方案 (左键应用 / 右键删除)", 0.8f);
            presetLabel.Top.Set(106, 0);
            presetLabel.TextColor = Color.Gray;
            Append(presetLabel);
            presetLabel.TextColor = Color.Gray;
            Append(presetLabel);

            // 保存/改名预设: 输入方案名后点击, 创建或覆盖同名方案
            var addRow = new UIStackPanel();
            addRow.Horizontal = true;
            addRow.Width.Set(0, 1f);
            addRow.Height.Set(34, 0);
            addRow.Top.Set(134, 0);
            addRow.ItemMargin = 6;
            Append(addRow);

            _presetNameBox = new UITextBox("方案名称");
            _presetNameBox.Width.Set(-96, 1f);
            _presetNameBox.Height.Set(30, 0);
            addRow.Append(_presetNameBox);

            var savePresetBtn = new UIButton1("存为预设");
            savePresetBtn.Width.Set(88, 0);
            savePresetBtn.Height.Set(30, 0);
            savePresetBtn.OnLeftClick += (evt, element) =>
            {
                string name = _presetNameBox.Text;
                if (string.IsNullOrEmpty(name))
                    return;
                IEnumerable<int> ids = BuffSetting.CurrentSelectedBuffs
                    .Where(kv => kv.Value).Select(kv => kv.Key);
                PresetSetting.Add(name, ids);
                _presetNameBox.SetText("");
                BuildPresets();
            };
            addRow.Append(savePresetBtn);

            _presetScroll = new UIScrollViewer();
            _presetScroll.Width.Set(0, 1f);
            _presetScroll.Height.Set(60, 0);
            _presetScroll.Top.Set(172, 0);
            Append(_presetScroll);

            _presetList = new UIStackPanel();
            _presetList.Width.Set(0, 1f);
            _presetList.ItemMargin = 2;
            _presetList.IsAutoUpdateSize = true;
            _presetScroll.SetChild(_presetList);
            _presetList.OnUpdate += _ => _presetList.UpdateContainer_Height();

            // 操作按钮行
            _selectAllButton = new UIButton1("全选", 0.8f);
            _selectAllButton.Width.Set(-10, 1 / 3f);
            _selectAllButton.Height.Set(26, 0);
            _selectAllButton.Top.Set(236, 0);
            _selectAllButton.VAlign = 0f;
            _selectAllButton.HAlign = 0f;
            _selectAllButton.OnLeftClick += (evt, element) => SelectVisible(true);
            Append(_selectAllButton);

            _deselectAllButton = new UIButton1("全不选", 0.8f);
            _deselectAllButton.Width.Set(-10, 1 / 3f);
            _deselectAllButton.Height.Set(26, 0);
            _deselectAllButton.Top.Set(236, 0);
            _deselectAllButton.HAlign = 0.5f;
            _deselectAllButton.OnLeftClick += (evt, element) => SelectVisible(false);
            Append(_deselectAllButton);

            _saveButton = new UIButton1("保存", 0.8f);
            _saveButton.Width.Set(-10, 1 / 3f);
            _saveButton.Height.Set(26, 0);
            _saveButton.Top.Set(236, 0);
            _saveButton.HAlign = 1f;
            _saveButton.OnLeftClick += (evt, element) =>
            {
                BuffSetting.SaveNow();
                _titleText.SetText("已保存!");
            };
            Append(_saveButton);

            // 增益图标网格
            _scrollViewer = new UIScrollViewer();
            _scrollViewer.Width.Set(0, 1f);
            _scrollViewer.Height.Set(-288, 1f);
            _scrollViewer.Top.Set(266, 0);
            Append(_scrollViewer);

            _wrapPanel = new UIWrapPanel();
            _wrapPanel.Width.Set(0, 1f);
            _wrapPanel.ItemMargin = 2;
            _scrollViewer.SetChild(_wrapPanel);

            // 每帧刷新换行容器高度, 否则 UIList 拿不到正确内容高度, 滚动条失效
            OnUpdate += _ => _wrapPanel.UpdateContainer_Height();

            var hint = new UIText("左键: 启用/禁用  右键: 收藏置顶", 0.8f);
            hint.VAlign = 1f;
            hint.HAlign = 0.5f;
            hint.TextColor = Color.Gray;
            Append(hint);
        }

        public override void OnActivate()
        {
            base.OnActivate();
            BuildGrid();
            BuildPresets();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (!_gridBuilt) BuildGrid();

            // 数据库扫描完成时刷新一次 (药水食物过滤集合此时才可用)
            if (BuffDatabase.Ready && !_wasReady)
            {
                _wasReady = true;
                if (_filterConsumable) ApplyCurrentFilter();
            }

            // 背包过滤开启时, 定期重扫背包, 变化后刷新列表
            if (_filterInventory && ++_invRefreshTimer >= 30)
            {
                _invRefreshTimer = 0;
                int before = _inventoryBuffs.Count;
                _inventoryBuffs.Clear();
                RefreshInventoryBuffs();
                if (_inventoryBuffs.Count != before) ApplyCurrentFilter();
            }
        }

        /// <summary>
        /// 扫描当前背包, 收集其中物品可提供的增益 ID
        /// </summary>
        private void RefreshInventoryBuffs()
        {
            Player player = Main.LocalPlayer;
            if (player == null) return;
            foreach (Item it in player.inventory)
            {
                if (it == null || it.IsAir) continue;
                if (it.buffType > 0) _inventoryBuffs.Add(it.buffType);
            }
        }

        private void BuildGrid()
        {
            _allBuffIds.Clear();
            for (int i = 1; i < BuffID.Count; i++)
            {
                string name = Lang.GetBuffName(i);
                if (!string.IsNullOrEmpty(name)) _allBuffIds.Add(i);
            }

            _wrapPanel.RemoveAllChildren();
            _cells.Clear();
            foreach (int id in _allBuffIds)
            {
                var cell = new UIBuffCell(id, CellSize);
                cell.OnFavoriteChanged = ApplyCurrentFilter;
                _cells[id] = cell;
            }

            _gridBuilt = _allBuffIds.Count > 0;
            ApplyCurrentFilter();
        }

        /// <summary>
        /// 重建预设快速应用列表 (左键应用, 右键删除)
        /// </summary>
        private void BuildPresets()
        {
            _presetList.RemoveAllChildren();

            var presets = PresetSetting.CurrentPresets;
            if (presets == null || presets.Count == 0)
            {
                var empty = new UIText("暂无预设, 到模组设置中添加", 0.8f);
                empty.TextColor = Color.Gray;
                _presetList.Append(empty);
            }
            else
            {
                foreach (PresetEntry p in presets)
                {
                    var btn = new UIButton1($"{p.name} ({p.ids.Count})", 0.85f);
                    btn.Width.Set(0, 1f);
                    btn.Height.Set(24, 0);
                    btn.EnableColorBack = new Color(58, 90, 130) * 0.85f;
                    btn.MouseOverColorBack = new Color(74, 112, 160);
                    btn.OnUpdate += _ =>
                    {
                        if (btn.IsMouseHovering)
                            Main.instance.MouseText($"{p.name}  左键: 应用  右键: 删除");
                    };
                    string name = p.name;
                    btn.OnLeftClick += (evt, element) =>
                    {
                        PresetSetting.Apply(name);
                        ApplyCurrentFilter();
                        UpdateTitle();
                    };
                    btn.OnRightClick += (evt, element) =>
                    {
                        PresetSetting.Delete(name);
                        BuildPresets();
                    };
                    _presetList.Append(btn);
                }
            }
            _presetList.UpdateContainer_Height();
        }

        private void ApplyFilter(string s)
        {
            _filter = (s ?? "").Trim();
            ApplyCurrentFilter();
        }

        private void ApplyCurrentFilter()
        {
            string q = _filter.ToLower();

            _visibleIds = _allBuffIds.Where(id =>
            {
                // 搜索
                if (q.Length > 0)
                {
                    if (q.All(char.IsDigit) && id.ToString().StartsWith(q)) { /* ok */ }
                    else if (Lang.GetBuffName(id).ToLower().Contains(q)) { /* ok */ }
                    else return false;
                }

                // 仅背包已有
                if (_filterInventory && !_inventoryBuffs.Contains(id)) return false;

                // 仅药水/食物/增益站等可获得
                if (_filterConsumable && !BuffDatabase.IsObtainable(id)) return false;

                return true;
            })
            // 收藏置顶, 其余按 ID 排序 (参考 ImproveGame)
            .OrderBy(id => !BuffSetting.IsFavorite(id))
            .ToList();

            _wrapPanel.RemoveAllChildren();
            foreach (int id in _visibleIds)
                _wrapPanel.Append(_cells[id]);
            _wrapPanel.UpdateContainer_Height();

            UpdateTitle();
        }

        private void UpdateTitle()
        {
            int selected = _visibleIds.Count(BuffSetting.IsSelected);
            string suffix = _filter.Length > 0 ? $" 筛选 {_visibleIds.Count}/{_allBuffIds.Count}" : "";
            _titleText.SetText($"增益列表  已选 {selected}{suffix}");
        }

        /// <summary>
        /// 全选/全不选只作用于当前筛选结果, 配合搜索可以批量选择
        /// </summary>
        private void SelectVisible(bool value)
        {
            foreach (int id in _visibleIds)
                BuffSetting.SetSelected(id, value);
            UpdateTitle();
        }

        /// <summary>
        /// 单个增益图标格 (参考 ImproveGame SUIBuffButton: 左键开关, 右键收藏, 悬停提示)
        /// </summary>
        private class UIBuffCell : UIElement
        {
            public Action OnFavoriteChanged;

            private readonly int _buffId;
            private static Texture2D _pixel;

            public UIBuffCell(int buffId, float size)
            {
                _buffId = buffId;
                Width.Set(size, 0);
                Height.Set(size, 0);
                MarginLeft = 2;
                MarginRight = 2;
                MarginTop = 2;
                MarginBottom = 2;

                OnLeftClick += (evt, element) => ToggleSelected();
                OnRightClick += (evt, element) => ToggleFavorite();
            }

            private void ToggleSelected()
            {
                BuffSetting.SetSelected(_buffId, !BuffSetting.IsSelected(_buffId));
            }

            private void ToggleFavorite()
            {
                BuffSetting.SetFavorite(_buffId, !BuffSetting.IsFavorite(_buffId));
                OnFavoriteChanged?.Invoke(); // 收藏变化后重新排序置顶
            }

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                base.DrawSelf(spriteBatch);

                CalculatedStyle dim = GetDimensions();
                bool selected = BuffSetting.IsSelected(_buffId);
                bool favorite = BuffSetting.IsFavorite(_buffId);

                // 底色: 已启用为暗绿, 未启用为暗灰蓝
                Color back = selected
                    ? new Color(40, 92, 46) * 0.9f
                    : new Color(24, 30, 44) * 0.65f;
                spriteBatch.Draw(Pixel, dim.ToRectangle(), back);

                // 悬停高亮边框
                if (IsMouseHovering)
                {
                    var border = new Rectangle((int)dim.X, (int)dim.Y, (int)dim.Width, (int)dim.Height);
                    int t = 1;
                    spriteBatch.Draw(Pixel, new Rectangle(border.X, border.Y, border.Width, t), Color.White);
                    spriteBatch.Draw(Pixel, new Rectangle(border.X, border.Bottom - t, border.Width, t), Color.White);
                    spriteBatch.Draw(Pixel, new Rectangle(border.X, border.Y, t, border.Height), Color.White);
                    spriteBatch.Draw(Pixel, new Rectangle(border.Right - t, border.Y, t, border.Height), Color.White);
                }

                // 图标: 未启用时压暗 (参考 ImproveGame 黑名单的变暗处理)
                Texture2D tex = TextureAssets.Buff[_buffId].Value;
                float scale = Math.Min((dim.Width - 8) / tex.Width, (dim.Height - 8) / tex.Height);
                Vector2 pos = new Vector2(
                    dim.X + (dim.Width - tex.Width * scale) / 2,
                    dim.Y + (dim.Height - tex.Height * scale) / 2);
                Color iconColor = selected ? Color.White : Color.Lerp(Color.Black, Color.White, 0.35f);
                spriteBatch.Draw(tex, pos, null, iconColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

                // 收藏星标: 用游戏光标中的星标图案, 不依赖可能不存在的资源路径
                if (favorite)
                {
                    Texture2D star = TextureAssets.Cursors[3].Value;
                    if (star != null)
                        spriteBatch.Draw(star, new Vector2(dim.X + dim.Width - star.Width - 2, dim.Y + 2), Color.Gold);
                    else
                        spriteBatch.Draw(Pixel, new Rectangle((int)dim.X + (int)dim.Width - 10, (int)dim.Y + 2, 8, 8), Color.Gold);
                }

                if (IsMouseHovering)
                {
                    Main.LocalPlayer.mouseInterface = true;
                    string name = Lang.GetBuffName(_buffId);
                    string state = selected ? "已启用" : "未启用";
                    string fav = favorite ? " ★已收藏" : "";

                    string dur;
                    if (BuffDatabase.Ready)
                    {
                        int frames = BuffDatabase.GetDuration(_buffId);
                        dur = $"{frames / 3600}分{frames / 60 % 60}秒";
                    }
                    else
                        dur = "计算中";

                    string text = $"{name} (ID:{_buffId}){fav} [{state}] 持续:{dur} | 左键:启用/禁用 右键:收藏";
                    if (Main.debuff[_buffId])
                        Main.instance.MouseText(text, 220, 90, 90);
                    else
                        Main.instance.MouseText(text);
                }
            }

            private static Texture2D Pixel
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

            // 标签: 左对齐 (内容宽度, 靠左)
            _labelText = new UIText(_label, 0.8f);
            _labelText.Left.Set(0, 0);
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

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;
using tContentPatch.Content.UI;

namespace BetterBuffGet
{
    internal class UIBuffList : UIElement
    {
        private UIPanel _panel;
        private UIScrollViewer _scrollViewer;
        private UIStackPanel _stackPanel;
        private UIButton1 _saveButton;
        private UIButton1 _selectAllButton;
        private UIButton1 _deselectAllButton;

        private Dictionary<int, bool> _selectedDict = new Dictionary<int, bool>();
        private List<int> _availableBuffIds = new List<int>();

        public UIBuffList()
        {
            Width.Set(0, 1f);
            Height.Set(500, 0);

            _panel = new UIPanel();
            _panel.Width.Set(0, 1f);
            _panel.Height.Set(0, 1f);
            _panel.SetPadding(5);
            Append(_panel);

            var title = new UIText("启用的增益");
            title.HAlign = 0.5f;
            title.Top.Set(5, 0);
            _panel.Append(title);

            // 按钮面板
            var buttonPanel = new UIStackPanel();
            buttonPanel.Horizontal = true;
            buttonPanel.Width.Set(0, 1f);
            buttonPanel.Height.Set(150, 0);
            buttonPanel.Top.Set(30, 0);
            buttonPanel.ItemMargin = 5;
            _panel.Append(buttonPanel);

            _selectAllButton = new UIButton1("全选");
            _selectAllButton.Width.Set(60, 0);
            _selectAllButton.Height.Set(24, 0);
            _selectAllButton.OnLeftClick += (evt, element) => SelectAll(true);
            buttonPanel.Append(_selectAllButton);

            _deselectAllButton = new UIButton1("全不选");
            _deselectAllButton.Width.Set(60, 0);
            _deselectAllButton.Height.Set(24, 0);
            _deselectAllButton.OnLeftClick += (evt, element) => SelectAll(false);
            buttonPanel.Append(_deselectAllButton);

            // 创建滚动视图
            _scrollViewer = new UIScrollViewer();
            _scrollViewer.Width.Set(-20, 1f);
            _scrollViewer.Height.Set(-120, 1f);
            _scrollViewer.Top.Set(75, 0);
            _scrollViewer.Left.Set(0, 0);
            _panel.Append(_scrollViewer);

            // 创建垂直堆叠面板
            _stackPanel = new UIStackPanel();
            _stackPanel.Width.Set(0, 1f);
            _stackPanel.Height.Set(0, 1f);
            _stackPanel.Horizontal = false;
            _stackPanel.ItemMargin = 2;
            _stackPanel.IsAutoUpdateSize = true;
            _scrollViewer.SetChild(_stackPanel);

            _saveButton = new UIButton1("保存");
            _saveButton.Top.Set(-40, 1f);
            _saveButton.HAlign = 0.5f;
            _saveButton.Width.Set(60, 0);
            _saveButton.Height.Set(24, 0);
            _saveButton.OnLeftClick += (evt, element) => SaveSettings();
            _panel.Append(_saveButton);
        }

        public override void OnActivate()
        {
            base.OnActivate();
            LoadFromConfig();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (_availableBuffIds.Count == 0)
                LoadFromConfig();
        }

        private void LoadFromConfig()
        {
            _availableBuffIds = AvailableBuffsSetting.CurrentAvailableBuffs?.ToList() ?? new List<int>();
            _availableBuffIds.Sort();

            var currentSelected = BuffSetting.CurrentSelectedBuffs;
            _selectedDict.Clear();
            foreach (int id in _availableBuffIds)
            {
                bool selected = currentSelected.ContainsKey(id) ? currentSelected[id] : false;
                _selectedDict[id] = selected;
            }

            RefreshList();
        }

        private void RefreshList()
        {
            _stackPanel.RemoveAllChildren();

            if (_availableBuffIds.Count == 0)
            {
                var hint = new UIText("未配置任何可用增益，请先到模组设置中添加");
                hint.TextColor = Color.Gray;
                hint.Width.Set(0, 1f);
                hint.Height.Set(24, 0);
                _stackPanel.Append(hint);
                return;
            }

            foreach (int id in _availableBuffIds)
            {
                bool isChecked = _selectedDict[id];
                string name = Lang.GetBuffName(id);
                var item = new UIBuffItem(id, name, isChecked, OnCheckChanged);
                _stackPanel.Append(item);
            }

            _scrollViewer.Recalculate();
        }

        private void OnCheckChanged(int buffId, bool isChecked)
        {
            _selectedDict[buffId] = isChecked;
        }

        private void SelectAll(bool select)
        {
            foreach (int id in _availableBuffIds)
            {
                _selectedDict[id] = select;
            }
            RefreshList();
        }

        private void SaveSettings()
        {
            var setting = new BuffSetting();
            setting.UpdateData(_selectedDict);
            setting.Save();
        }

        private class UIBuffItem : UIElement
        {
            private int _buffId;
            private string _buffName;
            private bool _isChecked;
            private Action<int, bool> _onCheckChanged;
            private UIImage _icon;
            private UIText _text;

            private static Texture2D _whitePixel;

            public UIBuffItem(int id, string name, bool checked_, Action<int, bool> onCheckChanged)
            {
                if (_whitePixel == null)
                {
                    _whitePixel = new Texture2D(Main.graphics.GraphicsDevice, 1, 1);
                    _whitePixel.SetData(new[] { Color.White });
                }

                _buffId = id;
                _buffName = name;
                _isChecked = checked_;
                _onCheckChanged = onCheckChanged;

                Width.Set(0, 1f);
                Height.Set(32, 0);

                _icon = new UIImage(TextureAssets.Buff[id].Value);
                _icon.Width.Set(30, 0);
                _icon.Height.Set(30, 0);
                _icon.Left.Set(2, 0);
                _icon.VAlign = 0.5f;
                _icon.ScaleToFit = true;
                Append(_icon);

                _text = new UIText(name);
                _text.Left.Set(40, 0);
                _text.VAlign = 0.5f;
                if (Main.debuff[id])
                    _text.TextColor = Color.Red;
                Append(_text);

                OnLeftClick += (evt, element) => Toggle();
            }

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                base.DrawSelf(spriteBatch);
                var dim = GetDimensions();

                int checkSize = 20;
                float checkX = dim.X + dim.Width - checkSize - 5;
                float checkY = dim.Y + (dim.Height - checkSize) / 2;

                spriteBatch.Draw(_whitePixel, new Rectangle((int)checkX, (int)checkY, checkSize, checkSize), Color.White);
                if (_isChecked)
                {
                    spriteBatch.Draw(_whitePixel, new Rectangle((int)checkX + 2, (int)checkY + 2, checkSize - 4, checkSize - 4), Color.LimeGreen);
                }
            }

            private void Toggle()
            {
                _isChecked = !_isChecked;
                _onCheckChanged?.Invoke(_buffId, _isChecked);
            }
        }
    }
}
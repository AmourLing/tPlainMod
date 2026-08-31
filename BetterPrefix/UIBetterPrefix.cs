using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using tContentPatch.Content.UI;
using Terraria;
using Terraria.GameContent.UI;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace BetterPrefix
{
    internal class UIBetterPrefix : UIWindow
    {
        private Item _targetItem = new Item();
        private UIItemSlot _itemSlot;
        private UIText _itemNameText;
        private UIText _prefixText;
        private UIText _statusText;
        private UIButton1 _resetButton;
        private UIButton1 _randomButton;
        private UIButton1 _perfectButton;
        private UIScrollViewer _presetScrollViewer;
        private UIWrapPanel _presetWrapPanel;
        private bool _isBest = false;

        private int _reforgeCooldown = 0;

        private List<int> _presetPrefixIds;

        private int _maxAttempts;

        public UIBetterPrefix(string title, int width, int height) : base(title, width, height)
        {
            HAlign = 0f;
            VAlign = 0.5f;
            MinWidth.Pixels = 420;
            MinHeight.Pixels = 340;

            // 读取设置值
            _presetPrefixIds = PresetPrefixesSetting.CurrentPresetIds ?? new List<int> { 65, 72, 81, 82, 83, 84, 85 };
            _maxAttempts = MaxAttemptsSetting.CurrentMaxAttempts > 0 ? MaxAttemptsSetting.CurrentMaxAttempts : 100;

            BuildUI();
            CreatePresetPanel();
        }

        private void BuildUI()
        {
            // 顶部面板：物品槽 + 物品信息
            var panelTop = new UIPanel();
            panelTop.Width.Set(0, 1f);
            panelTop.Height.Set(64, 0);
            panelTop.SetPadding(5);
            Child.Append(panelTop);

            _itemSlot = new UIItemSlot();
            _itemSlot.Width.Set(52, 0);
            _itemSlot.Height.Set(52, 0);
            _itemSlot.Left.Set(4, 0);
            _itemSlot.Top.Set(2, 0);
            _itemSlot.OnItemChanged += OnItemChanged;
            panelTop.Append(_itemSlot);

            _itemNameText = new UIText("未选择物品");
            _itemNameText.Left.Set(66, 0);
            _itemNameText.Top.Set(8, 0);
            panelTop.Append(_itemNameText);

            _prefixText = new UIText("当前前缀: 无", 0.8f);
            _prefixText.Left.Set(66, 0);
            _prefixText.Top.Set(32, 0);
            panelTop.Append(_prefixText);

            // 中部面板：预设前缀按钮区域（可滚动、自动换行）
            var panelMiddle = new UIPanel();
            panelMiddle.Width.Set(0, 1f);
            panelMiddle.Height.Set(-142, 1f); // 减去顶部、状态行和底部按钮的高度
            panelMiddle.Top.Set(68, 0);
            panelMiddle.SetPadding(5);
            panelMiddle.OverflowHidden = true;
            Child.Append(panelMiddle);

            var presetTitle = new UIText("预设前缀 (点击应用到物品)", 0.8f);
            presetTitle.Top.Set(2, 0);
            presetTitle.HAlign = 0.5f;
            panelMiddle.Append(presetTitle);

            _presetScrollViewer = new UIScrollViewer();
            _presetScrollViewer.Width.Set(0, 1f);
            _presetScrollViewer.Height.Set(-22, 1f);
            _presetScrollViewer.Top.Set(20, 0);
            panelMiddle.Append(_presetScrollViewer);

            _presetWrapPanel = new UIWrapPanel();
            _presetWrapPanel.Width.Set(0, 1f);
            _presetWrapPanel.ItemMargin = 4;
            _presetScrollViewer.SetChild(_presetWrapPanel);

            // 状态行
            _statusText = new UIText("把背包中的物品放入左上角槽位", 0.8f);
            _statusText.VAlign = 1f;
            _statusText.Top.Set(-48, 0);
            _statusText.HAlign = 0.5f;
            Child.Append(_statusText);

            // 底部面板：重置 / 随机重铸 / 完美重铸
            var panelBottom = new UIPanel();
            panelBottom.Width.Set(0, 1f);
            panelBottom.Height.Set(44, 0);
            panelBottom.VAlign = 1f;
            panelBottom.SetPadding(5);
            Child.Append(panelBottom);

            _resetButton = new UIButton1("重置前缀");
            _resetButton.Width.Set(-12, 1 / 3f);
            _resetButton.Height.Set(32, 0);
            _resetButton.HAlign = 0f;
            _resetButton.VAlign = 0.5f;
            _resetButton.OnLeftClick += (evt, element) => ResetPrefix();
            panelBottom.Append(_resetButton);

            _randomButton = new UIButton1("随机重铸");
            _randomButton.Width.Set(-12, 1 / 3f);
            _randomButton.Height.Set(32, 0);
            _randomButton.HAlign = 0.5f;
            _randomButton.VAlign = 0.5f;
            _randomButton.OnLeftClick += (evt, element) => RandomReforge();
            panelBottom.Append(_randomButton);

            _perfectButton = new UIButton1("完美重铸");
            _perfectButton.Width.Set(-12, 1 / 3f);
            _perfectButton.Height.Set(32, 0);
            _perfectButton.HAlign = 1f;
            _perfectButton.VAlign = 0.5f;
            _perfectButton.OnLeftClick += (evt, element) => PerfectReforge();
            panelBottom.Append(_perfectButton);
        }

        private void CreatePresetPanel()
        {
            foreach (int id in _presetPrefixIds)
                _presetWrapPanel.Append(CreatePresetButton(id));

            _presetWrapPanel.UpdateContainer_Height();
        }

        private UIButton1 CreatePresetButton(int id)
        {
            string name = GetPrefixName(id);
            var btn = new UIButton1(name);
            btn.Width.Set(84, 0);
            btn.Height.Set(28, 0);
            btn.MarginLeft = 2;
            btn.MarginRight = 2;
            btn.MarginTop = 2;
            btn.MarginBottom = 2;
            btn.EnableColorBack = new Color(96, 66, 148) * 0.85f;
            btn.MouseOverColorBack = new Color(120, 88, 174);
            btn.OnUpdate += _ =>
            {
                if (btn.IsMouseHovering)
                    Main.instance.MouseText($"应用前缀: {name}");
            };

            int capturedId = id;
            btn.OnLeftClick += (evt, element) => ApplyPreset(capturedId);
            return btn;
        }

        private string GetPrefixName(int prefixId)
        {
            if (prefixId <= 0) return "无";
            if (prefixId < Lang.prefix.Length && Lang.prefix[prefixId] != null)
                return Lang.prefix[prefixId].Value;
            return $"前缀 {prefixId}";
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // 预设按钮换行后容器高度跟随内容, 保证滚动条正确
            _presetWrapPanel?.UpdateContainer_Height();

            if (_reforgeCooldown > 0 && --_reforgeCooldown <= 0)
                _perfectButton.isEnable = true;
        }

        private void SetStatus(string text, Color color)
        {
            _statusText.SetText(text);
            _statusText.TextColor = color;
        }

        private void OnItemChanged(Item item)
        {
            _targetItem = item?.Clone() ?? new Item();
            _isBest = false;
            UpdateItemNameText();
        }

        private void UpdateItemNameText()
        {
            if (_targetItem.IsAir)
            {
                _itemNameText.SetText("未选择物品");
                _itemNameText.TextColor = Color.White;
                _prefixText.SetText("当前前缀: 无");
                _prefixText.TextColor = Color.Gray;
            }
            else
            {
                _itemNameText.SetText(_targetItem.Name);
                _itemNameText.TextColor = _isBest ? new Color(0xEE, 0x60, 0x60) : Color.White;

                if (_targetItem.prefix > 0 && _targetItem.prefix < Lang.prefix.Length && Lang.prefix[_targetItem.prefix] != null)
                {
                    _prefixText.SetText($"当前前缀: {Lang.prefix[_targetItem.prefix].Value}");
                    _prefixText.TextColor = _isBest ? new Color(255, 215, 0) : new Color(255, 236, 170);
                }
                else
                {
                    _prefixText.SetText("当前前缀: 无");
                    _prefixText.TextColor = Color.Gray;
                }
            }
        }

        private void WriteToInventorySlot(int slot, Item item)
        {
            if (slot < 0 || slot >= Main.LocalPlayer.inventory.Length) return;
            Main.LocalPlayer.inventory[slot] = item.Clone();
        }

        private bool TryGetTargetSlot()
        {
            if (_targetItem.IsAir)
            {
                SetStatus("请先放入物品", Color.OrangeRed);
                return false;
            }

            int slot = _itemSlot.SourceSlot;
            if (slot == -1 || slot >= Main.LocalPlayer.inventory.Length)
            {
                SetStatus("找不到来源槽位", Color.OrangeRed);
                return false;
            }
            return true;
        }

        private void ResetPrefix()
        {
            if (!TryGetTargetSlot()) return;

            int slot = _itemSlot.SourceSlot;

            _targetItem.prefix = 0; // 移除前缀

            WriteToInventorySlot(slot, _targetItem);
            _itemSlot.SetItem(_targetItem.Clone(), slot);
            _isBest = false;
            UpdateItemNameText();
            SetStatus("已移除前缀", Color.White);
        }

        /// <summary>
        /// 对目标物品做一次随机重铸
        /// </summary>
        private Item RollReforge(out bool topTier)
        {
            Item tempItem = new Item();
            tempItem.SetDefaults(_targetItem.type);
            tempItem.stack = _targetItem.stack;

            if (!tempItem.Prefix(-2, out topTier))
                return null;
            return tempItem;
        }

        /// <summary>
        /// 把重铸结果写回物品槽, 并刷新显示与浮动文字
        /// </summary>
        private void ApplyReforgeResult(Item resultItem, bool gotBest, string statusText, Color statusColor)
        {
            resultItem.favorited = _targetItem.favorited;

            int slot = _itemSlot.SourceSlot;
            WriteToInventorySlot(slot, resultItem);
            _itemSlot.SetItem(resultItem.Clone(), slot);

            _isBest = gotBest;
            UpdateItemNameText();
            SetStatus(statusText, statusColor);

            Player player = Main.LocalPlayer;
            Vector2 position = player.Center;
            PopupText.NewText(gotBest ? PopupTextContext.ItemReforge_Best : PopupTextContext.ItemReforge, resultItem, position, resultItem.stack, noStack: true);
        }

        private void StartCooldown()
        {
            _reforgeCooldown = 60;
            _perfectButton.isEnable = false;
        }

        /// <summary>
        /// 随机重铸: 只重铸一次
        /// </summary>
        private void RandomReforge()
        {
            if (!TryGetTargetSlot()) return;

            bool topTier;
            Item finalItem = RollReforge(out topTier);
            if (finalItem == null) return;

            string prefixName = GetPrefixName(finalItem.prefix);
            ApplyReforgeResult(finalItem, topTier,
                topTier ? $"随机重铸: {prefixName} (完美!)" : $"随机重铸: {prefixName}",
                Color.White);
        }

        /// <summary>
        /// 完美重铸: 反复重铸, 直到获得完美前缀或达到最大尝试次数
        /// </summary>
        private void PerfectReforge()
        {
            if (!TryGetTargetSlot()) return;

            Item finalItem = null;
            bool gotBest = false;
            int attempts = 0;

            for (int attempt = 0; attempt < _maxAttempts; attempt++)
            {
                attempts++;
                bool topTier;
                Item tempItem = RollReforge(out topTier);
                if (tempItem == null) return;

                finalItem = tempItem;
                if (topTier)
                {
                    gotBest = true;
                    break;
                }
            }

            if (finalItem == null) return;

            string prefixName = GetPrefixName(finalItem.prefix);
            if (gotBest)
            {
                ApplyReforgeResult(finalItem, true, $"完美重铸: 第 {attempts} 次获得完美 {prefixName}", new Color(255, 215, 0));
                StartCooldown();
            }
            else
            {
                ApplyReforgeResult(finalItem, false, $"完美重铸: {attempts} 次未达完美, 当前 {prefixName}", Color.Gray);
            }
        }

        /// <summary>
        /// 预设前缀: 反复重铸, 直到获得目标前缀或达到最大尝试次数
        /// </summary>
        private void ApplyPreset(int targetPrefixId)
        {
            if (!TryGetTargetSlot()) return;

            Item resultItem = null;
            bool gotBest = false;
            int attempts = 0;

            for (int attempt = 0; attempt < _maxAttempts; attempt++)
            {
                attempts++;
                bool topTier;
                Item tempItem = RollReforge(out topTier);
                if (tempItem == null) return;

                if (tempItem.prefix == targetPrefixId)
                {
                    resultItem = tempItem;
                    gotBest = topTier;
                    break;
                }
            }

            string targetName = GetPrefixName(targetPrefixId);
            if (resultItem == null)
            {
                SetStatus($"未在 {_maxAttempts} 次内获得 [{targetName}], 已保留原前缀", Color.OrangeRed);
                return;
            }

            ApplyReforgeResult(resultItem, gotBest,
                gotBest ? $"已应用 [{targetName}] (完美! 第 {attempts} 次)" : $"已应用 [{targetName}] (第 {attempts} 次尝试)",
                gotBest ? new Color(255, 215, 0) : Color.White);

            if (gotBest)
                StartCooldown();
        }

        public override void OnDeactivate()
        {
            base.OnDeactivate();
            if (!_targetItem.IsAir)
            {
                int slot = _itemSlot.SourceSlot;
                if (slot != -1 && slot < Main.LocalPlayer.inventory.Length && Main.LocalPlayer.inventory[slot].IsAir)
                {
                    WriteToInventorySlot(slot, _targetItem);
                }
                else
                {
                    for (int i = 0; i < Main.LocalPlayer.inventory.Length; i++)
                    {
                        if (Main.LocalPlayer.inventory[i].IsAir)
                        {
                            WriteToInventorySlot(i, _targetItem);
                            break;
                        }
                    }
                }
                _itemSlot.SetItem(null);
            }
        }

        private class UIItemSlot : UIElement
        {
            public event Action<Item> OnItemChanged;

            private Item _item = new Item();
            private int _sourceSlot = -1;

            public int SourceSlot => _sourceSlot;
            public Item CurrentItem => _item;

            public UIItemSlot()
            {
                Width.Set(52, 0);
                Height.Set(52, 0);
            }

            public void SetItem(Item item, int slot = -1)
            {
                _item = item?.Clone() ?? new Item();
                _sourceSlot = slot;
                OnItemChanged?.Invoke(_item);
            }

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                var dim = GetDimensions();
                if (_item.IsAir)
                    spriteBatch.Draw(Terraria.GameContent.TextureAssets.InventoryBack.Value, dim.Position(), Color.White);
                else
                    ItemSlot.Draw(spriteBatch, ref _item, ItemSlot.Context.InventoryItem, dim.Position());

                if (ContainsPoint(Main.MouseScreen))
                {
                    Main.LocalPlayer.mouseInterface = true;
                    Item[] itemArray = new Item[] { _item };
                    ItemSlot.OverrideHover(itemArray, ItemSlot.Context.InventoryItem, 0);

                    if (_item.IsAir)
                        Main.instance.MouseText("放入背包中的物品");

                    if (Main.mouseLeft && Main.mouseLeftRelease)
                    {
                        Item mouseItem = Main.mouseItem;
                        if (!mouseItem.IsAir)
                        {
                            if (!_item.IsAir)
                            {
                                Main.NewText("请先取出槽位中的物品");
                                return;
                            }
                            int slot = -1;
                            for (int i = 0; i < Main.LocalPlayer.inventory.Length; i++)
                            {
                                Item invItem = Main.LocalPlayer.inventory[i];
                                if (!invItem.IsAir && invItem.type == mouseItem.type && invItem.prefix == mouseItem.prefix && invItem.stack == mouseItem.stack)
                                {
                                    slot = i;
                                    break;
                                }
                            }
                            if (slot == -1)
                            {
                                Main.NewText("请直接拿起整个物品堆叠放入（不要拆分）");
                                return;
                            }
                            _item = mouseItem.Clone();
                            _sourceSlot = slot;
                            Main.mouseItem = new Item();
                            OnItemChanged?.Invoke(_item);
                        }
                        else if (!_item.IsAir)
                        {
                            Main.mouseItem = _item.Clone();
                            _item = new Item();
                            _sourceSlot = -1;
                            OnItemChanged?.Invoke(_item);
                        }
                    }
                }
            }
        }
    }
}

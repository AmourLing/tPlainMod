using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using tContentPatch.Content.UI;
using Terraria;
using Terraria.GameContent.UI;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;

namespace BetterPrefix
{
    internal class UIBetterPrefix : UIWindow
    {
        private Item _targetItem = new Item();
        private UIItemSlot _itemSlot;
        private UIText _itemNameText;
        private UIButton1 _resetButton;
        private UIButton1 _reforgeButton;
        private UIPanel _presetPanel;
        private UIWrapPanel _presetWrapPanel;
        private bool _isBest = false;

        private int _reforgeCooldown = 0;
        private bool _reforgeButtonEnabled = true;

        private List<int> _presetPrefixIds;

        private int _maxAttempts;

        public UIBetterPrefix(string title, int width, int height) : base(title, width, height)
        {
            HAlign = 0f;
            VAlign = 0.5f;

            // 读取设置值
            _presetPrefixIds = PresetPrefixesSetting.CurrentPresetIds ?? new List<int> { 65, 72, 81, 82, 83, 84, 85 };
            _maxAttempts = MaxAttemptsSetting.CurrentMaxAttempts > 0 ? MaxAttemptsSetting.CurrentMaxAttempts : 100;

            BuildUI();
            CreatePresetPanel();
        }

        private void BuildUI()
        {
            // 顶部面板：物品槽
            var panelTop = new UIPanel();
            panelTop.Width.Set(0, 1f);
            panelTop.Height.Set(60, 0);
            panelTop.SetPadding(5);
            Child.Append(panelTop);

            _itemSlot = new UIItemSlot();
            _itemSlot.Width.Set(52, 0);
            _itemSlot.Height.Set(52, 0);
            _itemSlot.Left.Set(5, 0);
            _itemSlot.Top.Set(5, 0);
            _itemSlot.OnItemChanged += OnItemChanged;
            panelTop.Append(_itemSlot);

            _itemNameText = new UIText("未选择物品");
            _itemNameText.Left.Set(65, 0);
            _itemNameText.Top.Set(20, 0);
            panelTop.Append(_itemNameText);

            // 中部面板：预设前缀按钮区域
            var panelMiddle = new UIPanel();
            panelMiddle.Width.Set(0, 1f);
            panelMiddle.Height.Set(-110, 1f); // 减去顶部和底部面板高度
            panelMiddle.Top.Set(60, 0);
            panelMiddle.SetPadding(5);
            panelMiddle.OverflowHidden = true;
            Child.Append(panelMiddle);

            _presetPanel = new UIPanel();
            _presetPanel.Width.Set(0, 1f);
            _presetPanel.Height.Set(0, 1f);
            _presetPanel.SetPadding(5);
            _presetPanel.BackgroundColor = Color.Transparent;
            _presetPanel.BorderColor = Color.Transparent;
            panelMiddle.Append(_presetPanel);

            _presetWrapPanel = new UIWrapPanel();
            _presetWrapPanel.Width.Set(0, 1f);
            _presetWrapPanel.Height.Set(0, 1f);
            _presetWrapPanel.OverflowHidden = false;
            _presetPanel.Append(_presetWrapPanel);

            // 底部面板：重置和随机重铸两个按钮
            var panelBottom = new UIPanel();
            panelBottom.Width.Set(0, 1f);
            panelBottom.Height.Set(50, 0);
            panelBottom.VAlign = 1f;
            panelBottom.SetPadding(5);
            Child.Append(panelBottom);

            _resetButton = new UIButton1("重置前缀");
            _resetButton.Width.Set(80, 0);
            _resetButton.Height.Set(30, 0);
            _resetButton.HAlign = 0.3f;
            _resetButton.VAlign = 0.5f;
            _resetButton.OnLeftClick += (evt, element) => ResetPrefix();
            panelBottom.Append(_resetButton);

            _reforgeButton = new UIButton1("随机重铸");
            _reforgeButton.Width.Set(80, 0);
            _reforgeButton.Height.Set(30, 0);
            _reforgeButton.HAlign = 0.7f;
            _reforgeButton.VAlign = 0.5f;
            _reforgeButton.OnLeftClick += (evt, element) =>
            {
                if (_reforgeButtonEnabled)
                    RandomReforge();
            };
            panelBottom.Append(_reforgeButton);
        }

        private void CreatePresetPanel()
        {
            foreach (int id in _presetPrefixIds)
            {
                string name = GetPrefixName(id);
                var btn = new UIButton1(name);
                btn.Width.Set(70, 0);
                btn.Height.Set(30, 0);
                btn.MarginLeft = 2;
                btn.MarginRight = 2;
                btn.MarginTop = 2;
                btn.MarginBottom = 2;
                btn.BackgroundColor = Color.DarkSlateBlue;
                btn.TextColor = Color.White;

                int capturedId = id;
                btn.OnLeftClick += (evt, element) => ApplyPreset(capturedId);
                _presetWrapPanel.Append(btn);
            }
        }

        private string GetPrefixName(int prefixId)
        {
            if (prefixId >= 0 && prefixId < Lang.prefix.Length && Lang.prefix[prefixId] != null)
                return Lang.prefix[prefixId].Value;
            return $"前缀 {prefixId}";
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (_reforgeCooldown > 0)
            {
                _reforgeCooldown--;
                if (_reforgeCooldown <= 0)
                {
                    _reforgeButtonEnabled = true;
                    _reforgeButton.BackgroundColor = Color.Gray;
                }
                else
                {
                    _reforgeButtonEnabled = false;
                    _reforgeButton.BackgroundColor = Color.DarkGray;
                }
            }
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
            }
            else
            {
                string prefixName = _targetItem.prefix == 0 ? "" : Lang.prefix[_targetItem.prefix]?.Value + " ";
                _itemNameText.SetText(prefixName + _targetItem.Name);
                _itemNameText.TextColor = _isBest ? new Color(0xEE, 0x00, 0x00) : Color.White;
            }
        }

        private void WriteToInventorySlot(int slot, Item item)
        {
            if (slot < 0 || slot >= Main.LocalPlayer.inventory.Length) return;
            Main.LocalPlayer.inventory[slot] = item.Clone();
        }

        private void ResetPrefix()
        {
            if (_targetItem.IsAir) return;

            int slot = _itemSlot.SourceSlot;
            if (slot == -1 || slot >= Main.LocalPlayer.inventory.Length) return;

            _targetItem.prefix = 0; // 移除前缀

            WriteToInventorySlot(slot, _targetItem);
            _itemSlot.SetItem(_targetItem.Clone(), slot);
            _isBest = false;
            UpdateItemNameText();

            /*Item newItem = new Item();
            newItem.SetDefaults(_targetItem.type);
            newItem.stack = _targetItem.stack;

            WriteToInventorySlot(slot, newItem);
            _itemSlot.SetItem(newItem.Clone(), slot);
            _isBest = false;
            UpdateItemNameText();*/
        }

        private void RandomReforge()
        {
            if (_targetItem.IsAir) return;

            int slot = _itemSlot.SourceSlot;
            if (slot == -1 || slot >= Main.LocalPlayer.inventory.Length) return;

            Item finalItem = null;
            bool gotBest = false;
            bool originalFavorited = _targetItem.favorited;

            for (int attempt = 0; attempt < _maxAttempts; attempt++)
            {
                Item tempItem = new Item();
                tempItem.SetDefaults(_targetItem.type);
                tempItem.stack = _targetItem.stack;

                bool topTier;
                if (!tempItem.Prefix(-2, out topTier))
                    return;

                finalItem = tempItem;
                if (topTier)
                {
                    gotBest = true;
                    break;
                }
            }

            if (finalItem == null) return;

            finalItem.favorited = originalFavorited;

            WriteToInventorySlot(slot, finalItem);
            _itemSlot.SetItem(finalItem.Clone(), slot);
            _isBest = gotBest;
            UpdateItemNameText();

            Player player = Main.LocalPlayer;
            Vector2 position = player.Center;
            PopupText.NewText(gotBest ? PopupTextContext.ItemReforge_Best : PopupTextContext.ItemReforge, finalItem, position, finalItem.stack, noStack: true);

            if (gotBest)
            {
                _reforgeCooldown = 60;
                _reforgeButtonEnabled = false;
                _reforgeButton.BackgroundColor = Color.DarkGray;
            }
        }

        private void ApplyPreset(int targetPrefixId)
        {
            if (_targetItem.IsAir) return;

            int slot = _itemSlot.SourceSlot;
            if (slot == -1 || slot >= Main.LocalPlayer.inventory.Length) return;

            Item resultItem = null;
            bool gotBest = false;
            bool originalFavorited = _targetItem.favorited;

            for (int attempt = 0; attempt < _maxAttempts; attempt++)
            {
                Item tempItem = new Item();
                tempItem.SetDefaults(_targetItem.type);
                tempItem.stack = _targetItem.stack;

                bool topTier;
                if (!tempItem.Prefix(-2, out topTier))
                    return;

                if (tempItem.prefix == targetPrefixId)
                {
                    resultItem = tempItem;
                    gotBest = topTier;
                    break;
                }
            }

            if (resultItem == null)
                return; // 未获得目标前缀，静默失败

            resultItem.favorited = originalFavorited;

            WriteToInventorySlot(slot, resultItem);
            _itemSlot.SetItem(resultItem.Clone(), slot);
            _isBest = gotBest;
            UpdateItemNameText();

            Player player = Main.LocalPlayer;
            Vector2 position = player.Center;
            PopupText.NewText(gotBest ? PopupTextContext.ItemReforge_Best : PopupTextContext.ItemReforge, resultItem, position, resultItem.stack, noStack: true);

            if (gotBest)
            {
                _reforgeCooldown = 60;
                _reforgeButtonEnabled = false;
                _reforgeButton.BackgroundColor = Color.DarkGray;
            }
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
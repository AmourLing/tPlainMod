using Microsoft.Xna.Framework.Graphics;
using System;
using tContentPatch;
using tContentPatch.Content.UI.ModSet;
using Terraria;
using Terraria.UI;

namespace BetterPrefix
{
    internal class Setting : ModSetting
    {
        public override string Name => "设置";
        public override string Title => "更好的前缀: 设置";
        public override string FilePath => "setting.json";
        public override Type DataType => typeof(bool);

        private bool _addToQuickButton = true;
        private Action<bool> _updateUI;

        public override void Load(object v)
        {
            if (v == null)
            {
                SetDefault();
                Save();
            }
            else
            {
                _addToQuickButton = (bool)v;
            }

            if (_addToQuickButton)
                ModQuickButton.TryAddButton();
            else
                ModQuickButton.TryRemoveButton();
        }

        public override object GetSaveData() => _addToQuickButton;

        public override void SetDefault()
        {
            _addToQuickButton = true;
            NeedSave = true;
            _updateUI?.Invoke(_addToQuickButton);
        }

        public override UIElement GetUI()
        {
            var ui = new UIItemSwitch(
                Main.Assets.Request<Texture2D>("Images/UI/ButtonPlay", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value,
                "添加到快捷按钮");
            ui.OnValUpdate += v =>
            {
                if (_addToQuickButton == v) return;
                _addToQuickButton = v;
                NeedSave = true;

                if (_addToQuickButton)
                    ModQuickButton.TryAddButton();
                else
                    ModQuickButton.TryRemoveButton();
            };
            ui.OnUpdate += _ => { if (ui.IsMouseHovering) Main.instance.MouseText("需要 QuickButton 模组"); };
            _updateUI = ui.SetVal;

            _updateUI(_addToQuickButton);
            return ui;
        }
    }
}
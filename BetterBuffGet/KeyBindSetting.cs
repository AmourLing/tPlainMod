using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using QuickSetting;
using System;
using tContentPatch;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace BetterBuffGet
{
    public class KeyBindSetting : ModSetting
    {
        public override string Name => "开关按键";
        public override string Title => "更好的增益获取: 开关按键";
        public override string FilePath => "toggleKey.json";
        public override Type DataType => typeof(string);

        private static string _toggleKey = null;
        private static Action<string> _updateUI;

        public override void Load(object v)
        {
            _toggleKey = v as string;
            BetterBuffGetMod.SetToggleKey(_toggleKey);
        }

        public override UIElement GetUI()
        {
            var ui = new UIKeyBind("开关增益功能");
            ui.SetKey(_toggleKey);
            ui.OnKeyUpdate += s =>
            {
                _toggleKey = s;
                NeedSave = true;
                BetterBuffGetMod.SetToggleKey(_toggleKey);
            };
            _updateUI = ui.SetKey;
            return ui;
        }

        public override object GetSaveData() => _toggleKey;

        public override void SetDefault()
        {
            _toggleKey = null;
            NeedSave = true;
            BetterBuffGetMod.SetToggleKey(_toggleKey);
            _updateUI?.Invoke(_toggleKey);
        }
    }

    // 自定义按键绑定控件（参考 QuickSetting 的 UIKeyBind）
    internal class UIKeyBind : UIElement
    {
        public event Action<string> OnKeyUpdate;
        private string _key;
        private UIText _display;
        private bool _listening;

        public UIKeyBind(string description)
        {
            var text = new UIText(description);
            text.Left.Set(0, 0);
            text.VAlign = 0.5f;
            Append(text);

            _display = new UIText("未绑定");
            _display.HAlign = 1f;
            _display.VAlign = 0.5f;
            _display.TextColor = Color.Gray;
            Append(_display);

            OnLeftClick += (evt, element) =>
            {
                if (_listening) return;
                _listening = true;
                _display.TextColor = Color.Gold;
                ListenInput.AddListenInputOne(OnKeyReceived);
            };
        }

        public void SetKey(string key)
        {
            _key = key;
            _listening = false;
            _display.SetText(string.IsNullOrEmpty(key) ? "未绑定" : key);
            _display.TextColor = string.IsNullOrEmpty(key) ? Color.Gray : Color.White;
        }

        private void OnKeyReceived(string key)
        {
            SetKey(key);
            OnKeyUpdate?.Invoke(key);
        }
    }
}
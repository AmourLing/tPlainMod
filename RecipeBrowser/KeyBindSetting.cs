using System;
using tContentPatch;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace RecipeBrowser
{
    /// <summary>
    /// 打开面板的快捷键 (默认键盘 2), 可在模组设置中配置
    /// </summary>
    public class KeyBindSetting : ModSetting
    {
        public override string Name => "快捷键";
        public override string Title => "配方浏览器: 快捷键";
        public override string FilePath => "toggleKey.json";
        public override Type DataType => typeof(string);

        private static string _toggleKey = "NumPad2"; // 小键盘 2
        private static Action<string> _updateUI;

        public override void Load(object v)
        {
            if (v == null)
            {
                SetDefault();
                Save();
            }
            else
            {
                _toggleKey = v as string;
            }
            RecipeBrowserMod.SetToggleKey(_toggleKey);
        }

        public override object GetSaveData() => _toggleKey;

        public override void SetDefault()
        {
            _toggleKey = "NumPad2";
            NeedSave = true;
            RecipeBrowserMod.SetToggleKey(_toggleKey);
            _updateUI?.Invoke(_toggleKey);
        }

        public override UIElement GetUI()
        {
            var ui = new UIKeyBind("开关配方浏览器");
            ui.SetKey(_toggleKey);
            ui.OnKeyUpdate += s =>
            {
                _toggleKey = s;
                NeedSave = true;
                RecipeBrowserMod.SetToggleKey(_toggleKey);
            };
            ui.OnUpdate += _ => { if (ui.IsMouseHovering) Main.instance.MouseText("需要 QuickSetting 模组的按键监听"); };
            _updateUI = ui.SetKey;
            return ui;
        }

        // 自定义按键绑定控件 (与 BetterBuffGet 同款)
        private class UIKeyBind : UIElement
        {
            public event Action<string> OnKeyUpdate;
            private string _key;
            private readonly UIText _display;
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
                _display.TextColor = Microsoft.Xna.Framework.Color.Gray;
                Append(_display);

                OnLeftClick += (evt, element) =>
                {
                    if (_listening) return;
                    _listening = true;
                    _display.TextColor = Microsoft.Xna.Framework.Color.Gold;
                    ListenInput.AddListenInputOne(OnKeyReceived);
                };
            }

            public void SetKey(string key)
            {
                _key = key;
                _listening = false;
                _display.SetText(string.IsNullOrEmpty(key) ? "未绑定" : key);
                _display.TextColor = string.IsNullOrEmpty(key) ? Microsoft.Xna.Framework.Color.Gray : Microsoft.Xna.Framework.Color.White;
            }

            private void OnKeyReceived(string key)
            {
                SetKey(key);
                OnKeyUpdate?.Invoke(key);
            }
        }
    }
}

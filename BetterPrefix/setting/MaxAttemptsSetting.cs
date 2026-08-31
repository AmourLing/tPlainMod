using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using tContentPatch;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace BetterPrefix
{
    public class MaxAttemptsSetting : ModSetting
    {
        public override string Name => "最大尝试次数";
        public override string Title => "更好的前缀: 最大尝试次数";
        public override string FilePath => "maxAttempts.json";
        public override Type DataType => typeof(int);

        private int _maxAttempts = 100;
        private Action<int> _updateUI;

        public static int CurrentMaxAttempts { get; private set; } = 100;

        public override void Load(object v)
        {
            if (v == null)
            {
                SetDefault();
                Save();
            }
            else
            {
                _maxAttempts = (int)v;
            }
            CurrentMaxAttempts = _maxAttempts;
        }

        public override object GetSaveData() => _maxAttempts;

        public override void SetDefault()
        {
            _maxAttempts = 100;
            NeedSave = true;
            _updateUI?.Invoke(_maxAttempts);
        }

        public override UIElement GetUI()
        {
            var panel = new UIPanel();
            panel.Width.Set(0, 1f);
            panel.Height.Set(100, 0);
            panel.SetPadding(5);

            var label = new UIText("随机/预设重铸最大尝试次数");
            label.Top.Set(5, 0);
            panel.Append(label);

            var slider = new UISlider(1, 500);
            slider.Top.Set(30, 0);
            slider.Width.Set(-20, 1f);
            slider.Height.Set(20, 0);
            slider.SetValue(_maxAttempts);
            slider.OnValueChanged += (evt, element) =>
            {
                _maxAttempts = (int)((UISlider)element).Value;
                NeedSave = true;
            };
            panel.Append(slider);

            var valueText = new UIText(_maxAttempts.ToString());
            valueText.Top.Set(55, 0);
            valueText.HAlign = 0.5f;
            panel.Append(valueText);

            var hint = new UIText("修改后需要重新加载模组生效");
            hint.Top.Set(80, 0);
            hint.TextColor = Color.Gray;
            panel.Append(hint);

            _updateUI = v =>
            {
                slider.SetValue(v);
                valueText.SetText(v.ToString());
            };
            return panel;
        }

        // 简单的滑块控件（tContentPatch可能已有，这里模拟）
        private class UISlider : UIElement
        {
            public float Value { get; private set; }
            private float _min, _max;
            public event UIElement.MouseEvent OnValueChanged;

            public UISlider(float min, float max)
            {
                _min = min;
                _max = max;
                Value = min;
            }

            public void SetValue(float val)
            {
                Value = MathHelper.Clamp(val, _min, _max);
            }

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                // 简单绘制一个条，实际应使用更美观的实现
                var dim = GetDimensions();
                var handlePos = dim.X + dim.Width * (Value - _min) / (_max - _min);
                spriteBatch.Draw(MagicPixel, new Rectangle((int)dim.X, (int)dim.Y + 10, (int)dim.Width, 2), Color.Gray);
                spriteBatch.Draw(MagicPixel, new Rectangle((int)handlePos - 5, (int)dim.Y + 5, 10, 12), Color.White);

                if (ContainsPoint(Main.MouseScreen) && Main.mouseLeft)
                {
                    float newVal = _min + (Main.MouseScreen.X - dim.X) / dim.Width * (_max - _min);
                    SetValue(newVal);
                    OnValueChanged?.Invoke(null, this);
                }
            }

            private static Texture2D _magicPixel;
            private static Texture2D MagicPixel
            {
                get
                {
                    if (_magicPixel == null)
                    {
                        _magicPixel = new Texture2D(Main.graphics.GraphicsDevice, 1, 1);
                        _magicPixel.SetData(new[] { Color.White });
                    }
                    return _magicPixel;
                }
            }
        }
    }
}
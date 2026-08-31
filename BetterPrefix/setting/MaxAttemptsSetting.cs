using Microsoft.Xna.Framework;
using System;
using tContentPatch;
using tContentPatch.Content.UI;
using tContentPatch.Content.UI.ModSet;
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
            var stack = new UIStackPanel();
            stack.Width.Set(0, 1f);
            stack.ItemMargin = 6;
            stack.IsAutoUpdateSize = true;

            // 与模组设置风格一致的滑块, 数值显示在标题右侧
            var slider = new UIItemValueSlider(1, 500, null, "最大尝试次数");
            slider.FloatToString = v => ((int)v).ToString();
            slider.SetVal(_maxAttempts);
            slider.OnValUpdate += v =>
            {
                _maxAttempts = (int)v;
                NeedSave = true;
            };
            stack.Append(slider);

            var hint1 = new UIText("完美重铸与预设前缀单次操作的最大重铸次数", 0.8f);
            hint1.TextColor = Color.Gray;
            stack.Append(hint1);

            var hint2 = new UIText("修改后需要重新加载模组生效", 0.8f);
            hint2.TextColor = Color.Gray;
            stack.Append(hint2);

            _updateUI = v => slider.SetVal(v);
            return stack;
        }
    }
}

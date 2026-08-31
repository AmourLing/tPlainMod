using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using tContentPatch.Content.UI;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace BetterAchievementUnlocker
{
    internal class UIAchievementUnlocker : UIWindow
    {
        private UIButton1 _unlockButton;

        public UIAchievementUnlocker(string title, int width, int height) : base(title, width, height)
        {
            HAlign = 0.5f;
            VAlign = 0.5f;

            // 主面板
            var panel = new UIPanel();
            panel.Width.Set(0, 1f);
            panel.Height.Set(0, 1f);
            panel.SetPadding(20);
            Child.Append(panel);

            // 说明文字
            var desc = new UIText("点击下方按钮将解锁当前未完成的所有成就");
            desc.HAlign = 0.5f;
            desc.Top.Set(20, 0);
            desc.TextColor = Color.LightGray;
            panel.Append(desc);

            // 解锁按钮
            _unlockButton = new UIButton1("解锁全部成就");
            _unlockButton.Width.Set(200, 0);
            _unlockButton.Height.Set(50, 0);
            _unlockButton.HAlign = 0.5f;
            _unlockButton.VAlign = 0.5f;
            _unlockButton.BackgroundColor = Color.DarkGreen;
            _unlockButton.TextColor = Color.White;
            _unlockButton.OnLeftClick += (evt, element) =>
            {
                BetterAchievementUnlockerMod.UnlockAllAchievements();
                // 可选：关闭窗口
                // Close();
            };
            panel.Append(_unlockButton);

            // 提示
            var hint = new UIText("解锁后可能需要重新打开成就面板刷新显示", 0.8f);
            hint.HAlign = 0.5f;
            hint.Top.Set(-20, 1f);
            hint.TextColor = Color.Gray;
            panel.Append(hint);
        }
    }
}
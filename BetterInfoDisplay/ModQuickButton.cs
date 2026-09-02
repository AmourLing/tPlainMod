using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using System.Reflection;
using tContentPatch;
using Terraria;
using Terraria.GameContent.UI.Elements;

namespace BetterInfoDisplay
{
    internal static class ModQuickButton
    {
        private static bool _buttonAdded = false;
        private static readonly string ButtonIdentifier = "BetterInfoDisplay.ToggleUI";

        /// <summary>
        /// 尝试向 QuickButton 添加按钮
        /// </summary>
        public static void TryAddButton()
        {
            if (_buttonAdded) return;
            if (!IsQuickButtonLoaded()) return;

            try
            {
                var modObjects = ContentPatch.GetModObjects();
                var quickButtonMod = modObjects?.FirstOrDefault(m => m.assembly?.GetName().Name == "QuickButton");
                if (quickButtonMod == null) return;

                var quickButtonType = quickButtonMod.assembly.GetType("QuickButton.QuickButton.QuickButton");
                if (quickButtonType == null) return;

                var addMethod = quickButtonType.GetMethod("Add", BindingFlags.Public | BindingFlags.Static);
                if (addMethod == null) return;

                // 图标: 天平(信息/属性类)
                var icon = Main.Assets.Request<Texture2D>("Images/Item_493", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
                var buttonImage = new UIImage(icon)
                {
                    Width = { Pixels = 32 },
                    Height = { Pixels = 32 },
                    ScaleToFit = true
                };
                buttonImage.OnUpdate += _ =>
                {
                    if (buttonImage.IsMouseHovering)
                        Main.instance.MouseText("角色信息");
                };
                buttonImage.OnLeftClick += (evt, element) =>
                {
                    Terraria.Audio.SoundEngine.PlaySound(12);
                    QuickSetting.QuickSetting.QuickSetting.SwitchOpenOrClose();
                };

                addMethod.Invoke(null, new object[] { ButtonIdentifier, buttonImage });
                _buttonAdded = true;
            }
            catch { /* 静默失败 */ }
        }

        /// <summary>
        /// 尝试从 QuickButton 移除按钮
        /// </summary>
        public static void TryRemoveButton()
        {
            if (!_buttonAdded) return;
            if (!IsQuickButtonLoaded()) return;

            try
            {
                var modObjects = ContentPatch.GetModObjects();
                var quickButtonMod = modObjects?.FirstOrDefault(m => m.assembly?.GetName().Name == "QuickButton");
                if (quickButtonMod == null) return;

                var quickButtonType = quickButtonMod.assembly.GetType("QuickButton.QuickButton.QuickButton");
                if (quickButtonType == null) return;

                var removeMethod = quickButtonType.GetMethod("Remove", BindingFlags.Public | BindingFlags.Static);
                if (removeMethod == null) return;

                removeMethod.Invoke(null, new object[] { ButtonIdentifier });
                _buttonAdded = false;
            }
            catch { }
        }

        /// <summary>
        /// 检查 QuickButton 模组是否已加载
        /// </summary>
        private static bool IsQuickButtonLoaded()
        {
            var modObjects = ContentPatch.GetModObjects();
            return modObjects?.Any(m => m.assembly?.GetName().Name == "QuickButton") ?? false;
        }
    }
}

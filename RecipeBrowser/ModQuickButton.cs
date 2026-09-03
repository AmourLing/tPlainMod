using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using System.Reflection;
using tContentPatch;
using Terraria;
using Terraria.GameContent.UI.Elements;

namespace RecipeBrowser
{
    internal static class ModQuickButton
    {
        private static bool _buttonAdded = false;
        private static readonly string ButtonIdentifier = "RecipeBrowser.ToggleUI";

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

                // 图标: 铁砧 (合成)
                var icon = Main.Assets.Request<Texture2D>("Images/Item_16", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
                var buttonImage = new UIImage(icon)
                {
                    Width = { Pixels = 32 },
                    Height = { Pixels = 32 },
                    ScaleToFit = true
                };
                buttonImage.OnUpdate += _ =>
                {
                    if (buttonImage.IsMouseHovering)
                        Main.instance.MouseText("配方浏览器");
                };
                buttonImage.OnLeftClick += (evt, element) =>
                {
                    Terraria.Audio.SoundEngine.PlaySound(12);
                    RecipeBrowserMod.ToggleUI();
                };

                addMethod.Invoke(null, new object[] { ButtonIdentifier, buttonImage });
                _buttonAdded = true;
            }
            catch { /* 静默失败 */ }
        }

        /// <summary>
        /// 尝试从 QuickButton 移除按钮 (QuickButton 当前无 Remove API, 预留兼容)
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
                removeMethod?.Invoke(null, new object[] { ButtonIdentifier });
                _buttonAdded = false;
            }
            catch { }
        }

        private static bool IsQuickButtonLoaded()
        {
            var modObjects = ContentPatch.GetModObjects();
            return modObjects?.Any(m => m.assembly?.GetName().Name == "QuickButton") ?? false;
        }
    }
}

using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using System.Reflection;
using tContentPatch;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;

namespace BetterAchievementUnlocker
{
    internal static class ModQuickButton
    {
        private static bool _buttonAdded = false;
        private static readonly string ButtonIdentifier = "BetterAchievementUnlocker.ToggleUI";

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

                // 使用成就图标（奖杯）
                string iconPath = "Images/NPC_Head_" + NPCHeadID.TaxCollector; // 或使用其他图标
                var icon = Main.Assets.Request<Texture2D>(iconPath, ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
                var buttonImage = new UIImage(icon)
                {
                    Width = { Pixels = 32 },
                    Height = { Pixels = 32 },
                    ScaleToFit = true
                };
                buttonImage.OnUpdate += _ =>
                {
                    if (buttonImage.IsMouseHovering)
                        Main.instance.MouseText("成就解锁器");
                };
                buttonImage.OnLeftClick += (evt, element) =>
                {
                    Terraria.Audio.SoundEngine.PlaySound(12);
                    BetterAchievementUnlockerMod.ToggleUI();
                };

                addMethod.Invoke(null, new object[] { ButtonIdentifier, buttonImage });
                _buttonAdded = true;
            }
            catch { /* 静默失败 */ }
        }

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

        private static bool IsQuickButtonLoaded()
        {
            var modObjects = ContentPatch.GetModObjects();
            return modObjects?.Any(m => m.assembly?.GetName().Name == "QuickButton") ?? false;
        }
    }
}
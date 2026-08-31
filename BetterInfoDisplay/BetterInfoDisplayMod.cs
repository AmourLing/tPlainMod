using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using QuickSetting;
using tContentPatch;
using Terraria;
using ReLogic.Content;

namespace BetterInfoDisplay
{
    public class BetterInfoDisplayMod : PatchMain
    {
        public static bool InfiniteFlightEnabled = false;

        public override void Initialize()
        {
            if (Main.dedServ) return;
            var icon = Main.Assets.Request<Texture2D>("Images/Item_3124", AssetRequestMode.ImmediateLoad).Value;
            QuickSetting.QuickSetting.QuickSetting.AddItem(icon, "角色信息", new UIInfoDisplay());
        }

        public override void UpdatePrefix(GameTime gameTime)
        {
            if (Main.gameMenu) return;
            Player player = Main.LocalPlayer;
            if (player == null || player.dead) return;

            if (InfiniteFlightEnabled)
            {
                if (player.wingTime > 0 || player.wingTimeMax > 0)
                    player.wingTime = player.wingTimeMax;
            }
        }
    }
}
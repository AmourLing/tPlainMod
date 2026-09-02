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
        // ===== 功能开关 (会话内生效, 重启重置) =====
        public static bool InfiniteFlight = false;
        public static bool InfiniteMana = false;
        public static bool LockLife = false;
        public static bool InfiniteBreath = false;
        public static bool NoFallDamage = false;

        public override void Initialize()
        {
            if (Main.dedServ) return;
            var icon = Main.Assets.Request<Texture2D>("Images/Item_3124", AssetRequestMode.ImmediateLoad).Value;
            QuickSetting.QuickSetting.QuickSetting.AddItem(icon, "角色信息", new UIInfoDisplay());

            var damageIcon = Main.Assets.Request<Texture2D>("Images/Item_757", AssetRequestMode.ImmediateLoad).Value;
            QuickSetting.QuickSetting.QuickSetting.AddItem(damageIcon, "伤害详情", new UIDamageStats());
        }

        public override void UpdatePrefix(GameTime gameTime)
        {
            if (Main.gameMenu) return;
            Player p = Main.LocalPlayer;
            if (p == null || !p.active || p.dead) return;

            // 每帧维持各开关的效果
            if (InfiniteFlight && p.wingTimeMax > 0)
                p.wingTime = p.wingTimeMax;

            if (InfiniteMana)
                p.statMana = p.statManaMax2;

            if (LockLife)
                p.statLife = p.statLifeMax2;

            if (InfiniteBreath)
                p.breath = p.breathMax;

            if (NoFallDamage)
                p.noFallDmg = true;
        }
    }
}

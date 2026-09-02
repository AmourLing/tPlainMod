using Microsoft.Xna.Framework;
using System;
using tContentPatch.Content.UI;
using Terraria;
using Terraria.GameContent.UI.Elements;

namespace BetterInfoDisplay
{
    /// <summary>
    /// 角色信息主页: 基础 / 移动 / 永久增益 / 功能开关 / 快捷操作
    /// (伤害类属性在「伤害详情」页)
    /// </summary>
    internal class UIInfoDisplay : InfoPageBase
    {
        protected override void Build()
        {
            // ===== 基础 =====
            Section("【基础】");
            Line("生命", p => $"{p.statLife} / {p.statLifeMax2}",
                p => p.statLife >= p.statLifeMax2 ? Color.LightGreen : Color.White);
            Line("魔力", p => $"{p.statMana} / {p.statManaMax2}",
                p => p.statMana >= p.statManaMax2 ? Color.LightGreen : Color.White);
            Line("防御", p => $"{p.statDefense}");
            Line("幸运", p => $"{p.luck:F2}");
            Line("生命再生", p => $"{p.lifeRegen}");
            Line("无敌帧", p => $"{p.immuneTime}帧",
                p => p.immuneTime > 0 ? Color.Orange : Color.White);

            // ===== 移动 =====
            Section("【移动】");
            Line("移动速度", p => $"{p.moveSpeed:F0}%");
            Line("飞行时间", p => $"{p.wingTime:F0} / {p.wingTimeMax:F0}",
                p => p.wingTimeMax > 0 && p.wingTime >= p.wingTimeMax ? Color.LightGreen : Color.White);
            Line("重力", p => p.gravDir == -1 ? "反转" : "正常",
                p => p.gravDir == -1 ? Color.Yellow : Color.White);

            // ===== 永久增益 =====
            Section("【永久增益】");
            PermLine("工匠面包", p => p.ateArtisanBread);
            PermLine("活力水晶", p => p.usedAegisCrystal);
            PermLine("神盾果", p => p.usedAegisFruit);
            PermLine("奥术水晶", p => p.usedArcaneCrystal);
            PermLine("仙馔密酒", p => p.usedAmbrosia);
            PermLine("粘性蠕虫", p => p.usedGummyWorm);
            PermLine("星系珍珠", p => p.usedGalaxyPearl);

            // ===== 功能开关 =====
            Section("【功能开关】");
            BoolToggle("无限飞行", () => BetterInfoDisplayMod.InfiniteFlight, v => BetterInfoDisplayMod.InfiniteFlight = v,
                "翅膀飞行时间每帧回满");
            BoolToggle("无限魔力", () => BetterInfoDisplayMod.InfiniteMana, v => BetterInfoDisplayMod.InfiniteMana = v,
                "魔力每帧回满, 施法无消耗");
            BoolToggle("生命锁定", () => BetterInfoDisplayMod.LockLife, v => BetterInfoDisplayMod.LockLife = v,
                "生命每帧回满 (不免疫即死机制)");
            BoolToggle("无限呼吸", () => BetterInfoDisplayMod.InfiniteBreath, v => BetterInfoDisplayMod.InfiniteBreath = v,
                "氧气每帧回满, 水下不溺");
            BoolToggle("摔落免疫", () => BetterInfoDisplayMod.NoFallDamage, v => BetterInfoDisplayMod.NoFallDamage = v,
                "免疫摔落伤害");

            // ===== 快捷操作 =====
            Section("【快捷操作】");
            var actionRow = new UIStackPanel();
            actionRow.Horizontal = true;
            actionRow.Width.Set(0, 1f);
            actionRow.Height.Set(30, 0);
            actionRow.ItemMargin = 6;
            Stack.Append(actionRow);

            var flipBtn = new UIButton1("反转重力", 0.8f);
            flipBtn.Width.Set(-6, 0.5f);
            flipBtn.Height.Set(26, 0);
            flipBtn.OnLeftClick += (evt, el) =>
            {
                Player p = Main.LocalPlayer;
                if (p != null && p.active) p.gravDir = -p.gravDir;
            };
            actionRow.Append(flipBtn);

            var clearBtn = new UIButton1("清除减益", 0.8f);
            clearBtn.Width.Set(-6, 0.5f);
            clearBtn.Height.Set(26, 0);
            clearBtn.OnLeftClick += (evt, el) => ClearDebuffs();
            actionRow.Append(clearBtn);

            var hint = new UIText("开关实时生效, 重启游戏后重置为关", 0.8f);
            hint.TextColor = Color.Gray;
            Stack.Append(hint);
        }

        private void BoolToggle(string label, Func<bool> get, Action<bool> set, string hint)
        {
            var row = new UIStackPanel();
            row.Horizontal = true;
            row.Width.Set(0, 1f);
            row.Height.Set(28, 0);
            row.ItemMargin = 6;
            Stack.Append(row);

            var name = new UIText(label, 0.85f);
            name.VAlign = 0.5f;
            row.Append(name);

            var btn = new UIButton1(get() ? "开" : "关", 0.85f);
            btn.Width.Set(56, 0);
            btn.Height.Set(22, 0);
            btn.HAlign = 1f;
            btn.VAlign = 0.5f;
            btn.TextColor = get() ? Color.LightGreen : new Color(200, 120, 120);
            btn.OnLeftClick += (evt, el) =>
            {
                bool nv = !get();
                set(nv);
                btn.SetText(nv ? "开" : "关");
                btn.TextColor = nv ? Color.LightGreen : new Color(200, 120, 120);
            };
            btn.OnUpdate += _ =>
            {
                if (btn.IsMouseHovering && hint.Length > 0)
                    Main.instance.MouseText(hint);
            };
            row.Append(btn);
        }

        private void ClearDebuffs()
        {
            Player p = Main.LocalPlayer;
            if (p == null || !p.active) return;

            for (int i = p.buffType.Length - 1; i >= 0; i--)
            {
                int t = p.buffType[i];
                if (t > 0 && Main.debuff[t])
                    p.DelBuff(i);
            }
        }
    }
}

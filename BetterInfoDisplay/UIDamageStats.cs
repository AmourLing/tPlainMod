using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.GameContent.UI.Elements;

namespace BetterInfoDisplay
{
    /// <summary>
    /// 伤害详情页 (参考 ImproveGame PlayerStats 的分类方式, 按伤害类型分区):
    /// 近战 / 远程 / 魔法 / 召唤, 含攻速、穿甲、弹药加成、法力回复、消耗减免、仆从与哨兵数量
    /// </summary>
    internal class UIDamageStats : InfoPageBase
    {
        protected override void Build()
        {
            // ===== 近战 =====
            Section("【近战】");
            Line("伤害", p => Bonus((p.meleeDamage - 1) * 100f), BonusColor(p => (p.meleeDamage - 1) * 100f));
            Line("暴击率", p => $"{p.meleeCrit}%");
            Line("攻速", p => Bonus((p.meleeSpeed - 1) * 100f), BonusColor(p => (p.meleeSpeed - 1) * 100f));
            Line("穿甲", p => $"{p.meleeArmorPenetration}");

            // ===== 远程 =====
            Section("【远程】");
            Line("伤害", p => Bonus((p.rangedDamage - 1) * 100f), BonusColor(p => (p.rangedDamage - 1) * 100f));
            Line("暴击率", p => $"{p.rangedCrit}%");
            Line("箭矢加成", p => Bonus((p.arrowDamage - 1) * 100f), BonusColor(p => (p.arrowDamage - 1) * 100f));
            Line("子弹加成", p => Bonus((p.bulletDamage - 1) * 100f), BonusColor(p => (p.bulletDamage - 1) * 100f));

            // ===== 魔法 =====
            Section("【魔法】");
            Line("伤害", p => Bonus((p.magicDamage - 1) * 100f), BonusColor(p => (p.magicDamage - 1) * 100f));
            Line("暴击率", p => $"{p.magicCrit}%");
            Line("法力回复", p => $"{p.manaRegen / 2f}/s");
            Line("消耗减免", p => Bonus((p.manaCost - 1) * 100f),
                p => p.manaCost < 1f ? Color.LightGreen : p.manaCost > 1f ? new Color(255, 120, 120) : Color.White);

            // ===== 召唤 =====
            Section("【召唤】");
            Line("伤害", p => Bonus((p.minionDamage - 1) * 100f), BonusColor(p => (p.minionDamage - 1) * 100f));
            Line("仆从栏", p => $"{p.slotsMinions:F1} / {p.maxMinions}");
            Line("哨兵", p => $"{CountSentries(p)} / {p.maxTurrets}",
                p => CountSentries(p) >= p.maxTurrets ? Color.LightGreen : Color.White);

            var hint = new UIText("加成着色: 绿=增益 红=减益; 哨兵=场上可清除炮台数", 0.8f);
            hint.TextColor = Color.Gray;
            Stack.Append(hint);
        }

        private static string Bonus(float v)
        {
            if (v > 0.01f) return $"+{v:F0}%";
            if (v < -0.01f) return $"{v:F0}%";
            return "0%";
        }

        /// <summary>
        /// 当前场上属于玩家的哨兵数 (同 ImproveGame: 统计 WipableTurret 弹射物)
        /// </summary>
        private static int CountSentries(Player p)
        {
            if (p == null) return 0;
            int count = 0;
            for (int i = 0; i < Main.projectile.Length; i++)
            {
                Projectile proj = Main.projectile[i];
                if (proj.active && proj.owner == p.whoAmI && proj.WipableTurret)
                    count++;
            }
            return count;
        }
    }
}

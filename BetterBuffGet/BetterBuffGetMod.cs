using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using QuickSetting;
using System;
using System.Collections.Generic;
using tContentPatch;
using Terraria;
using Terraria.ID;
using ReLogic.Content;

namespace BetterBuffGet
{
    public class BetterBuffGetMod : PatchMain
    {
        private static bool _keyPressed = false;
        private static bool _enabled = true;
        private static string _toggleKey = null;

        /// <summary>是否自动持续获得增益 (每 10s 刷新一次), 默认关闭</summary>
        public static bool AutoApply = false;
        private static int _autoTimer = 0;
        private const int AutoInterval = 600; // 10s @60fps
        private const int AutoMinFrames = 720; // 补 buff 阈值: 剩余 <12s 才补 (留余量)

        public override void Initialize()
        {
            if (Main.dedServ) return;

            var icon = Main.Assets.Request<Texture2D>("Images/Item_2347", AssetRequestMode.ImmediateLoad).Value;
            QuickSetting.QuickSetting.QuickSetting.AddItem(icon, "增益获取", new UIBuffList());
        }

        public override void UpdatePrefix(GameTime gameTime)
        {
            if (Main.gameMenu || Main.myPlayer == -1) return;

            // 分帧扫描增益时长数据库, 完成前不提供快捷上增益
            BuffDatabase.Update();
            if (!BuffDatabase.Ready) return;

            // 聊天输入/打开聊天时误触
            if (Main.drawingPlayerChat) return;

            // 自动持续获得: 每 10s 检查一次, 仅补剩余时间不足的增益
            // (避免每次无脑重刷触发药水音效/副作用)
            if (AutoApply && _enabled)
            {
                _autoTimer++;
                if (_autoTimer >= AutoInterval)
                {
                    _autoTimer = 0;
                    AutoTopUpBuffs();
                }
            }

            if (Main.keyState.IsKeyDown(Keys.B) && !_keyPressed)
            {
                _keyPressed = true;
                if (_enabled)
                    ApplyBuffs();
            }
            else if (Main.keyState.IsKeyUp(Keys.B))
            {
                _keyPressed = false;
            }
        }

        private void ApplyBuffs()
        {
            Player player = Main.LocalPlayer;
            if (player == null || player.dead) return;

            var selected = BuffSetting.CurrentSelectedBuffs;
            if (selected == null || selected.Count == 0) return;

            int applied = 0;
            foreach (var kv in selected)
            {
                if (!kv.Value) continue;
                int buffId = kv.Key;
                if (buffId > 0 && buffId < BuffID.Count)
                {
                    // 最少 2 分钟 (7200 帧), 防止部分短时增益消失太快
                    int duration = Math.Max(BuffDatabase.GetDuration(buffId), 7200);
                    player.AddBuff(buffId, duration);
                    applied++;
                }
            }

            if (applied > 0 && !AutoApply)
            { }
        }

        /// <summary>
        /// 自动补续: 仅补剩余时间 <12s 的增益, 其余不重刷
        /// </summary>
        private void AutoTopUpBuffs()
        {
            Player player = Main.LocalPlayer;
            if (player == null || player.dead) return;

            var selected = BuffSetting.CurrentSelectedBuffs;
            if (selected == null || selected.Count == 0) return;

            int added = 0;
            int topped = 0;

            foreach (var kv in selected)
            {
                if (!kv.Value) continue;
                int buffId = kv.Key;
                if (buffId <= 0 || buffId >= BuffID.Count) continue;

                int dur = Math.Max(BuffDatabase.GetDuration(buffId), 7200);
                int idx = player.FindBuffIndex(buffId);
                if (idx < 0)
                {
                    player.AddBuff(buffId, dur);
                    added++;
                }
                else if (player.buffTime[idx] < AutoMinFrames)
                {
                    player.buffTime[idx] = dur;
                    topped++;
                }
            }

            if (added > 0 || topped > 0)
            { }
        }

        public static void SetToggleKey(string key)
        {
            if (_toggleKey == key) return;
            if (_toggleKey != null)
                ListenInput.DelListenInput(_toggleKey, OnToggleKeyPress);
            _toggleKey = key;
            if (_toggleKey != null)
                ListenInput.AddListenInput(_toggleKey, OnToggleKeyPress);
        }

        private static void OnToggleKeyPress(bool firstPress)
        {
            if (firstPress)
            {
                _enabled = !_enabled;
            }
        }
    }
}

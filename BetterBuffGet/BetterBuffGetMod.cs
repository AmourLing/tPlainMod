using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using QuickSetting; // 如果 ListenInput 在 QuickSetting 命名空间下
using System;
using tContentPatch;
using Terraria;
using Terraria.ID;
using System.Collections.Generic;
using ReLogic.Content;

namespace BetterBuffGet
{
    public class BetterBuffGetMod : PatchMain
    {
        private static bool _keyPressed = false;
        private static bool _enabled = true;
        private static string _toggleKey = null;

        public override void Initialize()
        {
            if (Main.dedServ) return;

            var icon = Main.Assets.Request<Texture2D>("Images/Item_2347", AssetRequestMode.ImmediateLoad).Value;
            QuickSetting.QuickSetting.QuickSetting.AddItem(icon, "增益获取", new UIBuffList());
        }

        public override void UpdatePrefix(GameTime gameTime)
        {
            if (Main.gameMenu || Main.myPlayer == -1) return;

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

            var selectedDict = BuffSetting.CurrentSelectedBuffs;
            if (selectedDict == null) return;

            foreach (var kv in selectedDict)
            {
                if (!kv.Value) continue;
                int buffId = kv.Key;
                if (buffId > 0 && buffId < BuffID.Count)
                {
                    int duration = GetDefaultBuffDuration(buffId);
                    player.AddBuff(buffId, duration);
                }
            }
        }

        private static Dictionary<int, int> _defaultBuffDurations;

        private int GetDefaultBuffDuration(int buffId)
        {
            if (_defaultBuffDurations == null)
            {
                _defaultBuffDurations = new Dictionary<int, int>();

                for (int type = 1; type < ItemID.Count; type++)
                {
                    Item item = new Item();
                    item.SetDefaults(type);

                    if (item.buffType > 0 && item.buffTime > 0)
                    {
                        int id = item.buffType;
                        int time = item.buffTime;
                        if (!_defaultBuffDurations.ContainsKey(id))
                            _defaultBuffDurations[id] = time;
                        else if (time > _defaultBuffDurations[id])
                            _defaultBuffDurations[id] = time;
                    }
                }
            }

            if (_defaultBuffDurations.TryGetValue(buffId, out int duration))
                return duration;

            if (Main.debuff[buffId])
                return 60;
            else
                return 300;
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
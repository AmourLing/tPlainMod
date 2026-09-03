using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using tContentPatch;
using Terraria;
using Terraria.UI;

namespace RecipeBrowser
{
    public class RecipeBrowserMod : PatchMain
    {
        private static UIRecipeBrowser _ui;
        private static UserInterface _userInterface;
        private static UIState _uiState;

        static RecipeBrowserMod()
        {
            _userInterface = new UserInterface();
            _uiState = new UIState();
            _userInterface.SetState(_uiState);
        }

        public override void Initialize()
        {
            if (Main.dedServ) return;
            _ui = new UIRecipeBrowser("配方浏览器", 530, 480);
        }

        public override void UpdateUIStatesPostfix(GameTime gameTime)
        {
            if (Main.gameMenu)
                _userInterface?.SetState(null);
            else
            {
                _userInterface?.SetState(_uiState);
                _userInterface?.Update(gameTime);
            }
        }

        public override void SetupDrawInterfaceLayersPostfix(List<GameInterfaceLayer> layers)
        {
            int index = layers.FindIndex(l => l.Name == "Vanilla: Inventory");
            if (index != -1)
            {
                layers.Insert(index, new LegacyGameInterfaceLayer(
                    "RecipeBrowser: UI",
                    () =>
                    {
                        // 任何异常都不能打断绘制流程, 否则原版光标阶段被跳过 (鼠标消失)
                        try
                        {
                            _userInterface?.Draw(Main.spriteBatch, Main.gameTimeCache);
                        }
                        catch (Exception ex)
                        {
                            ErrorLog.Write(ex, "Draw");
                        }
                        return true;
                    },
                    InterfaceScaleType.UI));
            }
        }

        public static void ToggleUI()
        {
            if (_ui == null) return;
            if (_ui.IsOpen)
                _ui.Close();
            else
                _ui.Open(_uiState);
        }

        // ===== 快捷键开关 (默认键盘 2, KeyBindSetting 可配置) =====
        private static string _toggleKey;

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
            if (firstPress && !Main.gameMenu && !Main.drawingPlayerChat)
                ToggleUI();
        }
    }
}

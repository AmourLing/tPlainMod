using Microsoft.Xna.Framework;
using System.Collections.Generic;
using tContentPatch;
using tContentPatch.Content.UI;
using Terraria;
using Terraria.UI;

namespace BetterPrefix
{
    public class BetterPrefixMod : PatchMain
    {
        private static UIBetterPrefix _ui;
        private static UserInterface _userInterface;
        private static UIState _uiState;

        static BetterPrefixMod()
        {
            _userInterface = new UserInterface();
            _uiState = new UIState();
            _userInterface.SetState(_uiState);
        }

        public override void Initialize()
        {
            if (Main.dedServ) return;
            _ui = new UIBetterPrefix("前缀编辑器", 500, 430);
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
                    "BetterPrefix: UI",
                    () =>
                    {
                        _userInterface?.Draw(Main.spriteBatch, Main.gameTimeCache);
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
    }
}
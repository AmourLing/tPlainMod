using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using tContentPatch.Content.UI;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace BetterInfoDisplay
{
    internal class UIInfoDisplay : UIElement
    {
        private UIPanel _mainPanel;
        private UIScrollViewer _scrollViewer;
        private UIStackPanel _stackPanel;
        private Dictionary<string, EditableProperty> _editableProps;
        private UIText _readOnlyText;
        private static Texture2D _whitePixel;

        public UIInfoDisplay()
        {
            Width.Set(0, 1f);
            Height.Set(500, 0);

            _mainPanel = new UIPanel();
            _mainPanel.Width.Set(0, 1f);
            _mainPanel.Height.Set(0, 1f);
            _mainPanel.SetPadding(10);
            Append(_mainPanel);

            _scrollViewer = new UIScrollViewer();
            _scrollViewer.Width.Set(0, 1f);
            _scrollViewer.Height.Set(0, 1f);
            _mainPanel.Append(_scrollViewer);

            _stackPanel = new UIStackPanel();
            _stackPanel.Width.Set(0, 1f);
            _stackPanel.Horizontal = false;
            _stackPanel.ItemMargin = 8;
            _stackPanel.SetPadding(5);
            _stackPanel.IsAutoUpdateSize = true;
            _scrollViewer.SetChild(_stackPanel);

            if (_whitePixel == null)
            {
                _whitePixel = new Texture2D(Main.graphics.GraphicsDevice, 1, 1);
                _whitePixel.SetData(new[] { Color.White });
            }

            _editableProps = new Dictionary<string, EditableProperty>
            {
                ["无限飞行"] = new EditableProperty(
                    getter: () => BetterInfoDisplayMod.InfiniteFlightEnabled,
                    setter: (val) => BetterInfoDisplayMod.InfiniteFlightEnabled = (bool)val,
                    type: typeof(bool)
                )
            };

            BuildUI();
        }

        private void BuildUI()
        {
            _stackPanel.RemoveAllChildren();

            var readOnlyTitle = new UIText("【只读信息】", 1.2f);
            readOnlyTitle.TextColor = Color.Gold;
            _stackPanel.Append(readOnlyTitle);

            _readOnlyText = new UIText("");
            _readOnlyText.IsWrapped = true;
            _readOnlyText.Width.Set(0, 1f);
            _stackPanel.Append(_readOnlyText);

            var separator = new UIHorizontalSeparator(_whitePixel);
            separator.Height.Set(2, 0);
            _stackPanel.Append(separator);

            var editTitle = new UIText("【可编辑属性】", 1.2f);
            editTitle.TextColor = Color.Gold;
            _stackPanel.Append(editTitle);

            foreach (var kv in _editableProps)
            {
                var propPanel = new UIStackPanel();
                propPanel.Horizontal = true;
                propPanel.Width.Set(0, 1f);
                propPanel.Height.Set(30, 0);
                propPanel.ItemMargin = 10;
                _stackPanel.Append(propPanel);

                var label = new UIText(kv.Key);
                label.Width.Set(120, 0);
                label.VAlign = 0.5f;
                propPanel.Append(label);

                UIElement control = null;
                if (kv.Value.Type == typeof(bool))
                {
                    var btn = new UIButton1(kv.Value.GetValue<bool>() ? "开启" : "关闭");
                    btn.Width.Set(80, 0);
                    btn.Height.Set(24, 0);
                    btn.VAlign = 0.5f;
                    btn.OnLeftClick += (evt, el) =>
                    {
                        bool newVal = !kv.Value.GetValue<bool>();
                        kv.Value.SetValue(newVal);
                        btn.SetText(newVal ? "开启" : "关闭");
                    };
                    control = btn;
                }
                else
                {
                    var textBox = new UITextBox("");
                    textBox.Width.Set(100, 0);
                    textBox.Height.Set(24, 0);
                    textBox.VAlign = 0.5f;
                    textBox.OnTextChanged += (text) =>
                    {
                        if (kv.Value.Type == typeof(int))
                        {
                            if (int.TryParse(text, out int val))
                            {
                                val = Clamp(val, (int)kv.Value.Min, (int)kv.Value.Max);
                                kv.Value.SetValue(val);
                                textBox.SetText(val.ToString());
                            }
                            else
                            {
                                textBox.SetText(kv.Value.GetValue<int>().ToString());
                            }
                        }
                        else if (kv.Value.Type == typeof(float))
                        {
                            if (float.TryParse(text, out float val))
                            {
                                val = Clamp(val, kv.Value.Min, kv.Value.Max);
                                kv.Value.SetValue(val);
                                textBox.SetText(val.ToString(kv.Value.Format));
                            }
                            else
                            {
                                textBox.SetText(kv.Value.GetValue<float>().ToString(kv.Value.Format));
                            }
                        }
                    };
                    if (kv.Value.Type == typeof(int))
                        textBox.SetText(kv.Value.GetValue<int>().ToString());
                    else
                        textBox.SetText(kv.Value.GetValue<float>().ToString(kv.Value.Format));
                    control = textBox;
                }
                propPanel.Append(control);
            }

            var hint = new UIText("提示：修改后实时生效，关闭界面不会重置", 0.8f);
            hint.TextColor = Color.Gray;
            _stackPanel.Append(hint);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            UpdateReadOnlyInfo();
        }

        private string _lastReadOnlyText = "";

        private void UpdateReadOnlyInfo()
        {
            Player p = Main.LocalPlayer;
            if (p == null || !p.active)
            {
                if (_lastReadOnlyText != "未找到玩家")
                {
                    _readOnlyText?.SetText("未找到玩家");
                    _lastReadOnlyText = "未找到玩家";
                    RefreshLayout();
                }
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"生命: {p.statLife} / {p.statLifeMax2}");
            sb.AppendLine($"魔力: {p.statMana} / {p.statManaMax2}");
            sb.AppendLine($"防御: {p.statDefense}");
            sb.AppendLine($"近战伤害加成: {((p.meleeDamage - 1) * 100):F1}%");
            sb.AppendLine($"近战暴击率: {p.meleeCrit}%");
            sb.AppendLine($"魔法伤害加成: {((p.magicDamage - 1) * 100):F1}%");
            sb.AppendLine($"魔法暴击率: {p.magicCrit}%");
            sb.AppendLine($"远程伤害加成: {((p.rangedDamage - 1) * 100):F1}%");
            sb.AppendLine($"箭矢伤害加成: {((p.arrowDamage - 1) * 100):F1}%");
            sb.AppendLine($"子弹伤害加成: {((p.bulletDamage - 1) * 100):F1}%");
            sb.AppendLine($"远程暴击率: {p.rangedCrit}%");
            sb.AppendLine($"召唤伤害加成: {((p.minionDamage - 1) * 100):F1}%");
            sb.AppendLine($"仆从栏: {p.slotsMinions} / {p.maxMinions}");
            //sb.AppendLine($"哨兵栏: {p.numTurrets} / {p.maxTurrets}"); //Player中numTurrets不存在
            sb.AppendLine($"哨兵栏:  / {p.maxTurrets}");
            sb.AppendLine($"翅膀飞行时间: {p.wingTime} / {p.wingTimeMax}");
            sb.AppendLine($"重力反转: {(p.gravDir == -1 ? "是" : "否")}");
            sb.AppendLine($"无敌帧剩余: {p.immuneTime}");
            sb.AppendLine($"幸运: {p.luck}");
            sb.AppendLine($"伤害减免: {p.endurance * 100}%");
            sb.AppendLine($"生命再生: {p.lifeRegen}");
            sb.AppendLine($"移动速度: {p.moveSpeed * 100}%");
            sb.AppendLine($"永久增益(工匠面包): {(p.ateArtisanBread ? "✓" : "✗")}");
            sb.AppendLine($"永久增益(活力水晶): {(p.usedAegisCrystal ? "✓" : "✗")}");
            sb.AppendLine($"永久增益(神盾果): {(p.usedAegisFruit ? "✓" : "✗")}");
            sb.AppendLine($"永久增益(奥术水晶): {(p.usedArcaneCrystal ? "✓" : "✗")}");
            sb.AppendLine($"永久增益(仙馔密酒): {(p.usedAmbrosia ? "✓" : "✗")}");
            sb.AppendLine($"永久增益(粘性蠕虫): {(p.usedGummyWorm ? "✓" : "✗")}");
            sb.AppendLine($"永久增益(星系珍珠): {(p.usedGalaxyPearl ? "✓" : "✗")}");

            string newText = sb.ToString();
            if (_lastReadOnlyText != newText)
            {
                _readOnlyText?.SetText(newText);
                _lastReadOnlyText = newText;
                RefreshLayout();
            }
        }

        private void RefreshLayout()
        {
            _stackPanel.Recalculate();
            _scrollViewer.Recalculate();
        }

        private class EditableProperty
        {
            public Func<object> Getter { get; }
            public Action<object> Setter { get; }
            public Type Type { get; }
            public float Min { get; }
            public float Max { get; }
            public string Format { get; }

            public EditableProperty(Func<object> getter, Action<object> setter, Type type, float min = 0, float max = 1, string format = null)
            {
                Getter = getter;
                Setter = setter;
                Type = type;
                Min = min;
                Max = max;
                Format = format ?? (type == typeof(float) ? "F2" : "");
            }

            public T GetValue<T>() => (T)Getter();

            public void SetValue(object val) => Setter(val);
        }

        private class UIHorizontalSeparator : UIElement
        {
            private Texture2D _pixel;

            public UIHorizontalSeparator(Texture2D pixel)
            {
                _pixel = pixel;
                Width.Set(0, 1f);
            }

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                var dim = GetDimensions();
                spriteBatch.Draw(_pixel, new Rectangle((int)dim.X, (int)(dim.Y + dim.Height / 2), (int)dim.Width, 2), Color.Gray);
            }
        }

        private static T Clamp<T>(T value, T min, T max) where T : IComparable<T>
        {
            if (value.CompareTo(min) < 0) return min;
            if (value.CompareTo(max) > 0) return max;
            return value;
        }
    }
}
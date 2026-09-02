using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using tContentPatch.Content.UI;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace BetterInfoDisplay
{
    /// <summary>
    /// 信息页基类: 分区 + 逐行缓存刷新 (只在文本变化时 SetText)
    /// </summary>
    internal abstract class InfoPageBase : UIElement
    {
        private readonly List<InfoLine> _lines = new List<InfoLine>();
        private static Texture2D _whitePixel;

        /// <summary>子类追加自定义控件的容器</summary>
        protected UIStackPanel Stack { get; private set; }

        protected InfoPageBase()
        {
            Width.Set(0, 1f);
            Height.Set(520, 0);

            UIPanel panel = new UIPanel();
            panel.Width.Set(0, 1f);
            panel.Height.Set(0, 1f);
            panel.SetPadding(8);
            Append(panel);

            if (_whitePixel == null)
            {
                _whitePixel = new Texture2D(Main.graphics.GraphicsDevice, 1, 1);
                _whitePixel.SetData(new[] { Color.White });
            }

            UIScrollViewer scrollViewer = new UIScrollViewer();
            scrollViewer.Width.Set(0, 1f);
            scrollViewer.Height.Set(0, 1f);
            panel.Append(scrollViewer);

            Stack = new UIStackPanel();
            Stack.Width.Set(0, 1f);
            Stack.Horizontal = false;
            Stack.ItemMargin = 3;
            Stack.IsAutoUpdateSize = true;
            scrollViewer.SetChild(Stack);

            Build();
        }

        /// <summary>子类构建页面内容</summary>
        protected abstract void Build();

        protected void Section(string title)
        {
            UIText t = new UIText(title, 1.1f);
            t.TextColor = Color.Gold;
            Stack.Append(t);
        }

        protected void Line(string label, Func<Player, string> value, Func<Player, Color> color = null)
        {
            UIText ui = new UIText("", 0.85f);
            Stack.Append(ui);
            _lines.Add(new InfoLine { Text = p => $"{label}: {value(p)}", Ui = ui, Color = color });
        }

        protected void PermLine(string label, Func<Player, bool> has)
        {
            Line(label, p => has(p) ? "已获得 ✓" : "未获得 ✗",
                p => has(p) ? Color.LightGreen : Color.Gray);
        }

        /// <summary>加成类数值着色: 正=绿 负=红 零=白</summary>
        protected static Func<Player, Color> BonusColor(Func<Player, float> value)
        {
            return p =>
            {
                float d = value(p);
                if (d > 0.01f) return Color.LightGreen;
                if (d < -0.01f) return new Color(255, 120, 120);
                return Color.White;
            };
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            Player p = Main.LocalPlayer;
            bool ok = p != null && p.active;

            foreach (InfoLine line in _lines)
            {
                string text = ok ? line.Text(p) : "—";
                if (line.Last != text)
                {
                    line.Last = text;
                    line.Ui.SetText(text);
                }
                if (ok && line.Color != null)
                {
                    Color c = line.Color(p);
                    if (line.Ui.TextColor != c)
                        line.Ui.TextColor = c;
                }
            }
        }

        protected class InfoLine
        {
            public Func<Player, string> Text;
            public Func<Player, Color> Color;
            public UIText Ui;
            public string Last = "\0"; // 保证首帧必定刷新
        }
    }
}

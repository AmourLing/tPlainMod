using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using tContentPatch.Content.UI;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;

namespace RecipeBrowser
{
    /// <summary>
    /// 配方浏览器窗口 (参考 tML RecipeBrowser 的布局):
    /// 配方页 = 产物分类过滤 + 配方结果网格 + 底部所需物品槽
    /// 物品页 = 分类图标过滤 + 全物品网格 (可制作绿点) + 双击跳转配方
    /// </summary>
    internal class UIRecipeBrowser : UIWindow
    {
        // ===== 分类过滤 (基于原版创意物品分组 ItemGroup) =====
        private class FilterDef
        {
            public string Name;
            public Func<int, bool> Match;
            public int IconItem; // 代表性物品图标 (0 = 文本按钮)

            public FilterDef(string name, Func<int, bool> match, int iconItem = 0)
            {
                Name = name;
                Match = match;
                IconItem = iconItem;
            }
        }

        private static readonly int[] KnownGroups =
        {
            530, 540, 550, 560,
            500, 510, 520,
            600, 610, 620, 630,
            10000,
            120, 100, 150,
            50, 51, 52, 53, 54, 40, 94, 97,
            96
        };

        // 分类: 近战/远程/魔法/召唤/工具/防具/饰品/材料/可放置/消耗品/弹药/其他
        private static readonly FilterDef[] Filters =
        {
            new FilterDef("近战",   g => g == 530),
            new FilterDef("远程",   g => g == 540),
            new FilterDef("魔法",   g => g == 550),
            new FilterDef("召唤",   g => g == 560),
            new FilterDef("工具",   g => g == 500 || g == 510 || g == 520),
            new FilterDef("防具",   g => g == 600 || g == 610 || g == 620),
            new FilterDef("饰品",   g => g == 630),
            new FilterDef("材料",   g => g == 10000),
            new FilterDef("可放置", g => g == 120 || g == 100 || g == 150),
            new FilterDef("消耗品", g => g == 50 || g == 51 || g == 52 || g == 53 || g == 54 || g == 40 || g == 94 || g == 97),
            new FilterDef("弹药",   g => g == 96),
            new FilterDef("其他",   g => Array.IndexOf(KnownGroups, g) < 0),
        };

        private const int PageSize = 200;

        private static readonly Color RecipeTabColor = new Color(73, 94, 171);
        // 标签页选中态: 绿色背景 + 白字 (参考 CraftUI.color); 未选中: 蓝色背景 + 灰字
        private static readonly Color TabActiveColor = new Color(90, 158, 57);
        private static readonly Color TabInactiveColor = new Color(73, 94, 171);

        private int _tab; // 0=配方 1=物品
        private bool _built;
        private UIButton1 _tabRecipe;
        private UIButton1 _tabItems;
        private UIText _statusText;
        private UIText _pageText;
        private UIButton1 _pagePrev;
        private UIButton1 _pageNext;

        // ===== 配方页状态 =====
        private UITextBox _recipeSearch;
        private UIWrapPanel _recipeCatRow;
        private UIScrollViewer _recipeGridScroll;
        private UIWrapPanel _recipeGridWrap;
        private UIGuideSlot _guideSlot;
        private UIText _recipeInfoText;
        private UIWrapPanel _needRow;
        private UIText _needMarkText;

        private List<int> _visibleRecipes = new List<int>();
        private int _selectedRecipeIndex = -1;
        private int _queryItemType = -1;   // 只看产物为该物品的配方 (-1 全部)
        private int _recipeCatIndex = -1;     // 只看该分类产物的配方 (-1 全部)
        private string _recipeSearchText = "";
        private int _recipePage;
        private int _lastRecipeClickIndex = -1;
        private int _lastRecipeClickTick;
        // ===== 物品页状态 =====
        private UITextBox _itemSearch;
        private UIWrapPanel _catRow;
        private UIScrollViewer _itemGridScroll;
        private UIWrapPanel _itemGridWrap;
        private UIText _itemInfoText;
        private bool _onlyCraftable;

        private readonly Dictionary<int, UIItemCell> _itemCells = new Dictionary<int, UIItemCell>();
        /// <summary>玩家背包各物品持有数量 (PopulateItemGrid 时刷新, 物品格绘制用)</summary>
        internal static readonly Dictionary<int, int> _playerStacks = new Dictionary<int, int>();
        private List<ItemEntry> _visibleItems = new List<ItemEntry>();
        private int _selectedItemType = -1;
        private string _itemSearchText = "";
        private int _itemCatIndex = -1; // -1 = 全部
        private int _itemPage;
        private int _lastClickType = -1;
        private int _lastClickTick;
        private int _craftRefreshTimer;

        public UIRecipeBrowser(string title, int width, int height) : base(title, width, height)
        {
            // 不用 HAlign/VAlign 居中: 框架拖拽限制按 Left∈[0, 屏宽-宽] 计算,
            // 居中偏移叠加会导致只能拖动屏幕中间一小段。改为打开时绝对定位。
            MinWidth.Pixels = 440;
            MinHeight.Pixels = 340;

            // 标签页按钮 - 模仿原版彩色标签 (80×26, Left 10/85, 75px spacing)
            _tabRecipe = new UIButton1("配方", 0.9f);
            _tabRecipe.Width.Set(80, 0);
            _tabRecipe.Height.Set(26, 0);
            _tabRecipe.Left.Set(10, 0);
            _tabRecipe.Top.Set(2, 0);
            _tabRecipe.OnLeftClick += (evt, element) => SwitchTab(0);
            Child.Append(_tabRecipe);

            _tabItems = new UIButton1("物品", 0.9f);
            _tabItems.Width.Set(80, 0);
            _tabItems.Height.Set(26, 0);
            _tabItems.Left.Set(85, 0);
            _tabItems.Top.Set(2, 0);
            _tabItems.OnLeftClick += (evt, element) => SwitchTab(1);
            Child.Append(_tabItems);
            UpdateTabColors();

            // ===== 配方页 =====
            _recipeRoot = new UIPanel();
            _recipeRoot.Width.Set(0, 1f);
            // Top=26 且 Height=-82 → 底边 = 26 + (H-82) = H-56, 真正留出底部 56px 给底部栏
            _recipeRoot.Height.Set(-82, 1f);
            _recipeRoot.Top.Set(26, 0);
            _recipeRoot.SetPadding(8);
            Child.Append(_recipeRoot);

            // 查询槽
            _guideSlot = new UIGuideSlot();
            _guideSlot.Top.Set(0, 0);
            _guideSlot.Left.Set(0, 0);
            _guideSlot.OnItemChanged = OnGuideItemChanged;
            _recipeRoot.Append(_guideSlot);

            _recipeInfoText = new UIText("选择一个配方查看所需物品", 0.75f);
            _recipeInfoText.Top.Set(0, 0);
            _recipeInfoText.Left.Set(42, 0);
            _recipeInfoText.TextColor = Color.White;
            _recipeRoot.Append(_recipeInfoText);

            _recipeSearch = new UITextBox("搜索名称/ID");
            _recipeSearch.Width.Set(150, 0);
            _recipeSearch.Height.Set(25, 0);
            _recipeSearch.Top.Set(0, 0);
            _recipeSearch.HAlign = 1f;
            _recipeSearch.OnTextChanged += s =>
            {
                _recipeSearchText = (s ?? "").Trim();
                _recipePage = 0;
                PopulateRecipeGrid();
            };
            _recipeRoot.Append(_recipeSearch);

            // 分类图标条 (参考版本: 独立圆角面板 + 纯图标)
            var recipeCatStrip = new UIPanel();
            recipeCatStrip.Width.Set(-8, 1f);
            recipeCatStrip.Height.Set(38, 0);
            recipeCatStrip.Top.Set(50, 0);
            recipeCatStrip.Left.Set(0, 0);
            recipeCatStrip.SetPadding(4);
            _recipeRoot.Append(recipeCatStrip);

            _recipeCatRow = new UIWrapPanel();
            _recipeCatRow.Width.Set(0, 1f);
            _recipeCatRow.Height.Set(28, 0);
            _recipeCatRow.Top.Set(0, 0);
            _recipeCatRow.Left.Set(0, 0);
            _recipeCatRow.ItemMargin = 2;
            _recipeCatRow.OverflowHidden = true;
            recipeCatStrip.Append(_recipeCatRow);

            _recipeGridScroll = new UIScrollViewer();
            _recipeGridScroll.Width.Set(-8, 1f);
            _recipeGridScroll.Height.Set(-(98 + 60 + 12), 1f);
            _recipeGridScroll.Top.Set(98, 0);
            _recipeRoot.Append(_recipeGridScroll);

            _recipeGridWrap = new UIWrapPanel();
            _recipeGridWrap.Width.Set(-12, 1f);
            _recipeGridWrap.Top.Set(6, 0);
            _recipeGridWrap.Left.Set(6, 0);
            _recipeGridWrap.ItemMargin = 3;
            _recipeGridScroll.SetChild(_recipeGridWrap);

            // 底部: 所需物品
            var needRoot = new UIPanel();
            needRoot.Width.Set(-12, 1f);
            needRoot.Height.Set(60, 0);
            needRoot.VAlign = 1f;
            needRoot.Left.Set(6, 0);
            _recipeRoot.Append(needRoot);

            var needLabel = new UIText("所需物品:", 0.75f);
            needLabel.Top.Set(0, 0);
            needRoot.Append(needLabel);

            _needRow = new UIWrapPanel();
            _needRow.Width.Set(0, 1f);
            _needRow.Height.Set(40, 0);
            _needRow.Top.Set(18, 0);
            _needRow.ItemMargin = 3;
            _needRow.OverflowHidden = true;
            needRoot.Append(_needRow);

            _needMarkText = new UIText("", 0.7f);
            _needMarkText.Top.Set(-2, 0);
            _needMarkText.HAlign = 1f;
            _needMarkText.TextColor = Color.Gray;
            needRoot.Append(_needMarkText);

            // ===== 物品页 =====
            _itemRoot = new UIPanel();
            _itemRoot.Width.Set(0, 1f);
            _itemRoot.Height.Set(-82, 1f);     // Top=26 + Height=-82 → 底边 = H-56
            _itemRoot.Top.Set(26, 0);
            _itemRoot.SetPadding(8);
            // 不在此处 Append, 由 SwitchTab 控制 (两个页根同时挂会叠在一起)

            var craftToggle = new UIButton1("仅可制作: 关", 0.7f);
            craftToggle.Width.Set(100, 0);
            craftToggle.Height.Set(22, 0);
            craftToggle.Top.Set(0, 0);
            craftToggle.Left.Set(0, 0);
            craftToggle.OnLeftClick += (evt, element) =>
            {
                _onlyCraftable = !_onlyCraftable;
                craftToggle.SetText("仅可制作: " + (_onlyCraftable ? "开" : "关"));
                _itemPage = 0;
                PopulateItemGrid();
            };
            _itemRoot.Append(craftToggle);

            _itemSearch = new UITextBox("搜索名称/ID");
            _itemSearch.Width.Set(150, 0);
            _itemSearch.Height.Set(25, 0);
            _itemSearch.Top.Set(0, 0);
            _itemSearch.HAlign = 1f;
            _itemSearch.OnTextChanged += s =>
            {
                _itemSearchText = (s ?? "").Trim();
                _itemPage = 0;
                PopulateItemGrid();
            };
            _itemRoot.Append(_itemSearch);

            // 分类图标条 (参考版本: 独立圆角面板 + 纯图标)
            var catStrip = new UIPanel();
            catStrip.Width.Set(-8, 1f);
            catStrip.Height.Set(38, 0);
            catStrip.Top.Set(36, 0);
            catStrip.Left.Set(0, 0);
            catStrip.SetPadding(4);
            _itemRoot.Append(catStrip);

            _catRow = new UIWrapPanel();
            _catRow.Width.Set(0, 1f);
            _catRow.Height.Set(28, 0);
            _catRow.Top.Set(0, 0);
            _catRow.Left.Set(0, 0);
            _catRow.ItemMargin = 2;
            _catRow.OverflowHidden = true;
            catStrip.Append(_catRow);

            _itemGridScroll = new UIScrollViewer();
            _itemGridScroll.Width.Set(-8, 1f);
            _itemGridScroll.Height.Set(-(84 + 34 + 12), 1f);
            _itemGridScroll.Top.Set(84, 0);
            _itemRoot.Append(_itemGridScroll);

            _itemGridWrap = new UIWrapPanel();
            _itemGridWrap.Width.Set(-12, 1f);
            _itemGridWrap.Top.Set(6, 0);
            _itemGridWrap.Left.Set(6, 0);
            _itemGridWrap.ItemMargin = 3;
            _itemGridScroll.SetChild(_itemGridWrap);

            var itemBottom = new UIPanel();
            itemBottom.Width.Set(-12, 1f);
            itemBottom.Height.Set(32, 0);
            itemBottom.VAlign = 1f;
            itemBottom.Left.Set(6, 0);
            _itemRoot.Append(itemBottom);

            _itemInfoText = new UIText("单击选择, 双击查看配方", 0.75f);
            _itemInfoText.Top.Set(2, 0);
            _itemInfoText.TextColor = Color.Gray;
            itemBottom.Append(_itemInfoText);

            // 底部栏 (两个标签页共用): 状态文字 + 翻页按钮
            // VAlign=1 + Top=-10 → 底边距窗口底 10px, 位于 [H-40, H-10], 页面根面板底边在 H-56, 无重叠
            var bottomBar = new UIPanel();
            bottomBar.Width.Set(-16, 1f);
            bottomBar.Height.Set(30, 0);
            bottomBar.VAlign = 1f;
            bottomBar.Top.Set(-10, 0);
            bottomBar.Left.Set(8, 0);
            Child.Append(bottomBar);

            _statusText = new UIText("正在构建物品库...", 0.7f);
            _statusText.Left.Set(6, 0);
            _statusText.VAlign = 0.5f;
            _statusText.TextColor = Color.LightGray;
            bottomBar.Append(_statusText);

            _pagePrev = new UIButton1("◀", 0.7f);
            _pagePrev.Width.Set(26, 0);
            _pagePrev.Height.Set(22, 0);
            _pagePrev.HAlign = 1f;
            _pagePrev.VAlign = 0.5f;
            _pagePrev.Left.Set(-90, 0);
            _pagePrev.EnableColorBack = RecipeTabColor;
            _pagePrev.MouseOverColorBack = RecipeTabColor * 1.2f;
            _pagePrev.TextColor = Color.White;
            _pagePrev.OnLeftClick += (evt, element) =>
            {
                if (_tab == 0) SetPage(_recipePage - 1);
                else SetItemPage(_itemPage - 1);
            };
            bottomBar.Append(_pagePrev);

            _pageText = new UIText("", 0.7f);
            _pageText.HAlign = 1f;
            _pageText.VAlign = 0.5f;
            _pageText.Left.Set(-62, 0);
            _pageText.TextColor = Color.White;
            bottomBar.Append(_pageText);

            _pageNext = new UIButton1("▶", 0.7f);
            _pageNext.Width.Set(26, 0);
            _pageNext.Height.Set(22, 0);
            _pageNext.HAlign = 1f;
            _pageNext.VAlign = 0.5f;
            _pageNext.Left.Set(-24, 0);
            _pageNext.EnableColorBack = RecipeTabColor;
            _pageNext.MouseOverColorBack = RecipeTabColor * 1.2f;
            _pageNext.TextColor = Color.White;
            _pageNext.OnLeftClick += (evt, element) =>
            {
                if (_tab == 0) SetPage(_recipePage + 1);
                else SetItemPage(_itemPage + 1);
            };
            bottomBar.Append(_pageNext);

            // 每帧刷新换行容器高度
            OnUpdate += _ =>
            {
                _recipeGridWrap.UpdateContainer_Height();
                _itemGridWrap.UpdateContainer_Height();
            };
        }

        private UIPanel _recipeRoot;
        private UIPanel _itemRoot;

        private void SwitchTab(int tab)
        {
            _tab = tab;
            UpdateTabColors();
            if (_tab == 0)
            {
                Child.RemoveChild(_itemRoot);
                Child.Append(_recipeRoot);
                if (_built) RefreshRecipeStatus();   // 底部栏状态与当前页保持一致
            }
            else
            {
                Child.RemoveChild(_recipeRoot);
                Child.Append(_itemRoot);
                if (_built) RefreshItemStatus();
            }
        }

        /// <summary>当前选中的标签页: 绿色背景 + 白字; 未选中: 蓝色背景 + 白字 (参考版本标签均为白字)</summary>
        private void UpdateTabColors()
        {
            bool r = _tab == 0, i = _tab == 1;
            _tabRecipe.EnableColorBack = r ? TabActiveColor : TabInactiveColor;
            _tabRecipe.MouseOverColorBack = (r ? TabActiveColor : TabInactiveColor) * 1.15f;
            _tabRecipe.TextColor = Color.White;
            _tabItems.EnableColorBack = i ? TabActiveColor : TabInactiveColor;
            _tabItems.MouseOverColorBack = (i ? TabActiveColor : TabInactiveColor) * 1.15f;
            _tabItems.TextColor = Color.White;
        }

        // ===== 构建: 配方分类过滤按钮 (与物品页同一套分类, 按产物类别) =====

        private void BuildRecipeCategoryButtons()
        {
            _recipeCatRow.RemoveAllChildren();

            var allBtn = new UIButton1("全部", 0.8f);
            allBtn.Width.Set(44, 0);
            allBtn.Height.Set(26, 0);
            allBtn.MarginRight = 3;
            allBtn.OnLeftClick += (evt, element) =>
            {
                _recipeCatIndex = -1;
                HighlightRecipeCategories();
                _recipePage = 0;
                PopulateRecipeGrid();
            };
            _recipeCatRow.Append(allBtn);

            foreach (FilterDef f in Filters)
            {
                int idx = Array.IndexOf(Filters, f);
                int iconType = FindCategoryIcon(f);
                Texture2D catTex = iconType > 0 ? RecipeDatabase.GetItemTexture(iconType) : null;

                UIButton1 textBtn = null;
                IconButton iconBtn = null;
                if (catTex != null)
                {
                    iconBtn = new IconButton(catTex, f.Name, idx);
                    iconBtn.Tag2 = idx;
                    iconBtn.OnClick = _ =>
                    {
                        _recipeCatIndex = idx;
                        HighlightRecipeCategories();
                        _recipePage = 0;
                        PopulateRecipeGrid();
                    };
                }
                else
                {
                    textBtn = new UIButton1(f.Name, 0.8f);
                    textBtn.Width.Set(48, 0);
                    textBtn.Height.Set(26, 0);
                    textBtn.MarginRight = 2;
                    textBtn.OnLeftClick += (evt, element) =>
                    {
                        _recipeCatIndex = idx;
                        HighlightRecipeCategories();
                        _recipePage = 0;
                        PopulateRecipeGrid();
                    };
                }

                if (iconBtn != null) _recipeCatRow.Append(iconBtn);
                else _recipeCatRow.Append(textBtn);
            }
            HighlightRecipeCategories();
        }

        private void HighlightRecipeCategories()
        {
            int i = 0;
            foreach (UIElement e in _recipeCatRow.Children)
            {
                UIButton1 b = e as UIButton1;
                if (b != null)
                {
                    bool activeText = _recipeCatIndex == -1 && i == 0;
                    b.EnableColorBack = activeText ? new Color(73, 94, 171) * 0.95f : new Color(73, 94, 171) * 0.5f;
                    i++;
                    continue;
                }
                IconButton ib = e as IconButton;
                if (ib == null) continue;
                ib.Active = ib.Tag2 == _recipeCatIndex;
            }
        }


        // ===== 构建: 物品分类按钮 =====

        private void BuildCategoryButtons()
        {
            _catRow.RemoveAllChildren();

            var allBtn = new UIButton1("全部", 0.8f);
            allBtn.Width.Set(44, 0);
            allBtn.Height.Set(26, 0);
            allBtn.MarginRight = 3;
            allBtn.OnLeftClick += (evt, element) =>
            {
                _itemCatIndex = -1;
                HighlightCategories();
                _itemPage = 0;
                PopulateItemGrid();
            };
            _catRow.Append(allBtn);

            foreach (FilterDef f in Filters)
            {
                int iconType = FindCategoryIcon(f);
                Texture2D catTex = iconType > 0 ? RecipeDatabase.GetItemTexture(iconType) : null;
                if (catTex == null)
                {
                    // 无代表物品时用文本按钮
                    var textBtn = new UIButton1(f.Name, 0.8f);
                    textBtn.Width.Set(48, 0);
                    textBtn.Height.Set(26, 0);
                    textBtn.MarginRight = 2;
                    int idx2 = Array.IndexOf(Filters, f);
                    textBtn.OnLeftClick += (evt, element) =>
                    {
                        _itemCatIndex = idx2;
                        HighlightCategories();
                        _itemPage = 0;
                        PopulateItemGrid();
                    };
                    _catRow.Append(textBtn);
                    continue;
                }

                int idx = Array.IndexOf(Filters, f);
                var btn = new IconButton(catTex, f.Name, idx);
                btn.Tag2 = idx;
                btn.OnClick = _ =>
                {
                    _itemCatIndex = idx;
                    HighlightCategories();
                    _itemPage = 0;
                    PopulateItemGrid();
                };
                _catRow.Append(btn);
            }
            HighlightCategories();
        }

        private void HighlightCategories()
        {
            int i = 0;
            foreach (UIElement e in _catRow.Children)
            {
                UIButton1 b = e as UIButton1;
                if (b != null)
                {
                    bool activeText = _itemCatIndex == -1 && i == 0;
                    b.EnableColorBack = activeText ? new Color(73, 94, 171) * 0.95f : new Color(73, 94, 171) * 0.5f;
                    i++;
                    continue;
                }
                IconButton ib = e as IconButton;
                if (ib == null) continue;
                int idx = ib.Tag2;
                ib.Active = idx == _itemCatIndex;
            }
        }

        /// <summary>找该分类下第一个物品作为图标</summary>
        private int FindCategoryIcon(FilterDef f)
        {
            foreach (ItemEntry e in RecipeDatabase.Items)
            {
                if (f.Match(e.Group)) return e.Type;
            }
            return 0;
        }

        // ===== 构建/刷新 =====

        private bool _placedCenter;

        public override void OnActivate()
        {
            base.OnActivate();
            // 首次打开时居中 (绝对坐标, 保证拖拽范围覆盖全屏)
            if (!_placedCenter && Main.screenWidth > 100)
            {
                _placedCenter = true;
                Left.Set(Math.Max(0, (Main.screenWidth - Width.Pixels) / 2), 0);
                Top.Set(Math.Max(0, (Main.screenHeight - Height.Pixels) / 2), 0);
            }
            EnsureBuilt();
        }

        private void EnsureBuilt()
        {
            if (_built) return;
            _built = true;
            try
            {
                RecipeDatabase.Build();
                RebuildItemCells();
                PopulateItemGrid();     // 先构建物品页 (状态文字会被配方页覆盖)
                BuildRecipeCategoryButtons();
                BuildCategoryButtons();
                PopulateRecipeGrid();   // 默认页最后刷新, 状态文字与当前页一致
                SwitchTab(_tab);
            }
            catch (Exception ex)
            {
                ErrorLog.Write(ex, "EnsureBuilt");
            }
        }

        private void RebuildItemCells()
        {
            _itemCells.Clear();
            foreach (ItemEntry e in RecipeDatabase.Items)
            {
                UIItemCell cell = new UIItemCell(e.Type, e.Name);
                cell.OnClick = ItemCellClicked;
                _itemCells[e.Type] = cell;
            }
        }

        private void ItemCellClicked(int type)
        {
            int now = Environment.TickCount;
            if (_lastClickType == type && now - _lastClickTick < 400)
            {
                // 双击: 跳到配方页, 只看该物品的配方
                _lastClickType = -1;
                _queryItemType = type;
                _selectedRecipeIndex = -1;
                _recipePage = 0;
                // 左上查询槽同步显示该物品, 空手单击槽位即可取消筛选
                Item query;
                if (ContentSamples.ItemsByType.TryGetValue(type, out query) && query != null)
                    _guideSlot.Item = query.Clone();
                else
                    _guideSlot.Item = new Item { type = type, stack = 1 };
                _guideSlot.Item.stack = 1;
                _recipeInfoText.SetText($"查询: {Lang.GetItemNameValue(type)}");
                SwitchTab(0);
                PopulateRecipeGrid();
                return;
            }

            _lastClickType = type;
            _lastClickTick = now;
            _selectedItemType = type;
            HighlightItemCells();

            ItemEntry e;
            string text;
            if (RecipeDatabase.ItemsByType.TryGetValue(type, out e))
            {
                List<int> recipes;
                RecipeDatabase.ByResult.TryGetValue(type, out recipes);
                text = $"{e.Name}: {BuildStatsText(e)}";
                if (recipes != null && recipes.Count > 0)
                    text += $" | 配方 {recipes.Count} 个, 双击查看";
                else
                    text += " | 无配方";
            }
            else
            {
                text = Lang.GetItemNameValue(type);
            }
            _itemInfoText.SetText(text);
        }

        private void HighlightItemCells()
        {
            foreach (KeyValuePair<int, UIItemCell> kv in _itemCells)
                kv.Value.Selected = kv.Key == _selectedItemType;
        }

        // ===== 配方页逻辑 =====

        private void PopulateRecipeGrid()
        {
            try
            {
                EnsureBuilt();

                // 刷新可制作集合 (配方格绿色背景判定)
                RecipeDatabase.RefreshCraftable(Main.LocalPlayer);

                string q = _recipeSearchText.ToLower();
                List<int> list = new List<int>();

                int n = Math.Min(Recipe.numRecipes, Main.recipe.Length);
                for (int i = 0; i < n; i++)
                {
                    Recipe r = Main.recipe[i];
                    if (r == null || r.createItem == null || r.createItem.IsAir) continue;
                    if (_queryItemType >= 0)
                    {
                        // 与 RecipeBrowser 一致: 该物品是产物 或 是材料 都算命中
                        bool isResult = r.createItem.type == _queryItemType;
                        bool isIngredient = false;
                        for (int k = 0; k < r.requiredItem.Length; k++)
                        {
                            Item ing = r.requiredItem[k];
                            if (ing != null && !ing.IsAir && ing.type == _queryItemType)
                            {
                                isIngredient = true;
                                break;
                            }
                        }
                        if (!isResult && !isIngredient) continue;
                    }
                    if (_recipeCatIndex >= 0)
                    {
                        ItemEntry ie;
                        if (!RecipeDatabase.ItemsByType.TryGetValue(r.createItem.type, out ie) || ie == null) continue;
                        if (!Filters[_recipeCatIndex].Match(ie.Group)) continue;
                    }

                    if (q.Length > 0)
                    {
                        string name = Lang.GetItemNameValue(r.createItem.type).ToLower();
                        if (q.All(char.IsDigit) && r.createItem.type.ToString().StartsWith(q)) { }
                        else if (name.Contains(q)) { }
                        else continue;
                    }
                    list.Add(i);
                }

                _visibleRecipes = list;
                _recipeGridWrap.RemoveAllChildren();

                int start = _recipePage * PageSize;
                int end = Math.Min(start + PageSize, _visibleRecipes.Count);
                for (int i = start; i < end; i++)
                {
                    int recipeIndex = _visibleRecipes[i];
                    _recipeGridWrap.Append(new UIRecipeCell(Main.recipe[recipeIndex], recipeIndex, _selectedRecipeIndex == recipeIndex, RecipeCellClicked));
                }
                _recipeGridWrap.UpdateContainer_Height();

                if (_visibleRecipes.Count == 0)
                {
                    var none = new UIText("没有找到该物品的配方 (可能非合成获得)", 0.85f);
                    none.TextColor = Color.Gray;
                    _recipeGridWrap.Append(none);
                }
                _recipeGridWrap.UpdateContainer_Height();

                int pages = Math.Max(1, (_visibleRecipes.Count + PageSize - 1) / PageSize);
                RefreshRecipeStatus();
                ScrollGridToTop(_recipeGridScroll);
                HighlightRecipeCells();

                // 自动选中第一个配方, 底部"所需物品"同步刷新 (不再残留上一个)
                if (_visibleRecipes.Count > 0)
                {
                    SelectRecipe(_visibleRecipes[0]);
                }
                else
                {
                    _selectedRecipeIndex = -1;
                    _needRow.RemoveAllChildren();
                    _needMarkText.SetText("该物品没有配方 (可能非合成获得)");
                    _needMarkText.TextColor = Color.Gray;
                }
            }
            catch (Exception ex)
            {
                ErrorLog.Write(ex, "PopulateRecipeGrid");
            }
        }

        private void SetPage(int page)
        {
            int pages = Math.Max(1, (_visibleRecipes.Count + PageSize - 1) / PageSize);
            int clamped = Math.Max(0, Math.Min(pages - 1, page));
            if (clamped == _recipePage) return;
            _recipePage = clamped;
            PopulateRecipeGrid();
        }

        private void RefreshRecipeStatus()
        {
            int pages = Math.Max(1, (_visibleRecipes.Count + PageSize - 1) / PageSize);
            string filter = _queryItemType >= 0 ? $" · 筛选: {Lang.GetItemNameValue(_queryItemType)}" : "";
            _statusText.SetText($"配方 {Recipe.numRecipes} · 显示 {_visibleRecipes.Count}{filter}");
            _pageText.SetText($"{_recipePage + 1}/{pages}");
        }

        /// <summary>配方格点击: 单击选中; 400ms 内双击同一格 = 原版合成栏跳转到该配方</summary>
        private void RecipeCellClicked(int recipeIndex)
        {
            int now = Environment.TickCount;
            if (_lastRecipeClickIndex == recipeIndex && now - _lastRecipeClickTick < 400)
            {
                _lastRecipeClickIndex = -1;
                FocusVanillaCrafting(recipeIndex);
                return;
            }
            _lastRecipeClickIndex = recipeIndex;
            _lastRecipeClickTick = now;
            SelectRecipe(recipeIndex);
        }

        /// <summary>
        /// 让原版合成栏滚动并高亮指定配方 (需该配方当前可合成, 即在 availableRecipe 列表中)
        /// </summary>
        private void FocusVanillaCrafting(int recipeIndex)
        {
            try
            {
                if (Main.gameMenu) return;
                Player p = Main.LocalPlayer;
                if (p == null || p.dead) return;

                Recipe.UpdateRecipeList(); // 按当前背包/环境刷新原版可合成列表

                if (!Main.playerInventory)
                    Main.playerInventory = true; // 合成栏随背包显示

                for (int i = 0; i < Main.numAvailableRecipes; i++)
                {
                    if (Main.availableRecipe[i] == recipeIndex)
                    {
                        Main.focusRecipe = i;
                        Main.craftingUI.VisuallyRepositionRecipes(i);
                        _recipeInfoText.SetText("已在合成栏中定位该配方");
                        return;
                    }
                }
                _recipeInfoText.SetText("该配方当前不可合成, 无法定位");
            }
            catch (Exception ex)
            {
                ErrorLog.Write(ex, "FocusVanillaCrafting");
            }
        }

        private void SelectRecipe(int recipeIndex)
        {
            _selectedRecipeIndex = recipeIndex;
            HighlightRecipeCells();
            BuildNeedRow();

            Recipe r = Main.recipe[recipeIndex];
            if (r != null && r.createItem != null)
            {
                _recipeInfoText.SetText($"查询: {Lang.GetItemNameValue(r.createItem.type)}");
            }
        }

        /// <summary>左上角槽位放入/取出物品 → 只看该物品的配方</summary>
        private void OnGuideItemChanged(Item item)
        {
            if (item != null && !item.IsAir)
            {
                _queryItemType = item.type;
                _recipeInfoText.SetText($"查询: {Lang.GetItemNameValue(item.type)}");
            }
            else
            {
                _queryItemType = -1;
                _recipeInfoText.SetText("选择一个配方查看所需物品");
            }
            _selectedRecipeIndex = -1;
            _recipePage = 0;
            PopulateRecipeGrid();
        }

        /// <summary>把手上 (光标) 的物品放回背包第一个空位, 成功返回 true</summary>
        private static bool ReturnMouseItemToInventory()
        {
            try
            {
                if (Main.mouseItem == null || Main.mouseItem.IsAir) return false;

                Item[] inv = Main.LocalPlayer.inventory;
                for (int i = 0; i < inv.Length; i++)
                {
                    if (inv[i] == null || inv[i].IsAir)
                    {
                        inv[i] = Main.mouseItem;
                        Main.mouseItem = new Item();
                        return true;
                    }
                }
                return false; // 背包满了, 留在手上
            }
            catch (Exception ex)
            {
                ErrorLog.Write(ex, "ReturnMouseItemToInventory");
                return false;
            }
        }

        private void HighlightRecipeCells()
        {
            // 配方格是每次分页重建的, 通过 Children 遍历刷新选中态
            foreach (UIElement e in _recipeGridWrap.Children)
            {
                UIRecipeCell c = e as UIRecipeCell;
                if (c != null) c.Selected = c.RecipeIndex == _selectedRecipeIndex;
            }
        }

        /// <summary>
        /// 底部所需物品槽: 缺少的材料数量标红、图标灰置; 点击 = 只看该材料的配方
        /// </summary>
        private void BuildNeedRow()
        {
            _needRow.RemoveAllChildren();

            Recipe r = _selectedRecipeIndex >= 0 && _selectedRecipeIndex < Main.recipe.Length
                ? Main.recipe[_selectedRecipeIndex] : null;
            if (r == null || r.createItem == null)
            {
                _needMarkText.SetText("");
                return;
            }

            Player p = Main.LocalPlayer;
            Dictionary<int, int> owned = RecipeDatabase.BuildOwnedDict(p);

            List<Recipe.RequiredItemEntry> entries = new List<Recipe.RequiredItemEntry>();
            r.GetIngredientsForOneCraft(p, entries);

            bool allEnough = entries.Count > 0;
            int times = int.MaxValue;

            foreach (Recipe.RequiredItemEntry e in entries)
            {
                try
                {
                    int have = RecipeDatabase.CountForEntry(e, owned);
                    bool enough = have >= e.stack;
                    if (!enough) allEnough = false;
                    times = Math.Min(times, enough ? have / e.stack : 0);

                    int iconType = RecipeDatabase.GetEntryIconType(e);
                    string dispName = RecipeDatabase.GetGroupText(e.itemIdOrRecipeGroup);
                    if (string.IsNullOrEmpty(dispName))
                    {
                        try { r.ProcessGroupsForText(e.itemIdOrRecipeGroup, out dispName); } catch { }
                    }
                    _needRow.Append(ItemSlot(iconType, e.stack, t =>
                    {
                        Item disp;
                        if (!ContentSamples.ItemsByType.TryGetValue(t, out disp) || disp == null)
                            disp = new Item { type = t };
                        _guideSlot.Item = disp.Clone();
                        _guideSlot.Item.stack = 1;

                        _queryItemType = t;
                        _recipeInfoText.SetText($"查询: {Lang.GetItemNameValue(t)}");
                        _selectedRecipeIndex = -1;
                        _recipePage = 0;
                        PopulateRecipeGrid();
                    }, !enough, have, dispName));
                }
                catch (Exception ex)
                {
                    ErrorLog.Write(ex, "BuildNeedRow slot");
                }
            }

            // 制作站/环境条件 (缺失对象会填入列表)
            List<string> missingEnv = new List<string>();
            bool envOk = r.PlayerMeetsEnvironmentConditions(p, missingEnv);

            string station = RecipeDatabase.StationText(r);
            if (!allEnough)
            {
                string extra = envOk ? "" : $", 还缺 {string.Join("、", missingEnv)}";
                _needMarkText.SetText($"{station}{extra} · 缺少材料, 请先集齐 (红色数量)");
                _needMarkText.TextColor = Color.Gray;
            }
            else if (!envOk)
            {
                _needMarkText.SetText($"材料齐全, 但缺少: {string.Join("、", missingEnv)}");
                _needMarkText.TextColor = Color.Orange;
            }
            else
            {
                _needMarkText.SetText($"{station} · 材料齐全, 可做 {times} 次");
                _needMarkText.TextColor = Color.LightGreen;
            }
        }

        // ===== 物品页逻辑 =====

        private void PopulateItemGrid()
        {
            try
            {
                EnsureBuilt();

                // 可制作集合定期刷新
                RecipeDatabase.RefreshCraftable(Main.LocalPlayer);

                // 刷新玩家持有数量 (用于物品格右下角数字)
                _playerStacks.Clear();
                foreach (Item inv in Main.LocalPlayer.inventory)
                {
                    if (inv == null || inv.IsAir || inv.stack <= 0) continue;
                    int cur;
                    _playerStacks.TryGetValue(inv.type, out cur);
                    _playerStacks[inv.type] = cur + inv.stack;
                }

                string q = _itemSearchText.ToLower();
                _visibleItems = RecipeDatabase.Items.Where(e =>
                {
                    if (_itemCatIndex >= 0 && !Filters[_itemCatIndex].Match(e.Group)) return false;
                    if (_onlyCraftable && !RecipeDatabase.CraftableResultTypes.Contains(e.Type)) return false;
                    if (q.Length == 0) return true;
                    if (q.All(char.IsDigit) && e.Type.ToString().StartsWith(q)) return true;
                    return e.Name.ToLower().Contains(q);
                }).ToList();

                _itemGridWrap.RemoveAllChildren();
                int start = _itemPage * PageSize;
                int end = Math.Min(start + PageSize, _visibleItems.Count);
                for (int i = start; i < end; i++)
                {
                    UIItemCell cell;
                    if (_itemCells.TryGetValue(_visibleItems[i].Type, out cell))
                        _itemGridWrap.Append(cell);
                }
                _itemGridWrap.UpdateContainer_Height();

                RefreshItemStatus();
                ScrollGridToTop(_itemGridScroll);
                HighlightItemCells();
            }
            catch (Exception ex)
            {
                ErrorLog.Write(ex, "PopulateItemGrid");
            }
        }

        private void RefreshItemStatus()
        {
            int pages = Math.Max(1, (_visibleItems.Count + PageSize - 1) / PageSize);
            _statusText.SetText($"物品 {RecipeDatabase.Items.Count} · 显示 {_visibleItems.Count}");
            _pageText.SetText($"{_itemPage + 1}/{pages}");
        }

        private void SetItemPage(int page)
        {
            int pages = Math.Max(1, (_visibleItems.Count + PageSize - 1) / PageSize);
            int clamped = Math.Max(0, Math.Min(pages - 1, page));
            if (clamped == _itemPage) return;
            _itemPage = clamped;
            PopulateItemGrid();
        }

        // ===== 共用 =====

        private void ScrollGridToTop(UIScrollViewer sv)
        {
            try
            {
                UIScrollbar sb = sv.Children.ElementAtOrDefault(1) as UIScrollbar;
                if (sb != null) sb.ViewPosition = 0;
            }
            catch { }
        }

        private string BuildStatsText(ItemEntry entry)
        {
            if (entry == null) return "";
            List<string> parts = new List<string>();
            if (entry.Damage > 0) parts.Add($"伤害 {entry.Damage}");
            if (entry.Defense > 0) parts.Add($"防御 {entry.Defense}");
            if (entry.Pick > 0) parts.Add($"镐力 {entry.Pick}%");
            if (entry.Axe > 0) parts.Add($"斧力 {entry.Axe}%");
            if (entry.Hammer > 0) parts.Add($"锤力 {entry.Hammer}%");
            parts.Add(RecipeDatabase.ValueText(entry.Value));
            parts.Add($"ID {entry.Type}");
            return string.Join(" · ", parts);
        }

        /// <summary>物品槽位: 图标居中, 数量居下; missing=true 时图标灰置+数量标红</summary>
        private UIElement ItemSlot(int type, int stack, Action<int> onClick, bool missing = false, int have = -1, string displayName = null)
        {
            return new UINeedSlot(type, stack, onClick, missing, have, displayName);
        }

        public override void Update(GameTime gameTime)
        {
            try
            {
                base.Update(gameTime);
                EnsureBuilt();

                // 每 2s 刷新可制作集合 (两个页面的绿色格子/需要行都依赖它)
                if (++_craftRefreshTimer >= 120)
                {
                    _craftRefreshTimer = 0;
                    int before = RecipeDatabase.CraftableResultTypes.Count;
                    RecipeDatabase.RefreshCraftable(Main.LocalPlayer);
                    if (_tab == 1 && _onlyCraftable && RecipeDatabase.CraftableResultTypes.Count != before)
                    {
                        PopulateItemGrid();
                    }
                    if (_tab == 0 && _selectedRecipeIndex >= 0)
                    {
                        BuildNeedRow(); // 刷新可合成标记与可做次数
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorLog.Write(ex, "Update");
            }
        }


        // ===== 通用图标按钮 (站点/分类) =====

        private class IconButton : UIElement
        {
            public Action<int> OnClick;     // 参数 = Tag
            public int Tag;                 // 站点 TileId
            public int Tag2;                // 分类下标
            public bool Active;
            private readonly Texture2D _tex;
            private readonly string _tooltip;
            private Texture2D _pixel;

            public IconButton(Texture2D tex, string tooltip, int tag)
            {
                _tex = tex;
                _tooltip = tooltip ?? "";
                Tag = tag;
                Width.Set(26, 0);
                Height.Set(26, 0);
                MarginRight = 3;
                MarginBottom = 3;

                OnLeftClick += (evt, element) => OnClick?.Invoke(Tag);
            }

            private Texture2D Pixel
            {
                get
                {
                    if (_pixel == null)
                    {
                        _pixel = new Texture2D(Main.graphics.GraphicsDevice, 1, 1);
                        _pixel.SetData(new[] { Color.White });
                    }
                    return _pixel;
                }
            }

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                try
                {
                    base.DrawSelf(spriteBatch);
                    CalculatedStyle dim = GetDimensions();
                    if (dim.Width <= 0) return;

                    // 图标保持宽高比居中绘制, 避免拉伸变形
                    Rectangle source = new Rectangle(0, 0, _tex.Width, _tex.Height);
                    float avail = dim.Width - 2;
                    float iconScale = Math.Min(1f, Math.Min(avail / source.Width, avail / source.Height));
                    Vector2 iconCenter = new Vector2(dim.X + dim.Width / 2f, dim.Y + dim.Height / 2f);
                    spriteBatch.Draw(_tex, iconCenter, source, Color.White, 0f, source.Size() / 2f, iconScale, SpriteEffects.None, 0f);

                    if (IsMouseHovering || Active)
                    {
                        Color bc = Active ? Color.Gold : Color.White;
                        int x = (int)dim.X, y = (int)dim.Y, w = (int)dim.Width, h = (int)dim.Height, t = 1;
                        spriteBatch.Draw(Pixel, new Rectangle(x, y, w, t), bc);
                        spriteBatch.Draw(Pixel, new Rectangle(x, y + h - t, w, t), bc);
                        spriteBatch.Draw(Pixel, new Rectangle(x, y, t, h), bc);
                        spriteBatch.Draw(Pixel, new Rectangle(x + w - t, y, t, h), bc);
                    }

                    if (IsMouseHovering && _tooltip.Length > 0)
                    {
                        Main.LocalPlayer.mouseInterface = true;
                        Main.instance.MouseText(_tooltip);
                    }
                }
                catch (Exception ex)
                {
                    ErrorLog.Write(ex, "IconButton.DrawSelf");
                }
            }
        }

        // ===== 配方格 (结果图标 + 数量) =====

        private class UIRecipeCell : UIElement
        {
            public readonly int RecipeIndex;
            private readonly int _itemType;
            private readonly int _stack;
            private readonly Action<int> _onSelect;
            private bool _selected;
            private Texture2D _pixel;
            private const float SlotScale = 0.75f;

            public UIRecipeCell(Recipe r, int recipeIndex, bool selected, Action<int> onSelect)
            {
                RecipeIndex = recipeIndex;
                _itemType = r.createItem.type;
                _stack = r.createItem.stack;
                _selected = selected;
                _onSelect = onSelect;
                float slotSize = TextureAssets.InventoryBack9.Width() * SlotScale;
                Width.Set((int)slotSize, 0);
                Height.Set((int)slotSize, 0);
                MarginLeft = 1;
                MarginRight = 1;
                MarginTop = 1;
                MarginBottom = 1;

                OnLeftClick += (evt, element) => _onSelect?.Invoke(RecipeIndex);
            }

            public bool Selected
            {
                get { return _selected; }
                set { _selected = value; }
            }

            private Texture2D Pixel
            {
                get
                {
                    if (_pixel == null)
                    {
                        _pixel = new Texture2D(Main.graphics.GraphicsDevice, 1, 1);
                        _pixel.SetData(new[] { Color.White });
                    }
                    return _pixel;
                }
            }

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                try
                {
                    CalculatedStyle dim = GetDimensions();
                    if (dim.Width <= 0) return;

                    // 原版物品槽背景 (参考版本: 材料齐全可制作的配方显示绿色槽位)
                    bool craftable = RecipeDatabase.CraftableRecipeIndices.Contains(RecipeIndex);
                    Color slotColor = craftable ? Color.LightGreen : Color.White;
                    spriteBatch.Draw(TextureAssets.InventoryBack9.Value, dim.Position(), null, slotColor, 0f, Vector2.Zero, SlotScale, SpriteEffects.None, 0f);

                    // 物品图标: 保持宽高比居中绘制 (参考 UIItemSlot), 支持动画帧 (带原版物品光照)
                    float available = TextureAssets.InventoryBack9.Width() * SlotScale;
                    Texture2D tex = RecipeDatabase.GetItemTexture(_itemType);
                    if (tex != null)
                    {
                        Rectangle source = Main.itemAnimations[_itemType]?.GetFrame(tex) ?? new Rectangle(0, 0, tex.Width, tex.Height);
                        float drawScale = 1f;
                        if (source.Width > available || source.Height > available)
                            drawScale = source.Width > source.Height ? available / source.Width : available / source.Height;
                        drawScale *= SlotScale;
                        Vector2 center = dim.Position() + new Vector2(available / 2f);

                        Color lightColor = Color.White;
                        float lightScale = 1f;
                        Item sample;
                        if (ContentSamples.ItemsByType.TryGetValue(_itemType, out sample))
                            Terraria.UI.ItemSlot.GetItemLight(ref lightColor, ref lightScale, sample, false);
                        spriteBatch.Draw(tex, center, source, sample != null ? sample.GetAlpha(lightColor) : lightColor, 0f, source.Size() / 2f, drawScale, SpriteEffects.None, 0f);
                    }

                    // 数量 (参考版本位置: (10,26)*scale, ItemStack 字体)
                    if (_stack > 1)
                    {
                        Vector2 countPos = dim.Position() + new Vector2(10f, 26f) * SlotScale;
                        Terraria.UI.Chat.ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.ItemStack.Value, _stack.ToString(), countPos, Color.White, 0f, Vector2.Zero, new Vector2(SlotScale), -1f, SlotScale);
                    }

                    if (IsMouseHovering || _selected)
                    {
                        Color bc = _selected ? Color.Gold : Color.White * Main.essScale;
                        int x = (int)dim.X, y = (int)dim.Y, w = (int)dim.Width, h = (int)dim.Height, t = 1;
                        spriteBatch.Draw(Pixel, new Rectangle(x, y, w, t), bc);
                        spriteBatch.Draw(Pixel, new Rectangle(x, y + h - t, w, t), bc);
                        spriteBatch.Draw(Pixel, new Rectangle(x, y, t, h), bc);
                        spriteBatch.Draw(Pixel, new Rectangle(x + w - t, y, t, h), bc);
                    }

                    if (IsMouseHovering)
                    {
                        // 原版风格悬停提示 (同参考版本: 设置 HoverItem 触发完整提示框)
                        Main.LocalPlayer.mouseInterface = true;
                        Recipe r = Main.recipe[RecipeIndex];
                        if (r != null && r.createItem != null && !r.createItem.IsAir)
                        {
                            Main.HoverItem = r.createItem.Clone();
                            Main.hoverItemName = Main.HoverItem.Name;
                        }
                    }
                }
                catch (Exception ex)
                {
                    ErrorLog.Write(ex, "UIRecipeCell.DrawSelf");
                }
            }
        }

        /// <summary>
        /// 左上角查询槽: 只记录物品信息用于筛选, 物品本体放进去后立即回背包
        /// 空手单击 = 清除筛选
        /// </summary>
        private class UIGuideSlot : UIElement
        {
            public Item Item = new Item(); // 仅信息副本, 不是真实物品
            public Action<Item> OnItemChanged;
            private Texture2D _pixel;
            private const float SlotScale = 0.75f;

            private Texture2D Pixel
            {
                get
                {
                    if (_pixel == null)
                    {
                        _pixel = new Texture2D(Main.graphics.GraphicsDevice, 1, 1);
                        _pixel.SetData(new[] { Color.White });
                    }
                    return _pixel;
                }
            }

            public UIGuideSlot()
            {
                float slotSize = TextureAssets.InventoryBack9.Width() * SlotScale;
                Width.Set((int)slotSize, 0);
                Height.Set((int)slotSize, 0);
            }

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                base.DrawSelf(spriteBatch);
                CalculatedStyle dim = GetDimensions();

                if (Item.IsAir)
                    spriteBatch.Draw(TextureAssets.InventoryBack9.Value, dim.Position(), null, Color.White, 0f, Vector2.Zero, SlotScale, SpriteEffects.None, 0f);
                else
                {
                    float oldScale = Main.inventoryScale;
                    Main.inventoryScale = SlotScale;
                    Terraria.UI.ItemSlot.Draw(spriteBatch, ref Item, Terraria.UI.ItemSlot.Context.InventoryItem, dim.Position());
                    Main.inventoryScale = oldScale;
                }

                if (ContainsPoint(Main.MouseScreen))
                {
                    Main.LocalPlayer.mouseInterface = true;
                    if (Item.IsAir)
                    {
                        Main.instance.MouseText("放入物品以查询其配方");
                    }
                    // 有物品时上面 ItemSlot.Draw 已设置 Main.HoverItem, 自动显示原版完整提示框

                    if (Main.mouseLeft && Main.mouseLeftRelease)
                    {
                        if (!Main.mouseItem.IsAir)
                        {
                            // 复制信息用于筛选, 物品本体回背包
                            Item copy = Main.mouseItem.Clone();
                            if (ReturnMouseItemToInventory())
                            {
                                Item = copy;
                                OnItemChanged?.Invoke(Item);
                            }
                        }
                        else if (!Item.IsAir)
                        {
                            // 空手单击 → 清除筛选
                            Item = new Item();
                            OnItemChanged?.Invoke(Item);
                        }
                    }
                }
            }
        }

        // ===== 所需物品槽 (原版槽背景 + 图标 + 数量) =====

        private class UINeedSlot : UIElement
        {
            private readonly int _type;
            private readonly int _stack;
            private readonly Action<int> _onClick;
            private readonly bool _missing;
            private readonly int _have;
            private readonly string _displayName;
            private const float SlotScale = 0.75f;

            public UINeedSlot(int type, int stack, Action<int> onClick, bool missing, int have, string displayName)
            {
                _type = type;
                _stack = stack;
                _onClick = onClick;
                _missing = missing;
                _have = have;
                _displayName = displayName;
                float slotSize = TextureAssets.InventoryBack9.Width() * SlotScale;
                Width.Set((int)slotSize, 0);
                Height.Set((int)slotSize, 0);
                MarginRight = 2;
                MarginBottom = 2;
                OnLeftClick += (evt, element) => _onClick?.Invoke(_type);
            }

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                try
                {
                    CalculatedStyle dim = GetDimensions();
                    if (dim.Width <= 0) return;

                    // 原版物品槽背景 (需求行槽位保持普通样式)
                    spriteBatch.Draw(TextureAssets.InventoryBack9.Value, dim.Position(), null, Color.White, 0f, Vector2.Zero, SlotScale, SpriteEffects.None, 0f);

                    // 物品图标: 保持宽高比居中绘制, 支持动画帧
                    float availableWidth = TextureAssets.InventoryBack9.Width() * SlotScale;
                    Texture2D tex = RecipeDatabase.GetItemTexture(_type);
                    if (tex == null)
                        tex = TextureAssets.MagicPixel.Value;
                    Rectangle source = Main.itemAnimations[_type]?.GetFrame(tex) ?? new Rectangle(0, 0, tex.Width, tex.Height);
                    float drawScale = 1f;
                    if (source.Width > availableWidth || source.Height > availableWidth)
                    {
                        drawScale = source.Width > source.Height ? availableWidth / source.Width : availableWidth / source.Height;
                    }
                    drawScale *= SlotScale;
                    Vector2 center = dim.Position() + new Vector2(availableWidth / 2f);
                    Vector2 origin = source.Size() / 2;
                    Color iconColor = _missing ? Color.Lerp(Color.Black, Color.White, 0.4f) : Color.White;
                    spriteBatch.Draw(tex, center, source, iconColor, 0f, origin, drawScale, SpriteEffects.None, 0f);

                    // 数量
                    if (_stack > 1)
                    {
                        Vector2 countPos = dim.Position() + new Vector2(10f, 26f) * SlotScale;
                        Terraria.UI.Chat.ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.ItemStack.Value, "×" + _stack, countPos, _missing ? new Color(255, 90, 90) : Color.White, 0f, Vector2.Zero, new Vector2(SlotScale), -1f, SlotScale);
                    }

                    if (IsMouseHovering)
                    {
                        // 原版风格悬停提示
                        Main.LocalPlayer.mouseInterface = true;
                        Item sample;
                        if (ContentSamples.ItemsByType.TryGetValue(_type, out sample) && sample != null)
                        {
                            Main.HoverItem = sample.Clone();
                            Main.hoverItemName = Main.HoverItem.Name;
                        }
                        else
                        {
                            string nm = !string.IsNullOrEmpty(_displayName) ? _displayName : Lang.GetItemNameValue(_type);
                            Main.instance.MouseText($"{nm} (ID:{_type})");
                        }
                    }
                }
                catch (Exception ex)
                {
                    ErrorLog.Write(ex, "UINeedSlot.DrawSelf");
                }
            }
        }

        // ===== 物品格 (图标 + 可制作绿点) =====

        private class UIItemCell : UIElement
        {
            public Action<int> OnClick;
            public readonly int Type;
            private readonly string _name;
            private bool _selected;
            private Texture2D _pixel;
            private const float SlotScale = 0.75f;

            public UIItemCell(int type, string name)
            {
                Type = type;
                _name = name ?? "";
                float slotSize = TextureAssets.InventoryBack9.Width() * SlotScale;
                Width.Set((int)slotSize, 0);
                Height.Set((int)slotSize, 0);
                MarginLeft = 1;
                MarginRight = 1;
                MarginTop = 1;
                MarginBottom = 1;

                OnLeftClick += (evt, element) => OnClick?.Invoke(Type);
            }

            public bool Selected
            {
                get { return _selected; }
                set { _selected = value; }
            }

            private Texture2D Pixel
            {
                get
                {
                    if (_pixel == null)
                    {
                        _pixel = new Texture2D(Main.graphics.GraphicsDevice, 1, 1);
                        _pixel.SetData(new[] { Color.White });
                    }
                    return _pixel;
                }
            }

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                try
                {
                    CalculatedStyle dim = GetDimensions();
                    if (dim.Width <= 0) return;

                    // 原版物品槽背景 (参考版本: 可制作的物品槽位显示为绿色)
                    bool craftable = RecipeDatabase.CraftableResultTypes.Contains(Type);
                    Color slotColor = craftable ? Color.LightGreen : Color.White;
                    spriteBatch.Draw(TextureAssets.InventoryBack9.Value, dim.Position(), null, slotColor, 0f, Vector2.Zero, SlotScale, SpriteEffects.None, 0f);

                    // 物品图标: 保持宽高比居中绘制, 支持动画帧 (带原版物品光照)
                    float availableWidth = TextureAssets.InventoryBack9.Width() * SlotScale;
                    Texture2D tex = RecipeDatabase.GetItemTexture(Type);
                    if (tex == null)
                        tex = TextureAssets.MagicPixel.Value;
                    Rectangle source = Main.itemAnimations[Type]?.GetFrame(tex) ?? new Rectangle(0, 0, tex.Width, tex.Height);
                    float drawScale = 1f;
                    if (source.Width > availableWidth || source.Height > availableWidth)
                    {
                        drawScale = source.Width > source.Height ? availableWidth / source.Width : availableWidth / source.Height;
                    }
                    drawScale *= SlotScale;
                    Vector2 center = dim.Position() + new Vector2(availableWidth / 2f);
                    Vector2 origin = source.Size() / 2;

                    Color lightColor = Color.White;
                    float lightScale = 1f;
                    Item sample;
                    if (ContentSamples.ItemsByType.TryGetValue(Type, out sample))
                        Terraria.UI.ItemSlot.GetItemLight(ref lightColor, ref lightScale, sample, false);
                    spriteBatch.Draw(tex, center, source, sample != null ? sample.GetAlpha(lightColor) : lightColor, 0f, origin, drawScale, SpriteEffects.None, 0f);

                    // 持有数量 (右下角, 参考版本样式)
                    int owned;
                    if (_playerStacks.TryGetValue(Type, out owned) && owned > 1)
                    {
                        Vector2 countPos = dim.Position() + new Vector2(10f, 26f) * SlotScale;
                        Terraria.UI.Chat.ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.ItemStack.Value, owned.ToString(), countPos, Color.White, 0f, Vector2.Zero, new Vector2(SlotScale), -1f, SlotScale);
                    }

                    if (IsMouseHovering || _selected)
                    {
                        Color bc = _selected ? Color.Gold : Color.White * Main.essScale;
                        int x = (int)dim.X, y = (int)dim.Y, wd = (int)dim.Width, hgt = (int)dim.Height, t = 1;
                        spriteBatch.Draw(Pixel, new Rectangle(x, y, wd, t), bc);
                        spriteBatch.Draw(Pixel, new Rectangle(x, y + hgt - t, wd, t), bc);
                        spriteBatch.Draw(Pixel, new Rectangle(x, y, t, hgt), bc);
                        spriteBatch.Draw(Pixel, new Rectangle(x + wd - t, y, t, hgt), bc);
                    }

                    if (IsMouseHovering)
                    {
                        // 原版风格悬停提示
                        Main.LocalPlayer.mouseInterface = true;
                        Item tipItem;
                        if (ContentSamples.ItemsByType.TryGetValue(Type, out tipItem) && tipItem != null)
                        {
                            Main.HoverItem = tipItem.Clone();
                            Main.hoverItemName = Main.HoverItem.Name;
                        }
                        else
                        {
                            Main.instance.MouseText($"{_name} (ID:{Type})");
                        }
                    }
                }
                catch (Exception ex)
                {
                    ErrorLog.Write(ex, "UIItemCell.DrawSelf (Type=" + Type + ")");
                }
            }
        }
    }
}

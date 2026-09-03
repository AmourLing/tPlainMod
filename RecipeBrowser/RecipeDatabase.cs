using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;

namespace RecipeBrowser
{
    /// <summary>物品目录条目</summary>
    internal class ItemEntry
    {
        public int Type;
        public string Name;
        public int Group;      // ContentSamples.CreativeHelper.ItemGroup 数值
        public int Value;
        public int Damage;
        public int Defense;
        public int Pick;
        public int Axe;
        public int Hammer;
        public int CreateTile; // 放置的图块 (-1 无)
    }

    /// <summary>制作站定义 (从配方自动提取)</summary>
    internal class StationDef
    {
        public int TileId;
        public string Name;
        public int IconItem; // 代表性物品 (用于图标), 0 表示无
    }

    /// <summary>
    /// 物品目录 + 配方索引 + 可合成判定 (全部走原版公开 API)
    /// </summary>
    internal static class RecipeDatabase
    {
        public static List<ItemEntry> Items = new List<ItemEntry>();
        public static Dictionary<int, ItemEntry> ItemsByType = new Dictionary<int, ItemEntry>();

        // 产物 → 配方下标列表 / 材料类型 → 配方下标列表 (Main.recipe 下标)
        public static Dictionary<int, List<int>> ByResult = new Dictionary<int, List<int>>();
        public static Dictionary<int, List<int>> ByIngredient = new Dictionary<int, List<int>>();

        public static bool Ready;

        /// <summary>
        /// 物品贴图 (强制同步加载): TextureAssets.Item[type] 是异步加载, 冷门物品 .Value 可能为 null
        /// </summary>
        public static Microsoft.Xna.Framework.Graphics.Texture2D GetItemTexture(int type)
        {
            if (type <= 0 || type >= ItemID.Count) return null; // 配方组假 ID (1000000+) 没有贴图
            try
            {
                return Main.Assets.Request<Microsoft.Xna.Framework.Graphics.Texture2D>(
                    "Images/Item_" + type,
                    ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }
            catch
            {
                try { return TextureAssets.Item[type].Value; }
                catch { return null; }
            }
        }

        /// <summary>全部配方涉及的制作站 (自动提取)</summary>
        public static List<StationDef> Stations = new List<StationDef>();

        /// <summary>当前可合成的产物类型集合 (RefreshCraftable 刷新)</summary>
        public static HashSet<int> CraftableResultTypes = new HashSet<int>();

        /// <summary>当前材料齐全的配方索引集合 (配方格绿色背景判定, 按配方精确判定)</summary>
        public static HashSet<int> CraftableRecipeIndices = new HashSet<int>();

        public static void Build()
        {
            if (Ready) return;
            BuildItems();
            BuildRecipeIndexes();
            BuildStations();
            Ready = true;
        }

        /// <summary>
        /// 刷新"当前可合成产物"集合 (每个配方跑一次判定, 全量约 1800 次, 几毫秒)
        /// </summary>
        public static void RefreshCraftable(Player p)
        {
            CraftableResultTypes.Clear();
            CraftableRecipeIndices.Clear();
            if (p == null || p.dead) return;

            int n = Math.Min(Recipe.numRecipes, Main.recipe.Length);
            for (int i = 0; i < n; i++)
            {
                try
                {
                    Recipe r = Main.recipe[i];
                    if (r == null || r.createItem == null || r.createItem.IsAir) continue;

                    bool craftable;
                    int times;
                    EvaluateCraft(r, p, out craftable, out times);
                    if (craftable)
                    {
                        CraftableResultTypes.Add(r.createItem.type);
                        CraftableRecipeIndices.Add(i);
                    }
                }
                catch (Exception ex)
                {
                    ErrorLog.Write(ex, "RefreshCraftable (index=" + i + ")");
                }
            }
        }

        private static void BuildItems()
        {
            Items.Clear();
            ItemsByType.Clear();

            for (int type = 1; type < ItemID.Count; type++)
            {
                try
                {
                    Item item;
                    if (!ContentSamples.ItemsByType.TryGetValue(type, out item) || item == null) continue;

                    string name = Lang.GetItemNameValue(type);
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    int order;
                    var group = ContentSamples.CreativeHelper.GetItemGroup(item, out order);

                    ItemEntry e = new ItemEntry
                    {
                        Type = type,
                        Name = name,
                        Group = (int)group,
                        Value = item.value,
                        Damage = item.damage,
                        Defense = item.defense,
                        Pick = item.pick,
                        Axe = item.axe,
                        Hammer = item.hammer,
                        CreateTile = item.createTile
                    };
                    Items.Add(e);
                    ItemsByType[type] = e;
                }
                catch (Exception ex)
                {
                    ErrorLog.Write(ex, "BuildItems (type=" + type + ")");
                }
            }
        }

        /// <summary>找放置指定图块的第一个物品作为站点图标</summary>
        private static int FindIconItemForTile(int tileId)
        {
            foreach (ItemEntry e in Items)
            {
                if (e.CreateTile == tileId) return e.Type;
            }
            return 0;
        }

        private static void BuildStations()
        {
            Stations.Clear();
            HashSet<int> seen = new HashSet<int>();

            int n = Math.Min(Recipe.numRecipes, Main.recipe.Length);
            for (int i = 0; i < n; i++)
            {
                Recipe r = Main.recipe[i];
                if (r == null || r.requiredTile < 0 || !seen.Add(r.requiredTile)) continue;

                StationDef s = new StationDef();
                try
                {
                    s.TileId = r.requiredTile;
                    s.Name = Recipe.GetRequiredTileName(r.requiredTile);
                    s.IconItem = FindIconItemForTile(r.requiredTile);
                }
                catch
                {
                    s.Name = "站点 " + r.requiredTile;
                }
                Stations.Add(s);
            }
        }

        private static void BuildRecipeIndexes()
        {
            ByResult.Clear();
            ByIngredient.Clear();

            int n = Math.Min(Recipe.numRecipes, Main.recipe.Length);
            for (int i = 0; i < n; i++)
            {
                try
                {
                    Recipe r = Main.recipe[i];
                    if (r == null || r.createItem == null || r.createItem.IsAir) continue;

                    AddTo(ByResult, r.createItem.type, i);

                    for (int k = 0; k < r.requiredItem.Length; k++)
                    {
                        Item ing = r.requiredItem[k];
                        if (ing == null || ing.IsAir || ing.stack <= 0) continue;
                        AddTo(ByIngredient, ing.type, i);
                    }
                }
                catch (Exception ex)
                {
                    ErrorLog.Write(ex, "BuildRecipeIndexes (index=" + i + ")");
                }
            }
        }

        private static void AddTo(Dictionary<int, List<int>> dict, int type, int index)
        {
            List<int> list;
            if (!dict.TryGetValue(type, out list))
            {
                list = new List<int>();
                dict[type] = list;
            }
            if (list.Count == 0 || list[list.Count - 1] != index) list.Add(index);
        }

        /// <summary>统计玩家持有物 (背包 + 四个存储), 含数量</summary>
        public static Dictionary<int, int> BuildOwnedDict(Player p)
        {
            Dictionary<int, int> owned = new Dictionary<int, int>();
            if (p == null) return owned;

            AccumulateOwned(p.inventory, owned);
            if (p.bank != null) AccumulateOwned(p.bank.item, owned);
            if (p.bank2 != null) AccumulateOwned(p.bank2.item, owned);
            if (p.bank3 != null) AccumulateOwned(p.bank3.item, owned);
            if (p.bank4 != null) AccumulateOwned(p.bank4.item, owned);
            return owned;
        }

        private static void AccumulateOwned(Item[] items, Dictionary<int, int> owned)
        {
            if (items == null) return;
            for (int i = 0; i < items.Length; i++)
            {
                Item it = items[i];
                if (it == null || it.IsAir || it.stack <= 0) continue;
                owned.TryGetValue(it.type, out int v);
                owned[it.type] = v + it.stack;
            }
        }

        /// <summary>某条需求的持有量 (配方组会汇总组内所有物品)</summary>
        public static int CountForEntry(Recipe.RequiredItemEntry e, Dictionary<int, int> owned)
        {
            if (e.IsRecipeGroup)
            {
                try
                {
                    // 假 ID = RegisteredId + FakeItemIdOffset, 反查出组
                    RecipeGroup g = RecipeGroup.recipeGroups[e.itemIdOrRecipeGroup - RecipeGroup.FakeItemIdOffset];
                    int sum = 0;
                    foreach (int t in g.ValidItems)
                    {
                        if (owned.TryGetValue(t, out int v)) sum += v;
                    }
                    return sum;
                }
                catch { return 0; }
            }
            return owned.TryGetValue(e.itemIdOrRecipeGroup, out int have) ? have : 0;
        }

        /// <summary>配方组的显示名 (如"任何木材"); 非组或异常返回空串</summary>
        public static string GetGroupText(int fakeItemId)
        {
            try
            {
                RecipeGroup g = RecipeGroup.recipeGroups[fakeItemId - RecipeGroup.FakeItemIdOffset];
                return g.GetText?.Invoke() ?? "";
            }
            catch { return ""; }
        }

        /// <summary>某条需求的代表图标物品 (配方组取组内第一个有目录条目的物品)</summary>
        public static int GetEntryIconType(Recipe.RequiredItemEntry e)
        {
            if (!e.IsRecipeGroup) return e.itemIdOrRecipeGroup;
            try
            {
                // 假 ID 反查出组; 图标用原版占位物品 (如"任意木材"显示为木材)
                RecipeGroup g = RecipeGroup.recipeGroups[e.itemIdOrRecipeGroup - RecipeGroup.FakeItemIdOffset];
                int placeholder = g.GetPlaceholderItemType();
                if (placeholder > 0 && ItemsByType.ContainsKey(placeholder)) return placeholder;
                foreach (int t in g.ValidItems)
                {
                    if (ItemsByType.ContainsKey(t)) return t;
                }
            }
            catch { }
            return 0; // 拿不到真实物品就无图标, 永不返回假 ID
        }

        /// <summary>
        /// 评估配方: 材料/环境是否满足, 以及可做几次
        /// 材料范围 = 背包 + 四个存储 (同原版 CollectItems 口径的子集, 不含周围箱子)
        /// 不调用 Recipe.UpdateRecipeList, 避免干扰原版合成窗口状态
        /// </summary>
        public static void EvaluateCraft(Recipe r, Player p, out bool craftable, out int times)
        {
            craftable = false;
            times = 0;
            try
            {
                if (r == null || p == null || p.dead) return;

                List<Recipe.RequiredItemEntry> req = new List<Recipe.RequiredItemEntry>();
                r.GetIngredientsForOneCraft(p, req);
                if (req.Count == 0) return;

                Dictionary<int, int> owned = BuildOwnedDict(p);

                times = int.MaxValue;
                foreach (Recipe.RequiredItemEntry e in req)
                {
                    int have = CountForEntry(e, owned);
                    if (have < e.stack)
                    {
                        times = 0;
                        return;
                    }
                    times = Math.Min(times, have / e.stack);
                }

                if (!r.PlayerMeetsEnvironmentConditions(p, null))
                {
                    times = 0;
                    return;
                }
                craftable = true;
            }
            catch (Exception ex)
            {
                ErrorLog.Write(ex, "EvaluateCraft");
                craftable = false;
                times = 0;
            }
        }

        private static int CountOwned(Player p, Recipe.RequiredItemEntry e)
        {
            int have = 0;
            CountIn(p.inventory, e, ref have);
            if (p.bank4 != null && p.bank4.item != null) CountIn(p.bank4.item, e, ref have); // 虚空袋
            return have;
        }

        private static void CountIn(Item[] items, Recipe.RequiredItemEntry e, ref int have)
        {
            for (int i = 0; i < items.Length; i++)
            {
                Item it = items[i];
                if (it == null || it.IsAir) continue;
                if (e.Matches(it.type)) have += it.stack;
            }
        }

        /// <summary>站点/环境条件文本, 如 "铁砧 + 水"</summary>
        public static string StationText(Recipe r)
        {
            List<string> parts = new List<string>();
            try
            {
                if (r.requiredTile >= 0)
                    parts.Add(Recipe.GetRequiredTileName(r.requiredTile));
                if (r.needWater) parts.Add("水");
                if (r.needHoney) parts.Add("蜂蜜");
                if (r.needLava) parts.Add("岩浆");
                if (r.needSnowBiome) parts.Add("雪原");
                if (r.needGraveyardBiome) parts.Add("墓地");
                if (r.alchemy) parts.Add("水瓶");
                if (r.needTorchGodsFavor) parts.Add("火把神的恩赐");
                if (r.needMechdusa) parts.Add("机械美杜莎");
            }
            catch { }

            return parts.Count == 0 ? "无需站点" : string.Join(" + ", parts);
        }

        /// <summary>铜币计价值 → 中文文本</summary>
        public static string ValueText(int value)
        {
            if (value <= 0) return "无价值";
            int copper = value % 100;
            int silver = value / 100 % 100;
            int gold = value / 10000 % 100;
            int platinum = value / 1000000;

            List<string> parts = new List<string>();
            if (platinum > 0) parts.Add($"{platinum}铂");
            if (gold > 0) parts.Add($"{gold}金");
            if (silver > 0) parts.Add($"{silver}银");
            if (copper > 0 || parts.Count == 0) parts.Add($"{copper}铜");
            return string.Join(" ", parts);
        }
    }
}

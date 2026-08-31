using System;
using System.Collections.Generic;
using System.Linq;
using tContentPatch;
using Terraria;
using Terraria.UI;

namespace BetterBuffGet
{
    [Serializable]
    public class BuffEntry
    {
        public int id;
        public bool selected;
        public bool favorite;
    }

    /// <summary>
    /// 已选增益与收藏, 由主界面的增益列表面板直接读写(即时生效), 保存时落盘
    /// </summary>
    public class BuffSetting : ModSetting
    {
        public override string Name => "已选增益";
        public override string Title => "更好的增益获取: 已选增益";
        public override string FilePath => "selectedBuffs.json";
        public override Type DataType => typeof(List<BuffEntry>);
        public override bool HasUI => false; // 由主界面增益列表面板管理

        public static Dictionary<int, bool> CurrentSelectedBuffs { get; private set; } = new Dictionary<int, bool>();
        public static HashSet<int> CurrentFavoriteBuffs { get; private set; } = new HashSet<int>();

        public static bool IsSelected(int buffId) => CurrentSelectedBuffs.TryGetValue(buffId, out bool v) && v;
        public static bool IsFavorite(int buffId) => CurrentFavoriteBuffs.Contains(buffId);

        public static void SetSelected(int buffId, bool value)
        {
            CurrentSelectedBuffs[buffId] = value;
            if (!value && !IsFavorite(buffId)) CurrentSelectedBuffs.Remove(buffId);
        }

        public static void SetFavorite(int buffId, bool value)
        {
            if (value) CurrentFavoriteBuffs.Add(buffId);
            else CurrentFavoriteBuffs.Remove(buffId);
        }

        /// <summary>应用预设: 把已选增益重置为给定集合 (收藏状态保留)</summary>
        public static void ApplyPreset(IEnumerable<int> ids)
        {
            CurrentSelectedBuffs.Clear();
            if (ids != null)
            {
                foreach (int id in ids)
                {
                    if (id > 0) CurrentSelectedBuffs[id] = true;
                }
            }
        }

        /// <summary>把当前静态状态立即保存到文件</summary>
        public static void SaveNow()
        {
            var setting = new BuffSetting();
            setting.NeedSave = true;
            setting.Save();
        }

        public override void Load(object v)
        {
            if (v == null)
            {
                SetDefault();
                Save();
                return;
            }

            var selected = new Dictionary<int, bool>();
            var favorites = new HashSet<int>();
            foreach (BuffEntry e in (List<BuffEntry>)v)
            {
                if (e == null || e.id <= 0) continue;
                if (e.selected) selected[e.id] = true;
                if (e.favorite) favorites.Add(e.id);
            }

            CurrentSelectedBuffs = selected;
            CurrentFavoriteBuffs = favorites;
        }

        public override object GetSaveData()
        {
            // 已选 + 收藏 取并集保存
            var ids = new HashSet<int>(CurrentFavoriteBuffs);
            foreach (KeyValuePair<int, bool> kv in CurrentSelectedBuffs)
            {
                if (kv.Value) ids.Add(kv.Key);
            }
            return ids
                .OrderBy(i => i)
                .Select(i => new BuffEntry { id = i, selected = IsSelected(i), favorite = IsFavorite(i) })
                .ToList();
        }

        public override void SetDefault()
        {
            CurrentSelectedBuffs = new Dictionary<int, bool>();
            CurrentFavoriteBuffs = new HashSet<int>();
            NeedSave = true;
        }
    }
}

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
    }

    public class BuffSetting : ModSetting
    {
        public override string Name => "已选增益";
        public override string Title => "更好的增益获取: 已选增益";
        public override string FilePath => "selectedBuffs.json";
        public override Type DataType => typeof(List<BuffEntry>);

        private List<BuffEntry> _buffEntries = new List<BuffEntry>();

        public static Dictionary<int, bool> CurrentSelectedBuffs { get; private set; } = new Dictionary<int, bool>();

        public override void Load(object v)
        {
            if (v == null)
            {
                SetDefault();
                Save();
            }
            else
            {
                _buffEntries = ((List<BuffEntry>)v).ToList();
            }

            var available = AvailableBuffsSetting.CurrentAvailableBuffs ?? new List<int>();
            var savedDict = _buffEntries.ToDictionary(e => e.id, e => e.selected);
            var newDict = new Dictionary<int, bool>();
            foreach (int id in available)
            {
                bool selected = savedDict.ContainsKey(id) ? savedDict[id] : false;
                newDict[id] = selected;
            }
            CurrentSelectedBuffs = newDict;
        }

        public override object GetSaveData()
        {
            return CurrentSelectedBuffs.Select(kv => new BuffEntry { id = kv.Key, selected = kv.Value }).ToList();
        }

        public override void SetDefault()
        {
            _buffEntries = new List<BuffEntry>();
            NeedSave = true;
            // CurrentSelectedBuffs 将在 Load 中重建
        }

        public override UIElement GetUI() => null;

        public void UpdateData(Dictionary<int, bool> selectedDict)
        {
            CurrentSelectedBuffs = new Dictionary<int, bool>(selectedDict);
            NeedSave = true;
        }
    }
}
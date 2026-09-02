using System;
using System.Collections.Generic;
using tContentPatch;
using Terraria.UI;

namespace BetterAchievementUnlocker
{
    /// <summary>
    /// 选中成就 (待解锁) 的持久化, 按成就内部名 Name 记录
    /// </summary>
    public class AchievementSetting : ModSetting
    {
        public override string Name => "选中成就";
        public override string Title => "成就解锁器: 选中成就";
        public override string FilePath => "selectedAchievements.json";
        public override Type DataType => typeof(List<string>);
        public override bool HasUI => false; // 由成就解锁器主窗口管理

        public static HashSet<string> CurrentSelected { get; private set; } = new HashSet<string>();

        public override void Load(object v)
        {
            CurrentSelected = new HashSet<string>();
            if (v is List<string> list)
            {
                foreach (string s in list)
                {
                    if (!string.IsNullOrEmpty(s)) CurrentSelected.Add(s);
                }
            }
        }

        public override object GetSaveData() => new List<string>(CurrentSelected);

        public override void SetDefault()
        {
            CurrentSelected = new HashSet<string>();
            NeedSave = true;
        }

        public static bool IsSelected(string name)
        {
            return CurrentSelected.Contains(name);
        }

        public static void SetSelected(string name, bool value)
        {
            if (value) CurrentSelected.Add(name);
            else CurrentSelected.Remove(name);
        }

        public static void ClearAll()
        {
            CurrentSelected.Clear();
        }

        /// <summary>立即保存选中状态到文件</summary>
        public static void SaveNow()
        {
            AchievementSetting s = new AchievementSetting();
            s.NeedSave = true;
            s.Save();
        }
    }
}

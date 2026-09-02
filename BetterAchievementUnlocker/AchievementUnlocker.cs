using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.Achievements;

namespace BetterAchievementUnlocker
{
    /// <summary>
    /// 成就解锁核心: 枚举条件字典需要一次反射,
    /// 完成链路走公开 API (Condition.Complete → 成就计数 → OnCompleted → 自动存档)
    /// </summary>
    internal static class AchievementUnlocker
    {
        private static bool _helperCalled;
        private static bool _inited;
        private static FieldInfo _conditionsField;

        // 计数型条件的 value 字段在派生类上, 按具体类型缓存
        private static readonly Dictionary<Type, FieldInfo> _valueFields = new Dictionary<Type, FieldInfo>();
        private static readonly Dictionary<Type, FieldInfo> _maxValueFields = new Dictionary<Type, FieldInfo>();
        private static readonly Dictionary<Type, PropertyInfo> _valueProps = new Dictionary<Type, PropertyInfo>();

        /// <summary>
        /// 强制触发成就条件注册 (等价于进世界时的钩子), 只调一次
        /// </summary>
        public static void EnsureConditionsRegistered()
        {
            if (_helperCalled) return;
            _helperCalled = true;
            try
            {
                Type helperType = Type.GetType("Terraria.GameContent.Achievements.AchievementsHelper, Terraria");
                helperType
                    ?.GetMethod("OnPlayerEnteredWorld", BindingFlags.NonPublic | BindingFlags.Static)
                    ?.Invoke(null, new object[] { Main.LocalPlayer });
            }
            catch { }
        }

        /// <summary>全部成就 (按 Id 排序)</summary>
        public static List<Achievement> GetAll()
        {
            EnsureConditionsRegistered();
            List<Achievement> list = Main.Achievements?.CreateAchievementsList() ?? new List<Achievement>();
            list.Sort((a, b) => a.Id.CompareTo(b.Id));
            return list;
        }

        public static bool IsUnlocked(Achievement ach)
        {
            return ach != null && ach.IsCompleted;
        }

        /// <summary>解锁单个成就, 返回是否成功</summary>
        public static bool Unlock(Achievement ach)
        {
            if (ach == null) return false;
            if (ach.IsCompleted) return true;

            EnsureConditionsRegistered();
            Init();

            IDictionary conditions = _conditionsField?.GetValue(ach) as IDictionary;
            if (conditions == null || conditions.Count == 0) return false;

            foreach (DictionaryEntry entry in conditions)
            {
                if (entry.Value is AchievementCondition cond)
                {
                    FillCountedValue(cond);
                    cond.Complete(); // 计数+1, 全部完成时成就自动完成并触发存档
                }
            }
            return ach.IsCompleted;
        }

        /// <summary>按内部名解锁一批, 返回成功数; 完成后写存档</summary>
        public static int UnlockMany(IEnumerable<string> names)
        {
            AchievementManager manager = Main.Achievements;
            if (manager == null) return 0;

            int count = 0;
            foreach (string name in names)
            {
                Achievement ach = manager.GetAchievement(name);
                if (ach != null && Unlock(ach)) count++;
            }
            manager.Save();
            return count;
        }

        public static int GetIconIndex(Achievement ach)
        {
            try { return Main.Achievements?.GetIconIndex(ach.Name) ?? 0; }
            catch { return 0; }
        }

        /// <summary>
        /// 取消解锁 (本地清除进度并存档)
        /// Steam 后端开启时 manager.Clear 会拒绝, 此时回退为本地 ClearProgress + Save
        /// (Steam 服务器侧的成就记录无法移除, 只影响本地成就文件)
        /// </summary>
        public static bool ClearProgress(Achievement ach)
        {
            if (ach == null) return false;
            AchievementManager manager = Main.Achievements;
            if (manager == null) return false;

            bool cleared = false;
            try { cleared = manager.Clear(ach.Name); } catch { }

            if (!cleared && ach.IsCompleted)
            {
                ach.ClearProgress();
                manager.Save();
            }
            return true;
        }

        private static void Init()
        {
            if (_inited) return;
            _inited = true;
            _conditionsField = typeof(Achievement).GetField("_conditions", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        /// <summary>计数型条件: 把 _value 顶到 _maxValue, 保证进度显示完整</summary>
        private static void FillCountedValue(AchievementCondition cond)
        {
            Type t = cond.GetType();
            string n = t.Name;
            if (n != "CustomIntCondition" && n != "CustomFloatCondition") return;

            if (!_valueFields.TryGetValue(t, out FieldInfo valueField))
            {
                valueField = t.GetField("_value", BindingFlags.NonPublic | BindingFlags.Instance);
                _valueFields[t] = valueField;
            }
            if (!_maxValueFields.TryGetValue(t, out FieldInfo maxField))
            {
                maxField = t.GetField("_maxValue", BindingFlags.NonPublic | BindingFlags.Instance);
                _maxValueFields[t] = maxField;
            }
            if (!_valueProps.TryGetValue(t, out PropertyInfo valueProp))
            {
                valueProp = t.GetProperty("Value");
                _valueProps[t] = valueProp;
            }

            if (maxField != null)
            {
                object max = maxField.GetValue(cond);
                valueField?.SetValue(cond, max);
                valueProp?.SetValue(cond, max);
            }
        }
    }
}

using Microsoft.Xna.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using tContentPatch;
using Terraria;
using Terraria.UI;

namespace BetterAchievementUnlocker
{
    public class BetterAchievementUnlockerMod : PatchMain
    {
        private static UIAchievementUnlocker _ui;
        private static UserInterface _userInterface;
        private static UIState _uiState;

        static BetterAchievementUnlockerMod()
        {
            _userInterface = new UserInterface();
            _uiState = new UIState();
            _userInterface.SetState(_uiState);
        }

        public override void Initialize()
        {
            if (Main.dedServ) return;
            _ui = new UIAchievementUnlocker("成就解锁器", 400, 200);
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
                    "BetterAchievementUnlocker: UI",
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

        public static void UnlockAllAchievements()
        {
            if (Main.gameMenu)
            {
                Main.NewText("请先进入一个世界再解锁成就", Color.Yellow);
                return;
            }

            try
            {
                var manager = Main.Achievements;
                if (manager == null)
                {
                    Main.NewText("成就管理器为 null", Color.Red);
                    return;
                }

                var managerType = manager.GetType();

                // ---- 强制触发成就注册 ----
                var helperType = Type.GetType("Terraria.GameContent.Achievements.AchievementsHelper, Terraria");
                if (helperType != null)
                {
                    var onPlayerEntered = helperType.GetMethod("OnPlayerEnteredWorld", BindingFlags.NonPublic | BindingFlags.Static);
                    if (onPlayerEntered != null)
                        onPlayerEntered.Invoke(null, new object[] { Main.LocalPlayer });
                }

                // ---- 获取成就字典 ----
                var dictField = managerType.GetField("_achievements", BindingFlags.NonPublic | BindingFlags.Instance);
                if (dictField == null)
                {
                    Main.NewText("未找到 _achievements 字段", Color.Red);
                    return;
                }
                var dictObj = dictField.GetValue(manager);
                if (dictObj == null)
                {
                    Main.NewText("_achievements 字段为 null", Color.Red);
                    return;
                }
                var dict = dictObj as IDictionary;
                if (dict == null || dict.Count == 0)
                {
                    Main.NewText("成就字典为空，请先手动打开成就界面后重试", Color.Yellow);
                    return;
                }

                Main.NewText($"获取到 {dict.Count} 个成就", Color.LightGray);

                // ---- 获取类型和成员 ----
                var achievementType = Type.GetType("Terraria.Achievements.Achievement, Terraria");
                var conditionType = Type.GetType("Terraria.Achievements.AchievementCondition, Terraria");
                if (achievementType == null || conditionType == null)
                {
                    Main.NewText("无法找到成就相关类型", Color.Red);
                    return;
                }

                var conditionsField = achievementType.GetField("_conditions", BindingFlags.NonPublic | BindingFlags.Instance);
                var completedCountField = achievementType.GetField("_completedCount", BindingFlags.NonPublic | BindingFlags.Instance);
                var onCompletedField = achievementType.GetField("OnCompleted", BindingFlags.NonPublic | BindingFlags.Instance);
                var isCompletedProp = achievementType.GetProperty("IsCompleted");
                var nameProp = achievementType.GetProperty("Name");

                var completeMethod = conditionType.GetMethod("Complete", BindingFlags.Public | BindingFlags.Instance);
                var isCompletedField = conditionType.GetField("_isCompleted", BindingFlags.NonPublic | BindingFlags.Instance);
                var valueField = conditionType.GetField("_value", BindingFlags.NonPublic | BindingFlags.Instance);
                var maxValueField = conditionType.GetField("_maxValue", BindingFlags.NonPublic | BindingFlags.Instance);
                var valueProp = conditionType.GetProperty("Value");

                // ---- 统计初始状态 ----
                int initialCompleted = 0;
                foreach (DictionaryEntry entry in dict)
                {
                    var ach = entry.Value;
                    if (ach == null) continue;
                    if (isCompletedProp != null && (bool)isCompletedProp.GetValue(ach))
                        initialCompleted++;
                }
                Main.NewText($"初始已完成: {initialCompleted}/{dict.Count}", Color.LightGray);

                int modifiedCount = 0;
                string modifiedNames = "";

                // ---- 遍历所有成就 ----
                foreach (DictionaryEntry entry in dict)
                {
                    var ach = entry.Value;
                    if (ach == null) continue;

                    string achName = nameProp?.GetValue(ach) as string ?? "Unknown";

                    // 获取条件字典（使用非泛型 IDictionary）
                    var conditionsObj = conditionsField?.GetValue(ach);
                    if (conditionsObj == null)
                    {
                        Main.NewText($"成就 {achName}: 条件字典为 null", Color.LightGray);
                        continue;
                    }
                    var conditionsDict = conditionsObj as IDictionary;
                    if (conditionsDict == null || conditionsDict.Count == 0)
                    {
                        Main.NewText($"成就 {achName}: 条件字典为空 (Count=0)", Color.LightGray);
                        continue;
                    }

                    bool alreadyDone = isCompletedProp != null && (bool)isCompletedProp.GetValue(ach);
                    if (alreadyDone)
                        continue; // 已完成的跳过

                    // ---- 强制完成每个条件 ----
                    foreach (DictionaryEntry condEntry in conditionsDict)
                    {
                        var cond = condEntry.Value;
                        if (cond == null) continue;

                        Type condType = cond.GetType();
                        // 如果是计数型，设置 _value 和 Value
                        if (condType.Name == "CustomIntCondition" || condType.Name == "CustomFloatCondition")
                        {
                            if (maxValueField != null)
                            {
                                var maxVal = maxValueField.GetValue(cond);
                                valueField?.SetValue(cond, maxVal);
                                if (valueProp != null)
                                    valueProp.SetValue(cond, maxVal);
                            }
                        }
                        // 调用 Complete
                        completeMethod?.Invoke(cond, null);
                        // 强制标记为已完成
                        isCompletedField?.SetValue(cond, true);
                    }

                    // 设置成就的 _completedCount 为条件总数
                    completedCountField?.SetValue(ach, conditionsDict.Count);

                    // 触发 OnCompleted 事件
                    var onCompletedDelegate = onCompletedField?.GetValue(ach) as Delegate;
                    if (onCompletedDelegate != null)
                    {
                        foreach (var handler in onCompletedDelegate.GetInvocationList())
                        {
                            try { handler.DynamicInvoke(ach); }
                            catch { }
                        }
                    }

                    modifiedCount++;
                    modifiedNames += achName + ", ";
                }

                Main.NewText($"尝试修改了 {modifiedCount} 个成就", Color.LightGray);
                if (modifiedCount > 0)
                    Main.NewText($"修改的成就: {modifiedNames.TrimEnd(',', ' ')}", Color.LightGray);

                // ---- 保存 ----
                var saveMethod = managerType.GetMethod("Save", BindingFlags.Public | BindingFlags.Instance);
                if (saveMethod != null)
                {
                    saveMethod.Invoke(manager, null);
                    Main.NewText("数据已保存", Color.LightGray);
                }

                // ---- 最终验证 ----
                int finalCompleted = 0;
                foreach (DictionaryEntry entry in dict)
                {
                    var ach = entry.Value;
                    if (ach == null) continue;
                    if (isCompletedProp != null && (bool)isCompletedProp.GetValue(ach))
                        finalCompleted++;
                }
                Main.NewText($"最终验证: {finalCompleted}/{dict.Count} 个成就已解锁", Color.LightGray);

                // ---- 强制刷新 UI ----
                RefreshAchievementsUI();

                if (finalCompleted == dict.Count)
                {
                    Main.NewText("所有成就已经解锁！", Color.Green);
                }
                else
                {
                    Main.NewText($"还有 {dict.Count - finalCompleted} 个成就未解锁", Color.Yellow);
                    Main.NewText("请关闭并重新打开成就界面，若仍未解锁，请联系开发者", Color.LightGray);
                }
            }
            catch (Exception ex)
            {
                Main.NewText($"解锁失败: {ex.Message}", Color.Red);
                Main.NewText($"堆栈: {ex.StackTrace}", Color.Red);
            }
        }

        private static void RefreshAchievementsUI()
        {
            try
            {
                var menuType = Type.GetType("Terraria.UI.UIAchievementsMenu, Terraria");
                if (menuType != null)
                {
                    var constructor = menuType.GetConstructor(Type.EmptyTypes);
                    if (constructor != null)
                    {
                        var newMenu = constructor.Invoke(null);
                        var field = typeof(Main).GetField("AchievementsMenu", BindingFlags.Public | BindingFlags.Static);
                        if (field != null)
                            field.SetValue(null, newMenu);
                    }
                }

                if (Main.menuMode == 888)
                {
                    Main.menuMode = 0;
                    Main.menuMode = 888;
                }
            }
            catch { }
        }
    }
}
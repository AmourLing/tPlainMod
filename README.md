# tPlainMod

基于 [tPlainModLoader](https://github.com/AmourLing/tPlainModLoader) 的 Terraria (1.4.4) 功能模组合集。

## 模组列表

| 模组 | 简介 |
|---|---|
| **BetterPrefix** 更好的前缀 | 前缀编辑器: 随机/完美/预设重铸, 全前缀总表与搜索 |
| **BetterBuffGet** 更好的增益获取 | 增益图标网格, 一键/自动上增益, 命名预设方案 |
| **BetterInfoDisplay** 更好的信息显示 | 角色属性分区展示, 伤害详情页, 实用功能开关 |
| **BetterAchievementUnlocker** 成就解锁器 | 成就图标网格, 逐个选择/解锁/取消解锁 |

## 依赖

- [tPlainModLoader](https://github.com/AmourLing/tPlainModLoader)（含 tContentPatch 公共库）
- [QuickSetting](https://github.com/AmourLing/tPlainModLoader)（可选, 提供设置面板与页面）
- [QuickButton](https://github.com/AmourLing/tPlainModLoader)（可选, 提供快捷按钮栏）

## 构建

Visual Studio / MSBuild, `Release | x86`, 产物自动输出到游戏 `Mods` 目录（路径由 `Directory.Build.props` 配置）。

## 许可证

[MIT](LICENSE) © 2026 AmourLing

## 致谢

- [ImproveGame 更好的体验](https://github.com/487666123/ImproveGame)（MIT, © 局长）—— BetterBuffGet 的增益图标网格、BetterInfoDisplay 的伤害详情分类等界面设计参考了该模组（未使用其代码）。

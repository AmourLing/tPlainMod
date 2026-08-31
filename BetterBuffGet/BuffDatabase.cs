using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;

namespace BetterBuffGet
{
    /// <summary>
    /// 增益时长数据库: 分帧扫描所有物品, 缓存各增益对应的最长物品持续时间
    /// (参考 ImproveGame 无限药水的缓存重建思路, 避免一次性扫描造成卡顿)
    /// </summary>
    internal static class BuffDatabase
    {
        private const int ScanChunkSize = 60;

        private static readonly Dictionary<int, int> _durations = new Dictionary<int, int>();
        private static int _scanIndex = 1;

        /// <summary>全量扫描是否完成</summary>
        public static bool Ready { get; private set; }

        /// <summary>
        /// 可通过药水/食物等可消耗物品获得的增益 ID 集合 (扫描期间填充)
        /// </summary>
        public static HashSet<int> ConsumableBuffIds { get; } = new HashSet<int>();

        /// <summary>
        /// 增益站(可放置方块)提供的增益: 篝火/红心灯笼/向日葵/和平蜡烛/水蜡烛/战斗/猫雕像/旗帜等
        /// 这些来自方块而非物品, 不在物品扫描中, 静态硬编码
        /// </summary>
        public static HashSet<int> StationBuffIds { get; } = new HashSet<int>
        {
            BuffID.Campfire,
            BuffID.HeartLamp,
            BuffID.Sunflower,
            BuffID.PeaceCandle,
            BuffID.Calm,
            BuffID.WaterCandle,
            BuffID.Battle,
            BuffID.CatBast,
            BuffID.MonsterBanner,
        };

        /// <summary>
        /// 是否属于"可通过药水/食物/增益站等获得的增益"
        /// </summary>
        public static bool IsObtainable(int buffId) =>
            ConsumableBuffIds.Contains(buffId) || StationBuffIds.Contains(buffId);

        /// <summary>
        /// 每帧推进一部分扫描, 在游戏内 Update 中调用
        /// </summary>
        public static void Update()
        {
            if (Ready) return;

            int end = Math.Min(_scanIndex + ScanChunkSize, ItemID.Count);
            for (int type = _scanIndex; type < end; type++)
            {
                Item item = new Item();
                item.SetDefaults(type);

                if (item.buffType > 0 && item.buffTime > 0)
                {
                    int id = item.buffType;
                    int time = item.buffTime;
                    if (!_durations.TryGetValue(id, out int cur) || time > cur)
                        _durations[id] = time;

                    // 药水/食物等可消耗物品提供的增益
                    if (item.consumable)
                        ConsumableBuffIds.Add(id);
                }
            }

            _scanIndex = end;
            if (_scanIndex >= ItemID.Count) Ready = true;
        }

        /// <summary>
        /// 获取增益的默认持续时间(帧), 未扫描到时按减益/增益给保守默认值
        /// </summary>
        public static int GetDuration(int buffId)
        {
            if (_durations.TryGetValue(buffId, out int d)) return d;
            return Main.debuff[buffId] ? 60 : 300;
        }
    }
}

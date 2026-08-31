using System;
using System.Collections.Generic;
using tContentPatch;
using Terraria.UI;

namespace BetterBuffGet
{
    /// <summary>
    /// 旧版"可用增益"白名单, 已由主界面的全增益列表取代;
    /// 仅保留旧配置文件的读取兼容, 不再显示设置页
    /// </summary>
    public class AvailableBuffsSetting : ModSetting
    {
        public override string Name => "可用增益";
        public override string Title => "更好的增益获取: 可用增益";
        public override string FilePath => "availableBuffs.json";
        public override Type DataType => typeof(List<int>);
        public override bool HasUI => false;

        public override void Load(object v)
        {
            // 旧文件读取后忽略, 白名单逻辑已废弃
        }

        public override object GetSaveData() => null;
        public override void SetDefault() { }
    }
}

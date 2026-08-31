using System;
using tContentPatch;
using Terraria;
using Terraria.UI;

namespace BetterBuffGet
{
    public class Setting : ModSetting
    {
        public override string Name => "增益列表";
        public override string Title => "更好的增益获取: 增益列表";
        public override string FilePath => null; // 不保存数据
        public override Type DataType => null;

        public override UIElement GetUI()
        {
            return new UIBuffList();
        }

        public override void Load(object v) { }
        public override object GetSaveData() => null;
        public override void SetDefault() { }
    }
}
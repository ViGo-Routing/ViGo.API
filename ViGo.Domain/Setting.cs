using System;
using System.Collections.Generic;
using ViGo.Domain.Enumerations;

namespace ViGo.Domain
{
    public partial class Setting
    {
        public override Guid Id { get; set; }
        public string Key { get; set; } = null!;
        public string Value { get; set; } = null!;
        public SettingType Type { get; set; }
        public SettingDataType DataType { get; set; }
        public SettingDataUnit DataUnit { get; set; }
    }
}

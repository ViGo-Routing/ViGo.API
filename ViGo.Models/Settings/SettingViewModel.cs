using ViGo.Domain;
using ViGo.Domain.Enumerations;

namespace ViGo.Models.Settings
{
    public class SettingViewModel
    {
        public Guid Id { get; set; }
        public string Key { get; set; }
        public string Description { get; set; }
        public string Value { get; set; }
        public SettingType Type { get; set; }
        public SettingDataType DataType { get; set; }
        public SettingDataUnit DataUnit { get; set; }

        public SettingViewModel(Setting setting, string description)
        {
            Id = setting.Id;
            Key = setting.Key;
            Description = description;
            Value = setting.Value;
            Type = setting.Type;
            DataType = setting.DataType;
            DataUnit = setting.DataUnit;
        }
    }
}

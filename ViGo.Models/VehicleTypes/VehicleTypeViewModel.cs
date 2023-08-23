using ViGo.Domain;
using ViGo.Domain.Enumerations;

namespace ViGo.Models.VehicleTypes
{
    public class VehicleTypeViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public short Slot { get; set; }
        public VehicleSubType Type { get; set; }
        public DateTime CreatedTime { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime UpdatedTime { get; set; }
        public Guid UpdatedBy { get; set; }
        public bool IsDeleted { get; set; }

        public VehicleTypeViewModel(VehicleType vehicleType)
        {
            Id = vehicleType.Id;
            Name = vehicleType.Name;
            Slot = vehicleType.Slot;
            Type = vehicleType.Type;
            CreatedTime = vehicleType.CreatedTime;
            CreatedBy = vehicleType.CreatedBy;
            UpdatedTime = vehicleType.UpdatedTime;
            UpdatedBy = vehicleType.UpdatedBy;
            IsDeleted = vehicleType.IsDeleted;

        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.Models.VehicleTypes;
using ViGo.Repository.Core;
using ViGo.Services.Core;

namespace ViGo.Services
{
    public class VehicleTypeServices : BaseServices<VehicleType>
    {
        public VehicleTypeServices(IUnitOfWork work) : base(work)
        {
        }

        public async Task<IEnumerable<VehicleType>> GetAllVehicleTypesAsync()
        {
            IEnumerable<VehicleType> vehicleType = await work.VehicleTypes.GetAllAsync();
            return vehicleType;
        }

        public async Task<VehicleType> GetVehicleTypeByIdAsync(Guid id)
        {
            VehicleType vehicleType = await work.VehicleTypes.GetAsync(id);
            return vehicleType;
        }

        public async Task<VehicleType> CreateVehicleTypeAsync(VehicleTypeCreateModel newVehicleType)
        {
            var entry = new VehicleType
            {
                Name = newVehicleType.Name,
                Slot = newVehicleType.Slot,
                Type = newVehicleType.Type,
                IsDeleted = false,
            };
            await work.VehicleTypes.InsertAsync(entry);
            var result = await work.SaveChangesAsync();
            if (result > 0)
            {
                return entry;
            }
            return null!;

        }

        public async Task<VehicleType> UpdateVehicleTypeAsync(Guid id, VehicleTypeUpdateModel vehicleTypeUpdate)
        {
            var currentVehicleType = await GetVehicleTypeByIdAsync(id);

            if (currentVehicleType != null)
            {
                if (vehicleTypeUpdate.Name != null) currentVehicleType.Name = vehicleTypeUpdate.Name;
                if (vehicleTypeUpdate.Slot != null) currentVehicleType.Slot = (short)vehicleTypeUpdate.Slot;
                if (vehicleTypeUpdate.Type != null) currentVehicleType.Type = (VehicleSubType)vehicleTypeUpdate.Type;
                if (vehicleTypeUpdate.IsDeleted != null) currentVehicleType.IsDeleted = (bool)vehicleTypeUpdate.IsDeleted;
            }

            await work.VehicleTypes.UpdateAsync(currentVehicleType);
            await work.SaveChangesAsync();
            return currentVehicleType;
        }
    }
}

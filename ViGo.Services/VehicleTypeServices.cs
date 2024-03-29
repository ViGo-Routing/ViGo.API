﻿using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.Models.VehicleTypes;
using ViGo.Repository.Core;
using ViGo.Services.Core;

namespace ViGo.Services
{
    public class VehicleTypeServices : BaseServices
    {
        public VehicleTypeServices(IUnitOfWork work, ILogger logger) : base(work, logger)
        {
        }

        public async Task<IEnumerable<VehicleTypeViewModel>> GetAllVehicleTypesAsync(
            //PaginationParameter pagination,
            HttpContext context,
            CancellationToken cancellationToken)
        {
            IEnumerable<VehicleType> vehicleTypes = await work.VehicleTypes.GetAllAsync(cancellationToken: cancellationToken);

            int totalRecords = vehicleTypes.Count();

            IEnumerable<VehicleTypeViewModel> models = from vehicleType in vehicleTypes
                                                       select new VehicleTypeViewModel(vehicleType);

            //return vehicleType.ToPagedEnumerable(
            //    pagination.PageNumber, pagination.PageSize, 
            //    totalRecords, context);

            return models;

        }

        public async Task<VehicleTypeViewModel> GetVehicleTypeByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            VehicleType vehicleType = await work.VehicleTypes.GetAsync(id, cancellationToken: cancellationToken);
            return new VehicleTypeViewModel(vehicleType);
        }

        public async Task<VehicleType> CreateVehicleTypeAsync(VehicleTypeCreateModel newVehicleType, CancellationToken cancellationToken)
        {
            var entry = new VehicleType
            {
                Name = newVehicleType.Name,
                Slot = newVehicleType.Slot,
                Type = newVehicleType.Type,
                IsDeleted = false,
            };
            await work.VehicleTypes.InsertAsync(entry, cancellationToken: cancellationToken);
            var result = await work.SaveChangesAsync(cancellationToken: cancellationToken);
            if (result > 0)
            {
                return entry;
            }
            return null!;

        }

        public async Task<VehicleType> UpdateVehicleTypeAsync(Guid id, VehicleTypeUpdateModel vehicleTypeUpdate)
        {
            var currentVehicleType = await work.VehicleTypes.GetAsync(id);

            if (currentVehicleType is null)
            {
                throw new ApplicationException("Loại phương tiện không tồn tại!!");
            }

            if (vehicleTypeUpdate.Name != null) currentVehicleType.Name = vehicleTypeUpdate.Name;
            if (vehicleTypeUpdate.Slot != null) currentVehicleType.Slot = (short)vehicleTypeUpdate.Slot;
            if (vehicleTypeUpdate.Type != null) currentVehicleType.Type = (VehicleSubType)vehicleTypeUpdate.Type;
            if (vehicleTypeUpdate.IsDeleted != null) currentVehicleType.IsDeleted = (bool)vehicleTypeUpdate.IsDeleted;

            await work.VehicleTypes.UpdateAsync(currentVehicleType);
            await work.SaveChangesAsync();
            return currentVehicleType;
        }
    }
}

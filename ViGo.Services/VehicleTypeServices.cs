﻿using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.Models.VehicleTypes;
using ViGo.Repository.Core;
using ViGo.Repository.Pagination;
using ViGo.Services.Core;

namespace ViGo.Services
{
    public class VehicleTypeServices : BaseServices
    {
        public VehicleTypeServices(IUnitOfWork work, ILogger logger) : base(work, logger)
        {
        }

        public async Task<IPagedEnumerable<VehicleType>> GetAllVehicleTypesAsync(
            PaginationParameter pagination,
            HttpContext context,
            CancellationToken cancellationToken)
        {
            IEnumerable<VehicleType> vehicleType = await work.VehicleTypes.GetAllAsync(cancellationToken: cancellationToken);

            int totalRecords = vehicleType.Count();

            return vehicleType.ToPagedEnumerable(
                pagination.PageNumber, pagination.PageSize, 
                totalRecords, context);
        }

        public async Task<VehicleType> GetVehicleTypeByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            VehicleType vehicleType = await work.VehicleTypes.GetAsync(id, cancellationToken: cancellationToken);
            return vehicleType;
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

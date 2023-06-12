using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Models.Vehicles;
using ViGo.Repository.Core;
using ViGo.Services.Core;

namespace ViGo.Services
{
    public class VehicleServices : BaseServices<Vehicle>
    {
        public VehicleServices(IUnitOfWork work) : base(work)
        {
        }

        public async Task<IEnumerable<Vehicle>> GetAllVehiclesAsync()
        {
            IEnumerable<Vehicle> vehicles = await work.Vehicles.GetAllAsync();

            return vehicles;
        }

        public async Task<Vehicle> GetVehicleByIdAsync(Guid id)
        {
            Vehicle vehicle = await work.Vehicles.GetAsync(id);

            return vehicle;
        }

        public async Task<Vehicle> CreateVehicleAsync(VehiclesCreateModel vehicle)
        {
            //check vehicle type id exist 
            Vehicle entry = new Vehicle
            {
                Name = vehicle.Name,
                LicensePlate = vehicle.LicensePlate,
                VehicleTypeId = vehicle.VehicleTypeId,
                UserId = vehicle.UserId,
                IsDeleted = false,
            };

            await work.Vehicles.InsertAsync(entry);
            var result = await work.SaveChangesAsync();
            if (result > 0)
            {
                return entry;
            }
            return null!;
        }

        public async Task<Vehicle> UpdateVehicleAsync(Guid id, VehiclesUpdateModel vehiclesUpdate)
        {
            var currentVehicle = await GetVehicleByIdAsync(id);

            if (currentVehicle != null)
            {
                if (vehiclesUpdate.Name != null)
                {
                    currentVehicle.Name = vehiclesUpdate.Name;
                }
                if (vehiclesUpdate.LicensePlate != null)
                {
                    currentVehicle.LicensePlate = vehiclesUpdate.LicensePlate;
                }
                if (vehiclesUpdate.VehicleTypeId != null)
                {
                    currentVehicle.VehicleTypeId = (Guid)vehiclesUpdate.VehicleTypeId;
                }
                if (vehiclesUpdate.IsDeleted != null)
                {
                    currentVehicle.IsDeleted = (bool)vehiclesUpdate.IsDeleted;
                }
            }

            await work.Vehicles.UpdateAsync(currentVehicle!);
            await work.SaveChangesAsync();
            return currentVehicle!;
        }
    }
}

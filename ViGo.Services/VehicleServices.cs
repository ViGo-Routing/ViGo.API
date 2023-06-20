using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Models.Users;
using ViGo.Models.Vehicles;
using ViGo.Models.VehicleTypes;
using ViGo.Repository.Core;
using ViGo.Services.Core;

namespace ViGo.Services
{
    public class VehicleServices : BaseServices
    {
        public VehicleServices(IUnitOfWork work, ILogger logger) : base(work, logger)
        {
        }

        public async Task<IEnumerable<VehiclesViewModel>> GetAllVehiclesAsync()
        {
            IEnumerable<Vehicle> vehicles = await work.Vehicles.GetAllAsync();
            IEnumerable<Guid> userIds = vehicles.Select(ids => ids.UserId);
            IEnumerable<Guid> vehicleTypeIds = vehicles.Select(ids => ids.VehicleTypeId);
            IEnumerable<User> users = await work.Users.GetAllAsync(q => q.Where(items => userIds.Contains(items.Id)));
            IEnumerable<VehicleType> vehicleTypes = await work.VehicleTypes.GetAllAsync(q => q.Where(items => vehicleTypeIds.Contains(items.Id)));

            IEnumerable<UserViewModel> userViewModels = from user in users 
                                                        select new UserViewModel(user);

            IEnumerable<VehicleTypeViewModel> vehicleTypeViewModels = from vehicleType in vehicleTypes
                                                                      select new VehicleTypeViewModel(vehicleType);

            IEnumerable<VehiclesViewModel> listVehicle = from vehicle in vehicles
                                                         join userModel in userViewModels
                                                            on vehicle.UserId equals userModel.Id
                                                         join vehicleTypeModel in vehicleTypeViewModels
                                                            on vehicle.VehicleTypeId equals vehicleTypeModel.Id
                                                         select new VehiclesViewModel(vehicle, userModel, vehicleTypeModel);
            return listVehicle;
        }

        public async Task<VehiclesViewModel> GetVehicleByIdAsync(Guid id)
        {
            Vehicle vehicle = await work.Vehicles.GetAsync(id);
            Guid userId = vehicle.UserId;
            Guid vehicleTypeId = vehicle.VehicleTypeId;

            User user = await work.Users.GetAsync(userId);
            VehicleType vehicleType = await work.VehicleTypes.GetAsync(vehicleTypeId);
            UserViewModel userViewModel = new UserViewModel(user);
            VehicleTypeViewModel vehicleTypeViewModel = new VehicleTypeViewModel(vehicleType);

            VehiclesViewModel vehicleModel = new VehiclesViewModel(vehicle, userViewModel, vehicleTypeViewModel);

            return vehicleModel;
            //return vehicle;
        }

        public async Task<IEnumerable<VehiclesViewModel>> GetVehicleByUserIdAsync(Guid userId)
        {
            IEnumerable<Vehicle> vehicles = await work.Vehicles.GetAllAsync(v => v.Where(q => q.UserId.Equals(userId)));
            IEnumerable<Guid> userIds = vehicles.Select(ids => ids.UserId);
            IEnumerable<Guid> vehicleTypeIds = vehicles.Select(ids => ids.VehicleTypeId);
            IEnumerable<User> users = await work.Users.GetAllAsync(q => q.Where(items => userIds.Contains(items.Id)));
            IEnumerable<VehicleType> vehicleTypes = await work.VehicleTypes.GetAllAsync(q => q.Where(items => vehicleTypeIds.Contains(items.Id)));

            IEnumerable<UserViewModel> userViewModels = from user in users
                                                        select new UserViewModel(user);

            IEnumerable<VehicleTypeViewModel> vehicleTypeViewModels = from vehicleType in vehicleTypes
                                                                      select new VehicleTypeViewModel(vehicleType);

            IEnumerable<VehiclesViewModel> listVehicle = from vehicle in vehicles
                                                         join userModel in userViewModels
                                                            on vehicle.UserId equals userModel.Id
                                                         join vehicleTypeModel in vehicleTypeViewModels
                                                            on vehicle.VehicleTypeId equals vehicleTypeModel.Id
                                                         select new VehiclesViewModel(vehicle, userModel, vehicleTypeModel);
            return listVehicle;
        }

        public async Task<VehiclesViewModel> CreateVehicleAsync(VehiclesCreateModel vehicle)
        {
            //check vehicle type id exist 
            Vehicle newVehicle = new Vehicle
            {
                Name = vehicle.Name,
                LicensePlate = vehicle.LicensePlate,
                VehicleTypeId = vehicle.VehicleTypeId,
                UserId = vehicle.UserId,
                IsDeleted = false,
            };


            await work.Vehicles.InsertAsync(newVehicle);
            var result = await work.SaveChangesAsync();

            User user = await work.Users.GetAsync(vehicle.UserId);
            UserViewModel userViewModel = new UserViewModel(user);
            VehicleType vehicleType = await work.VehicleTypes.GetAsync(vehicle.VehicleTypeId);
            VehicleTypeViewModel vehicleTypeViewModel = new VehicleTypeViewModel(vehicleType);
            VehiclesViewModel vehicleView = new VehiclesViewModel(newVehicle, userViewModel, vehicleTypeViewModel);
            if (result > 0)
            {
                return vehicleView;
            }
            return null!;
        }

        public async Task<VehiclesViewModel> UpdateVehicleAsync(Guid id, VehiclesUpdateModel vehiclesUpdate)
        {
            var currentVehicle = await work.Vehicles.GetAsync(id);

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
            User user = await work.Users.GetAsync(currentVehicle.UserId);
            UserViewModel userViewModel = new UserViewModel(user);
            VehicleType vehicleType = await work.VehicleTypes.GetAsync(currentVehicle.VehicleTypeId);
            VehicleTypeViewModel vehicleTypeViewModel = new VehicleTypeViewModel(vehicleType);
            VehiclesViewModel vehicleView = new VehiclesViewModel(currentVehicle, userViewModel, vehicleTypeViewModel);

            return vehicleView!;
        }
    }
}

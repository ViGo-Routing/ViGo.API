using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Models.UserLicenses;
using ViGo.Models.Users;
using ViGo.Models.Vehicles;
using ViGo.Models.VehicleTypes;
using ViGo.Repository.Core;
using ViGo.Repository.Pagination;
using ViGo.Services.Core;

namespace ViGo.Services
{
    public class VehicleServices : BaseServices
    {
        public VehicleServices(IUnitOfWork work, ILogger logger) : base(work, logger)
        {
        }

        public async Task<IPagedEnumerable<VehiclesViewModel>> GetAllVehiclesAsync(
            PaginationParameter pagination,
            HttpContext context,
            CancellationToken cancellationToken)
        {
            IEnumerable<Vehicle> vehicles = await work.Vehicles.GetAllAsync(cancellationToken: cancellationToken);

            int totalRecords = vehicles.Count();

            vehicles = vehicles.ToPagedEnumerable(
                pagination.PageNumber, pagination.PageSize).Data;

            IEnumerable<Guid> userIds = vehicles.Select(ids => ids.UserId);
            IEnumerable<Guid> vehicleTypeIds = vehicles.Select(ids => ids.VehicleTypeId);
            IEnumerable<User> users = await work.Users.GetAllAsync(
                q => q.Where(items => userIds.Contains(items.Id)), cancellationToken: cancellationToken);
            IEnumerable<VehicleType> vehicleTypes = await work.VehicleTypes.GetAllAsync(
                q => q.Where(items => vehicleTypeIds.Contains(items.Id)), cancellationToken: cancellationToken);

            IEnumerable<Guid> userLicenseIds = vehicles.Select(ids => ids.UserLicenseId);
            IEnumerable<UserLicense> userLicenses = await work.UserLicenses.GetAllAsync(
                q => q.Where(items => userLicenseIds.Contains(items.Id)), cancellationToken: cancellationToken);
            IEnumerable<UserLicenseViewModel> userLicenseViews = from userLicense in userLicenses
                                                                 select new UserLicenseViewModel(userLicense);

            IEnumerable<UserViewModel> userViewModels = from user in users
                                                        select new UserViewModel(user);

            IEnumerable<VehicleTypeViewModel> vehicleTypeViewModels = from vehicleType in vehicleTypes
                                                                      select new VehicleTypeViewModel(vehicleType);

            IEnumerable<VehiclesViewModel> listVehicle = from vehicle in vehicles
                                                         join userModel in userViewModels
                                                            on vehicle.UserId equals userModel.Id
                                                         join vehicleTypeModel in vehicleTypeViewModels
                                                            on vehicle.VehicleTypeId equals vehicleTypeModel.Id
                                                         join userLicenseView in userLicenseViews
                                                            on vehicle.UserLicenseId equals userLicenseView.Id
                                                         select new VehiclesViewModel(vehicle, userModel, vehicleTypeModel, userLicenseView);
            return listVehicle.ToPagedEnumerable(pagination.PageNumber,
                pagination.PageSize, totalRecords, context);
        }

        public async Task<VehiclesViewModel> GetVehicleByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            Vehicle vehicle = await work.Vehicles.GetAsync(id, cancellationToken: cancellationToken);
            Guid userId = vehicle.UserId;
            Guid vehicleTypeId = vehicle.VehicleTypeId;
            Guid userLicenceId = vehicle.UserLicenseId;

            User user = await work.Users.GetAsync(userId, cancellationToken: cancellationToken);
            VehicleType vehicleType = await work.VehicleTypes.GetAsync(vehicleTypeId, cancellationToken: cancellationToken);
            UserLicense userLicense = await work.UserLicenses.GetAsync(userLicenceId, cancellationToken: cancellationToken);
            UserViewModel userViewModel = new UserViewModel(user);
            VehicleTypeViewModel vehicleTypeViewModel = new VehicleTypeViewModel(vehicleType);
            UserLicenseViewModel userLicenseView = new UserLicenseViewModel(userLicense);

            VehiclesViewModel vehicleModel = new VehiclesViewModel(vehicle, userViewModel, vehicleTypeViewModel, userLicenseView);

            return vehicleModel;
        }

        public async Task<IEnumerable<VehiclesViewModel>> GetVehicleByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            IEnumerable<Vehicle> vehicles = await work.Vehicles.GetAllAsync(v => v.Where(q => q.UserId.Equals(userId)), cancellationToken: cancellationToken);
            IEnumerable<Guid> userIds = vehicles.Select(ids => ids.UserId);
            IEnumerable<Guid> vehicleTypeIds = vehicles.Select(ids => ids.VehicleTypeId);
            IEnumerable<User> users = await work.Users.GetAllAsync(q => q.Where(items => userIds.Contains(items.Id)), cancellationToken: cancellationToken);
            IEnumerable<VehicleType> vehicleTypes = await work.VehicleTypes.GetAllAsync(q => q.Where(items => vehicleTypeIds.Contains(items.Id)), cancellationToken: cancellationToken);

            IEnumerable<UserViewModel> userViewModels = from user in users
                                                        select new UserViewModel(user);

            IEnumerable<VehicleTypeViewModel> vehicleTypeViewModels = from vehicleType in vehicleTypes
                                                                      select new VehicleTypeViewModel(vehicleType);
            IEnumerable<Guid> userLicenseIds = vehicles.Select(ids => ids.UserLicenseId);
            IEnumerable<UserLicense> userLicenses = await work.UserLicenses.GetAllAsync(
                q => q.Where(items => userLicenseIds.Contains(items.Id)), cancellationToken: cancellationToken);
            IEnumerable<UserLicenseViewModel> userLicenseViews = from userLicense in userLicenses
                                                                 select new UserLicenseViewModel(userLicense);

            IEnumerable<VehiclesViewModel> listVehicle = from vehicle in vehicles
                                                         join userModel in userViewModels
                                                            on vehicle.UserId equals userModel.Id
                                                         join vehicleTypeModel in vehicleTypeViewModels
                                                            on vehicle.VehicleTypeId equals vehicleTypeModel.Id
                                                         join userLicenseView in userLicenseViews
                                                            on vehicle.UserLicenseId equals userLicenseView.Id
                                                         select new VehiclesViewModel(vehicle, userModel, vehicleTypeModel, userLicenseView);
            return listVehicle;
        }

        public async Task<VehiclesViewModel> CreateVehicleAsync(VehiclesCreateModel vehicle, CancellationToken cancellationToken)
        {
            //check vehicle type id exist 
            Vehicle newVehicle = new Vehicle
            {
                Name = vehicle.Name,
                LicensePlate = vehicle.LicensePlate,
                VehicleTypeId = vehicle.VehicleTypeId,
                UserId = vehicle.UserId,
                UserLicenseId = vehicle.UserLicenseId,
                IsDeleted = false,
            };


            await work.Vehicles.InsertAsync(newVehicle, cancellationToken: cancellationToken);
            var result = await work.SaveChangesAsync(cancellationToken: cancellationToken);

            User user = await work.Users.GetAsync(vehicle.UserId, cancellationToken: cancellationToken);
            UserViewModel userViewModel = new UserViewModel(user);
            VehicleType vehicleType = await work.VehicleTypes.GetAsync(vehicle.VehicleTypeId, cancellationToken: cancellationToken);
            VehicleTypeViewModel vehicleTypeViewModel = new VehicleTypeViewModel(vehicleType);
            UserLicense userLicense = await work.UserLicenses.GetAsync(vehicle.UserLicenseId, cancellationToken: cancellationToken);
            UserLicenseViewModel userLicenseView = new UserLicenseViewModel(userLicense);

            VehiclesViewModel vehicleView = new VehiclesViewModel(newVehicle, userViewModel, vehicleTypeViewModel, userLicenseView);
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
                if (vehiclesUpdate.UserLicenseId != null)
                {
                    currentVehicle.UserLicenseId = (Guid)vehiclesUpdate.UserLicenseId;
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
            UserLicense userLicense = await work.UserLicenses.GetAsync(currentVehicle.UserLicenseId);
            UserLicenseViewModel userLicenseView = new UserLicenseViewModel(userLicense);
            VehiclesViewModel vehicleView = new VehiclesViewModel(currentVehicle, userViewModel, vehicleTypeViewModel, userLicenseView);

            return vehicleView!;
        }
    }
}

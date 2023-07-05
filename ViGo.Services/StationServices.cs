using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.Models.Stations;
using ViGo.Repository.Core;
using ViGo.Repository.Pagination;
using ViGo.Services.Core;
using ViGo.Utilities;
using ViGo.Utilities.Google;
using ViGo.Utilities.Validator;

namespace ViGo.Services
{
    public class StationServices : BaseServices
    {
        public StationServices(IUnitOfWork work, ILogger logger) : base(work, logger)
        {
        }

        public async Task<StationViewModel?> GetStationAsync(Guid stationId,
            CancellationToken cancellationToken)
        {
            Station station = await work.Stations.GetAsync(stationId, cancellationToken: cancellationToken);
            if (station == null)
            {
                return null;
            }

            StationViewModel model = new StationViewModel(station);
            return model;

        }

        public async Task<IEnumerable<StationViewModel>> GetMetroStationsAsync(
            CancellationToken cancellationToken)
        {
            IEnumerable<Station> stations = await work.Stations.GetAllAsync(
                query => query.Where(
                    s => s.Type == Domain.Enumerations.StationType.METRO),
                cancellationToken: cancellationToken);

            IEnumerable<StationViewModel> models =
                from station in stations
                select new StationViewModel(station);
            return models;
        }

        public async Task<bool> IsStationInRegionAsync(
            GoogleMapPoint googleMapPoint, CancellationToken cancellationToken)
        {
            PointF pointF = googleMapPoint.ToPointF();
            return MapsUtilities.IsInRegion(pointF);
        }

        public async Task<Station> CreateStationAsync(
            StationCreateModel model, CancellationToken cancellationToken)
        {
            // Check for Name
            model.Name.StringValidate(
                allowEmpty: false,
                emptyErrorMessage: "Tên điểm di chuyển không được bỏ trống!!",
                minLength: 5,
                minLengthErrorMessage: "Tên điểm di chuyển phải có ít nhất 5 kí tự!!",
                maxLength: 255,
                maxLengthErrorMessage: "Tên điểm di chuyển không được vượt quá 255 kí tự!!");

            // Check for Address
            model.Address.StringValidate(
                allowEmpty: false,
                emptyErrorMessage: "Địa chỉ điểm di chuyển không được bỏ trống!!",
                minLength: 5,
                minLengthErrorMessage: "Địa chỉ điểm di chuyển phải có ít nhất 5 kí tự!!",
                maxLength: 500,
                maxLengthErrorMessage: "Địa chỉ điểm di chuyển không được vượt quá 500 kí tự!!");

            // Check for Type
            if (!Enum.IsDefined(model.Type))
            {
                throw new ApplicationException("Loại điểm di chuyển không hợp lệ!!");
            }

            // Check for longtitude and latitude
            if (model.Type == StationType.OTHER &&
                !MapsUtilities.IsInRegion(new PointF((float)model.Latitude, (float)model.Longitude)))
            {
                throw new ApplicationException("Hiện tại ViGo chỉ hỗ trợ trong khu vực TP.HCM và TP.Thủ Đức!");
            }

            Station checkStation = await work.Stations.GetAsync(
                s => ((s.Longitude == model.Longitude
                       && s.Latitude == model.Latitude)
                       || s.Address.ToLower().Equals(model.Address.ToLower()))
                    && s.Status == StationStatus.ACTIVE,
                cancellationToken: cancellationToken);

            if (checkStation != null)
            {
                throw new ApplicationException("Thông tin điểm di chuyển (tọa độ hoặc địa chỉ) " +
                    "đã tồn tại trong hệ thống!!");
            }

            Station station = new Station()
            {
                Latitude = model.Latitude,
                Longitude = model.Longitude,
                Name = model.Name,
                Address = model.Address,
                Type = model.Type,
                Status = StationStatus.ACTIVE,

            };

            await work.Stations.InsertAsync(station, cancellationToken: cancellationToken);
            await work.SaveChangesAsync(cancellationToken);

            return station;
        }

        public async Task<IPagedEnumerable<StationViewModel>> GetStationsAsync(
            PaginationParameter pagination, HttpContext context,
            CancellationToken cancellationToken)
        {
            IEnumerable<Station> stations = await work.Stations.GetAllAsync(cancellationToken: cancellationToken);
            stations = stations.OrderByDescending(s => s.Type);

            int totalRecords = stations.Count();
            stations = stations.ToPagedEnumerable(pagination.PageNumber,
                pagination.PageSize).Data;

            IEnumerable<StationViewModel> models = from station in stations
                                                   select new StationViewModel(station);
            return models.ToPagedEnumerable(
                pagination.PageNumber, pagination.PageSize,
                totalRecords, context);
        }

        public async Task<Station> DeleteStationAsync(Guid stationId,
            CancellationToken cancellationToken)
        {
            Station? station = await work.Stations.GetAsync(stationId, cancellationToken: cancellationToken);
            if (station is null)
            {
                throw new ApplicationException("Điểm di chuyển không tồn tại!!");
            }

            await work.Stations.DeleteAsync(station);
            await work.SaveChangesAsync(cancellationToken);

            return station;
        }

        public async Task<Station> UpdateStationAsync(StationUpdateModel model,
            CancellationToken cancellationToken)
        {
            Station? station = await work.Stations.GetAsync(model.Id, cancellationToken: cancellationToken);
            if (station is null)
            {
                throw new ApplicationException("Điểm di chuyển không tồn tại!!");
            }

            if (model.Name != null)
            {
                // Check for Name
                model.Name.StringValidate(
                    allowEmpty: false,
                    emptyErrorMessage: "Tên điểm di chuyển không được bỏ trống!!",
                    minLength: 5,
                    minLengthErrorMessage: "Tên điểm di chuyển phải có ít nhất 5 kí tự!!",
                    maxLength: 255,
                    maxLengthErrorMessage: "Tên điểm di chuyển không được vượt quá 255 kí tự!!");

                station.Name = model.Name;
            }

            if (model.Address != null)
            {
                // Check for Address
                model.Address.StringValidate(
                    allowEmpty: false,
                    emptyErrorMessage: "Địa chỉ điểm di chuyển không được bỏ trống!!",
                    minLength: 5,
                    minLengthErrorMessage: "Địa chỉ điểm di chuyển phải có ít nhất 5 kí tự!!",
                    maxLength: 500,
                    maxLengthErrorMessage: "Địa chỉ điểm di chuyển không được vượt quá 500 kí tự!!");

                station.Address = model.Address;
            }

            // Check for Type
            if (model.Type != null)
            {
                if (!Enum.IsDefined(model.Type.Value))
                {
                    throw new ApplicationException("Loại điểm di chuyển không hợp lệ!!");
                }

                station.Type = model.Type.Value;
            }

            // Check for Status
            if (model.Status != null)
            {
                if (!Enum.IsDefined(model.Status.Value))
                {
                    throw new ApplicationException("Trạng thái điểm di chuyển không hợp lệ!!");
                }

                station.Status = model.Status.Value;
            }

            // Check for longtitude and latitude
            if (model.Longitude.HasValue && model.Latitude.HasValue)
            {
                if (((model.Type.HasValue && model.Type == StationType.OTHER)
                    || (!model.Type.HasValue && station.Type == StationType.OTHER)) &&
                !MapsUtilities.IsInRegion(new PointF((float)model.Latitude, (float)model.Longitude)))
                {
                    throw new ApplicationException("Hiện tại ViGo chỉ hỗ trợ trong khu vực TP.HCM và TP.Thủ Đức!");
                }

                Station checkStation = await work.Stations.GetAsync(
                s => ((s.Longitude == model.Longitude
                       && s.Latitude == model.Latitude)
                       || (model.Address != null && s.Address.ToLower().Equals(model.Address.ToLower())))
                    && s.Status == StationStatus.ACTIVE,
                cancellationToken: cancellationToken);

                if (checkStation != null)
                {
                    throw new ApplicationException("Thông tin điểm di chuyển (tọa độ hoặc địa chỉ) " +
                        "đã tồn tại trong hệ thống!!");
                }

                station.Longitude = model.Longitude.Value;
                station.Latitude = model.Latitude.Value;
            }
            else if ((model.Longitude.HasValue && !model.Latitude.HasValue)
                || (!model.Longitude.HasValue && model.Latitude.HasValue))
            {
                throw new ApplicationException("Thiếu thông tin về tọa độ điểm di chuyển!!");
            }

            await work.Stations.UpdateAsync(station);
            await work.SaveChangesAsync(cancellationToken);

            return station;

        }
    }
}

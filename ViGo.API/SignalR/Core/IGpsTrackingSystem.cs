using ViGo.Models.GoogleMaps;

namespace ViGo.API.SignalR.Core
{
    public interface IGpsTrackingSystem
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tripId">Booking Detail Id</param>
        /// <param name="googleMapPoint"></param>
        /// <returns></returns>
        Task SendLocation(Guid tripId, GoogleMapPoint googleMapPoint);
    }
}

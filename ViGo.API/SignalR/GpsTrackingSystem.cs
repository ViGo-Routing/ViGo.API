using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using ViGo.API.SignalR.Core;
using ViGo.Models.GoogleMaps;
using ViGo.Utilities.Extensions;

namespace ViGo.API.SignalR
{
    public class GpsTrackingSystem : Hub, IGpsTrackingSystem
    {
        private readonly Dictionary<string, List<Guid>> registerredConnections;
        private readonly ILogger<GpsTrackingSystem> _logger;
        private readonly IHubContext<GpsTrackingSystem> _hubContext;

        public GpsTrackingSystem(ILogger<GpsTrackingSystem> logger,
            IHubContext<GpsTrackingSystem> hubContext)
        {
            registerredConnections = new Dictionary<string, List<Guid>>();
            _logger = logger;
            _hubContext = hubContext;
        }

        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            RemoveConnection(Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        [Authorize(Roles = "DRIVER")]
        public async Task SendLocation(Guid tripId, GoogleMapPoint googleMapPoint)
        {
            try
            {
                _logger.LogInformation("Location received: Lat: {0}, Long: {1}", googleMapPoint.Latitude, googleMapPoint.Longitude);
                await _hubContext.Clients.Group(tripId.ToString())
                    .SendAsync("locationTracking",
                    JsonConvert.SerializeObject(googleMapPoint));
            }
            catch (Exception ex)
            {
                _logger.LogError("Error on sending location. Detail: {0}", ex.GeneratorErrorMessage());
            }

        }

        [Authorize(Roles = "DRIVER,CUSTOMER")]
        public async Task Register(string tripId)
        {
            try
            {
                _logger.LogInformation("Trip is registered on HUB {0}", tripId);

                await RegisterTrip(Context.ConnectionId, tripId);

                await Groups.AddToGroupAsync(Context.ConnectionId, tripId);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error on register user to track location. Detail: {0}", ex.GeneratorErrorMessage());
            }
        }

        private async Task RegisterTrip(string connectionId, string tripId)
        {
            lock (registerredConnections)
            {
                if (registerredConnections.ContainsKey(connectionId))
                {
                    var value = registerredConnections[connectionId];
                    value.Add(Guid.Parse(tripId));
                    value = value.Distinct().ToList();

                    registerredConnections[connectionId] = value;
                }
                else
                {
                    registerredConnections[connectionId] = new List<Guid>
                    {
                        Guid.Parse(tripId)
                    };
                }
            }
        }

        private void RemoveConnection(string connectionId)
        {
            try
            {
                lock (registerredConnections)
                {
                    registerredConnections[connectionId] = null;
                    registerredConnections.Remove(connectionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error on delete connection. Detail: {0}", ex.GeneratorErrorMessage());
            }
        }
    }
}

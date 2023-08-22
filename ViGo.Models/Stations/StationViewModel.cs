using ViGo.Domain;
using ViGo.Domain.Enumerations;

namespace ViGo.Models.Stations
{
    public class StationViewModel
    {
        public Guid Id { get; set; }
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public StationType Type { get; set; }
        public StationStatus Status { get; set; }

        public StationViewModel()
        {

        }

        public StationViewModel(Station station)
        {
            Id = station.Id;
            Longitude = station.Longitude;
            Latitude = station.Latitude;
            Name = station.Name;
            Address = station.Address;
            Type = station.Type;
            Status = station.Status;
        }
    }
}

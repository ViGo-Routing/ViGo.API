using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Domain.Enumerations;

namespace ViGo.Models.Stations
{
    public class StationViewModel
    {
        public double Longtitude { get; set; }
        public double Latitude { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public StationType Type { get; set; }

        public StationViewModel()
        {

        }

        public StationViewModel(Station station)
        {
            Longtitude = station.Longtitude;
            Latitude = station.Latitude;
            Name = station.Name;
            Address = station.Address;
            Type = station.Type;
        }
    }
}

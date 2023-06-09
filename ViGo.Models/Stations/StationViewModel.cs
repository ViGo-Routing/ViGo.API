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
        //public int StationIndex { get; set; }
        public StationStatus Status { get; set; }

        public StationViewModel(Station station)
        {
            Longtitude = station.Longtitude;
            Latitude = station.Latitude;
            Name = station.Name;
            Address = station.Address;
            //StationIndex = stationIndex;
            Status = station.Status;
        }
    }
}

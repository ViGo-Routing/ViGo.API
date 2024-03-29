﻿using ViGo.Domain.Enumerations;

namespace ViGo.Models.Stations
{
    public class StationCreateModel
    {
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public string Name { get; set; } = null!;
        public string Address { get; set; } = null!;
        public StationType Type { get; set; } = StationType.OTHER;
    }
}

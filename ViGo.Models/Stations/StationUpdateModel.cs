using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain.Enumerations;

namespace ViGo.Models.Stations
{
    public class StationUpdateModel
    {
        public Guid Id { get; set; }
        public double? Longitude { get; set; }
        public double? Latitude { get; set; }
        public string? Name { get; set; }
        public string? Address { get; set; }
        public StationType? Type { get; set; }
        public StationStatus? Status { get; set; }
    }
}

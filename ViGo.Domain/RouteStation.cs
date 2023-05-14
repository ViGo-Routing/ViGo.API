﻿using System;
using System.Collections.Generic;
using ViGo.Domain.Enumerations;

namespace ViGo.Domain
{
    public partial class RouteStation
    {
        public RouteStation()
        {
            BookingEndRouteStations = new HashSet<Booking>();
            BookingStartRouteStations = new HashSet<Booking>();
            InverseNextRouteStation = new HashSet<RouteStation>();
        }

        public Guid Id { get; set; }
        public Guid RouteId { get; set; }
        public Guid StationId { get; set; }
        public int StationIndex { get; set; }
        public Guid? NextRouteStationId { get; set; }
        public double? DistanceFromFirstStation { get; set; }
        public double? DurationFromFirstStation { get; set; }
        public RouteStationStatus Status { get; set; }
        public DateTimeOffset CreatedDate { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTimeOffset UpdatedDate { get; set; }
        public Guid UpdatedBy { get; set; }
        public bool IsDeleted { get; set; }

        public virtual User CreatedByNavigation { get; set; } = null!;
        public virtual RouteStation? NextRouteStation { get; set; }
        public virtual Route Route { get; set; } = null!;
        public virtual Station Station { get; set; } = null!;
        public virtual User UpdatedByNavigation { get; set; } = null!;
        public virtual ICollection<Booking> BookingEndRouteStations { get; set; }
        public virtual ICollection<Booking> BookingStartRouteStations { get; set; }
        public virtual ICollection<RouteStation> InverseNextRouteStation { get; set; }
    }
}

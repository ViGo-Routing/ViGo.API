using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.Repository.Core;
using ViGo.Services.Core;
using ViGo.Utilities;
using ViGo.Utilities.Google;

namespace ViGo.Services
{
    public class TripMappingServices : BaseServices
    {
        //private int ROUTE_TYPE_COUNT = Enum.GetNames(typeof(RouteType)).Length;
        public TripMappingServices(IUnitOfWork work, ILogger logger) : base(work, logger)
        {
        }

        /// <summary>
        /// Mapping the whole Booking, every BookingDetails will be considered to map
        /// </summary>
        /// <param name="bookingToMap"></param>
        /// <returns></returns>
        //public async Task MapBooking(Booking bookingToMap,
        //    ILogger logger)
        //{
        //    logger.LogInformation("Background Service: MapBooking is running...");
        //    if (bookingToMap is null)
        //    {
        //        throw new ArgumentNullException(nameof(bookingToMap), "Booking is null!!");
        //    }
        //    logger.LogInformation("\tTry to map for Booking " + bookingToMap.Id);

        //    // Mapping Booking
        //    // Get Start and End Station
        //    Route customerRoute = await work.Routes.GetAsync(bookingToMap.CustomerRouteId);
        //    Station startStation = await work.Stations.GetAsync(customerRoute.StartStationId);
        //    Station endStation = await work.Stations.GetAsync(customerRoute.EndStationId);

        //    IList<BookingDetail> bookingDetails = (await work.BookingDetails
        //        .GetAllAsync(query => query.Where(
        //            bd => bd.BookingId.Equals(bookingToMap.Id))))
        //            .OrderBy(b => b.Date).ToList();

        //    IList<Guid> assignedBookingDetails = new List<Guid>();
        //    IList<Guid> unassignedBookingDetails = new List<Guid>();

        //    IEnumerable<User> drivers = await work.Users.GetAllAsync(
        //        query => query.Where(
        //            u => u.Role == UserRole.DRIVER
        //            && u.Status == UserStatus.ACTIVE));

        //    IEnumerable<Guid> driverIds = drivers.Select(u => u.Id);

        //    Dictionary<Guid, IEnumerable<DriverTripsOfDate>> driverTrips =
        //        new Dictionary<Guid, IEnumerable<DriverTripsOfDate>>();

        //    // Get Drivers Trip of a Date
        //    foreach (Guid driverId in driverIds)
        //    {
        //        IEnumerable<BookingDetail> driverBookingDetails = await
        //            work.BookingDetails.GetAllAsync(query => query.Where(
        //                bd => bd.DriverId.HasValue &&
        //                bd.DriverId.Value.Equals(driverId)));

        //        IEnumerable<DriverTripsOfDate> driverTripsOfDates = new List<DriverTripsOfDate>();

        //        if (driverBookingDetails.Any())
        //        {
        //            IEnumerable<Guid> bookingIds = driverBookingDetails.Select(d => d.BookingId).Distinct();
        //            IEnumerable<Booking> bookings = await work.Bookings.GetAllAsync(
        //                query => query.Where(b => bookingIds.Contains(b.Id)));

        //            IEnumerable<Guid> customerRouteIds = bookings.Select(b => b.CustomerRouteId).Distinct();
        //            IEnumerable<Route> customerRoutes = await work.Routes.GetAllAsync(
        //                query => query.Where(r => customerRouteIds.Contains(r.Id)));

        //            IEnumerable<Guid> stationIds = customerRoutes.Select(r => r.StartStationId)
        //                .Concat(customerRoutes.Select(r => r.EndStationId)).Distinct();
        //            IEnumerable<Station> stations = await work.Stations.GetAllAsync(
        //                query => query.Where(s => stationIds.Contains(s.Id)));

        //            IEnumerable<DateOnly> tripDates = driverBookingDetails.Select(
        //                    d => DateOnly.FromDateTime(d.Date)).Distinct();
        //            foreach (DateOnly tripDate in tripDates)
        //            {
        //                IEnumerable<BookingDetail> tripsInDate = driverBookingDetails.Where(
        //                    bd => DateOnly.FromDateTime(bd.Date) == tripDate);
        //                IEnumerable<DriverTrip> trips = from detail in driverBookingDetails
        //                                                join booking in bookings
        //                                                    on detail.BookingId equals booking.Id
        //                                                join route in customerRoutes
        //                                                    on booking.CustomerRouteId equals route.Id
        //                                                join driverStartStation in stations
        //                                                    on route.StartStationId.Value equals startStation.Id
        //                                                join driverEndStation in stations
        //                                                    on route.EndStationId.Value equals endStation.Id
        //                                                select new DriverTrip()
        //                                                {
        //                                                    BeginTime = detail.BeginTime,
        //                                                    EndTime = DateTimeUtilities.CalculateTripEndTime(detail.BeginTime, booking.Duration),
        //                                                    StartLocation = new GoogleMapPoint()
        //                                                    {
        //                                                        Latitude = driverStartStation.Latitude,
        //                                                        Longtitude = driverStartStation.Longtitude
        //                                                    },
        //                                                    EndLocation = new GoogleMapPoint()
        //                                                    {
        //                                                        Latitude = driverEndStation.Latitude,
        //                                                        Longtitude = driverEndStation.Longtitude
        //                                                    }
        //                                                };

        //                trips = trips.OrderBy(t => t.BeginTime);

        //                driverTripsOfDates = driverTripsOfDates.Append(new DriverTripsOfDate
        //                {
        //                    Date = tripDate,
        //                    Trips = trips.ToList()
        //                });

        //            }

        //            //driverTrips.Add(driverId, driverTripsOfDates);
        //        }
        //        else
        //        {
        //            // No Trip yet
        //            //driverTrips.Add(new KeyValuePair<Guid, DriverTripsOfDate>(driverId))
        //            //driverTrips.Add(driverId, driverTripsOfDates);
        //        }

        //        driverTrips.Add(driverId, driverTripsOfDates.OrderBy(
        //            t => t.Date));
        //    }

        //    IEnumerable<Route> routes = await work.Routes.GetAllAsync(
        //        query => query.Where(
        //            r => driverIds.Contains(r.UserId)
        //            && r.Status == RouteStatus.ACTIVE));

        //    // Filter Route
        //    //IEnumerable<Route> routeSpecificRouteSpecificTime = routes.Where(
        //    //    r => r.RouteType == RouteType.SPECIFIC_ROUTE_SPECIFIC_TIME);

        //    //IEnumerable<Route> routeSpecificRouteEveryTime = routes.Where(
        //    //    r => r.RouteType == RouteType.SPECIFIC_ROUTE_EVERY_TIME);

        //    //IEnumerable<Route> routeEveryRouteSpecificTime = routes.Where(
        //    //    r => r.RouteType == RouteType.EVERY_ROUTE_SPECIFIC_TIME);

        //    //IEnumerable<Route> routeEveryRouteEveryTime = routes.Where(
        //    //    r => r.RouteType == RouteType.EVERY_ROUTE_EVERY_TIME);

        //    IEnumerable<Guid> routeIds = routes.Select(r => r.Id);
        //    IEnumerable<RouteRoutine> routeRoutines = await work.RouteRoutines.GetAllAsync(
        //        query => query.Where(
        //            r => routeIds.Contains(r.RouteId)));

        //    foreach (RouteRoutine routine in routeRoutines)
        //    {
        //        Route route = routes.SingleOrDefault(r => r.Id.Equals(routine.RouteId));
        //        routine.Route = route;
        //    }

        //    foreach (BookingDetail bookingDetail in bookingDetails)
        //    {
        //        TimeSpan bookingDetailEndTime = DateTimeUtilities.CalculateTripEndTime(bookingDetail.BeginTime, bookingToMap.Duration);
                    
        //        IList<DriverMappingItem> prioritizedDrivers = new List<DriverMappingItem>();

        //        IEnumerable<RouteRoutine> routinesByDate = routeRoutines
        //            .Where(r =>
        //            {
        //                //switch (r.Route.RouteType)
        //                //{
        //                //    case RouteType.SPECIFIC_ROUTE_SPECIFIC_TIME:
        //                //        return r.RoutineDate.Value.Date == bookingDetail.Date.Date
        //                //            && r.StartTime.Value <= bookingDetail.BeginTime
        //                //            && r.EndTime.Value >= bookingDetailEndTime;
        //                //        //break;
        //                //    case RouteType.SPECIFIC_ROUTE_EVERY_TIME:
        //                //        return r.RoutineDate.Value.Date == bookingDetail.Date.Date;
        //                //        //break;
        //                //    case RouteType.EVERY_ROUTE_SPECIFIC_TIME:
        //                //        return r.RoutineDate.Value.Date == bookingDetail.Date.Date
        //                //            && r.StartTime.Value <= bookingDetail.BeginTime
        //                //            && r.EndTime.Value >= bookingDetailEndTime;
        //                //    //break;
        //                //    case RouteType.EVERY_ROUTE_EVERY_TIME:
        //                //        return r.RoutineDate.Value.Date == bookingDetail.Date.Date;
        //                //        //break;
        //                //}
        //                //return false;
        //                return r.RoutineDate.Date == bookingDetail.Date.Date
        //                            && r.StartTime <= bookingDetail.BeginTime
        //                            && r.EndTime >= bookingDetailEndTime;
        //            });

        //        if (routinesByDate.Any())
        //        {
        //            foreach (RouteRoutine routineByDate in routinesByDate)
        //            {
        //                Route routineRoute = routes.SingleOrDefault(r => r.Id.Equals(routineByDate.RouteId));
        //                Guid driverId = routineRoute.UserId;

        //                IEnumerable<DriverTripsOfDate>? driverTripsOfDates =
        //                    driverTrips.GetValueOrDefault(driverId);
        //                if (driverTripsOfDates == null
        //                    || !driverTripsOfDates.Any())
        //                {
        //                    // Has not been configured or has no trips
        //                    if (routineByDate.StartTime.HasValue)
        //                    {
        //                        // EVERY_TIME Routes
        //                        prioritizedDrivers.Add(new
        //                            DriverMappingItem(driverId, routineByDate.RouteId, (bookingDetail.BeginTime - routineByDate.StartTime.Value).Minutes));
        //                    } else
        //                    {
        //                        prioritizedDrivers.Add(new
        //                            DriverMappingItem (driverId, routineByDate.RouteId, 
        //                                20 + (short)routineByDate.Route.RouteType)); // Average value
        //                    }
                            
        //                }
        //                else
        //                {
        //                    // Has trips
        //                    DriverTripsOfDate? tripsOfDate = driverTripsOfDates
        //                        .SingleOrDefault(t => t.Date == DateOnly.FromDateTime(bookingDetail.Date));

        //                    if (tripsOfDate == null || tripsOfDate.Trips.Count == 0)
        //                    {
        //                        // No trips in day
        //                        if (routineByDate.StartTime.HasValue)
        //                        {

        //                        prioritizedDrivers.Add(new
        //                            DriverMappingItem(driverId, routineByDate.RouteId, (bookingDetail.BeginTime - routineByDate.StartTime.Value).Minutes));
        //                        } else
        //                        {
        //                            prioritizedDrivers.Add(new DriverMappingItem(driverId, routineByDate.RouteId, 
        //                                25 + (short)routineByDate.Route.RouteType)); // Less prioritized than the one that has entirely no trips
        //                        }
        //                    }
        //                    else
        //                    {
        //                        // Has Trips in day
        //                        DriverTrip lastTrip = tripsOfDate.Trips.LastOrDefault();
        //                        TimeSpan lastTripEndTime = lastTrip.EndTime;
        //                        TimeSpan nextTripEndTime = DateTimeUtilities.CalculateTripEndTime(bookingDetail.BeginTime, bookingToMap.Duration);

        //                        switch (routineByDate.Route.RouteType)
        //                        {
        //                            case RouteType.SPECIFIC_ROUTE_SPECIFIC_TIME:
        //                                if (lastTripEndTime <= bookingDetail.BeginTime &&
        //                                        nextTripEndTime <= routineByDate.EndTime.Value)
        //                                {
        //                                    // TODO Code
        //                                    // Calculate duration to move from last Trip End Location to the Booking Detail Start Location
        //                                    prioritizedDrivers.Add(new DriverMappingItem(driverId, routineByDate.RouteId, (bookingDetail.BeginTime - lastTripEndTime).Minutes));
        //                                }
        //                                break;
        //                            case RouteType.SPECIFIC_ROUTE_EVERY_TIME:
        //                                if (lastTripEndTime <= bookingDetail.BeginTime)
        //                                {
        //                                    // TODO Code
        //                                    // Calculate duration to move from last Trip End Location to the Booking Detail Start Location
        //                                    prioritizedDrivers.Add(new DriverMappingItem(driverId, routineByDate.RouteId, (bookingDetail.BeginTime - lastTripEndTime).Minutes));
        //                                }
        //                                break;
        //                            case RouteType.EVERY_ROUTE_SPECIFIC_TIME:
        //                                if (lastTripEndTime <= bookingDetail.BeginTime &&
        //                                        nextTripEndTime <= routineByDate.EndTime.Value)
        //                                {
        //                                    // TODO Code
        //                                    // Calculate duration to move from last Trip End Location to the Booking Detail Start Location
        //                                    prioritizedDrivers.Add(new DriverMappingItem(driverId, routineByDate.RouteId, (bookingDetail.BeginTime - lastTripEndTime).Minutes));
        //                                }
        //                                break;
        //                            case RouteType.EVERY_ROUTE_EVERY_TIME:
        //                                if (lastTripEndTime <= bookingDetail.BeginTime)
        //                                {
        //                                    // TODO Code
        //                                    // Calculate duration to move from last Trip End Location to the Booking Detail Start Location
        //                                    prioritizedDrivers.Add(new DriverMappingItem(driverId, routineByDate.RouteId, (bookingDetail.BeginTime - lastTripEndTime).Minutes));
        //                                }
        //                                break;
        //                        }

        //                        // TODO Code
        //                        // Calculate for distance prioritization
                                
        //                    }
        //                }
        //            }
        //        }
                
        //        //prioritizedRoutines = (from routine in routinesByDate
        //        //                       select new KeyValuePair<Guid, int>(routine.Id, 
        //        //                            (routine.StartTime.Value - bookingDetail.BeginTime).Minutes)).ToList();

        //        //foreach (RouteRoutine routine in routinesByDate)
        //        //{
        //        //    // TODO code

        //        //}

        //        // Check for most prioritized Driver
        //        if (prioritizedDrivers.Count == 0)
        //        {
        //            // No Driver
        //            unassignedBookingDetails.Add(bookingDetail.Id);

        //        } else
        //        {
        //            prioritizedDrivers = prioritizedDrivers.OrderBy(d => d.PrioritizedPoint).ToList();
        //            DriverMappingItem mostPrioritizedDriver = prioritizedDrivers[0];

        //            bookingDetail.DriverId = mostPrioritizedDriver.DriverId;
        //            bookingDetail.DriverRouteId = mostPrioritizedDriver.DriverRouteId;
        //            bookingDetail.AssignedTime = DateTimeUtilities.GetDateTimeVnNow();
        //            bookingDetail.Status = BookingDetailStatus.ASSIGNED;

        //            // Save to database
        //            await work.BookingDetails.UpdateAsync(bookingDetail, true);

        //            assignedBookingDetails.Add(bookingDetail.Id);

        //            IList<DriverTripsOfDate> currentDriverTripsOfDate =
        //                driverTrips[mostPrioritizedDriver.DriverId].ToList();

        //            if (currentDriverTripsOfDate.Any())
        //            {
        //                DriverTripsOfDate? currentTripsOfDate = currentDriverTripsOfDate
        //                    .SingleOrDefault(t => t.Date == DateOnly.FromDateTime(bookingDetail.Date));
        //                if (currentTripsOfDate == null)
        //                {
        //                    currentTripsOfDate = new DriverTripsOfDate()
        //                    {
        //                        Date = DateOnly.FromDateTime(bookingDetail.Date),
        //                        Trips = new List<DriverTrip>
        //                    {
        //                        new DriverTrip
        //                        {
        //                            BeginTime = bookingDetail.BeginTime,
        //                            EndTime = DateTimeUtilities.CalculateTripEndTime(bookingDetail.BeginTime, bookingToMap.Duration),
        //                            StartLocation = new GoogleMapPoint
        //                            {
        //                                Latitude = startStation.Latitude,
        //                                Longtitude = endStation.Longtitude
        //                            },
        //                            EndLocation = new GoogleMapPoint
        //                            {
        //                                Latitude = endStation.Latitude,
        //                                Longtitude = endStation.Longtitude
        //                            }
        //                        }
        //                    }
        //                    };
        //                    currentDriverTripsOfDate.Add(currentTripsOfDate);
        //                    driverTrips[mostPrioritizedDriver.DriverId] = currentDriverTripsOfDate;
        //                }
        //                else
        //                {
        //                    IList<DriverTrip> tripsOfDate = currentTripsOfDate.Trips;
        //                    tripsOfDate.Add(new DriverTrip
        //                    {
        //                        BeginTime = bookingDetail.BeginTime,
        //                        EndTime = DateTimeUtilities.CalculateTripEndTime(bookingDetail.BeginTime, bookingToMap.Duration),
        //                        StartLocation = new GoogleMapPoint
        //                        {
        //                            Latitude = startStation.Latitude,
        //                            Longtitude = endStation.Longtitude
        //                        },
        //                        EndLocation = new GoogleMapPoint
        //                        {
        //                            Latitude = endStation.Latitude,
        //                            Longtitude = endStation.Longtitude
        //                        }
        //                    });

        //                    driverTrips[mostPrioritizedDriver.DriverId] = currentDriverTripsOfDate;
        //                }
        //            }
        //            else
        //            {
        //                // Nothing yet
        //                currentDriverTripsOfDate.Add(
        //                    new DriverTripsOfDate()
        //                    {
        //                        Date = DateOnly.FromDateTime(bookingDetail.Date),
        //                        Trips = new List<DriverTrip>
        //                        {
        //                        new DriverTrip
        //                        {
        //                            BeginTime = bookingDetail.BeginTime,
        //                            EndTime = DateTimeUtilities.CalculateTripEndTime(bookingDetail.BeginTime, bookingToMap.Duration),
        //                            StartLocation = new GoogleMapPoint
        //                            {
        //                                Latitude = startStation.Latitude,
        //                                Longtitude = endStation.Longtitude
        //                            },
        //                            EndLocation = new GoogleMapPoint
        //                            {
        //                                Latitude = endStation.Latitude,
        //                                Longtitude = endStation.Longtitude
        //                            }
        //                        }
        //                        }
        //                    });
        //                driverTrips[mostPrioritizedDriver.DriverId] = currentDriverTripsOfDate;
        //            }
        //        }

        //        // END of BookingDetail
        //    }

        //    // Database Save
        //    await work.SaveChangesAsync();

        //    // End Mapping Booking

        //    logger.LogInformation("\tDone mapping for Booking " + bookingToMap.Id);
        //    logger.LogInformation($"\t\tMap successfully for {assignedBookingDetails.Count} Booking Details");
        //    logger.LogInformation($"\t\tMap unsuccessfully for {unassignedBookingDetails.Count} Booking Details. " +
        //        $"List: {string.Join(", ", unassignedBookingDetails)}");
        //}

    }

    public class DriverTrip : IEquatable<DriverTrip>
    {
        public Guid Id { get; set; }
        public TimeSpan BeginTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public GoogleMapPoint StartLocation { get; set; }
        public GoogleMapPoint EndLocation { get; set; }

        public bool Equals(DriverTrip? other)
        {
            if (other is null)
            {
                return false;
            }

            return this.Id.Equals(other.Id);
        }
    }

    public class DriverTripsOfDate
    {
        public DateOnly Date { get; set; }
        public IList<DriverTrip> Trips { get; set; }
    }

    //public class DriverMappingItem
    //{
    //    public Guid DriverId { get; set; }
    //    public Guid DriverRouteId { get; set; }
    //    public int PrioritizedPoint { get; set; }

    //    public DriverMappingItem(Guid driverId, Guid driverRouteId, int prioritizedPoint)
    //    {
    //        DriverId = driverId;
    //        DriverRouteId = driverRouteId;
    //        PrioritizedPoint = prioritizedPoint;
    //    }
    //}
    public class DriverMappingItem
    {
        public BookingDetail BookingDetail { get; set; }
        public double PrioritizedPoint { get; set; }

        public DriverMappingItem(BookingDetail bookingDetail, double prioritizedPoint)
        {
            BookingDetail = bookingDetail;
            PrioritizedPoint = prioritizedPoint;
        }
    }
}

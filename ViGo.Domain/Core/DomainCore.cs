using ViGo.Domain.Core;

namespace ViGo.Domain
{
    #region Domain classes
    // Make sure all domain classes inherit the BaseEntity class and necessary interfaces
    public partial class Booking
        : BaseEntity, ITrackingCreated, ITrackingUpdated, ISoftDeletedEntity
    {

    }

    public partial class BookingDetail
        : BaseEntity, ITrackingCreated, ITrackingUpdated
    {

    }

    //public partial class Event
    //    : BaseEntity, ITrackingCreated, ITrackingUpdated, ISoftDeletedEntity
    //{

    //}

    public partial class Fare
        : BaseEntity, ITrackingCreated, ITrackingUpdated, ISoftDeletedEntity
    {

    }

    public partial class FarePolicy
        : BaseEntity, ITrackingCreated, ITrackingUpdated, ISoftDeletedEntity
    {

    }

    public partial class Notification
        : BaseEntity, ITrackingCreated, ITrackingUpdated, ISoftDeletedEntity
    {

    }

    //public partial class Promotion
    //    : BaseEntity, ITrackingCreated, ITrackingUpdated, ISoftDeletedEntity
    //{

    //}

    public partial class Report
        : BaseEntity, ITrackingCreated, ITrackingUpdated, ISoftDeletedEntity
    {

    }

    public partial class Route
        : BaseEntity, ITrackingCreated, ITrackingUpdated, ISoftDeletedEntity
    {

    }

    public partial class RouteRoutine
        : BaseEntity, ITrackingCreated, ITrackingUpdated, ISoftDeletedEntity,
        IEquatable<RouteRoutine>
    {
        public bool Equals(RouteRoutine? other)
        {
            if (other == null)
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            return this.RoutineDate == other.RoutineDate
                && this.PickupTime == other.PickupTime
                /*&& this.EndTime == other.EndTime*/;
        }
    }

    public partial class Setting
        : BaseEntity
    {

    }

    public partial class Station
        : BaseEntity, ITrackingCreated, ITrackingUpdated, ISoftDeletedEntity
    {

    }

    public partial class User
        : BaseEntity, ITrackingCreated, ITrackingUpdated
    {

    }

    public partial class UserLicense
        : BaseEntity, ITrackingCreated, ITrackingUpdated, ISoftDeletedEntity
    {

    }

    public partial class Vehicle
        : BaseEntity, ITrackingCreated, ITrackingUpdated, ISoftDeletedEntity
    {

    }

    public partial class VehicleType
        : BaseEntity, ITrackingCreated, ITrackingUpdated, ISoftDeletedEntity
    {

    }

    public partial class Wallet
        : BaseEntity, ITrackingCreated, ITrackingUpdated, ISoftDeletedEntity
    {

    }

    public partial class WalletTransaction
        : BaseEntity, ITrackingCreated, ITrackingUpdated, ISoftDeletedEntity
    {

    }
    #endregion
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViGo.Domain.Core
{
    // Base Class for Entity
    public abstract class BaseEntity
    {
        public abstract Guid Id { get; set; }
    }

    #region Interfaces
    public interface ISoftDeletedEntity
    {
        bool IsDeleted { get; set; }
    }

    public interface ITrackingCreated
    {
        Guid CreatedBy { get; set; }
        DateTime CreatedTime { get; set; }
    }

    public interface ITrackingUpdated
    {
        Guid UpdatedBy { get; set; }
        DateTime UpdatedTime { get; set; }
    }
    #endregion
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViGo.Domain.Core
{
    // Base Class for Entity
    public class BaseEntity
    {
        public Guid Id { get; set; }
    }

    #region Interfaces
    public interface ISoftDeletedEntity
    {
        bool IsDeleted { get; set; }
    }

    public interface ITrackingCreated
    {
        Guid CreatedBy { get; set; }
        DateTimeOffset CreatedDate { get; set; }
    }

    public interface ITrackingUpdated
    {
        Guid UpdatedBy { get; set; }
        DateTimeOffset UpdatedDate { get; set; }
    }
    #endregion
}

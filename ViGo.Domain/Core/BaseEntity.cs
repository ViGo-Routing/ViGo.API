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
    }

    #region Interfaces
    public interface ISoftDeletedEntity
    {
        bool IsDeleted { get; set; }
    }

    public interface ITrackingCreated
    {
        string CreatedBy { get; set; }
        DateTime CreatedDate { get; set; }
    }

    public interface ITrackingUpdated
    {
        string UpdatedBy { get; set; }
        DateTime UpdatedDate { get; set; }
    }
    #endregion
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViGo.Repository.Core
{
    public interface IUnitOfWork : IDisposable
    {
        #region Repositories
        #endregion

        /// <summary>
        /// Save changes to database
        /// </summary>
        /// <returns></returns>
        Task<int> SaveChangesAsync();
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Linq.Expressions;
using ViGo.Domain;
using ViGo.Domain.Core;

namespace ViGo.Repository.Core
{
    public abstract partial class BaseRepository<TEntity> : IRepository<TEntity>
        where TEntity : BaseEntity
    {

        #region Properties

        /// <summary>
        /// Database Context
        /// </summary>
        protected abstract ViGoDBContext Context { get; }

        /// <summary>
        /// Current table DbSet
        /// </summary>
        protected abstract DbSet<TEntity> Table { get; }

        /// <summary>
        /// Cache
        /// </summary>
        protected abstract IDistributedCache cache { get; }
        #endregion

        #region Methods

        /// <summary>
        /// Get the entity entry
        /// </summary>
        /// <param name="predicate">Predicate expression which will return a boolean value to determine the 
        /// single entry to get. If there are more than one entry, an exception will be thrown!
        /// </param>
        /// <param name="includeDeleted">Boolean value which will determine whether or not the returned result should
        /// contain the soft-deleted entities
        /// </param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains the entity entry
        /// </returns>
        public abstract Task<TEntity> GetAsync(
            Expression<Func<TEntity, bool>> predicate,
            bool includeDeleted = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get the entity entry
        /// </summary>
        /// <param name="id">ID Guid value of the entity</param>
        /// <param name="includeDeleted">Boolean value which will determine whether or not the returned result should
        /// contain the soft-deleted entities
        /// </param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains the entity entry
        /// </returns>
        public abstract Task<TEntity> GetAsync(
            Guid id,
            bool includeDeleted = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all entity entries based on the query
        /// </summary>
        /// <param name="func">Function to select entries</param>
        /// <param name="includeDeleted">Boolean value which will determine whether or not the returned result should
        /// contain the soft-deleted entities
        /// </param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains the entity entries
        /// </returns>
        public abstract Task<IEnumerable<TEntity>> GetAllAsync(
            Func<IQueryable<TEntity>, IQueryable<TEntity>> func,
            bool includeDeleted = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all entity entries
        /// </summary>
        /// <param name="includeDeleted">Boolean value which will determine whether or not the returned result should
        /// contain the soft-deleted entities
        /// </param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains the entity entries
        /// </returns>
        public abstract Task<IEnumerable<TEntity>> GetAllAsync(
            bool includeDeleted = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Insert a new entity entry
        /// </summary>
        /// <param name="entity">Entity entry to be inserted</param>
        /// <param name="isSelfCreatedEntity">Boolean value which will determine whether or not
        /// the entity being inserted is a self-created one. CreatedBy and UpdatedBy will be the same 
        /// as entity's Id</param>
        /// <param name="isManuallyAssignTracking">Boolean value which will determine whether or not 
        /// the entity being inserted has CreatedDate and UpdatedDate manually assigned by model
        /// </param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contain the inserted entity entry
        /// </returns>
        public abstract Task<TEntity> InsertAsync(TEntity entity,
            bool isSelfCreatedEntity = false,
            bool isManuallyAssignTracking = false,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Insert a number of entity entries
        /// </summary>
        /// <param name="entities">Entity entries to be inserted</param>
        /// <param name="isSelfCreatedEntity">Boolean value which will determine whether or not
        /// the entity being inserted is a self-created one. CreatedBy and UpdatedBy will be the same 
        /// as entity's Id
        /// </param>
        /// <param name="isManuallyAssignTracking">Boolean value which will determine whether or not 
        /// the entity being inserted has CreatedDate and UpdatedDate manually assigned by model
        /// </param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contain the inserted entity entries
        /// </returns>
        public abstract Task<IEnumerable<TEntity>> InsertAsync(
            IList<TEntity> entities,
            bool isSelfCreatedEntity = false,
            bool isManuallyAssignTracking = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete the entity entry
        /// </summary>
        /// <param name="entity">The entity entry to be deleted</param>
        /// <param name="isSoftDelete">Boolean value which will determine whether or not 
        /// the Delete action should be a soft delete or not
        /// </param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// </returns>
        public abstract Task DeleteAsync(TEntity entity,
            bool isSoftDelete = true,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete the entity entry
        /// </summary>
        /// <param name="predicate">Predicate expression which will return a boolean value to determine the 
        /// single entry to get. If there are more than one entry, an exception will be thrown!
        /// </param>
        /// <param name="isSoftDelete">Boolean value which will determine whether or not 
        /// the Delete action should be a soft delete or not
        /// </param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// </returns>
        public abstract Task DeleteAsync(Expression<Func<TEntity, bool>> predicate,
            bool isSoftDelete = true,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Update the entity entry
        /// </summary>
        /// <param name="entity">Entity entry to be updated</param>
        /// <param name="isManuallyAssignTracking">Boolean value which will determine whether or not 
        /// the entity being inserted has CreatedDate and UpdatedDate manually assigned by model
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contain the inserted entity entries
        /// </returns>
        public abstract Task<TEntity> UpdateAsync(TEntity entity,
            bool isManuallyAssignTracking = false);

        /// <summary>
        /// Detach an entity from the Change Tracker
        /// </summary>
        /// <param name="entity">The entity to be detached</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public abstract Task DetachAsync(TEntity entity);

        public abstract Task SaveChangesToRedisAsync(CancellationToken cancellationToken);
        #endregion
    }
}

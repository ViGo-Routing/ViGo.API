using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
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
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains the entity entry
        /// </returns>
        public abstract Task<TEntity> GetAsync(
            Expression<Func<TEntity, bool>> predicate,
            bool includeDeleted = false);

        /// <summary>
        /// Get the entity entry
        /// </summary>
        /// <param name="id">ID Guid value of the entity</param>
        /// <param name="includeDeleted">Boolean value which will determine whether or not the returned result should
        /// contain the soft-deleted entities
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains the entity entry
        /// </returns>
        public abstract Task<TEntity> GetAsync(
            Guid id,
            bool includeDeleted = false);

        /// <summary>
        /// Get all entity entries based on the query
        /// </summary>
        /// <param name="func">Function to select entries</param>
        /// <param name="includeDeleted">Boolean value which will determine whether or not the returned result should
        /// contain the soft-deleted entities
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains the entity entries
        /// </returns>
        public abstract Task<IEnumerable<TEntity>> GetAllAsync(
            Func<IQueryable<TEntity>, IQueryable<TEntity>> func,
            bool includeDeleted = false);

        /// <summary>
        /// Get all entity entries
        /// </summary>
        /// <param name="includeDeleted">Boolean value which will determine whether or not the returned result should
        /// contain the soft-deleted entities
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains the entity entries
        /// </returns>
        public abstract Task<IEnumerable<TEntity>> GetAllAsync(
            bool includeDeleted = false);

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
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contain the inserted entity entry
        /// </returns>
        public abstract Task<TEntity> InsertAsync(TEntity entity,
            bool isSelfCreatedEntity = false,
            bool isManuallyAssignTracking = false);

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
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contain the inserted entity entries
        /// </returns>
        public abstract Task<IEnumerable<TEntity>> InsertAsync(
            IEnumerable<TEntity> entities,
            bool isSelfCreatedEntity = false,
            bool isManuallyAssignTracking = false);

        /// <summary>
        /// Delete the entity entry
        /// </summary>
        /// <param name="entity">The entity entry to be deleted</param>
        /// <param name="isSoftDelete">Boolean value which will determine whether or not 
        /// the Delete action should be a soft delete or not
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// </returns>
        public abstract Task DeleteAsync(TEntity entity,
            bool isSoftDelete = true);

        /// <summary>
        /// Delete the entity entry
        /// </summary>
        /// <param name="predicate">Predicate expression which will return a boolean value to determine the 
        /// single entry to get. If there are more than one entry, an exception will be thrown!
        /// </param>
        /// <param name="isSoftDelete">Boolean value which will determine whether or not 
        /// the Delete action should be a soft delete or not
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// </returns>
        public abstract Task DeleteAsync(Expression<Func<TEntity, bool>> predicate,
            bool isSoftDelete = true);

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
        #endregion
    }
}

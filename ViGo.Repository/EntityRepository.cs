using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Domain.Core;
using ViGo.Repository.Core;
using ViGo.Utilities;

namespace ViGo.Repository
{
    public class EntityRepository<TEntity> : BaseRepository<TEntity>
        where TEntity : BaseEntity
    {
        #region Properties

        /// <summary>
        /// Database Context
        /// </summary>
        protected override ViGoDBContext Context { get; }

        /// <summary>
        /// Current table DbSet
        /// </summary>
        protected override DbSet<TEntity> Table { get; }
        #endregion

        #region Constructor
        public EntityRepository(ViGoDBContext context)
        {
            Context = context;
            Table = context.Set<TEntity>();
        }
        #endregion

        #region Private Utilities
        protected IQueryable<TEntity> AddDeletedFilter(
            IQueryable<TEntity> query, in bool includeDeleted = false
            )
        {
            if (includeDeleted)
            {
                return query;
            }

            if (!IsTypeOf(nameof(ISoftDeletedEntity)))
            {
                return query;
            }

            return query
                .AsNoTracking()
                .OfType<ISoftDeletedEntity>()
                .Where(entity => !entity.IsDeleted)
                .OfType<TEntity>();
        }

        protected bool IsTypeOf(string interfaceName)
        {
            return typeof(TEntity).GetInterface(interfaceName) != null;
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Delete the entity entry
        /// </summary>
        /// <param name="entity">The entity entry to be deleted</param>
        /// <param name="includeDeleted">Boolean value which will determine whether or not the returned result should
        /// contain the soft-deleted entities
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// </returns>
        public override async Task DeleteAsync(TEntity entity,
            bool isSoftDelete = true)
        {
            if (!isSoftDelete)
            {
                Table.Remove(entity);
                return;
            }

            switch (entity)
            {
                case null:
                    throw new ArgumentNullException(nameof(entity));

                case ISoftDeletedEntity softDeletedEntity:
                    if (!softDeletedEntity.IsDeleted)
                    {
                        softDeletedEntity.IsDeleted = true;

                        if (entity is ITrackingUpdated)
                        {
                            ((ITrackingUpdated)entity).UpdatedDate = DateTimeUtilities.GetDateTimeVnNow();
                            ((ITrackingUpdated)(entity)).UpdatedBy = IdentityUtilities.GetCurrentUserId();
                        }
                        Table.Update(entity);
                    }
                    break;

                default:
                    Table.Remove(entity);
                    break;
            }
        }

        /// <summary>
        /// Delete the entity entry
        /// </summary>
        /// <param name="predicate">Predicate expression which will return a boolean value to determine the 
        /// single entry to get. If there are more than one entry, an exception will be thrown!
        /// </param>
        /// <param name="includeDeleted">Boolean value which will determine whether or not the returned result should
        /// contain the soft-deleted entities
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// </returns>
        public override async Task DeleteAsync(
            Expression<Func<TEntity, bool>> predicate,
            bool isSoftDelete = true)
        {
            TEntity entity = await Table
                .SingleOrDefaultAsync(predicate);
        }

        /// <summary>
        /// Detach an entity from the Change Tracker
        /// </summary>
        /// <param name="entity">The entity to be detached</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public override async Task DetachAsync(TEntity entity)
        {
            Context.Entry(entity).State = EntityState.Detached;
        }

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
        public override async Task<IEnumerable<TEntity>> GetAllAsync(
            Func<IQueryable<TEntity>, IQueryable<TEntity>> func,
            bool includeDeleted = false)
        {
            IQueryable<TEntity> query = AddDeletedFilter(Table,
                includeDeleted);
            query = func(query);

            return await query.ToListAsync();
        }

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
        public override async Task<IEnumerable<TEntity>> GetAllAsync(
            bool includeDeleted = false)
        {
            IQueryable<TEntity> query = AddDeletedFilter(Table,
                includeDeleted);
            return await query.ToListAsync();
        }

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
        public override async Task<TEntity> GetAsync(
            Expression<Func<TEntity, bool>> predicate,
            bool includeDeleted = false)
        {
            IQueryable<TEntity> query = AddDeletedFilter(Table,
                includeDeleted);

            TEntity entity = await query.SingleOrDefaultAsync(predicate);
            return entity;
        }

        /// <summary>
        /// Get the entity entry
        /// </summary>
        /// <param name="id">ID string value of the entity</param>
        /// <param name="includeDeleted">Boolean value which will determine whether or not the returned result should
        /// contain the soft-deleted entities
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains the entity entry
        /// </returns>
        public override async Task<TEntity> GetAsync(
            string id,
            bool includeDeleted = false)
        {
            IQueryable<TEntity> query = AddDeletedFilter(Table,
                includeDeleted);

            TEntity entity = await query.SingleOrDefaultAsync(
                e => e.Id.Equals(Guid.Parse(id)));

            return entity;
        }

        /// <summary>
        /// Insert a new entity entry
        /// </summary>
        /// <param name="entity">Entity entry to be inserted</param>
        /// <param name="includeDeleted">Boolean value which will determine whether or not the returned result should
        /// contain the soft-deleted entities
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contain the inserted entity entry
        /// </returns>
        public override async Task<TEntity> InsertAsync(TEntity entity,
            bool isManuallyAssignDate = false)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(typeof(TEntity).ToString());
            }

            entity.Id = Guid.NewGuid();

            if (!isManuallyAssignDate)
            {
                DateTimeOffset vnNow = DateTimeUtilities.GetDateTimeVnNow();

                if (entity is ITrackingCreated)
                {
                    ((ITrackingCreated)entity).CreatedDate = vnNow;
                    ((ITrackingCreated)entity).CreatedBy = IdentityUtilities.GetCurrentUserId();
                }
                if (entity is ITrackingUpdated)
                {
                    ((ITrackingUpdated)entity).UpdatedDate = vnNow;
                    ((ITrackingUpdated)entity).UpdatedBy = IdentityUtilities.GetCurrentUserId();
                }
            }

            if (entity is ISoftDeletedEntity)
            {
                ((ISoftDeletedEntity)entity).IsDeleted = false;
            }

            await Table.AddAsync(entity);

            return entity;
        }

        /// <summary>
        /// Insert a number of entity entries
        /// </summary>
        /// <param name="entities">Entity entries to be inserted</param>
        /// <param name="includeDeleted">Boolean value which will determine whether or not the returned result should
        /// contain the soft-deleted entities
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contain the inserted entity entries
        /// </returns>
        public override async Task<IEnumerable<TEntity>> InsertAsync(
            IEnumerable<TEntity> entities,
            bool isManuallyAssignDate = false)
        {
            if (entities == null)
            {
                throw new ArgumentNullException(nameof(entities));
            }

            if (entities.Count() > 0)
            {
                DateTimeOffset vnNow = DateTimeUtilities.GetDateTimeVnNow();

                foreach (TEntity entity in entities)
                {
                    entity.Id = Guid.NewGuid();

                    if (!isManuallyAssignDate)
                    {
                        if (entity is ITrackingCreated)
                        {
                            ((ITrackingCreated)entity).CreatedDate = vnNow;
                            ((ITrackingCreated)entity).CreatedBy = IdentityUtilities.GetCurrentUserId();
                        }
                        if (entity is ITrackingUpdated)
                        {
                            ((ITrackingUpdated)entity).UpdatedDate = vnNow;
                            ((ITrackingUpdated)entity).UpdatedBy = IdentityUtilities.GetCurrentUserId();
                        }
                    }

                    if (entity is ISoftDeletedEntity)
                    {
                        ((ISoftDeletedEntity)entity).IsDeleted = false;
                    }
                }



                await Table.AddRangeAsync(entities);
            }
            return entities;
        }

        /// <summary>
        /// Update the entity entry
        /// </summary>
        /// <param name="entity">Entity entry to be updated</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contain the inserted entity entries
        /// </returns>
        public override async Task<TEntity> UpdateAsync(TEntity entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            if (entity is ITrackingUpdated)
            {
                ((ITrackingUpdated)entity).UpdatedDate = DateTimeUtilities.GetDateTimeVnNow();
                ((ITrackingUpdated)entity).UpdatedBy = IdentityUtilities.GetCurrentUserId();
            }


            Table.Update(entity);

            return entity;
        }
        #endregion
    }
}

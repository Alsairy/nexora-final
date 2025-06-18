using Microsoft.EntityFrameworkCore;
using Nexora.Core.Data;
using Nexora.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Nexora.Infrastructure.Repositories
{
    public class BaseRepository<T> : IRepository<T> where T : class, IEntity
    {
        protected readonly NexoraDbContext _dbContext;

        public BaseRepository(NexoraDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<T> GetByIdAsync(int id)
        {
            return await _dbContext.Set<T>().FindAsync(id);
        }

        public async Task<IReadOnlyList<T>> GetAllAsync()
        {
            return await _dbContext.Set<T>().ToListAsync();
        }

        public async Task<IReadOnlyList<T>> GetPagedAsync(int page, int pageSize)
        {
            // Added deterministic ordering with ThenBy(e => e.Id) to ensure consistent paging
            return await _dbContext.Set<T>()
                .OrderBy(e => e.CreatedAt)
                .ThenBy(e => e.Id) // Deterministic ordering
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<T>> GetPagedAsync(int page, int pageSize, Expression<Func<T, bool>> filter)
        {
            // Added deterministic ordering with ThenBy(e => e.Id) to ensure consistent paging
            return await _dbContext.Set<T>()
                .Where(filter)
                .OrderBy(e => e.CreatedAt)
                .ThenBy(e => e.Id) // Deterministic ordering
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<T>> GetPagedAsync<TKey>(int page, int pageSize, Expression<Func<T, bool>> filter, 
            Expression<Func<T, TKey>> orderBy, bool ascending = true)
        {
            // Added deterministic ordering with ThenBy(e => e.Id) to ensure consistent paging
            var query = _dbContext.Set<T>().Where(filter);
            
            if (ascending)
            {
                query = query.OrderBy(orderBy).ThenBy(e => e.Id); // Deterministic ordering
            }
            else
            {
                query = query.OrderByDescending(orderBy).ThenBy(e => e.Id); // Deterministic ordering
            }
            
            return await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<T> AddAsync(T entity)
        {
            await _dbContext.Set<T>().AddAsync(entity);
            await _dbContext.SaveChangesAsync();
            return entity;
        }

        public async Task UpdateAsync(T entity)
        {
            _dbContext.Entry(entity).State = EntityState.Modified;
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(T entity)
        {
            _dbContext.Set<T>().Remove(entity);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<int> CountAsync(Expression<Func<T, bool>> filter = null)
        {
            if (filter == null)
            {
                return await _dbContext.Set<T>().CountAsync();
            }
            
            return await _dbContext.Set<T>().CountAsync(filter);
        }
    }
}


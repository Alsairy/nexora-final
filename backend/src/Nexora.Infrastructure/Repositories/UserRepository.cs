using Microsoft.EntityFrameworkCore;
using Nexora.Core.Data;
using Nexora.Core.Entities;
using Nexora.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexora.Infrastructure.Repositories
{
    public class UserRepository : BaseRepository<User>, IUserRepository
    {
        public UserRepository(NexoraDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<User> GetByEmailAsync(string email)
        {
            return await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<IReadOnlyList<User>> GetByRoleAsync(string role, int page, int pageSize)
        {
            // Added deterministic ordering with ThenBy(e => e.Id) to ensure consistent paging
            return await _dbContext.Users
                .Where(u => u.Role == role)
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ThenBy(u => u.Id) // Deterministic ordering
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<User>> SearchAsync(string searchTerm, int page, int pageSize)
        {
            // Added deterministic ordering with ThenBy(e => e.Id) to ensure consistent paging
            return await _dbContext.Users
                .Where(u => u.FirstName.Contains(searchTerm) || 
                           u.LastName.Contains(searchTerm) || 
                           u.Email.Contains(searchTerm))
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ThenBy(u => u.Id) // Deterministic ordering
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<bool> IsEmailUniqueAsync(string email, int? excludeUserId = null)
        {
            if (excludeUserId.HasValue)
            {
                return !await _dbContext.Users
                    .AnyAsync(u => u.Email == email && u.Id != excludeUserId.Value);
            }
            
            return !await _dbContext.Users
                .AnyAsync(u => u.Email == email);
        }

        public async Task<IReadOnlyList<User>> GetRecentlyActiveUsersAsync(int count)
        {
            // Added deterministic ordering with ThenBy(e => e.Id) to ensure consistent paging
            return await _dbContext.Users
                .OrderByDescending(u => u.LastLoginAt)
                .ThenBy(u => u.Id) // Deterministic ordering
                .Take(count)
                .ToListAsync();
        }
    }
}


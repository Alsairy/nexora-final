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

        public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
        }

        public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == username, cancellationToken);
        }

        public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Users
                .AnyAsync(u => u.Email == email, cancellationToken);
        }

        public async Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Users
                .AnyAsync(u => u.Email == username, cancellationToken);
        }

        public async Task<IEnumerable<User>> GetByTenantAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Users
                .Where(u => u.TenantId.ToString() == tenantId)
                .ToListAsync(cancellationToken);
        }

        public async Task<User?> GetWithRolesAsync(string userId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Id.ToString() == userId, cancellationToken);
        }

        public async Task UpdateLastLoginAsync(string userId, DateTime lastLogin, CancellationToken cancellationToken = default)
        {
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Id.ToString() == userId, cancellationToken);
            
            if (user != null)
            {
                user.UpdatedAt = lastLogin; // Using UpdatedAt to track last login since LastLoginAt was removed
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<IEnumerable<User>> SearchUsersAsync(string searchTerm, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Users
                .Where(u => u.FirstName.Contains(searchTerm) || 
                           u.LastName.Contains(searchTerm) || 
                           u.Email.Contains(searchTerm))
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ThenBy(u => u.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<User>> GetByRoleAsync(string role, CancellationToken cancellationToken = default)
        {
            // Added deterministic ordering with ThenBy(e => e.Id) to ensure consistent paging
            return await _dbContext.Users
                .Where(u => u.Role == role)
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ThenBy(u => u.Id)
                .ToListAsync(cancellationToken);
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
                .OrderByDescending(u => u.UpdatedAt ?? u.CreatedAt) // Using UpdatedAt as fallback since LastLoginAt was removed
                .ThenBy(u => u.Id) // Deterministic ordering
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<User>> GetUsersAsync(int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Users
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ThenBy(u => u.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }
    }
}


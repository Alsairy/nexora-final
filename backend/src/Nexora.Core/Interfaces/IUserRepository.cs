using Nexora.Core.Entities;

namespace Nexora.Core.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetByTenantAsync(string tenantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetByRoleAsync(string role, CancellationToken cancellationToken = default);
    Task<User?> GetWithRolesAsync(string userId, CancellationToken cancellationToken = default);
    Task UpdateLastLoginAsync(string userId, DateTime lastLogin, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> SearchUsersAsync(string searchTerm, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetUsersAsync(int page = 1, int pageSize = 10, CancellationToken cancellationToken = default);
}

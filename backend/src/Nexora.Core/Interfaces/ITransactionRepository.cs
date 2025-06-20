using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexora.Core.Entities;
using Nexora.Core.DTOs;

namespace Nexora.Core.Interfaces
{
    public interface ITransactionRepository : IRepository<Transaction>
    {
        Task<IEnumerable<Transaction>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Transaction>> GetByTenantIdAsync(int tenantId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Transaction>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, int tenantId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Transaction>> GetByStatusAsync(string status, int tenantId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Transaction>> GetByAmountRangeAsync(decimal minAmount, decimal maxAmount, int tenantId, CancellationToken cancellationToken = default);
        Task<decimal> GetTotalAmountByTenantAsync(int tenantId, CancellationToken cancellationToken = default);
        Task<int> GetCountByStatusAsync(string status, int tenantId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Transaction>> SearchTransactionsAsync(string searchTerm, int tenantId, int page, int pageSize, CancellationToken cancellationToken = default);
        Task<PagedResponse<TransactionResponse>> GetTransactionsAsync(int page, int pageSize, string status = "");
        Task<PagedResponse<TransactionResponse>> GetUserTransactionsAsync(int userId, int page, int pageSize);
    }
}

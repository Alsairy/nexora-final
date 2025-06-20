using Microsoft.EntityFrameworkCore;
using Nexora.Core.Data;
using Nexora.Core.Entities;
using Nexora.Core.Interfaces;
using Nexora.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexora.Infrastructure.Repositories
{
    public class TransactionRepository : BaseRepository<Transaction>, ITransactionRepository
    {
        public TransactionRepository(NexoraDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<IEnumerable<Transaction>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Transactions
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ThenBy(t => t.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Transaction>> GetByTenantIdAsync(int tenantId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Transactions
                .Where(t => t.TenantId == tenantId)
                .OrderByDescending(t => t.CreatedAt)
                .ThenBy(t => t.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Transaction>> GetByStatusAsync(string status, int tenantId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Transactions
                .Where(t => t.Status == status && t.TenantId == tenantId)
                .OrderByDescending(t => t.CreatedAt)
                .ThenBy(t => t.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Transaction>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, int tenantId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Transactions
                .Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate && t.TenantId == tenantId)
                .OrderByDescending(t => t.CreatedAt)
                .ThenBy(t => t.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Transaction>> GetByAmountRangeAsync(decimal minAmount, decimal maxAmount, int tenantId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Transactions
                .Where(t => t.Amount >= minAmount && t.Amount <= maxAmount && t.TenantId == tenantId)
                .OrderByDescending(t => t.CreatedAt)
                .ThenBy(t => t.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task<decimal> GetTotalAmountByTenantAsync(int tenantId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Transactions
                .Where(t => t.TenantId == tenantId && t.Status == "Completed")
                .SumAsync(t => t.Amount, cancellationToken);
        }

        public async Task<int> GetCountByStatusAsync(string status, int tenantId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Transactions
                .CountAsync(t => t.Status == status && t.TenantId == tenantId, cancellationToken);
        }

        public async Task<IEnumerable<Transaction>> SearchTransactionsAsync(string searchTerm, int tenantId, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Transactions
                .Where(t => t.TenantId == tenantId && 
                           (t.ReferenceId.Contains(searchTerm) || 
                            t.Description.Contains(searchTerm) ||
                            t.Status.Contains(searchTerm)))
                .OrderByDescending(t => t.CreatedAt)
                .ThenBy(t => t.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task<Transaction> GetByReferenceIdAsync(string referenceId)
        {
            return await _dbContext.Transactions
                .FirstOrDefaultAsync(t => t.ReferenceId == referenceId);
        }

        public async Task<IReadOnlyList<Transaction>> GetRecentTransactionsAsync(int count)
        {
            return await _dbContext.Transactions
                .OrderByDescending(t => t.CreatedAt)
                .ThenBy(t => t.Id)
                .Take(count)
                .ToListAsync();
        }

        public async Task<PagedResponse<TransactionResponse>> GetTransactionsAsync(int page, int pageSize, string status = "")
        {
            var query = _dbContext.Transactions.AsQueryable();
            
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(t => t.Status == status);
            }

            var totalCount = await query.CountAsync();
            var transactions = await query
                .OrderByDescending(t => t.CreatedAt)
                .ThenBy(t => t.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new TransactionResponse
                {
                    Id = t.Id,
                    ReferenceId = t.ReferenceId,
                    Amount = t.Amount,
                    Currency = t.Currency,
                    Status = t.Status,
                    Type = t.Type,
                    Description = t.Description,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt,
                    UserId = t.UserId,
                    TenantId = t.TenantId
                })
                .ToListAsync();

            return new PagedResponse<TransactionResponse>
            {
                Data = transactions,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };
        }

        public async Task<PagedResponse<TransactionResponse>> GetUserTransactionsAsync(int userId, int page, int pageSize)
        {
            var totalCount = await _dbContext.Transactions
                .CountAsync(t => t.UserId == userId);
                
            var transactions = await _dbContext.Transactions
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ThenBy(t => t.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new TransactionResponse
                {
                    Id = t.Id,
                    ReferenceId = t.ReferenceId,
                    Amount = t.Amount,
                    Currency = t.Currency,
                    Status = t.Status,
                    Type = t.Type,
                    Description = t.Description,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt,
                    UserId = t.UserId,
                    TenantId = t.TenantId
                })
                .ToListAsync();

            return new PagedResponse<TransactionResponse>
            {
                Data = transactions,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };
        }
    }
}


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
    public class TransactionRepository : BaseRepository<Transaction>, ITransactionRepository
    {
        public TransactionRepository(NexoraDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<IReadOnlyList<Transaction>> GetByUserIdAsync(int userId, int page, int pageSize)
        {
            // Added deterministic ordering with ThenBy(e => e.Id) to ensure consistent paging
            return await _dbContext.Transactions
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ThenBy(t => t.Id) // Deterministic ordering
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<Transaction>> GetByStatusAsync(string status, int page, int pageSize)
        {
            // Added deterministic ordering with ThenBy(e => e.Id) to ensure consistent paging
            return await _dbContext.Transactions
                .Where(t => t.Status == status)
                .OrderByDescending(t => t.CreatedAt)
                .ThenBy(t => t.Id) // Deterministic ordering
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<Transaction>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, int page, int pageSize)
        {
            // Added deterministic ordering with ThenBy(e => e.Id) to ensure consistent paging
            return await _dbContext.Transactions
                .Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate)
                .OrderByDescending(t => t.CreatedAt)
                .ThenBy(t => t.Id) // Deterministic ordering
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalAmountByUserIdAsync(int userId)
        {
            return await _dbContext.Transactions
                .Where(t => t.UserId == userId && t.Status == "Completed")
                .SumAsync(t => t.Amount);
        }

        public async Task<Transaction> GetByReferenceIdAsync(string referenceId)
        {
            return await _dbContext.Transactions
                .FirstOrDefaultAsync(t => t.ReferenceId == referenceId);
        }

        public async Task<IReadOnlyList<Transaction>> GetRecentTransactionsAsync(int count)
        {
            // Added deterministic ordering with ThenBy(e => e.Id) to ensure consistent paging
            return await _dbContext.Transactions
                .OrderByDescending(t => t.CreatedAt)
                .ThenBy(t => t.Id) // Deterministic ordering
                .Take(count)
                .ToListAsync();
        }
    }
}


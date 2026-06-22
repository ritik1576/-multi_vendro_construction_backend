using InframartAPI_New.Data;
using InframartAPI_New.Models;
using InframartAPI_New.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace InframartAPI_New.Repositories
{
    public class WalletRepository : IWalletRepository
    {
        private readonly AppDbContext _context;

        public WalletRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Wallet?> GetWalletByUserIdAsync(long userId)
        {
            return await _context.Wallets
                .FirstOrDefaultAsync(w => w.UserId == userId);
        }

        public async Task<decimal> GetMonthlyExpenditureAsync(long walletId)
        {
            var startOfMonth = new System.DateTime(System.DateTime.UtcNow.Year, System.DateTime.UtcNow.Month, 1, 0, 0, 0, System.DateTimeKind.Utc);
            return await _context.WalletTransactions
                .Where(t => t.WalletId == walletId && 
                            t.Direction == TransactionDirection.Debit && 
                            t.Status == TransactionStatus.Success && 
                            t.CreatedAt >= startOfMonth)
                .SumAsync(t => t.Amount);
        }

        public async Task<(System.Collections.Generic.List<WalletTransaction> items, int totalCount)> GetWalletTransactionsAsync(long walletId, int page, int pageSize)
        {
            var query = _context.WalletTransactions
                .Where(t => t.WalletId == walletId);

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }
    }
}

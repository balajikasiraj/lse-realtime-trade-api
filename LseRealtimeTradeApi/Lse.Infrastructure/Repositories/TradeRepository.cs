using Lse.Domain;
using Lse.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Lse.Infrastructure.Repositories
{
    public class TradeRepository : ITradeRepository
    {
        private readonly LseDbContext _db;

        public TradeRepository(LseDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(Trade trade, CancellationToken ct = default)
        {
            // Use EF Core execution strategy to retry on transient failures (e.g., SQL connection issues)
            var strategy = _db.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                // For relational providers, wrap in a transaction so retries are safe
                if (_db.Database.IsRelational())
                {
                    await using var transaction = await _db.Database.BeginTransactionAsync(ct);

                    await _db.Trades.AddAsync(trade, ct);
                    await _db.SaveChangesAsync(ct);

                    await transaction.CommitAsync(ct);
                }
                else
                {
                    // InMemory and other non-relational providers do not support transactions
                    await _db.Trades.AddAsync(trade, ct);
                    await _db.SaveChangesAsync(ct);
                }
            });
        }

        public async Task<IEnumerable<Trade>> GetByTickerAsync(string ticker, CancellationToken ct = default)
        {
            return await _db.Trades.AsNoTracking().Where(t => t.Ticker == ticker).ToListAsync(ct);
        }

        public async Task<IEnumerable<Trade>> GetByTickersAsync(IEnumerable<string> tickers, CancellationToken ct = default)
        {
            var set = tickers.ToHashSet();
            return await _db.Trades.AsNoTracking().Where(t => set.Contains(t.Ticker)).ToListAsync(ct);
        }

        public async Task<IEnumerable<Trade>> GetAllAsync(CancellationToken ct = default)
        {
            return await _db.Trades.AsNoTracking().ToListAsync(ct);
        }
    }
}

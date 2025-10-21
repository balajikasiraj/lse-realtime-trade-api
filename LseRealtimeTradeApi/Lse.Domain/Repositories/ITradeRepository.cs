using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lse.Domain;

namespace Lse.Domain.Repositories
{
    public interface ITradeRepository
    {
        Task AddAsync(Trade trade, CancellationToken ct = default);
        Task<IEnumerable<Trade>> GetByTickerAsync(string ticker, CancellationToken ct = default);
        Task<IEnumerable<Trade>> GetByTickersAsync(IEnumerable<string> tickers, CancellationToken ct = default);
        Task<IEnumerable<Trade>> GetAllAsync(CancellationToken ct = default);
    }
}
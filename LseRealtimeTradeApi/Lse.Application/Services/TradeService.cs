using Lse.Domain;
using Lse.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Lse.Application.Services
{
    public interface ITradeService
    {
        Task RecordTradeAsync(Trade trade, CancellationToken ct = default);
        Task<decimal?> GetCurrentValueAsync(string ticker, CancellationToken ct = default);
        Task<IDictionary<string, decimal?>> GetCurrentValuesAsync(IEnumerable<string> tickers, CancellationToken ct = default);
        Task<IDictionary<string, decimal>> GetAllCurrentValuesAsync(CancellationToken ct = default);
    }

    public class TradeService : ITradeService
    {
        private readonly ITradeRepository _repo;

        public TradeService(ITradeRepository repo)
        {
            _repo = repo;
        }

        public async Task RecordTradeAsync(Trade trade, CancellationToken ct = default)
        {
            // Service-level validation using DataAnnotations
            var validationContext = new ValidationContext(trade);
            var validationResults = new List<ValidationResult>();
            if (!Validator.TryValidateObject(trade, validationContext, validationResults, validateAllProperties: true))
            {
                var msg = string.Join("; ", validationResults.Select(r => r.ErrorMessage));
                throw new Lse.Application.Exceptions.ValidationException(msg);
            }

            trade.Id = Guid.NewGuid();
            trade.Timestamp = DateTime.UtcNow;
            await _repo.AddAsync(trade, ct);
        }

        public async Task<decimal?> GetCurrentValueAsync(string ticker, CancellationToken ct = default)
        {
            var trades = await _repo.GetByTickerAsync(ticker, ct);
            if (!trades.Any())
                throw new Lse.Application.Exceptions.NotFoundException($"Ticker '{ticker}' not found");

            // average price across all trades for that ticker
            return trades.Average(t => t.Price);
        }

        public async Task<IDictionary<string, decimal?>> GetCurrentValuesAsync(IEnumerable<string> tickers, CancellationToken ct = default)
        {
            var trades = await _repo.GetByTickersAsync(tickers, ct);
            var groups = trades.GroupBy(t => t.Ticker)
                .ToDictionary(g => g.Key, g => (decimal?)g.Average(t => t.Price));

            // ensure all requested tickers present
            var result = new Dictionary<string, decimal?>();
            foreach (var tk in tickers)
            {
                result[tk] = groups.TryGetValue(tk, out var v) ? v : null;
            }

            return result;
        }

        public async Task<IDictionary<string, decimal>> GetAllCurrentValuesAsync(CancellationToken ct = default)
        {
            var trades = await _repo.GetAllAsync(ct);
            return trades.GroupBy(t => t.Ticker)
                .ToDictionary(g => g.Key, g => g.Average(t => t.Price));
        }
    }
}

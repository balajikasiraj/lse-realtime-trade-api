using Lse.Domain;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Lse.Application.Services
{
    public class CachedTradeService : ITradeService
    {
        private readonly TradeService _inner;
        private readonly IDistributedCache _cache;
        private readonly TimeSpan _ttl;

        public CachedTradeService(TradeService inner, IDistributedCache cache, IConfiguration configuration, TimeSpan? ttl = null)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));

            if (ttl != null)
            {
                _ttl = ttl.Value;
            }
            else if (configuration != null)
            {
                var seconds = configuration.GetValue<int?>("Cache:TradeValueTtlSeconds");
                _ttl = TimeSpan.FromSeconds(seconds ?? 120);
            }
            else
            {
                _ttl = TimeSpan.FromSeconds(120);
            }
        }

        public Task RecordTradeAsync(Trade trade, CancellationToken ct = default)
        {
            return InternalRecordTradeAsync(trade, ct);
        }

        private async Task InternalRecordTradeAsync(Trade trade, CancellationToken ct = default)
        {
            await _inner.RecordTradeAsync(trade, ct);

            var singleKey = GetKeyForTicker(trade.Ticker);
            await _cache.RemoveAsync(singleKey, ct);
            await _cache.RemoveAsync(GetAllKey(), ct);
        }

        public async Task<decimal?> GetCurrentValueAsync(string ticker, CancellationToken ct = default)
        {
            var key = GetKeyForTicker(ticker);
            var cached = await _cache.GetAsync(key, ct);
            if (cached != null)
            {
                return JsonSerializer.Deserialize<decimal?>(cached);
            }

            var value = await _inner.GetCurrentValueAsync(ticker, ct);
            var data = JsonSerializer.SerializeToUtf8Bytes<decimal?>(value);
            var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = _ttl };
            await _cache.SetAsync(key, data, options, ct);
            return value;
        }

        public async Task<IDictionary<string, decimal?>> GetCurrentValuesAsync(IEnumerable<string> tickers, CancellationToken ct = default)
        {
            var result = new Dictionary<string, decimal?>(StringComparer.OrdinalIgnoreCase);
            var toFetch = new List<string>();

            foreach (var t in tickers)
            {
                var key = GetKeyForTicker(t);
                var cached = await _cache.GetAsync(key, ct);
                if (cached != null)
                {
                    result[t] = JsonSerializer.Deserialize<decimal?>(cached);
                }
                else
                {
                    toFetch.Add(t);
                }
            }

            if (toFetch.Count > 0)
            {
                var fetched = await _inner.GetCurrentValuesAsync(toFetch, ct);
                foreach (var kv in fetched)
                {
                    result[kv.Key] = kv.Value;
                    var data = JsonSerializer.SerializeToUtf8Bytes<decimal?>(kv.Value);
                    var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = _ttl };
                    await _cache.SetAsync(GetKeyForTicker(kv.Key), data, options, ct);
                }
            }

            return result;
        }

        public async Task<IDictionary<string, decimal>> GetAllCurrentValuesAsync(CancellationToken ct = default)
        {
            var key = GetAllKey();
            var cached = await _cache.GetAsync(key, ct);
            if (cached != null)
            {
                return JsonSerializer.Deserialize<IDictionary<string, decimal>>(cached)!;
            }

            var vals = await _inner.GetAllCurrentValuesAsync(ct);
            var data = JsonSerializer.SerializeToUtf8Bytes(vals);
            var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = _ttl };
            await _cache.SetAsync(key, data, options, ct);
            return vals;
        }

        private static string GetKeyForTicker(string ticker) => $"trade:value:{ticker}";
        private static string GetAllKey() => "trade:value:all";
    }
}

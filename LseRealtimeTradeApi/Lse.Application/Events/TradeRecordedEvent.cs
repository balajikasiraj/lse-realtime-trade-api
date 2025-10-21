using System;

namespace Lse.Application.Events
{
    public sealed class TradeRecordedEvent
    {
        public Guid TradeId { get; init; }
        public string Ticker { get; init; } = string.Empty;
        public decimal Price { get; init; }
        public decimal Quantity { get; init; }
        public string BrokerId { get; init; } = string.Empty;
        public DateTime Timestamp { get; init; }
    }
}

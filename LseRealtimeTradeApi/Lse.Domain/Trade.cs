using System;
using System.ComponentModel.DataAnnotations;


namespace Lse.Domain
{
    public sealed class Trade
    {
        public Guid Id { get; set; }

        [Required]
        [StringLength(TradeConstraints.TickerMaxLength, MinimumLength = TradeConstraints.TickerMinLength)]
        public string Ticker { get; set; } = string.Empty; // e.g. "VOD"

        [Range(TradeConstraints.PriceMin, TradeConstraints.PriceMax)]
        public decimal Price { get; set; } // in pounds

        [Range(TradeConstraints.QuantityMin, TradeConstraints.QuantityMax)]
        public decimal Quantity { get; set; } // can be decimal

        [Required]
        [StringLength(TradeConstraints.BrokerIdMaxLength)]
        public string BrokerId { get; set; } = string.Empty;

        [Required]
        public DateTime Timestamp { get; set; }
    }
}

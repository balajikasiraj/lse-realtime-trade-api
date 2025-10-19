namespace Lse.Domain
{
    public static class TradeConstraints
    {
        public const int TickerMaxLength = 16;
        public const int TickerMinLength = 1;
        public const int BrokerIdMaxLength = 64;

        // Range attribute requires primitive constant types (double)
        public const double PriceMin = 0.0001;
        public const double PriceMax = 1000000;
        public const double QuantityMin = 0.0001;
        public const double QuantityMax = 1000000;
    }
}

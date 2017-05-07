using System;

namespace BitcoinYield
{
    public class PriceHistory
    {
        public DateTimeOffset Date { get; set; }
        public decimal Price { get; set; }
        public decimal Amount { get; set; }
    }
}
using System.Linq;
using Raven.Abstractions.Indexing;
using Raven.Client.Indexes;

namespace BitcoinYield
{
    public class PriceHistory_ByDate : AbstractIndexCreationTask<PriceHistory, PriceHistory_ByDate.Result>
    {
        public class Result
        {
            public long DateTicks { get; set; }
        }

        public PriceHistory_ByDate()
        {
            this.Map = prices => 
                from price in prices
                select new Result
                {
                    DateTicks = price.Date.Ticks
                };

            this.Index(f => f.DateTicks, FieldIndexing.Analyzed);
            this.Sort(f => f.DateTicks, SortOptions.Long);
        }
    }
}
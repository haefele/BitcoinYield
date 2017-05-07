using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using Raven.Abstractions.Extensions;
using Raven.Client.Document;
using Raven.Client.Indexes;
using Raven.Client.Util;

namespace BitcoinYield
{
    class Program
    {
        static void Main(string[] args)
        {
            var documentStore = new DocumentStore();
            documentStore.ParseConnectionString("Url = http://localhost:8080;Database=BitcoinYield");

            documentStore.Initialize(ensureDatabaseExists: true);

            //InsertFromCsv(documentStore);
            CalculateYield(documentStore);

            Console.ReadLine();
        }

        private static void InsertFromCsv(DocumentStore documentStore)
        {
            IndexCreation.CreateIndexes(typeof(Program).Assembly(), documentStore);

            Console.WriteLine("Start!");

            var watch = Stopwatch.StartNew();

            using (var bulkInsert = documentStore.BulkInsert())
            using (var stream = File.OpenRead("./price-history.csv"))
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            using (var csv = new CsvReader(reader, new CsvConfiguration {CultureInfo = new CultureInfo("en-US")}))
            {
                //int counter = 0;

                while (csv.Read())
                {
                    //counter++;

                    var date = DateTimeOffset.FromUnixTimeSeconds(csv.GetField<int>(0));
                    var price = csv.GetField<decimal>(1);
                    var amount = csv.GetField<decimal>(2);

                    bulkInsert.Store(new PriceHistory
                    {
                        Date = date,
                        Price = price,
                        Amount = amount
                    });

                    //if (counter % 1000 == 0)
                    //    Console.WriteLine(counter);
                }
            }

            watch.Stop();

            Console.WriteLine($"Inserting the whole csv took {watch.Elapsed:g}");
        }

        private static void CalculateYield(DocumentStore documentStore)
        {
            DateTimeOffset since = new DateTimeOffset(2016, 1, 1, 12, 0, 0, TimeSpan.Zero);
            decimal investmentPerMonth = 50;
            TimeSpan interval = TimeSpan.FromDays(30);

            decimal investmentAmount = 0m;
            decimal bitcoinAmount = 0m;
            PriceHistory lastPrice = null;

            DateTimeOffset current = since;
            while (current < DateTimeOffset.Now)
            {
                var price = documentStore.OpenSession()
                    .Query<PriceHistory_ByDate.Result, PriceHistory_ByDate>()
                    .Where(f => f.DateTicks >= current.Ticks)
                    .OfType<PriceHistory>()
                    .FirstOrDefault();

                if (price == null)
                    break;

                investmentAmount += investmentPerMonth;
                bitcoinAmount += investmentPerMonth / price.Price;
                lastPrice = price;

                current = current.Add(interval);
            }

            decimal currentValue = bitcoinAmount * lastPrice?.Price ?? 0m;

            Console.WriteLine($"Invested: {investmentAmount}");
            Console.WriteLine($"Current value: {currentValue}");
            Console.WriteLine($"Yield: {currentValue - investmentAmount} ({(currentValue / investmentAmount) - 1:P})");
        }
    }
}
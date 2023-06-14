using System.Diagnostics;
using static System.Formats.Asn1.AsnWriter;

namespace FuzzyProductSearch
{
    internal class Program
    {
        private static DataStore<Product, IndexedProduct> _store;

        static void Main(string[] args)
        {
            _store = new DataStore<Product, IndexedProduct>();

            ulong id = 1;
            foreach (var line in File.ReadLines("C:\\Users\\tn\\Downloads\\nsn-extract-2-21-23.xls.csv"))
            {
                var parts = line.Split(';');
                if (parts.Length != 2) continue;

                _store.Add(new Product(id++, parts[1], parts[0]));
            }

            Console.WriteLine($"{_store.Count} items on record\n");

            while (true)
            {
                Console.Write("search > ");
                var query = Console.ReadLine();
                if (query == null || query == "!q") return;

                Search(query);
            }
        }

        static void Search(string query)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var results = _store.Find(query).ToArray();

            stopwatch.Stop();

            foreach (var result in results)
            {
                var parts = result.Item.Name.Split(' ').Select(x => x.Trim())
                    .Concat(result.Item.Manufacturer.Split(' ').Select(x => x.Trim())).ToArray();
                parts[result.BestMatchIndex] = $"[{parts[result.BestMatchIndex]}]";
                var nameWithHighlight = string.Join(' ', parts.Take(result.Item.NameParts.Length));
                var manufacturerWithHighlight = string.Join(' ', parts.Skip(result.Item.NameParts.Length));

                Console.WriteLine($"{manufacturerWithHighlight} {nameWithHighlight} ({result.Rank}, {result.Item.Id})");
            }

            Console.WriteLine($"{stopwatch.ElapsedMilliseconds}ms\n");
        }
    }
}
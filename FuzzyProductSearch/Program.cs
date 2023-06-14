using System.Diagnostics;
using static System.Formats.Asn1.AsnWriter;

namespace FuzzyProductSearch
{
    internal class Program
    {
        private static DataStore<Product> _store;

        static void Main(string[] args)
        {
            _store = new DataStore<Product>();

            ulong id = 1;
            foreach (var line in File.ReadLines("C:\\Users\\tn\\Downloads\\test.csv"))
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
                var nameParts = result.Item.Name.Split(' ').Select(x => x.Trim()).ToArray();
                var manufacturerParts = result.Item.Manufacturer.Split(' ').Select(x => x.Trim()).ToArray();

                var parts = manufacturerParts.Concat(nameParts).ToArray();
                parts[result.BestMatchIndex] = $"[{parts[result.BestMatchIndex]}]";

                var manufacturerWithHighlight = string.Join(' ', parts.Take(manufacturerParts.Length));
                var nameWithHighlight = string.Join(' ', parts.Skip(manufacturerParts.Length));

                Console.WriteLine($"{manufacturerWithHighlight} {nameWithHighlight} ({result.Rank}, {result.Item.Id})");
            }

            Console.WriteLine($"{stopwatch.ElapsedMilliseconds}ms\n");
        }
    }
}
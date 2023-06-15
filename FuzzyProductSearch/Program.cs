using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using FuzzyProductSearch.Exceptions;
using Console = System.Console;

namespace FuzzyProductSearch
{
    internal class Program
    {
        private static DataStore<Product> _store;

        static void Main(string[] args)
        {
            _store = new DataStore<Product>();

            ulong id = 1;
            foreach (var line in File.ReadLines("C:\\Users\\tn\\Downloads\\nsn-extract-2-21-23.xls.csv"))
            //foreach (var line in File.ReadLines("C:\\Users\\tn\\Downloads\\test.csv"))
            {
                var parts = line.Split(';');
                if (parts.Length != 2) continue;

                _store.Add(new Product(id++, parts[1], parts[0]));
            }

            Console.WriteLine($"{_store.Count} items on record\n");

            while (true)
            {
                Console.Write("query > ");
                var query = Console.ReadLine();
                if (query == null || query == "!q") return;

                try
                {
                    ExecuteQuery(query);
                }
                catch (QueryException qe)
                {
                    Console.WriteLine("ERROR: " + qe.Message);
                }
            }
        }

        static void ExecuteQuery(string query)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var results = _store.Query(query).ToArray();

            stopwatch.Stop();

            foreach (var result in results)
            {
                var nameParts = result.Item.Name.Split(' ').Select(x => x.Trim()).ToArray();
                var manufacturerParts = result.Item.Manufacturer.Split(' ').Select(x => x.Trim()).ToArray();

                var partIndex = 0;

                PrintResultParts(manufacturerParts, ref partIndex, result.BestMatchIndex);
                Console.Write(" ");
                PrintResultParts(nameParts, ref partIndex, result.BestMatchIndex);

                Console.WriteLine($" ({result.Rank}, {result.Item.Id})");
            }

            Console.WriteLine($"{stopwatch.ElapsedMilliseconds}ms\n");
        }

        private static void PrintResultParts(string[] parts, ref int partCounter, int highlightedPart)
        {
            for (var i = 0; i < parts.Length; i++)
            {
                var backgroundColor = Console.BackgroundColor;
                if (partCounter == highlightedPart)
                {
                    Console.BackgroundColor = ConsoleColor.Blue;
                }

                Console.Write(parts[i]);
                Console.BackgroundColor = backgroundColor;

                if (i < parts.Length - 1)
                {
                    Console.Write(" ");
                }

                partCounter++;
            }
        }
    }
}
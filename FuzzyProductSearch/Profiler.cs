using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace FuzzyProductSearch
{
    public static class Profiler
    {
        private static StringBuilder _stringBuilder = new StringBuilder();

        public static ProfilerScope Start(string name)
        {
            return new ProfilerScope(name);
        }

        public static void Profile(string name, Action a)
        {
            var scope = Start(name);
            a();
            scope.Stop();
        }

        public static TReturn Profile<TReturn>(string name, Func<TReturn> a)
        {
            var scope = Start(name);
            var result = a();
            scope.Stop();
            return result;
        }

        public static void Print()
        {
            Console.WriteLine("==== PROFILER RESULTS ====");
            Console.WriteLine(_stringBuilder.ToString());
            Console.WriteLine("==========================");
        }

        public struct ProfilerScope
        {
            public long Age => _stopwatch.ElapsedMilliseconds;
            public string Name;

            private Stopwatch _stopwatch;

            public ProfilerScope(string name)
            {
                Name = name;

                _stopwatch = new Stopwatch();
                _stopwatch.Start();
            }

            public void Stop()
            {
                _stopwatch.Stop();
                _stringBuilder.AppendLine($"{Name}: {_stopwatch.ElapsedMilliseconds}ms");
            }
        }
    }
}

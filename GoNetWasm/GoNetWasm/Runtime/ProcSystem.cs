using System;
using System.Diagnostics;
using System.IO;

namespace GoNetWasm.Runtime
{
    internal class ProcSystem
    {
        internal static long GetNanoTime()
        {
            var nano = 10000L * Stopwatch.GetTimestamp();
            nano /= TimeSpan.TicksPerMillisecond;
            nano *= 100L;
            return nano;
        }

        public static string Cwd()
        {
            var dir = Directory.GetCurrentDirectory();
            return dir;
        }

        public override string ToString() => nameof(ProcSystem);
    }
}
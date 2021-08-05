using System;
using System.Diagnostics;

namespace GoNetWasm.Runtime
{
    internal class ProcSystem
    {
        public static long GetNanoTime()
        {
            var nano = 10000L * Stopwatch.GetTimestamp();
            nano /= TimeSpan.TicksPerMillisecond;
            nano *= 100L;
            return nano;
        }
    }
}
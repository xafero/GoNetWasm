using System;

namespace GoNetWasm.Internal
{
    internal static class Crypto
    {
        private static readonly Random Random = new Random();

        internal static Span<byte> GetRandomValues(Span<byte> buffer)
        {
            Random.NextBytes(buffer);
            return buffer;
        }
    }
}
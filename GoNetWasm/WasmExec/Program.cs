using System;

namespace WasmExec
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            if (args == null || args.Length < 1)
            {
                Console.WriteLine("usage: " + nameof(WasmExec) + " [wasm binary] [arguments]");
                return;
            }

        }
    }
}
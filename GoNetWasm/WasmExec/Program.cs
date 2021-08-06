using System;
using System.IO;
using GoNetWasm;
using NLog;
using Wasmtime;

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
            var wasmFile = Path.GetFullPath(args[0]);
            var logger = LogManager.GetCurrentClassLogger();
            using var go = new Go(logger);
            go.Create(new Engine(), e => Module.FromFile(e, wasmFile));
            go.ImportObject();
            go.Instantiate();
            go.Run();
        }
    }
}
using System;
using System.IO;
using System.Reflection;

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
            using var go = new Go();
            go.Create(new Engine(), e => Module.FromFile(e, wasmFile));
            go.ImportObject();
            go.Instantiate();
            go.Run();
        }
    }
}
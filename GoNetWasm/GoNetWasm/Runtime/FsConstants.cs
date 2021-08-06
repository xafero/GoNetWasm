using System.Collections.Generic;
using System.Reflection;

namespace GoNetWasm.Runtime
{
    internal class FsConstants
    {
        public readonly int F_OK = 0;

        public readonly int O_RDONLY = 0;

        public readonly int O_WRONLY = 1;

        public readonly int O_RDWR = 2;

        public readonly int O_CREAT = 64;

        public readonly int O_EXCL = 128;

        public readonly int O_NOCTTY = 256;

        public readonly int O_TRUNC = 512;

        public readonly int O_APPEND = 1024;

        public readonly int O_NONBLOCK = 2048;

        public readonly int O_DSYNC = 4096;

        public readonly int O_DIRECT = 16384;

        public readonly int O_DIRECTORY = 65536;

        public readonly int O_NOFOLLOW = 131072;

        public readonly int O_NOATIME = 262144;

        public readonly int O_SYNC = 1052672;

        public override string ToString() => nameof(FsConstants);

        internal static IList<string> FindFlags(int value)
        {
            var flags = new List<string>();
            foreach (var field in Fields)
            {
                if (!field.Name.StartsWith("O_"))
                    continue;
                var fieldVal = (int) field.GetValue(Single);
                if ((fieldVal & value) == 0)
                    continue;
                flags.Add(field.Name);
            }
            return flags;
        }

        private static readonly FsConstants Single = new FsConstants();
        private static readonly FieldInfo[] Fields = typeof(FsConstants).GetFields();
    }
}
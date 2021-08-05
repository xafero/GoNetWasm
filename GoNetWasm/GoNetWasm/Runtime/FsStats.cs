using System.IO;

namespace GoNetWasm.Runtime
{
    internal class FsStats
    {
        private readonly FileInfo _info;

        public object Dev { get; set; } = 1;
        public object Ino { get; set; } = 2;
        public object Mode { get; set; } = 3;
        public object Nlink { get; set; } = 4;
        public object Uid { get; set; } = 5;
        public object Gid { get; set; } = 6;
        public object Rdev { get; set; } = 7;
        public object Size { get; set; } = 8;
        public object Blksize { get; set; } = 9;
        public object Blocks { get; set; } = 10;
        public object AtimeMs { get; set; } = 11;
        public object MtimeMs { get; set; } = 12;
        public object CtimeMs { get; set; } = 13;

        public FsStats(FileInfo info)
        {
            _info = info;
        }
    }
}
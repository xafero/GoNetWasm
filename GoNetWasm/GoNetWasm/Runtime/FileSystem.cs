using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GoNetWasm.Data;
using GoNetWasm.Internal;

namespace GoNetWasm.Runtime
{
    internal class FileSystem
    {
        public readonly FsConstants Constants = new FsConstants();

        private static readonly UTF8Encoding Encoding = new UTF8Encoding();

        private string _outputBuf = "";

        public void Stat(string path, Func<object[], object> call)
        {
            var fullPath = Path.GetFullPath(path);
            var fsInfo = new FileInfo(fullPath);
            call(new object[] {JsNull.S, new FsStats(fsInfo)});
        }

        /// <summary>
        /// writeSync(fd, buf)
        /// </summary>
        /// <param name="fd">file descriptor</param>
        /// <param name="buf">some bytes</param>
        public int WriteSync(double _, IList<byte> buf)
        {
            _outputBuf += Encoding.GetString(buf.ToArray());
            var nl = _outputBuf.LastIndexOf('\n');
            if (nl != -1)
            {
                Console.WriteLine(_outputBuf.Substring(0, nl));
                _outputBuf = _outputBuf.Substring(nl + 1);
            }
            return buf.Count;
        }

        /// <summary>
        /// write(fd, buf, offset, length, position, callback)
        /// </summary>
        public void Write(double fd, IList<byte> buf, int offset, double length, object position,
            Func<object[], object> call)
        {
            if (offset != 0 || ((int) length) != buf.Count || !position.IsUndefinedOrNull())
            {
                call(new object[] {Errors.EnoSys()});
                return;
            }
            var n = WriteSync(fd, buf);
            call(new object[] {JsNull.S, n, buf});
        }

        public override string ToString() => nameof(FileSystem);
    }
}
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

        private readonly IDictionary<int, FileDescriptor> _fileDesc;
        private string _outputBuf = "";

        internal FileSystem()
        {
            _fileDesc = new Dictionary<int, FileDescriptor>();
        }

        public void Stat(string path, Func<object[], object> call)
        {
            var fullPath = Path.GetFullPath(path);
            var fsInfo = new FileInfo(fullPath);
            call(new object[] {JsNull.S, new FsStats(fsInfo)});
        }

        public void Fstat(double fileId, Func<object[], object> call)
        {
            var fileDesc = _fileDesc[(int) fileId];
            var path = fileDesc.Path;
            Stat(path, call);
        }

        public void Mkdir(string path, object mode, Func<object[], object> call)
        {
            Directory.CreateDirectory(path);
            call(new object[] {JsNull.S});
        }

        /// <summary>
        /// writeSync(fd, buf)
        /// </summary>
        /// <param name="fd">file descriptor</param>
        /// <param name="buf">some bytes</param>
        public int WriteSync(double fd, IList<byte> buf)
        {
            if (_fileDesc.TryGetValue((int) fd, out var fileDesc))
            {
                fileDesc.Stream.Write(buf.ToArray());
                fileDesc.Stream.Flush();
                return buf.Count;
            }
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

        public void Read(double fd, IList<byte> buf, int offset, double length, object position,
            Func<object[], object> call)
        {
            var fileDesc = _fileDesc[(int) fd];
            var buff = buf.ToArray();
            if (position is double posD)
                fileDesc.Stream.Seek((long) posD, SeekOrigin.Begin);
            var n = fileDesc.Stream.Read(buff, offset, (int) length);
            buf.Overwrite(buff);
            call(new object[] {JsNull.S, n, buf});
        }

        public void Open(string path, double flags, double mode, Func<object[], object> call)
        {
            var foundFlags = FsConstants.FindFlags((int) flags);

            if (foundFlags.Contains(nameof(FsConstants.O_CREAT)))
            {
                var created = File.Create(path);
                var createHandle = new FileDescriptor(foundFlags, created, path);
                _fileDesc[createHandle.Id] = createHandle;
                call(new object[] {JsNull.S, createHandle.Id});
                return;
            }

            if ((int) flags == Constants.O_RDONLY)
            {
                var read = File.OpenRead(path);
                var readHandle = new FileDescriptor(foundFlags, read, path);
                _fileDesc[readHandle.Id] = readHandle;
                call(new object[] {JsNull.S, readHandle.Id});
                return;
            }

            throw new InvalidOperationException();
        }

        public void Close(double fileId, Func<object[], object> call)
        {
            using var fileDesc = _fileDesc[(int) fileId];
            _fileDesc.Remove(fileDesc.Id);
            call(new object[] {JsNull.S});
        }

        public void Unlink(string path, Func<object[], object> call)
        {
            if (!File.Exists(path))
            {
                call(new object[] {Errors.EnoSys()});
                return;
            }
            try
            {
                File.Delete(path);
            }
            catch (Exception e)
            {
                call(new object[] {new Error(e.Message)});
            }
        }

        public void Rmdir(string path, Func<object[], object> call)
        {
            if (!Directory.Exists(path))
            {
                call(new object[] {Errors.EnoSys()});
                return;
            }
            try
            {
                Array.ForEach(Directory.GetFiles(path), File.Delete);
                Directory.Delete(path);
            }
            catch (Exception e)
            {
                call(new object[] {new Error(e.Message)});
            }
        }

        public override string ToString() => nameof(FileSystem);

        private class FileDescriptor : IDisposable
        {
            private static int _nextFileId = 100;

            public FileDescriptor(IList<string> flags, FileStream stream, string path)
            {
                Id = ++_nextFileId;
                Flags = flags;
                Stream = stream;
                Path = path;
            }

            public int Id { get; }
            public IList<string> Flags { get; }
            public FileStream Stream { get; }
            public string Path { get; }

            public void Dispose()
            {
                Flags.Clear();
                Stream.Flush();
                Stream.Close();
                Stream.Dispose();
            }
        }
    }
}
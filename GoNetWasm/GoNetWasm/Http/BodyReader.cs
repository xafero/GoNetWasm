using System.Net.Http;
using System.Threading.Tasks;

namespace GoNetWasm.Http
{
    internal class BodyReader
    {
        private readonly HttpResponseMessage _message;
        private readonly FetchResponse _parent;
        private readonly Task<byte[]> _task;
        private bool[] _done;

        public BodyReader(FetchResponse parent, HttpResponseMessage message)
        {
            _parent = parent;
            _message = message;
            _task = _message.Content.ReadAsByteArrayAsync();
            _done = new[] {false};
        }

        public Promise<byte[]> Read()
        {
            return new Promise<byte[]>(_parent, _task, Wrap);
        }

        private object Wrap(byte[] arg) => new ReadResult(arg, _done);

        public void Cancel()
        {
        }
    }
}
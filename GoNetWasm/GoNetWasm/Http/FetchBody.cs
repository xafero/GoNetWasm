using System.Net.Http;

namespace GoNetWasm.Http
{
    internal class FetchBody
    {
        private readonly HttpResponseMessage _message;
        private readonly FetchResponse _parent;

        public FetchBody(FetchResponse parent, HttpResponseMessage message)
        {
            _parent = parent;
            _message = message;
        }

        public BodyReader GetReader()
        {
            return new BodyReader(_parent, _message);
        }
    }
}
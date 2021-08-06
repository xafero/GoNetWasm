using System;
using System.Net.Http;

namespace GoNetWasm.Http
{
    internal class FetchResponse : IDisposable
    {
        private readonly HttpResponseMessage _message;

        public FetchResponse(HttpResponseMessage message)
        {
            _message = message;
            Headers = new Headers(message.Headers);
            Body = new FetchBody(this, message);
            ArrayBuffer = new FetchBuffer();
            Status = (int) _message.StatusCode;
        }

        public Headers Headers { get; set; }

        public FetchBody Body { get; set; }

        public FetchBuffer ArrayBuffer { get; set; }

        public int Status { get; set; }

        public override string ToString() => nameof(FetchResponse);

        public void Dispose()
        {
            _message?.Dispose();
        }
    }
}
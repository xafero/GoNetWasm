using System.Collections.Generic;
using System.Net.Http.Headers;
using GoNetWasm.Data;

namespace GoNetWasm.Http
{
    internal class Headers : Dictionary<string, string>
    {
        public Headers()
        {
        }

        public Headers(HttpResponseHeaders responseHeaders)
        {
            foreach (var header in responseHeaders)
                this[header.Key] = string.Join(" ", header.Value).Trim();
        }

        public JsIterator Entries() => new JsIterator(GetEnumerator());

        public void Append(string key, string value)
        {
            this[key] = value;
        }

        public override string ToString() => nameof(Headers);
    }
}
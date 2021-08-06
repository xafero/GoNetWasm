using System;
using System.Collections;
using System.Net.Http;
using GoNetWasm.Data;

namespace GoNetWasm.Http
{
    internal static class Network
    {
        internal static object Fetch(object[] args)
        {
            if (args.Length == 2)
                return Fetch((string) args[0], (IDictionary) args[1]);

            throw new NotImplementedException(nameof(Fetch));
        }

        private static object Fetch(string url, IDictionary dict)
        {
            var method = (string) dict["method"];
            var client = new HttpClient();
            switch (method.ToUpperInvariant())
            {
                case "GET":
                    var get = client.GetAsync(url);
                    return new Promise<HttpResponseMessage>(client, get, Wrap);
                case "POST":
                    var headers = (Headers) dict["headers"];
                    var body = (JsUint8Array) dict["body"];
                    var data = new ByteArrayContent(body.ToArray());
                    AddHeaders(data, headers);
                    var post = client.PostAsync(url, data);
                    return new Promise<HttpResponseMessage>(client, post, Wrap);
            }
            throw new NotImplementedException($"{nameof(Fetch)}: {method} on '{url}'!");
        }

        private static void AddHeaders(HttpContent data, Headers headers)
        {
            foreach (var header in headers)
                data.Headers.Add(header.Key, header.Value);
        }

        private static object Wrap(HttpResponseMessage arg) => new FetchResponse(arg);
    }
}
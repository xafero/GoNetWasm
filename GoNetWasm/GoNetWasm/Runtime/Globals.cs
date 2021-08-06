using System;
using System.Collections.Generic;
using GoNetWasm.Data;
using GoNetWasm.Http;
using GoNetWasm.Internal;

namespace GoNetWasm.Runtime
{
    internal class Globals
    {
        private readonly Dictionary<string, object> _values;
        private readonly ProcSystem _proc = new ProcSystem();
        private readonly FileSystem _fs = new FileSystem();
        private readonly Crypto _crypto = new Crypto();

        internal Globals()
        {
            _values = new Dictionary<string, object>
            {
                {"Object", new Func<object>(CreateObject)},
                {"Array", new Func<object>(CreateArray)},
                {"Uint8Array", new Func<object>(CreateByteArray)},
                {"Date", new Func<object>(CreateDate)},
                {"AbortController", new Func<object>(CreateAbortController)},
                {"Headers", new Func<object>(CreateHeaders)},
                {"process", _proc},
                {"fs", _fs},
                {"crypto", _crypto},
                {"fetch", new Func<object[], object>(Network.Fetch)}
            };
        }

        private static object CreateObject() => new JsObject();
        private static object CreateArray() => new JsArray();
        private static object CreateByteArray() => new JsUint8Array();
        private static object CreateDate() => new JsDate();
        private static object CreateAbortController() => new AbortController();
        private static object CreateHeaders() => new Headers();

        internal object this[string key]
        {
            get
            {
                if (_values.TryGetValue(key, out var value))
                    return value;

                throw new NotImplementedException(nameof(Globals) + ": " + key);
            }
        }

        public override string ToString() => nameof(Globals);
    }
}
using System;
using System.Collections.Generic;
using GoNetWasm.Data;

namespace GoNetWasm.Runtime
{
    internal class Globals
    {
        private readonly Dictionary<string, object> _values;
        private readonly ProcSystem _proc = new ProcSystem();
        private readonly FileSystem _fs = new FileSystem();

        internal Globals()
        {
            _values = new Dictionary<string, object>
            {
                {"Object", new Func<object>(() => (object) new JsObject())},
                {"Array", new Func<object>(() => (object) new JsArray())},
                {"Uint8Array", new Func<object>(() => (object) new JsUint8Array())},
                {"process", _proc},
                {"fs", _fs}
            };
        }

        internal object this[string key]
        {
            get
            {
                if (_values.TryGetValue(key, out var value))
                    return value;

                throw new NotImplementedException(nameof(Globals) + ": " + key);
            }
        }
    }
}
using System;
using System.Text;
using Wasmtime;
using System.Collections.Generic;
using GoNetWasm.Runtime;

namespace GoNetWasm
{
    public class Go : IDisposable
    {
        private readonly UTF8Encoding _encoding;
        private readonly Dictionary<int, object> _scheduledTimeouts;
        private readonly Dictionary<int, double> _goRefCounts;
        private readonly Dictionary<int, object> _values;
        private readonly Stack<int> _idPool;
        private readonly Dictionary<object, int> _ids;

        private int _nextCallbackTimeoutId;
        private Engine _engine;
        private Module _module;
        private Linker _linker;
        private IStore _store;
        private Instance _instance;

        private EventData PendingEvent { get; set; }
        private bool? Exited { get; set; }

        public Go()
        {
            _encoding = new UTF8Encoding();
            _scheduledTimeouts = new Dictionary<int, object>();
            _goRefCounts = new Dictionary<int, double>();
            _values = new Dictionary<int, object>();
            _idPool = new Stack<int>();
            _ids = new Dictionary<object, int>();
            _nextCallbackTimeoutId = 1;
            PendingEvent = null;
            Exited = null;
        }

        public void Create(Engine engine, Func<Engine, Module> setup)
        {
            _engine = engine;
            _module = setup(engine);
            _store = new Store(_engine);
            _linker = new Linker(_engine);
        }

        public void Instantiate()
        {
            _instance = _linker.Instantiate(_store, _module);
        }

        public void Dispose()
        {
            _linker?.Dispose();
            _module?.Dispose();
            _engine.Dispose();
        }
    }
}
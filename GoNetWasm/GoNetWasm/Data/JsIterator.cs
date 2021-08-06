using System;
using System.Collections;

namespace GoNetWasm.Data
{
    internal class JsIterator : IDisposable
    {
        internal class NextItem
        {
            public bool Done { get; set; }
            public object Value { get; set; }
        }

        private readonly IEnumerator _enumerator;

        public JsIterator(IEnumerator enumerator)
        {
            _enumerator = enumerator;
        }

        public NextItem Next()
        {
            var isDone = !_enumerator.MoveNext();
            return new NextItem
            {
                Done = isDone, Value = isDone ? JsUndefined.S : _enumerator.Current
            };
        }

        public void Dispose() => (_enumerator as IDisposable)?.Dispose();

        public override string ToString() => nameof(JsIterator);
    }
}
using GoNetWasm.Data;

namespace GoNetWasm.Http
{
    internal class ReadResult
    {
        private readonly byte[] _bytes;
        private readonly JsUint8Array _value;
        private readonly bool[] _done;

        public ReadResult(byte[] bytes, bool[] done)
        {
            _bytes = bytes;
            _value = new JsUint8Array(_bytes);
            _done = done;
        }

        public object Value
        {
            get
            {
                var val = _value;
                Done = true;
                return val;
            }
        }

        public bool Done
        {
            get => _done[0];
            set => _done[0] = value;
        }
    }
}
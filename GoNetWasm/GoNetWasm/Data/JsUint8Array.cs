using System;
using System.Collections.Generic;
using System.Text;

namespace GoNetWasm.Data
{
    internal class JsUint8Array : List<byte>
    {
        internal JsUint8Array()
        {
        }

        internal JsUint8Array(Span<byte> span) : base(span.ToArray())
        {
        }

        public int ByteLength => Count;

        public override string ToString() => ToString(false);

        private string ToString(bool showContent)
        {
            var bld = new StringBuilder();
            bld.Append("Uint8Array" + "(" + Count + ") [");
            if (showContent)
                bld.Append(string.Join(", ", this));
            bld.Append("]");
            return bld.ToString();
        }
    }
}
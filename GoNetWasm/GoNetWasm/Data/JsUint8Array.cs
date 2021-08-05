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

        public override string ToString()
        {
            var bld = new StringBuilder();
            bld.Append("Uint8Array" + "(" + Count + ") [");
            bld.Append(string.Join(", ", this));
            bld.Append("]");
            return bld.ToString();
        }
    }
}
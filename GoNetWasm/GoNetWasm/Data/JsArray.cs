using System.Collections.Generic;

namespace GoNetWasm.Data
{
    internal class JsArray : List<object>
    {
        public override string ToString() => nameof(JsArray);
    }
}
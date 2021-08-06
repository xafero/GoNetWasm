using System.Collections.Generic;

namespace GoNetWasm.Data
{
    internal class JsObject : Dictionary<object, object>
    {
        public override string ToString() => nameof(JsObject);
    }
}
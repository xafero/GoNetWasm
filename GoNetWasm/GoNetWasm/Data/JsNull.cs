namespace GoNetWasm.Data
{
    internal class JsNull
    {
        private JsNull()
        {
        }

        internal static readonly JsNull S = new JsNull();

        public override string ToString() => nameof(JsNull);
    }
}
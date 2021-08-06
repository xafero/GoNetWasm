namespace GoNetWasm.Data
{
    internal class JsUndefined
    {
        private JsUndefined()
        {
        }

        internal static readonly JsUndefined S = new JsUndefined();
        
        public override string ToString() => nameof(JsUndefined);
    }
}
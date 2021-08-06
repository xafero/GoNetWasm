namespace GoNetWasm.Http
{
    internal class AbortController
    {
        public AbortSignal Signal { get; }

        internal AbortController()
        {
            Signal = new AbortSignal();
        }

        public override string ToString() => nameof(AbortController);
    }
}
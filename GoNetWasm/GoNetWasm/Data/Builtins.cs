namespace GoNetWasm.Data
{
    internal static class Builtins
    {
        internal static bool IsJsNull(this object obj) => obj is JsNull;

        internal static bool IsJsUndefined(this object obj) => obj == null || obj is JsUndefined;

        internal static bool IsUndefinedOrNull(this object obj) => IsJsNull(obj) || IsJsUndefined(obj);
    }
}
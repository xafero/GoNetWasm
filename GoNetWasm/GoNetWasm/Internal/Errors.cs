namespace GoNetWasm.Internal
{
    internal static class Errors
    {
        internal static Error EnoSys()
        {
            var err = new Error("not implemented")
            {
                Code = "ENOSYS"
            };
            return err;
        }
    }
}
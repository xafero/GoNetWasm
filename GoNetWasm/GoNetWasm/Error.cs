using System;

namespace GoNetWasm
{
    internal class Error : Exception
    {
        internal Error(string text) : base(text)
        {
        }

        internal string Code { get; set; }
    }
}
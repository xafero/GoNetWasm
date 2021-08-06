using System;

namespace GoNetWasm
{
    internal class Error : Exception
    {
        internal Error(string text) : base(text)
        {
        }

        public string Code { get; set; }
    }
}
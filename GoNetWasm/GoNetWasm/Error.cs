using System;

namespace GoNetWasm
{
    public class Error : Exception
    {
        public Error(string text) : base(text)
        {
        }

        public string Code { get; set; }
    }
}
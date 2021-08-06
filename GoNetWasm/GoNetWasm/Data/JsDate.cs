using System;

namespace GoNetWasm.Data
{
    internal class JsDate
    {
        private readonly DateTime _current;

        public JsDate()
        {
            _current = DateTime.Now;
        }

        public int GetTimeZoneOffset()
        {
            var minutes = Math.Floor((DateTime.UtcNow - _current).TotalMinutes);
            return (int) minutes;
        }

        public override string ToString() => nameof(JsDate);
    }
}
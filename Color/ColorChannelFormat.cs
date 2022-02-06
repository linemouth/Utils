using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Utils
{
    public struct ColorChannelFormat
    {
        public string suffix;
        public int precision;
        public bool useSigFigs;
        public static readonly Regex regex = new Regex(@"^\s*(?<sigFigs>\d+|0?\.(?<decimals>\d+))?(?<suffix>deg|%)?\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline);

        public ColorChannelFormat(string suffix, int precision, bool useSigFigs = false)
        {
            this.suffix = suffix;
            this.precision = precision;
            this.useSigFigs = useSigFigs;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Utils
{
    public class ColorFormat
    {
        public string model;
        public ColorChannelFormat[] channels;
        public ColorChannelFormat this[int i] => channels[i];

        private static readonly Regex colorModelRegex = new Regex(@"(?<model>\w+)(?:\s*\(\s*(?<values>.*?)\s*\))?", RegexOptions.IgnoreCase | RegexOptions.Multiline);
        private static readonly Regex spaceCommaRegex = new Regex(@"\s*,\s*");

        public ColorFormat(string model, ColorChannelFormat[] channels)
        {
            this.model = model;
            this.channels = channels;
        }
    }
}

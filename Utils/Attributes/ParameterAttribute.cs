using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Utils.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ParameterAttribute : Attribute
    {
        public string Label { get; }
        public string Group { get; }
        public double Min { get; }
        public double Max { get; }
        public bool ShowSlider { get; }
        public IColor DefaultColor { get; }
        public string[] Options { get; }

        private static readonly Regex nameRegex = new Regex(@"^(?<label>[^@]+)?(?:@(?<group>[^@]+))?$");

        public ParameterAttribute(string label)
        {
            Match match = nameRegex.Match(label);
            if(match.Groups["group"].Success)
            {
                Group = match.Groups["group"].Value;
                Label = match.Groups["label"].Success ? match.Groups["label"].Value : null;
            }
            else
            {
                Group = match.Groups["label"].Value;
                Label = null;
            }
        }
        public ParameterAttribute(string label, double min, double max, bool showSlider = false) : this(label)
        {
            Min = min;
            Max = max;
            ShowSlider = showSlider;
        }
        public ParameterAttribute(string label, string text) : this(label) {
            if (Color.TryParse(text, out IColor color))
            {
                DefaultColor = color;
            }
            else
            {
                Options = text.Split('|');
            }
        }
    }
}

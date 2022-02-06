using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Utils
{
    public static class RegexExtensions
    {
        public static readonly Regex spaceCommaRegex = new Regex(@"\s*,\s*");

        public static bool TryMatch(this Regex regex, string input, out Match match) { match = regex.Match(input); return match.Success; }
        public static bool TryMatch(this Regex regex, string input, out string capture)
        {
            Match match = regex.Match(input);
            capture = match.Success ? match.Groups[0].Value : null;
            return match.Success;
        }
        public static string Take(this Regex regex, string input, out string extracted)
        {
            Match match = regex.Match(input);
            if(match.Success)
            {
                extracted = match.Groups[1].Value;
                return regex.Replace(input, "");
            }
            else
            {
                extracted = "";
                return input;
            }
        }
        public static Regex ParsePattern(string pattern)
        {
            if(patternParser.TryMatch(pattern, out Match match))
            {
                pattern = match.Groups[1].Value;
                RegexOptions options = RegexOptions.Compiled;
                foreach(char o in match.Groups[2].Value)
                {
                    options |= regexOptionMap[o];
                }
                return new Regex(pattern, options);
            }
            else
            {
                return new Regex(pattern, RegexOptions.Compiled | RegexOptions.Multiline);
            }
        }

        private static readonly Regex patternParser = new Regex(@"^[\\\*]?/(.*)/([gimsncx]*)$", RegexOptions.IgnoreCase);
        private static readonly Map<char, RegexOptions> regexOptionMap = new Map<char, RegexOptions> {
            { 'g', RegexOptions.None }, // Global flag is implicit in C#
            { 'i', RegexOptions.IgnoreCase },
            { 'm', RegexOptions.Multiline },
            { 's', RegexOptions.Singleline },
            { 'n', RegexOptions.ExplicitCapture },
            { 'c', RegexOptions.Compiled },
            { 'x', RegexOptions.IgnorePatternWhitespace }
        };
    }
}

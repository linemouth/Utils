using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Utils
{
    public static class RegexExtensions
    {
        #region Regex
        public static readonly Regex spaceCommaRegex = new Regex(@"\s*,\s*");
        public static bool TryMatch(this Regex regex, string input, out Match match) { match = regex.Match(input); return match.Success; }
        public static bool TryMatch(this Regex regex, string input, out string group1)
        {
            Match match = regex.Match(input);
            group1 = null;
            if (match.Success)
            {
                if (match.Groups.Count > 1)
                {
                    group1 = match.Groups[1].Value;
                }
                return true;
            }
            return false;
        }
        public static bool TryMatch(this Regex regex, string input, out string group1, out string group2)
        {
            Match match = regex.Match(input);
            group1 = null;
            group2 = null;
            if (match.Success)
            {
                if(match.Groups.Count > 2)
                {
                    group1 = match.Groups[1].Value;
                    group2 = match.Groups[2].Value;
                }
                return true;
            }
            return false;
        }
        public static string Take(this Regex regex, string input, out string extracted)
        {
            if (TryMatch(regex, input, out Match match))
            {
                extracted = match.Groups[1].Value;
                int start = match.Index;
                if (start == 0)
                {
                    input = input.Substring(match.Length);
                }
                else
                {
                    int end = start + match.Length;
                    if (end >= input.Length)
                    {
                        input = input.Substring(0, start);
                    }
                    else
                    {
                        input = input.Substring(0, start) + input.Substring(end);
                    }
                }
            }
            else
            {
                extracted = null;
            }
            return input;
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
        #endregion

        #region GroupCollection
        public static string Get(this GroupCollection groups, string name) => groups[name].Success ? groups[name].Value : null;
        public static string Get(this GroupCollection groups, int index) => groups[index].Success ? groups[index].Value : null;
        public static string First(this GroupCollection groups, bool includeOverall = false)
        {
            for(int i = 1; i < groups.Count; ++i)
            {
                if(groups[i].Success)
                {
                    return groups[i].Value;
                }
            }
            return groups[0].Value;
        }
        #endregion

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

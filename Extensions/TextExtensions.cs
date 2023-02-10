using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Text.RegularExpressions;

namespace Utils
{
    public static class TextExtensions
    {
        public class TextScopeRef
        {
            public readonly int Start;
            public int End { get; private set; } = -1;
            public int Length => Math.Max(End - Start, -1);
            public string Content => SourceText.Substring(Start, Length);
            public readonly TextScopeRef Parent = null;
            public readonly List<TextScopeRef> InnerScopes = new List<TextScopeRef>();
            public readonly char StartDelimiter;
            public readonly char EndDelimiter;

            private readonly string SourceText;

            public static TextScopeRef Parse(string text, char startDelimiter, char endDelimiter)
            {
                TextScopeRef scope = null;
                for(int i = 0; i < text.Length; ++i)
                {
                    if(text[i] == startDelimiter)
                    {
                        scope = new TextScopeRef(text, i + 1, scope, startDelimiter, endDelimiter);
                        if(scope.Parent != null)
                        {
                            scope.Parent.InnerScopes.Add(scope);
                        }
                    }
                    else if(text[i] == endDelimiter)
                    {
                        if(scope == null)
                        {
                            break;
                        }

                        scope.End = i;

                        if(scope.Parent == null)
                        {
                            break;
                        }

                        scope = scope.Parent;
                    }
                }
                return scope;
            }
            public override string ToString() => $"{StartDelimiter}{Content}{EndDelimiter}";

            private TextScopeRef(string sourceText, int start, TextScopeRef parent, char startDelimiter, char endDelimiter)
            {
                SourceText = sourceText;
                Start = start;
                Parent = parent;
                StartDelimiter = startDelimiter;
                EndDelimiter = endDelimiter;
            }
        }
        public class TextScope
        {
            public string StartDelimiter { get; set; }
            public string EndDelimiter { get; set; }
            public readonly List<object> Contents = new List<object>();
            public string InnerText
            {
                get
                {
                    StringBuilder builder = new StringBuilder();
                    foreach(object item in Contents)
                    {
                        builder.Append(Contents.ToString());
                    }
                    return builder.ToString();
                }
            }
            public TextScope Parent { get; set; }

            public TextScope(string startDelimiter, string content, string endDelimiter)
            {
                StartDelimiter = startDelimiter;
                EndDelimiter = endDelimiter;
                Contents.Add(content);
            }
            public TextScope(string startDelimiter, IEnumerable<object> contents, string endDelimiter)
            {
                StartDelimiter = startDelimiter;
                EndDelimiter = endDelimiter;
                foreach(object item in contents)
                {
                    if(item is TextScope)
                    {
                        Contents.Add(item);
                        Parent = this;
                    }
                    else
                    {
                        Contents.Add(item.ToString());
                    }
                }
            }
            public override string ToString()
            {
                StringBuilder builder = new StringBuilder(StartDelimiter);
                foreach(object item in Contents)
                {
                    builder.Append(Contents.ToString());
                }
                builder.Append(EndDelimiter);
                return builder.ToString();
            }
        }

        public static string Wrap(this string text, int maxWidth, string indent = "") => string.Join($"\n{indent}", WrapLines(text, maxWidth));
        public static List<string> WrapLines(this string text, int maxWidth)
        {
            List<string> lines = new List<string>();
            foreach(Match match in Regex.Matches(text,
                @"(?:"
                    // -- Words/Characters 
                    + @"(" // (1 start)
                        + @"(?>" // Atomic Group - Match words with valid breaks
                            + $".{{1,{maxWidth}}}" //  1-N characters
                            + @"(?:" //  Followed by one of 4 prioritized, non-linebreak whitespace break types:
                                + @"(?<= [^\S\r\n] )" // 1. - Behind a non-linebreak whitespace
                                + @"[^\S\r\n]?" //  ( optionally accept an extra non-linebreak whitespace )
                                + @"|(?= \r ? \n )" // 2. - Ahead a linebreak
                                + @"|$" // 3. - EOS
                                + @"|[^\S\r\n]" // 4. - Accept an extra non-linebreak whitespace
                            + @")"
                        + @")" // End atomic group
                    + $"|.{{1,{maxWidth}}}" // No valid word breaks, just break on the N'th character
                + @")" // (1 end)
                + @"(?:\r?\n)?" // Optional linebreak after Words/Characters
                + @"|(?:\r?\n|$)" // Stand alone linebreak or at EOS
                + @")",
                RegexOptions.Multiline))
            {
                if(match.Groups[1].Length > 0)
                {
                    lines.Add(match.Groups[1].Value.Trim());
                }
            }
            return lines;
        }
        public static TextScopeRef GetScopeHierarchy(this string text, char startDelimiter = '(', char endDelimiter = ')') => TextScopeRef.Parse(text, startDelimiter, endDelimiter);
        public static bool StartEquals(this string text, char start) => string.IsNullOrWhiteSpace(text) ? false : text[0] == start;
        public static bool StartEquals(this string text, string start)
        {
            if(string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(start) || text.Length < start.Length)
            {
                return false;
            }
            for(int i = 0; i < start.Length; ++i)
            {
                if(text[i] != start[i])
                {
                    return false;
                }
            }
            return true;
        }
        public static bool EndEquals(this string text, char end) => string.IsNullOrWhiteSpace(text) ? false : text[text.Length - 1] == end;
        public static bool EndEquals(this string text, string end)
        {
            if(string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(end) || text.Length < end.Length)
            {
                return false;
            }
            int offset = text.Length - end.Length;
            for(int i = 0; i < end.Length; ++i)
            {
                if(text[offset + i] != end[i])
                {
                    return false;
                }
            }
            return true;
        }
        public static string Terminate(this string text, string suffix) => EndEquals(text, suffix) ? text : text + suffix;
        public static string Terminate(this string text, char suffix) => EndEquals(text, suffix) ? text : text + suffix;
        public static string AddIfNotNull(this string text, string optionalText, string prefix = "", string suffix = "") => string.IsNullOrWhiteSpace(optionalText) ? text : $"{text}{prefix}{optionalText}{suffix}";
        public static string ReplaceVariables(this string text, Dictionary<string, string> variables)
        {
            foreach(KeyValuePair<string, string> entry in variables)
            {
                string key = "$" + entry.Key;
                text = text.Replace(key, entry.Value);
            }
            return text;
        }
    }
}

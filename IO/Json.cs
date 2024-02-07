using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Utils
{
    public class Json : OrderedDictionary<string, object>
    {
        public class SerializerOptions
        {
            public enum SpaceMode { None, Space, Newline };
            public enum FormatMode { Minimized, SingleLine, Compact, Smart, Expanded }

            public bool keyQuotes = true;
            public int scope = 0;
            public string indent = "    ";
            public FormatMode format = FormatMode.Expanded;

            public SerializerOptions(int initialScope = 0) => scope = initialScope;
        }

        private static readonly RegexOptions regexOptions = RegexOptions.Multiline | RegexOptions.Compiled;
        private static readonly Regex nameNeedsQuotesRegex = new Regex(@"[\s:]", regexOptions);
        private static readonly Regex keyRegex = new Regex(@"\G\s*(?:(?<quote>""|')(?<quotedKey>.*?)\k<quote>|(?<key>\w+))\s*:\s*", regexOptions);
        private static readonly Regex stringRegex = new Regex(@"\G\s*(?<quote>""|')(?<value>.*?)(?<!\\)\k<quote>\s*,?\s*", regexOptions);
        private static readonly Regex hexRegex = new Regex(@"\G\s*0[xX](?<value>[0-9a-fA-F]+)\s*,?\s*", regexOptions);
        private static readonly Regex intRegex = new Regex(@"\G\s*\+?(?<value>-?\d+)\s*,?\s*", regexOptions);
        private static readonly Regex uintRegex = new Regex(@"\G\s*\+?(?<value>\d+)\s*,?\s*", regexOptions);
        private static readonly Regex floatRegex = new Regex(@"\G\s*\+?(?<value>-?(?:\d+\.\d*|\d*\.\d+)(?:[eE]-?\d+)?)\s*,?\s*", regexOptions);
        private static readonly Regex keywordRegex = new Regex(@"\G\s*(?<value>[-\w]+)\s*,?\s*", regexOptions);
        private static readonly Regex listStartRegex = new Regex(@"\G\s*\[\s*", regexOptions);
        private static readonly Regex listEndRegex = new Regex(@"\G\s*\]\s*,?\s*", regexOptions);
        private static readonly Regex objectStartRegex = new Regex(@"\G\s*\{\s*", regexOptions);
        private static readonly Regex objectEndRegex = new Regex(@"\G\s*\}\s*,?\s*", regexOptions);
        private static readonly Dictionary<string, object> parseKeywords = new Dictionary<string, object>
        {
            {"null", null},
            {"undefined", null},
            {"true", true},
            {"false", false},
            {"nan", double.NaN},
            {"infinity", double.PositiveInfinity},
            {"-infinity", double.NegativeInfinity}
        };
        private static readonly Dictionary<object, string> formatKeywords = new Dictionary<object, string>
        {
            {true, "true"},
            {false, "false"},
            {double.NaN, "nan"},
            {float.NaN, "nan"},
            {double.PositiveInfinity, "infinity"},
            {float.PositiveInfinity, "infinity"},
            {double.NegativeInfinity, "-infinity"},
            {float.NegativeInfinity, "-infinity"}
        };

        public static Json Parse(string path) => Parse(path, Encoding.UTF8);
        public static Json Parse(string path, Encoding encoding)
        {
            IOException error = null;
            for(int i = 0; i < 10; ++i)
            {
                try
                {
                    using (Stream stream = File.OpenRead(path))
                    {
                        return Parse(stream, encoding);
                    }
                }
                catch(IOException e) {
                    error = e;
                    Thread.Sleep(250);
                }
            }
            throw error;
        }
        public static Json Parse(Stream stream) => Parse(stream, Encoding.UTF8);
        public static Json Parse(Stream stream, Encoding encoding) => Parse(new RegexReader(stream, encoding));
        public static Json Parse(RegexReader reader) => ParseObject(reader, reader.TryReadRegex(objectStartRegex, out _));
        public static string Serialize(IEnumerable<KeyValuePair<string, object>> data, SerializerOptions options)
        {
            StringBuilder sb = new StringBuilder();
            FormatValue(sb, options, data);
            return sb.ToString();
        }
        public Json() { }
        public Json(IEnumerable<KeyValuePair<string, object>> entries)
        {
            foreach(var entry in entries)
            {
                Add(entry);
            }
        }
        public Json GetValue(string key) => GetValue(key, v => (Json)v);
        public T GetValue<T>(string key) where T : IConvertible => GetValue(key, v => (T)Convert.ChangeType(v, typeof(T)));
        public T GetValue<T>(string key, Func<object, T> selector)
        {
            if(base.TryGetValue(key, out object value))
            {
                return selector(value);
            }
            throw new KeyNotFoundException($"Could not find kob '{key}'.");
        }
        public Json GetValueOrDefault(string key, Json defaultValue) => GetValueOrDefault(key, v => (Json)v, defaultValue);
        public T GetValueOrDefault<T>(string key) where T : IConvertible => GetValueOrDefault<T>(key, default);
        public T GetValueOrDefault<T>(string key, T defaultValue) where T : IConvertible => GetValueOrDefault(key, v => (T)Convert.ChangeType(v, typeof(T)), defaultValue);
        public T GetValueOrDefault<T>(string key, Func<object, T> selector, T defaultValue)
        {
            if(base.TryGetValue(key, out object value))
            {
                return selector(value);
            }
            return defaultValue;
        }
        public bool TryGetValue(string key, out Json value) => TryGetValue(key, v => (Json)v, out value);
        public bool TryGetValue<T>(string key, out T value) where T : IConvertible => TryGetValue(key, v => (T)Convert.ChangeType(v, typeof(T)), out value);
        public bool TryGetValue<T>(string key, Func<object, T> selector, out T value)
        {
            if(base.TryGetValue(key, out object o))
            {
                value = selector(o);
                return true;
            }
            value = default;
            return false;
        }
        public List<Json> GetList(string key) => GetList(key, v => (Json)v);
        public List<T> GetList<T>(string key) where T : IConvertible => GetList(key, v => (T)Convert.ChangeType(v, typeof(T)));
        public List<T> GetList<T>(string key, Func<object, T> selector)
        {
            if(base.TryGetValue(key, out object value))
            {
                return ((IEnumerable<object>)value).Select(selector).ToList();
            }
            throw new KeyNotFoundException($"Could not find kob '{key}'.");
        }
        public List<Json> GetListOrDefault(string key, List<Json> defaultList) => GetListOrDefault(key, v => (Json)v, defaultList);
        public List<T> GetListOrDefault<T>(string key) where T : IConvertible => GetListOrDefault<T>(key, null);
        public List<T> GetListOrDefault<T>(string key, List<T> defaultList) where T : IConvertible => GetListOrDefault(key, v => (T)Convert.ChangeType(v, typeof(T)), defaultList);
        public List<T> GetListOrDefault<T>(string key, Func<object, T> selector, List<T> defaultList)
        {
            if(base.TryGetValue(key, out object value))
            {
                return ((IEnumerable<object>)value).Select(selector).ToList();
            }
            return defaultList;
        }
        public bool TryGetList(string key, out List<Json> value) => TryGetList(key, v => (Json)v, out value);
        public bool TryGetList<T>(string key, out List<T> value) => TryGetList(key, v => (T)Convert.ChangeType(v, typeof(T)), out value);
        public bool TryGetList<T>(string key, Func<object, T> selector, out List<T> list)
        {
            if(base.TryGetValue(key, out object value))
            {
                list = ((IEnumerable<object>)value).Select(selector).ToList();
                return true;
            }
            list = default;
            return false;
        }
        public string Serialize(SerializerOptions options) => Serialize(this, options);

        private static Json ParseObject(RegexReader reader, bool requireClosure)
        {
            Json data = new Json();
            while(reader.HasNext)
            {
                if(TryParseKey(reader, out string key))
                {
                    object value = ParseValue(reader);
                    data.Add(key, value);
                }
                else if (requireClosure)
                {
                    if(reader.TryReadRegex(objectEndRegex, out _))
                    {
                        break;
                    }
                    throw new FormatException($"Could not find object closure at {reader.FormatPosition()}");
                }
            }
            return data;
        }
        private static List<object> ParseList(RegexReader reader)
        {
            List<object> list = new List<object>();
            while (true)
            {
                if (reader.TryReadRegex(listEndRegex, out _))
                {
                    return list;
                }
                else
                {
                    list.Add(ParseValue(reader));
                }
            }
        }
        private static object ParseValue(RegexReader reader)
        {
            if (reader.TryReadRegex(stringRegex, out Match match))
            {
                return match.Groups["value"].Value;
            }
            else if (reader.TryReadRegex(hexRegex, out match))
            {
                return ulong.Parse(match.Groups["value"].Value, System.Globalization.NumberStyles.HexNumber);
            }
            else if (reader.TryReadRegex(floatRegex, out match))
            {
                return double.Parse(match.Groups["value"].Value);
            }
            else if (reader.TryReadRegex(uintRegex, out match))
            {
                return ulong.Parse(match.Groups["value"].Value);
            }
            else if (reader.TryReadRegex(intRegex, out match))
            {
                return long.Parse(match.Groups["value"].Value);
            }
            else if (reader.TryReadRegex(objectStartRegex, out match))
            {
                return ParseObject(reader, true);
            }
            else if (reader.TryReadRegex(listStartRegex, out match))
            {
                return ParseList(reader);
            }
            else if (reader.TryReadRegex(keywordRegex, out match))
            {
                if (parseKeywords.TryGetValue(match.Groups["value"].Value.ToLower(), out object value))
                {
                    return value;
                }
                else
                {
                    throw new FormatException($"Unexpected token at {reader.FormatPosition()}");
                }
            }
            throw new FormatException($"Unexpected token at {reader.FormatPosition()}");
        }
        private static bool TryParseKey(RegexReader reader, out string key)
        {
            if (reader.TryReadRegex(keyRegex, out Match match))
            {
                key = match.Groups["quotedKey"].Success ? match.Groups["quotedKey"].Value : match.Groups["key"].Value;
                return true;
            }
            key = default;
            return false;
        }
        private static void FormatObject(StringBuilder sb, SerializerOptions options, IEnumerable<KeyValuePair<string, object>> data)
        {
            List<KeyValuePair<string, object>> items = new List<KeyValuePair<string, object>>(data.Cast<KeyValuePair<string, object>>());

            SerializerOptions.FormatMode format = options.format;
            if (format == SerializerOptions.FormatMode.Smart)
            {
                int keyLength = items.Sum(item => item.Key.Length + 6) - 2;
                format = (items.All(item => item.Value is string)
                    ? keyLength + items.Sum(item => ((string)item.Value).Length) > 240 // If the items are a few short strings, we can make it a single line.
                    : keyLength > 120 || items.Count > 16 || items.Any(item => item.Value is IEnumerable && !(item.Value is string))) // If there are no objects or lists, we can make it a single line.
                    ? SerializerOptions.FormatMode.Expanded : SerializerOptions.FormatMode.SingleLine;
            }

            // Opening brace
            sb.Append('{');
            ++options.scope;
            switch (format)
            {
                case SerializerOptions.FormatMode.Compact:
                case SerializerOptions.FormatMode.Expanded:
                    sb.Append('\n');
                    FormatIndent(sb, options);
                    break;
                default:
                    break;
            }

            // Contents
            bool first = true;
            foreach (KeyValuePair<string, object> item in data)
            {
                if(first)
                {
                    first = false;
                }
                else
                {
                    switch (format)
                    {
                        case SerializerOptions.FormatMode.Minimized:
                            sb.Append(",");
                            break;
                        case SerializerOptions.FormatMode.SingleLine:
                            sb.Append(", ");
                            break;
                        case SerializerOptions.FormatMode.Compact:
                        case SerializerOptions.FormatMode.Expanded:
                            sb.Append(",\n");
                            FormatIndent(sb, options);
                            break;
                    }
                }
                FormatString(sb, options, item.Key, false);
                switch (format)
                {
                    case SerializerOptions.FormatMode.Minimized:
                        sb.Append(":");
                        break;
                    case SerializerOptions.FormatMode.SingleLine:
                    case SerializerOptions.FormatMode.Compact:
                    case SerializerOptions.FormatMode.Expanded:
                        sb.Append(": ");
                        break;
                }
                FormatValue(sb, options, item.Value);
            }

            // Closing brace
            --options.scope;
            switch (format)
            {
                case SerializerOptions.FormatMode.Compact:
                case SerializerOptions.FormatMode.Expanded:
                    sb.Append('\n');
                    FormatIndent(sb, options);
                    break;
            }
            sb.Append('}');
        }
        private static void FormatList(StringBuilder sb, SerializerOptions options, IEnumerable list)
        {
            List<object> items = new List<object>(list.Cast<object>());

            SerializerOptions.FormatMode format = options.format;
            if (format == SerializerOptions.FormatMode.Smart)
            {
                format = (items.All(item => item is string)
                    ? items.Count > 20 || items.Count * 4 + items.Sum(item => ((string)item).Length) > 240 // If the list is of few short strings, we can make it a single line.
                    : items.Count > 32 || items.Any(item => item is IEnumerable && !(item is string))) // If there are no objects or lists, we can make it a single line.
                    ? SerializerOptions.FormatMode.Expanded : SerializerOptions.FormatMode.SingleLine;
            }

            // Opening bracket
            sb.Append('[');
            ++options.scope;
            switch (format)
            {
                case SerializerOptions.FormatMode.Expanded:
                    sb.Append("\n");
                    FormatIndent(sb, options);
                    break;
            }

            // Contents
            bool first = true;
            foreach (object item in items)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    sb.Append(",");
                    switch (format)
                    {
                        case SerializerOptions.FormatMode.SingleLine:
                        case SerializerOptions.FormatMode.Compact:
                            sb.Append(" ");
                            break;
                        case SerializerOptions.FormatMode.Expanded:
                            sb.Append("\n");
                            FormatIndent(sb, options);
                            break;
                    }
                }
                FormatValue(sb, options, item);
            }

            // Closing bracket
            --options.scope;
            switch (format)
            {
                case SerializerOptions.FormatMode.Expanded:
                    sb.Append("\n");
                    FormatIndent(sb, options);
                    break;
            }
            sb.Append(']');
        }
        private static void FormatIndent(StringBuilder sb, SerializerOptions options)
        {
            for (int i = 0; i < options.scope; i++)
            {
                sb.Append(options.indent);
            }
        }
        private static void FormatString(StringBuilder sb, SerializerOptions options, string value, bool requireQuotes)
        {
            bool needQuotes = requireQuotes || options.keyQuotes || nameNeedsQuotesRegex.Match(value).Success;
            if (needQuotes)
            {
                char quote = '"';
                bool escape = false;
                if (value.Contains("\""))
                {
                    if (value.Contains("'"))
                    {
                        escape = true;
                    }
                    else
                    {
                        quote = '\'';
                    }
                }
                sb.Append(quote);
                if (escape)
                {
                    value = value.Replace("\"", "\\\"");
                }
                sb.Append(value);
                sb.Append(quote);
            }
            else
            {
                sb.Append(value);
            }
        }
        private static void FormatValue(StringBuilder sb, SerializerOptions options, object value)
        {
            if(value == null)
            {
                sb.Append("null");
                return;
            }
            Type type = value.GetType();
            if(formatKeywords.TryGetValue(value, out string text))
            {
                sb.Append(text);
            }
            else if (value is string)
            {
                FormatString(sb, options, value.ToString(), true);
            }
            else if (value is IConvertible)
            {
                sb.Append(value.ToString());
            }
            else if(value is IEnumerable<KeyValuePair<string, object>>)
            {
                FormatObject(sb, options, (IEnumerable<KeyValuePair<string, object>>)value);
            }
            else if(value is IEnumerable)
            {
                FormatList(sb, options, (IEnumerable)value);
            }
            else
            {
                throw new ArgumentException($"Cannot serialize unsupported type: '{type}'.");
            }
        }
    }
}

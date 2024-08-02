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
        public static Json Parse(Stream stream, Encoding encoding)
        {
            using (RegexReader parser = new RegexReader(stream, encoding))
            {
                return Parse(parser);
            }
        }
        public static Json Parse(RegexReader parser) => ParseObject(parser, parser.TryReadRegex(objectStartRegex, out _));
        public static string Serialize(IEnumerable<KeyValuePair<string, object>> data, SerializerOptions options = null)
        {
            using (StringWriter writer = new StringWriter())
            {
                SerializeValue(writer, options, data);
                writer.Flush();
                return writer.ToString();
            }
        }
        public static void Serialize(IEnumerable<KeyValuePair<string, object>> data, Stream stream, SerializerOptions options = null)
        {
            using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8))
            {
                SerializeValue(writer, options, data);
            }
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
        public string Serialize(SerializerOptions options = null) => Serialize(this, options);
        public void Serialize(Stream stream, SerializerOptions options = null) => Serialize(this, stream, options);

        private static Json ParseObject(RegexReader parser, bool requireClosure)
        {
            Json data = new Json();
            while(parser.HasNext)
            {
                if(TryParseKey(parser, out string key))
                {
                    object value = ParseValue(parser);
                    data.Add(key, value);
                }
                else if (requireClosure)
                {
                    if(parser.TryReadRegex(objectEndRegex, out _))
                    {
                        break;
                    }
                    throw new FormatException($"Could not find object closure at {parser.FormatPosition()}");
                }
            }
            return data;
        }
        private static List<object> ParseList(RegexReader parser)
        {
            List<object> list = new List<object>();
            while (true)
            {
                if (parser.TryReadRegex(listEndRegex, out _))
                {
                    return list;
                }
                else
                {
                    list.Add(ParseValue(parser));
                }
            }
        }
        private static object ParseValue(RegexReader parser)
        {
            if (parser.TryReadRegex(stringRegex, out Match match))
            {
                return match.Groups["value"].Value;
            }
            else if (parser.TryReadRegex(hexRegex, out match))
            {
                return ulong.Parse(match.Groups["value"].Value, System.Globalization.NumberStyles.HexNumber);
            }
            else if (parser.TryReadRegex(floatRegex, out match))
            {
                return double.Parse(match.Groups["value"].Value);
            }
            else if (parser.TryReadRegex(uintRegex, out match))
            {
                return ulong.Parse(match.Groups["value"].Value);
            }
            else if (parser.TryReadRegex(intRegex, out match))
            {
                return long.Parse(match.Groups["value"].Value);
            }
            else if (parser.TryReadRegex(objectStartRegex, out match))
            {
                return ParseObject(parser, true);
            }
            else if (parser.TryReadRegex(listStartRegex, out match))
            {
                return ParseList(parser);
            }
            else if (parser.TryReadRegex(keywordRegex, out match))
            {
                if (parseKeywords.TryGetValue(match.Groups["value"].Value.ToLower(), out object value))
                {
                    return value;
                }
                else
                {
                    throw new FormatException($"Unexpected token at {parser.FormatPosition()}");
                }
            }
            throw new FormatException($"Unexpected token at {parser.FormatPosition()}");
        }
        private static bool TryParseKey(RegexReader parser, out string key)
        {
            if (parser.TryReadRegex(keyRegex, out Match match))
            {
                key = match.Groups["quotedKey"].Success ? match.Groups["quotedKey"].Value : match.Groups["key"].Value;
                return true;
            }
            key = default;
            return false;
        }
        private static void SerializeObject(TextWriter writer, SerializerOptions options, IEnumerable<KeyValuePair<string, object>> data)
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
            writer.Write('{');
            ++options.scope;
            switch (format)
            {
                case SerializerOptions.FormatMode.Compact:
                case SerializerOptions.FormatMode.Expanded:
                    writer.Write('\n');
                    SerializeIndent(writer, options);
                    break;
                default:
                    break;
            }

            // Contents
            bool first = true;
            foreach (KeyValuePair<string, object> item in data)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    switch (format)
                    {
                        case SerializerOptions.FormatMode.Minimized:
                            writer.Write(",");
                            break;
                        case SerializerOptions.FormatMode.SingleLine:
                            writer.Write(", ");
                            break;
                        case SerializerOptions.FormatMode.Compact:
                        case SerializerOptions.FormatMode.Expanded:
                            writer.Write(",\n");
                            SerializeIndent(writer, options);
                            break;
                    }
                }
                SerializeString(writer, options, item.Key, false);
                switch (format)
                {
                    case SerializerOptions.FormatMode.Minimized:
                        writer.Write(":");
                        break;
                    case SerializerOptions.FormatMode.SingleLine:
                    case SerializerOptions.FormatMode.Compact:
                    case SerializerOptions.FormatMode.Expanded:
                        writer.Write(": ");
                        break;
                }
                SerializeValue(writer, options, item.Value);
            }

            // Closing brace
            --options.scope;
            switch (format)
            {
                case SerializerOptions.FormatMode.Compact:
                case SerializerOptions.FormatMode.Expanded:
                    writer.Write('\n');
                    SerializeIndent(writer, options);
                    break;
            }
            writer.Write('}');
        }
        private static void SerializeList(TextWriter writer, SerializerOptions options, IEnumerable list)
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
            writer.Write('[');
            ++options.scope;
            switch (format)
            {
                case SerializerOptions.FormatMode.Expanded:
                    writer.Write("\n");
                    SerializeIndent(writer, options);
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
                    writer.Write(",");
                    switch (format)
                    {
                        case SerializerOptions.FormatMode.SingleLine:
                        case SerializerOptions.FormatMode.Compact:
                            writer.Write(" ");
                            break;
                        case SerializerOptions.FormatMode.Expanded:
                            writer.Write("\n");
                            SerializeIndent(writer, options);
                            break;
                    }
                }
                SerializeValue(writer, options, item);
            }

            // Closing bracket
            --options.scope;
            switch (format)
            {
                case SerializerOptions.FormatMode.Expanded:
                    writer.Write("\n");
                    SerializeIndent(writer, options);
                    break;
            }
            writer.Write(']');
        }
        private static void SerializeIndent(TextWriter writer, SerializerOptions options)
        {
            for (int i = 0; i < options.scope; i++)
            {
                writer.Write(options.indent);
            }
        }
        private static void SerializeString(TextWriter writer, SerializerOptions options, string value, bool requireQuotes)
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
                writer.Write(quote);
                if (escape)
                {
                    value = value.Replace("\"", "\\\"");
                }
                writer.Write(value);
                writer.Write(quote);
            }
            else
            {
                writer.Write(value);
            }
        }
        private static void SerializeValue(TextWriter writer, SerializerOptions options, object value)
        {
            if(options == null)
            {
                options = new SerializerOptions();
            }

            if (value == null)
            {
                writer.Write("null");
                return;
            }
            Type type = value.GetType();
            if (formatKeywords.TryGetValue(value, out string text))
            {
                writer.Write(text);
            }
            else if (value is string)
            {
                SerializeString(writer, options, value.ToString(), true);
            }
            else if (value is IConvertible)
            {
                writer.Write(value.ToString());
            }
            else if (value is IEnumerable<KeyValuePair<string, object>>)
            {
                SerializeObject(writer, options, (IEnumerable<KeyValuePair<string, object>>)value);
            }
            else if (value is IEnumerable)
            {
                SerializeList(writer, options, (IEnumerable)value);
            }
            else
            {
                throw new ArgumentException($"Cannot serialize unsupported type: '{type}'.");
            }
        }
    }
}

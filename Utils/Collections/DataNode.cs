using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Utils
{
    public class DataNode : IEnumerable, IEnumerable<DataNode>
    {
        public readonly List<string> Errors = new List<string>();
        public DataNode Parent { get; }

        internal interface IWritable
        {
            void Write(TextWriter writer, string indent);
        }
        internal class Newline : IWritable
        {
            public int Count;

            public Newline(int count = 1) => this.Count = count;
            public void Write(TextWriter writer, string indent)
            {
                for(int i = 0; i < Count; ++i)
                {
                    writer.WriteLine();
                }
            }
        }
        internal class Comment : IWritable
        {
            public string Text { get; set; }

            public Comment(string text) => Text = text;
            public void Write(TextWriter writer, string indent)
            {
                if(Text.Length > 117)
                {
                    List<string> lines = Text.WrapLines(120);
                    writer.Write($"\n{indent}/* {lines[0]}");
                    for(int i = 0; i < lines.Count - 1; ++i)
                    {
                        writer.Write($"\n{indent}{lines[0]}");
                    }
                    writer.Write($"\n{indent}{lines[0]} */");
                }
                else
                {
                    writer.Write($"\n{indent}// {Text}");
                }
            }
        }
        internal abstract class TagBase<T>
        {
            public readonly string Key;
            public TagBase(string key) => Key = key;
            public List<T> Values { get; } = new List<T>();
            public T Value { get => HasValue ? Values[0] : default; set => Set(value); }
            public int Count => Values.Count;
            public bool HasValue => Count > 0;
            public bool IsList {
                get => isList;
                set
                {
                    isList = value || Values.Count > 1;
                }
            }

            private bool isList = false;

            public void Clear()
            {
                Values.Clear();
            }
            public void Set(T value)
            {
                if(Values.Count == 1)
                {
                    Values[0] = value;
                }
                else
                {
                    Clear();
                    Values.Add(value);
                }
                IsList = false;
            }
            public void Set(IEnumerable<T> values)
            {
                Values.Clear();
                Values.AddRange(values);
                IsList = true;
            }
            public void Add(T value)
            {
                Values.Add(value);
                if(Values.Count > 1)
                {
                    IsList = true;
                }
            }
            public void Add(IEnumerable<T> values)
            {
                Values.AddRange(values.ToList());
                IsList = true;
            }
            public bool TryGetItem(out T item)
            {
                if(Values.Count > 0)
                {
                    item = Values[0];
                    return true;
                }
                item = default;
                return false;
            }
        }
        internal class Tag : TagBase<IConvertible>, IWritable
        {
            public Tag(string key) : base(key) { }
            public Tag(string key, IConvertible value) : base(key) => Set(value);
            public Tag(string key, IEnumerable<IConvertible> values) : base(key) => Set(values);
            public void Write(TextWriter writer, string indent)
            {
                if(Count > 0)
                {
                    // Get formatted values
                    List<string> values = Values.Select(value => value.Serialize()).ToList();
                    bool multiline = false;
                    int width = -2;
                    if(Count > 1)
                    {
                        foreach(string value in values)
                        {
                            width += value.Length + 2;
                            if(width > 120)
                            {
                                multiline = true;
                                break;
                            }
                        }
                    }

                    // Write declaration
                    writer.Write($"\n{indent}{Key}: ");

                    if(IsList)
                    {
                        // Open list
                        writer.Write('[');

                        // Write values
                        if(multiline)
                        {
                            for(int i = 0; i < values.Count; ++i)
                            {
                                // Add comma if this is not the first element
                                if(i > 0)
                                {
                                    writer.Write(',');
                                }

                                writer.Write($"\n{indent}    {values[i]}");
                            }
                            writer.Write($"\n{indent}");
                        }
                        else
                        {
                            writer.Write(string.Join(", ", values));
                        }

                        // Close list
                        writer.Write(']');
                    }
                    else
                    {
                        // Write value
                        writer.Write(values[0]);
                    }
                }
            }
        }
        internal class Branch : TagBase<DataNode>, IWritable
        {
            public Branch(string key) : base(key) { }
            public Branch(string key, DataNode value) : base(key) => Set(value);
            public Branch(string key, IEnumerable<DataNode> values) : base(key) => Set(values);
            public void Write(TextWriter writer, string indent)
            {
                if(Count > 0)
                {
                    // Format declaration
                    writer.Write($"\n{indent}{Key}: ");

                    if(IsList)
                    {
                        string contentsIndent = indent + "    ";

                        // Open list
                        writer.Write($"[\n{contentsIndent}");

                        // Write nodes
                        for(int i = 0; i < Values.Count; ++i)
                        {
                            // Add comma if this is not the first element
                            if(i > 0)
                            {
                                writer.Write(',');
                            }

                            Values[i].Write(writer, contentsIndent);
                        }

                        // Close list
                        writer.Write($"\n{indent}]");
                    }
                    else
                    {
                        // Write node
                        Value.Write(writer, indent);
                    }
                }
            }
        }

        private readonly LinkedList<IWritable> children = new LinkedList<IWritable>();
        private readonly Dictionary<string, Tag> tags = new Dictionary<string, Tag>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Branch> branches = new Dictionary<string, Branch>(StringComparer.OrdinalIgnoreCase);

        private struct Query
        {
            public readonly Regex Regex;
            public readonly int StartIndex;
            public readonly int EndIndex;

            private static readonly Regex queryRegex = new Regex(@"^(?<query>.*?)(\[(?<index>\d+)|(?<start>\d*),(?<end>\d*)\])?$", RegexOptions.ExplicitCapture);

            public Query(Regex regex, int startIndex = 0, int endIndex = int.MaxValue)
            {
                Regex = regex;
                StartIndex = startIndex;
                EndIndex = endIndex + 1 - startIndex;
            }
            public IEnumerable<T> FilterItems<T>(IEnumerable<T> items) => items.Skip(StartIndex).Take(EndIndex + 1 - StartIndex);
            public static IEnumerable<Query> ParsePath(string path) => path.Split('/').Select(q => Parse(q));
            public static Query Parse(string query)
            {
                if(queryRegex.TryMatch(query, out Match match))
                {
                    Regex regex = new Regex(match.Groups.Get("query"));
                    if(match.Groups["index"].Success)
                    {
                        return new Query(regex, int.Parse(match.Groups.Get("index")));
                    }
                    else if(match.Groups["start"].Success)
                    {
                        if(match.Groups["end"].Success)
                        {
                            return new Query(regex, int.Parse(match.Groups.Get("start")), int.Parse(match.Groups.Get("end")));
                        }
                        else
                        {
                            return new Query(regex, int.Parse(match.Groups.Get("start")), int.MaxValue);
                        }
                    }
                    else if(match.Groups["end"].Success)
                    {
                        return new Query(regex, 0, int.Parse(match.Groups.Get("end")));
                    }
                    else
                    {
                        return new Query(regex);
                    }
                }
                throw new ArgumentException($"Could not parse query: '{query}'");
            }
        }
        private static readonly RegexOptions regexOptions = RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Multiline;
        private static readonly Regex commentRegex = new Regex(@"\G\s*(\/\/\s*(?<line>.*?)\s*$|\/\*\s*(?<body>[\S\s]*?)\s*\*\/)", regexOptions);
        private static readonly Regex keyRegex = new Regex(@"\G\s*((?<quote>[""'])(?<string>.+?)(?<=[^\\])\k<quote>|(?<word>\w+))\s*[=:]", regexOptions);
        private static readonly Regex valueRegex = new Regex(@"\G\s*(?<value>(?<quote>[""'])[\S\s]*?(?<=[^\\])\k<quote>|[\S]+?)\s*(?=,|$|\}|\]|\/\/|\/\*)", regexOptions);
        private static readonly Regex nodeStartRegex = new Regex(@"\G\s*\{", regexOptions);
        private static readonly Regex nodeEndRegex = new Regex(@"\G\s*\}", regexOptions);
        private static readonly Regex listStartRegex = new Regex(@"\G\s*\[", regexOptions);
        private static readonly Regex listEndRegex = new Regex(@"\G\s*\]", regexOptions);
        private static readonly Regex commaRegex = new Regex(@"\G\s*,", regexOptions);
        private static readonly Regex newlineRegex = new Regex(@"\s*?\n(?<extras>(\s*?\n)+)", regexOptions);

        #region Constructors
        /// <summary>Constructor for an empty DataNode.</summary>
        public DataNode(DataNode parent = null) => Parent = parent;
        /// <summary>Parse a DataNode from a text representation.</summary>
        public DataNode(string text, DataNode parent = null) : this(new RegexReader(text), parent) { }
        /// <summary>Parse a DataNode from a text stream.</summary>
        public DataNode(Stream stream, DataNode parent = null) : this(new RegexReader(stream), parent) { }
        /// <summary>Parse a DataNode from a RegexReader.</summary>
        public DataNode(RegexReader reader, DataNode parent = null) : this(parent) => Parse(reader, this);
        public static DataNode Parse(RegexReader reader, DataNode node = null, DataNode parent = null)
        {
            node = node ?? new DataNode(parent);
            bool isRootNode = node.Parent == null;
            string key = null;
            string mode = "Node";
            List<object> list = null;
            bool listIsValues = false;
            bool listIsNodes = false;
            bool listMustClose = false;

            while(reader.HasNext)
            {
                Match match = null;
                switch(mode)
                {
                    case "Node":
                        if(reader.TryReadRegex(newlineRegex, out match))
                        {
                            node.AddNewline(match.Groups.Get("extras").Count(c => c == '\n'));
                        }
                        else if(reader.TryReadRegex(commentRegex, out match))
                        {
                            node.AddComment(match.Groups.First());
                        }
                        else if(reader.TryReadRegex(keyRegex, out match))
                        {
                            key = match.Groups.Get("string") ?? match.Groups.Get("word");
                            mode = "Value";
                        }
                        else if(reader.TryReadRegex(commaRegex, out match))
                        {
                            // It's just a delimiter, ignore it.
                        }
                        else if(reader.TryReadRegex(nodeEndRegex, out match))
                        {
                            return node;
                        }
                        else
                        {
                            throw new InvalidDataException($"At {reader.FormatPosition(true, true)}: unable to parse following data inside node.");
                        }
                        break;
                    case "Value":
                        if(reader.TryReadRegex(nodeStartRegex, out match))
                        {
                            node.SetNode(key, new DataNode(reader, node));
                            mode = "Node";
                        }
                        else if(reader.TryReadRegex(listStartRegex, out match))
                        {
                            list = new List<object>();
                            listIsValues = false;
                            listIsNodes = false;
                            listMustClose = false;
                            mode = "List";
                        }
                        else if(reader.TryReadRegex(valueRegex, out match))
                        {
                            IConvertible value = Conversion.Parse(match.Groups.Get("value"), out Conversion.TypeCategory detectedType);
                            switch(detectedType)
                            {
                                case Conversion.TypeCategory.Null:
                                case Conversion.TypeCategory.String: node.SetValue(key, (string)value); break;
                                case Conversion.TypeCategory.Bool: node.SetValue(key, (bool)value); break;
                                case Conversion.TypeCategory.Signed: node.SetValue(key, (long)value); break;
                                case Conversion.TypeCategory.Unsigned: node.SetValue(key, (ulong)value); break;
                                case Conversion.TypeCategory.Float: node.SetValue(key, (double)value); break;
                            }
                            mode = "Node";
                        }
                        break;
                    case "List":
                        if(reader.TryReadRegex(commaRegex, out match))
                        {
                            listMustClose = false;
                        }
                        else if(reader.TryReadRegex(listEndRegex, out match))
                        {
                            if(listIsValues)
                            {
                                node.SetValues(key, list.ConvertAll(item => (IConvertible)item));
                            }
                            else if(listIsNodes)
                            {
                                node.SetNodes(key, list.ConvertAll(item => (DataNode)item));
                            }
                            else
                            {
                                // I don't know whether this is a list of values or nodes, ignore it.
                            }
                            list = null;
                            mode = "Node";
                        }
                        else if(!listMustClose && !listIsNodes && reader.TryReadRegex(valueRegex, out match))
                        {
                            IConvertible value = Conversion.Parse(match.Groups.Get("value"), out Conversion.TypeCategory detectedType);
                            switch(detectedType)
                            {
                                case Conversion.TypeCategory.Null:
                                case Conversion.TypeCategory.String: list.Add((string)value); break;
                                case Conversion.TypeCategory.Bool: list.Add((bool)value); break;
                                case Conversion.TypeCategory.Signed: list.Add((long)value); break;
                                case Conversion.TypeCategory.Unsigned: list.Add((ulong)value); break;
                                case Conversion.TypeCategory.Float: list.Add((double)value); break;
                            }
                            listMustClose = true;
                            listIsValues = true;
                        }
                        else if(!listMustClose && !listIsValues && reader.TryReadRegex(nodeStartRegex, out match))
                        {
                            list.Add(new DataNode(reader, node));
                            listMustClose = true;
                            listIsNodes = true;
                            mode = "Node";
                        }
                        else
                        {
                            throw new InvalidDataException($"At {reader.FormatPosition(true, true)}: unexpected data in list.");
                        }
                        break;
                    default:
                        break;
                }
            }

            if(!isRootNode || mode != "Node")
            {
                throw new EndOfStreamException($"At {reader.FormatPosition(true, true)}: unexpected end of stream.");
            }

            reader.Dispose();
            return node;
        }
        #endregion

        #region Public Functions
        // Values
        public void ClearValues()
        {
            foreach(Tag tag in tags.Values)
            {
                children.Remove(tag);
            }
            tags.Clear();
        }
        public void RemoveValue(string key)
        {
            if(tags.TryGetValue(key, out Tag tag))
            {
                children.Remove(tag);
                tags.Remove(key);
            }
        }
        public void SetValue<T>(string key, T value) where T : IConvertible
        {
            if(tags.ContainsKey(key))
            {
                tags[key].Set(value);
            }
            else
            {
                Tag tag = new Tag(key, value);
                children.AddLast(tag);
                tags.Add(key, tag);
            }
        }
        public void SetValues<T>(string key, IEnumerable<T> values) where T : IConvertible
        {
            if(tags.ContainsKey(key))
            {
                tags[key].Set((IEnumerable<IConvertible>)values);
            }
            else
            {
                Tag tag = new Tag(key, (IEnumerable<IConvertible>)values);
                children.AddLast(tag);
                tags.Add(key, tag);
            }
        }
        public T GetValue<T>(string key) where T : IConvertible => (T)tags[key].Value;
        public T GetValue<T>(string key, T fallback) where T : IConvertible => tags.ContainsKey(key) ? (T)tags[key].Value : fallback;
        public List<T> GetValues<T>(string key) where T : IConvertible => tags[key].Values.ConvertAll(value => (T)value).ToList();
        public bool TryGetValue<T>(string key, out T value) where T : IConvertible
        {
            if(tags.TryGetValue(key, out Tag tag) && tag.TryGetItem(out IConvertible item))
            {
                try
                {
                    value = (T)item;
                    return true;
                }
                catch { }
            }
            value = default;
            return false;
        }
        public bool TryGetValues<T>(string key, out List<T> values) where T : IConvertible
        {
            if(tags.TryGetValue(key, out Tag tag))
            {
                try
                {
                    values = tag.Values.ConvertAll(value => (T)value).ToList();
                    return true;
                }
                catch { }
            }
            values = null;
            return false;
        }
        // Nodes
        public void ClearNodes()
        {
            foreach(Branch node in branches.Values)
            {
                children.Remove(node);
            }
            branches.Clear();
        }
        public void RemoveNode(string key)
        {
            if(branches.TryGetValue(key, out Branch node))
            {
                children.Remove(node);
                branches.Remove(key);
            }
        }
        public void SetNode(string key, DataNode node)
        {
            if(branches.ContainsKey(key))
            {
                branches[key].Set(node);
            }
            else
            {
                Branch branch = new Branch(key, node);
                children.AddLast(branch);
                branches.Add(key, branch);
            }
        }
        public void SetNodes(string key, IEnumerable<DataNode> nodes)
        {
            if(branches.ContainsKey(key))
            {
                branches[key].Set(nodes);
            }
            else
            {
                Branch branch = new Branch(key, nodes);
                children.AddLast(branch);
                branches.Add(key, branch);
            }
        }
        public DataNode GetNode(string key) => branches[key].Value;
        public List<DataNode> GetNodes(string key) => branches[key].Values;
        public bool TryGetNode(string key, out DataNode node)
        {
            if(branches.TryGetValue(key, out Branch branch) && branch.TryGetItem(out node))
            {
                return true;
            }
            node = null;
            return false;
        }
        public bool TryGetNodes(string key, out List<DataNode> nodes)
        {
            if(branches.TryGetValue(key, out Branch branch))
            {
                nodes = branch.Values;
                return true;
            }
            nodes = null;
            return false;
        }
        // Queries
        /*public IEnumerable<Tag> QueryTags(string path)
        {
            IEnumerable<Query> queries = Query.ParsePath(path);
            IEnumerable<Query> childQueries = queries.Take(queries.Count() - 1);
            Query keyQuery = queries.Last();
            IEnumerable<DataNode> children = QueryChildren(childQueries);
            foreach(DataNode child in children)
            {
                foreach(string key in child.tags.Keys.Where(key => keyQuery.Regex.IsMatch(key)))
                {
                    yield return child.tags[key];
                }
            }
        }
        public IEnumerable<Branch> QueryNodes(string path)
        {
            IEnumerable<Query> queries = Query.ParsePath(path);
            IEnumerable<Query> childQueries = queries.Take(queries.Count() - 1);
            Query keyQuery = queries.Last();
            IEnumerable<DataNode> children = QueryChildren(childQueries);
            foreach(DataNode child in children)
            {
                foreach(string key in child.branches.Keys.Where(key => keyQuery.Regex.IsMatch(key)))
                {
                    yield return child.branches[key];
                }
            }
        }*/
        // General
        public void AddNewline(int count = 1) => children.AddLast(new Newline(count));
        public void AddComment(string comment) => children.AddLast(new Comment(comment));
        public void Add(string key, DataNode node)
        {
            if(branches.TryGetValue(key, out Branch branch))
            {
                branch.Add(node);
            }
            else
            {
                SetNode(key, node);
            }
        }
        public void Add(string key, IEnumerable<DataNode> nodes)
        {
            if(branches.TryGetValue(key, out Branch branch))
            {
                branch.Add(nodes);
            }
            else
            {
                SetNodes(key, nodes);
            }
        }
        public void Add(string key, IConvertible value)
        {
            if(tags.TryGetValue(key, out Tag tag))
            {
                tag.Add(value);
            }
            else
            {
                SetValue(key, value);
            }
        }
        public void Add(string key, IEnumerable<IConvertible> values)
        {
            if(tags.TryGetValue(key, out Tag tag))
            {
                tag.Add(values);
            }
            else
            {
                SetValues(key, values);
            }
        }
        public void Add(string comment) => AddComment(comment);
        // Output
        public override string ToString() => $"DataNode ({tags.Count} tags, {branches.Count} nodes)";
        public void Write(TextWriter writer) => WriteChildren(writer, "");
        public string Write()
        {
            using(StringWriter writer = new StringWriter())
            {
                Write(writer);
                return writer.ToString();
            }
        }
        public IEnumerator<KeyValuePair<string, DataNode>> GetEnumerator() => throw new NotImplementedException();
        IEnumerator<DataNode> IEnumerable<DataNode>.GetEnumerator()
        {
            foreach(Branch branch in branches.Values)
            {
                foreach(DataNode node in branch.Values)
                {
                    yield return node;
                }
            }
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        #endregion

        #region Helper functions
        private void Write(TextWriter writer, string indent)
        {
            if(children.Count > 0)
            {
                // Open node
                writer.Write($"{{");

                // Write branches, tags, and comments
                WriteChildren(writer, indent + "    ");

                // Close node
                writer.Write($"\n{indent}}}");
            }
        }
        private void WriteChildren(TextWriter writer, string indent)
        {
            // Write branches, tags, and comments
            int childCount = 0;
            foreach(IWritable child in children)
            {
                child.Write(writer, indent);
                if(child is Tag || child is Branch)
                {
                    if(++childCount < tags.Count + branches.Count)
                    {
                        writer.Write(',');
                    }
                }
            }
        }
        private IEnumerable<DataNode> QueryChildren(IEnumerable<Query> queries)
        {
            IEnumerable<DataNode> dataNodes = new List<DataNode>() { this };
            foreach(Query query in queries)
            {
                dataNodes = QueryChildren(dataNodes, query).ToList();
            }
            return dataNodes;
        }
        private IEnumerable<DataNode> QueryChildren(IEnumerable<DataNode> parentNodes, Query query)
        {
            foreach(DataNode parent in parentNodes)
            {
                foreach(string key in branches.Keys.Where(key => query.Regex.IsMatch(key)))
                {
                    foreach(DataNode child in branches[key].Values)
                    {
                        yield return child;
                    }
                }
            }
        }
        #endregion
    }
}

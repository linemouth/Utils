using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils
{
    public class DirectoryContents
    {
        public readonly DirectoryContents Parent = null;
        public readonly DirectoryInfo Root;
        public readonly Ignores Ignores = null;
        public readonly List<FileInfo> Files;
        public readonly List<DirectoryInfo> Dirs;
        public IEnumerable<DirectoryContents> Children => Dirs.Select(dirInfo => new DirectoryContents(dirInfo, this));

        public DirectoryContents(string path) : this(new DirectoryInfo(path)) { }
        public DirectoryContents(string path, Ignores ignores) : this(new DirectoryInfo(path), ignores) { }
        public DirectoryContents(DirectoryInfo rootInfo) : this(rootInfo, null, null) { }
        public DirectoryContents(DirectoryInfo rootInfo, DirectoryContents parent) : this(rootInfo, null, parent) { }
        public DirectoryContents(DirectoryInfo rootInfo, Ignores ignores) : this(rootInfo, ignores, null) { }
        public DirectoryContents(DirectoryInfo rootInfo, Ignores ignores, DirectoryContents parent)
        {
            // Store top-level information
            Parent = parent;
            Root = rootInfo;
            Ignores = ignores;

            // Search for contents
            Files = rootInfo.GetFiles().Where(fileInfo => !IsIgnored(fileInfo)).ToList();
            Dirs = rootInfo.GetDirectories().Where(dirInfo => !IsIgnored(dirInfo)).ToList();
        }

        private bool IsIgnored(FileInfo fileInfo) => (Ignores?.IsIgnored(fileInfo) ?? false) || (Parent?.IsIgnored(fileInfo) ?? false);
        private bool IsIgnored(DirectoryInfo dirInfo) => (Ignores?.IsIgnored(dirInfo) ?? false) || (Parent?.IsIgnored(dirInfo) ?? false);
    }
}

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
        public readonly DirectoryContents parent = null;
        public readonly DirectoryInfo root;
        public readonly FileFilters filters = null;
        public readonly List<FileInfo> files;
        public readonly List<DirectoryInfo> dirs;
        public IEnumerable<DirectoryContents> children => dirs.Select(dirInfo => new DirectoryContents(dirInfo, this));

        public DirectoryContents(string path) : this(new DirectoryInfo(path), null, null) { }
        public DirectoryContents(string path, DirectoryContents parent) : this(new DirectoryInfo(path), parent, null) { }
        public DirectoryContents(string path, FileFilters filters) : this(new DirectoryInfo(path), null, filters) { }
        public DirectoryContents(string path, DirectoryContents parent, FileFilters filters) : this(new DirectoryInfo(path), parent, filters) { }
        public DirectoryContents(DirectoryInfo root) : this(root, null, null) { }
        public DirectoryContents(DirectoryInfo root, DirectoryContents parent) : this(root, parent, null) { }
        public DirectoryContents(DirectoryInfo root, FileFilters filters) : this(root, null, filters) { }
        public DirectoryContents(DirectoryInfo root, DirectoryContents parent, FileFilters filters)
        {
            // Store top-level information
            this.parent = parent;
            this.root = root;
            this.filters = filters;

            // Search for contents
            IEnumerable<FileInfo> foundFiles = root.GetFiles();
            IEnumerable<DirectoryInfo> foundDirs = root.GetDirectories();
            if(filters != null)
            {
                foundFiles = foundFiles.Where(fileInfo => filters.IsIncluded(fileInfo));
                foundDirs = foundDirs.Where(dirInfo => filters.IsIncluded(dirInfo));
            }
            files = foundFiles.ToList();
            dirs = foundDirs.ToList();
        }
    }
}

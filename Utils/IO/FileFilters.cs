using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Utils
{
    public class FileFilters
    {
        public readonly List<Regex> includePatterns;
        public readonly List<Regex> excludePatterns;
        public readonly bool includeHidden;
        public readonly bool includeSystem;

        public FileFilters() : this(new List<Regex>(), new List<Regex>()) { }
        public FileFilters(IEnumerable<string> includePatterns, IEnumerable<string> excludePatterns, bool includeHidden = false, bool includeSystem = false) : this(includePatterns.Select(p => new Regex(p)), excludePatterns.Select(p => new Regex(p)), includeHidden, includeSystem) { }
        public FileFilters(IEnumerable<Regex> includePatterns, IEnumerable<Regex> excludePatterns, bool includeHidden = false, bool includeSystem = false)
        {
            this.includePatterns = includePatterns.ToList();
            this.excludePatterns = excludePatterns.ToList();
            this.includeHidden = includeHidden;
            this.includeSystem = includeSystem;
        }
        public bool IsIncluded(string path)
        {
            FileInfo fileInfo = new FileInfo(path);
            if (fileInfo.Exists)
            {
                return IsIncluded(fileInfo);
            }

            DirectoryInfo dirInfo = new DirectoryInfo(path);
            if (dirInfo.Exists)
            {
                return IsIncluded(dirInfo);
            }

            return false;
        }
        public bool IsIncluded(FileSystemInfo info)
        {
            // Check patterns
            string path = info.GetFormattedPath();
            if(
                (includePatterns.Count > 0 && !includePatterns.Any(pattern => pattern.IsMatch(path)))
                || (excludePatterns.Count > 0 && excludePatterns.Any(pattern => pattern.IsMatch(path)))
            )
            {
                return false;
            }

            // Check attributes
            if(
                (!includeHidden && info.Attributes.HasFlag(FileAttributes.Hidden))
                || (includeSystem && info.Attributes.HasFlag(FileAttributes.System))
            )
            {
                return false;
            }

            return true;
        }
    }
}

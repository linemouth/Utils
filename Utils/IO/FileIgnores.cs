using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Utils
{
    public struct FileIgnores
    {
        public List<Regex> Patterns;
        public FileAttributes Attributes;

        public bool IsIgnored(string path, FileAttributes attributes) => (attributes & Attributes) != 0 || (Patterns?.Any(p => p.IsMatch(path)) ?? false);
    }
}

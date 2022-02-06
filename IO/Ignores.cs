using System.IO;

namespace Utils
{
    public class Ignores
    {
        public FileIgnores Global;
        public FileIgnores Dir;
        public FileIgnores File;

        public bool IsIgnored(FileInfo fileInfo) => Global.IsIgnored(fileInfo.FullName, fileInfo.Attributes) || File.IsIgnored(fileInfo.FullName, fileInfo.Attributes);
        public bool IsIgnored(DirectoryInfo dirInfo) => Global.IsIgnored(dirInfo.FullName, dirInfo.Attributes) || Dir.IsIgnored(dirInfo.FullName, dirInfo.Attributes);
    }
}

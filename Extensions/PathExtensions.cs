using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Utils
{
    public static class PathExtensions
    {
        public static bool IsDirectory(this string path) => new DirectoryInfo(path).Exists;
        public static bool IsFile(this string path) => new FileInfo(path).Exists;
        public static string GetAbsolutePath(this string path) => Path.GetFullPath(path).GetFormattedPath(Directory.Exists(path));
        public static string GetFormattedPath(this DirectoryInfo dirInfo, bool appendSeparator = true) => GetFormattedPath(dirInfo.FullName, appendSeparator);
        public static string GetFormattedPath(this FileInfo fileInfo) => GetFormattedPath(fileInfo.FullName);
        public static string GetFormattedPath(this string path, bool appendSeparator = false) => appendSeparator ? path.Replace('\\', '/').Terminate('/') : path.Replace('\\', '/');
        public static string GetRelativePath(this FileInfo fileInfo, DirectoryInfo dirInfo) => GetRelativePath(fileInfo.FullName, dirInfo.FullName);
        public static string GetRelativePath(this FileInfo fileInfo, string root) => GetRelativePath(fileInfo.FullName, root);
        public static string GetRelativePath(this string path, DirectoryInfo dirInfo) => GetRelativePath(path, dirInfo.FullName);
        public static string GetRelativePath(this string path, string root)
        {
            path = GetFormattedPath(path);
            root = GetFormattedPath(root, true);
            if(path.StartEquals(root))
            {
                return path.Substring(root.Length);
            }
            else
            {
                throw new ArgumentException("Path does not contain root.", "root");
            }
        }
        public static string JoinPath(this string root, string relativePath) => root.GetFormattedPath(true) + relativePath;
        public static string JoinPath(this DirectoryInfo rootInfo, string relativePath) => rootInfo.GetFormattedPath() + relativePath;
        public static IEnumerable<DirectoryContents> Walk(this string path, int depth = int.MaxValue, DirectoryContents parent = null) => Walk(new DirectoryInfo(path), depth, parent);
        public static IEnumerable<DirectoryContents> Walk(this DirectoryInfo rootInfo, int depth = int.MaxValue, DirectoryContents parent = null)
        {
            if(rootInfo.Exists)
            {
                var root = new DirectoryContents(rootInfo, parent);
                yield return root;
                if(depth > 0)
                {
                    foreach(var dir in root.Dirs)
                    {
                        foreach(DirectoryContents subDir in Walk(dir, depth - 1))
                        {
                            yield return subDir;
                        }
                    }
                }
            }
        }
    }
}

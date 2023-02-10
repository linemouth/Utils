using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Utils
{
    public static class PathExtensions
    {
        private struct InfoPair
        {
            public readonly FileSystemInfo sourceInfo;
            public readonly FileSystemInfo destinationInfo;

            public InfoPair(FileSystemInfo source, FileSystemInfo destination)
            {
                sourceInfo = source;
                destinationInfo = destination;
            }
        }

        public static bool IsDirectory(this string path) => new DirectoryInfo(path).Exists;
        public static bool IsFile(this string path) => new FileInfo(path).Exists;
        public static string GetAbsolutePath(this string path) => Path.GetFullPath(path).GetFormattedPath(Directory.Exists(path));
        public static string GetFormattedPath(this FileSystemInfo info, bool appendSeparator = false) => GetFormattedPath(info.FullName, appendSeparator);
        public static string GetFormattedPath(this string path, bool appendSeparator = false) => appendSeparator ? path.Replace('\\', '/').Terminate('/') : path.Replace('\\', '/');
        public static string GetRelativePath(this FileSystemInfo subInfo, DirectoryInfo rootInfo) => GetRelativePath(subInfo.FullName, rootInfo.FullName);
        public static string GetRelativePath(this FileSystemInfo subInfo, string rootPath) => GetRelativePath(subInfo.FullName, rootPath);
        public static string GetRelativePath(this string subPath, DirectoryInfo rootInfo) => GetRelativePath(subPath, rootInfo.FullName);
        public static string GetRelativePath(this string subPath, string rootPath)
        {
            subPath = GetFormattedPath(subPath);
            rootPath = GetFormattedPath(rootPath, true);
            if(subPath.StartEquals(rootPath))
            {
                return subPath.Substring(rootPath.Length);
            }
            else
            {
                throw new ArgumentException("Path does not contain root.", "root");
            }
        }
        public static string JoinPath(this string root, string relativePath) => root.GetFormattedPath(true) + relativePath;
        public static string JoinPath(this DirectoryInfo rootInfo, string relativePath) => rootInfo.GetFormattedPath() + relativePath;
        public static IEnumerable<DirectoryContents> Walk(this string path, int depth = int.MaxValue) => Walk(new DirectoryInfo(path), null, depth, null);
        public static IEnumerable<DirectoryContents> Walk(this string path, DirectoryContents parent) => Walk(new DirectoryInfo(path), parent, int.MaxValue, null);
        public static IEnumerable<DirectoryContents> Walk(this string path, FileFilters filters) => Walk(new DirectoryInfo(path), null, int.MaxValue, filters);
        public static IEnumerable<DirectoryContents> Walk(this string path, int depth, FileFilters filters) => Walk(new DirectoryInfo(path), null, depth, filters);
        public static IEnumerable<DirectoryContents> Walk(this string path, DirectoryContents parent, int depth) => Walk(new DirectoryInfo(path), parent, depth, null);
        public static IEnumerable<DirectoryContents> Walk(this string path, DirectoryContents parent, FileFilters filters) => Walk(new DirectoryInfo(path), parent, int.MaxValue, filters);
        public static IEnumerable<DirectoryContents> Walk(this string path, DirectoryContents parent, int depth, FileFilters filters) => Walk(new DirectoryInfo(path), parent, depth, filters);
        public static IEnumerable<DirectoryContents> Walk(this DirectoryInfo pathInfo, int depth = int.MaxValue) => Walk(pathInfo, null, depth, null);
        public static IEnumerable<DirectoryContents> Walk(this DirectoryInfo pathInfo, DirectoryContents parent) => Walk(pathInfo, parent, int.MaxValue, null);
        public static IEnumerable<DirectoryContents> Walk(this DirectoryInfo pathInfo, FileFilters filters) => Walk(pathInfo, null, int.MaxValue, filters);
        public static IEnumerable<DirectoryContents> Walk(this DirectoryInfo pathInfo, int depth, FileFilters filters) => Walk(pathInfo, null, depth, filters);
        public static IEnumerable<DirectoryContents> Walk(this DirectoryInfo pathInfo, DirectoryContents parent, int depth) => Walk(pathInfo, parent, depth, null);
        public static IEnumerable<DirectoryContents> Walk(this DirectoryInfo pathInfo, DirectoryContents parent, FileFilters filters) => Walk(pathInfo, parent, int.MaxValue, filters);
        public static IEnumerable<DirectoryContents> Walk(this DirectoryInfo pathInfo, DirectoryContents parent, int depth, FileFilters filters)
        {
            if(pathInfo.Exists)
            {
                DirectoryContents root = null;
                try
                {
                    root = new DirectoryContents(pathInfo, parent, filters);
                }
                catch (UnauthorizedAccessException) {}
                if (root != null)
                {
                    yield return root;
                    if (depth > 0)
                    {
                        foreach (var dir in root.dirs)
                        {
                            foreach (DirectoryContents subDir in Walk(dir, root, depth - 1, filters))
                            {
                                yield return subDir;
                            }
                        }
                    }
                }
            }
        }
        public static FileProgress StartCopy(string sourceRoot, string destinationRoot, IEnumerable<string> sourcePaths, out PromisedResult<Dictionary<string, List<string>>> result, FileCopyOptions options, FileFilters filters = null)
        {
            result = new PromisedResult<Dictionary<string, List<string>>>();
            PromisedResult<Dictionary<string, List<string>>> internalResult = result;
            FileProgress progress = new FileProgress("Copying", false);
            progress.Run(p => internalResult.Set(Copy(sourceRoot, destinationRoot, sourcePaths, options, filters, progress)));
            return progress;
        }
        public static Progress StartCopy(string sourcePath, string destPath, FileCopyOptions options) => StartCopy(new FileInfo(sourcePath), new FileInfo(destPath), options);
        public static Progress StartCopy(FileInfo sourceInfo, FileInfo destInfo, FileCopyOptions options)
        {
            FileProgress progress = new FileProgress($"Copying '{sourceInfo.FullName}'", false, sourceInfo.Length);
            progress.Run(p =>
            {
                using(FileStream srcStream = sourceInfo.OpenRead())
                {
                    using(FileStream destStream = destInfo.OpenWrite())
                    {
                        Task task = srcStream.CopyToAsync(destStream);
                        while(!task.IsCompleted)
                        {
                            p.Value = srcStream.Position;
                            task.Wait(50);
                        }
                        p.Value = srcStream.Position;
                    }
                }
            });
            return progress;
        }
        public static void Copy(string sourcePath, string destPath, FileCopyOptions options) => Copy(new FileInfo(sourcePath), new FileInfo(destPath), options);
        public static void Copy(FileInfo sourceInfo, FileInfo destInfo, FileCopyOptions options)
        {
            if(!sourceInfo.Exists)
            {
                throw new FileNotFoundException($"Could not find source file: '{sourceInfo.FullName}'");
            }
            if(destInfo.Exists)
            {
                if(!options.overwrite)
                {
                    throw new Exception($"Unable to overwrite destination file: '{destInfo.FullName}'");
                }
                if(FilesAreEqual(sourceInfo, destInfo, options.compareOptions))
                {

                }
            }
            else
            {
                DirectoryInfo destDirInfo = destInfo.Directory;
                if(!destDirInfo.Exists)
                {
                    destDirInfo.Create();
                }
                sourceInfo.CopyTo(destInfo.FullName, true);
            }
        }
        public static bool FilesAreEqual(string a, string b, FileCompareOptions options) => FilesAreEqual(new FileInfo(a), new FileInfo(b), options);
        public static bool FilesAreEqual(FileInfo a, FileInfo b, FileCompareOptions options)
        {
            FileAttributes attrMask = FileAttributes.ReadOnly | FileAttributes.Hidden | FileAttributes.System | FileAttributes.Directory
                | FileAttributes.Archive | FileAttributes.Normal | FileAttributes.Temporary | FileAttributes.SparseFile
                | FileAttributes.Compressed | FileAttributes.Encrypted | FileAttributes.IntegrityStream | FileAttributes.NoScrubData;
            if(!a.Exists || !b.Exists || a.Length != b.Length || (options.attributes && (a.Attributes & attrMask) != (b.Attributes & attrMask)) || (options.time && a.LastWriteTime != b.LastWriteTime))
            {
                return false;
            }
            if(options.binary || options.header)
            {
                long totalLength = options.binary ? a.Length : Math.Min(a.Length, 0x100000);
                using(FileStream streamA = a.OpenRead())
                {
                    using(FileStream streamB = b.OpenRead())
                    {
                        int bufferSize = 4096;
                        byte[] bufferA = new byte[bufferSize];
                        byte[] bufferB = new byte[bufferSize];
                        while(streamA.Position < totalLength)
                        {
                            streamA.Read(bufferA, 0, bufferSize);
                            streamB.Read(bufferB, 0, bufferSize);
                            for(int i = 0; i < bufferSize; i++)
                            {
                                if(bufferA[i] != bufferB[i])
                                {
                                    return false;
                                }
                            }
                        }
                    }
                }
            }
            return true;
        }
        
        private static Dictionary<string, List<string>> Copy(string sourceRoot, string destRoot, IEnumerable<string> sourcePaths, FileCopyOptions options, FileFilters filters, Progress progress)
        {
            FileProgress fp = (FileProgress)progress;
            List<InfoPair> pairs = new List<InfoPair>();
            Dictionary<string, List<string>> failedFiles = new Dictionary<string, List<string>>();

            // Scan for pairs.
            sourceRoot = sourceRoot.Terminate("/");
            destRoot = destRoot.Terminate("/");
            DirectoryInfo sourceRootInfo = new DirectoryInfo(sourceRoot);
            sourceRoot = sourceRootInfo.GetFormattedPath();
            DirectoryInfo destRootInfo = new DirectoryInfo(destRoot);
            destRoot = destRootInfo.GetFormattedPath();
            if(!destRootInfo.Exists)
            {
                destRootInfo.Create();
            }
            progress.Set(0, 0, "Scanning");
            progress.Suffix = "F";
            foreach(string sourcePath in sourcePaths)
            {
                string localRoot = sourceRoot.JoinPath(sourcePath);
                DirectoryInfo localRootInfo = new DirectoryInfo(localRoot);
                if(localRootInfo.Exists)
                {
                    localRoot = localRootInfo.GetFormattedPath();
                    if(!localRootInfo.Exists)
                    {
                        throw new ArgumentException($"Source directory does not exist: '{localRoot}'.");
                    }
                    foreach(DirectoryContents dirContents in Walk(localRootInfo, filters))
                    {
                        progress.CurrentItem = dirContents.root.FullName;

                        // Ensure directory exists.
                        DirectoryInfo destDirInfo = new DirectoryInfo(dirContents.root.GetFormattedPath().Replace(sourceRoot, destRoot));
                        if(!destDirInfo.Exists)
                        {
                            destDirInfo.Create();
                        }

                        // Scan files.
                        foreach(FileInfo srcFileInfo in dirContents.files)
                        {
                            string src = srcFileInfo.GetFormattedPath();
                            string dest = src.GetFormattedPath().Replace(sourceRoot, destRoot);
                            FileInfo destFileInfo = new FileInfo(dest);
                            ++progress.Total;
                            fp.BytesTotal += srcFileInfo.Length;
                            pairs.Add(new InfoPair(srcFileInfo, destFileInfo));
                        }

                        // Create directories.
                        foreach(DirectoryInfo srcDirInfo in dirContents.dirs)
                        {
                            string src = srcDirInfo.GetFormattedPath();
                            string dest = src.GetFormattedPath().Replace(sourceRoot, destRoot);
                            DirectoryInfo desDirtInfo = new DirectoryInfo(dest);
                            if(!destDirInfo.Exists)
                            {
                                destDirInfo.Create();
                            }
                        }
                    }
                }
                else
                {
                    FileInfo srcFileInfo = new FileInfo(localRoot);
                    string src = srcFileInfo.GetFormattedPath();
                    string dest = src.GetFormattedPath().Replace(sourceRoot, destRoot);
                    FileInfo destFileInfo = new FileInfo(dest);
                    ++progress.Total;
                    fp.BytesTotal += srcFileInfo.Length;
                    pairs.Add(new InfoPair(srcFileInfo, destFileInfo));
                }
            }

            // Copy all pairs.
            progress.Description = "Copying";
            foreach(InfoPair pair in pairs)
            {
                if(pair.sourceInfo is FileInfo srcFileInfo && pair.destinationInfo is FileInfo destFileInfo)
                {
                    progress.CurrentItem = srcFileInfo.FullName;

                    try
                    {
                        // Skip files we don't want to overwrite.
                        if(destFileInfo.Exists && !options.overwrite)
                        {
                            fp.BytesTotal -= srcFileInfo.Length;
                            progress.Total--;
                        }
                        // Update the attributes of identical files.
                        else if(destFileInfo.Exists && FilesAreEqual(srcFileInfo, destFileInfo, options.compareOptions))
                        {
                            // Copy attributes.
                            destFileInfo.IsReadOnly = false;
                            File.SetCreationTime(destFileInfo.FullName, srcFileInfo.CreationTime);
                            File.SetLastWriteTime(destFileInfo.FullName, srcFileInfo.LastWriteTime);
                            File.SetLastAccessTime(destFileInfo.FullName, srcFileInfo.LastAccessTime);
                            File.SetAttributes(destFileInfo.FullName, srcFileInfo.Attributes);
                            
                            fp.BytesTotal -= srcFileInfo.Length;
                            progress.Value++;
                        }
                        else
                        {
                            // Ensure the file is writable if it already exists.
                            if(destFileInfo.Exists)
                            {
                                destFileInfo.IsReadOnly = false;
                            }

                            // If the file is larger than 16MB, copy it progressively.
                            long previousBytes = fp.Bytes;
                            if(srcFileInfo.Length > 0x1000000)
                            {
                                using(FileStream srcStream = srcFileInfo.OpenRead())
                                {
                                    using(FileStream destStream = destFileInfo.Open(FileMode.OpenOrCreate, FileAccess.Write))
                                    {
                                        Task task = srcStream.CopyToAsync(destStream);
                                        while(!task.IsCompleted)
                                        {
                                            fp.Bytes = previousBytes + srcStream.Position;
                                            task.Wait(50);
                                        }
                                    }
                                }

                                /*Progress subprogress = StartCopy(srcFileInfo, destFileInfo, true);
                                subprogress.OnUpdate += () =>
                                {
                                    progress.CurrentItem = $"{srcFileInfo.FullName} ({subprogress.Percent:F2}%)";
                                };
                                subprogress.Task.Wait();
                                progress.Bytes += srcFileInfo.Length;*/

                                // Copy attributes.
                                File.SetCreationTime(destFileInfo.FullName, srcFileInfo.CreationTime);
                                File.SetLastWriteTime(destFileInfo.FullName, srcFileInfo.LastWriteTime);
                                File.SetLastAccessTime(destFileInfo.FullName, srcFileInfo.LastAccessTime);
                                File.SetAttributes(destFileInfo.FullName, srcFileInfo.Attributes);
                            }
                            // Otherwise, just do a normal system copy.
                            else
                            {
                                srcFileInfo.CopyTo(destFileInfo.FullName, true);
                            }

                            fp.Bytes = previousBytes + srcFileInfo.Length;
                            progress.Value++;
                        }
                    }
                    catch(Exception e)
                    {
                        FileError error = new FileError(srcFileInfo.FullName, e.Message);
                        if(failedFiles.TryGetValue(error.message, out List<string> files))
                        {
                            files.Add(error.filename);
                        }
                        else
                        {
                            failedFiles[error.message] = new List<string>() { error.filename };
                        }
                    }
                }
            }

            progress.CurrentItem = null;
            progress.Description = "Copying: Complete";
            return failedFiles;
        }
    }
}

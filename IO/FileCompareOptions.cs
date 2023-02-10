using System;
using System.IO;

namespace Utils
{
    public class FileCompareOptions
    {
        public bool attributes;
        public bool time;
        public bool header;
        public bool binary;

        public FileCompareOptions(bool attributes, bool time, bool header, bool binary)
        {
            this.attributes = attributes;
            this.time = time;
            this.header = header;
            this.binary = binary;
        }
        public FileCompareOptions(string options)
        {
            attributes = options.Contains("attributes");
            time = options.Contains("time");
            header = options.Contains("header");
            binary = options.Contains("binary");
        }
        public bool FilesAreEqual(string pathA, string pathB) => FilesAreEqual(new FileInfo(pathA), new FileInfo(pathB));
        public bool FilesAreEqual(FileInfo a, FileInfo b) => PathExtensions.FilesAreEqual(a, b, this);
    }
}

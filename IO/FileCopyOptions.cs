using System;
using System.Collections.Generic;
using System.Text;

namespace Utils
{
    public class FileCopyOptions
    {
        public bool overwrite;
        public FileCompareOptions compareOptions;

        public FileCopyOptions(bool overwrite, string compareOptions) : this(overwrite, new FileCompareOptions(compareOptions)) { }
        public FileCopyOptions(bool overwrite, FileCompareOptions compareOptions)
        {
            this.overwrite = overwrite;
            this.compareOptions = compareOptions;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Utils
{
    public class FileError
    {
        public string filename;
        public string message;

        public FileError(string filename, string message, string rootPath = null)
        {
            this.filename = filename;
            this.message = message;
            if(rootPath != null)
            {
                string relativePath = this.filename.Replace(rootPath, "");
                this.message.Replace(relativePath, "[...]");
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Utils
{
    public static class CryptographicRandom
    {
        public static void GenerateRandomData(Stream stream, long byteCount, Progress progress = null)
        {
            progress?.Set(0, byteCount, "Generating random data");

            // Initialize buffers
            HashAlgorithm hasher = SHA512.Create();
            int blockSize = hasher.HashSize / sizeof(byte);
        }
    }
}

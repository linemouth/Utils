using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Utils
{
    public static class Hash
    {
        public static string[] Algorithms => algorithms.Keys.ToArray();

        private static readonly OrderedDictionary<string, Func<HashAlgorithm>> algorithms = new OrderedDictionary<string, Func<HashAlgorithm>>
        {
            {"SHA1",   SHA1.Create },
            {"SHA256", SHA256.Create },
            {"SHA384", SHA384.Create },
            {"SHA512", SHA512.Create }
        };

        public static byte[] ComputeHash(this byte[] input, string algorithm = "SHA1") => algorithms[algorithm]().ComputeHash(input);
        public static byte[] ComputeHash(this Stream input, string algorithm = "SHA1") => algorithms[algorithm]().ComputeHash(input);
    }
}

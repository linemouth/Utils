using System;
using System.Linq;
using System.Security.Cryptography;
using Dsa = System.Security.Cryptography.DSA;
// Requires System.Security.Cryptography.Algorithms NuGet package

namespace Utils.Cryptography
{
    public static class DSA
    {
        public static DSAParameters ConstructParametersFromKeys(byte[] keys) => new DSAParameters
        {
            P = keys.Skip(0).Take(128).ToArray(),
            Q = keys.Skip(128).Take(20).ToArray(),
            G = keys.Skip(148).Take(128).ToArray(),
            X = keys.Skip(276).Take(20).ToArray(),
            Y = keys.Skip(296).Take(128).ToArray(),
        };
        public static DSAParameters ConstructParametersFromKeys(byte[] publicKey, byte[] privateKey) => new DSAParameters
        {
            P = publicKey.Skip(0).Take(128).ToArray(),
            Q = publicKey.Skip(128).Take(20).ToArray(),
            G = publicKey.Skip(148).Take(128).ToArray(),
            X = privateKey.Skip(276).Take(20).ToArray(),
            Y = publicKey.Skip(276).Take(128).ToArray(),
        };
        public static DSAParameters ConstructParametersFromPublicKey(byte[] publicKey) => new DSAParameters
        {
            P = publicKey.Skip(0).Take(128).ToArray(),
            Q = publicKey.Skip(128).Take(20).ToArray(),
            G = publicKey.Skip(148).Take(128).ToArray(),
            Y = publicKey.Skip(276).Take(128).ToArray(),
        };
        public static DSAParameters ConstructParametersFromPrivateKey(byte[] privateKey) => new DSAParameters
        {
            P = privateKey.Skip(0).Take(128).ToArray(),
            Q = privateKey.Skip(128).Take(20).ToArray(),
            G = privateKey.Skip(148).Take(128).ToArray(),
            X = privateKey.Skip(276).Take(20).ToArray(),
        };
        public static byte[] GetPublicKey(this DSAParameters p) => p.P.Concat(p.Q).Concat(p.G).Concat(p.Y).ToArray();
        public static byte[] GetPrivateKey(this DSAParameters p) => p.P.Concat(p.Q).Concat(p.G).Concat(p.X).ToArray();
        public static byte[] GetKeys(this DSAParameters p) => p.P.Concat(p.Q).Concat(p.G).Concat(p.X).Concat(p.Y).ToArray();
        /// <summary>Generates new key parameters for the Digital Signature Algorithm of a given key size.</summary>
        /// <param name="keySizeInBits">Must be a multiple of 64 in the range [512, 1024].</param>
        /*public static DSAParameters GenerateParameters(int keySizeInBits = 1024)
        {
            using(Dsa dsa = Dsa.Create(keySizeInBits))
            {
                return dsa.ExportParameters(true);
            }
        }
        public static byte[] Sign(byte[] hash, DSAParameters parameters)
        {
            using(var dsa = Dsa.Create(parameters))
            {
                return dsa.CreateSignature(hash);
            }
        }
        public static bool Verify(byte[] hash, byte[] signature, DSAParameters parameters)
        {
            using(var dsa = Dsa.Create(parameters))
            {
                return dsa.VerifySignature(hash, signature);
            }
        }*/
    }
}
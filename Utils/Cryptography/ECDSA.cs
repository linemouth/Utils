using System;
using System.Linq;
using System.Security.Cryptography;
// Requires System.Security.Cryptography.Algorithms NuGet package

namespace Utils
{
    public static class ECDSA
    {
        public static ECParameters ConstructParametersFromKeys(byte[] keys, ECCurve? curve = null) => new ECParameters
        {
            Curve = curve ?? ECCurve.NamedCurves.nistP384,
            Q = new ECPoint
            {
                X = keys.Take(keys.Length / 3).ToArray(),
                Y = keys.Skip(keys.Length / 3).Take(keys.Length / 3).ToArray()
            },
            D = keys.Skip(keys.Length * 2 / 3).Take(keys.Length / 3).ToArray()
        };
        public static ECParameters ConstructParametersFromKeys(byte[] publicKey, byte[] privateKey, ECCurve? curve = null) => new ECParameters
        {
            Curve = curve ?? ECCurve.NamedCurves.nistP384,
            Q = new ECPoint
            {
                X = publicKey.Take(publicKey.Length / 2).ToArray(),
                Y = publicKey.Skip(publicKey.Length / 2).Take(publicKey.Length / 2).ToArray()
            },
            D = privateKey
        };
        public static ECParameters ConstructParametersFromPublicKey(byte[] publicKey, ECCurve? curve = null) => new ECParameters
        {
            Curve = curve ?? ECCurve.NamedCurves.nistP384,
            Q = new ECPoint
            {
                X = publicKey.Take(publicKey.Length / 2).ToArray(),
                Y = publicKey.Skip(publicKey.Length / 2).Take(publicKey.Length / 2).ToArray()
            }
        };
        public static ECParameters ConstructParametersFromPrivateKey(byte[] privateKey, ECCurve? curve = null) => new ECParameters
        {
            Curve = curve ?? ECCurve.NamedCurves.nistP384,
            D = privateKey
        };
        public static byte[] GetPublicKey(this ECParameters p) => p.Q.X.Concat(p.Q.Y).ToArray();
        public static byte[] GetPrivateKey(this ECParameters p) => p.D;
        public static byte[] GetKeys(this ECParameters p) => p.Q.X.Concat(p.Q.Y).Concat(p.D).ToArray();
        /// <summary>Generates a new key for use with the Elliptic-Curve Digital Signature Algorithm using a given curve.</summary>
        /// <param name="curve">The parameters or named curve to use.</param>
        public static ECParameters GenerateKey(ECCurve? curve = null)
        {
            using(var ecdsa = ECDsa.Create(curve ?? ECCurve.NamedCurves.nistP384))
            {
                return ecdsa.ExportParameters(true);
            }
        }
        public static byte[] Sign(byte[] hash, byte[] publicKey, byte[] privateKey, ECCurve? curve = null) => ECDsa.Create(ConstructParametersFromKeys(privateKey, publicKey, curve)).SignHash(hash);
        public static bool Verify(byte[] hash, byte[] publicKey, byte[] signature, ECCurve? curve = null) => ECDsa.Create(ConstructParametersFromKeys(publicKey, curve)).VerifyHash(hash, signature);
    }
}

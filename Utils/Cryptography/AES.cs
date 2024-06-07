using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using SSC = System.Security.Cryptography;

namespace Utils
{
    public static class AES
    {
        public static byte[] Encrypt(byte[] input, byte[] key, byte[] iv)
        {
            using(MemoryStream inputStream = new MemoryStream(input, false))
            {
                using(MemoryStream outputStream = new MemoryStream(input.Length))
                {
                    Encrypt(inputStream, outputStream, key, iv);
                    return outputStream.ToArray();
                }
            }
        }
        public static long Encrypt(Stream input, Stream output, byte[] key, byte[] iv)
        {
            ICryptoTransform transform = Aes.Create().CreateEncryptor(key, iv);
            long bytesEncrypted = 0;
            int bufferSize = (int)input.Length.Clamp(0x100, 0x10000000);
            byte[] inputBuffer = new byte[bufferSize];
            byte[] outputBuffer = new byte[bufferSize];
            while(input.Position < input.Length)
            {
                int blockSize = (int)Math.Min(bufferSize, input.Length - input.Position);
                input.Read(inputBuffer, 0, blockSize);
                transform.TransformBlock(inputBuffer, 0, blockSize, outputBuffer, 0);
                output.Write(outputBuffer, 0, blockSize);
                bytesEncrypted += blockSize;
            }
            return bytesEncrypted;
        }


        /*public static byte[] SymmetricEncrypt(SymmetricEncryptionAlgorithm algorithm, byte[] data, byte[] key, byte[] iv = null)
        {
            ICryptoTransform transform = null;
            switch(algorithm)
            {
                case SymmetricEncryptionAlgorithm.AES:
                    transform = Aes.Create().CreateDecryptor(key, iv);
                    break;
            }
            if(transform != null)
            {

            }
            return null;
        }
        public static byte[] AsymmetricEncrypt(AsymmetricEncryptionAlgorithm algorithm, byte[] data, byte[] publicKey, byte[] privateKey)
        {
            ICryptoTransform transform = null;
            switch(algorithm)
            {
                case AsymmetricEncryptionAlgorithm.RSA:
                    RSA rsa = RSA.Create();
                    transform = Aes.Create().CreateDecryptor(key, iv);
                    break;
            }
            if(transform != null)
            {

            }
            return null;
        }
        public static byte[] AsymmetricDecrypt(AsymmetricEncryptionAlgorithm algorithm, byte[] data, byte[] publicKey)
        {
            ICryptoTransform transform = null;
            switch(algorithm)
            {
                case AsymmetricEncryptionAlgorithm.RSA:
                    transform = Aes.Create().CreateDecryptor(key, iv);
                    break;
            }
            if(transform != null)
            {

            }
            return null;
        }
        public static byte[] Sign(this byte[] data, SigningAlgorithm algorithm, byte[] publicKey, byte[] privateKey)
        {
            return null;
        }
        public static bool Verify(this byte[] data, SigningAlgorithm algorithm, byte[] publicKey, byte[] signature)
        {
            return false;
        }
        public static KeyPair GenerateRsaKeyPair(int keySize = 2048)
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048);
            RSAParameters parameters = rsa.ExportParameters(true);
            return new KeyPair(parameters.P, parameters.Q);
        }*/
    }
}

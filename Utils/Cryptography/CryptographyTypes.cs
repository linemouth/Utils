using System;
using System.Collections.Specialized;

namespace Utils
{
    public class KeyPair
    {
        public byte[] PublicKey { get; set; }
        public byte[] PrivateKey { get; set; }

        public KeyPair(byte[] publicKey, byte[] privateKey)
        {
            PublicKey = publicKey;
            PrivateKey = privateKey;
        }
    }
    public class Key
    {
        public string Name { get; set; }
        public byte[] Value { get; set; }

        public Key(string name, byte[] value)
        {
            Name = name;
            Value = value;
        }
    }
    public class KeySet : OrderedDictionary { }
}

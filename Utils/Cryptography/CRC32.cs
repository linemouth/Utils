using System;
using System.Collections.Generic;

namespace Utils
{
    public static class Crc32
    {
        public const uint defaultPolynomial = 0x04C11DB7; // CCITT standard polynomial

        private static readonly Dictionary<uint, uint[]> tables = new Dictionary<uint, uint[]>();

        public static uint Calculate(byte[] data, uint initial = 0, uint polynomial = defaultPolynomial) => Calculate(data, true, initial, polynomial);
        public static uint Calculate(byte[] data, bool useTable = true, uint initial = 0, uint polynomial = defaultPolynomial)
        {
            uint crc = ~initial;
            if(useTable)
            {
                uint[] table = GetTable(polynomial);
                foreach(byte b in data)
                {
                    crc = (crc >> 8) ^ table[(crc ^ b) & 0xFF];
                }
            }
            else
            {
                uint reflectedPolynomial = polynomial.Reflect();
                foreach(byte b in data)
                {
                    crc ^= b;
                    for(int i = 0; i < 8; ++i)
                    {
                        uint t = ~((crc & 1) - 1);
                        crc = (crc >> 1) ^ (reflectedPolynomial & t);
                    }
                }
            }
            return ~crc;
        }

        // Calculates the lookup table to be used by Calculate(). For efficiency, the lookup table for a particular
        // polynomial is generated the first time it is requested, then it is stored until the program ends.
        private static uint[] GetTable(uint polynomial)
        {
            if(!tables.ContainsKey(polynomial))
            {
                uint reflectedPolynomial = polynomial.Reflect();
                uint[] table = new uint[256];
                for(uint i = 0; i < table.Length; ++i)
                {
                    uint entry = i;
                    for(uint j = 0; j < 8; ++j)
                        if((entry & 1) == 1)
                            entry = (entry >> 1) ^ reflectedPolynomial;
                        else
                            entry >>= 1;
                    table[i] = entry;
                }
                tables[polynomial] = table;
            }
            return tables[polynomial];
        }
    }
}

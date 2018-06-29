using System;
using System.Text;

namespace ACNESCreator.Core
{
    public static class Utility
    {
        public static byte[] GetPaddedData(byte[] Data, int MaxLength, byte PaddingValue = 0)
        {
            int Length = Data.Length;
            if (Length == MaxLength)
            {
                return Data;
            }

            Array.Resize(ref Data, MaxLength);
            if (PaddingValue != 0 && Length < MaxLength)
            {
                for (int i = Length; i < Data.Length; i++)
                {
                    Data[i] = PaddingValue;
                }
            }

            return Data;
        }

        public static byte[] GetPaddedStringData(string Input, int MaxLength, byte PaddingValue = 0)
            => GetPaddedData(Encoding.ASCII.GetBytes(Input), MaxLength, PaddingValue);
    }
}

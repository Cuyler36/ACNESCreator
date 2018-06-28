using System;
using System.Collections.Generic;
using System.Text;

namespace ACNESCreator.Core
{
    public static class Utility
    {
        public static byte[] GetPaddedStringData(string Input, int MaxLength, byte PaddingValue = 0)
        {
            byte[] Data = Encoding.ASCII.GetBytes(Input);
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
    }
}

using System;
using System.Collections.Generic;
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

        // From https://github.com/Daniel-McCarthy/Mr-Peeps-Compressor/blob/6c08b20c079fdb3e48400421f0385f02c5a55a05/PeepsCompress/PeepsCompress/Abstract%20Classes/SlidingWindowAlgorithm.cs
        public static int[] FindAllMatches(ref List<byte> dictionary, byte match)
        {
            List<int> matchPositons = new List<int>();

            for (int i = 0; i < dictionary.Count; i++)
            {
                if (dictionary[i] == match)
                {
                    matchPositons.Add(i);
                }
            }

            return matchPositons.ToArray();
        }

        public static int[] FindLargestMatch(ref List<byte> dictionary, int[] matchesFound, ref byte[] file, int fileIndex, int maxMatch)
        {
            int[] matchSizes = new int[matchesFound.Length];

            for (int i = 0; i < matchesFound.Length; i++)
            {
                int matchSize = 1;
                bool matchFound = true;

                while (matchFound && matchSize < maxMatch && (fileIndex + matchSize < file.Length) && (matchesFound[i] + matchSize < dictionary.Count)) //NOTE: This could be relevant to compression issues? I suspect it's more related to writing
                {
                    if (file[fileIndex + matchSize] == dictionary[matchesFound[i] + matchSize])
                    {
                        matchSize++;
                    }
                    else
                    {
                        matchFound = false;
                    }

                }

                matchSizes[i] = matchSize;
            }

            int[] bestMatch = new int[2];

            bestMatch[0] = matchesFound[0];
            bestMatch[1] = matchSizes[0];

            for (int i = 1; i < matchesFound.Length; i++)
            {
                if (matchSizes[i] > bestMatch[1])
                {
                    bestMatch[0] = matchesFound[i];
                    bestMatch[1] = matchSizes[i];
                }
            }

            return bestMatch;

        }
    }
}

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

        /* Adapted from: https://gist.github.com/infval/18d65dd034290fb908f589dcc10c6d25 */
        private static int FDSCRC(in byte[] data, int start, int end)
        {
            int s = 0x8000;

            // Process each byte in the data array
            for (; start < end; start++)
            {
                byte b = data[start];
                s |= b << 16;
                for (int i = 0; i < 8; i++)
                {
                    if ((s & 1) != 0)
                    {
                        s ^= 0x8408 << 1;
                    }
                    s >>= 1;
                }
            }

            // Process two additional 0x00 bytes
            for (int j = 0; j < 2; j++)
            {
                for (int i = 0; i < 8; i++)
                {
                    if ((s & 1) != 0)
                    {
                        s ^= 0x8408 << 1;
                    }
                    s >>= 1;
                }
            }

            return s;
        }

        private static void InsertCrc(in byte[] data, ref byte[] qdData, ref int qdWriteIdx, int start, int end)
        {
            int crc = FDSCRC(data, start, end);
            qdData[qdWriteIdx++] = (byte)(crc & 0xFF);
            qdData[qdWriteIdx++] = (byte)(crc >> 8);
        }

        public static byte[] ConvertFDSToQD(in byte[] disk)
        {
            int pos = 0;

            if (Encoding.ASCII.GetString(disk, 0, 3) == "FDS")
            {
                pos = 16; // skip FDS added to the beginning
            }

            if (disk[pos] != 0x01)
            {
                return Array.Empty<byte>();
            }

            byte[] qdData = new byte[0x10000];
            int qdWriteIdx = 0;

            Buffer.BlockCopy(disk, pos, qdData, qdWriteIdx, 0x38);
            qdWriteIdx += 0x38;
            InsertCrc(disk, ref qdData, ref qdWriteIdx, pos, pos + 0x38);
            Buffer.BlockCopy(disk, pos + 0x38, qdData, qdWriteIdx, 2);
            qdWriteIdx += 2;
            InsertCrc(disk, ref qdData, ref qdWriteIdx, pos + 0x38, pos + 0x3A);
            pos += 0x3A;

            try
            {
                while (disk[pos] == 3)
                {
                    int fileSize = (disk[pos + 0xD]) | (disk[pos + 0xE] << 8);
                    Buffer.BlockCopy(disk, pos, qdData, qdWriteIdx, 0x10);
                    qdWriteIdx += 0x10;
                    InsertCrc(disk, ref qdData, ref qdWriteIdx, pos, pos + 0x10);
                    pos += 0x10;
                    Buffer.BlockCopy(disk, pos, qdData, qdWriteIdx, fileSize + 1);
                    qdWriteIdx += fileSize + 1;
                    InsertCrc(disk, ref qdData, ref qdWriteIdx, pos, pos + 1 + fileSize);
                    pos += 1 + fileSize;
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                return Array.Empty<byte>();
            }

            return qdData;
        }
    }
}

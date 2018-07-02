using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ACNESCreator.Core
{
    public static class Yaz0
    {
        /// <summary>
        /// Verifies that the supplied byte array is Yaz0 compressed.
        /// </summary>
        /// <param name="Data">The Yaz0 compressed data array</param>
        /// <returns>IsDataYaz0Compressed</returns>
        public static bool IsYaz0(byte[] Data)
            => Data.Length > 0x10 && Encoding.ASCII.GetString(Data, 0, 4).Equals("Yaz0");

        /// <summary>
        /// Decompresses Yaz0 compressed data.
        /// </summary>
        /// <param name="Data">The Yaz0 compressed data array</param>
        /// <returns>The decompressed data</returns>
        public static byte[] Decompress(byte[] Data)
        {
            if (!IsYaz0(Data))
            {
                throw new ArgumentException("The supplied data does not appear to be Yaz0 compressed!");
            }

            uint Size = (uint)(Data[4] << 24 | Data[5] << 16 | Data[6] << 8 | Data[7]);
            byte[] Output = new byte[Size];
            int ReadOffset = 16;
            int OutputOffset = 0;

            while (true)
            {
                byte Bitmap = Data[ReadOffset++];
                for (int i = 0; i < 8; i++)
                {
                    if ((Bitmap & 0x80) != 0)
                    {
                        Output[OutputOffset++] = Data[ReadOffset++];
                    }
                    else
                    {
                        byte b = Data[ReadOffset++];
                        int OffsetAdjustment = ((b & 0xF) << 8 | Data[ReadOffset++]) + 1;
                        int Length = (b >> 4) + 2;
                        if (Length == 2)
                        {
                            Length = Data[ReadOffset++] + 0x12;
                        }

                        for (int j = 0; j < Length; j++)
                        {
                            Output[OutputOffset] = Output[OutputOffset - OffsetAdjustment];
                            OutputOffset++;
                        }
                    }

                    Bitmap <<= 1;

                    if (OutputOffset >= Size)
                    {
                        return Output;
                    }
                }
            }
        }

        public static byte[] Compress(byte[] file)
        {
            List<byte> layoutBits = new List<byte>();
            List<byte> dictionary = new List<byte>();

            List<byte> uncompressedData = new List<byte>();
            List<int[]> compressedData = new List<int[]>();

            int maxDictionarySize = 4096;
            int minMatchLength = 3;
            int maxMatchLength = 255 + 0x12;
            int decompressedSize = 0;

            for (int i = 0; i < file.Length; i++)
            {

                if (dictionary.Contains(file[i]))
                {
                    //compressed data
                    int[] matches = Utility.FindAllMatches(ref dictionary, file[i]);
                    int[] bestMatch = Utility.FindLargestMatch(ref dictionary, matches, ref file, i, maxMatchLength);

                    if (bestMatch[1] >= minMatchLength)
                    {
                        layoutBits.Add(0);
                        bestMatch[0] = dictionary.Count - bestMatch[0];

                        for (int j = 0; j < bestMatch[1]; j++)
                        {
                            dictionary.Add(file[i + j]);
                        }

                        i = i + bestMatch[1] - 1;

                        compressedData.Add(bestMatch);
                        decompressedSize += bestMatch[1];
                    }
                    else
                    {
                        //uncompressed data
                        layoutBits.Add(1);
                        uncompressedData.Add(file[i]);
                        dictionary.Add(file[i]);
                        decompressedSize++;
                    }
                }
                else
                {
                    //uncompressed data
                    layoutBits.Add(1);
                    uncompressedData.Add(file[i]);
                    dictionary.Add(file[i]);
                    decompressedSize++;
                }

                if (dictionary.Count > maxDictionarySize)
                {
                    int overflow = dictionary.Count - maxDictionarySize;
                    dictionary.RemoveRange(0, overflow);
                }
            }

            return buildYAZ0CompressedBlock(ref layoutBits, ref uncompressedData, ref compressedData, decompressedSize);
        }

        public static byte[] buildYAZ0CompressedBlock(ref List<byte> layoutBits, ref List<byte> uncompressedData, ref List<int[]> offsetLengthPairs, int decompressedSize)
        {
            List<byte> finalYAZ0Block = new List<byte>();
            List<byte> layoutBytes = new List<byte>();
            List<byte> compressedDataBytes = new List<byte>();
            List<byte> extendedLengthBytes = new List<byte>();

            //add Yaz0 magic number & decompressed file size
            finalYAZ0Block.AddRange(Encoding.ASCII.GetBytes("Yaz0"));
            finalYAZ0Block.AddRange(BitConverter.GetBytes(decompressedSize.Reverse()));

            //add 8 zeros per format specification
            for (int i = 0; i < 8; i++)
            {
                finalYAZ0Block.Add(0);
            }

            //assemble layout bytes
            while (layoutBits.Count > 0)
            {
                byte B = 0;
                for (int i = 0; i < (8 > layoutBits.Count ? layoutBits.Count : 8); i++)
                {
                    B |= (byte)(layoutBits[i] << (7 - i));
                }

                layoutBytes.Add(B);
                layoutBits.RemoveRange(0, (layoutBits.Count < 8) ? layoutBits.Count : 8);
            }

            //assemble offsetLength shorts
            foreach (int[] offsetLengthPair in offsetLengthPairs)
            {
                //if < 18, set 4 bits -2 as matchLength
                //if >= 18, set matchLength == 0, write length to new byte - 0x12

                int adjustedOffset = offsetLengthPair[0];
                int adjustedLength = (offsetLengthPair[1] >= 18) ? 0 : offsetLengthPair[1] - 2; //vital, 4 bit range is 0-15. Number must be at least 3 (if 2, when -2 is done, it will think it is 3 byte format), -2 is how it can store up to 17 without an extra byte because +2 will be added on decompression

                if (adjustedLength == 0)
                {
                    extendedLengthBytes.Add((byte)(offsetLengthPair[1] - 18));
                }

                int compressedInt = ((adjustedLength << 12) | adjustedOffset - 1);

                byte[] compressed2Byte = new byte[2];
                compressed2Byte[0] = (byte)(compressedInt & 0xFF);
                compressed2Byte[1] = (byte)((compressedInt >> 8) & 0xFF);

                compressedDataBytes.Add(compressed2Byte[1]);
                compressedDataBytes.Add(compressed2Byte[0]);
            }

            //add rest of file
            for (int i = 0; i < layoutBytes.Count; i++)
            {
                finalYAZ0Block.Add(layoutBytes[i]);
                byte LayoutByte = layoutBytes[i];

                for (int j = 7; ((j > -1) && ((uncompressedData.Count > 0) || (compressedDataBytes.Count > 0))); j--)
                {
                    if (((LayoutByte >> j) & 1) == 1)
                    {
                        finalYAZ0Block.Add(uncompressedData[0]);
                        uncompressedData.RemoveAt(0);
                    }
                    else
                    {
                        if (compressedDataBytes.Count > 0)
                        {
                            int length = compressedDataBytes[0] >> 4;

                            finalYAZ0Block.Add(compressedDataBytes[0]);
                            finalYAZ0Block.Add(compressedDataBytes[1]);
                            compressedDataBytes.RemoveRange(0, 2);

                            if (length == 0)
                            {
                                finalYAZ0Block.Add(extendedLengthBytes[0]);
                                extendedLengthBytes.RemoveAt(0);
                            }
                        }
                    }
                }
            }

            return finalYAZ0Block.ToArray();
        }
    }
}

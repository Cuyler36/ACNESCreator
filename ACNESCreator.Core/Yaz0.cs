using System;
using System.IO;
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

        private static uint Encode(byte[] Source, int Size, int Position, ref uint MatchPosition)
        {
            MatchPosition = 0;

            int startPosition = Position - 0x1000;
            uint byteCount = 1;
            int i = 0;
            int j = 0;

            if (startPosition < 0)
                startPosition = 0;

            for (i = startPosition; i < Position; i++)
            {
                for (j = 0; j < Size - Position; j++)
                {
                    if (Source[i + j] != Source[j + Position])
                        break;
                }

                if (j > byteCount)
                {
                    byteCount = (uint)j;
                    MatchPosition = (uint)i;
                }
            }

            if (byteCount == 2)
                byteCount = 1;

            return byteCount;
        }

        private static bool previousFlag = false;
        private static uint previousByteCount = 0;
        private static uint previousMatchPosition = 0;

        private static uint NintendoEncode(byte[] Source, int Size, int Position, ref uint MatchPosition)
        {
            uint byteCount = 1;

            if (previousFlag == true)
            {
                MatchPosition = previousMatchPosition;
                previousFlag = false;
                return previousByteCount;
            }

            byteCount = Encode(Source, Size, Position, ref previousMatchPosition);
            MatchPosition = previousMatchPosition;

            if (byteCount >= 3)
            {
                previousByteCount = Encode(Source, Size, Position + 1, ref previousMatchPosition);

                if (previousByteCount >= byteCount + 2)
                {
                    byteCount = 1;
                    previousFlag = true;
                }
            }

            return byteCount;
        }

        /*
         * Compresses a file using Yaz0 compression
         * 
         * Params:
         *  @Data = data array to compress
         * 
         * Returns:
         *  @byte[] CompressedData = a byte array containing a file header ["Yaz0", Decompress Size] and the compressed data
         */
        public static byte[] Compress(byte[] Data)
        {
            int OutputBufferSize = 0;
            int SourcePosition = 0;
            int WritePosition = 0;
            byte[] OutputBuffer = new byte[24];
            MemoryStream OutputStream = new MemoryStream();

            uint ValidBitCount = 0;
            byte CurrentCodeByte = 0;
            uint ByteCount = 0;
            uint MatchPosition = 0;
            byte A = 0, B = 0, C = 0;

            while (SourcePosition < Data.Length)
            {
                ByteCount = 0;
                MatchPosition = 0;

                ByteCount = NintendoEncode(Data, Data.Length, SourcePosition, ref MatchPosition);

                if (ByteCount < 3)
                {
                    OutputBuffer[WritePosition] = Data[SourcePosition];
                    WritePosition++;
                    SourcePosition++;
                    CurrentCodeByte |= (byte)(0x80 >> (int)ValidBitCount);
                }
                else
                {
                    uint Distance = (uint)(SourcePosition - MatchPosition - 1);
                    A = 0;
                    B = 0;
                    C = 0;

                    if (ByteCount >= 0x12)
                    {
                        A = (byte)(0 | (Distance >> 8));
                        B = (byte)(Distance & 0xFF);
                        OutputBuffer[WritePosition++] = A;
                        OutputBuffer[WritePosition++] = B;

                        if (ByteCount > 0xFF + 0x12)
                        {
                            ByteCount = 0xFF + 0x12;
                        }

                        C = (byte)(ByteCount - 0x12);
                        OutputBuffer[WritePosition++] = C;
                    }
                    else
                    {
                        A = (byte)(((ByteCount - 2) << 4) | (Distance >> 8));
                        B = (byte)(Distance & 0xFF);
                        OutputBuffer[WritePosition++] = A;
                        OutputBuffer[WritePosition++] = B;
                    }
                    SourcePosition += (int)ByteCount;
                }

                ValidBitCount++;

                if (ValidBitCount == 8)
                {
                    OutputStream.WriteByte(CurrentCodeByte);
                    OutputStream.Write(OutputBuffer, 0, WritePosition);
                    OutputBufferSize += WritePosition + 1;
                    OutputStream.Position = OutputBufferSize;

                    CurrentCodeByte = 0;
                    ValidBitCount = 0;
                    WritePosition = 0;
                }
            }

            if (ValidBitCount > 0)
            {
                OutputStream.WriteByte(CurrentCodeByte);
                OutputStream.Write(OutputBuffer, 0, WritePosition);
                OutputBufferSize += WritePosition + 1;
                OutputStream.Position = OutputBufferSize;

                CurrentCodeByte = 0;
                ValidBitCount = 0;
                WritePosition = 0;
            }

            byte[] FileData = new byte[OutputStream.Length + 0x10];
            Encoding.ASCII.GetBytes("Yaz0").CopyTo(FileData, 0);
            BitConverter.GetBytes(Data.Length.Reverse()).ToArray().CopyTo(FileData, 4);
            OutputStream.ToArray().CopyTo(FileData, 0x10);

            return FileData;
        }
    }
}

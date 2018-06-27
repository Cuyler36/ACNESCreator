using System;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections.Generic;

namespace Animal_Crossing_NES_Game_Creator
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args == null || args.Length < 1)
            {
                Console.WriteLine("Enter the name of the NES game: ");
                args = new string[1] { Console.ReadLine() };
            }
            if (args.Length < 2)
            {
                Console.WriteLine("Enter the path to the NES game to inject: ");
                args = new string[2] { args[0], Console.ReadLine().Replace("\"", "") };
            }

            if (args[0].Length >= 4 && File.Exists(args[1]))
            {
                byte[] NESData = File.ReadAllBytes(args[1]);
                if (Encoding.ASCII.GetString(NESData, 0, 3) == "NES")
                {
                    // Pad name with spaces
                    string Name = args[0];
                    if (Name.Length < 0x10)
                    {
                        for (int i = Name.Length; i < 0x10; i++)
                        {
                            Name += (char)(0x20);
                        }
                    }

                    byte[] ACData = new byte[NESData.Length + 0x60];
                    byte[] NameData = Encoding.ASCII.GetBytes("ZZ" + Name);

                    Array.Resize(ref NameData, 0x12);

                    NameData.CopyTo(ACData, 0x40);
                    // Copy Game Size
                    ushort ResizedData = (ushort)(NESData.Length >> 4);
                    BitConverter.GetBytes(ResizedData.Reverse()).CopyTo(ACData, 0x52);

                    // Copy the NES ROM Data
                    NESData.CopyTo(ACData, 0x60);

                    // Create a new Blank GCI File
                    GCI NESGCIFile = new GCI
                    {
                        Comment2 = args[0] + " NES Game",
                        Data = ACData
                    };
                    // Set the File Name
                    NESGCIFile.Header.FileName = "DobutsunomoriP_F_" + args[0].Substring(0, 4).ToUpper();

                    // Write the newly created GCI file
                    string OutputFile = Path.GetDirectoryName(args[1]) + "\\" + args[0] + "_InjectedData.gci";
                    using (var Stream = new FileStream(OutputFile, FileMode.OpenOrCreate))
                    {
                        byte[] Data = NESGCIFile.GetData();
                        SetChecksum(ref Data);
                        Stream.Write(Data, 0, Data.Length);
                    }

                    Console.WriteLine("Successfully generated a GCI file with the NES rom in it!\nFile Location: " + OutputFile);
                    Console.ReadLine();
                }
            }
            else
            {
                Console.WriteLine("The NES name must be four or more characters long!");
                Console.ReadLine();
            }
        }

        internal static void SetChecksum(ref byte[] Data)
        {
            byte Checksum = 0;
            for (int i = 0x40; i < Data.Length; i++) // Skip the header by adding 0x40
            {
                Checksum += Data[i];
            }

            Data[Data.Length - 1] = (byte)-Checksum;
        }

        internal static byte[] SetHasBannerImage(ref byte[] ACData, byte[] BannerData, byte[] NESData)
        {
            ACData[0x1C] |= 0x80; // Upper bit is the banner image flag
            ushort BannerSize = (ushort)BannerData.Length;
            BitConverter.GetBytes(BannerSize.Reverse()).CopyTo(ACData, 0x5A);

            List<byte> ConcatenatedData = new List<byte>();
            ConcatenatedData.AddRange(ACData);
            ConcatenatedData.AddRange(BannerData);
            ConcatenatedData.AddRange(NESData);

            return ConcatenatedData.ToArray();
        }
    }
}

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

                    List<byte> FinalData = new List<byte>();

                    byte[] ACData = new byte[0x60];
                    byte[] NameData = Encoding.ASCII.GetBytes("\0Z" + Name);

                    Array.Resize(ref NameData, 0x12);

                    NameData.CopyTo(ACData, 0x40);
                    // Copy Game Size
                    ushort ResizedData = (ushort)(NESData.Length >> 4);
                    BitConverter.GetBytes(ResizedData.Reverse()).CopyTo(ACData, 0x52);

                    // Setup TagInfo Data Size
                    List<byte> TagInfo = new List<byte>();
                    TagInfo.AddRange(Encoding.ASCII.GetBytes("END\0"));
                    while (TagInfo.Count % 16 != 0)
                    {
                        TagInfo.Add(0); // All entries must be aligned to 0x10 bytes
                    }

                    // Copy TagInfo Data Size
                    ushort TagDataSize = (ushort)TagInfo.Count;
                    BitConverter.GetBytes(TagDataSize.Reverse()).CopyTo(ACData, 0x54);

                    // Set game can save flag
                    ACData[0x5C] = (0 << 0) | (1 << 1) | (0 << 3) | (3 << 5) | (1 << 7); // Set unknown attribute to 0, IconInfo to use Internal Icon, BannerInfo to No Banner, CommentInfo to Copy whole, & CanSave to true

                    // Create Save Comment Data
                    List<byte> BannerData = new List<byte>();
                    string Comment1 = "Animal Crossing Custom NES Save";
                    string Comment2 = string.Format("{0} NES Save Data", args[0]);

                    BannerData.AddRange(Encoding.ASCII.GetBytes(Comment1));
                    while (BannerData.Count < 0x20)
                    {
                        BannerData.Add(0);
                    }

                    if (Comment2.Length > 32)
                    {
                        Comment2 = Comment2.Substring(0, 32);
                    }

                    BannerData.AddRange(Encoding.ASCII.GetBytes(Comment2));
                    while (BannerData.Count < 0x40)
                    {
                        BannerData.Add(0);
                    }

                    //BannerData.AddRange(GCI.DefaultIconData.Take(0x520));
                    ushort BannerDataLength = (ushort)BannerData.Count;
                    // Set Banner/Comment Data Size
                    BitConverter.GetBytes(BannerDataLength.Reverse()).CopyTo(ACData, 0x5A);

                    // Set Icon Type
                    BitConverter.GetBytes(((ushort)IconFormats.Shared_CI8).Reverse()).CopyTo(ACData, 0x56);

                    // Copy Header Data
                    FinalData.AddRange(ACData);

                    // Copy the TagInfo Data
                    FinalData.AddRange(TagInfo);

                    // Copy Banner Data
                    FinalData.AddRange(BannerData);

                    // Copy the NES ROM Data
                    FinalData.AddRange(NESData);

                    // Create a new Blank GCI File
                    GCI NESGCIFile = new GCI
                    {
                        Comment2 = args[0] + " NES Game",
                        Data = FinalData.ToArray()
                    };
                    // Set the File Name
                    NESGCIFile.Header.FileName = "DobutsunomoriP_F_" + args[0].Substring(0, 4).ToUpper();

                    // Write the newly created GCI file
                    string OutputFile = Path.GetDirectoryName(args[1]) + "\\" + args[0] + "_InjectedData.gci";
                    using (var Stream = new FileStream(OutputFile, FileMode.OpenOrCreate))
                    {
                        byte[] Data = NESGCIFile.GetData();
                        SetChecksum(ref Data);
                        PrintChecksum(Data);
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

            Data[0x680] = (byte)-Checksum;
        }

        internal static void PrintChecksum(byte[] Data)
        {
            byte Checksum = 0;
            for (int i = 0x40; i < Data.Length; i++) // Skip the header by adding 0x40
            {
                Checksum += Data[i];
            }

            Console.WriteLine("Checksum: 0x" + Checksum.ToString("X2"));
        }
    }
}

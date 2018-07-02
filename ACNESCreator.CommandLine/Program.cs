using ACNESCreator.Core;
using System;
using System.IO;

namespace ACNESCreator.CommandLine
{
    class Program
    {
        readonly static string[] RegionCodes = new string[4] { "J", "E", "P", "U" };

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
            if (args.Length < 3)
            {
                Console.WriteLine("Enter your Animal Crossing game's region (J for Japan, E for North America, P for Europe, and U for Austrailia:");
                args = new string[3] { args[0], args[1], Console.ReadLine() };
            }

            if (args[0].Length >= 4 && File.Exists(args[1]))
            {
                int RegionIdx = Array.IndexOf(RegionCodes, args[2].ToUpper());
                if (RegionIdx > -1)
                {
                    NES NESFile = new NES(args[0], File.ReadAllBytes(args[1]), (Region)RegionIdx, false, false);

                    string OutputFile = Path.GetDirectoryName(args[1]) + "\\" + args[0] + "_" + args[2].ToUpper() + "_InjectedData.gci";
                    using (var Stream = new FileStream(OutputFile, FileMode.OpenOrCreate))
                    {
                        byte[] Data = NESFile.GenerateGCIFile();
                        PrintChecksum(Data);
                        Stream.Write(Data, 0, Data.Length);
                    }

                    Console.WriteLine("Successfully generated a GCI file with the NES rom in it!\nFile Location: " + OutputFile);
                    Console.ReadLine();
                }
                else
                {
                    Console.WriteLine("The region code you entered couldn't be found!\r\nThe supported codes are: J, E, P, and U!");
                    Console.ReadLine();
                }
            }
            else
            {
                Console.WriteLine("The NES name must be four or more characters long!");
                Console.ReadLine();
            }
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

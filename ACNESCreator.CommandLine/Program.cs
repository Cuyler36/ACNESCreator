using ACNESCreator.Core;
using System;
using System.IO;

namespace ACNESCreator.CommandLine
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
                NES NESFile = new NES(args[0], File.ReadAllBytes(args[1]), Region.NorthAmerica);

                string OutputFile = Path.GetDirectoryName(args[1]) + "\\" + args[0] + "_InjectedData.gci";
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

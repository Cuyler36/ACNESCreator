using ACNESCreator.Core;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace ACNESCreator.CommandLine
{
    class Program
    {
        readonly static string[] RegionCodes = new string[4] { "J", "E", "P", "U" };

        static void Main(string[] args)
        {
            // Enable Shift-JIS support.
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            string inputName = null;
            string outputName = null;
            string romName = null;
            string tagsFileName = null;
            string region = null;
            var ePlus = false;
            var canSave = true;
            var forceCompress = false;

            if (args == null || args.Length < 1)
            {
                Console.WriteLine("usage: ACNESCreator.CommandLine.exe nesRomFile outputFile");
            }
            else
            {
                for (var i = 0; i < args.Length; i++)
                {
                    if (i + 1 < args.Length)
                    {
                        switch (args[i].ToLower())
                        {
                            case "-i":
                            case "-input":
                                i++;
                                inputName = args[i];
                                break;

                            case "-o":
                            case "-output":
                                i++;
                                outputName = args[i];
                                break;

                            case "-n":
                            case "-name":
                                i++;
                                romName = args[i];
                                break;

                            case "-nosave":
                                canSave = false;
                                break;

                            case "-c":
                            case "-compress":
                                forceCompress = true;
                                break;

                            case "-e":
                            case "-eplus":
                                ePlus = true;
                                region = "J";
                                break;

                            case "-t":
                            case "-tags":
                                i++;
                                tagsFileName = args[i];
                                break;

                            case "-r":
                            case "-region":
                                i++;
                                if (!ePlus) region = args[i];
                                break;

                            default:
                                if (inputName == null)
                                {
                                    inputName = args[i + 1];
                                }
                                else if (outputName == null)
                                {
                                    outputName = args[i + 1];
                                }
                                break;
                        }
                    }
                }

                if (inputName == null || !File.Exists(inputName))
                {
                    Console.WriteLine("The input file doesn't exist!");
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(outputName))
                    {
                        Console.WriteLine("An output file name must be supplied!");
                    }
                    else
                    {
                        if (tagsFileName != null && !File.Exists(tagsFileName))
                        {
                            Console.WriteLine("The tags file doesn't exist! Default tags will be generated.");
                            tagsFileName = null;
                        }

                        if (romName != null && romName.Trim().Length == 0)
                        {
                            Console.WriteLine("The name supplied is invalid. Default name will be used.");
                            romName = "Custom NES Game";
                        }

                        if (region != null && !RegionCodes.Contains(region.ToUpper()))
                        {
                            Console.WriteLine("The region code supplied is invalid. Region code E (NTSC-U, Animal Crossing) will be used.");
                            region = "E";
                        }

                        // Generate the file with the supplied arguments.
                        try
                        {
                            Stream tagsStream = null;
                            if (tagsFileName != null)
                            {
                                tagsStream = File.OpenRead(tagsFileName);
                            }

                            using (tagsStream)
                            {
                                NES nesFile = new NES(romName, File.ReadAllBytes(inputName), forceCompress,
                                    (Region)Array.IndexOf(RegionCodes, region), canSave, ePlus, null,
                                    tagsStream);

                                var outputFile = Path.Combine(Path.GetDirectoryName(inputName),
                                    $"{Path.GetFileNameWithoutExtension(outputName)}.gci");

                                using (var stream = new FileStream(outputFile, FileMode.OpenOrCreate))
                                {
                                    var data = nesFile.GenerateGCIFile();
                                    PrintChecksum(data);
                                    stream.Write(data, 0, data.Length);
                                }

                                Console.WriteLine("Successfully generated a GCI file with the NES rom in it!");
                                Console.WriteLine($"File Location: {outputFile}");
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("An error occurred during file generation.");
                            Console.WriteLine(e.Message);
                            Console.WriteLine(e.StackTrace);
                        }
                    }
                }
            }

            Console.WriteLine("Press any key to close the window...");
            Console.ReadKey();
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

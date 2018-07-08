using System;
using System.Collections.Generic;
using System.Text;

namespace ACNESCreator.Core
{
    public enum Region
    {
        Japan = 0,
        NorthAmerica = 1,
        Europe = 2,
        Australia = 3
    }

    public class NES
    {
        public const int MaxROMSize = 0xFFFF0;
        const uint PatchAdjustOffset = 0x7F800000;

        const byte DefaultFlags1 = 0xEA;
        const byte DefaultFlags2 = 0;
        static readonly byte[] DefaultTagData = Encoding.ASCII.GetBytes("TAG\0" + "GNO\0x1F" + "END\0");
        static readonly string[] RegionCodes = new string[4] { "J", "E", "P", "U" };
        static readonly string[] TagList = new string[]
        {
            "END", "VEQ", "VNE", "GID", "GNM", "CPN", "OFS", "HSC",
            "GNO", "BBR", "QDS", "SPE", "ISZ", "IFM", "REM", "TCS",
            "ICS", "ESZ", "FIL", "ROM", "MOV", "NHD", "DIF", "PAT"
        };

        public class ACNESHeader
        {
            public byte Checksum = 0; // May not be the checksum byte.
            public byte Unknown = 0; // May not be used.
            public string Name;
            public ushort DataSize;
            public ushort TagsSize;
            public ushort IconFormat;
            public ushort IconFlags = 0; // IconFlags. Unsure what they do, but they're set in "SetupResIcon". Maybe they're not important? It's also passed as an argument to memcard_data_save, but appears to go unused.
            public ushort BannerSize;
            public byte Flags1;
            public byte Flags2;
            public ushort Padding; // 0 Padding? Doesn't appear to be used.

            public byte[] GetData(Region GameRegion)
            {
                byte[] Data = new byte[0x20];

                Data[0] = Checksum;
                Data[1] = Unknown;
                if (GameRegion == Region.Japan) // Japan games have item strings that are 10 characters long, compared to the 16 characters in non-Japanese games.
                {
                    Utility.GetPaddedStringData(Name, 0xA, 0x20).CopyTo(Data, 2);
                    BitConverter.GetBytes(DataSize.Reverse()).CopyTo(Data, 0xC);
                }
                else
                {
                    Utility.GetPaddedStringData(Name, 0x10, 0x20).CopyTo(Data, 2);
                    BitConverter.GetBytes(DataSize.Reverse()).CopyTo(Data, 0x12);
                }
                BitConverter.GetBytes(TagsSize.Reverse()).CopyTo(Data, 0x14);
                BitConverter.GetBytes(IconFormat.Reverse()).CopyTo(Data, 0x16);
                BitConverter.GetBytes(IconFlags.Reverse()).CopyTo(Data, 0x18);
                BitConverter.GetBytes(BannerSize.Reverse()).CopyTo(Data, 0x1A);
                Data[0x1C] = Flags1;
                Data[0x1D] = Flags2;
                BitConverter.GetBytes(Padding.Reverse()).CopyTo(Data, 0x1E);

                return Data;
            }
        }

        public class Banner
        {
            public string Title;
            public string Comment;
            public byte[] BannerData = new byte[0];
            public byte[] IconData = new byte[0];

            public byte[] GetData()
            {
                List<byte> Data = new List<byte>();
                if (!string.IsNullOrEmpty(Title) || !string.IsNullOrEmpty(Comment))
                {
                    Data.AddRange(Utility.GetPaddedStringData(Title, 0x20));
                    Data.AddRange(Utility.GetPaddedStringData(Comment, 0x20));
                }

                Data.AddRange(BannerData);
                Data.AddRange(IconData);

                return Data.ToArray();
            }

            public ushort GetSize()
                => (ushort)(((!string.IsNullOrEmpty(Title) || !string.IsNullOrEmpty(Comment)) ? 0x40 : 0) + BannerData.Length + IconData.Length);
        }

        public readonly ACNESHeader Header;
        public byte[] TagData { get; internal set; }
        public readonly Banner BannerData;
        public readonly byte[] ROM;

        public readonly Region GameRegion;
        public readonly bool IsDnMe;
        public readonly byte[] SaveIconData;

        public NES(string ROMName, byte[] ROMData, Region ACRegion, bool Compress, bool IsGameDnMe, byte[] IconData = null)
        {
            // Set the icon
            SaveIconData = IconData ?? GCI.DefaultIconData;

            // Is game Doubutsu no Mori e+?
            IsDnMe = IsGameDnMe;

            if (true || IsNESImage(ROMData))
            {
                // If Data is Yaz0 compressed, uncompress it first
                if (Yaz0.IsYaz0(ROMData))
                {
                    ROMData = Yaz0.Decompress(ROMData);
                    Compress = true;
                }

                if (ROMName == null || ROMName.Length < 4 || ROMName.Length > 0x10)
                {
                    throw new ArgumentException("ROMName cannot be less than 4 characters or longer than 16 characters.");
                }

                // Compress the ROM if compression is requested
                if (Compress)
                {
                    ROMData = Yaz0.Compress(ROMData);
                }

                if (!Compress && ROMData.Length > MaxROMSize)
                {
                    throw new ArgumentException(string.Format("This ROM cannot be used, as it is larger than the max ROM size.\r\nThe max ROM size is 0x{0} ({1}) bytes long!",
                        MaxROMSize.ToString("X"), MaxROMSize.ToString("N0")));
                }
            }

            TagData = Utility.GetPaddedData(DefaultTagData, (DefaultTagData.Length + 0xF) & ~0xF);

            BannerData = new Banner
            {
                Title = IsDnMe ? "Animal Forest e+" : "Animal Crossing",
                Comment = ROMName + "\n" + "NES Save Data"
            };

            Header = new ACNESHeader
            {
                Name = ROMName,
                DataSize = (ushort)((ROMData.Length + 0xF) >> 4), // If the ROM is compressed, the size is retrieved from the Yaz0 header.
                TagsSize = (ushort)((TagData.Length + 0xF) & ~0xF),
                IconFormat = (ushort)IconFormats.Shared_CI8,
                IconFlags = 0,
                BannerSize = (ushort)((BannerData.GetSize() + 0xF) & ~0xF),
                Flags1 = DefaultFlags1,
                Flags2 = DefaultFlags2,
                Padding = 0
            };

            ROM = ROMData;
            GameRegion = ACRegion;

            // Generate custom tag data if possible
            GenerateDefaultTagData(ROMName, IsNESImage(ROMData));
        }

        public NES(string ROMName, byte[] ROMData, bool CanSave, Region ACRegion, bool Compress, bool IsDnMe, byte[] IconData = null)
            : this(ROMName, ROMData, ACRegion, Compress, IsDnMe, IconData)
        {
            if (!CanSave)
            {
                Header.Flags1 &= 1; // Only save the lowest bit, as everything else is related to saving.
                Header.IconFormat = 0;
                Header.BannerSize = 0;
            }
        }

        internal bool IsNESImage(byte[] Data)
            => Encoding.ASCII.GetString(Data, 0, 3) == "NES";

        internal void SetChecksum(ref byte[] Data)
        {
            byte Checksum = 0;
            for (int i = 0x40; i < Data.Length; i++) // Skip the header by adding 0x40
            {
                Checksum += Data[i];
            }

            Data[0x680] = (byte)-Checksum;
        }

        internal void GenerateDefaultTagData(string GameName, bool NESImage)
        {
            if (GameName.Length > 1)
            {
                GameName = GameName.Trim().ToUpper();
                List<KeyValuePair<string, byte[]>> Tags = new List<KeyValuePair<string, byte[]>>
                {
                    new KeyValuePair<string, byte[]>("GID", Encoding.ASCII.GetBytes(GameName.Substring(0, 1) + GameName.Substring(GameName.Length - 1, 1))),
                    new KeyValuePair<string, byte[]>("GNM", Encoding.ASCII.GetBytes(GameName)),
                    new KeyValuePair<string, byte[]>("GNO", new byte[1] { 0x1F }),
                };

                // Patch ROM if not NES Image
                if (!NESImage)
                {
                    AddPatchData(ref Tags, 0x80003970, Patch.PatcherData);
                    AddPatchData(ref Tags, 0x806D4B9C, Patch.PatcherEntryPointData);
                }
                
                Tags.Add(new KeyValuePair<string, byte[]>("END", new byte[0]));

                GenerateTagData(Tags);
            }
        }

        internal void AddPatchData(ref List<KeyValuePair<string, byte[]>> Tags, uint WriteStartOffset, byte[] PatchData)
        {
            uint WriteAddress = WriteStartOffset;
            uint DataOffset = 0;

            while (WriteAddress < 0x807FFFFF && DataOffset < PatchData.Length)
            {
                List<byte> PatchDataList = new List<byte>();
                uint WriteAmount = (uint)(PatchData.Length - DataOffset);
                if (WriteAmount > 0xFB)
                {
                    WriteAmount = 0xFB;
                }

                PatchDataList.Add((byte)((WriteAddress - PatchAdjustOffset) >> 16));
                PatchDataList.Add((byte)WriteAmount);
                PatchDataList.Add((byte)((WriteAddress & 0xFF00) >> 8));
                PatchDataList.Add((byte)(WriteAddress & 0xFF));

                for (uint i = DataOffset; i < DataOffset + WriteAmount; i++)
                {
                    PatchDataList.Add(PatchData[DataOffset + i]);
                }

                DataOffset += WriteAmount;
                WriteAddress += WriteAmount;

                Tags.Add(new KeyValuePair<string, byte[]>("PAT", PatchDataList.ToArray()));
            }
        }

        public bool IsNESImage()
            => IsNESImage(ROM);

        public void GenerateTagData(List<KeyValuePair<string, byte[]>> Tags)
        {
            bool HasEND = false;
            List<byte> TagRawData = new List<byte>();

            // The first tag is printed and then ignored, so we can put whatever we want here
            TagRawData.AddRange(Encoding.ASCII.GetBytes("TAG\0"));

            foreach (var TagInfo in Tags)
            {
                // Ensure the tag is capitalized
                string Tag = TagInfo.Key.ToUpper();

                // Confirm the tag is valid
                if (Array.IndexOf(TagList, Tag) > -1)
                {
                    // Add the tag
                    TagRawData.AddRange(Encoding.ASCII.GetBytes(Tag));

                    // Add the tag data length
                    TagRawData.Add((byte)(TagInfo.Value.Length & 0xFF));

                    if (Tag == "END")
                    {
                        HasEND = true;
                    }

                    // Add the tag data
                    TagRawData.AddRange(TagInfo.Value);
                }
            }

            // Ensure that an END tag is present so the parser will stop
            if (!HasEND)
            {
                TagRawData.AddRange(Encoding.ASCII.GetBytes("END\0"));
            }

            byte[] TagBinaryData = TagRawData.ToArray();
            TagBinaryData = Utility.GetPaddedData(TagBinaryData, (TagBinaryData.Length + 0xF) & ~0xF);

            Header.TagsSize = (ushort)TagBinaryData.Length;
            TagData = TagBinaryData;
        }

        public byte[] GenerateGCIFile()
        {
            List<byte> Data = new List<byte>();
            Data.AddRange(Header.GetData(GameRegion));
            Data.AddRange(TagData);
            if ((Header.Flags1 & 0x80) == 0x80) // Only add banner if saving is enabled
            {
                Data.AddRange(BannerData.GetData());
            }
            Data.AddRange(ROM);

            var BlankGCIFile = new GCI(SaveIconData)
            {
                Data = Data.ToArray(),
                Comment1 = IsDnMe ? "Animal Forest e+" : "Animal Crossing",
                Comment2 = "NES Game:\n" + Header.Name,
            };

            BlankGCIFile.Header.FileName = (IsDnMe ? "DobutsunomoriE_F_" : "DobutsunomoriP_F_") + Header.Name.Substring(0, 4).ToUpper();
            BlankGCIFile.Header.GameCode = (IsDnMe ? "GAE" : "GAF") + RegionCodes[(int)GameRegion];

            byte[] GCIData = BlankGCIFile.GetData();
            SetChecksum(ref GCIData);

            return GCIData;
        }
    }
}

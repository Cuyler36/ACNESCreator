using System.Collections.Generic;

namespace ACNESCreator.FrontEnd.Tags
{
    public static class TagInfo
    {
        public struct TagDescription
        {
            public string Tag;
            public string Name;
            public string Description;

            public TagDescription(string tag, string name, string description)
            {
                Tag = tag;
                Name = name;
                Description = description;
            }
        }

        public static readonly List<TagDescription> Descriptions = new List<TagDescription>
        {
            new TagDescription("END", "End of Tags", "Causes the tag parser to terminate immediately. No further tags are processed."),
            new TagDescription("VEQ", "Variable Equal", "If the boolean argument is not set to true, the parser will skip the next tag."),
            new TagDescription("VNE", "Variable Not Equal", "If the boolean argument is set to true, the parser will skip the next tag."),
            new TagDescription("GID", "Game ID", "A string that represents the game. Can be anything. Used for generating a save name."),
            new TagDescription("GNM", "Game Name", "A string which is the title of the game. Can be anything."),
            new TagDescription("CPN", "Copy Name", "A string which is the title of the save game. Can be anything."),
            new TagDescription("OFS", "Offset", "Sets the offset into the save storage area in RAM. Same for both BBR and QDS."),
            new TagDescription("HSC", "High Score", "Sets the high score for a specific area in the game's save file."),
            new TagDescription("GNO", "Game Number", "Sets the game number. Used for internal games mainly."),
            new TagDescription("BBR", "Battery Backed RAM", "Marks a portion of the Battery Backup RAM area for saving."),
            new TagDescription("QDS", "Quick Disk Save", "Marks a protion of the Famicom Disk System save area for saving."),
            new TagDescription("SPE", "Special", "Calls a special subroutine for use on The Legend of Zelda. Modifies some of the game & calculates a checksum."),
            new TagDescription("TCS", "Tag Checksum", "Calculates the checksum for the tags. If the checksum + additive argument isn't equal to zero, the tags are considered invalid & the parser stops execution immediately."),
            new TagDescription("ICS", "Image Checksum", "Calculates the checksum for the ROM image. If the checksum + additive argument isn't equal to zero, the ROM image is considered corrupted."),
            new TagDescription("ESZ", "Expanded Size", "Sets the emulator's ROM expanded size variable."),
            new TagDescription("ROM", "Load ROM", "Loads another ROM from the internal ROMs. The current tags are used as the tags for that ROM."),
            new TagDescription("MOV", "Move Data", "Moves a section of data in the ROM from one location to another."),
            new TagDescription("NHD", "New Header Data", "Copies new header data to the ROM's header."),
            new TagDescription("DIF", "Difference", "Unused command. Acts like END and causes the parser to terminate execution immediately."),
            new TagDescription("PAT", "Patch Memory", "Allows a memory patch to be created. Writes data to an offset in RAM."),
            // TODO: Do we want to include completely unused tags like FIL, ISZ, IFM, and REM?
        };
    }
}

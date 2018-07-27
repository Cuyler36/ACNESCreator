# ACNESCreator
ACNESCreator allows you to create GameCube GCI save files with your favorite NES ROMs included in them to be played on Animal Crossing.

## Supported Titles
* Dōbutsu no Mori+ (どうぶつの森+)
* Animal Crossing
  * NTSC
  * PAL
  * Australia
* Dōbutsu no Mori e+ (どうぶつの森e+)

## Features
* Create save files with NES ROMs to play on the game-less NES in Animal Crossing
* Create memory patches & code to be injected

## Creating a NES ROM Save File
To create a NES ROM save file, open the program, give your ROM a name, browse for the NES ROM, and select your region.
You can change the save file icon by right clicking on the current one and clicking "Import". 32x32 PNG is the supported format.
Select any other options you wish to change, then click "Generate GCI File" to generate your save file.
Once the save file has been created, import the save file to Dolphin, or use GCMM to get it on your physical memory card.

## Creating a Memory Patch
By taking advantage of Animal Crossing's NES emulator's "Tag" settings, we can overwrite data anywhere in RAM with our own.
First, you'll need to create a blank file.
Then, you'll need to follow this structure format in that file:
```c
struct AnimalCrossingNESPatchHeader {
	uint16_t GlobalFlags; // Global Loader Flags. Currently, setting the last flag will enable the JUTReportConsole without zurumode. [JUTConsoleEnabled = GlobalFlags & 1]
	uint16_t PatchCount; // Number of patches to copy.
};

struct AnimalCrossingNESPatch
{
  uint32_t PatchAddress; // The location in memory to write data to.
  uint32_t PatchSize; // Size in bytes of patch data to copy to RAM.
  uint32_t PatchFlags; // Only the last flag used to mark the code as exectuable currently. [Executable = PatchFlags & 1]
  uint8_t Data[]; // The data to copy to RAM.
} Patches[];
```
You can find the source code of the loader [here](https://github.com/jamchamb/ac-patch-loader).

After you've created your file, you should follow the same process as creating a NES ROM.
The program will automatically detect that the file is a patch file and notify you of so.

#### Special thanks to James Chambers for discovering the NES Memory Card loading functionality & the PAT tag.

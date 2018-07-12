// Animal Crosing ACE Loader
// Created by:
//	Cuyler (Jeremy Olsen) & pyrocow (James Chambers)
//
// This loader takes advantage of the Memory Card NES Loader in Animal Crossing to patch itself to 0x8003640.
// It then reads the NES ROM as patch data.
// Patch structure:
// struct ACNESPatch{
//	 uint_32t PatchAddress; // the address to begin writing the patch to
//   uint_32t PatchSize; // the total size in bytes to write
//   uint_32t IsExecutable; // if set to 0, the patch will be treated as data. if set to anything else, it will be treated as executable, and the loader will
//        jump to the PatchAddress when done. r0 will contain the calling function of the loader's return address in case you want to continue execution of the game.
// }
.text
// allocate stack frame
stwu r1, -0x30(r1)

// save LR through r0
mflr r0

// store r0/r3/r4/r5/r6 registers
stw r0, 0x20(r1)
stw r3, 0x1C(r1)
stw r4, 0x18(r1)
stw r5, 0x14(r1)
stw r6, 0x10(r1)

// loader (loads from ROM Data)
lis r3, NES_ROM_DATA_PTR_ADDRESS@h
addi r3, r3, NES_ROM_DATA_PTR_ADDRESS@l
lwz r3, 0(r3)

// check if the ROM start address is nullptr
cmplwi r3, 0
beq exit

// load patch offset
lwz r4, 0(r3)
cmplwi r4, 0
beq exit
stw r4, 0x28(r1) // save jump offset
lwz r6, 0x08(r3) // the third int should be "bool isExecutable". If anything other than 0, the loader will jump to the address it 
stw r6, 0x24(r1)// save executable flag
lwz r6, 0x04(r3) // the second int should be the size to copy (ROM size - 8)
stw r6, 0x2C(r1) // save size for invalidation operation later
addi r5, r3, 0xC

// start patching
patchLoop:
cmpwi r6, 0
ble exitPatchLoop
lbz r3, 0(r5)
stb r3, 0(r4)
addi r4, r4, 1
addi r5, r5, 1
addi r6, r6, -1
b patchLoop

exitPatchLoop:
// invalidate instruction and data caches
lwz r4, 0x2C(r1) // load size
lwz r3, 0x28(r1) // load address
clrlwi. r0, r4, 27
beq align
addi r4, r4, 0x20

align:
addi r4, r4, 0x1F
srwi r4, r4, 5
mtctr r4

invalidationLoop:
icbi r0, r3
dcbi r0, r3
addi r3, r3, 0x20
bdnz invalidationLoop

// sync invalidaitons
flushCache:
sync
isync

// restore register for arguments to my_zelda_free
lwz r3, 0x1C(r1)

// restore the original pointer to my_zelda_free and branch to it
lis r5, MY_ZELDA_FREE@h
ori r5, r5, MY_ZELDA_FREE@l
lis r6, MY_FREE_PTR@h
ori r6, r6, MY_FREE_PTR@l
stw r5, 0x0(r6)
mtctr r5
bctrl

// check if the patch is executable
lwz r4, 0x24(r1)
cmplwi r4, 0
beq restoreLR
// restore offset and jump
lwz r4, 0x28(r1)
lwz r0, 0x20(r1) // set previous function LR in r0
mtlr r4
b cleanup

restoreLR:
// restore LR
lwz r0, 0x20(r1)
mtlr r0

cleanup:
// restore rest of registers and clear stack frame
lwz r4, 0x18(r1)
lwz r5, 0x14(r1)
lwz r6, 0x10(r1)
addi r1, r1, 0x30
blr

.data
MY_ZELDA_FREE = 0x8062D4CC;
MY_FREE_PTR = 0x806D4B9C;
NES_ROM_DATA_PTR_ADDRESS = 0x801F6C64;
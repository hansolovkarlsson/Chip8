// chip8.cpp

// Chip8 CPU emulator
// main will link in the c8_execute1
// actually, in this case, I should be able to do a class

// use stdio for console log and debug interactive interface
// wish I could uses iostream but it's not installed!?
#include <stdio.h>      /* printf, scanf, puts, NULL */
#include <stdlib.h>     /* srand, rand */
#include <time.h>       /* time */
#include "Screen.h"
#include "Sound.cpp"

// 1. print sprite operator
//		put pixels in global screen buffer
// 2. clear screen operator (clear buffer)
// any screen handling is done outside of Chip8, keep things separated
// except the screen buffer

// registers, memory, fontset
// V0-VF: 16 8-bit registers, VF is also flag for carry, borrow and collision
// I: address register, 16b, but only 12 used
// PC: program counter
// Memory: 12 bit memory space
//		0x000-0x1FF: font data as sprites (4 bits x 5 bytes, only high nibble)
//		0x200-0xE9F: program
//		0xEA0-0xEFF: reserved for stack and internal use (not used in emulator)
//		0xF00-0xFFF: reserved for display buffer (not used in emulator)
// SP: stack pointer
// ST: stack memory, separated from mem

typedef unsigned char byte;
typedef unsigned short int word;
typedef unsigned int double_word;


bool QUIRK_SH1VAR = true;		// true: V[x]=shift(V[x])+carry, false: V[x]=shift(V[y]) (org)
bool QUIRK_KEEPIX = true;		// true: IX unchanged after STO/RCL (schip), false: changes
bool QUIRK_SPR16 = true;		// true: DRAW (x,y)#0=> 16 byte sprite, false: #0=no draw


byte V[16];
word IX;
word PC;
word SP;
byte DT; // delay timer
byte ST; // sound time
byte KEY;// current key


const int STACK_SIZE = 16;
word stack[STACK_SIZE];

const int MEM_SIZE		= 0x10000; // 64kB memory
const int PROG_START	= 0x0200;
const int PROG_END		= 0x1000;	// using the "reseved" space as well
const int PROG_MAX_SIZE	= PROG_END - PROG_START;
byte* mem;
int prog_size;

// According to one documentation, font is stored in 0x8110
// I'm putting it in x1000, above code area
const int FONT_START = 0x1000;

// EXPANSION
// add all characters, in <200
// Also test moving fonts to >0x1000 area for large fonts

// I could eventually expand VF for more flags
// setting to use expanded flag register (ZSV) collision can still be carry (b0)
// or a separate flag register with separate handling, branch instructions by flags

// Also, some mechanics to set I, hi nib, for high addressing

// Make sure more operations included: NOT, NEG, ROT
// maybe add data stack
// expand return stack

// Also, I want to rename "I" to "X"
// instead of Vx, I'd like Rx
// Instructions to store single register to mem by X, and recall as well

// more advanced code matching would be to use regexp or something
// string, 0-9, A-F exact matches, while n,x,y would be fill-ins

// Font, extended:
// 0-9 (10)
// A-Z (26)
// a-z (26)
// ~`!@#$%^&* ()-_=+[{]} \|;:'",<.> /? (32)
// 10+26+26+32 = 94
// 94*5 = 470 (0x1D6)
// 0x1E0-0x1FF (32 bytes): free for game variables

// A WAITMR would be useful, wait for timer to reach 0, instead of:
// LOOP:
// SET r0, TIMER
// SKEQ r0, #00
// JMP LOOP
// Lot of code for something that's quite common

// That's something I probably should do, go through existing code, and optimize with new ops


void chip_init() {
	mem = new byte[MEM_SIZE];

	byte fontset[] = {
		0xF0, 0x90, 0x90, 0x90, 0xF0,		// 0
		0x20, 0x60, 0x20, 0x20, 0x70,		// 1
		0xF0, 0x10, 0xF0, 0x80, 0xF0,		// 2
		0xF0, 0x10, 0xF0, 0x10, 0xF0,		// 3
		0x90, 0x90, 0xF0, 0x10, 0x10,		// 4
		0xF0, 0x80, 0xF0, 0x10, 0xF0,		// 5
		0xF0, 0x80, 0xF0, 0x90, 0xF0,		// 6
		0xF0, 0x10, 0x20, 0x40, 0x40,		// 7
		0xF0, 0x90, 0xF0, 0x90, 0xF0,		// 8
		0xF0, 0x90, 0xF0, 0x10, 0xF0,		// 9
		0xF0, 0x90, 0xF0, 0x90, 0x90,		// A
		0xE0, 0x90, 0xE0, 0x90, 0xE0,		// B
		0xF0, 0x80, 0x80, 0x80, 0xF0,		// C
		0xE0, 0x90, 0x90, 0x90, 0xE0,		// D
		0xF0, 0x80, 0xF0, 0x80, 0xF0,		// E
		0xF0, 0x80, 0xF0, 0x80, 0x80		// F
	};
	for( int i=0; i<sizeof(fontset); i++)
		mem[FONT_START+i] = fontset[i];

	for( int i=0; i<16; i++)
		V[i] = 0;

	IX=0;
	PC=0x200;
	SP=0;		// stack grow up, SP basically is the size, so when it's 16, it's full, 0 empty

	srand (time(NULL));

	sound_init();

}

// op codes enum
// High nibble
enum OP_GRP {
	SYS_OP	= 0x0,		// 0ppp		system calls, 00E0, 00EE
	JMP_N	= 0x1,		// 1nnn		jump to address nnn
	CALL_N	= 0x2,		// 2nnn		call subroutine nnn
	SKEQ_VN	= 0x3,		// 3xnn		skip next instruction if Vx==nn
	SKNE_VN	= 0x4,		// 4xnn		skip next instruction if Vx<>nn
	SKEQ_VV	= 0x5,		// 5xy0		skip next instruction if Vx==Vy
						// what could the last nib be used for? maybe compare, set new flags?
						// oh, could do branch, word size, so 0=2B, F=32B
						// 1: skip if Vx>Vy
						// 2: Sto Vx..Vy to memory at IX, IX not changed, x-y can be asc or desc
						// 3: Rcl Vx..Vy from memory at IX, IX not changed
						// I like my version better, number of instructions
	SET_VN	= 0x6,		// 6xnn		set Vx=nn
	ADD_VN	= 0x7,		// 7xnn		add nn to Vx, no carry
	ALU_OP	= 0x8,		// 8xyp		ALU operations, 0-7, E
	SKNE_VV	= 0x9,		// 9xy0		skip next instruction if Vx<>Vy
						// 1: VF,VX = VX*VY
						// 2: VX,VF = VX/VY
						// 3: Convert VX,VY to 16 bit BCD, I+0..4, IX not change
	SET_IN	= 0xA,		// Annn		set I = nnn
	JMP_V0N	= 0xB,		// Bnnn		jump to address nnn+V0
						// Bxnn		CHIP48: jump to address Vx+nn
	RND_VN	= 0xC,		// Cxnn		assign random number, Vx=rand & nn
	DRAW_VVN= 0xD,		// Dxyn		draw sprite at coordinate (Vx,Vy)
						//			width 8 pixels, height n pixels
						//			VF set if collision
						// SCHP: n=0, draw 16x16 sprite
	KEY_OP	= 0xE,		// Eppp		Key operations, EX9E, EXA1
	SPEC_OP	= 0xF,		// Fppp		Special operations, 07 0A 15 18 1E 29 33 55 65
};

// 01pp-0Fpp free

// don't like this instruction
// rather move it to 0xFxpp where Vx is number of pixels or something
// plus it's a waste of a whole code group
// I want to move these to Fx..
byte OP_SCHP_SCRU = 0xB;	// 00Bn		SCHP scroll up n pixels
byte OP_SCHP_SCRD = 0xC;	// 00Cn		SCHP scroll down n pixels
byte OP_XOCHIP_SCRU = 0xD;	// 00Dn		XOChip scroll up n pixels


enum OP_SYS { // 0x00pp
	// Original
	SCRON	= 0x4B,		// original: turn screen on (reset)
	CLS		= 0xE0,
	RET		= 0xEE,

	// CHIP8 CPU
	NOP		= 0x00,		// CHIP8CPU

	// Super-Chip
	SCRR	= 0xFB,		// scroll right 4 pixels (hires), 2 pix (lores)
	SCRL	= 0xFC,		// scroll left 4 pixels (hires), 2 pix (lores
	RST		= 0xFD,		// Exit to Hex monitor, actually, it's RST (reset, restart)
	LORES	= 0xFE,		// 64x32
	HIRES	= 0xFF,		// 128x64

	// add scroll up/down, also other resolutions, mid:64x64
	// F0-FA, 64x64, scroll up, down, 1 pix, 4 pix, left right 1 pix
	// scrolling: width 64, scroll 2 pix, width 128 scroll 4 pix
	// height 32 scroll 1 pix, height 64, 2 pix
	// Add NOT, NEG, SGN, TEST
	// Also want CMP, Flag ops, Branch of flags, Block
	// 0x01pp - 0x0Fpp are totally open and unused
	// add instruction to switch between the emulator modes too
};

enum OP_ALU { // 0x8xyP, P=operation
	CP		= 0x0,		// 8xy0		Vx=Vy
	OR		= 0x1,		// 8xy1		Vx=Vx | Vy
	AND		= 0x2,		// 8xy2		Vx=Vx & Vy
	XOR		= 0x3,		// 8xy3		Vx=Vx ^ Vy
	ADD		= 0x4,		// 8xy4		Vx=Vx + Vy (carry)
	SUB		= 0x5,		// 8xy5		Vx=Vx - Vy (borrow)
	SHR		= 0x6,		// 8xy6		CHIP8:	Vx=Vy>>1
						//			SCHIP:	Vx=Vx>>1 (carry)
	RSUB	= 0x7,		// 8xy7		Vx=Vy - Vx (borrow)
	SHL		= 0xE,		// 8xyE		C8: Vx=Vy<<1
						//			SC: Vx=Vx<<1 (carry)
	// 0x8-0xD, 0xF
};
// for the shift instructions, if y=0, shift x, if y>0, shift y
// that way it's automatic

// In key group, I'll add more of my functions, like pixel draw etc, and color
enum OP_KEY { // 0xExpp
	SKEQ_KV	= 0x9E,		// Ex9E		Skip if Key = Vx
	SKNE_KV = 0xA1,		// ExA1		Skip if Key != Vx
};

enum OP_SPEC { // 0xFxp
	// Original Chip-8
	GET_VT	= 0x07,		// Fx07		Vx = Timer
	WAIT_VK	= 0x0A,		// Fx0A		Wait for key, Vx=Key
	SET_TV	= 0x15,		// Fx15		Timer = Vx
	SET_SV	= 0x18,		// Fx18		Sound = Vx (length)
	ADD_IV	= 0x1E,		// Fx1E		I = I + Vx (no carry, except Amiga version)
	GET_IF	= 0x29,		// Fx29		I = Font_Addr + Vx*5 (by digit, 0x0-0xF)
	BCD_IV	= 0x33,		// Fx33		Store BCD of Vx to memory I+0..2
	STO_IV	= 0x55,		// Fx55		Store V0-Vx in memory I+0..x
	RCL_IV	= 0x65,		// Fx65		Recall V0-Vx from memory I+0..x
						// 			Chip8, Chip48: I ends up changed
						//			SCHP: I unchanged

	SUB_IV	= 0x1F,		// Fx1F		my addition: IX=IX-Vx

	// CHIP-8 CPU
	STOP_V	= 0x00,		// F000		Exit to monitor
						//			My addition: Fx00, Vx=return code and halt CPU
	SET_PV	= 0x17,		// Fx17		Pitch = Vx
	ASC_IF	= 0x2A,		// Fx2A		I = ASCII_Addr + Vx*? (by ascii code, 0x20-)
						//			0x20-07E, 4x5 pixels
	OUT_RSV	= 0x70,		// Fx70		Output RS486 = Vx
	IN_VRS	= 0x71,		// Fx71		Input Vx = RS485
	SET_BV	= 0x72,		// Fx72		Set Baud = Vx


	// Super-Chip
	BIG_IF	= 0x30,		// Fx30		Large font, 8 pix * 10 bytes, I=Font_Big_Addr+Vx*10
						// 			perhaps Fx31 for large ascii font
	SAVE_V	= 0x75,		// Fx75		save registers to persistent storage, Vx..VF (high scores)
	LOAD_V	= 0x85,		// Fx85		load registers from peristent storage, Vx..VF

	// Other extensions
	BRCH_V	= 0xA4,		// FxA4		Jump V[x] instruction steps forward (PC+V[x]*2)
	BRCHB_V = 0xAE,		// FxAE		Jump V[x] instructions backwards (PC-V[x]*2), V[x]=1: infinte loop

	// XO Chip
	SET_PV2	= 0x3A,		//			Set Pitch = 4000*2^((Vx-64)/48) Hertz

	// COSMAC ELF
	// EXT_JMP = 0xFF	// FFFF	NNNN	Two word instruction, jump to NNNN

};

// For debugger:
// basically store every address (nnn) made by JMP, JMPV0, CALL, SETIX
// when trace prints out, print a "@" to mark it
const int entry_max = 128;
word entry[entry_max]; // allow up to 16 entry points recorded
int entry_cnt = 0;
int exit_code = 0;

void add_entry(word addr)
{
	bool exist = false;
	for(int i=0; i<entry_max; i++)
		if(entry[i] == addr) {
			exist = true;
			break;
		}
	if(!exist)
		entry[entry_cnt++] = addr;
}

// load binary file
// return true if success
// false if fail
bool chip_load_file(const char* filename)
{
	bool ret = false;

	FILE* file = fopen(filename, "rb");
	if (file == NULL) {
		printf("File not found [%s]\n", filename);
		ret = false;
	} else {
		fseek(file, 0, SEEK_END);
		prog_size = ftell(file);
		printf("ROM size: %d\n", prog_size);
		rewind(file);

		if (prog_size > PROG_MAX_SIZE) {
			printf("File is too large [%s]\n", filename);
			ret = false;
		} else {
			// read program into memory
			fread(mem+PROG_START, 1, prog_size, file);
			ret = true;
		}
		fclose(file);
	}
	return ret;
}

void chip_dump_mem()
{
	printf("mem size=%d", prog_size);
	for(int i=0; i<prog_size; i++)	{
		int addr = PROG_START + i;
		if(i%16==0)
			printf("\n%04X: \t", addr);
		printf("%02X ", mem[addr]);
	}
	printf("\n");
}

void print_stack()
{
	printf("\t[PC:%04X,SP:%02X,stack:(", PC, SP);
	for(int i=0; i<SP; i++)	{
		if(i>0)
			printf(" ");
		printf("%04X", stack[i]);
	}
	printf(")]");
}

bool op_ret()
{
	bool ret = true;
	if(SP==0) {
		printf("Empty stack\n");
		ret = false;
	} else {
		SP--;
		PC=stack[SP];
		print_stack();
	}
	return ret;
}

bool op_jmp(word addr)
{
	bool ret = true;
	add_entry(addr);
	if(addr>=PROG_END) {
		printf("Jump outside of memory PC:%04X\n", addr);
		ret = false;
	} else {
		PC = addr;
		printf("\t[PC:%04X]", PC);
	}
	return ret;
}

bool op_call(word addr)
{
	bool ret = true;
	add_entry(addr);
	if(SP>=STACK_SIZE) {
		printf("Stack full SP:%d\n", SP);
		ret = false;
	} else if (addr>=PROG_END) {
		printf("Call outside of memory PC:%04X\n", addr);
		ret = false;
	} else {
		stack[SP] = PC; // print stack
		SP++;
		PC = addr;
		print_stack();
	}
	return ret;
}

bool op_skip_equal(byte v1, byte v2)
{
	bool ret = true;
	if(v1==v2) {
		PC += 2;
		if(PC>=PROG_END) {
			printf("Skip outside memory PC:%04X\n", PC);
			ret = false;
		} else
			printf("\t[?%02X=%02X,PC:%04X]", v1, v2, PC);
	}
	return ret;
}


bool op_skip_not_equal(byte v1, byte v2)
{
	bool ret = true;
	if(v1!=v2) {
		PC+=2;
		if(PC>=PROG_END) {
			printf("Skip outside memory PC:%04X\n", PC);
			ret = false;
		} else
			printf("\t[?%02X=%02X,PC:%04X]", v1, v2, PC);
	}
	return ret;
}

void op_set_reg(byte reg, byte val)
{
	V[reg] = val;
	printf("\t[r%01X:%02X]", reg, V[reg]);
}

void op_add_reg(byte reg, byte val)
{
	V[15] = 0;
	word sum = (word)V[reg] + (word)val;
	if(sum>0xFF)
		V[15] = 1;
	V[reg] = (byte)(sum & 0xFF);
	printf("\t[r%01X:%02X,rF:%02X]", reg, V[reg], V[15]);
}

void op_or_reg(byte reg, byte val)
{
	V[reg] |= val;
	printf("\t[r%01X:%02X]", reg, V[reg]);
}

void op_and_reg(byte reg, byte val)
{
	V[reg] &= val;
	printf("\t[r%01X:%02X]", reg, V[reg]);
}

void op_xor_reg(byte reg, byte val)
{
	V[reg] ^= val;
	printf("\t[r%01X:%02X]", reg, V[reg]);
}

void op_sub_reg(byte reg, byte val)
{
	// borrow flag
	word diff = 0x100 + (word)V[reg] - (word)val;
	V[15]=1;
	if((diff & 0x100)==0)
		V[15]=0;
	V[reg] = (byte)(diff & 0xFF);
	printf("\t[r%01X:%02X, rF:%02X]", reg, V[reg], V[15]);
}

void op_rsub_reg(byte reg, byte val)
{
	// borrow flag
	word diff = 0x100 + (word)val - (word)V[reg];
	V[15]=1;
	if((diff & 0x100)==0)
		V[15]=0;
	V[reg] = (byte)(diff & 0xFF);
	printf("\t[r%01X:%02X, rF:%02X]", reg, V[reg], V[15]);
}

void op_shr_reg(byte reg, byte val)
{
	if(QUIRK_SH1VAR) {
		V[15]=V[reg]&0x1;
		V[reg] = V[reg]>>1;
	} else
		V[reg] = val>>1;

	printf("\t[r%01X:%02X]", reg, V[reg]);
}

void op_shl_reg(byte reg, byte val)
{
	if(QUIRK_SH1VAR) {
		V[15] = (V[reg]&0x80)==0?0:1;
		V[reg] = V[reg]<<1; // add flag for 2reg
	} else
		V[reg] = val<<1;

	printf("\t[r%01X:%02X]", reg, V[reg]);
}

void op_set_ix(word val)
{
	add_entry(val);
	IX = val;
	printf("\t[IX:%04X]", IX);
}

extern void start_delay_timer(); // from Program.cpp

void op_set_timer(byte val)
{
	DT = val;
	printf("\t[DT:%02X]", DT);
	start_delay_timer();
}

word pitch[] = {
	// 0x00-0x0F
	0,65,73,82,87,98,110,123,131,147,165,175,196,220,247,262,
	// 0x10-0x1D
	294,330,349,392,440,494,523,587,659,698,784,880,988,1047
};
byte pitch_size = 0x1E;

void op_set_pitch(byte val)
{
	if(val<pitch_size)
		sound_pitch = pitch[val];
}

void op_set_sound(byte val)
{
	ST = val;
	printf("\t[ST:%02X]", ST);

	// start sound
	sound_start(val/60.0);
}

void chip_timers_tick()
{
	printf("TIMERS:");
	if(DT>0)
	{
		DT--;
		printf("\t[DT:%02X]", DT);
	}
	if(ST>0)
	{
		ST--;
		printf("\t[ST:%02X]", ST);
		// check sound, eventuall turn off sound
		sound_check(); // worried if timer goes out before
	}
	printf("\n");
}

void op_rand(byte reg, byte val)
{
	int rnd = rand();
	printf("\trnd:%d", rnd);
	V[reg] = (byte)(rnd & 0xFF) & val;
	printf("\t[r%01X:%02X]", reg, V[reg]);
}

void op_draw(byte x, byte y, byte spr_h)
{
	V[15] = 0;

	// spr_h = sprite hight, i.e. number of bytes
	if(spr_h==0 && QUIRK_SPR16)
		spr_h = 16;

	for(byte i=0; i<spr_h; i++) { // screen_y=y+i
		word addr = IX+i;
		byte spr_b = mem[addr];
		for(byte cnt=0; cnt<8; cnt++) // just shift byte 8 times, screen_x=x+cnt
		{
			if((spr_b & 0x80)>0) // set pixel
				if(scr_xor_pixel(x+cnt, y+i))
					V[15] = 1;
			spr_b = spr_b<<1;
		}
	}
}

byte chip_get_key()
{
	byte ret = 0xFF; // no key

	/* key map:
	 * 1234		=> 123C
	 * qwer		=> 456D
	 * asdf		=> 789E
	 * zxcv		=> A0BF
	 */

	switch(KEY) {
		case '1':	ret = 0x01;		break;
		case '2':	ret = 0x02;		break;
		case '3':	ret = 0x03;		break;
		case '4':	ret = 0x0C;		break;
		case 'q':	ret = 0x04;		break;
		case 'w':	ret = 0x05;		break;
		case 'e':	ret = 0x06;		break;
		case 'r':	ret = 0x0D;		break;
		case 'a':	ret = 0x07;		break;
		case 's':	ret = 0x08;		break;
		case 'd':	ret = 0x09;		break;
		case 'f':	ret = 0x0E;		break;
		case 'z':	ret = 0x0A;		break;
		case 'x':	ret = 0x00;		break;
		case 'c':	ret = 0x0B;		break;
		case 'v':	ret = 0x0F;		break;
		default:	ret = 0xFF;		break;
	}
	printf("CHIP_KEY: '%c' %02X => %02X\n", KEY, KEY, ret);
	return ret;
}

// return true of ok. return false to quit
bool op_skip_equal_key(byte val)
{
	bool ret = true;
	if(KEY==27)
		ret = false;
	else {
		byte key = chip_get_key();
		ret = op_skip_equal(key, val);
	}
	return ret;
}

bool op_skip_not_equal_key(byte val)
{
	bool ret = true;
	if(KEY==27)
		ret = false;
	else {
		byte key = chip_get_key();
		ret = op_skip_not_equal(key, val);
	}
	return ret;
}

void op_wait_key_reg(byte reg)
{
	if(KEY!=0)
		V[reg] = chip_get_key();
	else
		PC=PC-2; // redo wait
}


bool op_sto_bcd(byte val)
{
	bool ret = true;
	// if over memory, return false
	byte work = val;
// 	printf(" work:%d", work);

	byte ones = work % 10;
	work = work / 10;
// 	printf(" ones:%d work:%d", ones, work);

	byte tens = work % 10;
	work = work / 10;
// 	printf(" tens:%d work:%d", tens, work);

	byte hundred = work % 10;
// 	printf(" hundred:%d", hundred);

	printf("\t[BCD: %d %d %d]", hundred, tens, ones);

	mem[IX]=hundred;
	mem[IX+1]=tens;
	mem[IX+2]=ones;

	if(!QUIRK_KEEPIX) // STD: IX changes
		IX+=3;

	return ret;
}


bool op_sto_mem_reg(byte reg1, byte reg2)
{
	bool ret = true;
	unsigned int ix = IX;

// 	printf("\nstore %X..%X @ ix:%X\n", reg1, reg2, ix);
	printf("\t[%X: ", ix);

	for(int i=reg1; i<=reg2; i++)
	{
// 		printf("ix:%X=", ix);
		if(ix>=MEM_SIZE) {
			printf("Out of memory IX:%X\n", ix);
			ret = false;
			break;
		} else {
			mem[ix] = V[i];
			printf("%X ", mem[ix]);
			ix++;
		}
	}
	printf("]");

	if(!QUIRK_KEEPIX)
		IX = ix;

	return ret;
}

bool op_rcl_mem_reg(byte reg1, byte reg2)
{
	bool ret = true;
	unsigned int ix = IX;

// 	printf("\nrecall %X..%X @ ix:%X\n", reg1, reg2, ix);
	printf("\t[%X: ", ix);

	for(int i=reg1; i<=reg2; i++)
	{
// 		printf("ix:%X=", ix);
		if(ix>=MEM_SIZE) {
			printf("Out of memory IX:%X\n", ix);
			ret = false;
			break;
		} else {
			V[i] = mem[ix];
			printf("%X ", mem[ix]);
			ix++;
		}
	}
	printf("]");

	if(!QUIRK_KEEPIX)
		IX = ix;

	return ret;
}



// // Support functions
// void chip_draw_sprite( byte* sprite,  int length,  int x,  int y)
// {
// 	// loop through sprite data and put in screen buffer
// 	scr_buffer[0] = SCREEN_PIXEL;
// }

// I need to make functions for each operator to make it readable and manageable

// execute 1
// return false to exit
// true for continue execution
bool chip_exec1()
{
	bool ret = true;
	if(PC>=(PROG_START+prog_size))
	{
		printf("End of program\n");
		ret = false;
	} else {

		printf("%04X:", PC);
		for(word ent : entry)
			if(ent==PC)
			{
				printf("@");
				break;
			}
		printf("\t");

		byte op1 = mem[PC++];
		byte op2 = mem[PC++];
		byte op1h = op1>>4;	// top nibble
		byte op1l = op1&0xF;	// low nibble
		byte op2h = op2>>4;
		byte op2l = op2&0xF;
		word  op12 = ((op1&0xF)<<8)|(op2);

		printf("%02X%02X\t", op1, op2);

		switch(op1h) {
			case SYS_OP:	// printf("SYS ");
				if(op1l==0) { // only 0x00pp
					if(op2h==OP_SCHP_SCRD)
						printf("SCRD %d", op2l);
					else
						switch(op2) {
							case NOP:	printf("NOP");
										break;

							case CLS:	printf("CLS");
										scr_clear();
										break;

							case RET:	printf("RET");
										ret = op_ret();
										break;

							case RST:	printf("RST");
										PC = 0x0000; // boot into hex monitor
										break;

							case SCRR:	printf("SCRR");				break;
							case SCRL:	printf("SCRL");				break;
							case LORES:	printf("LORES");			break;
							case HIRES:	printf("HIRES");			break;

							default:	printf("UNDEF");
										ret=false;
										break;
						}
				} else {
					printf("UNDEF");
					ret = false;
				}
				break;

			case JMP_N:		printf("JMP  %04X", op12);
							ret=op_jmp(op12);
							break;

			case CALL_N:	printf("CALL %04X", op12);
							ret=op_call(op12);
							break;

			case SKEQ_VN:	printf("SKEQ r%01X, #%02X", op1l, op2);
							ret=op_skip_equal(V[op1l], op2);
							break;

			case SKNE_VN:	printf("SKNE r%01X, #%02X", op1l, op2);
							ret=op_skip_not_equal(V[op1l], op2);
							break;

			case SKEQ_VV:	printf("SKEQ r%01X, r%01X", op1l, op2h);
							ret=op_skip_equal(V[op1l], V[op2h]);
							break;

			case SET_VN:	printf("SET  r%01X, #%02X", op1l, op2);
							op_set_reg(op1l, op2);
							break;

			case ADD_VN:	printf("ADD  r%01X, #%02X", op1l, op2);
							op_add_reg(op1l, op2);
							break;

			case ALU_OP:	// printf("ALU ");
				switch(op2l) {
					case CP:	printf("SET  r%01X, r%01X", op1l, op2h);
								op_set_reg(op1l, V[op2h]);
								break;

					case OR:	printf("OR   r%01X, r%01X", op1l, op2h);
								op_or_reg(op1l, V[op2h]);
								break;

					case AND:	printf("AND  r%01X, r%01X", op1l, op2h);
								op_and_reg(op1l, V[op2h]);
								break;

					case XOR:	printf("XOR  r%01X, r%01X", op1l, op2h);
								op_xor_reg(op1l, V[op2h]);
								break;

					case ADD:	printf("ADD  r%01X, r%01X", op1l, op2h);
								op_add_reg(op1l, V[op2h]);
								break;

					case SUB:	printf("SUB  r%01X, r%01X", op1l, op2h);
								op_sub_reg(op1l, V[op2h]);
								break;

					case SHR:	printf("SHR  r%01X, r%01X", op1l, op2h);
								op_shr_reg(op1l, V[op2h]);
								break;

					case RSUB:	printf("RSUB r%01X, r%01X", op1l, op2h);
								op_rsub_reg(op1l, V[op2h]);
								break;

					case SHL:	printf("SHL  r%01X, r%01X", op1l, op2h);
								op_shl_reg(op1l, V[op2h]);
								break;

					default:	printf("UNDEF");
								ret = false;
								break;
				}
				break;

			case SKNE_VV:	printf("SKNE r%01X, r%01X", op1l, op2h);
							op_skip_not_equal(V[op1l], V[op2h]);
							break;

			case SET_IN:	printf("SET  IX, #%04X", op12);
							op_set_ix(op12);
							break;

			case JMP_V0N:	printf("JPV0 %04X", op12);
							op_jmp(op2+V[0]);
							break;

			case RND_VN:	printf("RAND r%01X, #%02X", op1l, op2);
							op_rand(op1l, op2);
							break;


			case DRAW_VVN:	printf("DRAW (r%01X,r%01X), M(IX)..#%01X", op1l, op2h, op2l);
							op_draw(V[op1l], V[op2h], op2l);
							break;

			case KEY_OP:	// printf("KEY ");
				switch(op2) {
					case SKEQ_KV:	printf("SKEQ KEY, r%01X", op1l);
									// op_skip_equal(KEY, V[op1l]);
									ret = op_skip_equal_key(V[op1]);
									break;

					case SKNE_KV:	printf("SKNE KEY, r%01X", op1l);
									ret = op_skip_not_equal_key(V[op1l]);
									break;

					default:		printf("UNDEF");
									ret = false;
									break;
				}
				break;

			case SPEC_OP:	// printf("SPEC ");
				switch(op2) {
					case STOP_V:	printf("STOP r%01X", op1l);	// exit to emulator
									exit_code=V[op1l];
									ret=false;
									break;

					case GET_VT:	printf("SET  r%01X, TIMER", op1l);
									op_set_reg(op1l, DT);
									break;

					case WAIT_VK:	printf("WAIT r%01X, KEY", op1l);
									op_wait_key_reg(op1l);
									break;

					case SET_TV:	printf("SET  TIMER, r%01X", op1l);
									op_set_timer(V[op1l]);
									break;

					case SET_PV:	printf("SET  PITCH, r%01X", op1l);
									op_set_pitch(V[op1l]);
									break;

					case SET_SV:	printf("SET  SOUND, r%01X", op1l);
									op_set_sound(V[op1l]);
									break;

					case ADD_IV:	printf("ADD  IX, r%01X", op1l);
									// Atari does carry
									// Add quirk flags instead of emulator mode
									IX += V[op1l];
									break;

					case GET_IF:	printf("SET  IX, FONT(r%01X)", op1l);
									IX = FONT_START + V[op1l]*5;
									break;

					case BIG_IF:	printf("SET  IX, BIG(r%01X)", op1l);		break;

					case BCD_IV:	printf("BCD  M(IX), r%01X", op1l);
									ret = op_sto_bcd(V[op1l]);
									break;

					case STO_IV:	printf("STO  M(IX), r0..r%01X", op1l);
									ret = op_sto_mem_reg(0, op1l);
									break;

					case RCL_IV:	printf("RCL  r0..r%01X, M(IX)", op1l);
									ret = op_rcl_mem_reg(0, op1l);
									break;

					case OUT_RSV:	printf("OUT  r%01X", op1l);					break;
					case IN_VRS:	printf("IN   r%01X", op1l);					break;
					case SET_BV:	printf("SET  BAUD, r%01X", op1l);			break;
					case SAVE_V:	printf("SAVE r0..r%01X", op1l);				break;
					case LOAD_V:	printf("LOAD r0..r%01X", op1l);				break;

					default:		printf("UNDEF");
									ret = false;
									break;
				}
				break;
		}

		printf("\n");
	}
	return ret;
}


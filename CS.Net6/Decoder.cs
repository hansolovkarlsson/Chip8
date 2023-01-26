using System;

namespace Chip8
{
	// 1. Enum for every operation, but in regular sequential IDs, not op codes
	// 2. Struct with the OP_ID and data values associated with it, like n, nn, nnn, x, y
	// 3. Decoder method takes an integer, returns the struct
	// 4. Provide a print function to write it as assembly code

	// this way, the emulator take use it for execution
	// but also the disassembler can use it for printing

	public enum OP_ID {
		ID_INVALID_CODE,			// when no match
		ID_NOP,						// 0000			Color		No operation

		ID_JUMP_IX,					// 0001			Hyper		Jump to address by IX
		ID_INCREMENT_IX,			// 0002			Hyper
		ID_DECREMENT_IX,			// 0003			Hyper

		ID_SKIP_IF_ERASE_COLLISION,	// 0071			ACE			Collision when erase a pixel
		ID_SKIP_IF_DRAW_COLLISION,	// 0072			ACE			Collision when set a pixel
		ID_HALT,					// 007F			ACE			Halt CPU, wait for "reset" (any key)

		ID_SET_DATA_SEGMENT,		// 00An			Hyper		Set data segment, so when LDIXnnn is used, most significant nibble is set by DS
		ID_SET_CODE_SEGMENT,		// 00Bn			Hyper		Set code segment, add nibble to JUMP, CALL
		ID_SCROLL_DOWN_N,			// 00Cn			SChip		scroll N/2 pix for lowres, N pix for highres
		ID_SCROLL_UP_N,				// 00Dn			XOChip		scroll up, like scroll down

		ID_CLEAR_SCREEN,			// 00E0
		ID_RETURN,					// 00EE

		ID_SCROLL_RIGHT,			// 00FB			SChip		Scroll 2 pix in lowres, 4 pix for highres
		ID_SCROLL_LEFT,				// 00FC			SChip		Scroll 2 pix lowres, 4 pix hires
		ID_QUIT,					// 00FD			SChip
		ID_MODE_CHIP8_GRAPHICS,		// 00FE			SChip		Change resolution and clear screen
		ID_MODE_SCHIP_GRAPHICS,		// 00FF			SChip

		ID_JUMP_NNN,				// 1nnn
		ID_CALL_NNN,				// 2nnn
		ID_SKIP_IF_X_EQ_NN,			// 3xnn
		ID_SKIP_IF_X_NEQ_NN,		// 4xnn

		ID_SKIP_IF_X_EQ_Y,			// 5xy0
		ID_STORE_IX_X_Y,			// 5xy2			XOChip,8E	Save Vx..Vy to [IX..], no change to IX
		ID_RECALL_IX_X_Y,			// 5xy3			XOChip,8E	Load Vx..Vy from [IX..], no change to IX
		ID_MUL_X_Y,					// 5xy4			Hyper		VF,Vx = Vx*Vy (ELF:9xy1)
		ID_DIV_X_Y,					// 5xy6			Hyper		Vx=Vx/Vy (modulo), VF=remainder (ELF:9xy2)

		ID_COPY_X_NN,				// 6xnn
		ID_ADD_X_NN,				// 7xnn

		ID_COPY_X_Y,				// 8xy0
		ID_OR_X_Y,					// 8xy1
		ID_AND_X_Y,					// 8xy2
		ID_XOR_X_Y,					// 8xy3
		ID_ADD_X_Y,					// 8xy4
		ID_SUB_X_Y,					// 8xy5
		ID_SHIFT_RIGHT_X,			// 8xy6
		ID_NEGATIVE_SUB_X_Y,		// 8xy7
		ID_SHIFT_LEFT_X,			// 8xyE

		ID_SKIP_IF_X_NEQ_Y,			// 9xy0
		ID_SKIP_IF_X_LT_Y,			// 9xy1			Hyper		Skip if X<Y (ELF:5xy2)
		ID_SKIP_IF_X_GT_Y,			// 9xy2			Hyper		Skip if X>Y (ACE,ELF:5xy1)
		ID_SKIP_IF_X_LE_Y,			// 9xy3			Hyper		Skip if X<=Y
		ID_SKIP_IF_X_GE_Y,			// 9xy4			Hyper		Skip if X>=Y

		ID_COPY_IX_NNN,				// Annn
		ID_JUMP_V0_NNN,				// Bnnn
		ID_RANDOM_X_AND_NN,			// Cxnn
		ID_DRAW_X_Y_N,				// Dxyn

		ID_SKIP_IF_KEY_EQ_X,		// Ex9E						Hex board key (Full keyboard at Fx9E)
		ID_SKIP_IF_KEY_NEQ_X,		// ExA1						Hex board key (Full keyboard at FxA1)

		ID_GET_TIMER_X,				// Fx07
		ID_WAIT_KEY_X,				// Fx0A
		ID_GET_KB_X,				// Fx0E			ACE			Get full keyboard key to Vx

		ID_SET_TIMER_X,				// Fx15
		ID_SET_PITCH_X,				// Fx17			Color		Set sound pitch
		ID_SET_SOUND_X,				// Fx18
		ID_ADD_IX_X,				// Fx1E

		ID_GET_FONT_IX_X,			// Fx29						5 byte font sprite
		ID_GET_CHAR_IX_X,			// Fx2A			Color		Get 7 byte sprite ascii character font
		ID_GET_FONT_IX_HX,			// FX2C			ACE			Point I at 5 byte sprite for Vx most significant nibble (FONT only does LSN-less significant nibble)

		ID_GET_HIFONT_IX_X,			// Fx30			SChip1		10 byte font
		ID_STORE_BCD_IX_X,			// Fx33

		ID_WAIT_TIMER_X,			// Fx4F			Chip8E		Set Timer to Vx and wait until timer=0

		ID_CLEAR_HX,				// Fx50			ACE			Clear most significnat nibble of Vx (VX = Vx&0F)
		ID_STORE_IX_X,				// Fx55						Mode IXINC, Mode IXFIX, default IXFIX, IX no change after STORE/RECALL
		ID_RECALL_IX_X,				// Fx65

		ID_OUT_X,					// Fx70			Color		Send Vx to data port
		ID_IN_X,					// Fx71			Color		Wait for and receive data from port to Vx
		ID_SPEED_X,					// Fx72			Color		Set port speed
		ID_SET_COLOR_X,				// Fx75			Color		Set color to Vx, IIRRGGBB. 0x00=no pixel, any value is a pixel for collision ("xor")

		ID_SAVE_HP_X,				// Fx75
		ID_LOAD_HP_X,				// Fx85

		ID_SKIP_IF_KB_EQ_X,			// Ex9E			HC8		Skip if KB key == Vx
		ID_SKIP_IF_KB_NEQ_X,		// ExA1			HC8		Skip if KB key <> Vx

		ID_NOT_X,					// ExB0			HC8
		ID_NEGATE_X,				// ExB1			HC8
		ID_INCREMENT_X,				// ExB2			HC8
		ID_DECREMENT_X,				// ExB3			HC8

		ID_SUB_IX_X,				// FxD4			ACE		IX = IX - Vx

		ID_COPY_IX_NNNN,			// FFFF nnnn	ELF		Double instruction, LDL IX nnnn (long address)

		ID_ENUM_SIZE,				// should be last number
	};





	// struct for return from the actual decode function
	public struct OP_DECODE_RET {
		public OP_ID	id;
		public byte		n;
		public byte		nn;
		public int		nnn;
		public byte		vx;
		public byte		vy;
	};

	class Decoder
	{

		// OP Code Groups
		private const int OPGRP_MASK = 0xF000;
		private enum OPGRP {
			OP_SYS				= 0x0000,		// 00pp		sys calls
			OP_JUMP_NNN			= 0x1000,		// 1nnn		jump to nnn
			OP_CALL_NNN			= 0x2000,		// 2nnn		call subroutine at nnn
			OP_SKIP_IF_X_EQ_NN	= 0x3000,		// 3xnn		skip if Vx==nn
			OP_SKIP_IF_X_NEQ_NN	= 0x4000,		// 4xnn		skip if Vx<>nn
			OP_SKIP_IF_X_EQ_Y	= 0x5000,		// 5xy0		skip if Vx==Vy
			OP_COPY_X_NN		= 0x6000,		// 6xnn		let Vx = nn
			OP_ADD_X_NN			= 0x7000,		// 7xnn		let Vx = Vx + nn
			OP_ALU				= 0x8000,		// 8xyp		alu functions
			OP_SKIP_IF_X_NEQ_Y	= 0x9000,		// 9xy0		skip if Vx<>Vy
			OP_COPY_IX_NNN		= 0xA000,		// Annn		let IX = nnn
			OP_JUMP_V0_NNN		= 0xB000,		// Bnnn		jump to nnn+V0
			OP_RANDOM_X_AND_NN	= 0xC000,		// Cxnn		let Vx = rand & nn
			OP_DRAW_X_Y_N		= 0xD000,		// Dxyn		draw sprite at (Vx,Vy) n height
			OP_KEY				= 0xE000,		// Expp		key press functions
			OP_MISC				= 0xF000,		// Fxpp		miscellaneous functions
		};

		// Special OP from SChip that needs a nibble parameter
		// 0x00Cn
		private const int OPSPEC_MASK		= 0xFFF0;
		private const int OP_SCROLL_DOWN_N	= 0x00C0;	// 00Cn	scroll down n pixels

		// SYS OP2: 0x0ppp
		private const int OPSYS_MASK 	= 0x0FFF;
		private enum OPSYS {
			OP_SCROLL_RIGHT				= 0x00FB,		// 00FB		scroll right 4 pixels
			OP_SCROLL_LEFT				= 0x00FC,		// 00FC		scroll left 4 pixels
			OP_QUIT						= 0x00FD,		// 00FD		quit the emulator
			OP_SET_CHIP8_GRAPHIC_MODE	= 0x00FE,		// 00FE		set graphic mode Chip-8
			OP_SET_SCHIP_GRAPHIC_MODE	= 0x00FF,		// 00FF		set graphic mode Super-Chip
			OP_ERASE_SCREEN				= 0x00E0,		// 00E0		clear screen
			OP_RETURN_FROM_SUBROUTINE	= 0x00EE,		// 00EE		return from subroutine
		};

		// ALU Operations: 0x8xyp
		private const int OPALU_MASK = 0xF00F;
		private enum OPALU {
			OP_COPY_X_Y			= 0x8000,		// 8xy0		let Vx=Vy
			OP_OR_X_Y			= 0x8001,		// 8xy1		let Vx=Vx|Vy
			OP_AND_X_Y			= 0x8002,		// 8xy2		let Vx=Vx&Vy
			OP_XOR_X_Y			= 0x8003,		// 8xy3		let Vx=Vx^Vy
			OP_ADD_X_Y			= 0x8004,		// 8xy4		let Vx=Vx+Vy, Vf=carry
			OP_SUB_X_Y			= 0x8005,		// 8xy5		let Vx=Vx-Vy, Vf=~borrow
			OP_SHIFT_RIGHT_X	= 0x8006,		// 8xy6		let Vx=Vx>>1, Vf=carry
			OP_NEGATIVE_SUB_X_Y	= 0x8007,		// 8xy7		let Vx=Vy-Vx, Vf=~borrow
			OP_SHIFT_LEFT_X		= 0x800E,		// 8xyE		let Vx=Vx<<1, Vf=carry
		};

		// KEY Operations: 0xExpp
		private const int OPKEY_MASK = 0xF0FF;
		private enum OPKEY {
			OP_SKIP_IF_KEY_EQ_X		= 0xE09E,		// Ex9E		skip if key==Vx
			OP_SKIP_IF_KEY_NEQ_X	= 0xE0A1,		// ExA1		skip if key<>Vx
		};

		// MISC Operations: 0xFxpp
		private const int OPMISC_MASK = 0xF0FF;
		private enum OPMISC {
			OP_GET_TIMER_X		= 0xF007,		// Fx07		let Vx=timer
			OP_WAIT_KEY_X		= 0xF00A,		// Fx0A		wait for key, Vx=key
			OP_SET_TIMER_X		= 0xF015,		// Fx15		let timer=Vx
			OP_SET_SOUND_X		= 0xF018,		// Fx18		let sound=Vx
			OP_ADD_IX_X			= 0xF01E,		// Fx1E		let IX=IX+Vx (no carry)
			OP_GET_FONT_IX_X	= 0xF029,		// Fx29		let IX=FONT(Vx) 4x5 font sprite
			OP_STORE_BCD_IX_X	= 0xF033,		// Fx33		store BCD(Vx) in M(IX..IX+2)
			OP_STORE_IX_X		= 0xF055,		// Fx55		store V0..Vx in M(IX..)
			OP_RECALL_IX_X		= 0xF065,		// Fx65		load V0..Vx from M(IX..)
			OP_SAVE_HP_X		= 0xF075,		// Fx75		save V0..Vx to file
			OP_LOAD_HP_X		= 0xF085,		// Fx85		load V0..Vx from file
		};

		public static OP_DECODE_RET Decode(int op)
		{
			OP_DECODE_RET ret = new OP_DECODE_RET(){
				id = OP_ID.ID_INVALID_CODE,
				nnn = op & 0x0FFF,
				nn = (byte)(op & 0x00FF),
				n = (byte)(op & 0x000F),
				vx = (byte)((op & 0x0F00)>>8),
				vy = (byte)((op & 0x00F0)>>4)
			};


			// break up into the op-groups
			switch((OPGRP)(op & OPGRP_MASK))
			{
				case OPGRP.OP_JUMP_NNN:			ret.id = OP_ID.ID_JUMP_NNN;			break;
				case OPGRP.OP_CALL_NNN:			ret.id = OP_ID.ID_CALL_NNN;			break;
				case OPGRP.OP_SKIP_IF_X_EQ_NN:	ret.id = OP_ID.ID_SKIP_IF_X_EQ_NN;	break;
				case OPGRP.OP_SKIP_IF_X_NEQ_NN:	ret.id = OP_ID.ID_SKIP_IF_X_NEQ_NN;	break;
				case OPGRP.OP_SKIP_IF_X_EQ_Y:	ret.id = OP_ID.ID_SKIP_IF_X_EQ_Y;	break;
				case OPGRP.OP_COPY_X_NN:			ret.id = OP_ID.ID_COPY_X_NN;			break;
				case OPGRP.OP_ADD_X_NN:			ret.id = OP_ID.ID_ADD_X_NN;			break;
				case OPGRP.OP_SKIP_IF_X_NEQ_Y:	ret.id = OP_ID.ID_SKIP_IF_X_NEQ_Y;	break;
				case OPGRP.OP_COPY_IX_NNN:		ret.id = OP_ID.ID_COPY_IX_NNN;		break;
				case OPGRP.OP_JUMP_V0_NNN:		ret.id = OP_ID.ID_JUMP_V0_NNN;		break;
				case OPGRP.OP_RANDOM_X_AND_NN:	ret.id = OP_ID.ID_RANDOM_X_AND_NN;	break;
				case OPGRP.OP_DRAW_X_Y_N:		ret.id = OP_ID.ID_DRAW_X_Y_N;		break;

				case OPGRP.OP_SYS:
					if((op & OPSPEC_MASK)==OP_SCROLL_DOWN_N)		ret.id = OP_ID.ID_SCROLL_DOWN_N;
					else
						switch((OPSYS)(op & OPSYS_MASK))
						{
							case OPSYS.OP_QUIT:						ret.id = OP_ID.ID_QUIT;						break;
							case OPSYS.OP_ERASE_SCREEN: 			ret.id = OP_ID.ID_CLEAR_SCREEN; 			break;
							case OPSYS.OP_RETURN_FROM_SUBROUTINE:	ret.id = OP_ID.ID_RETURN;	break;
							case OPSYS.OP_SCROLL_LEFT:				ret.id = OP_ID.ID_SCROLL_LEFT;				break;
							case OPSYS.OP_SCROLL_RIGHT:				ret.id = OP_ID.ID_SCROLL_RIGHT;				break;
							case OPSYS.OP_SET_CHIP8_GRAPHIC_MODE:	ret.id = OP_ID.ID_MODE_CHIP8_GRAPHICS;	break;
							case OPSYS.OP_SET_SCHIP_GRAPHIC_MODE:	ret.id = OP_ID.ID_MODE_SCHIP_GRAPHICS;	break;
						}
					break;

				case OPGRP.OP_ALU:
					switch((OPALU)(op & OPALU_MASK))
					{
						case OPALU.OP_COPY_X_Y:				ret.id = OP_ID.ID_COPY_X_Y;				break;
						case OPALU.OP_AND_X_Y:				ret.id = OP_ID.ID_AND_X_Y;				break;
						case OPALU.OP_OR_X_Y:				ret.id = OP_ID.ID_OR_X_Y;				break;
						case OPALU.OP_XOR_X_Y:				ret.id = OP_ID.ID_XOR_X_Y;				break;
						case OPALU.OP_SHIFT_LEFT_X:			ret.id = OP_ID.ID_SHIFT_LEFT_X;			break;
						case OPALU.OP_SHIFT_RIGHT_X:		ret.id = OP_ID.ID_SHIFT_RIGHT_X;		break;
						case OPALU.OP_ADD_X_Y:				ret.id = OP_ID.ID_ADD_X_Y;				break;
						case OPALU.OP_SUB_X_Y:				ret.id = OP_ID.ID_SUB_X_Y;				break;
						case OPALU.OP_NEGATIVE_SUB_X_Y:		ret.id = OP_ID.ID_NEGATIVE_SUB_X_Y;		break;
					}
					break;

				case OPGRP.OP_KEY:
					switch((OPKEY)(op & OPKEY_MASK))
					{
						case OPKEY.OP_SKIP_IF_KEY_EQ_X:		ret.id = OP_ID.ID_SKIP_IF_KEY_EQ_X;		break;
						case OPKEY.OP_SKIP_IF_KEY_NEQ_X:	ret.id = OP_ID.ID_SKIP_IF_KEY_NEQ_X;	break;
					}
					break;

				case OPGRP.OP_MISC:
					switch((OPMISC)(op & OPMISC_MASK))
					{
						case OPMISC.OP_ADD_IX_X:			ret.id = OP_ID.ID_ADD_IX_X;				break;
						case OPMISC.OP_GET_FONT_IX_X:		ret.id = OP_ID.ID_GET_FONT_IX_X;		break;
						case OPMISC.OP_WAIT_KEY_X:			ret.id = OP_ID.ID_WAIT_KEY_X;			break;
						case OPMISC.OP_GET_TIMER_X:			ret.id = OP_ID.ID_GET_TIMER_X;			break;
						case OPMISC.OP_SET_SOUND_X:			ret.id = OP_ID.ID_SET_SOUND_X;			break;
						case OPMISC.OP_SET_TIMER_X:			ret.id = OP_ID.ID_SET_TIMER_X;			break;
						case OPMISC.OP_STORE_BCD_IX_X:		ret.id = OP_ID.ID_STORE_BCD_IX_X;		break;
						case OPMISC.OP_STORE_IX_X:			ret.id = OP_ID.ID_STORE_IX_X;			break;
						case OPMISC.OP_RECALL_IX_X:			ret.id = OP_ID.ID_RECALL_IX_X;			break;
						case OPMISC.OP_SAVE_HP_X:			ret.id = OP_ID.ID_SAVE_HP_X;			break;
						case OPMISC.OP_LOAD_HP_X:			ret.id = OP_ID.ID_LOAD_HP_X;			break;
					}
					break;
			}

			return ret;
		}

	}
}


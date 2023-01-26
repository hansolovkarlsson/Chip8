using System;
using System.IO;
using System.Text;


namespace Chip8
{
	class Emulator
	{
		// S-Chip8 emulator, uses "Peripheral" class for display, sound, timer, keyboard input
		// init of peripheral done by main method, not in here

		public bool running;	// true: running, false: halted

		public const int STACK_SIZE	= 256;		// much larger than original
		public const int MEM_SIZE	= 65536;	// allow for full memory
		public const int REG_SIZE	= 16;
		public const int PROG_START	= 0x200;
		public const int PROG_END	= 0xFFF;
		public const int FONT_START	= 0x1000;
		public const int DISP_START = 0xE000;	// 0x1000 (4096) bytes for display

		// public const string SAVEFILE= "chip8.save";

		// Registers

		public int		IP;		// program counter
		public int		OP;		// current operator
		public int		SP;		// stack pointer
		public int		IX;		// index register
		public byte[]	VAR;	// registers, Vx, Vy, etc

		// delay timer, sound timer, key, etc external in Peripheral
		// public Peripheral pio;
		// should be set by main routine, so CPU can set timer etc, call for display refresh

		// Stack
		public int[] stack;

		// Memory
		public byte[] mem;

		// Font data
		public byte[] fontdata = {
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


		// BIOS Monitor ROM, load to 0x0000-0x01FF
		private byte[] bios = { 0x12, 0x00 }; // default, jump to x200


		// Init registers, stack, memory (load font)
		public Emulator()
		{
			Init();
		}

		public void Init()
		{
			IP = 0x0000;	// start at 0, BIOS/Monitor Code
			OP = 0;
			IX = 0;
			SP = 0;
			VAR = new byte[REG_SIZE];
			stack = new int[STACK_SIZE];
			mem = new byte[MEM_SIZE];

			for(int i=0; i<bios.Length; i++)
				mem[i] = bios[i];

			for(int i=0; i<fontdata.Length; i++)
				mem[FONT_START+i] = fontdata[i];

			running = true;
		}

		// return size
		public int LoadFile(string fileName, int start_addr)
		{
			int size = 0;
			if (File.Exists(fileName))
        	{
				byte[] bin = File.ReadAllBytes(fileName);
				for(int i=0; i<bin.Length; i++)
					mem[start_addr+i] = bin[i];
				size = bin.Length;
        	} else {
				Console.WriteLine($"Error: file does not exist {fileName}");
			}
			return size;
		}

		public int SaveFile(string fileName, int fromAddress, int toAddress)
		{
			byte[] bin = new byte[toAddress-fromAddress];
			for(int i=0; i<bin.Length; i++)
				bin[i] = mem[fromAddress+i];
			File.WriteAllBytes(fileName, bin);
			return bin.Length;
		}

		public void Execute1()
		{
			byte op1 = mem[IP];
			byte op2 = mem[IP+1];

			OP = (op1<<8)|op2;	// full 16bit op

			/*
			// for convenience and easier to read code
			// set up different aliases
			int nnn = OP & 0x0FFF;			// if I need 0nnn
			byte nn = op2;					// if I need 00nn
			byte x = (byte)(op1 & 0x0F);	// if I need 0x00
			byte y = (byte)(op2 >> 4);		// if I need 00y0
			byte n = (byte)(op2 & 0x0F);	// if I need 000n
			*/

			// trace here
			IP+=2;

			OP_DECODE_RET decode = Decoder.Decode(OP);

			switch(decode.id)
			{
				case OP_ID.ID_INVALID_CODE:					Console.WriteLine($"ERROR: Invalid code IP:{IP-2:X4} OP:{OP:X4}");		break;
				case OP_ID.ID_SCROLL_DOWN_N:				op_scroll_down(decode.n);							break;
				case OP_ID.ID_SCROLL_RIGHT:					op_scroll_right();									break;
				case OP_ID.ID_SCROLL_LEFT:					op_scroll_left();									break;
				case OP_ID.ID_QUIT:							running = false;									break;
				case OP_ID.ID_MODE_CHIP8_GRAPHICS:			op_set_graphics_mode(GRAPHIC_MODE.MODE_64x32);		break;
				case OP_ID.ID_MODE_SCHIP_GRAPHICS:			op_set_graphics_mode(GRAPHIC_MODE.MODE_128x64);		break;
				case OP_ID.ID_CLEAR_SCREEN:					op_erase_screen();									break;
				case OP_ID.ID_RETURN:						op_return();										break;
				case OP_ID.ID_JUMP_NNN:						op_jump(decode.nnn);								break;
				case OP_ID.ID_CALL_NNN:						op_call(decode.nnn);								break;
				case OP_ID.ID_SKIP_IF_X_EQ_NN:				op_skip_eq(VAR[decode.vx], decode.nn);				break;
				case OP_ID.ID_SKIP_IF_X_NEQ_NN:				op_skip_neq(VAR[decode.vx], decode.nn);				break;
				case OP_ID.ID_SKIP_IF_X_EQ_Y:				op_skip_eq(VAR[decode.vx], VAR[decode.vy]);			break;
				case OP_ID.ID_COPY_X_NN:					op_set_var(decode.vx, decode.nn);						break;
				case OP_ID.ID_ADD_X_NN:						op_add(decode.vx, decode.nn);						break;
				case OP_ID.ID_COPY_X_Y:						op_set_var(decode.vx, VAR[decode.vy]);					break;
				case OP_ID.ID_OR_X_Y:						op_or(decode.vx, VAR[decode.vy]);					break;
				case OP_ID.ID_AND_X_Y:						op_and(decode.vx, VAR[decode.vy]);					break;
				case OP_ID.ID_XOR_X_Y:						op_xor(decode.vx, VAR[decode.vy]);					break;
				case OP_ID.ID_ADD_X_Y:						op_add(decode.vx, VAR[decode.vy]);					break;
				case OP_ID.ID_SUB_X_Y:						op_sub(decode.vx, VAR[decode.vy]);					break;
				case OP_ID.ID_SHIFT_RIGHT_X:				op_shift_right(decode.vx);							break;
				case OP_ID.ID_NEGATIVE_SUB_X_Y:				op_negsub(decode.vx, VAR[decode.vy]);				break;
				case OP_ID.ID_SHIFT_LEFT_X:					op_shift_left(decode.vx);							break;
				case OP_ID.ID_SKIP_IF_X_NEQ_Y:				op_skip_neq(VAR[decode.vx], VAR[decode.vy]);		break;
				case OP_ID.ID_COPY_IX_NNN:					op_set_ix(decode.nnn);								break;
				case OP_ID.ID_JUMP_V0_NNN:					op_jump(decode.nnn+VAR[0]);							break;
				case OP_ID.ID_RANDOM_X_AND_NN:				op_random(decode.vx, decode.nn);					break;
				case OP_ID.ID_DRAW_X_Y_N:					op_draw(VAR[decode.vx], VAR[decode.vy], decode.n);	break;
				case OP_ID.ID_SKIP_IF_KEY_EQ_X:				op_skip_key_eq(VAR[decode.vx]);						break;
				case OP_ID.ID_SKIP_IF_KEY_NEQ_X:			op_skip_key_neq(VAR[decode.vx]);					break;
				case OP_ID.ID_GET_TIMER_X:					op_get_timer(decode.vx);							break;
				case OP_ID.ID_WAIT_KEY_X:					op_wait_key(decode.vx);								break;
				case OP_ID.ID_SET_TIMER_X:					op_set_timer(VAR[decode.vx]);						break;
				case OP_ID.ID_SET_SOUND_X:					op_set_sound(VAR[decode.vx]);						break;
				case OP_ID.ID_ADD_IX_X:						op_add_ix(VAR[decode.vx]);							break;
				case OP_ID.ID_GET_FONT_IX_X:				op_get_font(VAR[decode.vx]);						break;
				case OP_ID.ID_STORE_BCD_IX_X:				op_store_bcd(VAR[decode.vx]);						break;
				case OP_ID.ID_STORE_IX_X:					op_store_vars(VAR[decode.vx]);						break;
				case OP_ID.ID_RECALL_IX_X:					op_recall_vars(VAR[decode.vx]);						break;
				case OP_ID.ID_SAVE_HP_X:					op_save_hp_vars(VAR[decode.vx]);					break;
				case OP_ID.ID_LOAD_HP_X:					op_load_hp_vars(VAR[decode.vx]);					break;
			}
		}

		// display, graphics
		void op_erase_screen(){}

		enum GRAPHIC_MODE { MODE_64x32, MODE_128x64 };
		void op_set_graphics_mode(GRAPHIC_MODE mode){}

		void op_scroll_down(byte num){}
		void op_scroll_right(){}
		void op_scroll_left(){}
		void op_draw(byte x, byte y, byte size){}

		// flow
		void op_jump(int addr){}
		void op_call(int addr){}
		void op_return(){}
		void op_skip_eq(byte num1, byte num2){}
		void op_skip_neq(byte num1, byte num2){}

		// alu
		void op_set_var(byte reg, byte num){}
		void op_and(byte reg, byte num){}
		void op_or(byte reg, byte num){}
		void op_xor(byte reg, byte num){}
		void op_add(byte reg, byte num){}
		void op_sub(byte reg, byte num){}
		void op_negsub(byte reg, byte num){}
		void op_shift_right(byte reg){}
		void op_shift_left(byte reg){}

		void op_random(byte reg, byte num){}

		// index
		void op_set_ix(int num){}
		void op_add_ix(int num){}
		void op_get_font(byte num){}

		// key
		void op_skip_key_eq(byte num){}
		void op_skip_key_neq(byte num){}
		void op_wait_key(byte reg){}

		// other
		void op_set_timer(byte num){}
		void op_get_timer(byte reg){}
		void op_set_sound(byte num){}

		// memory
		void op_store_bcd(byte num){}
		void op_store_vars(byte num){}
		void op_recall_vars(byte num){}
		void op_save_hp_vars(byte num){}
		void op_load_hp_vars(byte num){}


	}
}

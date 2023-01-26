using System;
using System.Collections.Generic;

// I should put the emulator optab in decoder.cs

namespace Chip8
{
	class Disassembler
	{
		// automatic label creation
		// id: _L<address>
		// which makes it easy to replace LETIX, CALL, JMP with a label name and assign the address
		// also two pass so the correct assignments of the labels can be done as well
		// do an array of address, label, assembly string

		private static Dictionary<int,string> labels = new Dictionary<int,string>(); // address:int, id:string

		private static Dictionary<int,string> dis_text = new Dictionary<int, string>(); // address:int, dis_text:string

		public static string Disassemble(byte[] code_block, int start_addr)
		{
			string ret = "";
			int addr = start_addr;

			labels.Clear();
			dis_text.Clear();


			// Loop through and create dis_text
			for(int i=0; i<code_block.Length; i+=2)
			{
				int op_code = (code_block[i] << 8);
				if((i+1)<code_block.Length) op_code |= code_block[i+1];
				dis_text.Add(addr, $"{op_code:X4}          {Disassemble1(op_code)}");
				addr += 2;
			}

			// Loop dis_text+labels and create ret text
			foreach(KeyValuePair<int,string> item in dis_text)
			{
				if(labels.ContainsKey(item.Key)) {
					ret += $"            {labels[item.Key]}:\n";
				}
				ret += $"{item.Key:X4}: {item.Value}\n";
			}

			return ret;
		}

		public static string Disassemble1(int op)
		{
			OP_DECODE_RET decode = Decoder.Decode(op);
			string ret = $"Unknown code {op:X4} '{(char)(op>>8)}{(char)(op&0xFF)}'";

			// need coder for proper naming of the mnemonics
			switch(decode.id)
			{
				case OP_ID.ID_INVALID_CODE:				break;
				case OP_ID.ID_SCROLL_DOWN_N:			ret=$"SCRL    DOWN {decode.n:X}h";								break;
				case OP_ID.ID_SCROLL_RIGHT:				ret=$"SCRL    RIGHT";											break;
				case OP_ID.ID_SCROLL_LEFT:				ret=$"SCRL    LEFT";											break;
				case OP_ID.ID_QUIT:						ret=$"QUIT";													break;
				case OP_ID.ID_MODE_CHIP8_GRAPHICS:		ret=$"MODE    64x32";											break;
				case OP_ID.ID_MODE_SCHIP_GRAPHICS:		ret=$"MODE    128x64";											break;
				case OP_ID.ID_CLEAR_SCREEN:				ret=$"CLS";														break;
				case OP_ID.ID_RETURN:					ret=$"RET";														break;
				case OP_ID.ID_JUMP_NNN:					ret=$"JUMP    {AddLabel(decode.nnn)}";							break;
				case OP_ID.ID_CALL_NNN:					ret=$"CALL    {AddLabel(decode.nnn)}";							break;
				case OP_ID.ID_SKIP_IF_X_EQ_NN:			ret=$"SKEQ    V{decode.vx:X}, {decode.nn:X2}h";					break;
				case OP_ID.ID_SKIP_IF_X_NEQ_NN:			ret=$"SKNE    V{decode.vx:X}, {decode.nn:X2}h";					break;
				case OP_ID.ID_SKIP_IF_X_EQ_Y:			ret=$"SKEQ    V{decode.vx:X}, V{decode.vy:X}";					break;
				case OP_ID.ID_COPY_X_NN:				ret=$"CP      V{decode.vx:X}, {decode.nn:X2}h";					break;
				case OP_ID.ID_ADD_X_NN:					ret=$"ADD     V{decode.vx:X}, {decode.nn:X2}h";					break;
				case OP_ID.ID_COPY_X_Y:					ret=$"CP      V{decode.vx:X}, V{decode.vy:X}";					break;
				case OP_ID.ID_OR_X_Y:					ret=$"OR      V{decode.vx:X}, V{decode.vy:X}";					break;
				case OP_ID.ID_AND_X_Y:					ret=$"AND     V{decode.vx:X}, V{decode.vy:X}";					break;
				case OP_ID.ID_XOR_X_Y:					ret=$"XOR     V{decode.vx:X}, V{decode.vy:X}";					break;
				case OP_ID.ID_ADD_X_Y:					ret=$"ADD     V{decode.vx:X}, V{decode.vy:X}";					break;
				case OP_ID.ID_SUB_X_Y:					ret=$"SUB     V{decode.vx:X}, V{decode.vy:X}";					break;
				case OP_ID.ID_SHIFT_RIGHT_X:			ret=$"SHR     V{decode.vx:X}";									break;
				case OP_ID.ID_NEGATIVE_SUB_X_Y:			ret=$"NSUB    V{decode.vx:X}, V{decode.vy:X}";					break;
				case OP_ID.ID_SHIFT_LEFT_X:				ret=$"SHL     V{decode.vx:X}";									break;
				case OP_ID.ID_SKIP_IF_X_NEQ_Y:			ret=$"SKNE    V{decode.vx:X}, V{decode.vy:X}";					break;
				case OP_ID.ID_COPY_IX_NNN:				ret=$"CP      IX, {AddLabel(decode.nnn)}";						break;
				case OP_ID.ID_JUMP_V0_NNN:				ret=$"JUMP    V0, {decode.nnn:X3}h";							break;
				case OP_ID.ID_RANDOM_X_AND_NN:			ret=$"RAND    V{decode.vx:X}, {decode.nn:X2}h";					break;
				case OP_ID.ID_DRAW_X_Y_N:				ret=$"DRAW    V{decode.vx:X}, V{decode.vy:X}, {decode.n:X}h";	break;
				case OP_ID.ID_SKIP_IF_KEY_EQ_X:			ret=$"SKEQ    KEY, V{decode.vx:X}";								break;
				case OP_ID.ID_SKIP_IF_KEY_NEQ_X:		ret=$"SKNE    KEY, V{decode.vx:X}";								break;
				case OP_ID.ID_GET_TIMER_X:				ret=$"GET     TIMER, V{decode.vx:X}";							break;
				case OP_ID.ID_WAIT_KEY_X:				ret=$"WAIT    KEY, V{decode.vx:X}";								break;
				case OP_ID.ID_SET_TIMER_X:				ret=$"SET     TIMER, V{decode.vx:X}";							break;
				case OP_ID.ID_SET_SOUND_X:				ret=$"SET     SOUND, V{decode.vx:X}";							break;
				case OP_ID.ID_ADD_IX_X:					ret=$"ADD     IX, V{decode.vx:X}";								break;
				case OP_ID.ID_GET_FONT_IX_X:			ret=$"FONT    IX, V{decode.vx:X}";								break;
				case OP_ID.ID_STORE_BCD_IX_X:			ret=$"BCD     V{decode.vx:X}";									break;
				case OP_ID.ID_STORE_IX_X:				ret=$"STO     IX, V{decode.vx:X}";								break;
				case OP_ID.ID_RECALL_IX_X:				ret=$"RCL     IX, V{decode.vx:X}";								break;
				case OP_ID.ID_SAVE_HP_X:				ret=$"SAVE    V{decode.vx:X}";									break;
				case OP_ID.ID_LOAD_HP_X:				ret=$"LOAD    V{decode.vx:X}";									break;
			}
			return ret;
		}

		static string AddLabel(int nnn)
		{
			string label = $"L_{nnn:X4}";
			if(labels.ContainsKey(nnn))
				label = labels[nnn];
			else
				labels.Add(nnn, label);
			return label;
		}

    }
}


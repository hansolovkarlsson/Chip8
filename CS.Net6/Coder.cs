using System;
using System.Collections.Generic;

namespace Chip8
{

	class Coder
	{
		// ":" to separate multiple instructions on one line
		// ";" start comment, rest of line ignored

		public static string ARG_SEP = ",";
		public static string INSTR_SEP = ":";
		public static string COMMENT = ";";

		private enum ARG_TYPE {
			NONE,
			VARX,		// Vx		.x..			(num & F)<<8
			VARY,		// Vy		..y.			(num & F)<<4
			NNN,		// nnn		.nnn
			NN,			// nn		..nn
			N,			// n		...n
		}

		private class ASM_MNEM {
			public OP_ID			id;
			public string			instr;
			public int				args;		// number of arguments
			public string			str1;		// argument 1, if string, KEY|TIMER|SOUND|CHIP8|SCHIP
			public ARG_TYPE			type1;		// argument 1 type
			public ARG_TYPE			type2;
			public ARG_TYPE			type3;
			public int				code;		// base hex code
			public ASM_MNEM() { id=OP_ID.ID_INVALID_CODE; instr=""; args=0; str1=""; code=0x0000; 
				type1=ARG_TYPE.NONE; type2=ARG_TYPE.NONE; type3=ARG_TYPE.NONE; }
		}


		private static ASM_MNEM[] asm_mnems = {
			new ASM_MNEM{ id=OP_ID.ID_QUIT,						instr="QUIT",	code=0x00FD,	args=0	},	// QUIT		00FD
			new ASM_MNEM{ id=OP_ID.ID_CLEAR_SCREEN,				instr="CLS",	code=0x00E0,	args=0	},	// CLS		00E0
			new ASM_MNEM{ id=OP_ID.ID_RETURN,					instr="RET",	code=0x00EE,	args=0	},	// RET		00EE
			new ASM_MNEM{ id=OP_ID.ID_RETURN,					instr="RTS",	code=0x00EE,	args=0	},	// RET		00EE

			new ASM_MNEM{ id=OP_ID.ID_MODE_CHIP8_GRAPHICS,		instr="MODE",	code=0x00FE,	args=1,	str1="64X32"	},	// MODE 64x32		00FE
			new ASM_MNEM{ id=OP_ID.ID_MODE_SCHIP_GRAPHICS,		instr="MODE",	code=0x00FF,	args=1,	str1="128X64"	},	// MODE 128x64		00FF

			// new ASM_MNEM{ id=OP_ID.ID_SCROLL_DOWN_N,			instr="SCRL",	code=0x00C0,	args=2, str1="DOWN",			type2=ARG_TYPE.N		},	// SCRL DOWN n		00Cn
			new ASM_MNEM{ id=OP_ID.ID_SCROLL_DOWN_N,			instr="SCRL",	code=0x00C0,	args=2, str1="DOWN",			type2=ARG_TYPE.N		},	// SCRL DOWN n		00Cn
			new ASM_MNEM{ id=OP_ID.ID_SCROLL_LEFT,				instr="SCRL",	code=0x00FC,	args=1, str1="LEFT"										},	// SCRL LEFT		00FC
			new ASM_MNEM{ id=OP_ID.ID_SCROLL_RIGHT,				instr="SCRL",	code=0x00FB,	args=1, str1="RIGHT"									},	// SCRL RIGHT		00FB

			new ASM_MNEM{ id=OP_ID.ID_DRAW_X_Y_N,				instr="DRAW",	code=0xD000,	args=3, type1=ARG_TYPE.VARX,	type2=ARG_TYPE.VARY,	type3=ARG_TYPE.N	},	// DRAW Vx Vy n		Dxyn

			new ASM_MNEM{ id=OP_ID.ID_JUMP_V0_NNN,				instr="JMP",	code=0xB000,	args=2, str1="V0",				type2=ARG_TYPE.NNN		},	// JUMP V0 nnn		Bnnn
			new ASM_MNEM{ id=OP_ID.ID_JUMP_NNN,					instr="JMP",	code=0x1000,	args=1, type1=ARG_TYPE.NNN								},	// JUMP nnn			1nnn

			new ASM_MNEM{ id=OP_ID.ID_JUMP_V0_NNN,				instr="JUMP",	code=0xB000,	args=2, str1="V0",				type2=ARG_TYPE.NNN		},	// JUMP V0 nnn		Bnnn
			new ASM_MNEM{ id=OP_ID.ID_JUMP_NNN,					instr="JUMP",	code=0x1000,	args=1, type1=ARG_TYPE.NNN								},	// JUMP nnn			1nnn

			new ASM_MNEM{ id=OP_ID.ID_CALL_NNN,					instr="JSR",	code=0x2000,	args=1,	type1=ARG_TYPE.NNN								},	// CALL nnn			2nnn
			new ASM_MNEM{ id=OP_ID.ID_CALL_NNN,					instr="CALL",	code=0x2000,	args=1,	type1=ARG_TYPE.NNN								},	// CALL nnn			2nnn

			new ASM_MNEM{ id=OP_ID.ID_SKIP_IF_KEY_EQ_X,			instr="SKEQ",	code=0xE09E,	args=2,	str1="KEY",				type2=ARG_TYPE.VARX		},	// SKEQ KEY Vx		Ex9E
			new ASM_MNEM{ id=OP_ID.ID_SKIP_IF_X_EQ_Y,			instr="SKEQ",	code=0x5000,	args=2,	type1=ARG_TYPE.VARX,	type2=ARG_TYPE.VARY		},	// SKEQ Vx Vy		5xy0
			new ASM_MNEM{ id=OP_ID.ID_SKIP_IF_X_EQ_NN,			instr="SKEQ",	code=0x3000,	args=2,	type1=ARG_TYPE.VARX,	type2=ARG_TYPE.NN		},	// SKEQ Vx nn		3xnn

			new ASM_MNEM{ id=OP_ID.ID_SKIP_IF_KEY_NEQ_X,		instr="SKNE",	code=0xE0A1,	args=2,	str1="KEY",				type2=ARG_TYPE.VARX		},	// SKNE KEY Vx		ExA1
			new ASM_MNEM{ id=OP_ID.ID_SKIP_IF_X_NEQ_Y,			instr="SKNE",	code=0x9000,	args=2, type1=ARG_TYPE.VARX,	type2=ARG_TYPE.VARY		},	// SKNE Vx Vy		9xy0
			new ASM_MNEM{ id=OP_ID.ID_SKIP_IF_X_NEQ_NN,			instr="SKNE",	code=0x4000,	args=2, type1=ARG_TYPE.VARX,	type2=ARG_TYPE.NN		},	// SKNE Vx nn		4xnn

			new ASM_MNEM{ id=OP_ID.ID_WAIT_KEY_X,				instr="WAIT",	code=0xF00A,	args=2,	str1="KEY",				type2=ARG_TYPE.VARX		},	// WAIT KEY, Vx		Fx0A
			
			new ASM_MNEM{ id=OP_ID.ID_COPY_X_NN,				instr="LD",		code=0x6000,	args=2,	type1=ARG_TYPE.VARX,	type2=ARG_TYPE.NN		},	// LET  Vx, nn		6xnn
			new ASM_MNEM{ id=OP_ID.ID_COPY_IX_NNN,				instr="LD",		code=0xA000,	args=2,	str1="IX",				type2=ARG_TYPE.NNN		},	// LET  IX, nnn		Annn

			new ASM_MNEM{ id=OP_ID.ID_COPY_X_Y,					instr="CP",		code=0x8000,	args=2,	type1=ARG_TYPE.VARX,	type2=ARG_TYPE.VARY		},	// COPY Vx, Vy		8xy0
			new ASM_MNEM{ id=OP_ID.ID_COPY_X_NN,				instr="CP",		code=0x6000,	args=2,	type1=ARG_TYPE.VARX,	type2=ARG_TYPE.NN		},	// LET  Vx, nn		6xnn
			new ASM_MNEM{ id=OP_ID.ID_COPY_IX_NNN,				instr="CP",		code=0xA000,	args=2,	str1="IX",				type2=ARG_TYPE.NNN		},	// LET  IX, nnn		Annn

			new ASM_MNEM{ id=OP_ID.ID_ADD_IX_X,					instr="ADD",	code=0xF01E,	args=2,	str1="IX",				type2=ARG_TYPE.VARX		},	// ADD  IX, Vx		Fx1E
			new ASM_MNEM{ id=OP_ID.ID_ADD_X_Y,					instr="ADD",	code=0x8004,	args=2,	type1=ARG_TYPE.VARX,	type2=ARG_TYPE.VARY		},	// ADD  Vx, Vy		8xy4
			new ASM_MNEM{ id=OP_ID.ID_ADD_X_NN,					instr="ADD",	code=0x7000,	args=2, type1=ARG_TYPE.VARX,	type2=ARG_TYPE.NN		},	// ADD  Vx, nn		7xnn

			new ASM_MNEM{ id=OP_ID.ID_SUB_X_Y,					instr="SUB",	code=0x8005,	args=2,	type1=ARG_TYPE.VARX,	type2=ARG_TYPE.VARY		},	// SUB  Vx, Vy		8xy5
			new ASM_MNEM{ id=OP_ID.ID_NEGATIVE_SUB_X_Y,			instr="NSUB",	code=0x8007,	args=2,	type1=ARG_TYPE.VARX,	type2=ARG_TYPE.VARY		},	// NSUB Vx, Vy		8xy7
			new ASM_MNEM{ id=OP_ID.ID_AND_X_Y,					instr="AND",	code=0x8002,	args=2,	type1=ARG_TYPE.VARX,	type2=ARG_TYPE.VARY		},	// AND  Vx, Vy		8xy2
			new ASM_MNEM{ id=OP_ID.ID_OR_X_Y,					instr="OR",		code=0x8001,	args=2,	type1=ARG_TYPE.VARX,	type2=ARG_TYPE.VARY		},	// OR   Vx, Vy		8xy1
			new ASM_MNEM{ id=OP_ID.ID_XOR_X_Y,					instr="XOR",	code=0x8003,	args=2,	type1=ARG_TYPE.VARX,	type2=ARG_TYPE.VARY		},	// XOR  Vx, Vy		8xy3
			new ASM_MNEM{ id=OP_ID.ID_SHIFT_RIGHT_X,			instr="SHR",	code=0x8006,	args=1,	type1=ARG_TYPE.VARX								},	// SHR  Vx			8xy6
			new ASM_MNEM{ id=OP_ID.ID_SHIFT_LEFT_X,				instr="SHL",	code=0x800E,	args=1,	type1=ARG_TYPE.VARX								},	// SHL  Vx			8xyE
			new ASM_MNEM{ id=OP_ID.ID_RANDOM_X_AND_NN,			instr="RAND",	code=0xC000,	args=2,	type1=ARG_TYPE.VARX,	type2=ARG_TYPE.NN		},	// RAND Vx, nn		Cxnn

			new ASM_MNEM{ id=OP_ID.ID_GET_FONT_IX_X,			instr="FONT",	code=0xF029,	args=2,	str1="IX",				type2=ARG_TYPE.VARX		},	// FONT IX, Vx		Fx29

			new ASM_MNEM{ id=OP_ID.ID_SET_TIMER_X,				instr="SET",	code=0xF015,	args=2,	str1="TIMER",			type2=ARG_TYPE.VARX		},	// SET  TIMER, Vx	Fx15
			new ASM_MNEM{ id=OP_ID.ID_GET_TIMER_X,				instr="GET",	code=0xF007,	args=2,	str1="TIMER",			type2=ARG_TYPE.VARX		},	// GET	 TIMER, Vx	Fx07
			new ASM_MNEM{ id=OP_ID.ID_SET_SOUND_X,				instr="SET",	code=0xF018,	args=2,	str1="SOUND",			type2=ARG_TYPE.VARX		},	// SET  SOUND, Vx	Fx18	

			new ASM_MNEM{ id=OP_ID.ID_STORE_BCD_IX_X,			instr="BCD",	code=0xF033,	args=2,	str1="IX",				type2=ARG_TYPE.VARX		},	// BCD  IX, Vx		Fx33
			new ASM_MNEM{ id=OP_ID.ID_STORE_IX_X,				instr="STO",	code=0xF055,	args=2,	str1="IX",				type2=ARG_TYPE.VARX		},	// STO  IX, Vx		Fx55
			new ASM_MNEM{ id=OP_ID.ID_RECALL_IX_X,				instr="RCL",	code=0xF065,	args=2,	str1="IX",				type2=ARG_TYPE.VARX		},	// RCL  IX, Vx		Fx65
			new ASM_MNEM{ id=OP_ID.ID_SAVE_HP_X,				instr="SAVE",	code=0xF075,	args=1,	type1=ARG_TYPE.VARX								},	// SAVE Vx			Fx75
			new ASM_MNEM{ id=OP_ID.ID_LOAD_HP_X,				instr="LOAD",	code=0xF085,	args=1,	type1=ARG_TYPE.VARX								},	// LOAD Vx			Fx85
		};

		public static List<string> keywords = new List<string>() {
			"IX", "KEY", "TIMER", "SOUND"
		};

		public class LabelDef {
			// so the difference between struct and class is that struct is passed by value, while class is passed by reference
			// change all my structs to class to avoid the confusion and problems i've been having
			public bool		resolved;
			public int		addr;
			public LabelDef() { resolved=false; addr=0; }
		};

		// public static Dictionary<string,int> labels = new System.Collections.Generic.Dictionary<string,int>();
		public static Dictionary<string,LabelDef> labels = new Dictionary<string,LabelDef>(); // key=label name


		public static void ClearLabels()
		{
			labels = new Dictionary<string, LabelDef>();
		}

		public static int[] CodifyLine(string[] items)
		{
			// go through line, and if it has ':' separate as individual codes
			// ':' has to be by itself, if ';' follows text, it's a label
			int start_pos = 0;
			bool end_search = false;

			List<int> ret = new List<int>();

			// the whole loop could probably be made into a single foreach(term) thing
			// add to a dynamic list
			// if hit by ; or : eval current list, and set it to blank
			// if found ",", just split and add each sub-item

			while(start_pos<items.Length && end_search==false)
			{
				List<string> asmlist = new List<string>();

				int item_ix;
				for(item_ix=start_pos; item_ix<items.Length; item_ix++)
				{
					// has to be a separate term
					if(items[item_ix]==INSTR_SEP){
						break;
					} else if(items[item_ix]==COMMENT) {	// do last instruction, but skip rest of line
						end_search = true;
						break;
					} else if(items[item_ix]==ARG_SEP || items[item_ix].Trim()=="") {
						// do nothing, don't want to add this to the list, skip
					} else if(items[item_ix].Contains(ARG_SEP)){
						// situations:
						// mnem a , b		=> mnem a b // taken care by previous if, just ignore the item
						// mnem a,b			=> mnem a b
						// mnem a ,b		=> mnem a b
						// mnem a, b		=> mnem a b
						// also: mnem a,b,c => mnem a b c
						// so best approach: split by "," and add every item
						// so a,b => (a b)		,b=>( b)		a,=>(a )		a,b,c=>(a b c)

						string[] subterms = items[item_ix].Split(ARG_SEP);
						foreach(string subterm in subterms)
							if(subterm.Trim()!="")
								asmlist.Add(subterm);
					} else {
						asmlist.Add(items[item_ix]);
					}

				}
				start_pos = item_ix+1;

				string[] assarr = asmlist.ToArray();
				int? op = Coder.Codify1(assarr);

				if(op is null)
					break;
				else
					ret.Add((int)op!);
			}

			return ret.ToArray<int>();
		}


		// new coder
		public static int? Codify1(string[] terms)
		{
			int? ret = null;

			Term term1 = new Term();
			Term term2 = new Term();
			Term term3 = new Term();

			if(terms.Length>1) 		term1 = EvalTerm(terms[1]);
			if(terms.Length>2)		term2 = EvalTerm(terms[2]);
			if(terms.Length>3)		term3 = EvalTerm(terms[3]);

			if(terms.Length>0)
			{
				// loop through mnem array until successful match
				for(int i=0; i<asm_mnems.Length; i++)
				{
					ASM_MNEM mnem = asm_mnems[i];

					// if successful match, break loop
					if(terms[0].ToUpper()==mnem.instr)
					{ // match instruction mnemonic, now check if args, str1, etc matches
						if(terms.Length>mnem.args) // ex: QUIT, args=0, length=1, so length>args
						{ // have enough parameters, confirm the other args

							int code = mnem.code;		// work code
							bool match = true; // default to ok

							if(mnem.args>0) // check arg1, either: str1<>"" or type1=VARX | NN | NNN
							{
								// if mnem.str1<>"": means the modifier is needed for match, if not match, set arg1=false
								if(mnem.str1!="" && mnem.str1==term1.asStr)					match = true;						// no change to code
								else if(mnem.type1==ARG_TYPE.VARX && term1.isVar) 			code = code | (term1.asInt<<8);		// .x..
								else if(mnem.type1==ARG_TYPE.NN && term1.isNum)				code = code | (term1.asInt & 0xFF);	// ..nn
								else if(mnem.type1==ARG_TYPE.NNN && term1.isNum)			code = code | (term1.asInt & 0xFFF);// .nnn
								else														match = false;
							}

							if(mnem.args>1) // check arg2, either: type2 = VARX | VARY | N | NN | NNN
							{
								if(mnem.type2==ARG_TYPE.VARX && term2.isVar)				code = code | (term2.asInt<<8);		// .x..
								else if(mnem.type2==ARG_TYPE.VARY && term2.isVar)			code = code | (term2.asInt<<4);		// ..y.
								else if(mnem.type2==ARG_TYPE.N && term2.isNum)				code = code | (term2.asInt & 0xF);	// ...n
								else if(mnem.type2==ARG_TYPE.NN && term2.isNum)				code = code | (term2.asInt & 0xFF);	// ..nn
								else if(mnem.type2==ARG_TYPE.NNN && term2.isNum)			code = code | (term2.asInt & 0xFFF);// .nnn
								else														match = false;
							}

							if(mnem.args>2) // check arg3, only one option now: type3 = N
							{
								if(mnem.type3==ARG_TYPE.N && term3.isNum)					code = code | (term3.asInt & 0xF);	// ...n
								else														match = false;
							}
								
							if(match)
							{
								ret = code;
								break;			// end loop
							}
						}
					}
				}
			}

			// syntax error has to be handled by caller instead
			// if(ret is null)
			// 	Console.WriteLine($"SYNTAX ERROR [{String.Join(" ", terms)}]");

			return ret;
		}
	
		// add labels/variables, defined with "=" and ":"
		// 
		public class Term {
			public string	asStr;		// to upper
			public bool		isVar;		// Vx
			public bool		isNum;		// asInt parsed ok
			public int		asInt;		// #num is dec, %num is bin, num is hex, 'ch is ascii, Vn is var hex
			public bool		isKeyword;	// IX, KEY, TIMER, SOUND
			public Term()	{ asStr=""; asInt=0; isVar=false; isNum=false; isKeyword=false; }
		}

		public static Term EvalTerm(string term)
		{
			// remove any "," before or after

			Term ret = new Term(){
				asStr = term.ToUpper(),
				asInt = 0,
				isVar = false,
				isNum = false,
				isKeyword = false,
			};

			// I need to add recognition of reserved keywords, IX, TIMER, SOUND, KEY are like Vx
			// return as string, or rather Term should have "isKeyword"

			try {
				ret.isNum = true; // assume it worked

				char first = ret.asStr[0];
				char second = ' ';
				char last = ' ';
				if(ret.asStr.Length>1)
				{
					second = ret.asStr[1];
					last = ret.asStr[ret.asStr.Length-1];
				}

				// %5, v5										register
				// 'a'											ascii value
				// 0b1001, 1001b, 0b10010110, 0b1001_0110		binary
				// 0hC8, 0xC8, 0C8h, $C8						hex
				// 0d45, 045d, 45								decimal
				// $											current address
				// $$											segment start
				// id											label reference
				//
				// future: eval +, -, *, /, needs a higher level eval function
				// evalexpression: term op term op ...

				// skip $, $$, id for now, needs variable definition system
				// 1. reg
				// 2. char
				// 3. hex
				// 4. bin
				// 5. default for num, test

				// could make a evalterm class array:
				//		first, second, last match
				//		range, start, from end
				//		base
				keywords.Add("test");

				if(keywords.Contains(ret.asStr)) {						// a keyword
					ret.isKeyword = true;
					ret.isNum = false;
				} else if(first=='%' || first=='V') {							// register %hexnum
					ret.asInt = Convert.ToUInt16(term[1..], 16);
					ret.isVar = true;
				} else if(first=='\'') {								// char 'a'
					ret.asInt = (byte)term[2];
					ret.isNum = true;
				} else if(first=='0' && second=='B') {					// 0B...
					ret.asInt = Convert.ToUInt16(term[2..], 2);
					ret.isNum = true;
				} else if((first=='0'||first=='1') && last=='B') {		// 0..B or 1..B
					ret.asInt = Convert.ToUInt16(term[0..^1], 2);
					ret.isNum = true;
				} else if(first=='$' && term.Length>1) {				// $..
					ret.asInt = Convert.ToUInt16(term[1..], 16);
					ret.isNum = true;
				} else if((first=='0')&&(second=='H'||second=='X')) {	// 0H.. or 0X..
					ret.asInt = Convert.ToUInt16(term[2..], 16);
					ret.isNum = true;
				} else if(char.IsLetterOrDigit(first) && last=='H') {	// 0..H
					ret.asInt = Convert.ToUInt16(term[..^1], 16);
					ret.isNum = true;
				} else if(first=='0' && second=='D') {					// 0D..
					ret.asInt = Convert.ToUInt16(term[2..]);
					ret.isNum = true;
				} else if(char.IsDigit(first) && last=='D') {			// 0..D
					ret.asInt = Convert.ToUInt16(term[..^1]);
					ret.isNum = true;
				} else if(char.IsDigit(first)) {						// <digit>..
					ret.asInt = Convert.ToUInt16(term);
					ret.isNum = true;
				} else {
					ret.isNum = false;
					// label
					if(LabelDefined(ret.asStr)) { // exists and is defined
						ret.asInt = GetLabel(ret.asStr).addr;
						ret.isNum = true;
					} else { // if not add a dummy label, and return false
						CreateLabel(ret.asStr);
					}					
				}
 			} catch(Exception) { 
				ret.isNum = false;
			};

			return ret;
		}


		public static bool LabelExists(string name)
		{
			return labels.ContainsKey(name);
		}

		public static bool LabelDefined(string name)
		{
			bool ret = false;
			if(LabelExists(name)) {
				ret = labels[name].resolved;
			}
			return ret;
		}

		public static void CreateLabel(string name)
		{ // create a label without definition, for forward references
			if(!LabelExists(name))
				labels.Add(name, new LabelDef());
		}

		public static void SetLabel(string name, string def)
		{
			Term term = EvalTerm(def);

			if(term.isNum) {
				if(labels.ContainsKey(name)) {
					labels[name].resolved = true;
					labels[name].addr = term.asInt;

				} else {
					LabelDef item = new LabelDef();
					item.resolved = true;
					item.addr = term.asInt;
					labels.Add(name, item);
				}
			} else {
				Console.WriteLine("Invalid definition");
			}
		}


		public static LabelDef GetLabel(string name)
		{
			LabelDef ret = new LabelDef(); // empty definition
			if(labels.ContainsKey(name))
				ret = labels[name];
			return ret;
		}

		public static void PrintLabel(string name)
		{
			Console.Write($"{name} = ");
			if(LabelDefined(name))
				Console.WriteLine($"{labels[name].addr:X4}");
			else
				Console.WriteLine("Undefined");
		}

		public static void PrintLabels()
		{
			Console.WriteLine("Labels");
			foreach(string name in labels.Keys) {
				PrintLabel(name);
			}
		}

		public static string FindLabelForAddress(int addr)
		{
			// return labels.First(x => (x.Value.addr==addr)).Key;
			string ret = "";
			foreach(KeyValuePair<string,LabelDef> keyval in labels) {
				if(keyval.Value.addr==addr) {
					ret = keyval.Key;
					break;
				}
			}
			return ret;
		}

	}
}


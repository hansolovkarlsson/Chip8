
using System;
using System.Globalization;


namespace Chip8
{
	class Debugger
	{
		private const string DEFAULT_BIN_FILE = "default.bin";
		private const string DEFAULT_ASM_FILE = "default.asm";

		private string version = "Chip8 Debugger 1.0";
		private string helptext =
			"Debugger commands:\n" +
			"A [<addr> [<asm> : ..]]    Add code or asm line, ':' to separate multiple\n"+
			"B <addr>                   Break point, no addr=list, addr=on/off\n"+
			"C                          Code Help, assembly code\n"+
			"D [id [value]]             Define label or constant\n"+
			"E [<addr> [<val>..]]       Edit hex code, two char=byte, four char=word size\n"+
			"F [<file>]                 Show/set current file name\n"+
			"G <addr>                   Go, start execution, no addr=0x0200\n"+
			"H                          Print help\n"+
			"I <filename>               Import assembly file and compile\n"+
			"J                          ---\n"+
			"K key=c8_key               Map keys (should be saved to a config file as well)\n"+
			"L [<file> [<start>]]       Load binary code\n"+
			"M [<addr> [<addr>]]        Dump memory as hex code\n"+                          
			"N [<file>]                 New\n"+
			"O [file]                   Output disassembly to file\n"+
			"P [<addr> [<addr>]]        Print disassembly, no addr=0x0200-end\n"+
			"Q                          Quit\n"+
			"R [<reg> [<val>]]          Show Register data and stack, <reg>:edit\n"+
			"S [<file> [<from> [<to>]]] Save binary to file\n"+
			"T <mode>                   Trace mode, step, trace\n"+
			"U                          ---\n"+
			"V                          Print version\n"+
			"W <reg>                    Watch register\n"+
			"X [<asm> : ..]             Assemble and execute instruction directly, ':' for multiple operations\n"+
			"Y                          ---\n"+
			"Z [<addr>]                 Show/Set address of program end\n";

			// step, back, over, etc in trace mode only, not command line
			// lable, define, etc pass on to compiler/assembler, not in debugger

		private string helptext_asm =
			"ASSEMBLY CODE MNEMONICS\n"+
			"QUIT|CLS|RET\n"+
			"DRAW           Vx Vy n\n"+
			"JUMP           [V0] nnn\n"+
			"CALL           nnn\n"+
			"WAIT           KEY Vx\n"+
			"RAND           Vx nn\n"+
			"CP             IX nnn | Vx nn\n"+
			"ADD            IX Vx | Vx Vy | Vx nn\n"+
			"SET            TIMER Vx | SOUND Vx\n"+
			"GET            TIMER Vx\n"+
			"MODE           64x32|128x64\n"+
			"SCRL           LEFT | RIGHT | DOWN n\n"+
			"SKEQ|SKNE      KEY Vx | Vx Vy | Vx nn\n"+
			"FONT|BCD|STO|RCL          IX Vx\n"+
			"SHL|SHR|SAVE|LOAD         Vx\n"+
			"CP|SUB|NSUB|AND|OR|XOR    Vx Vy\n";

		private bool quit;
		private int prog_end_addr;
		private int last_dump_addr;
		string current_filename;

		private Emulator emu;

		public void Start()
		{
			Console.WriteLine($"{version}");
			quit = false;
			string cmdline = "";
			prog_end_addr = Emulator.PROG_START;
			last_dump_addr = Emulator.PROG_START;
			current_filename = DEFAULT_BIN_FILE;

			emu = new Emulator();

			try
			{
				while(!quit)
				{
					cmdline = ReadLine();
					EvalLine(cmdline);
				}
			}
			catch (Exception e)
			{
				Console.WriteLine($"\n*** ERROR! ***\n{e.ToString()}");
			}
		}

		private string ReadLine()
		{
			string? cmdline = "";
			while(string.IsNullOrEmpty(cmdline))
			{
				Console.Write("> ");
				// read line and trim leading/trailing spaces
				cmdline = Console.ReadLine()!.Trim();
			}
			return cmdline;
		}

		private void EvalLine(string cmdline)
		{
			string[] cmds = cmdline!.Split(" ");

			switch(cmds[0].ToUpper())
			{
				case "A":	AssembleInsert(cmds);					break;
				case "B":	BreakPoint(cmds);						break;
				case "C":	Console.Write(helptext_asm);			break;
				case "D":	DefineLabel(cmds);						break;
				case "E":	EditMemory(cmds);						break;
				case "F":	FileName(cmds);							break;
				case "G":	Go(cmds);								break;
				case "H":	Console.Write(helptext);				break;
				case "I":	ImportAndCompileFile(cmds);				break;
				// case "J":	break; // ???
				case "K":	KeyMapping(cmds);						break;
				case "L":	LoadFile(cmds);							break;
				case "M":	MemoryDump(cmds);						break;
				case "N":	NewFile(cmds);							break;
				case "O":	DisassembleToFile(cmds);				break;
				case "P":	Disassemble(cmds);						break;
				case "Q":	quit=true;								break;
				case "R":	Registers(cmds);						break;
				case "S":	SaveFile(cmds);							break;
				case "T":	TraceMode(cmds);						break;
				// case "U":	break; // ???
				case "V":	Console.WriteLine(version);				break;
				case "W":	WatchRegister(cmds);					break;
				case "X":	ExecuteCode(cmds);						break;
				// case "Y":	break; // ???
				case "Z":	SetProgramEnd(cmds);					break;
				default:	Console.WriteLine("Invalid Command");	break;
			}
		}

		private void ImportAndCompileFile(string[] cmds)
		{
			prog_end_addr = Assembler.AssembleFile(cmds[1], emu.mem);
		}

		private void TraceMode(string[] cmds){ Console.WriteLine("-Not defined"); }
		private void Go(string[] cmds){ Console.WriteLine("-Not defined"); }
		private void BreakPoint(string[] cmds){ Console.WriteLine("-Not defined"); }
		private void WatchRegister(string[] cmds){ Console.WriteLine("-Not defined"); }
		private void KeyMapping(string[] cmds){ Console.WriteLine("-Not defined"); }

		private void DefineLabel(string[] cmds)
		{
			// D				print list of labels
			// D <id>			print value of label
			// D <id> <val>		assign value to label, val can be number, $, or expression(later)

			string	name = "";

			if(cmds.Length>2) { // ID VAL		define
				Coder.SetLabel(cmds[1].ToUpper(), cmds[2]);
			} else if (cmds.Length>1) { // ID
				name = cmds[1].ToUpper(); // not doing case sensitive
				// Console.WriteLine($"{id} = {Coder.GetLabel(id):X4}");
				Coder.PrintLabel(name);
			} else { // show all
				Coder.PrintLabels();
			}

		}

		private void NewFile(string[] cmds)
		{
			emu.Init();
			prog_end_addr	= Emulator.PROG_START;
			last_dump_addr	= Emulator.PROG_START;
			if(cmds.Length>1)
				current_filename = cmds[1];
		}

		private void LoadFile(string[] cmds)
		{
			string filename = current_filename;
			int start_addr = Emulator.PROG_START;

			if(cmds.Length>1) 
				filename = cmds[1];
			if(cmds.Length>2) // start address
				start_addr = int.Parse(cmds[2], NumberStyles.HexNumber);

			current_filename = filename;
			last_dump_addr = start_addr;

			int size = emu.LoadFile(filename, start_addr);
			Console.WriteLine($"Loaded file {filename} size:{size:X4} address:{start_addr:X4}-{start_addr+size:X4}");

			prog_end_addr = start_addr+size;
		}

		private void MemoryDump(string[] cmds)
		{
			/*	D addr addr				dump from-to address
			//	D addr					dump from address, and 0x0100 bytes forward
			//	D						dump 0x200-0x2FF
			*/
			int from = last_dump_addr;	// 0x200;
			int to = from + 0xFF;

			if(cmds.Length>1)
			{
				from = int.Parse(cmds[1], NumberStyles.HexNumber) & 0xFFF0;
				to = from + 0xFF;
			}
			if(cmds.Length>2)
				to = int.Parse(cmds[2], NumberStyles.HexNumber) | 0x000F;
		
			last_dump_addr = to + 1;
			string chars = "";

			Console.Write($"Dump memory {from:X4}-{to:X4}");
			for(int i=from; i<=to; i+=2)
			{
				if(i%16==0)
				{
					Console.WriteLine($"\t{chars}");
					Console.Write($"{i:X4}:\t");
					chars = "";
				}
				int word = (emu.mem[i]<<8)|(emu.mem[i+1]&0xFF);
				Console.Write($"{word:X4} ");
				char ch = (char)emu.mem[i];
				if(char.IsControl(ch)) chars += ".";
				else chars += ch;
				ch = (char)emu.mem[i+1];
				if(char.IsControl(ch)) chars += ".";
				else chars += ch;
			}
			Console.WriteLine($"\t{chars}");
		}

		private void Registers(string[] cmds)
		{
			/* R <reg> <val>			set val to register
			// R <reg>					will ask for value, enter leaves reg unchanged
			// R						dump registers
			*/
			if(cmds.Length>1) // r reg
			{	// Edit register
				string? valstr = "";
				if(cmds.Length>2)
				{	// r reg val
					valstr = cmds[2];
				}
				else
				{
					// would be nice to print register name and current value %%%
					Console.Write($"{cmds[1].ToUpper()}:");
					switch(cmds[1].ToUpper())
					{
						case "IP":	Console.Write($"{emu.IP:X4}"); break;
						case "OP":	Console.Write($"{emu.OP:X4}"); break;
						case "IX":	Console.Write($"{emu.IX:X4}"); break;
						case "SP":	Console.Write($"{emu.SP:X2}"); break;
						default:
							int reg;
							if(cmds[1].Substring(0,1).ToUpper()=="V")
								reg = int.Parse(cmds[1].Substring(1,1), NumberStyles.HexNumber);
							else
								reg = int.Parse(cmds[1], NumberStyles.HexNumber);
							Console.Write($"{emu.VAR[reg]:X2}");
							break;
					}
					Console.Write("=");
					valstr = Console.ReadLine();
				}

				if(!string.IsNullOrEmpty(valstr))
				{
					int val = int.Parse(valstr!, NumberStyles.HexNumber);
					switch(cmds[1].ToUpper())
					{
						case "IP":	emu.IP = val;	break;
						case "OP":	emu.OP = val;	break;
						case "IX":	emu.IX = val;	break;
						case "SP":	emu.SP = val;	break;
						default: // assume a number
							int reg;
							if(cmds[1].Substring(0,1).ToUpper()=="V")
								reg = int.Parse(cmds[1].Substring(1,1), NumberStyles.HexNumber);
							else
								reg = int.Parse(cmds[1], NumberStyles.HexNumber);
							emu.VAR[reg] = (byte)(val & 0xFF);
							break;
					}
				}
			}

			Console.WriteLine($"IP:{emu.IP:X4}\tOP:{emu.OP:X4}\tIX:{emu.IX:X4}\tSP:{emu.SP:X2}");
			for(int i=0; i<16; i++)
				Console.Write($"V{i:X} ");
			Console.WriteLine();
			for(int i=0; i<16; i++)
				Console.Write($"{emu.VAR[i]:X2} ");
			Console.WriteLine();
			Console.Write("Stack:");
			for(int i=0; i<emu.SP; i++)
			{
				if(i%16==0)
					Console.WriteLine();
				Console.Write($"{emu.stack[i]:X4} ");
			}
			Console.WriteLine();
		}

		private void EditMemory(string[] cmds)
		{
			/* E <addr> <word>			change one place
			// E <addr>					inputs one line at a time to change
			//			addr: _
			//			<enter> leaves the edit
			//			hex numbers only
			// E						edit without address, will ask for address
			// Start: ____
			// ffff: __
			//
			// E <a> <w> <w> ...
			*/

			// improve by breaking out the data string/array evaluation

			string? addrstr = "";
			if(cmds.Length>1) // address on command line
			{
				addrstr = cmds[1];
			} else {
				Console.Write("Address: ");
				addrstr = Console.ReadLine();
			}

			if(!string.IsNullOrEmpty(addrstr))
			{
				int addr = int.Parse(addrstr, NumberStyles.HexNumber);
				// 2 different input, command line sequence, or input from keyboard until empty
				if(cmds.Length>2)
				{ // values on command line
					string[] substr = cmds[2..]; // oh wow. this works!
					addr = InsertData(addr, substr);
				} else { // ask for values from keyboard
					string? valstr = "";
					do {
						Console.Write($"{addr:X4}:\t");
						valstr = Console.ReadLine();
						if(!string.IsNullOrEmpty(valstr))
						{
							string[] vals = valstr!.Split(" ");
							addr = InsertData(addr, vals);
						}
					} while (!string.IsNullOrEmpty(valstr));
				}
				if(addr>prog_end_addr)
					prog_end_addr = addr;
			}
		}

		// return end_addr
		// %%% change to use EvalTerm in coder class
		private int InsertData(int start_addr, string[] data)
		{
			int addr = start_addr; // return after loop
			int val;

			/* data:
			// 1234			word size hex
			// 12			byte size hex
			// 'abc			ascii characters
			// "str ing"	string with spaces
			// %11101		binary
			// #123			decimal
			*/
			Console.Write($"{addr:X4}:\t");
			for(int i=0; i<data.Length; i++)
			{
				switch(data[i][0])
				{
					case '\'':
						for(int j=1; j<data[i].Length; j++)
						{
							Console.Write($"{data[i][j]:X2} ");
							emu.mem[addr] = (byte)(data[i][j]);
							addr+=1;
						}
						break;
					case '"':
						// add characters until ending \"
						// later fix %%%
						// assemble the string first, then loop through and store

						for(int j=1; j<data[i].Length; j++)
						{
							Console.Write($"{data[i][j]:X2} ");
							emu.mem[addr] = (byte)(data[i][j]);
							addr+=1;
						}
						break;
					case '#':
						val = int.Parse(data[i][1..]);
						Console.Write($"{val:X2} ");
						emu.mem[addr] = (byte)(val & 0xFF);
						addr+=1;
						break;
					case '%':
						val = Convert.ToInt16( data[i][1..], 2);
						Console.Write($"{val:X2} ");
						break;
					default:
						val = int.Parse(data[i], NumberStyles.HexNumber);
						if(data[i].Length<3) // byte
						{
							Console.Write($"{val:X2} ");
							emu.mem[addr] = (byte)(val & 0xFF);
							addr+=1;
						} else { // word
							Console.Write($"{val:X4} ");
							emu.mem[addr] = (byte)(val>>8);
							emu.mem[addr+1] = (byte)(val & 0xFF);
							addr+=2;
						}
						break;
				}
			}
			Console.WriteLine();
			return addr;
		}
	
		private void SaveFile(string[] cmds)
		{
			string filename = current_filename;
			int start_addr = Emulator.PROG_START;
			int end_addr = prog_end_addr;

			if(cmds.Length>1) 
				filename = cmds[1];
			if(cmds.Length>2) // start address
				start_addr = int.Parse(cmds[2], NumberStyles.HexNumber);
			if(cmds.Length>3) // end address
				end_addr = int.Parse(cmds[3], NumberStyles.HexNumber);

			if(end_addr<start_addr)
			{
				Console.WriteLine($"Bad address range: {start_addr:X4}-{end_addr:X4}");
			} else {
				int size = emu.SaveFile(filename, start_addr, end_addr);
				Console.WriteLine($"Saved file {filename} size:{size:X4} address:{start_addr:X4}-{end_addr:X4}");
			}
		}
	
		private void AssembleInsert(string[] cmds)
		{
			// a addr mnem p1 p2 p3

			int addr = prog_end_addr;
			if(addr<Emulator.PROG_START)
				addr = Emulator.PROG_START;
			if(cmds.Length>1)
			{
				try { addr = int.Parse(cmds[1], NumberStyles.HexNumber); }
				catch (Exception) {	Console.WriteLine($"Bad address format'{cmds[1]}'"); }
			}

			if(cmds.Length>2)
			{
				// command line
				string[] line = cmds[2..];
				int[] codes = Coder.CodifyLine(line);		// ':' separates commands

				Console.Write($"{addr:X4}:\t");
				for(int i=0; i<codes.Length; i++)
				{
					Console.Write($"{codes[i]:X4} ");
					emu.mem[addr] = (byte)(codes[i]>>8);
					emu.mem[addr+1] = (byte)(codes[i] & 0xFF);
					addr+=2;
				}
				Console.WriteLine();
			} else {
				// input loop until empty
				string? instr = "";
				do {
					Console.Write($"{addr:X4}? ");
					instr = Console.ReadLine();
					if(!string.IsNullOrEmpty(instr)) {
						string[] line = instr.Split(" ");
						int[] codes = Coder.CodifyLine(line);

						Console.Write($"{addr:X4}:\t");
						for(int i=0; i<codes.Length; i++)
						{
							Console.Write($"{codes[i]:X4} ");
							emu.mem[addr] = (byte)(codes[i]>>8);
							emu.mem[addr+1] = (byte)(codes[i] & 0xFF);
							addr+=2;
						}
						Console.WriteLine();
					}
				} while (!string.IsNullOrEmpty(instr));
			}
			if(addr>prog_end_addr)
				prog_end_addr = addr;
		}

		private void ExecuteCode(string[] cmds)
		{
			// , mnem p1 p2 p3 ; mnem2 ...

			if(cmds.Length>1)
			{
				// command line
				string[] line = cmds[1..];
				int[] codes = Coder.CodifyLine(line);		// ':' separates commands
				for(int i=0; i<codes.Length; i++)
					Console.Write($"{codes[i]:X4} ");
				Console.WriteLine();
			} else {
				// input loop until empty
				string? instr = "";
				do {
					Console.Write("? ");
					instr = Console.ReadLine();
					if(!string.IsNullOrEmpty(instr)) {
						string[] line = instr.Split(" ");
						int[] codes = Coder.CodifyLine(line);
						for(int i=0; i<codes.Length; i++)
							Console.Write($"{codes[i]:X4} ");
						Console.WriteLine();
					}
				} while (!string.IsNullOrEmpty(instr));
			}
		}

		private void SetProgramEnd(string[] cmds)
		{
			if(cmds.Length>1) // ? <addr>
				prog_end_addr = int.Parse(cmds[1], NumberStyles.HexNumber);
			Console.WriteLine($"Program End {prog_end_addr:X4}");
		}

		private void FileName(string[] cmds)
		{
			if(cmds.Length>1) // F <filename>
				current_filename = cmds[1];
			Console.WriteLine($"Current file name: {current_filename}");
		}

		private string DisassembleToString(int start, int end)
		{
			string ret = $"Disassembly Address:{start:X4}-{end:X4}\n";
			// for(int addr=start; addr<end; addr+=2)
			// {
			// 	int code = (emu.mem[addr]<<8)|(emu.mem[addr+1]);
			// 	string str = Disassembler.Disassemble1(code);
			// 	ret += $"{addr:X4}: {code:X4}\t{str}\n";
			// }
			Range r = new Range(start, end);
			byte[] code_block = emu.mem[r];
			ret += Disassembler.Disassemble(code_block, start);
			return ret;
		}

		private void Disassemble(string[] cmds)
		{
			int start = Emulator.PROG_START;
			int end = prog_end_addr;

			if(cmds.Length>1)
				start = int.Parse(cmds[1], NumberStyles.HexNumber);
			if(cmds.Length>2)
				end = int.Parse(cmds[2], NumberStyles.HexNumber);

			Console.WriteLine(DisassembleToString(start, end));
		}

		private void DisassembleToFile(string[] cmds)
		{
			FileInfo fi = new FileInfo(current_filename);
			string ext = fi.Extension;

			string filename = current_filename.Substring(0, current_filename.Length-ext.Length)+".asm";

			if(cmds.Length>1)
				filename = cmds[1];

			File.WriteAllText(filename, DisassembleToString(Emulator.PROG_START, prog_end_addr));
		}

	}
}


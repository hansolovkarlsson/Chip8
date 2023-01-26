using System;

/*
	Still to do
	.label id num			-- not needed, use .def for this. can labels be defined as a string though?
	.def id str
	'string'
	$ and $$
	expression like $ - msg
	.import file addr
*/

namespace Chip8
{
	class Assembler
	{
		// This is the full assembler function to compile a file to object code

		/*
			https://www.tutorialspoint.com/assembly_programming/assembly_basic_syntax.htm

			;				comment (different from debugger)

			terms/values
			$				current address
			$$				start of segment, 0200h in code block, <end of code block> in data block
			45				decimal number, 45d, 0d200
			0x45			hex, 0c8h, $0c8, 0xc8, 0hc8
			0b1000_1010		bin, 1001b, 1110_1100b
			'string'
			label			use label reference, creates a stub if it's missing	

			.include "file"
			label:			define label
			.data			compiler will put this after code section
			.code			compiler will put this at the top of memory

			.data
				msg		db	"Hello word", 0xa	; db, dw
				len		equ $ - msg
				num		equ 8 + 3 * 4	; +-/* only, space required, no priority, all just sequence
			
				choice	db 'y'	; ascii

				marks	times 9 dw '*'

				var		resb 3
				var2	resw 9

			
			.org 200h
			.data
				msg db 'Hello world$'
			.code
			start:
				...
			end start

			.import "binfile"
			.label name def ; useful for relative definitions


			Assembly procedure:
			Read file to List<CodeLine> and List<DataLine>
			"." imperatives always execute immediately, no store in lists
			CodeLine { LabelStr, AddrDone, Addr, CodeDone, Code (2B), Text }
			DataLine { LabelStr, AddrDone, Addr, CodeDone, Code (256B), Text }

			List<LabelDef>
			LabelDef { NameStr, Resolved, Addr }
			The labels should be in Coder.cs already, but is missing the "resolved" flag
			Resolved is used for forward reference, like:
			JMP my_label			; creates a label { "my_label", resolved=false, addr=0000h }
			my_label:				; updates label { "my_label", resolved=true, addr=<whatever current address>}

			List<MacroDef>
			MacroDef { NameStr, DefStr }
			macros right now are nothing but a definition, like CRLF='\n' stuff, replacing text
			Can be used for:
				.def	spr_x	VA
				.def	spr_y	VB
				LD spr_x, 10h
				LD spr_y, 05h


			Eval Code list first, and resolve code, but if refer to undefined label, leave CodeDone=false
			Populate List<LabelDef> while resolving
			Then eval data list, resolve addresses and data array (max 256 bytes)
			Continue to populate label list
			Re-eval code and data now with resolved labels. (data since there could be a label for later data reference)
				.data
					msg		"Hello", crlf
				.def crlf 0Ah, 0Dh

			If a line reference a unresolved label, error

		*/

		static int current_addr = 0;

		enum MODE {
			NONE,
			CODE_MODE,
			DATA_MODE
		}

		static MODE current_mode = MODE.NONE;

		class CodeLine {
			public int? addr = null;
			public int? code = null;		// set to null if not resolved
			public string line;
			public CodeLine(string str) { addr=null; code=null; line=str; }
		}

		static List<CodeLine> codeLines;

		class DataLine {
			public bool isEqu = false;			// for "id equ num", has no address, has no data
			public int? addr = null;
			public byte[] data; // assign during eval
			public string line;
			public DataLine(string str) { addr=null; line=str; isEqu=false; }
		}

		static List<DataLine> dataLines;

		// return last address
		public static int AssembleFile(string filename, byte[] mem)
		{
			// reset all collections first
			int ret = 0;

			// dump file name = filename, but with "lst" as extension
			int pos = filename.LastIndexOf('.');
			string dumpfile = filename[0..pos] + ".lst";

			Coder.ClearLabels();
			codeLines = new List<CodeLine>();
			dataLines = new List<DataLine>();

			// step 1:
			//		read all lines
			//		separate into data and code lists
			//		eval and execute dot-directives
			//		for code block
			//			do a preliminiary evaluation of asm, and assing addresses
			//			assign labels
			IncludeFile(filename);

			// 2. evaluate data list, assign addresses, assign labels
			EvalDataList();

			// 3. 2nd pass, go through and re-eval anything that wasn't complete (potentially including data section)
			ReEvalCodeList();

			// 4. store to memory
			ret = StoreToMemory(mem);

			// dump
			DumpResult(dumpfile);

			return ret;
		}

		private static void DumpResult(string filename)
		{
			string text = "";

			foreach(CodeLine line in codeLines)
			{
				if(line.addr is null)
					text += "????: ";
				else {
					string label = Coder.FindLabelForAddress((int)line.addr!);
					if(label!="")
						text += $"{label}:\n";
					text += $"{line.addr!:X4}: ";
				}

				if(line.code is null)
					text += "????    ";
				else
					text += $"{line.code!:X4}    ";
				text += $"{line.line}\n";
			}

			foreach(DataLine line in dataLines)
			{
				if(line.isEqu) {
					text += $"{line.line}\n";
				} else {
					if(line.addr is null)
						text += "????:";
					else {
						string label = Coder.FindLabelForAddress((int)line.addr!);
						if(label!="")
							text += $"{label}:\n";
						text += $"{line.addr:X4}:";
					}

					foreach(byte b in line.data)
						text += $" {b:X2}";
					
					text += $"\n              {line.line}\n";
				}

			}

			// Console.WriteLine(text);
			File.WriteAllText(filename, text);

			// Coder.PrintLabels();

		}

		public static void IncludeFile(string filename)
		{
			Console.WriteLine($"Include file: {filename}");
			string[] lines = File.ReadAllLines(filename);
				
			foreach(string line in lines) {
				// Console.WriteLine($"line={line}");
				// check if line starts with ';' or '.'
				string trimline = line.Trim();
				if(trimline!="")
					switch(trimline[0]) {
						case ';': 	break;	// comment line, skip

						case '.':	EvalDirective(trimline);
									break;	// directive, send to assembly eval

						default:	if(current_mode==MODE.DATA_MODE)
										dataLines.Add(new DataLine(trimline));
									else if(current_mode==MODE.CODE_MODE)
										EvalCodeLine(trimline);
									break;
					}				
			}
		}

		private static void EvalDirective(string line)
		{
			/* 
				.org <num>
				.include "file"
				.import "file" addr			; import binary to specified address, good for library functions, or "BIOS"
				.def id def
				.label id def
				.data
				.code
				.echo
			*/
			// replace \t with space
			string[] terms = line.Trim().Replace('\t', ' ').Split(" ", StringSplitOptions.RemoveEmptyEntries);

			switch(terms[0].ToUpper()) {
				case ".ORG":		current_addr = Coder.EvalTerm(terms[1]).asInt;
									break;

				case ".INCLUDE":	if(terms[1].StartsWith('"'))
										IncludeFile(terms[1].Substring(1,terms[1].Length-1));
									else
										IncludeFile(terms[1]);
									break;

				case ".IMPORT":		break; // make ImportFile() that loads code to codeLines
				case ".DEF":		break; // a definition is a macro (simple version now, token replacement only)
				case ".LABEL":		break; // a label is only a name for an address (int)
				case ".ECHO":		break; // echo text during assembly

				case ".DATA":		current_mode = MODE.DATA_MODE;							break;
				case ".CODE":		current_mode = MODE.CODE_MODE;							break;
				default:			Console.WriteLine($"Unknown directive {terms[0]}");		break;
			}
		}

		private static void EvalCodeLine(string line)
		{
			string[] terms = line.Trim().Replace('\t',' ').Replace(',', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries);

			// extract potential label first
			if(terms[0].EndsWith(':')) { // label
				string label = terms[0].TrimEnd(':');
				Coder.SetLabel(label.ToUpper(), Convert.ToString(current_addr));
				terms = terms[1..];
			}

			// remove comment
			// int pos = Array.IndexOf(terms, ";"); // won't pick up ";text"
			for(int pos=0; pos<terms.Length; pos++) {
				if(terms[pos].StartsWith(';')) {
					terms = terms[0..pos];
					break;
				}
			}
			

			if(terms.Length>0 && !terms[0].StartsWith(';')) { // still have items, so not just a label or empty line
				int? code = Coder.Codify1(terms);
			
				CodeLine codeline = new CodeLine(String.Join(' ', terms));
				codeline.addr = current_addr;
				current_addr += 2;
				codeline.code = code;
				codeLines.Add(codeline);
			}
		}

		private static void ReEvalCodeList()
		{
			foreach(CodeLine line in codeLines) {
				if(line.code is null) {
					string[] terms = line.line.Split(' ');
					int? code = Coder.Codify1(terms);
					line.code = code;
				}
			}
		}

		private static void EvalDataList()
		{
			/* format:
				<line>		::= <data byte> | <data word> | <constant> | <repeat> | <reserve byte> | <reserve word>
				<data byte>	::=	[label] db <exp list>
				<data word> ::= [label] dw <exp list>
				<constant>	::= <label> equ <expression>
				<repeat>	::= times <exp> <data byte>|<data word>
				<res byte>	::= <label> resb <expression>
				<res word>	::= <label> resw <expression>

				<exp list>::= <exp>, <exp> ...

				[label]		[db <exp list>|dw <exp list>|equ|times|resb|rews] <expression>
				msg		db	"Hello word", 0xa			; store data, byte size, and multiple
				msg		dw	8000h						; store word size data
				choice	db 'y'							; ascii, same as "y"

				marks	times 9 dw '*'					; repeat, like 9 dup dw '*'

				var		resb 3
				var2	resw 9

				len		equ $ - msg						; creates a constant, not store in memory, $=current address
				num		equ 8 + 3 * 4					; expression +-/* only, space required, no priority, all just sequence	

			*/

			foreach(DataLine dataline in dataLines) {
				// for each new data line, adjust to even address
				if((current_addr & 0x01)==0x01) // add position
					current_addr++;
				dataline.addr = current_addr;

				// check for label
				// check code word (db, dw, times, resb, resw, equ)
				// read and eval terms (not doing expression yet, and when I do, only in equ to make it easy)
				// watch out for ';'

				string line = dataline.line;

				// split line first
				string[] terms = line.Trim().Replace('\t',' ').Replace(',', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries);

				string label = "";

				// remove comment
				// int pos = Array.IndexOf(terms, ";"); // won't pick up ";text"
				for(int pos=0; pos<terms.Length; pos++) {
					if(terms[pos].StartsWith(';')) {
						terms = terms[0..pos];
						break;
					}
				}


				// flaw: equ is not an address label. if equ is found, change value of label
				// actually equ is more like def, i might have to do some redesign to get it right and some testing
				List<string> keywords = new List<string>() { "DB", "DW", "TIMES", "RESB", "RESW", "EQU" };
				if(!keywords.Contains(terms[0].ToUpper())) { // assume label
					label = terms[0].TrimEnd(':').ToUpper(); // remove any ':'
					Coder.SetLabel(label, Convert.ToString(current_addr));
					terms = terms[1..]; // remove label from terms
				}

				List<byte> byteList = new List<byte>();
				int item_ix = 0; // index for loop through terms
				while(item_ix<terms.Length) {
					// check if term is ';', then end loop
					if(terms[item_ix].StartsWith(';')) {
						item_ix = terms.Length;
						break;
					}

					// every keyword changes the index
					switch(terms[item_ix].ToUpper()) {
						case "BYTES":
						case "DB":		// db <num list>
										terms[item_ix] = "BYTES"; // replace keyword
										item_ix++;
										byteList.AddRange(ExtractByteData(terms[item_ix..]));

										// skip rest of line
										item_ix = terms.Length;

										break; // loop through items and store bytes

						case "WORDS":
						case "DW":		// dw <num_list>
										terms[item_ix] = "WORDS"; // replace keyword
										item_ix++;
										byteList.AddRange(ExtractWordData(terms[item_ix..]));

										item_ix = terms.Length;// skip rest of line

										break;

						case "TIMES":	// times <num> db|dw <num list>
										// for this to work, I need a EvalData function that reads the DB/DW part
										// and returns the array/list of values
										item_ix++;
										byteList.AddRange(ExtractDupData(terms[item_ix..]));
										item_ix = terms.Length; // end loop
										break;

						case "RESB":	// resb <num>
										item_ix++;
										Coder.Term resb = Coder.EvalTerm(terms[item_ix]);
										if(resb.isNum)
											for(int i=0; i<resb.asInt; i++)
												byteList.Add(0x00);
										item_ix = terms.Length;
										break;

						case "RESW":	// resw <num>
										item_ix++;
										Coder.Term resw = Coder.EvalTerm(terms[item_ix]);
										if(resw.isNum)
											for(int i=0; i<resw.asInt; i++) {
												byteList.Add(0x00);
												byteList.Add(0x00);
											}
										item_ix = terms.Length;
										break;

						case "EQU": 	// label equ <num>		; change value of label to result from equation
										item_ix++;
										Coder.Term equ = Coder.EvalTerm(terms[item_ix]);
										if(equ.isNum)
											Coder.SetLabel(label, Convert.ToString(equ.asInt));
										item_ix = terms.Length;
										dataline.isEqu = true;
										dataline.line = $"{label} EQU {equ.asInt:X4}";
										break;

						default:		break; // a term, interpret, and depending on mode, deal with it
					}
				}

				if(!dataline.isEqu) {
					dataline.line = String.Join(' ', terms);
					dataline.data = byteList.ToArray();
					current_addr += dataline.data.Length;
				}

			}
		}

		private static List<byte> ExtractDupData(string[] terms)
		{
			List<byte> ret = new List<byte>();

			Coder.Term num = Coder.EvalTerm(terms[0]);
			if(num.isNum) { // number of times
				List<byte> data = new List<byte>(); // empty list if switch-extract fails
				switch(terms[1].ToUpper()) {
					case "BYTES":
					case "DB":	data = ExtractByteData(terms[2..]);
								break;

					case "WORDS":
					case "DW":	data = ExtractWordData(terms[2..]);
								break;

					default: break;
				}
				for(int i=0; i<num.asInt; i++)
					ret.AddRange(data);
			}

			return ret;
		}

		private static List<byte> ExtractByteData(string[] terms)
		{
			List<byte> ret = new List<byte>();

			// I can test for " for strings here too. Later feature
			// or create a new DSTR

			foreach(string term in terms) {
				Coder.Term val = Coder.EvalTerm(term);
				if(val.isNum)
					ret.Add((byte)(val.asInt & 0xFF));
				else
					break;
			}

			return ret;
		}

		private static List<byte> ExtractWordData(string[] terms)
		{
			List<byte> ret = new List<byte>();

			foreach(string term in terms) {
				Coder.Term val = Coder.EvalTerm(term);
				if(val.isNum) {
					ret.Add((byte)((val.asInt>>8) & 0xFF));
					ret.Add((byte)(val.asInt & 0xFF));
				} else
					break;
			}

			return ret;
		}


		// return highest address
		private static int StoreToMemory(byte[] mem)
		{
			int ret = 0;

			foreach(CodeLine line in codeLines)
			{
				if(line.addr is null)
					Console.WriteLine($"ERROR: Missing address for {line.line}");
				else if(line.code is null)
					Console.WriteLine($"ERROR: Missing code for {line.line}");
				else {
					int addr = (int)line.addr!;
					int code = (int)line.code!;
					mem[addr] = (byte)(code>>8);
					mem[addr+1] = (byte)(code & 0xFF);
					if(addr>=ret) ret=addr+1;
				}
			}

			foreach(DataLine line in dataLines)
			{
				if(!line.isEqu) {
					if(line.addr is null)
						Console.WriteLine($"ERROR: Missing address for {line.line}");
					else if (line.data.Length==0)
						Console.WriteLine($"ERROR: No data for line {line.line}");
					else {
						int addr = (int)line.addr!;
						foreach(byte code in line.data) {
							mem[addr] = code;
							addr++;
							if(addr>ret) ret=addr;
						}
					}
				}
			}

			return ret;
		}

    }
}


// See https://aka.ms/new-console-template for more information
// Console.WriteLine("Hello, World!");

using System;

namespace Chip8
{
	/* skip command line options for now
	 * would be something like
	 * -r	load and run
	 * -c	compile
	 * -p	decompile
	 * -d	debug mode
	 * -t	trace mode
	 * -o	output file
	 *
	 * For now, do a debug mode by default
	 *
	 * 1. make basic S-Chip8
	 * 2. add my extensions, Not, Neg, console print/input, full font, ...
	 * 3. make a OS in low mem
	 *
	 *
	 */
    class Chip8
    {
        static int Main(string[] args)
        {
			// Console.WriteLine("Hello word!");
			Debugger dbg = new Debugger();
			dbg.Start();
			return 0;
		}
    }
}


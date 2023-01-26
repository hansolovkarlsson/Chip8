// main.cpp

#include <GL/glut.h>  // GLUT, include glu.h and gl.h
#include <stdio.h>

#include "Screen.cpp"
#include "Chip8.cpp"

#define ROM "roms/test_opcode.ch8"
// #define ROM "roms/PONG.bin"
// #define ROM "roms/TETRIS.bin"
// #define ROM "roms/BC_test.ch8"


// int loop_num = 0;
// int loop_cnt = 0;
void frame()
{
// 	loop_num++;
// 	if(loop_num>=scr_buf_size)
// 	{
// 		loop_num=0;
// 		loop_cnt++;
// 		printf("%d ", loop_cnt);
// 	}
// 	scr_buffer[loop_num] = SCREEN_PIXEL;

// 	if (chip_exec1()==false)
// 		exit(exit_code);
// 	else
//  		glutPostRedisplay();
		// should only do it if scr-buffer has changed
		// the way to do that is to only let screen handle buffer
		// putting pixels etc, make a function, and it'll do the post
		// chip doesn't have to, or loop here doesn't either

	// add soem global variable, refresh_scr, or something, bool
	// set to true whenever a draw function is called in the chip

	if(scr_refresh)
	{
		glutPostRedisplay();
		scr_refresh = false;
	}
}



void timer(int id)
{
// 	printf("***\n");
//	printf("\a");

	// timer id:1 = cpu cycle tick (1000/sec)
	if(id==1) {
		printf("!");
		if (chip_exec1()==false) {
// 			exit(exit_code);
			 glutLeaveMainLoop(); // freeglut extension
		}
		glutTimerFunc(1, timer, 1);				// CPU clock, 1 ms
	} else if(id==2) {
		// timer id 2: delay timer
		printf("*");
		chip_timers_tick();
// 		// should fix so it only restarts timer when DT>0
//  		glutTimerFunc(1000.0/60.0, timer, 2);	// 60/sec timers
		start_delay_timer();
	}

// 	glutPostRedisplay();
// 	glutTimerFunc(1000.0/60.0, timer, 0);
}

void start_delay_timer()
{
	// only restart when DT is set>0
	glutTimerFunc(1000.0/60.0, timer, 2);	// 60/sec timers
}


void key_input(unsigned char key, int x, int y)
{
	KEY = key;
	printf("KEY PRESSED:'%c' %02X\n", KEY, key);
}

void key_release(unsigned char key, int x, int y)
{
	KEY = 0x00;
	printf("KEY RELEASED:'%c' %02X\n", KEY, KEY);
}


void cli_arguments(int argc, char** argv)
{
	// argumnents:
	// <file>		- binary file to run, default load to 0x200

}

int main(int argc, char** argv)
{
	char* file_name = (char*)ROM;

	cli_arguments(argc, argv);

	chip_init();
	if(chip_load_file(file_name)) {
		chip_dump_mem();
		scr_start(argc, argv); // , loop, timer);

		glutIdleFunc(frame); 					// display update, redraw and flush
		glutTimerFunc(1, timer, 1);				// CPU clock, 1 ms
// 		glutTimerFunc(1000.0/60.0, timer, 2);	// 60/sec timers
		glutKeyboardFunc(key_input);
		glutKeyboardUpFunc(key_release);

		scr_refresh =true;

		glutMainLoop();           				// Enter the event-processing loop

	}

	sound_exit();
	return exit_code;
}

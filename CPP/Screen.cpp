// screen.cpp

#include <stdio.h>
// #include <stdlib.h>
#include <GL/freeglut.h>
#include <GL/glut.h>  // GLUT, include glu.h and gl.h

// #include "Screen.h"

// trouble converting the functions to a class, static or otherwise
// because of the function pointers

unsigned short int scr_width;
unsigned short int scr_height;
unsigned short int scr_buf_size;
const unsigned char SCREEN_PIXEL = 0xFF;
unsigned char* scr_buffer;

// origin at the center of the screen
// also, range is -1..1 for x and y
float scr_x_factor;
float scr_y_factor;

float scr_pixel_red;
float scr_pixel_green;
float scr_pixel_blue;

bool scr_refresh = false;


void scr_display() {
	// Clear the color scr_buffer (background)
	glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT | GL_STENCIL_BUFFER_BIT);

	// quads are probably a bit too slow.
	// if I use a premade asset instead, might be faster
	glBegin(GL_QUADS); // 4 vertices form a quad
	glColor3f(scr_pixel_red, scr_pixel_green, scr_pixel_blue);

	for(int i=0; i<scr_buf_size; i++)
		if(scr_buffer[i]!=0)
		{
			int x = i%scr_width-scr_width/2; // -32..31
			int y = i/scr_width-scr_height/2; // -16..15

			float glX = x*scr_x_factor;
			float glY = y*scr_y_factor;

			glVertex2f(glX, 				glY);
			glVertex2f(glX+scr_x_factor,	glY);
			glVertex2f(glX+scr_x_factor,	glY+scr_y_factor);
			glVertex2f(glX,					glY+scr_y_factor);
		}
	glEnd();
	//    	glFlush();  // Render now
	glutSwapBuffers();		// double scr_buffering swap
}


// void scr_idle() {
// 	glutPostRedisplay();
// }

void scr_clear() {
	for(int i=0; i<scr_buf_size; i++)
		scr_buffer[i] = 0;
	glutPostRedisplay();
}

// return true if collision
bool scr_xor_pixel(int x, int y) {
	int pos = x+y*scr_width;
	bool ret = false; // no collision
	if(scr_buffer[pos]!=0) {
		ret = true;
		scr_buffer[pos] = 0;
	} else {
		scr_buffer[x+y*scr_width] = SCREEN_PIXEL;
	}
	// glutPostRedisplay();
	scr_refresh = true;
	return ret;
}

void scr_start(int argc, char** argv) { // , void(*callback)(), void(*timer)(int)) {
	scr_width = 64;
	scr_height = 32;
	scr_buf_size = scr_width * scr_height;

	scr_buffer = new unsigned char[scr_buf_size];

	// (x,y) in screen, origin in top-left corner
	// (x,y) in GL, origin is in center of screen, bottom-left is (-,-), top-right (+,+)
	// scr_y_factor will invert sign for Y axis
	scr_x_factor = 2.0f/scr_width;	// = 0.03125
	scr_y_factor = -2.0f/scr_height;	// y has to be inverted

	scr_pixel_red = 1.0f;
	scr_pixel_green = 1.0f;
	scr_pixel_blue = 0.99f;

	for(int i=0; i<scr_buf_size; i++)
		scr_buffer[i]=0;

	glutInit(&argc, argv);

	// double scr_buffer slows it down... for wahtever reason
	glutInitDisplayMode(GLUT_DOUBLE | GLUT_RGB | GLUT_DEPTH);

	glutCreateWindow("Chip8");
	glutInitWindowSize(scr_width*10, scr_height*10);
	glutInitWindowPosition(50, 50);
	glutReshapeWindow(scr_width*10, scr_height*10);
	glClearColor(0.2f, 0.2f, 0.2f, 1.0f); // Set background color to black and opaque

	glutDisplayFunc(scr_display);

	// glutReshapeFunc(fn)
	// glutKeyboardFunc(fn)
	// glutSpecialFunc(fn)
	// glutMouseFunc(fn)

	// glutIdleFunc(scr_display); // dirty trick to make refresh the idel function
	// 	glutIdleFunc(scr_idle); // more accurate way of doing it

// 	glutIdleFunc(callback); // screen call back
//
// 	glut
// 	glutTimerFunc(1000.0/60.0, timer, 0);	// 60 times a second

// 	glutMainLoop();           				// Enter the event-processing loop
}






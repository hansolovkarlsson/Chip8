/*
 */
// #include <windows.h>  // for MS Windows
#include <stdio.h>
#include <GL/glut.h>  // GLUT, include glu.h and gl.h
//#include <GLUT/glut.h>	// mac?


// Testing program:
// 1. screen buffer
// 2. print from buffer
// 3. move a pixel in the buffer
// 4. dual-buffer tech to avoid blinking etc

// windows size: 640x320. Each "pixel" size:10x10
// pixel color, red for now
// screen buffer, 1 byte per pixel, 0xFF=set

// Create the screen buffer: 64*32 pixels
// Not compressing the pixels into bytes. Each position in array is a pixel
const int res_x = 64; // number of pixels <->
const int res_y = 32;
const char PIXEL = 0xFF;

char screen[res_x*res_y];	// x:64, y:32, formula: pos=x+y*64

int loop = 0;

 
/* Handler for window-repaint event. Call back when the window first appears and
   whenever the window needs to be re-painted. */
void display() {
	glClearColor(0.2f, 0.2f, 0.2f, 1.0f); // Set background color to black and opaque

	// Clear the color buffer (background)
	glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT | GL_STENCIL_BUFFER_BIT);


	screen[loop] = 0;
	loop+=1;
	if(loop>2048)
		loop=0;
	screen[loop] = PIXEL;


	// origin at the center of the screen
	// also, range is -1..1 for x and y
	float factX = 2.0f/62.0f; // = 0.03125
	float factY = -2.0f/32.0f; // y has to be inverted

	glBegin(GL_QUADS); // 4 vertices form a quad
// 	glColor3f(1.0f, 0.0f, 0.0f);	// red
	glColor3f(255.0f/255.0f, 255.0f/255.0f, 240.0f/255.0f);

	for(int i=0; i<2048; i++)
		if(screen[i]!=0)
		{
			// (x,y) in screen, origin in top-left corner
			// (x,y) in GL, origin is in center of screen, bottom-left is (-,-), top-right (+,+)
			// factY will invert sign for Y axis

			int x = i%64-32; // -32..31
			int y = i/64-16; // -16..15

			// x = -1..1
			// x=0 => 0-32 = -32 / 32 = -1
			// x=32 => 32-32 = 0 / 32 = 0
			// x=63 => 63-32 = 31 / 32 = 0.96875
			// x2 = 0.96875 + 0.03125 = 1
			// x1=(x-32)/32
			// x2=(x-32)/32+xfact
			// y1=(y-16)/16
			float glX1 = x*factX;
			float glY1 = y*factY;

			glVertex2f(glX1, 		glY1);
			glVertex2f(glX1+factX,	glY1);
			glVertex2f(glX1+factX,	glY1+factY);
			glVertex2f(glX1,		glY1+factY);
		}
	glEnd();

//    	glFlush();  // Render now
    	glutSwapBuffers();		// double buffering swap
}

void my_loop()
{
//  	for ( int loop = 0; loop<(32*64); loop+=3)
//  		screen[loop] = PIXEL;

// 	screen[10 + 10*64] = PIXEL;

// 	screen[loop] = 0;
	screen[loop] = PIXEL;
	loop+=1;
	if(loop>2048)
		loop=0;

	glutPostRedisplay();
}
 
/* Main function: GLUT runs as a console application starting at main()  */
int main(int argc, char** argv) {
   glutInit(&argc, argv);					// Initialize GLUT

   /* So apparently double buffer has issues, not sure if it's no supported or whatever
	* But, the begin/end for polygons is an old an deprecated function as well
	* And new buffer tech should be used for performance
	* Now, this project doesn't need all fancy stuff, so doing direct drawing is just fine
	* Seems to be fast, and no extra complications
	*/

   // double buffer mode, slow because GPU doesn't support it
//    glutInitDisplayMode(GLUT_DOUBLE);

   glutCreateWindow("Test Chip8 Screen");	// Create a window with the given title
   glutInitWindowSize(640, 320);			// Set the window's initial width & height
   glutInitWindowPosition(50, 50);			// Position the window's initial top-left corner

   glutReshapeWindow(640, 320);				// reformat the actual window

   glutDisplayFunc(display);			// Register display callback handler for window re-paint

   glutIdleFunc(display); // dirty trick to make refresh the idel function
//    glutIdleFunc(my_loop);

   glutMainLoop();           				// Enter the event-processing loop
   return 0;
};


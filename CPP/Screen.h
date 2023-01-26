// Screen.h

extern unsigned short int scr_width;
extern unsigned short int scr_height;
extern unsigned short int scr_buf_size;
extern const unsigned char SCREEN_PIXEL;
extern unsigned char* scr_buffer;

// origin at the center of the screen
// also, range is -1..1 for x and y
extern float scr_x_factor;
extern float scr_y_factor;

extern float scr_pixel_red;
extern float scr_pixel_green;
extern float scr_pixel_blue;


void scr_display();
void scr_idle();
void scr_start(int argc, char** argv, void(*callback)());
void scr_clear();
bool scr_xor_pixel(int x, int y);

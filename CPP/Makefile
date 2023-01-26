

# CC = gcc
CC = g++
CFLAGS =
LFLAGS = -lGL -lGLU -lglut -lalut -lopenal

# gcc -o simplealut simplealut.c `pkg-config --libs freealut`
# math library: -lm
# OBJS = Screen.o Chip8.o Program.o

TARGET = Program testGL testAL
SRCS =  Program.cpp Chip8.cpp Screen.cpp Sound.cpp

all: $(TARGET)

# compile and link in one
%: %.cpp $(SRCS)
	$(CC) $< $(CFLAGS) $(LFLAGS) -o $@

# testGL: testGL.cpp
# 	$(CC) $< $(CFLAGS) $(LFLAGS) -o $@

# # link from obj files to executable
# $(TARGET): $(OBJS)
# 	$(CC) -o $@ $(OBJS) $(LFLAGS)
# # compile to obj files from cpp file
# .cpp.o:
# 	$(CC) -c $< $(CFLAGS)

clean:
	rm -rf *.o $(TARGET)



// alut, OpenAL utility toolkit

#include <AL/al.h>
#include <AL/alc.h>
#include <AL/alut.h>
#include <stdio.h>

// gcc -o simplealut simplealut.c `pkg-config --libs freealut`
//
// #define FILENAME "sounds/file_example_WAV_1MG.wav"
#define FILENAME "sounds/beep-09.wav"

int main(int argc, char **argv)
{

    ALuint buffer, source;
    ALuint state;

    // Initialize the environment
	printf("alutInit\n");
	alutInit(&argc, argv);
//     alutInit(0, NULL);

    // Capture errors
	printf("alGetError\n");
    alGetError();

    // Load pcm data into buffer
// 	printf("alutCreateBufferFromFile(%s)\n", FILENAME);
//     buffer = alutCreateBufferFromFile(FILENAME);
	buffer = alutCreateBufferWaveform(ALUT_WAVEFORM_SINE, 440.0, 10.0, 1.0);

    // Create sound source (use buffer to fill source)
	printf("alGenSources\n");
    alGenSources(1, &source);
	printf("alSourcei\n");
    alSourcei(source, AL_BUFFER, buffer);

    // Play
	printf("alSourcePlay\n");
    alSourcePlay(source);

    // Wait for the song to complete
    do {
		printf("alGetSourcei\n");
        alGetSourcei(source, AL_SOURCE_STATE, (int*)&state);
    } while (state == AL_PLAYING);

    // Clean up sources and buffers
	printf("alDeleteSource\n");
    alDeleteSources(1, &source);
	printf("alDeleteBuffers\n");
    alDeleteBuffers(1, &buffer);

    // Exit everything
	printf("alutExit\n");
    alutExit();

    return 0;
}

// Sound.cpp

#include <AL/al.h>
#include <AL/alc.h>
#include <AL/alut.h>
#include <stdio.h>

ALuint sound_buffer, sound_source, sound_state;
ALfloat sound_pitch;

void sound_init()
{
	alutInit(0, NULL);
	alGetError();

	// sound_pitch = 440.0; // default
	sound_pitch = 880.0;
}

void sound_start(ALfloat dur)
{
	sound_buffer = alutCreateBufferWaveform(ALUT_WAVEFORM_SINE, sound_pitch, 10.0, dur);

    alGenSources(1, &sound_source);
    alSourcei(sound_source, AL_BUFFER, sound_buffer);
    alSourcePlay(sound_source);
}

void sound_check()
{
    // Wait for the song to complete
	alGetSourcei(sound_source, AL_SOURCE_STATE, (int*)&sound_state);

	if(sound_state!=AL_PLAYING) {
		alDeleteSources(1, &sound_source);
		alDeleteBuffers(1, &sound_buffer);
	}
}

void sound_exit()
{
    alutExit();
}

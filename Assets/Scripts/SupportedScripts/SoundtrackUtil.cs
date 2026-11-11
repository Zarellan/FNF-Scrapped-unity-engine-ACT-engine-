using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SoundtrackUtil
{

    public static void PlayMusic(string name)
    {
        GameObject.FindWithTag("SoundTrack").GetComponent<SoundTrackScript>().PlayMusic(name);
    }

    public static void PlayMusic(string name,float bpm)
    {
        GameObject.FindWithTag("SoundTrack").GetComponent<SoundTrackScript>().PlayMusic(name,bpm);
    }
    public static void SetVolume(float num)
    {
        GameObject.FindWithTag("SoundTrack").GetComponent<SoundTrackScript>().SetVolume(num);
    }

    public static void PauseMusic()
    {
        GameObject.FindWithTag("SoundTrack").GetComponent<SoundTrackScript>().PauseMusic();
    }

    public static void UnPauseMusic()
    {
        GameObject.FindWithTag("SoundTrack").GetComponent<SoundTrackScript>().UnPauseMusic();
    }
    public static void StopMusic()
    {
        GameObject.FindWithTag("SoundTrack").GetComponent<SoundTrackScript>().StopMusic();
    }
    public static AudioSource GetAudio()
    {
        return GameObject.FindWithTag("SoundTrack").GetComponent<AudioSource>();
    }

    public static void Time(float time)
    {
        GameObject.FindWithTag("SoundTrack").GetComponent<AudioSource>().time = time;
    } 


    public static float GetBPM()
    {
        return GameObject.FindWithTag("SoundTrack").GetComponent<SoundTrackScript>().GetBPM();
    }

}

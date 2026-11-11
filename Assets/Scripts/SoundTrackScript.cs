using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof(AudioSource))]
public class SoundTrackScript : MonoBehaviour
{
    float bpm; // the fan game depend on BPM so I'll have to use it

    public static SoundTrackScript instance;
    [SerializeField]
    AudioSource audioo;

    [SerializeField]
    AudioClip[] clips;

    // Start is called before the first frame update
    void Start()
    {
        audioo = GetComponent<AudioSource>();

        if (instance != null)
        {
            Destroy(this.gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(this);
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void PlayMusic(string name)
    {
        audioo.volume = 1;
        for (int i = 0;i < clips.Length;i++)
            {
                if (clips[i].name == name)
                {
                    audioo.clip = clips[i];
                    audioo.Play();
                }
            }

        SetBPM(120);
    }

    public void PlayMusic(string name,float bpm)
    {
        SetBPM(bpm);
        for (int i = 0;i < clips.Length;i++)
            {
                if (clips[i].name == name)
                {
                    audioo.clip = clips[i];
                    audioo.Play();
                }
            }
        audioo.volume = 1;
    }


    public void SetVolume(float volume)
    {
        audioo.volume = volume;
    }

    public void StopMusic()
    {
        audioo.Stop();
    }

    public void PauseMusic()
    {
        audioo.Pause();
    }

    public void UnPauseMusic()
    {
        audioo.UnPause();
    }

    public AudioSource GetAudio()
    {
        return GetComponent<AudioSource>();
    }

    public float GetBPM()
    {
        return bpm;
    }

    public void SetBPM(float bpm)
    {
        this.bpm = bpm;
    }

}

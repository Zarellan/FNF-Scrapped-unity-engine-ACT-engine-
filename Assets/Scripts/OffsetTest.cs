using PrimeTween;
using UnityEngine;

public class OffsetTest : MonoBehaviour
{
    [SerializeField] AudioSource song;
    [SerializeField] int stepsPerBeat = 4;
    private int baseRows = 16;
    [SerializeField] float bpm;
    [SerializeField] AudioClip testAud;
    [SerializeField] float msTime;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameHandler.audioOffsetMs = msTime;
        numberBeat = GetCurrentBeat(GameHandler.GetSongTime(song));
    }

    // Update is called once per frame
    void Update()
    {
        GameHandler.audioOffsetMs = msTime;
        PlayOnBeat();
    }
    int numberBeat = -1;
    int numBeat2 = 0;
    void PlayOnBeat()
    {
        if (GetCurrentBeat(GameHandler.GetSongTime(song)) != numberBeat)
        {   
            numBeat2++;
            if (numBeat2 == 2)
                Tween.CameraOrthographicSize(Camera.main,startValue:4.5f,endValue:5,1f,Ease.OutCirc);
            // song.PlayOneShot(testAud);
            numberBeat = GetCurrentBeat(GameHandler.GetSongTime(song));
            print(numBeat2);
            if (numBeat2 == 4)
                numBeat2 = 0;
        }
    }
    float GetBPMForSection(int section)
    {
        return bpm;
    }

    float GetSecondsPerRow(int section)
    {
        float bpm = GetBPMForSection(section);
        return 60f / bpm / stepsPerBeat;
    }

    int GetSectionFromTime(float time)
    {
        float accumTime = 0f;
        int section = 0;

        while (true)
        {
            float bpmForSection = GetBPMForSection(section);
            float secPerRow = 60f / bpmForSection / stepsPerBeat;
            float secDuration = baseRows * secPerRow;

            if (time < accumTime + secDuration)
                break;

            accumTime += secDuration;
            section++;
        }

        return section;
    }


    float GetBPMForTime(float time)
    {
        int section = GetSectionFromTime(time);
        return GetBPMForSection(section);
    }
    int GetCurrentStep(float songTime)
    {
        float bpm = GetBPMForTime(songTime); // your existing BPM fetcher per section
        float secPerBeat = 60f / bpm;
        float secPerStep = secPerBeat / stepsPerBeat; // usually 4 steps per beat
        return Mathf.FloorToInt(songTime / secPerStep);
    }

    // Returns current beat based on song time and BPM
    int GetCurrentBeat(float songTime)
    {
        int curStep = GetCurrentStep(songTime);
        return curStep / stepsPerBeat; // 4 steps = 1 beat
    }


}

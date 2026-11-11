using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System.IO;
using UnityEngine.UI;
using PrimeTween;
using UnityEngine.Networking;

public class FNFNote : MonoBehaviour
{
    public static FNFNote instance;
    [SerializeField] string songName;
    [Header("Note Settings")]
    [SerializeField] public AudioSource song;
    [SerializeField] private AudioSource songOpponent;
    [SerializeField] private AudioSource songPlayer;
    [SerializeField] private GameObject notePrefab;
    [SerializeField] private GameObject susPrefab;
    public Sprite dirEmptyNoteRight, dirEmptyNoteDown, dirEmptyNoteUp, dirEmptyNoteLeft;
    public Sprite dirNoteRight, dirNoteDown, dirNoteUp, dirNoteLeft;
    public Sprite dirNoteRightLight1, dirNoteDownLight1, dirNoteUpLight1, dirNoteLeftLight1;
    public Sprite dirNoteRightLight2, dirNoteDownLight2, dirNoteUpLight2, dirNoteLeftLight2;
    public Sprite dirNoteRightLight3, dirNoteDownLight3, dirNoteUpLight3, dirNoteLeftLight3;

    [SerializeField] Sprite dirStrumRight, dirStrumDown, dirStrumUp, dirStrumLeft;
    [SerializeField] Sprite dirStrumLeftLast, dirStrumDownLast, dirStrumUpLast, dirStrumRightLast;
    [Header("UI References")]
    [SerializeField] private RectTransform spawnLeft, spawnRight, spawnUp, spawnDown;
    [SerializeField] private RectTransform spawnLeftOpponent, spawnRightOpponent, spawnUpOpponent, spawnDownOpponent;
    [SerializeField] private RectTransform hitLeft, hitRight, hitUp, hitDown;
    [SerializeField] private RectTransform hitLeftOpponent, hitRightOpponent, hitUpOpponent, hitDownOpponent;

    [Header("Timing Settings")]
    [SerializeField] private float bpm = 120f;         // Optional, for reference
    [SerializeField] private float scrollSpeed = 300f; // Pixels per second
    [SerializeField] int stepsPerBeat = 4;
    private int baseRows = 16;

    public bool hitSound = false;

    public List<NoteSpawners> notes;
    Tween[] light2;
    Tween[] light3;

    GameObject playerSide;

    //json save
    NoteChart wrap;
    //
    void Awake()
    {
        // string jsonPath = Path.Combine(Application.streamingAssetsPath, "data", songName + "/");
        // wrap = JsonConvertor.LoadJson<NoteChart>("chart", jsonPath);
        string jsonPath = FileLoader.GetStreamingPath("data/" + songName + "/");
        wrap = JsonConvertor.LoadJson<NoteChart>("chart", jsonPath);
        notes = wrap.notes;
        notes.Sort((a, b) => a.noteHitTime.CompareTo(b.noteHitTime));
        StartCoroutine(ChangeSong(wrap));
        // XmlToAtlasBinaryConverter.Convert(
        //     "Images/Characters/BOYFRIEND",
        //     "Images/Characters/BOYFRIEND"
        // );
        //JsonConvertor.SaveJson(wrap, "saved song", true, Application.dataPath + "/jsonSaves/", true);

        // foreach (var note in notes)
        // {
        //     // No conversion needed since JSON is already in seconds
        //     note.hasSpawned = false;
        //     //note.side = "player";
        // }
        if (instance == null)
            instance = this;

        // NoteChart wrap = new NoteChart();
        // wrap.notes = notes;
        // JsonConvertor.SaveJson(wrap, "saved song", true, Application.dataPath + "/jsonSaves/", true);
        light2 = new Tween[4];
        light3 = new Tween[4];


        playerSide = GameObject.FindWithTag("PlayerSide");

        // LazyPlayerLoad();
    }
    void Start()
    {
        // LazyPlayerLoad();
    }
    // void ChangeSong(NoteChart wrap)
    // {
    //     song.clip = Resources.Load<AudioClip>((wrap.songPath==""?"soundtrack/"+songName:wrap.songPath) + "/inst");
    //     songOpponent.clip = Resources.Load<AudioClip>((wrap.songPath==""?"soundtrack/"+songName:wrap.songPath)+"/voices-opponent");
    //     songPlayer.clip = Resources.Load<AudioClip>((wrap.songPath==""?"soundtrack/"+songName:wrap.songPath)+"/voices-player");
    // }
    IEnumerator ChangeSong(NoteChart wrap)
    {
        string baseRelative = wrap.songPath == "" 
            ? "soundtrack/" + songName 
            : wrap.songPath;

        AudioClip inst = null;
        AudioClip opp = null;
        AudioClip player = null;

        Coroutine c1 = StartCoroutine(LoadAudioClipStream(Path.Combine(baseRelative, "inst.ogg"), c => inst = c));
        Coroutine c2 = StartCoroutine(LoadAudioClipStream(Path.Combine(baseRelative, "voices-opponent.ogg"), c => opp = c));
        Coroutine c3 = StartCoroutine(LoadAudioClipStream(Path.Combine(baseRelative, "voices-player.ogg"), c => player = c));

        yield return c1;
        yield return c2;
        yield return c3;

        if (inst == null)
        {
            Debug.LogError("Inst missing!");
            yield break;
        }

        song.clip = inst;
        if (opp != null) songOpponent.clip = opp;
        if (player != null) songPlayer.clip = player;

        PlaySong(39);
    }
    IEnumerator LoadAudioClip(string path, System.Action<AudioClip> onLoaded, bool required = true, bool forSong = true)
    {
        using (UnityWebRequest www =
            UnityWebRequestMultimedia.GetAudioClip(FileLoader.GetStreamingURL(path), GetAudioType(path)))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                if (forSong)
                {
                    if (required)
                        Debug.LogError("inst missing (required): " + www.error + " | " + path);
                    else
                        Debug.LogWarning("voices missing (optional): " + path);
                }
                else
                    Debug.LogError("audio missing: " + www.error + " | " + path);

                onLoaded?.Invoke(null);
                yield break;
            }

            onLoaded?.Invoke(DownloadHandlerAudioClip.GetContent(www));
        }
    }
    IEnumerator LoadAudioClipStream(string path, System.Action<AudioClip> onLoaded)
    {
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(FileLoader.GetStreamingURL(path), GetAudioType(path)))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Failed: " + www.error + " | " + path);
                onLoaded?.Invoke(null);
                yield break;
            }

            var dl = (DownloadHandlerAudioClip)www.downloadHandler;
            dl.streamAudio = true;

            AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
            onLoaded?.Invoke(clip);
        }
    }
    AudioType GetAudioType(string path)
    {
        path = path.ToLower();

        if (path.EndsWith(".ogg")) return AudioType.OGGVORBIS;
        if (path.EndsWith(".wav")) return AudioType.WAV;
        if (path.EndsWith(".mp3")) return AudioType.MPEG;

        return AudioType.UNKNOWN;
    }
    void PlaySong(float time = 0)
    {
        if (time != 0)
        {
            song.time = time;
            songOpponent.time = time;
            songPlayer.time = time;
        }

        song.PlayScheduled(AudioSettings.dspTime + 0.1);
        if (songOpponent.gameObject.activeSelf)
            songOpponent.PlayScheduled(AudioSettings.dspTime + 0.1);
        if (songPlayer.gameObject.activeSelf)
            songPlayer.PlayScheduled(AudioSettings.dspTime + 0.1);

    }
    int nextNoteIndex = 0; // needed for optimization
    float lastSongPitch = 1;
    void Update()
    {
        if (song == null || notes == null) return;

        for (int i = nextNoteIndex; i < notes.Count; i++)
        {
            NoteSpawners noteData = notes[i];
            if (noteData.hasSpawned) continue;

            RectTransform spawn = null, hit = null;
            if (noteData.side == "player")
            {
                switch (noteData.direction)
                {
                    case "right": spawn = spawnRight; hit = hitRight; break;
                    case "left": spawn = spawnLeft; hit = hitLeft; break;
                    case "up": spawn = spawnUp; hit = hitUp; break;
                    case "down": spawn = spawnDown; hit = hitDown; break;
                    default:
                        Debug.LogWarning($"Unknown direction: {noteData.direction}");
                        continue;
                }
            }
            else
            {
                switch (noteData.direction)
                {
                    case "right": spawn = spawnRightOpponent; hit = hitRightOpponent; break;
                    case "left": spawn = spawnLeftOpponent; hit = hitLeftOpponent; break;
                    case "up": spawn = spawnUpOpponent; hit = hitUpOpponent; break;
                    case "down": spawn = spawnDownOpponent; hit = hitDownOpponent; break;
                    default:
                    Debug.LogWarning($"Unknown direction: {noteData.direction}");
                    continue;
                }
            }

            float distance = Vector2.Distance(spawn.anchoredPosition, hit.anchoredPosition);
            if (distance < 0.001f) distance = 0.001f;
            float travelTime = distance / scrollSpeed;

            if (GameHandler.GetSongTime(song) >= noteData.noteHitTime - travelTime)
            {
                SpawnNote(noteData.noteHitTime, travelTime, noteData.direction, noteData.holdLength, noteData.side);
                noteData.hasSpawned = true;
                nextNoteIndex++;
            }

        }
        ButtonPress();
        PlayOnBeat();
        PlayOnSection();
        coolDownSinging -= Time.deltaTime;
        playerSingDuration -= Time.deltaTime;
        playerMissDuration -= Time.deltaTime;

        if (song.pitch != lastSongPitch)
        {
            songOpponent.time = songPlayer.time = song.time;
            songPlayer.pitch = songOpponent.pitch = song.pitch;
            lastSongPitch = song.pitch;
        }
        // PlayerSideSing();
    }
    int numberSection = 0;
    void PlayOnSection()
    {
        if (GetSectionFromTime(GameHandler.GetSongTime(song)) != numberSection)
        {
            Tween.CameraOrthographicSize(Camera.main,startValue:4.5f,endValue:5,1f,Ease.OutCirc);
            numberSection = GetSectionFromTime(GameHandler.GetSongTime(song));
            bpm = GetBPMForSection(numberSection);
        }

    }
    int numberBeat = 0;

    public static float playerSingDuration;
    public static float playerSingDurationMax = 0.6f;
    public static float playerMissDuration;
    public static float playerMissDurationMax = 0.1f;
    void PlayOnBeat()
    {
        if (GetCurrentBeat(GameHandler.GetSongTime(song)) != numberBeat)
        {
            // if (playerSingDuration <= 0 && playerSide.GetComponent<XmlSpriteAnimator>().prefix != playerSide.GetComponent<XmlSpriteAnimator>().FindBySingType("idle").prefix)
                // playerSide.GetComponent<XmlSpriteAnimator>().PlayAnimationSing("idle",true);
            // if (playerSingDuration <= 0 && playerSide.GetComponent<XmlSpriteAnimatorBinary>().prefix != playerSide.GetComponent<XmlSpriteAnimatorBinary>().FindBySingType("idle").prefix)
            //     playerSide.GetComponent<XmlSpriteAnimatorBinary>().PlayAnimationSing("idle",true);
            if (playerSide.TryGetComponent<XmlSpriteAnimatorBinary>(out var binary))
                if (playerSingDuration <= 0 && binary.prefix != binary.FindBySingType("idle").prefix)
                    binary.PlayAnimationSing("idle", true);
            if (playerSide.TryGetComponent<XmlSpriteAnimator>(out var xml))
                if (playerSingDuration <= 0 && xml.prefix != xml.FindBySingType("idle").prefix)
                    xml.PlayAnimationSing("idle", true);

            //playerSide.GetComponent<XmlSpriteAnimator>().PlayAnimation(playerSide.GetComponent<XmlSpriteAnimator>().FindBySingType("idle").prefix, true, playerSide.GetComponent<XmlSpriteAnimator>().FindBySingType("idle").pivotOffset);
            // Tween.CameraOrthographicSize(Camera.main,startValue:4.5f,endValue:5,0.2f,Ease.OutCirc);
            
            numberBeat = GetCurrentBeat(GameHandler.GetSongTime(song));
        }
    }
    public void GoodHit(string direction)
    {
        songPlayer.volume = 1;
    }

    public void MissHit(string direction)
    {
        playerMissDuration = playerMissDurationMax;
        songPlayer.volume = 0;
    }
    public static float coolDownSinging = 0;
    public static float coolDownSingingMax = 0.1f;
    public static string animationNameCheckPlayer = "";
    void LazyPlayerLoad()
    {
        playerSide.GetComponent<XmlSpriteAnimator>().PlayAnimation(playerSide.GetComponent<XmlSpriteAnimator>().FindBySingType("left").prefix, false, playerSide.GetComponent<XmlSpriteAnimator>().FindBySingType("left").pivotOffset);
        playerSide.GetComponent<XmlSpriteAnimator>().PlayAnimation(playerSide.GetComponent<XmlSpriteAnimator>().FindBySingType("down").prefix, false,playerSide.GetComponent<XmlSpriteAnimator>().FindBySingType("down").pivotOffset);
        playerSide.GetComponent<XmlSpriteAnimator>().PlayAnimation(playerSide.GetComponent<XmlSpriteAnimator>().FindBySingType("up").prefix, false, playerSide.GetComponent<XmlSpriteAnimator>().FindBySingType("up").pivotOffset);
        playerSide.GetComponent<XmlSpriteAnimator>().PlayAnimation(playerSide.GetComponent<XmlSpriteAnimator>().FindBySingType("right").prefix, false,playerSide.GetComponent<XmlSpriteAnimator>().FindBySingType("right").pivotOffset);
    }
    void SpawnNote(float hitTime, float travelTime, string direction, float holdLength = 0f, string side = "Opponent")
    {
        // 1. Spawn HEAD note
        GameObject note = Instantiate(notePrefab, transform, false);
        NoteObject noteScript = note.AddComponent<NoteObject>();
    
        RectTransform spawn = null;
        RectTransform hit = null;

        if (side == "player")
        {
            switch (direction)
            {
                case "right": spawn = spawnRight; hit = hitRight; note.GetComponent<Image>().sprite = dirNoteRight; break;
                case "left": spawn = spawnLeft; hit = hitLeft; note.GetComponent<Image>().sprite = dirNoteLeft; break;
                case "up": spawn = spawnUp; hit = hitUp; note.GetComponent<Image>().sprite = dirNoteUp; break;
                case "down": spawn = spawnDown; hit = hitDown; note.GetComponent<Image>().sprite = dirNoteDown; break;
            }
        }
        else
        {
            switch (direction)
            {
                case "right": spawn = spawnRightOpponent; hit = hitRightOpponent; note.GetComponent<Image>().sprite = dirNoteRight; break;
                case "left": spawn = spawnLeftOpponent; hit = hitLeftOpponent; note.GetComponent<Image>().sprite = dirNoteLeft; break;
                case "up": spawn = spawnUpOpponent; hit = hitUpOpponent; note.GetComponent<Image>().sprite = dirNoteUp; break;
                case "down": spawn = spawnDownOpponent; hit = hitDownOpponent; note.GetComponent<Image>().sprite = dirNoteDown; break;
            }
        }

        noteScript.Initialize(song, spawn, hit, hitTime, travelTime, direction, side);
        noteScript.SetScrollSpeed(scrollSpeed);

        float sustainPower = 0;
        switch(GameHandler.forgivenSustainLevel)
        {
            case ForgivenSustainLevel.goMercful: sustainPower = 0.40f;break;
            case ForgivenSustainLevel.forgiven: sustainPower = 0.70f;break;
            case ForgivenSustainLevel.normal: sustainPower = 0.80f;break;
            case ForgivenSustainLevel.strictful: sustainPower = 1.0f;break;
            case ForgivenSustainLevel.I_HATE_YOU: sustainPower = 1.30f;break;

        }
        // 2. Spawn SUSTAIN notes individually (1 per second or per small time step)
        if (holdLength > 0f)
        {
            float interval = 0.05f; // 50ms between sustain ticks (time-based)
            float sustainMultiplier = sustainPower;
            int sustainCount = Mathf.CeilToInt((holdLength * sustainMultiplier) / interval);

            for (int i = 1; i <= sustainCount; i++)
            {
                float sustainHitTime = hitTime + i * interval;

                GameObject sustain = Instantiate(susPrefab, transform, false);
                NoteObject sustainScript = sustain.AddComponent<NoteObject>();

                RectTransform sustainRect = sustain.GetComponent<RectTransform>();

                // Base position
                sustainRect.anchoredPosition = spawn.anchoredPosition;

                // Calculate pixel spacing from time step
                float sustainStepHeight = scrollSpeed * interval;

                Sprite strum = null;
                Sprite strumLast = null;
                switch (direction)
                {
                    case "right": strum = dirStrumRight; strumLast = dirStrumRightLast; break;
                    case "left": strum = dirStrumLeft; strumLast = dirStrumLeftLast; break;
                    case "up": strum = dirStrumUp; strumLast = dirStrumUpLast; break;
                    case "down": strum = dirStrumDown; strumLast = dirStrumDownLast; break;
                }
                sustain.GetComponent<Image>().sprite = strum;
                // Offset the sustain piece by how far it should be at this tick
                if (direction == "up" || direction == "down")
                    sustainRect.anchoredPosition += new Vector2(0, -sustainStepHeight * i);

                sustainRect.sizeDelta = new Vector2(50, (scrollSpeed / 1000) * 50);
                // Flip or last piece logic
                if (i % 2 == 1)
                {
                    sustainRect.localScale = new Vector2(1, -1);
                    sustain.GetComponent<Image>().fillOrigin = 1;
                }
                if (i == sustainCount)
                {
                    sustainRect.localScale = new Vector2(1, 1);
                    sustain.GetComponent<Image>().sprite = strumLast;
                    sustain.GetComponent<Image>().fillOrigin = 0;
                }
                sustainScript.Initialize(song, spawn, hit, sustainHitTime, travelTime, direction, side);
                sustainScript.SetScrollSpeed(scrollSpeed);
                sustainScript.IsSustain();
            }
        }
    }


    public void SetScrollSpeed(float newSpeed)
    {
        scrollSpeed = newSpeed;
    }

    void ButtonPress()
    {
        NoteNormal();
    }
    // bool[] codeToPress = { GameHandler.LeftKeyUp(), GameHandler.LeftKeyUp(), GameHandler.LeftKeyUp(), GameHandler.LeftKeyUp() };
    void NoteNormal()
    {
        // bool[] codeToPress = { GameHandler.LeftKeyUp(), GameHandler.LeftKeyUp(), GameHandler.LeftKeyUp(), GameHandler.LeftKeyUp() };
        Sprite[] emptySprite = { dirEmptyNoteLeft, dirEmptyNoteDown, dirEmptyNoteUp, dirEmptyNoteRight };
        RectTransform[] hits = { hitLeft, hitDown, hitUp, hitRight };

        for (int i = 0; i < 4; i++)
        {
            if (GameHandler.GetKeyUp(GameHandler.keys[i]))
            {
                if (light2[i].isAlive)
                    light2[i].Stop();
                if (light3[i].isAlive)
                    light3[i].Stop();
                hits[i].GetComponent<Image>().sprite = emptySprite[i];
                lightPlayed[i] = false;
            }
        }
    }
    public static int GetIndexByDir(string direction)
    {
        switch (direction)
        {
            case "left": return 0;
            case "down": return 1;
            case "up":  return 2;
            case "right": return 3;
            default: return -1;
        }
    }
    public static bool[] lightPlayed = {false, false, false, false};
    public void Notelight1(string direction, RectTransform hitPoint)
    {
        switch (direction)
        {
            case "left": hitPoint.GetComponent<Image>().sprite = dirNoteLeftLight1;light2[0] = Tween.Delay(0.1f).OnComplete(()=>Notelight2(direction,hitPoint)); break;
            case "down": hitPoint.GetComponent<Image>().sprite = dirNoteDownLight1; light2[1] = Tween.Delay(0.1f).OnComplete(()=>Notelight2(direction,hitPoint));break;
            case "up": hitPoint.GetComponent<Image>().sprite = dirNoteUpLight1; light2[2] = Tween.Delay(0.1f).OnComplete(()=>Notelight2(direction,hitPoint)); break;
            case "right":hitPoint.GetComponent<Image>().sprite = dirNoteRightLight1; light2[3] = Tween.Delay(0.1f).OnComplete(()=>Notelight2(direction,hitPoint)); break;
        }
    }
    public void Notelight2(string direction,RectTransform hitPoint)
    {
        switch (direction)
        {
            case "left": hitPoint.GetComponent<Image>().sprite = dirNoteLeftLight2;light3[0] = Tween.Delay(0.1f).OnComplete(()=>Notelight3(direction,hitPoint)); break;
            case "down": hitPoint.GetComponent<Image>().sprite = dirNoteDownLight2; light3[1] = Tween.Delay(0.1f).OnComplete(()=>Notelight3(direction,hitPoint));break;
            case "up": hitPoint.GetComponent<Image>().sprite = dirNoteUpLight2; light3[2] = Tween.Delay(0.1f).OnComplete(()=>Notelight3(direction,hitPoint)); break;
            case "right":hitPoint.GetComponent<Image>().sprite = dirNoteRightLight2; light3[3] = Tween.Delay(0.1f).OnComplete(()=>Notelight3(direction,hitPoint)); break;
       }
    }
    public void Notelight3(string direction, RectTransform hitPoint)
    {
        switch (direction)
        {
            case "left": hitPoint.GetComponent<Image>().sprite = dirNoteLeftLight3; break;
            case "down": hitPoint.GetComponent<Image>().sprite = dirNoteDownLight3; break;
            case "up": hitPoint.GetComponent<Image>().sprite = dirNoteUpLight3; break;
            case "right": hitPoint.GetComponent<Image>().sprite = dirNoteRightLight3; break;
        }
        
    }

    float GetBPMForSection(int section)
    {
        // if (wrap..TryGetValue(section, out float bpm))
        return wrap.GetBPM(section);
        // return wrap.defaultBPM;
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

// Wrapper for JSON saving
[System.Serializable]
public class NoteSpawners
{
    public string side;
    public string direction;
    public float noteHitTime;
    public float holdLength;
    public int section;
    public int rowIndex;
    public bool hasSpawned;
}
[System.Serializable]
public class SectionAct
{
    public int sectionIndex;
    public float bpm;
}

[System.Serializable]
public class NoteChart
{
    public string songName;
    public float defaultBPM;
    public string songPath;
    public List<NoteSpawners> notes = new List<NoteSpawners>();
    public List<SectionAct> sections = new List<SectionAct>();

    // Helper to get BPM for a section
    public float GetBPM(int sectionIndex)
    {
        var sec = sections.Find(s => s.sectionIndex == sectionIndex);
        return sec != null ? sec.bpm : defaultBPM;
    }

    // Helper to set or update BPM
    public void SetBPM(int sectionIndex, float bpm)
    {
        var sec = sections.Find(s => s.sectionIndex == sectionIndex);
        if (sec != null) sec.bpm = bpm;
        else sections.Add(new SectionAct { sectionIndex = sectionIndex, bpm = bpm });
    }
}

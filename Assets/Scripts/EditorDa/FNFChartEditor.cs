using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.Networking;
using System.IO;

[System.Serializable]
public class NoteData
{
    public enum Owner { Player, Opponent }

    public int column;       // 0=left, 1=down, 2=up, 3=right
    public float time;       // seconds
    public Owner side;       // player or opponent
    public int section;      // section index
    public int rowIndex;     // for editor visualization
    public float holdLength;
}

public class FNFChartEditor : MonoBehaviour//864.3335
{
    [SerializeField] string songName;
    [Header("Grid & Notes")]
    [SerializeField] GameObject gridTilePrefab;
    [SerializeField] GameObject notePrefab;
    [SerializeField] GameObject posChecker;
    [SerializeField] Camera editorCamera;

    [Header("Audio")]
    [SerializeField] AudioSource song;
    [SerializeField] AudioSource songOpponent;
    [SerializeField] AudioSource songPlayer;
    [SerializeField] float bpm = 120f;
    [SerializeField] int stepsPerBeat = 4;
    [SerializeField] Sprite[] noteDirsSprites;
    [SerializeField] private Transform gridParentTransform;
    private Grid grid;
    private List<NoteData> notes = new List<NoteData>();
    private Dictionary<NoteData, GameObject> noteObjects = new Dictionary<NoteData, GameObject>();

    // timing & sections
    private float baseSecondsPerRow;
    private float secondsPerRow;
    private float sectionDuration;

    private bool isScrolling = false;
    private float manualScrollOffset = 0f;

    private int baseRows = 16;
    private int extendedRows = 32;
    private int currentRows;
    private bool isExtended = false;

    private int currentSection = 0;
    private int lastSection = -1;
    private NoteData selectedNote = null;
    [SerializeField] private float defaultBPM = 120f;
    private Dictionary<int, float> bpmChanges = new Dictionary<int, float>();

    [SerializeField] AudioSource hitSound;

    #region "waveform"
    [Header("Waveform (Editor)")]
    [SerializeField] private bool showWaveform = true;
    [SerializeField] private UnityEngine.UI.RawImage waveformRawImageOpponent
    ; // assign in inspector (UI/Canvas -> RawImage)
    [SerializeField] private UnityEngine.UI.RawImage waveformRawImagePlayer; // assign in inspector (UI/Canvas -> RawImage)
    [SerializeField] private Color waveformColorOpponent = new Color(0.05f, 0.8f, 0.25f, 1f);
    [SerializeField] private Color waveformColorPlayer = new Color(0.05f, 0.8f, 0.25f, 1f);
    [SerializeField] private Color waveformBGColor = new Color(0, 0, 0, 0f);
    [Tooltip("How many pixels of waveform per second of audio (higher = higher detail, heavier)")]
    [SerializeField] private int pixelsPerSecond = 100;
    [Tooltip("Height of the waveform texture in pixels")]
    [SerializeField] private int waveformHeight = 128;

    // runtime
    private Texture2D waveformTextureOpponent;
    private Texture2D waveformTexturePlayer;
    private bool waveformReady = false;
    private Coroutine waveformJobOpponent;
    private Coroutine waveformJobPlayer;

    [SerializeField] WaveFormRect[] waveSizes;

    #endregion
    [SerializeField] TMP_Text textTimer;
    [SerializeField] TMP_Text textSection;
    [SerializeField] TMP_Text textStep;
    [SerializeField] TMP_Text textBeat;
    void Start()
    {
        currentRows = baseRows;
        grid = new Grid(8, currentRows, 1f, new Vector3(-7, -4), CreateTile);

        baseSecondsPerRow = 60f / bpm / stepsPerBeat;
        sectionDuration = baseRows * baseSecondsPerRow;
        secondsPerRow = sectionDuration / currentRows;

        if (editorCamera == null)
            editorCamera = Camera.main;

        InitializeWave();
        PlaySong();
        PauseSong();
    }

    void InitializeWave()
    {
        if (showWaveform && waveformRawImageOpponent != null && songOpponent != null && songOpponent.clip != null)
        {
            if (waveformJobOpponent != null) StopCoroutine(waveformJobOpponent);
            waveformJobOpponent = StartCoroutine(GenerateWaveformSection(songOpponent.clip, currentSection,waveformRawImageOpponent,waveformTextureOpponent,waveformColorOpponent));
        }
        if (showWaveform && waveformRawImagePlayer != null && songOpponent != null && songPlayer.clip != null)
        {
            if (waveformJobPlayer != null) StopCoroutine(waveformJobPlayer);
            waveformJobPlayer = StartCoroutine(GenerateWaveformSection(songPlayer.clip, currentSection,waveformRawImagePlayer,waveformTexturePlayer,waveformColorPlayer));
        }

    }
    void Update()
    {
        Vector3 mousePos = editorCamera.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;

        if (Input.GetMouseButtonDown(1))
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0;

            Vector2Int gridPos = grid.GetGridPosition(mouseWorldPos);
            Debug.Log($"Clicked grid cell: {gridPos.x}, {gridPos.y}");
        }

        HandleTileClick(mousePos);
        HandleScrolling();
        HandleGridResize();
        UpdateCameraPosition();
        UpdateSelectedNoteColor();
        posChecker.transform.position = new Vector3(editorCamera.transform.position.x - 4.6f, editorCamera.transform.position.y, 0) + new Vector3(1.63f, 0, 0);

        if (Input.GetKeyDown(KeyCode.S))
            SaveChartToJson();
        if (Input.GetKeyDown(KeyCode.L))
            LoadChartFromJson(songName);

        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            GoToNextSection();
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            GoToPreviousSection();

        if (Input.GetKeyDown(KeyCode.C))
            CopyPreviousSection();
        else if (Input.GetKeyDown(KeyCode.V))
            SwapColumns();

        HandleHoldLengthInput();

        if (Input.GetKeyDown(KeyCode.B))
        {
            bpmChanges[currentSection] = 110;
            baseSecondsPerRow = 60f / bpmChanges[currentSection] / stepsPerBeat;
            sectionDuration = baseRows * baseSecondsPerRow;
            secondsPerRow = sectionDuration / currentRows;
            ShowSection(currentSection);
            LoadNotesOnly();
        }
        textTimer.text = $"{GameHandler.GetSongTime(song):F2}/{song.clip.length}";
        textSection.text = $"Section:{currentSection}";

        textBeat.text = "Beats: " + GetCurrentBeat(GameHandler.GetSongTime(song));
        textStep.text = "Steps: " + GetCurrentStep(GameHandler.GetSongTime(song));

        if (isScrolling)
            PlayOnBeatEditor();
    }

    int defaultBeat = -1;
    int currentBeats;
    void PlayOnBeatEditor()
    {
        if (defaultBeat != GetCurrentBeat(GameHandler.GetSongTime(song)))
        {
            currentBeats = GetCurrentBeat(GameHandler.GetSongTime(song));

            if (currentBeats % 4 == 0 && song.isPlaying)
                GlobalAudio.instance.PlayAudio("bpmHit1");
            else if (currentBeats % 4 != 0 && song.isPlaying)
                GlobalAudio.instance.PlayAudio("bpmHit2");
            defaultBeat = GetCurrentBeat(GameHandler.GetSongTime(song));
        }
    }
    float GetBPMForSection(int section)
    {
        if (bpmChanges.TryGetValue(section, out float bpm))
            return bpm;
        return defaultBPM;
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
    bool IsOutSideTheGrid()
    {
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;

        Vector2Int gridPos = grid.GetGridPosition(mouseWorldPos);

        if (gridPos.x < 0 || gridPos.x > 7 || gridPos.y < 0 || gridPos.y > currentRows - 1)
            return true;

        return false;
    }
    public static NoteData clickedNote;
    void HandleTileClick(Vector3 mousePos)
    {
        if (!Input.GetMouseButtonDown(0)) return;
        bool isFree = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        if (isCollidingNote && clickedNote != null && !isFree && !Input.GetKey(KeyCode.LeftControl))
        {
            if (noteObjects.ContainsKey(clickedNote))
                Destroy(noteObjects[clickedNote]);

            noteObjects.Remove(clickedNote);
            notes.Remove(clickedNote);

            clickedNote = null;
            return;
        }

        if (IsOutSideTheGrid())
            return;
        
        NoteData note = TileToNote(mousePos);

        // Assign section
        note.section = currentSection;

        if (note.section != currentSection) return;

        // Check duplicates
        // NoteData existingNote = notes.Find(n =>
        //     Mathf.Abs(n.time - note.time) < 0.01f &&
        //     n.column == note.column &&
        //     n.side == note.side &&
        //     n.section == currentSection);
        // NoteData existingNote = notes.Find(n =>
        // n.section == currentSection &&
        // n.column == note.column &&
        // n.side == note.side &&
        // Mathf.Abs(n.rowIndex - note.rowIndex) <= 0);
        int targetRowKey = Mathf.RoundToInt((note.time - GetSectionStartTime(currentSection)) / GetSecondsPerRow(currentSection));
        NoteData existingNote = notes.Find(n =>
            n.section == currentSection &&
            n.column == note.column &&
            n.side == note.side &&
            Mathf.RoundToInt((n.time - GetSectionStartTime(n.section)) / GetSecondsPerRow(n.section)) == targetRowKey
        );

        if (clickedNote != null && !isFree && isCollidingNote)
        {
            if (Input.GetKey(KeyCode.LeftControl))
            {
                selectedNote = clickedNote;
            }

        }
        else
        {
            Vector3 pos = NoteTimeToWorldPosition(note);
            GameObject noteObj = Instantiate(notePrefab, pos, Quaternion.identity);
            noteObj.GetComponent<SpriteRenderer>().sprite = noteDirsSprites[note.column];
            noteObj.GetComponent<NoteEditorObject>().noteData = note;
            noteObjects[note] = noteObj;
            notes.Add(note);
            selectedNote = note;
        }

    }
    float GetRowKey(float time, int section)
    {
        float sectionStart = GetSectionStartTime(section);
        float secPerRow = GetSecondsPerRow(section);

        float rawRow = (time - sectionStart) / secPerRow;

        return rawRow * baseRows;
    }
    void UpdateSelectedNoteColor()
    {
        foreach (var kvp in noteObjects)
        {
            GameObject noteObj = kvp.Value;
            SpriteRenderer sr = noteObj.GetComponent<SpriteRenderer>();
            if (sr == null) continue;

            if (kvp.Key == selectedNote)
                sr.color = new Color(1,1,0,sr.color.a);
            else
                sr.color = new Color(1,1,1,sr.color.a);
        }
    }

    void HandleHoldLengthInput()
    {
        if (selectedNote == null) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            selectedNote.holdLength += secondsPerRow;
            UpdateHoldVisual(selectedNote);
            UpdateNotePosition(selectedNote);
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            selectedNote.holdLength = Mathf.Max(0f, selectedNote.holdLength - secondsPerRow);
            UpdateHoldVisual(selectedNote);
            UpdateNotePosition(selectedNote);
        }
    }

    void UpdateNotePosition(NoteData note)
    {
        if (!noteObjects.ContainsKey(note)) return;
        noteObjects[note].transform.position = NoteTimeToWorldPosition(note);
        UpdateHoldVisual(note);
    }

    Vector3 NoteTimeToWorldPosition(NoteData note)
    {
        int visualColumn = (note.side == NoteData.Owner.Player) ? note.column : note.column + 4;

        float sectionStart = GetSectionStartTime(note.section);
        float secPerRow = GetSecondsPerRow(note.section);

        float rowFloat = (note.time - sectionStart) / secPerRow;
        int targetRows = currentRows;
        float scaledRow = rowFloat * targetRows / baseRows;

        float y = grid.originPosition.y + scaledRow * grid.size;
        float x = grid.WorldPosition(visualColumn, 0).x;

        return new Vector3(x, y+0.5f, 0);
    }

    void UpdateHoldVisual(NoteData note)
    {
        if (!noteObjects.ContainsKey(note)) return;

        GameObject visualLen = noteObjects[note].transform.Find("VisualLength")?.gameObject;
        if (visualLen == null) return;

        float rows = note.holdLength / secondsPerRow;
        float height = Mathf.Max(0.1f, rows * grid.size);

        visualLen.transform.localScale = new Vector3(0.5f, height, visualLen.transform.localScale.z);
        visualLen.transform.localPosition = new Vector3(0f, 0, visualLen.transform.localPosition.z);
    }

    void HandleScrolling()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isScrolling = !isScrolling;
            if (song != null)
            {
                if (isScrolling)
                {
                    songOpponent.time = songPlayer.time = song.time;
                    PlaySong();
                }
                else
                    PauseSong();
            }
        }

        float scrollDelta = Input.mouseScrollDelta.y;
        if (scrollDelta != 0f && song != null)
        {
            if (song.isPlaying) PauseSong();
            float songTime = song.time;
            songTime += scrollDelta * secondsPerRow;
            song.time = Mathf.Clamp(songTime, 0f, song.clip.length);
        }
    }

    void PlaySong()
    {
        song.Play();
        songOpponent.Play();
        songPlayer.Play();
    }
    void PauseSong()
    {
        song.Pause();
        songOpponent.Pause();
        songPlayer.Pause();
    }

    void HandleGridResize()
    {
        int maxExtendedRows = baseRows * 5; // maximum extend multiplier

        if (Input.GetKeyDown(KeyCode.X))
        {
            if (!isExtended)
            {
                isExtended = true;
                currentRows = Mathf.Min(extendedRows, maxExtendedRows);
            }
            else if (currentRows < maxExtendedRows)
            {
                currentRows = Mathf.Min(currentRows + baseRows, maxExtendedRows);
            }

            grid.ResizeGrid(8, currentRows, CreateTile);
            secondsPerRow = sectionDuration / currentRows;
            UpdateAllNotePositions();
            ShowSection(currentSection);
        }
        else if (Input.GetKeyDown(KeyCode.Z))
        {
            if (currentRows > baseRows)
            {
                currentRows = Mathf.Max(baseRows, currentRows - baseRows);
                if (currentRows == baseRows) isExtended = false;

                grid.ResizeGrid(8, currentRows, CreateTile);
                secondsPerRow = sectionDuration / currentRows;
                UpdateAllNotePositions();
                ShowSection(currentSection);
            }
        }

        waveformRawImageOpponent.GetComponent<RectTransform>().sizeDelta = new Vector2(waveformRawImageOpponent.GetComponent<RectTransform>().sizeDelta.x, 1728.667f * (currentRows / 16));
        waveformRawImagePlayer.GetComponent<RectTransform>().sizeDelta = new Vector2(waveformRawImagePlayer.GetComponent<RectTransform>().sizeDelta.x,1728.667f * (currentRows / 16));
        // for (int i = 0; i < waveSizes.Length; i++)
        // {
        //     if (waveSizes[i].requiredRow == currentRows)
        //     {
        //         waveformRawImageOpponent.GetComponent<RectTransform>().anchoredPosition = waveSizes[i].position;
        //         waveformRawImageOpponent.GetComponent<RectTransform>().sizeDelta = waveSizes[i].size;
        //         waveformRawImageOpponent.GetComponent<RectTransform>().localScale = waveSizes[i].scale;

        //         waveformRawImagePlayer.GetComponent<RectTransform>().anchoredPosition = waveSizes[i].position;
        //         waveformRawImagePlayer.GetComponent<RectTransform>().sizeDelta = waveSizes[i].size;
        //         waveformRawImagePlayer.GetComponent<RectTransform>().localScale = waveSizes[i].scale;

        //         waveAmplitudeLimit = waveSizes[i].length;
        //     }
        // }
        // InitializeWave();
    }

    void UpdateAllNotePositions()
    {
        foreach (var n in notes)
        {
            if (!noteObjects.ContainsKey(n)) continue;
            noteObjects[n].transform.position = NoteTimeToWorldPosition(n);
            UpdateHoldVisual(n);
        }
    }

    float GetSectionStartTime(int section)
    {
        float time = 0f;
        for (int i = 0; i < section; i++)
        {
            float bpm = GetBPMForSection(i);
            float secPerRow = 60f / bpm / stepsPerBeat;
            float secDuration = baseRows * secPerRow;
            time += secDuration;
        }
        return time;
    }

    void UpdateCameraPosition()
    {
        if (song == null) return;

        int section = GetSectionFromTime(GameHandler.GetSongTime(song));
        if (!isManuallyChangingSection)
            currentSection = section;

        if (currentSection != lastSection)
        {
            lastSection = currentSection;
            ShowSection(currentSection);

            InitializeWave();
        }

        float sectionStart = GetSectionStartTime(currentSection);
        float wrappedRow = (GameHandler.GetSongTime(song) - sectionStart) / (sectionDuration / currentRows);

        Vector3 camPos = editorCamera.transform.position;
        editorCamera.transform.position = new Vector3(
            camPos.x,
            grid.originPosition.y + wrappedRow * grid.size,
            camPos.z
        );

        secondsPerRow = sectionDuration / currentRows;

        foreach (var n in notes)
        {
            if (n.section != currentSection || !noteObjects.ContainsKey(n)) continue;
            SetVisNote(n, n.time >= GameHandler.GetSongTime(song));
            noteObjects[n].transform.position = NoteTimeToWorldPosition(n);
            UpdateHoldVisual(n);
        }
        UpdateWaveformPosition(waveformRawImageOpponent);
        //UpdateWaveformPosition(waveformRawImagePlayer);
    }

    void SetVisNote(NoteData noteData, bool visible)
    {
        GameObject noteObj = noteObjects[noteData];
        var sr = noteObj.GetComponent<SpriteRenderer>();

        Color oldColor = sr.color;
        Color newColor = visible ? Color.white : new Color(1, 1, 1, 0.4f);
        
        if (oldColor.a != newColor.a && song.isPlaying)
            GlobalAudio.instance.PlayAudio("hitsound");

        sr.color = newColor;

        var lengthSr = noteObj.transform.Find("VisualLength").GetComponent<SpriteRenderer>();
        lengthSr.color = newColor;
    }
    bool isManuallyChangingSection = false;
    void GoToNextSection()
    {
        isManuallyChangingSection = true;

        bool fastJump = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        currentSection += fastJump ? 4 : 1;

        DefineSection();

        StartCoroutine(ResetManualFlag());
    }
    IEnumerator ResetManualFlag()
    {
        yield return new WaitForSeconds(0.1f);
        isManuallyChangingSection = false;
    }
    void GoToPreviousSection()
    {
        isManuallyChangingSection = true;
        bool fastJump = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        currentSection = Mathf.Max(0, currentSection - (fastJump?4:1));

        DefineSection();

        StartCoroutine(ResetManualFlag());
    }
    void DefineSection()
    {
        if (song != null) song.time = GetSectionStartTime(currentSection) + 0.01f;
        if (songOpponent != null) songOpponent.time = GetSectionStartTime(currentSection) + 0.01f;
        if (songPlayer != null) songPlayer.time = GetSectionStartTime(currentSection) + 0.01f;
        ShowSection(currentSection);
    }
    void ShowSection(int sectionIndex)
    {
        foreach (var obj in noteObjects.Values)
            Destroy(obj);
        noteObjects.Clear();

        float bpmToUse = bpmChanges.ContainsKey(sectionIndex) ? bpmChanges[sectionIndex] : defaultBPM;
        baseSecondsPerRow = 60f / bpmToUse / stepsPerBeat;
        sectionDuration = baseRows * baseSecondsPerRow;
        secondsPerRow = sectionDuration / currentRows;

        foreach (var n in notes)
        {
            if (n.section != sectionIndex) continue;
            Vector3 pos = NoteTimeToWorldPosition(n);
            GameObject noteObj = Instantiate(notePrefab, pos, Quaternion.identity);
            noteObj.GetComponent<SpriteRenderer>().sprite = noteDirsSprites[n.column];
            noteObj.GetComponent<NoteEditorObject>().noteData = n;
            noteObjects[n] = noteObj;
            UpdateHoldVisual(n);
        }
    }
    public static bool isCollidingNote = false;
    NoteData TileToNote(Vector3 worldPos)
    {
        Vector3 localPos = worldPos - grid.originPosition;

        int columnIndex = Mathf.FloorToInt(localPos.x / grid.size);
        columnIndex = Mathf.Clamp(columnIndex, 0, 7);

        NoteData.Owner side = (columnIndex <= 3) ? NoteData.Owner.Player : NoteData.Owner.Opponent;
        int column = columnIndex % 4;

        float sectionStart = GetSectionStartTime(currentSection);
        float secPerRow = GetSecondsPerRow(currentSection);

        float time;
        int rowIndex;

        bool isFree = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        if (isFree) // free mode 
        {
            float yOffset = localPos.y / grid.size - 0.5f;

            float rowInBaseScale = yOffset * baseRows / (float)currentRows;
            time = sectionStart + rowInBaseScale * secPerRow;

            // optional: still store visual row for reference
            rowIndex = Mathf.Clamp(Mathf.RoundToInt(yOffset), 0, currentRows - 1);
        }
        else
        {
            // 🟩 GRID MODE (your current behavior)
            rowIndex = Mathf.FloorToInt(localPos.y / grid.size);
            rowIndex = Mathf.Clamp(rowIndex, 0, currentRows - 1);

            float rowInBaseScale = rowIndex * baseRows / (float)currentRows;
            time = sectionStart + rowInBaseScale * secPerRow;
        }

        return new NoteData
        {
            column = column,
            time = time,
            side = side,
            section = currentSection,
            rowIndex = rowIndex
        };
    }
    GameObject CreateTile(Vector3 pos, float size)
    {
        GameObject obj = Instantiate(gridTilePrefab, pos, Quaternion.identity, gridParentTransform);
        obj.transform.localScale = Vector3.one * size;
        return obj;
    }

    [SerializeField] TMP_InputField copyPrevSection;
    public void CopyPreviousSection()
    {
        int previousIndex = copyPrevSection.text == ""?1:int.Parse(copyPrevSection.text); // number of previouses
        int targetSection = currentSection;
        int sourceSection = targetSection - previousIndex;
        if (sourceSection < 0) return;

        List<NoteData> newNotes = new List<NoteData>();

        foreach (var n in notes)
        {
            if (n.section != sourceSection) continue;

            NoteData copied = new NoteData
            {
                column = n.column,
                side = n.side,
                holdLength = n.holdLength,
                section = targetSection,
                time = GetSectionStartTime(targetSection) + (n.time - GetSectionStartTime(sourceSection)),
                rowIndex = n.rowIndex
            };

            newNotes.Add(copied);
            Vector3 pos = NoteTimeToWorldPosition(copied);
            GameObject noteObj = Instantiate(notePrefab, pos, Quaternion.identity);
            noteObj.GetComponent<SpriteRenderer>().sprite = noteDirsSprites[copied.column];
            noteObj.GetComponent<NoteEditorObject>().noteData = n;
            noteObjects[copied] = noteObj;
        }

        notes.AddRange(newNotes);
    }

    public void SwapColumns()
    {
        foreach (var n in notes)
        {
            if (n.section != currentSection) continue;
            n.side = (n.side == NoteData.Owner.Player) ? NoteData.Owner.Opponent : NoteData.Owner.Player;
            if (noteObjects.ContainsKey(n))
                noteObjects[n].transform.position = NoteTimeToWorldPosition(n);
        }
    }

    private List<NoteData> copiedNotes = new List<NoteData>();
    public void CopySection()
    {
        copiedNotes.Clear();

        foreach (var n in notes)
        {
            if (n.section != currentSection) continue;

            copiedNotes.Add(new NoteData
            {
                column = n.column,
                side = n.side,
                holdLength = n.holdLength,
                section = n.section,
                time = n.time,
                rowIndex = n.rowIndex
            });
        }
    }

    public void PasteSection()
    {
        if (copiedNotes.Count == 0) return;

        float sourceStart = GetSectionStartTime(copiedNotes[0].section);
        float targetStart = GetSectionStartTime(currentSection);

        List<NoteData> newNotes = new List<NoteData>();

        foreach (var n in copiedNotes)
        {
            NoteData pasted = new NoteData
            {
                column = n.column,
                side = n.side,
                holdLength = n.holdLength,
                section = currentSection,
                time = targetStart + (n.time - sourceStart),
                rowIndex = n.rowIndex
            };

            newNotes.Add(pasted);
            Vector3 pos = NoteTimeToWorldPosition(pasted);
            GameObject noteObj = Instantiate(notePrefab, pos, Quaternion.identity);
            noteObj.GetComponent<SpriteRenderer>().sprite = noteDirsSprites[pasted.column];
            noteObj.GetComponent<NoteEditorObject>().noteData = n;
            noteObjects[pasted] = noteObj;
        }

        notes.AddRange(newNotes);
    }

    [SerializeField, Range(0.1f, 1f)]
    private float waveAmplitudeLimit = 0.5f; // tweak in Inspector to reduce wave height

    IEnumerator GenerateWaveformSection(AudioClip clip, int sectionIndex, RawImage targetRawImage, Texture2D waveformTexture, Color waveformColor)
    {
        waveformReady = false;
        if (clip == null || targetRawImage == null)
            yield break;

        float sectionStart = GetSectionStartTime(sectionIndex);  // uses BPM per section
        float bpmForSection = GetBPMForSection(sectionIndex);
        float secPerRow = 60f / bpmForSection / stepsPerBeat;
        float sectionDuration = baseRows * secPerRow;
        float sectionEnd = Mathf.Min(sectionStart + sectionDuration, clip.length);
        float sectionLen = sectionEnd - sectionStart;
        if (sectionLen <= 0f) yield break;

        int startSample = Mathf.FloorToInt(sectionStart * clip.frequency);
        int endSample = Mathf.FloorToInt(sectionEnd * clip.frequency);
        int sampleCount = endSample - startSample;

        // ⬇️  FLIP AXES: height = timeline, width = amplitude
        int height = Mathf.Clamp(Mathf.CeilToInt(sectionLen * pixelsPerSecond), 64, 8192);
        int width = waveformHeight;

        float[] samples = new float[sampleCount * clip.channels];
        clip.GetData(samples, startSample);

        float[] rowMax = new float[height];
        int samplesPerPixel = Mathf.Max(1, sampleCount / height);

        // Collect max amplitude per "row" (time slice)
        for (int py = 0; py < height; py++)
        {
            int start = py * samplesPerPixel;
            int end = Mathf.Min(sampleCount, start + samplesPerPixel);
            float maxAmp = 0f;
            for (int s = start; s < end; s++)
            {
                for (int c = 0; c < clip.channels; c++)
                {
                    float v = Mathf.Abs(samples[s * clip.channels + c]);
                    if (v > maxAmp) maxAmp = v;
                }
            }
            rowMax[py] = maxAmp;
            if (py % 1024 == 0) yield return null;
        }

        if (waveformTexture != null)
            Destroy(waveformTexture);

        waveformTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        waveformTexture.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = waveformBGColor;

        int mid = width / 2;
        for (int y = 0; y < height; y++)
        {
            float amp = Mathf.Clamp01(rowMax[y] * waveAmplitudeLimit);
            if (amp <= 0.005f) continue; // skip silence

            int w = Mathf.RoundToInt(amp * (width / 2f));
            int x0 = Mathf.Clamp(mid - w, 0, width - 1);
            int x1 = Mathf.Clamp(mid + w, 0, width - 1);

            for (int x = x0; x <= x1; x++)
                pixels[y * width + x] = waveformColor;
        }

        waveformTexture.SetPixels(pixels);
        waveformTexture.Apply();

        waveformReady = true;
        targetRawImage.texture = waveformTexture;
        targetRawImage.uvRect = new Rect(0, 0, 1, 1);

        waveformJobOpponent = null;
        waveformJobPlayer = null;
    }
    void ClearWaveform(RawImage waveformRawImage)
    {
        waveformReady = false;
        if (waveformRawImage != null) waveformRawImage.texture = null;
        if (waveformTextureOpponent != null)
        {
            Destroy(waveformTextureOpponent);
            waveformTextureOpponent = null;
        }
        if (waveformTexturePlayer != null)
        {
            Destroy(waveformTexturePlayer);
            waveformTexturePlayer = null;
        }
        if (waveformJobOpponent != null)
        {
            StopCoroutine(waveformJobOpponent);
            waveformJobOpponent = null;
        }
        if (waveformJobPlayer != null)
        {
            StopCoroutine(waveformJobPlayer);
            waveformJobPlayer = null;
        }
    }
    void UpdateWaveformPosition(RawImage waveformRawImage)
    {
        if (!showWaveform || waveformRawImage == null || !waveformReady)
            return;

        // One section = full texture, so keep UV fixed
        waveformRawImage.uvRect = new Rect(0, 0, 1, 1);
    }
    float GetBPMForTime(float time)
    {
        int section = GetSectionFromTime(time);
        return GetBPMForSection(section);
    }
    int GetCurrentStep(float songTime)
    {
        float accumulatedTime = 0f;
        int totalSteps = 0;
        int section = 0;

        while (true)
        {
            float bpm = GetBPMForSection(section);
            float secPerBeat = 60f / bpm;
            float secPerStep = secPerBeat / stepsPerBeat;
            // float sectionDuration = baseRows * secPerStep * stepsPerBeat;
            float sectionDuration = (baseRows / (float)stepsPerBeat) * secPerBeat;
            if (songTime < accumulatedTime + sectionDuration)
            {
                float remainingTime = songTime - accumulatedTime;
                totalSteps += Mathf.FloorToInt(remainingTime / secPerStep);
                break;
            }

            // totalSteps += baseRows * stepsPerBeat;
            totalSteps += baseRows;
            accumulatedTime += sectionDuration;
            section++;
        }

        return totalSteps;
    }

    // Returns current beat based on song time and BPM
    int GetCurrentBeat(float songTime)
    {
        int curStep = GetCurrentStep(songTime);
        return curStep / stepsPerBeat; // 4 steps = 1 beat
    }

    void SaveChartToJson()
    {
        NoteChart wrap = new NoteChart();
        
        // Save notes
        foreach (var n in notes)
        {
            NoteSpawners spawner = new NoteSpawners();
            spawner.hasSpawned = false;
            spawner.noteHitTime = n.time;
            spawner.holdLength = n.holdLength;
            spawner.rowIndex = n.rowIndex;
            spawner.side = (n.side == NoteData.Owner.Player) ? "player" : "opponent";
            switch (n.column)
            {
                case 0: spawner.direction = "left"; break;
                case 1: spawner.direction = "down"; break;
                case 2: spawner.direction = "up"; break;
                case 3: spawner.direction = "right"; break;
            }
            spawner.section = n.section;
            wrap.notes.Add(spawner);
        }

        // Save BPM changes
        wrap.sections.Clear();
        foreach (var kvp in bpmChanges)
        {
            wrap.sections.Add(new SectionAct
            {
                sectionIndex = kvp.Key,
                bpm = kvp.Value
            });
        }
        wrap.defaultBPM = defaultBPM;
        wrap.songName = songName;
        wrap.songPath = "soundtrack/" +songName;
        // string basePath = Application.streamingAssetsPath + "/data/" + songName + "/";
        string basePath = FileLoader.GetPath("data/" + songName + "/");
        JsonConvertor.SaveJson(wrap, "chart", true, basePath, true);
        // JsonConvertor.SaveJson(wrap, "chart", true, Application.dataPath + "/Resources/data/"+songName+"/", true);
        Debug.Log("Chart + BPM saved to JSON!");
    }



    public void LoadChartFromJson(string fileName)
    {
        // NoteChart wrap = JsonConvertor.LoadJson<NoteChart>("chart", Application.dataPath + "/Resources/data/"+fileName+"/");
        //string basePath = Application.streamingAssetsPath + "/data/" + fileName + "/";
        string basePath = FileLoader.GetPath("data/" + songName + "/");
        NoteChart wrap = JsonConvertor.LoadJson<NoteChart>("chart", basePath);
        if (wrap == null)
        {
            Debug.LogWarning("No chart found at: " + fileName);
            return;
        }
        defaultBPM = wrap.defaultBPM;
        StartCoroutine(LoadSongAudio(fileName, wrap.songPath));
        // song.clip = Resources.Load<AudioClip>((wrap.songPath==""?"soundtrack/"+songName:wrap.songPath) + "/inst");
        // songOpponent.clip = Resources.Load<AudioClip>((wrap.songPath==""?"soundtrack/"+songName:wrap.songPath)+"/voices-opponent");
        // songPlayer.clip = Resources.Load<AudioClip>((wrap.songPath==""?"soundtrack/"+songName:wrap.songPath)+"/voices-player");
        // Clear old notes + visuals + bpm data
        foreach (var nObj in noteObjects.Values)
            Destroy(nObj);
        notes.Clear();
        noteObjects.Clear();
        bpmChanges.Clear();
        
        // --- Load Notes ---
        foreach (var spawner in wrap.notes)
        {
            int column = spawner.direction.ToLower() switch
            {
                "left" => 0,
                "down" => 1,
                "up" => 2,
                "right" => 3,
                _ => 0
            };

            NoteData.Owner side = spawner.side.ToLower() == "player"
                ? NoteData.Owner.Player
                : NoteData.Owner.Opponent;

            NoteData note = new NoteData
            {
                column = column,
                time = spawner.noteHitTime,
                side = side,
                section = spawner.section,
                holdLength = spawner.holdLength,
                rowIndex = spawner.rowIndex
            };

            notes.Add(note);

            // Instantiate visuals
            int visualColumn = (side == NoteData.Owner.Player) ? column : column + 4;
            float secPerRow = GetSecondsPerRow(note.section);
            float noteRow = (note.time % sectionDuration) / secPerRow;
            int rowIndex = note.rowIndex >= 0 ? note.rowIndex : Mathf.Clamp(Mathf.RoundToInt(noteRow), 0, currentRows - 1);

            Vector3 notePos = grid.WorldPosition(visualColumn, rowIndex);
            GameObject noteObj = Instantiate(notePrefab, notePos, Quaternion.identity);
            noteObj.GetComponent<SpriteRenderer>().sprite = noteDirsSprites[note.column];
            noteObj.GetComponent<NoteEditorObject>().noteData = note;
            noteObjects[note] = noteObj;
            UpdateHoldVisual(note);
        }

        // --- Load BPM changes ---
        foreach (var sec in wrap.sections)
        {
            bpmChanges[sec.sectionIndex] = sec.bpm;
        }

        ShowSection(currentSection);
        Debug.Log("Chart + BPM loaded from JSON: " + fileName);
    }
    IEnumerator LoadSongAudio(string songName, string songPath)
    {
        string basePath = "soundtrack/" + songName + "/";

        yield return FileLoader.LoadAudio(basePath + "inst.ogg", clip => song.clip = clip);
        yield return FileLoader.LoadAudio(basePath + "voices-opponent.ogg", clip => songOpponent.clip = clip);
        yield return FileLoader.LoadAudio(basePath + "voices-player.ogg", clip => songPlayer.clip = clip);

        Debug.Log("All audio loaded");
    }
    void LoadNotesOnly()
    {
        foreach (var nObj in noteObjects.Values)
            Destroy(nObj);
        noteObjects.Clear();

        foreach (var note in notes)
        {
            if (note.section != currentSection) continue;
            Vector3 notePos = NoteTimeToWorldPosition(note);
            GameObject noteObj = Instantiate(notePrefab, notePos, Quaternion.identity);
            noteObj.GetComponent<SpriteRenderer>().sprite = noteDirsSprites[note.column];
            noteObj.GetComponent<NoteEditorObject>().noteData = note;
            noteObjects[note] = noteObj;
            UpdateHoldVisual(note);
        }
        ShowSection(currentSection);
    }
    
}

[System.Serializable]
public class WaveFormRect
{
    public int requiredRow;
    public Vector2 position;
    public Vector2 size;
    public Vector2 scale;
    public float length;
}
using UnityEngine;

public enum ForgivenSustainLevel
{
    goMercful,
    forgiven,
    normal,
    strictful,
    I_HATE_YOU
}
public static class GameHandler
{

    public static float audioOffsetMs = 0;
    # region "gameplay difficulty"
    public static bool strict = false;
    public static bool forgivenSustain = true; // make it false if you like to get annoyed
    #endregion
    public static ForgivenSustainLevel forgivenSustainLevel = ForgivenSustainLevel.normal;
    public static KeyCode[][] keys = {
        new[] { KeyCode.LeftArrow, KeyCode.A },
        new[] { KeyCode.DownArrow, KeyCode.S },
        new[] { KeyCode.UpArrow, KeyCode.W },
        new[] { KeyCode.RightArrow, KeyCode.D }
    };

    public static bool GetKeyUp(KeyCode[] keyGroup)
    {
        foreach (var key in keyGroup)
        {
            if (Input.GetKeyUp(key))
                return true;
        }
        return false;
    }
    public static bool GetKeyDown(KeyCode[] keyGroup)
    {
        foreach (var key in keyGroup)
        {
            if (Input.GetKeyDown(key))
                return true;
        }
        return false;
    }
    public static bool GetKey(KeyCode[] keyGroup)
    {
        foreach (var key in keyGroup)
        {
            if (Input.GetKey(key))
                return true;
        }
        return false;
    }

    public static float GetSongTime(AudioSource song) // sync offset handler
    {
        float offsetSec = audioOffsetMs / 1000f;
        float songRes = song.time + offsetSec;

        return songRes;
    }
}

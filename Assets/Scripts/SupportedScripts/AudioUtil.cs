using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AudioUtil
{
    public static void PlayAudio(string name)
    {
        GameObject.FindWithTag("GlobalAudio").GetComponent<GlobalAudio>().PlayAudio(name);
    }
}

using System.Collections;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public static class FileLoader
{
    public static string GetPath(string relativePath)
    {
        return Path.Combine(Application.streamingAssetsPath, relativePath);
    }
    public static string GetStreamingURL(string relativePath)
    {
        return "file://" + GetStreamingPath(relativePath);
    }
    public static string GetStreamingPath(string relativePath)
    {
        return Path.Combine(Application.streamingAssetsPath, relativePath).Replace("\\", "/");
    }

    public static IEnumerator LoadAudio(string relativePath, System.Action<AudioClip> onLoaded)
    {
        string url = Path.Combine(Application.streamingAssetsPath, relativePath);

        using (UnityWebRequest www =
            UnityWebRequestMultimedia.GetAudioClip(url, AudioType.OGGVORBIS))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                onLoaded?.Invoke(DownloadHandlerAudioClip.GetContent(www));
            }
            else
            {
                Debug.LogWarning($"Audio failed: {relativePath}\n{www.error}");
                onLoaded?.Invoke(null);
            }
        }
    }
    
    public static string GetFullPath(string relativePath)
    {
        return relativePath;
    }
    public static string ToURL(string fullPath)
    {
        return new System.Uri(fullPath).AbsoluteUri;
    }
    public static IEnumerator LoadXMLText(string relativePath, System.Action<string> onLoaded)
    {
        string url = Path.Combine(Application.streamingAssetsPath, relativePath);

    #if !UNITY_EDITOR && UNITY_ANDROID
        url = new System.Uri(url).AbsoluteUri;
    #else
        url = "file://" + url;
    #endif

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string text = www.downloadHandler.text;
                text = text?.TrimStart('\uFEFF', ' ', '\n', '\r'); //cleanup is necessary since downloadHandler.text is including BOM
                //I could use File.readAllText() were it usually normalizes/removes BOM but android doesn't support readAllText()
                onLoaded?.Invoke(text);
            }
            else
            {
                Debug.LogError($"XML load failed: {relativePath}\n{www.error}");
                onLoaded?.Invoke(null);
            }
        }
    }
    public static IEnumerator LoadBytesCoroutine(string relativePath, System.Action<byte[]> onLoaded)
    {
        string url = Path.Combine(Application.streamingAssetsPath, relativePath);

    #if !UNITY_EDITOR && UNITY_ANDROID
        url = new System.Uri(url).AbsoluteUri;
    #else
        url = "file://" + url;
    #endif

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
                onLoaded?.Invoke(www.downloadHandler.data);
            else
                onLoaded?.Invoke(null);
        }
    }
}
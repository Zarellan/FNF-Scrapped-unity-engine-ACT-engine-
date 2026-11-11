using UnityEngine;
using System;
using System.IO;
public class JsonConvertor : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {

    }
    public static T LoadJson<T>(string name, string path = "jsonSaves/")
    {
        string json = File.ReadAllText(path + name + ".json");

        return JsonUtility.FromJson<T>(json);

    }


    public static void SaveJson(object jsonData, string name, bool cleanCode = true, string path = "jsonSaves/", bool overrideIt = false)
    {
        string json = JsonUtility.ToJson(jsonData, cleanCode);
        if (File.Exists(path + name + ".json"))
        {
            if (!overrideIt)
            {
                Debug.LogWarning("there is already file called: " + name);
                return;
            }
            else
            {
                File.Delete(path + name + ".json");
            }
        }


        Directory.CreateDirectory(path);
        File.WriteAllText(path + name + ".json", json);

    }

    public static bool JsonFileExist(string name, string path = "jsonSaves/")
    {
        return File.Exists(path + name + ".json");
    }
}

using System;
using System.IO;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using GLTFast;

public class GltfModLoader : MonoBehaviour
{
    [Serializable]
    public class ModConfig
    {
        public string rootBone;
        public float scale = 1f;
        public float[] offset;
        public AnimMap animations;
    }

    [Serializable]
    public class AnimMap
    {
        public string idle;
        public string left;
        public string down;
        public string up;
        public string right;
    }

    // ❌ REMOVE Animator (not reliable for GLTF)
    private Animation currentAnimation;

    void Awake()
    {
        LoadMod("Ch");
    }

    public async void LoadMod(string modFolderName)
    {
        string basePath = Path.Combine(Application.streamingAssetsPath, "Images", "Characters", modFolderName);
        string glbPath = Path.Combine(basePath, "model.glb");
        string configPath = Path.Combine(basePath, "config.json");

        string json = await LoadText(configPath);
        ModConfig config = JsonUtility.FromJson<ModConfig>(json);

        await LoadGltf(glbPath, config);

        PlayIdle(config);
    }

    public void PlayIdle(ModConfig config)
    {
        if (currentAnimation == null)
        {
            Debug.LogError("Animation not ready!");
            return;
        }

        currentAnimation.Play(config.animations.left);
    }

    private async System.Threading.Tasks.Task<string> LoadText(string path)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        using (UnityWebRequest req = UnityWebRequest.Get(path))
        {
            await req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(req.error);
                return null;
            }

            return req.downloadHandler.text;
        }
#else
        return File.ReadAllText(path);
#endif
    }

    private async System.Threading.Tasks.Task LoadGltf(string path, ModConfig config)
    {
        GltfImport gltf = new GltfImport();

#if UNITY_ANDROID && !UNITY_EDITOR
        using (UnityWebRequest req = UnityWebRequest.Get(path))
        {
            await req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(req.error);
                return;
            }

            byte[] data = req.downloadHandler.data;
            await gltf.Load(data);
        }
#else
        await gltf.Load(path);
#endif

        GameObject root = new GameObject("ModCharacter");
        await gltf.InstantiateMainSceneAsync(root.transform);

        ApplyConfig(root, config);

        // 🔥 FIX: Get Animation component instead of Animator
        currentAnimation = root.GetComponentInChildren<Animation>();

        // 🔥 SAFETY: if GLTF didn't create one, force add
        if (currentAnimation == null)
        {
            currentAnimation = root.AddComponent<Animation>();
        }

        Debug.Log("GLTF loaded + Animation ready");
    }

    private void ApplyConfig(GameObject obj, ModConfig config)
    {
        obj.transform.localScale = Vector3.one * config.scale;

        if (config.offset != null && config.offset.Length == 3)
        {
            obj.transform.position += new Vector3(
                config.offset[0],
                config.offset[1],
                config.offset[2]
            );
        }

        Debug.Log("Mod loaded successfully!");
    }

    // Optional helpers
    public void PlayLeft(ModConfig config)
    {
        if (currentAnimation != null)
            currentAnimation.Play(config.animations.left);
    }

    public void PlayRight(ModConfig config)
    {
        if (currentAnimation != null)
            currentAnimation.Play(config.animations.right);
    }
}
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class XmlAtlasLoaderBinary : MonoBehaviour
{
    [Header("Atlas Settings")]
    public string fileName = "BOYFRIEND";
    public SpriteRenderer previewRenderer;
    public float pixelsPerUnit = 100f;

    private Texture2D texture;

    private struct SpriteData
    {
        public short x, y, w, h;
        public short fx, fy, fw, fh;
    }

    private Dictionary<string, SpriteData> spriteData = new Dictionary<string, SpriteData>();
    private Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

    private float scaleX = 1f;
    private float scaleY = 1f;

    void Awake()
    {
        LoadTexture();
        LoadBinary();

        if (previewRenderer != null)
        {
            previewRenderer.sprite = GetSprite("BF NOTE LEFT0000");
        }
        // DebugDumpBinary();
    }

    void LoadTexture()
    {
        string path = Path.Combine(Application.streamingAssetsPath,
            "Images/Characters/" + fileName + ".texture.dat");

        using (BinaryReader reader = new BinaryReader(File.OpenRead(path)))
        {
            string magic = reader.ReadString();
            int version = reader.ReadInt32();

            int width = reader.ReadInt32();
            int height = reader.ReadInt32();

            byte compression = reader.ReadByte();
            int dataLength = reader.ReadInt32();

            byte[] data = reader.ReadBytes(dataLength);

            // decompress if needed
            if (compression == 1) // GZip
            {
                data = DecompressGZip(data);
            }

            texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.LoadRawTextureData(data);
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
        }
    }
    public async void LoadTextureAsync()
    {
        string path = Path.Combine(Application.streamingAssetsPath,
            "Images/Characters/" + fileName + ".texture.dat");

        byte[] fileData = await File.ReadAllBytesAsync(path);

        Texture2D tex = FastTextureSerializer.Load(fileData);

        texture = tex;
    }
    
    byte[] DecompressGZip(byte[] data)
    {
        using (MemoryStream input = new MemoryStream(data))
        using (System.IO.Compression.GZipStream gzip =
            new System.IO.Compression.GZipStream(input, System.IO.Compression.CompressionMode.Decompress))
        using (MemoryStream output = new MemoryStream())
        {
            gzip.CopyTo(output);
            return output.ToArray();
        }
    }
    void LoadBinary()
    {
        string path = Path.Combine(Application.streamingAssetsPath,
            "Images/Characters/" + fileName + ".atlas.dat");

        using (BinaryReader reader = new BinaryReader(File.OpenRead(path)))
        {
            int count = reader.ReadInt32();

            for (int i = 0; i < count; i++)
            {
                string name = reader.ReadString();

                name = name.Replace("_", " ")
                        .Replace("-", " ")
                        .Trim();

                SpriteData d = new SpriteData
                {
                    x = reader.ReadInt16(),
                    y = reader.ReadInt16(),
                    w = reader.ReadInt16(),
                    h = reader.ReadInt16(),
                    fx = reader.ReadInt16(),
                    fy = reader.ReadInt16(),
                    fw = reader.ReadInt16(),
                    fh = reader.ReadInt16()
                };

                spriteData[name] = d;
            }
        }
    }
    // public void DebugDumpBinary()
    // {
    //     string path = Path.Combine(Application.streamingAssetsPath,
    //         "Images/Characters/" + fileName + ".dat");

    //     using (BinaryReader reader = new BinaryReader(File.OpenRead(path)))
    //     {
    //         int count = reader.ReadInt32();
    //         Debug.Log($"[ATLAS DEBUG] Sprite count: {count}");

    //         for (int i = 0; i < count; i++)
    //         {
    //             string name = reader.ReadString();

    //             short x = reader.ReadInt16();
    //             short y = reader.ReadInt16();
    //             short w = reader.ReadInt16();
    //             short h = reader.ReadInt16();

    //             short fx = reader.ReadInt16();
    //             short fy = reader.ReadInt16();
    //             short fw = reader.ReadInt16();
    //             short fh = reader.ReadInt16();

    //             Debug.Log(
    //                 $"[SPRITE {i}] {name} | " +
    //                 $"x:{x} y:{y} w:{w} h:{h} | " +
    //                 $"fx:{fx} fy:{fy} fw:{fw} fh:{fh}"
    //             );
    //         }
    //     }
    // }
    public Sprite GetSprite(string name)
    {
        if (spriteCache.TryGetValue(name, out var cached))
            return cached;

        if (!spriteData.TryGetValue(name, out var d))
            return null;

        Rect rect = new Rect(d.x, texture.height - d.y - d.h, d.w, d.h);

        Vector2 pivot = new Vector2(
            (float)d.fx / Mathf.Max(1, d.fw),
            (d.h / 2f - d.fy) / Mathf.Max(1, d.fh)
        );

        Sprite sprite = Sprite.Create(texture, rect, pivot, pixelsPerUnit);
        sprite.name = name;

        spriteCache[name] = sprite;
        return sprite;
        
    }
    public List<string> GetAllSpriteNames(string prefix)
    {
        List<string> result = new List<string>();

        foreach (var key in spriteData.Keys)
        {
            if (key.StartsWith(prefix))
                result.Add(key);
        }

        return result;
    }
}
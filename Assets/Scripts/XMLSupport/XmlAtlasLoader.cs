using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEngine;
using System.Collections;

public class XmlAtlasLoader : MonoBehaviour
{
    [Header("Atlas Settings")]
    public string xmlFileName = "BOYFRIEND";
    public SpriteRenderer previewRenderer;
    public float pixelsPerUnit = 100f;

    [Header("Original Atlas Size")]
    public int originalAtlasWidth = 0;
    public int originalAtlasHeight = 0;

    private Texture2D texture;

    // FINAL SPRITES (cached after creation)
    public Dictionary<string, Sprite> spriteAtlas = new Dictionary<string, Sprite>();

    // XML raw nodes (only used once during load)
    private Dictionary<string, XmlNode> spriteNodes = new Dictionary<string, XmlNode>();

    // pivot cache
    public Dictionary<string, Vector2> spritePivot = new Dictionary<string, Vector2>();

    // 🔥 NEW: prefix → frame names cache (BIG PERFORMANCE WIN)
    private Dictionary<string, List<string>> frameNameCache = new Dictionary<string, List<string>>();

    private float scaleX = 1f;
    private float scaleY = 1f;

    void Awake()
    {
        StartCoroutine(LoadAndPreviewAtlas());
    }

    private IEnumerator LoadAndPreviewAtlas()
    {
        yield return LoadAtlasFromStreamingAssets("Images/Characters/" + xmlFileName);

        if (previewRenderer != null)
        {
            Sprite preview = GetSprite("BF NOTE LEFT0000");
            if (preview != null)
                previewRenderer.sprite = preview;
        }
    }

    #region LOADING

    private IEnumerator LoadAtlasFromStreamingAssets(string relativePath)
    {
        string pngRelative = relativePath + ".png";
        string xmlRelative = relativePath + ".xml";

        byte[] pngData = null;
        string xmlText = null;

        yield return FileLoader.LoadBytesCoroutine(pngRelative, data => pngData = data);
        yield return FileLoader.LoadXMLText(xmlRelative, text => xmlText = text);

        if (pngData == null || xmlText == null)
        {
            Debug.LogError($"Atlas missing: {relativePath}");
            yield break;
        }

        texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.LoadImage(pngData);

        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(xmlText);

        ParseXml(xmlDoc);

        Debug.Log($"Loaded atlas '{xmlFileName}' ({texture.width}x{texture.height})");
    }

    private void ParseXml(XmlDocument xmlDoc)
    {
        XmlNode root = xmlDoc.DocumentElement;

        int xmlWidth = originalAtlasWidth;
        int xmlHeight = originalAtlasHeight;

        if (root.Attributes["width"] != null && root.Attributes["height"] != null)
        {
            int.TryParse(root.Attributes["width"].Value, out xmlWidth);
            int.TryParse(root.Attributes["height"].Value, out xmlHeight);
        }

        if (xmlWidth > 0 && xmlHeight > 0)
        {
            scaleX = texture.width / (float)xmlWidth;
            scaleY = texture.height / (float)xmlHeight;
        }

        XmlNodeList subTextures = xmlDoc.GetElementsByTagName("SubTexture");

        spriteNodes.Clear();
        spriteAtlas.Clear();
        spritePivot.Clear();
        frameNameCache.Clear();

        // 🔥 build lookup ONCE
        foreach (XmlNode node in subTextures)
        {
            string name = node.Attributes["name"].Value;
            spriteNodes[name] = node;

            string prefix = ExtractPrefix(name);

            if (!frameNameCache.ContainsKey(prefix))
                frameNameCache[prefix] = new List<string>();

            frameNameCache[prefix].Add(name);
        }

        Debug.Log($"Parsed {spriteNodes.Count} sprites (optimized cache mode).");
    }

    private string ExtractPrefix(string name)
    {
        // splits BEFORE first digit group
        for (int i = 0; i < name.Length; i++)
        {
            if (char.IsDigit(name[i]))
                return name.Substring(0, i);
        }
        return name;
    }

    #endregion

    #region SPRITE SYSTEM

    public Sprite GetSprite(string name)
    {
        string key = xmlFileName + ":" + name;

        // 🔥 1. GLOBAL CACHE FIRST (fastest path)
        if (GlobalSpriteCache.TryGet(key, out var cachedSprite))
        {
            spritePivot[name] = spritePivot.ContainsKey(name)
                ? spritePivot[name]
                : Vector2.zero;

            return cachedSprite;
        }

        // 🔥 2. LOCAL CACHE (optional safety)
        if (spriteAtlas.TryGetValue(name, out var localSprite))
            return localSprite;

        // 🔥 3. XML lookup
        if (!spriteNodes.TryGetValue(name, out var node))
        {
            Debug.LogWarning($"Sprite '{name}' not found in atlas.");
            return null;
        }

        try
        {
            int x = int.Parse(node.Attributes["x"].Value);
            int y = int.Parse(node.Attributes["y"].Value);
            int width = int.Parse(node.Attributes["width"].Value);
            int height = int.Parse(node.Attributes["height"].Value);

            int frameX = node.Attributes["frameX"] != null ? int.Parse(node.Attributes["frameX"].Value) : 0;
            int frameY = node.Attributes["frameY"] != null ? int.Parse(node.Attributes["frameY"].Value) : 0;
            int frameWidth = node.Attributes["frameWidth"] != null ? int.Parse(node.Attributes["frameWidth"].Value) : width;
            int frameHeight = node.Attributes["frameHeight"] != null ? int.Parse(node.Attributes["frameHeight"].Value) : height;

            float scaledX = x * scaleX;
            float scaledY = y * scaleY;
            float scaledWidth = width * scaleX;
            float scaledHeight = height * scaleY;

            float scaledFrameX = frameX * scaleX;
            float scaledFrameY = frameY * scaleY;
            float scaledFrameWidth = frameWidth * scaleX;
            float scaledFrameHeight = frameHeight * scaleY;

            float flippedY = texture.height - scaledY - scaledHeight;

            Rect rect = new Rect(scaledX, flippedY, scaledWidth, scaledHeight);

            Vector2 pivot = new Vector2(
                scaledFrameX / scaledFrameWidth,
                (scaledHeight / 2f - scaledFrameY) / scaledFrameHeight
            );

            // 🔥 4. CREATE ONLY ONCE
            Sprite sprite = Sprite.Create(texture, rect, pivot, pixelsPerUnit);
            sprite.name = name;

            // 🔥 5. STORE IN BOTH CACHES
            spriteAtlas[name] = sprite;
            spritePivot[name] = pivot;
            if (!GlobalSpriteCache.TryGet(key, out _))
            {
                GlobalSpriteCache.Add(key, sprite);
            }
            return sprite;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Failed sprite '{name}': {e.Message}");
            return null;
        }
    }

    #endregion

    #region PUBLIC API

    // 🔥 NOW FAST (no scanning anymore)
    public List<string> GetAllSpriteNames(string prefix = null)
    {
        if (prefix == null)
        {
            return new List<string>(spriteNodes.Keys);
        }

        if (frameNameCache.TryGetValue(prefix, out var list))
            return list;

        return new List<string>();
    }

    #endregion
}
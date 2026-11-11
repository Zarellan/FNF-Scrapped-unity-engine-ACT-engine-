// using System.Collections.Generic;
// using System.Xml;
// using UnityEngine;

// public class XmlAtlasLoader : MonoBehaviour
// {
//     [Header("Atlas Settings")]
//     public string xmlFileName = "BOYFRIEND"; // Without extension
//     public SpriteRenderer previewRenderer;
//     public float pixelsPerUnit = 100f;

//     private Texture2D texture;

//     // Stores already created sprites
//     public Dictionary<string, Sprite> spriteAtlas = new Dictionary<string, Sprite>();

//     // Stores XML node info for lazy loading
//     private Dictionary<string, XmlNode> spriteNodes = new Dictionary<string, XmlNode>();
//     public Dictionary<string, Vector2> spritePivot = new Dictionary<string, Vector2>();

//     void Awake()
//     {
//         LoadAtlasXml(xmlFileName);

//         // Optional: preview one sprite
//         if (previewRenderer != null)
//         {
//             Sprite preview = GetSprite("BF NOTE LEFT0000");
//             if (preview != null)
//                 previewRenderer.sprite = preview;
//         }
//     }

//     /// <summary>
//     /// Load XML and texture, store XML nodes for lazy sprite creation.
//     /// </summary>
//     void LoadAtlasXml(string baseName)
//     {
//         texture = Resources.Load<Texture2D>(baseName);
//         if (texture == null)
//         {
//             Debug.LogError("Texture not found: " + baseName);
//             return;
//         }

//         TextAsset xmlAsset = Resources.Load<TextAsset>(baseName);
//         if (xmlAsset == null)
//         {
//             Debug.LogError("XML not found: " + baseName);
//             return;
//         }

//         XmlDocument xmlDoc = new XmlDocument();
//         xmlDoc.LoadXml(xmlAsset.text);
//         XmlNodeList subTextures = xmlDoc.GetElementsByTagName("SubTexture");

//         foreach (XmlNode node in subTextures)
//         {
//             string name = node.Attributes["name"].Value;
//             spriteNodes[name] = node; // store XML info for later
//         }

//         Debug.Log($"Parsed {spriteNodes.Count} sprites from {baseName}.xml (lazy loading)");
//     }

//     /// <summary>
//     /// Get a sprite by name. Creates it on-demand if it doesn't exist yet.
//     /// </summary>
//     public Sprite GetSprite(string name)
//     {
//         // Return existing sprite if already created
//         if (spriteAtlas.TryGetValue(name, out var sprite))
//             return sprite;

//         // Create sprite lazily
//         if (!spriteNodes.TryGetValue(name, out var node))
//         {
//             Debug.LogWarning($"Sprite '{name}' not found in XML atlas.");
//             return null;
//         }

//         try
//         {
//             int x = int.Parse(node.Attributes["x"].Value);
//             int y = int.Parse(node.Attributes["y"].Value);
//             int width = int.Parse(node.Attributes["width"].Value);
//             int height = int.Parse(node.Attributes["height"].Value);

//             // Optional frame data for trimmed sprites
//             int frameX = node.Attributes["frameX"] != null ? int.Parse(node.Attributes["frameX"].Value) : 0;
//             int frameY = node.Attributes["frameY"] != null ? int.Parse(node.Attributes["frameY"].Value) : 0;
//             int frameWidth = node.Attributes["frameWidth"] != null ? int.Parse(node.Attributes["frameWidth"].Value) : width;
//             int frameHeight = node.Attributes["frameHeight"] != null ? int.Parse(node.Attributes["frameHeight"].Value) : height;

//             int flippedY = texture.height - y - height;
//             Rect rect = new Rect(x, flippedY, width, height);

//             // Pivot adjusted for trimmed frames
//             // Vector2 pivot = new Vector2(
//             //     0.5f - (float)frameX / frameWidth,
//             //     0.5f + (float)(frameY + (frameHeight - height)) / frameHeight
//             // );

//             // Rect info
//             float trimmedWidth = rect.width;
//             float trimmedHeight = rect.height;
//             // Pivot adjusted for trimmed frames relative to original frame
//             Vector2 pivot = new Vector2(
//                 (float)frameX / frameWidth,
//                 (height / 2f - frameY) / frameHeight
//             );
//             sprite = Sprite.Create(texture, rect, new Vector2(pivot.x,pivot.y), pixelsPerUnit);
//             sprite.name = name;
//             spriteAtlas[name] = sprite;
//             spritePivot[name] = new Vector2(pivot.x,0);
//             if (name.Contains("BF NOTE LEFT"))
//                 Debug.Log($"{name} pivot={pivot} frameX={frameX}, frameY={frameY}");

//             return sprite;
//         }

//         catch (System.Exception e)
//         {
//             Debug.LogWarning($"Failed to create sprite '{name}': {e.Message}");
//             return null;
//         }

        
//     }

//     /// <summary>
//     /// Optional: get all sprite names for animations or UI.
//     /// </summary>
//     public List<string> GetAllSpriteNames(string prefix = null)
//     {
//         var list = new List<string>();
//         foreach (var key in spriteNodes.Keys)
//         {
//             if (prefix == null || key.StartsWith(prefix))
//                 list.Add(key);
//         }
//         list.Sort();
//         return list;
//     }
// }


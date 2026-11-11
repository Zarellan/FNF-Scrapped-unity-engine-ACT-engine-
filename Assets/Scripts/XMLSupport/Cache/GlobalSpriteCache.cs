using System.Collections.Generic;
using UnityEngine;

public static class GlobalSpriteCache
{
    // key = atlas + sprite name
    private static Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    public static bool TryGet(string key, out Sprite sprite)
    {
        return cache.TryGetValue(key, out sprite);
    }

    public static void Add(string key, Sprite sprite)
    {
        if (!cache.ContainsKey(key))
            cache[key] = sprite;
    }

    public static void Clear()
    {
        cache.Clear();
    }
}
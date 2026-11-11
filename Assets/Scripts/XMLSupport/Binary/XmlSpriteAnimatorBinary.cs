using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PrimeTween;
using System.Linq;

public class XmlSpriteAnimatorBinary : MonoBehaviour
{
    [Header("Binary Atlas & Animation")]
    public XmlAtlasLoaderBinary atlasLoader;   // 🔥 switched to binary loader
    public string prefix = "BF NOTE DOWN";
    public float fps = 12f;

    private SpriteRenderer sr;

    private List<Sprite> frames = new List<Sprite>();
    private Dictionary<string, List<Sprite>> animationCache = new Dictionary<string, List<Sprite>>();

    private int currentFrame = 0;
    private bool loop;

    private Vector2 localPosition;
    private Vector2 localOffset;

    [SerializeField] List<AnimationData> animData;

    private Tween playAn;

    void Awake()
    {
        localPosition = transform.position;
    }

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();

        frames = GetFrames(prefix);

        if (frames.Count > 0)
            sr.sprite = frames[0];

        Preload(
            FindBySingType("idle")?.prefix,
            FindBySingType("left")?.prefix,
            FindBySingType("right")?.prefix,
            FindBySingType("up")?.prefix,
            FindBySingType("down")?.prefix
        );
    }

    List<Sprite> GetFrames(string animPrefix)
    {
        if (animationCache.TryGetValue(animPrefix, out var cached))
            return cached;

        List<Sprite> newFrames = new List<Sprite>();

        foreach (var name in atlasLoader.GetAllSpriteNames(animPrefix))
        {
            if (name.StartsWith(animPrefix))
            {
                if (name.Length > animPrefix.Length && char.IsDigit(name[animPrefix.Length]))
                {
                    Sprite sprite = atlasLoader.GetSprite(name);

                    if (sprite != null)
                        newFrames.Add(sprite);
                }
            }
        }

        newFrames.Sort((a, b) =>
            string.Compare(a.name, b.name, System.StringComparison.Ordinal));

        if (newFrames.Count == 0)
            Debug.LogWarning($"No frames found with prefix '{animPrefix}'");

        animationCache[animPrefix] = newFrames;
        return newFrames;
    }

    void Preload(params string[] animations)
    {
        foreach (var anim in animations)
            if (!string.IsNullOrEmpty(anim))
                GetFrames(anim);
    }

    public void PlayAnimationSing(string name, bool loop)
    {
        var data = FindBySingType(name);
        PlayAnimation(data.prefix, loop, data.pivotOffset);
    }

    public void PlayAnimation(string name, bool loop, Vector2 posPivot = new Vector2())
    {
        this.prefix = name;
        this.loop = loop;

        currentFrame = 0;
        localOffset = posPivot;

        transform.position = localPosition + localOffset;

        frames = GetFrames(prefix);

        PlayAnimationManager(true);
    }

    void PlayAnimationManager(bool force)
    {
        if ((frames.Count == 0) || (currentFrame <= 0 && !force && !loop))
            return;

        if (playAn.isAlive && currentFrame <= 0)
            playAn.Stop();

        sr.sprite = frames[currentFrame];

        currentFrame = (currentFrame + 1) % frames.Count;

        playAn = Tween.Delay(1f / FindByPrefix(prefix).fps)
            .OnComplete(() => PlayAnimationManager(false));
    }

    public void PlaySingleFrame(string frameName)
    {
        var sprite = atlasLoader.GetSprite(frameName);

        if (sprite != null)
            sr.sprite = sprite;
        else
            Debug.LogWarning($"Frame '{frameName}' not found in binary atlas.");
    }

    public AnimationData FindByPrefix(string name)
    {
        return animData.FirstOrDefault(a => a.prefix == name);
    }

    public AnimationData FindBySingType(string name)
    {
        return animData.FirstOrDefault(a => a.singType == name);
    }
}

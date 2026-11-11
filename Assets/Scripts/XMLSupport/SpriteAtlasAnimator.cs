using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PrimeTween;
using System.Linq;

public class XmlSpriteAnimator : MonoBehaviour
{
    [Header("XML Atlas & Animation")]
    public XmlAtlasLoader atlasLoader;   // Reference to your XmlAtlasLoader
    public string prefix = "BF NOTE DOWN";   // Prefix of animation frames
    public float fps = 12f;

    private SpriteRenderer sr;
    private List<Sprite> frames = new List<Sprite>();
    private int currentFrame = 0;
    bool loop;
    Vector2 localPosition;
    Vector2 localOffset;
    [SerializeField] List<AnimationData> animData;
    private Dictionary<string, List<Sprite>> animationCache = new Dictionary<string, List<Sprite>>();
    void Awake()
    {
        localPosition = transform.position;
    }
    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        // LoadFramesFromXmlAtlas();
        frames = GetFrames(prefix);
        // Show first frame before animation starts
        if (frames.Count > 0)
            sr.sprite = frames[0];
        
        for (int i = 0; i < animData.Count;i++)
            PreloadSingle(FindBySingType(animData[i].singType)?.prefix);


        // Preload(FindBySingType("idle")?.prefix,
        // FindBySingType("left")?.prefix,
        // FindBySingType("right")?.prefix,
        // FindBySingType("up")?.prefix,
        // FindBySingType("down")?.prefix,
        // FindBySingType("downMiss")?.prefix,
        // FindBySingType("upMiss")?.prefix,
        // FindBySingType("rightMiss")?.prefix,
        // FindBySingType("leftMiss")?.prefix
        // );
        //StartCoroutine(PlayAnimation());
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            PlayAnimationSing("idle", true);
        }
        // if (Input.GetKeyDown(KeyCode.LeftArrow))
        //     PlayAnimation(FindBySingType("left").prefix, false, FindBySingType("left").pivotOffset);
        // else if (Input.GetKeyDown(KeyCode.DownArrow))
        //     PlayAnimation(FindBySingType("down").prefix, false,FindBySingType("down").pivotOffset);
        // else if (Input.GetKeyDown(KeyCode.UpArrow))
        //     PlayAnimation(FindBySingType("up").prefix, false, FindBySingType("up").pivotOffset);
        // else if (Input.GetKeyDown(KeyCode.RightArrow))
        //     PlayAnimation(FindBySingType("right").prefix, false,FindBySingType("right").pivotOffset);

    }
    List<Sprite> GetFrames(string animPrefix)
    {
        if (animationCache.TryGetValue(animPrefix, out var cachedFrames))
            return cachedFrames;

        // Not cached → load once
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

        newFrames.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));

        if (newFrames.Count == 0)
            Debug.LogWarning($"No frames found with prefix '{animPrefix}'");

        animationCache[animPrefix] = newFrames;

        return newFrames;
    }
    void Preload(params string[] animations)
    {
        foreach (var anim in animations)
            GetFrames(anim);
    }
    void PreloadSingle(string animations)
    {
        GetFrames(animations);
    }

    void LoadFramesFromXmlAtlas()
    {
        frames.Clear();

        foreach (var name in atlasLoader.GetAllSpriteNames(prefix))
        {
            if (name.StartsWith(prefix))
            {
                // Ensure next char is a digit
                if (name.Length > prefix.Length && char.IsDigit(name[prefix.Length]))
                {
                    Sprite sprite = atlasLoader.GetSprite(name);

                    if (sprite != null)
                        frames.Add(sprite);
                }
            }
        }

        // Sort frames by name for correct order
        frames.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
        if (frames.Count == 0)
            Debug.LogWarning($"No frames found with prefix '{prefix}' in XML atlas!");
    }

    private Tween playAn;

    public void PlayAnimation(string name, bool loop, Vector2 posPivot = new Vector2())
    {
        this.prefix = name;
        this.loop = loop;
        currentFrame = 0;
        localOffset = posPivot;
        transform.position = localPosition + localOffset;
        // LoadFramesFromXmlAtlas();
        frames = GetFrames(prefix);
        PlayAnimationManager(true);
    }
    void PlayAnimationManager(bool force)
    {
        if ((frames.Count == 0) || (currentFrame <= 0 && !force && !loop))
        {
            return;
        }
        if (playAn.isAlive && currentFrame <= 0)
            playAn.Stop();
        sr.sprite = frames[currentFrame];
        // if (currentFrame == 0 || currentFrame == 1)
        //     transform.position = localPosition + new Vector2(0.5f, 0.5f);
        // else
        //     transform.position = localPosition + new Vector2(0.53f, 0.48f);

        Vector2 offset = atlasLoader.spritePivot[frames[currentFrame].name] / atlasLoader.pixelsPerUnit;
        transform.localPosition = localPosition + localOffset /*+ offset*/;
            //transform.position = localPosition + posPivot;
        currentFrame = (currentFrame + 1) % frames.Count;

        // Schedule next frame
        playAn = Tween.Delay(1f / FindByPrefix(prefix).fps)
                    .OnComplete(() => PlayAnimationManager(false));
    }

    // Optional: play a single frame by name
    public void PlaySingleFrame(string frameName)
    {
        if (atlasLoader.spriteAtlas.TryGetValue(frameName, out Sprite frame))
            sr.sprite = frame;
        else
            Debug.LogWarning($"Frame '{frameName}' not found in XML atlas.");
    }
    public AnimationData FindByPrefix(string name)
    {
        return animData.FirstOrDefault(a => a.prefix == name);
    }
    public AnimationData FindBySingType(string name)
    {
        return animData.FirstOrDefault(a => a.singType == name);
    }
    public void PlayAnimationSing(string name, bool loop)
    {
        PlayAnimation(FindBySingType(name).prefix, loop, FindBySingType(name).pivotOffset);
    }

}


[System.Serializable]
public class AnimationData
{
    public string prefix;
    public string singType;
    public Vector2 pivotOffset;
    public float fps = 12f;
}
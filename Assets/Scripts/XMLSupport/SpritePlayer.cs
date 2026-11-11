using UnityEngine;

public class SpritePlayer : MonoBehaviour
{
    public XmlAtlasLoader atlasLoader;
    public string spriteName = "BF HEY!!0005";

    void Start()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        //sr.sprite = atlasLoader.spriteAtlas[spriteName];
    }
}

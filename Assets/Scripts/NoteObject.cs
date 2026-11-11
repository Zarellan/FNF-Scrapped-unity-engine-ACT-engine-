using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

public class NoteObject : MonoBehaviour
{
    private AudioSource song;
    private RectTransform rect;
    private RectTransform spawnPoint;
    private RectTransform hitPoint;

    private float hitTime;
    private float travelTime;
    private float scrollSpeed;
    private string direction;
    private string side;
    private bool isSustain;
    static int idSus;
    int idSus1;
    bool pointReached = false;

    GameObject playerSide;
    string rating;

    bool noteMiss = false;
    public void Initialize(AudioSource songSource, RectTransform spawn, RectTransform hit, float timeToHit, float travel, string direction, string side)
    {
        song = songSource;
        spawnPoint = spawn;
        hitPoint = hit;
        hitTime = timeToHit;
        travelTime = travel;
        this.direction = direction;
        this.side = side;
        rect = GetComponent<RectTransform>();

        playerSide = GameObject.FindWithTag("PlayerSide");
        // GetComponent<RectTransform>().SetSiblingIndex(!isSustain?10:9);

    }
    public void IsSustain()
    {
        isSustain = true;
        idSus1 = idSus;
        idSus++;
    }
    public void SetScrollSpeed(float speed)
    {
        scrollSpeed = speed;
    }

    public void SetTravelSpeed(float trav)
    {
        travelTime = trav;
    }

    void Update()
    {
        if (rect == null || song == null) return;

        // float distance = Mathf.Abs(spawnPoint.anchoredPosition.y - hitPoint.anchoredPosition.y);
        // float travelTime = distance / scrollSpeed;

        float distance = Vector2.Distance(spawnPoint.anchoredPosition, hitPoint.anchoredPosition);
        if (distance < 0.001f) distance = 0.001f;
        float travelTime = distance / scrollSpeed;

        float currentTime = song.time;
        float t = 1f - ((hitTime - currentTime) / travelTime);
        rect.anchoredPosition = Vector2.LerpUnclamped(spawnPoint.anchoredPosition, hitPoint.anchoredPosition, t);

        if (isSustain)
            rect.SetSiblingIndex(hitPoint.GetSiblingIndex() + 3);
        else
            rect.SetAsLastSibling();   // always on top

        if (side == "player")
        {
            if (t >= 1f)
            {
                pointReached = true;
            }
            switch (direction)
            {
                case "left": ButtonPress(GameHandler.keys[0]); break;
                case "right": ButtonPress(GameHandler.keys[3]); break;
                case "up": ButtonPress(GameHandler.keys[2]); break;
                case "down": ButtonPress(GameHandler.keys[1]); break;
            }
            if (isSustain)
            {
                if (!pointReached)
                    ObjectLookAt2D(gameObject, hitPoint.gameObject,-90);
            }
            if (t > 1.5f && pointReached)
            {
                noteMiss = true;
                Destroy(this.gameObject);
            }
        }
        else
        {
            // Optionally remove note after passing hit point
            if (t >= 1f)
            {
                pointReached = true;
                if (!isSustain)
                {
                    GetComponent<Image>().color = new Color(1, 1, 1, 0);
                    HitSoundNow();
                    Destroy(this.gameObject);
                }
                // if (isSustain)
                // {
                //     float sustainTopY = hitPoint.anchoredPosition.y + 10; // top of sustain, set based on your setup
                //     float currentY = rect.anchoredPosition.y - 5;

                //     float totalDistance = sustainTopY - hitPoint.anchoredPosition.y;
                //     float remainingDistance = Mathf.Clamp(currentY - hitPoint.anchoredPosition.y, 0f, totalDistance);

                //     GetComponent<Image>().fillAmount = 1f - (remainingDistance / totalDistance);
                // }
            }
            if (isSustain)
            {
                RectTransform rt = GetComponent<RectTransform>();

                float sustainHeight = rt.sizeDelta.y;

                if ((pointReached ? -Vector2.Distance(hitPoint.anchoredPosition, rt.anchoredPosition) : Vector2.Distance(hitPoint.anchoredPosition, rt.anchoredPosition)) < sustainHeight / 2)
                {
                    GetComponent<Image>().fillAmount = MapStartEnd(pointReached ? -Vector2.Distance(hitPoint.anchoredPosition, rt.anchoredPosition) : Vector2.Distance(hitPoint.anchoredPosition, rt.anchoredPosition), rt.sizeDelta.y / 2, -(rt.sizeDelta.y / 2));
                }
                if (!pointReached)
                    ObjectLookAt2D(gameObject, hitPoint.gameObject,-90);
            }

        }

        if (GetComponent<Image>().fillAmount <= 0)
        {
            Destroy(this.gameObject);
        }
    }
        float MapStartEnd(float value, float start, float end)
        {
            float mid = (start + end) / 2f; // midpoint
            if (value >= mid) 
                return Mathf.Lerp(1f, 0.5f, (start - value) / (start - mid));
            else
                return Mathf.Lerp(0.5f, 0f, (mid - value) / (mid - end));
        }

    Tween light2;
    Tween light3;
    bool animPlayed = false;
    void ButtonPress(KeyCode[] codes)
    {
        float distance = Mathf.Abs(rect.anchoredPosition.y - hitPoint.anchoredPosition.y);

        if (distance < (GameHandler.strict?120f:180f) && (!isSustain ? GameHandler.GetKeyDown(codes) : GameHandler.GetKey(codes)))
        {
            if (!isSustain)
            {
                GetComponent<Image>().color = new Color(1, 1, 1, 0);
                HitSoundNow();
                PlayAnimPlayer(direction, false);
                Destroy(this.gameObject);
            }
            if (isSustain)
            {
                RectTransform rt = GetComponent<RectTransform>();

                float sustainHeight = rt.sizeDelta.y;

                if ((pointReached ? -Vector2.Distance(hitPoint.anchoredPosition, rt.anchoredPosition) : Vector2.Distance(hitPoint.anchoredPosition, rt.anchoredPosition)) < sustainHeight / 2)
                {
                    GetComponent<Image>().fillAmount = MapStartEnd(pointReached ? -Vector2.Distance(hitPoint.anchoredPosition, rt.anchoredPosition) : Vector2.Distance(hitPoint.anchoredPosition, rt.anchoredPosition), rt.sizeDelta.y / 2, -(rt.sizeDelta.y / 2));
                }
                if (!animPlayed)
                {
                    if (FNFNote.coolDownSinging <= 0.0f && FNFNote.animationNameCheckPlayer != direction)
                    {
                        PlayAnimPlayer(direction, false);
                        FNFNote.animationNameCheckPlayer = direction;
                        FNFNote.coolDownSinging = FNFNote.coolDownSingingMax;
                    }
                    if (FNFNote.coolDownSinging <= 0.0f)
                        FNFNote.animationNameCheckPlayer = "";
                }
                animPlayed = true;
            }

            if (GameHandler.GetKeyDown(codes) && !FNFNote.lightPlayed[FNFNote.GetIndexByDir(direction)])
            {
                FNFNote.instance.Notelight1(direction, hitPoint);
                FNFNote.lightPlayed[FNFNote.GetIndexByDir(direction)] = true;
            }
            FNFNote.instance.GoodHit(direction);
            //Destroy(gameObject);
        }
        if (distance < (GameHandler.strict?120f:180f) && GameHandler.GetKeyUp(codes) && isSustain && GameHandler.forgivenSustain)
        {
            if (GetComponent<Image>().fillAmount < 1.0f)
                Destroy(this.gameObject);
        }

    }

    void HitSoundNow()
    {
        if (FNFNote.instance.hitSound)
            GlobalAudio.instance.PlayAudio("hitsound");
    }
    // void AnimToPlayPlayer(string direction)
    // {
    //     switch (direction)
    //     {
    //         case "left": PlayAnimPlayer("left", false); break;
    //         case "right": PlayAnimPlayer("left", false); break;
    //         case "up": PlayAnimPlayer("left", false); break;
    //         case "down": PlayAnimPlayer("left", false); break;
    //     }
    // }

    void PlayAnimPlayer(string name, bool loop)
    {
        // playerSide.GetComponent<XmlSpriteAnimator>().PlayAnimationSing(name,loop);
        // playerSide.GetComponent<XmlSpriteAnimatorBinary>().PlayAnimationSing(name,loop);
        if (playerSide.TryGetComponent<XmlSpriteAnimatorBinary>(out var binary))
            binary.PlayAnimationSing(name, loop);
        if (playerSide.TryGetComponent<XmlSpriteAnimator>(out var xml))
            xml.PlayAnimationSing(name, loop);
        FNFNote.playerSingDuration = FNFNote.playerSingDurationMax;
    }
    void OnDestroy()
    {

        if (side == "player")
        {
            if (pointReached && !isSustain && !noteMiss)
                print((song.time - hitTime) * 1000);
            if (noteMiss && FNFNote.playerMissDuration < 0.0f)
            {
                PlayAnimPlayer(direction+"Miss", false);
                FNFNote.instance.MissHit(direction);
            }
        }
        // FNFNote.nextNoteIndex++;
    }
    void ObjectLookAt2D(GameObject obj, GameObject target,int increaseBy)
    {
        if (obj == null || target == null)
            return;

        Vector2 direction = (target.transform.position - obj.transform.position).normalized;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        obj.transform.rotation = Quaternion.Euler(0f, 0f, angle+increaseBy);
    }
}

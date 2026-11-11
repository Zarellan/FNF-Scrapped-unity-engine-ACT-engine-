using UnityEngine;
using UnityEngine.UI;

public class NoteSustainAdv : MonoBehaviour
{
    private AudioSource song;
    private RectTransform rect;
    private RectTransform spawnPoint;
    private RectTransform hitPoint;

    private float hitTime;
    private float travelTime;
    private float scrollSpeed;
    private string direction;
    private bool isSustain;

    public void Initialize(AudioSource songSource, RectTransform spawn, RectTransform hit, float timeToHit, float travel, string direction)
    {
        song = songSource;
        spawnPoint = spawn;
        hitPoint = hit;
        hitTime = timeToHit;
        travelTime = travel;
        this.direction = direction;
        rect = GetComponent<RectTransform>();
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

        float distance = Mathf.Abs(spawnPoint.anchoredPosition.y - hitPoint.anchoredPosition.y);
        float travelTime = distance / scrollSpeed;

        float currentTime = song.time;
        float t = 1f - ((hitTime - currentTime) / travelTime);
        rect.anchoredPosition = Vector2.LerpUnclamped(spawnPoint.anchoredPosition, hitPoint.anchoredPosition, t);

        switch (direction)
        {
            case "left":  ButtonPress(KeyCode.LeftArrow);  break;
            case "right": ButtonPress(KeyCode.RightArrow); break;
            case "up":    ButtonPress(KeyCode.UpArrow);    break;
            case "down":  ButtonPress(KeyCode.DownArrow);  break;
        }

        // Optionally remove note after passing hit point
        // if (t >= 1f) Destroy(gameObject);
    }

    void ButtonPress(KeyCode code)
    {
        float distance = Mathf.Abs(rect.anchoredPosition.y - hitPoint.anchoredPosition.y);

        if (distance < 120f && (!isSustain ? Input.GetKeyDown(code) : Input.GetKey(code)))
        {
            Debug.Log("Note Hit! " + direction);
            GetComponent<Image>().color = new Color(1,1,1,0);
            //Destroy(gameObject);
        }
    }
}

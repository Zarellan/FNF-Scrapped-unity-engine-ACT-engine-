using UnityEngine;

public class NoteSustain : MonoBehaviour
{
    private AudioSource song;
    private float startTime;
    private float holdLength;
    private float scrollSpeed;
    private string direction;
    private RectTransform rect;
    private RectTransform spawnPoint;
    private RectTransform hitPoint;

    private bool load = false;
    private bool keyPressed = false;

    public void Initialize(AudioSource songSource, float start, float length, string dir, float speed, RectTransform hit, RectTransform spawn)
    {
        song = songSource;
        startTime = start;
        holdLength = length;
        direction = dir;
        scrollSpeed = speed;
        hitPoint = hit;
        spawnPoint = spawn;
        rect = GetComponent<RectTransform>();
    }
    float sustainHeightTo;
    void Update()
    {
        if (song == null || rect == null) return;

        float distance = Mathf.Abs(spawnPoint.anchoredPosition.y - hitPoint.anchoredPosition.y);
        float travelTime = distance / scrollSpeed;
        float currentTime = song.time;

        // Calculate top position
        Vector2 topPosition;

        if (keyPressed)
        {
            // Snap top position to hit point once pressed
            topPosition = hitPoint.anchoredPosition;
        }
        else
        {
            // Normal movement from spawn to hit
            float t = 1f - ((startTime - currentTime) / travelTime);
            //t = Mathf.Clamp01(t);
            topPosition = Vector2.LerpUnclamped(spawnPoint.anchoredPosition, hitPoint.anchoredPosition, t);
        }
        
        // Calculate sustain height based on remaining hold time
        float pixelsPerSecond = distance / travelTime;
        float elapsedHold = Mathf.Clamp(currentTime - startTime, 0f, holdLength);
        float remainingHold = holdLength - elapsedHold;
        float sustainHeight = remainingHold * pixelsPerSecond;

        // Initialize size only once
        if (!load)
        {
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, sustainHeight);
            load = true;
        }

        rect.anchoredPosition = topPosition;

        // Input handling
        if (currentTime >= startTime && currentTime <= startTime + holdLength)
        {
            KeyCode expectedKey = KeyCode.LeftArrow;
            switch (direction)
            {
                case "right": expectedKey = KeyCode.RightArrow; break;
                case "up": expectedKey = KeyCode.UpArrow; break;
                case "down": expectedKey = KeyCode.DownArrow; break;
                case "left": expectedKey = KeyCode.LeftArrow; break;
            }

            if (Input.GetKey(expectedKey))
            {
                keyPressed = true; // Mark that key is pressed
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, sustainHeight);
                sustainHeightTo += sustainHeight;
                Debug.Log($"Holding sustain note: {direction}");
            }
            else
            {
                keyPressed = false;
            }
        }

        // Auto-destroy after sustain ends
        if (currentTime > startTime + holdLength)
        {
            Destroy(gameObject);
        }
    }
}

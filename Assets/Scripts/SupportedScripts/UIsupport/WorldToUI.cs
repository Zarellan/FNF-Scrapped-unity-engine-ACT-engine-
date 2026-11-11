using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

public class WorldToUI : MonoBehaviour
{
    [Header("References")]
    public Transform target;
    public Camera cam;
    public RectTransform imag;
    public Canvas canvas;

    void Start()
    {
        if (cam == null)
            cam = Camera.main;

        if (canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera != cam)
            canvas.worldCamera = cam;

        // Ensure the UI image is anchored at the center (prevents offset issues)
        if (imag != null)
        {
            imag.anchorMin = imag.anchorMax = new Vector2(0.5f, 0.5f);
            imag.pivot = new Vector2(0.5f, 0.5f);
        }
    }

    void Update()
    {
        if (target == null || imag == null || cam == null || canvas == null)
            return;

        Vector3 screenPos = cam.WorldToScreenPoint(target.position);

        if (screenPos.z < 0)
        {
            imag.gameObject.SetActive(false);
            return;
        }

        imag.gameObject.SetActive(true);

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            screenPos,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : cam,
            out localPoint
        );

        // Apply the position to the UI image
        imag.anchoredPosition = localPoint;
    }

    public void SetPos(RectTransform imag, Transform target)
    {
        if (target == null || imag == null || cam == null || canvas == null)
            return;

        Vector3 screenPos = cam.WorldToScreenPoint(target.position);

        if (screenPos.z < 0)
        {
            imag.gameObject.SetActive(false);
            return;
        }

        imag.gameObject.SetActive(true);

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            screenPos,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : cam,
            out localPoint
        );

        // Apply the position to the UI image
        imag.anchoredPosition = localPoint;
    }

    public void SetPosTween(RectTransform imag, Transform target, float duration, Ease easeType)
    {
        if (target == null || imag == null || cam == null || canvas == null)
            return;

        Vector3 screenPos = cam.WorldToScreenPoint(target.position);

        if (screenPos.z < 0)
        {
            imag.gameObject.SetActive(false);
            return;
        }

        imag.gameObject.SetActive(true);

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            screenPos,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : cam,
            out localPoint
        );

        Tween.UIAnchoredPosition(imag,localPoint,duration,easeType);
    }
}

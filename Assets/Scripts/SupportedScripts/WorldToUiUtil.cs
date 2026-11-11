using UnityEngine;
using PrimeTween;
public static class WorldToUiUtil
{
    public static void SetPos(RectTransform imag, Transform target)
    {
        GameObject.FindWithTag("WorldToUI").GetComponent<WorldToUI>().SetPos(imag, target);
    }
    public static void SetPosTween(RectTransform imag,Transform target,float duration, Ease easeType)
    {
        GameObject.FindWithTag("WorldToUI").GetComponent<WorldToUI>().SetPosTween(imag,target,duration,easeType);
    }
}

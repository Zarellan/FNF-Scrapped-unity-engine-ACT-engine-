using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class FNFChartEditorExtra : MonoBehaviour
{
    bool released = false;
    [SerializeField] GameObject UIObject;
    [SerializeField] float release, unrelease;
    Tween releaseUITween;
    void Start()
    {

    }

    public void ButtonRelease()
    {
        if (releaseUITween.isAlive)
            releaseUITween.Stop();
        if (!released)
        {
            releaseUITween = Tween.UIAnchoredPositionX(UIObject.GetComponent<RectTransform>(), release, 0.3f, Ease.OutCirc);
            released = true;
        }
        else
        {
            releaseUITween = Tween.UIAnchoredPositionX(UIObject.GetComponent<RectTransform>(), unrelease, 0.3f, Ease.OutCirc);
            released = false;
        }
    }
    [SerializeField] TMP_InputField copyPrevSection;
    public void AddByPreviousField(int num)
    {
        int temporaryNum = copyPrevSection.text == "" ? 1 : int.Parse(copyPrevSection.text);
        temporaryNum += num;
        copyPrevSection.text = temporaryNum.ToString();
    }
}

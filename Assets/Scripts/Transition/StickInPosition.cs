using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class StickInPosition : MonoBehaviour
{
    Vector2 position;
    [SerializeField] Vector2 size;

    void Start()
    {
    }
    void LateUpdate()
    {
        GetComponent<RectTransform>().position = position;
        GetComponent<RectTransform>().rotation = Quaternion.Euler(0,0,0);
        GetComponent<RectTransform>().sizeDelta = size;
    }
}

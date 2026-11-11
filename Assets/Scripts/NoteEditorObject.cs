using UnityEngine;

public class NoteEditorObject : MonoBehaviour
{
    public NoteData noteData; // 🔥 attach the actual data

    bool collidedObj;

    void Update()
    {
        bool isFree = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        if (collidedObj && !isFree)
        {
            FNFChartEditor.isCollidingNote = true;
            // 🔥 tell editor which note was clicked
            FNFChartEditor.clickedNote = noteData;
        }
        if (!FNFChartEditor.isCollidingNote)
        {
            FNFChartEditor.clickedNote = null;
        }
    }

    void OnMouseOver()
    {
        collidedObj = true;
        FNFChartEditor.isCollidingNote = true;
    }

    void OnMouseExit()
    {
        collidedObj = false;
        FNFChartEditor.isCollidingNote = false;
    }
}
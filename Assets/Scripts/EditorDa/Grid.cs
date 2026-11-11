using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Grid
{
    public int width, height;
    public float size;
    int[,] gridArray;
    public delegate GameObject CreateText(Vector3 vec, float size);
    private GameObject[,] textMesh;
    public Vector3 originPosition;

    public Grid(int width, int height, float size, Vector3 originPosition, CreateText create)
    {
        this.width = width;
        this.height = height;
        this.size = size;
        this.originPosition = originPosition;
        gridArray = new int[width, height];
        textMesh = new GameObject[width, height];

        for (int i = 0; i < gridArray.GetLength(0); i++)
        {
            for (int j = 0; j < gridArray.GetLength(1); j++)
            {
                textMesh[i, j] = create(WorldPositionWithSize(i, j, this.size), size);
            }
        }
    }
    public Vector2Int GetGridPosition(Vector3 worldPosition)
    {
        int x = Mathf.FloorToInt((worldPosition - originPosition).x / size);
        int y = Mathf.FloorToInt((worldPosition - originPosition).y / size);
        return new Vector2Int(x, y);
    }

    Vector3 WorldPositionWithSize(float x, float y, float size)
    {
        return new Vector3(x + 0.5f, y + 0.5f) * size + originPosition;
    }

    // -------------------------------
    // NEW PUBLIC METHOD
    // -------------------------------
    public Vector3 WorldPosition(int x, int y)
    {
        return WorldPositionWithSize(x, y, size);
    }


    public void GetXY(Vector3 worldPosition, out int x, out int y)
    {
        x = Mathf.FloorToInt((worldPosition - originPosition).x / size);
        y = Mathf.FloorToInt((worldPosition - originPosition).y / size);
    }

    public void SetValue(int x, int y, int value)
    {
        if (x >= 0 && y >= 0 && x < width && y < height)
        {
            gridArray[x, y] = value;
            TMP_Text childText = textMesh[x, y].transform.Find("Canvas").transform.Find("Text").GetComponentInChildren<TMP_Text>();
            childText.text = value.ToString();
        }
    }

    public int GetValue(int x, int y)
    {
        if (x >= 0 && y >= 0 && x < width && y < height)
            return gridArray[x, y];
        else
            return 0;
    }

    public void SetValue(Vector3 worldPosition, int value)
    {
        int x, y;
        GetXY(worldPosition, out x, out y);
        SetValue(x, y, value);
    }

    public int GetValue(Vector3 worldPosition)
    {
        int x, y;
        GetXY(worldPosition, out x, out y);
        return GetValue(x, y);
    }

    // -------------------------------
    // ResizeGrid
    // -------------------------------
    public void ResizeGrid(int newWidth, int newHeight, CreateText create)
    {
        int[,] newGridArray = new int[newWidth, newHeight];
        GameObject[,] newTextMesh = new GameObject[newWidth, newHeight];
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (x >= newWidth || y >= newHeight)
                {
                    if (textMesh[x, y] != null)
                        GameObject.Destroy(textMesh[x, y]);
                }
            }
        }

        // Copy existing data
        for (int x = 0; x < Mathf.Min(width, newWidth); x++)
        {
            for (int y = 0; y < Mathf.Min(height, newHeight); y++)
            {
                newGridArray[x, y] = gridArray[x, y];
                newTextMesh[x, y] = textMesh[x, y];
            }
        }

        // Create new text objects for added cells
        for (int x = 0; x < newWidth; x++)
        {
            for (int y = 0; y < newHeight; y++)
            {
                if (newTextMesh[x, y] == null)
                {
                    Vector3 worldPos = WorldPositionWithSize(x, y, size);
                    newTextMesh[x, y] = create(worldPos, size);
                }
            }
        }

        gridArray = newGridArray;
        textMesh = newTextMesh;
        width = newWidth;
        height = newHeight;
    }
    public Vector3 WorldPositionForNote(int column, float time, float secondsPerRow)
    {
        float row = time / secondsPerRow;
        return originPosition + new Vector3(column + 0.5f, row + 0.5f, 0) * size;
    }
    public Vector3 WorldPositionByTime(int column, float time, float secondsPerRow)
    {
        float rowF = time / secondsPerRow; // fractional row
        return originPosition + new Vector3((column + 0.5f) * size, (rowF + 0.5f) * size, 0);
    }
    public float TimeFromWorldY(float worldY, float secondsPerRow)
    {
        float localY = worldY - originPosition.y;
        return localY / size * secondsPerRow; // fractional row → time
    }
    public Vector3 WorldPositionForNoteExact(NoteData note, float secondsPerRow)
    {
        int visualColumn = (note.side == NoteData.Owner.Player) ? note.column : note.column + 4;
        return WorldPositionByTime(visualColumn, note.time, secondsPerRow);
    }

}

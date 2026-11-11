using System.IO;
using UnityEngine;

public class PngToDatConverter : MonoBehaviour
{
    [Header("PNG file name inside StreamingAssets (example: test.png)")]
    public string pngFileName = "test.png";

    [Header("Output .dat file name")]
    public string outputDatName = "texture.dat";

    [ContextMenu("Convert PNG → DAT (StreamingAssets)")]
    public void Convert()
    {
        // =========================
        // INPUT PATH (StreamingAssets)
        // =========================
        string inputPath = Path.Combine(Application.streamingAssetsPath, pngFileName);

        if (!File.Exists(inputPath))
        {
            Debug.LogError("PNG not found in StreamingAssets: " + inputPath);
            return;
        }

        // =========================
        // READ PNG
        // =========================
        byte[] pngBytes = File.ReadAllBytes(inputPath);

        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);

        if (!tex.LoadImage(pngBytes))
        {
            Debug.LogError("Failed to load PNG image");
            return;
        }

        // =========================
        // CONVERT → .DAT
        // =========================
        byte[] datBytes = FastTextureSerializer.Save(
            tex,
            FastTextureSerializer.CompressionType.GZip
        );

        // =========================
        // OUTPUT PATH (StreamingAssets)
        // =========================
        string outputPath = Path.Combine(Application.streamingAssetsPath, outputDatName);

        File.WriteAllBytes(outputPath, datBytes);

        Debug.Log("✔ Converted PNG → DAT");
        Debug.Log("Input : " + inputPath);
        Debug.Log("Output: " + outputPath);
    }
}
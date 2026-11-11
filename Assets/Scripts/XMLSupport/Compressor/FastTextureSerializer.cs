using System;
using System.IO;
using System.IO.Compression;
using UnityEngine;

public static class FastTextureSerializer
{
    private const string MAGIC = "TEX0";
    private const int VERSION = 1;

    public enum CompressionType
    {
        None,
        GZip
    }

    // =========================
    // SAVE
    // =========================
    public static byte[] Save(Texture2D texture, CompressionType compression = CompressionType.GZip)
    {
        if (texture == null)
            throw new Exception("Texture is null");

        // Force safe format (VERY IMPORTANT)
        Texture2D readableTex = ConvertToRGBA32(texture);
        byte[] rawData = readableTex.GetRawTextureData();

        byte compressionByte = (byte)compression;
        byte[] finalData;

        if (compression == CompressionType.GZip)
            finalData = CompressGZip(rawData);
        else
            finalData = rawData;

        using (MemoryStream ms = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(ms))
        {
            // HEADER
            writer.Write(MAGIC);                 // 4 bytes magic
            writer.Write(VERSION);               // version
            writer.Write(texture.width);
            writer.Write(texture.height);
            writer.Write(compressionByte);
            writer.Write(finalData.Length);

            // DATA
            writer.Write(finalData);

            return ms.ToArray();
        }
    }
    private static Texture2D ConvertToRGBA32(Texture2D source)
    {
        RenderTexture rt = RenderTexture.GetTemporary(
            source.width,
            source.height,
            0,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.sRGB // IMPORTANT FIX
        );

        Graphics.Blit(source, rt);

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D tex = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        // tex.Apply();
        tex.Apply(false, false);

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);

        return tex;
    }
    // =========================
    // LOAD
    // =========================
    public static Texture2D Load(byte[] fileData)
    {
        using (MemoryStream ms = new MemoryStream(fileData))
        using (BinaryReader reader = new BinaryReader(ms))
        {
            string magic = reader.ReadString();
            if (magic != MAGIC)
                throw new Exception("Invalid texture format");

            int version = reader.ReadInt32();

            int width = reader.ReadInt32();
            int height = reader.ReadInt32();

            CompressionType compression = (CompressionType)reader.ReadByte();
            int dataLength = reader.ReadInt32();

            byte[] data = reader.ReadBytes(dataLength);

            if (compression == CompressionType.GZip)
                data = DecompressGZip(data);

            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
            tex.LoadRawTextureData(data);
            // tex.Apply();
            tex.Apply(false, false);

            return tex;
        }
    }

    // =========================
    // HELPERS
    // =========================
    private static Texture2D EnsureReadable(Texture2D source)
    {
        RenderTexture rt = RenderTexture.GetTemporary(
            source.width,
            source.height,
            0,
            RenderTextureFormat.Default,
            RenderTextureReadWrite.Linear
        );

        Graphics.Blit(source, rt);

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D tex = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        // tex.Apply();
        tex.Apply(false, false);
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);

        return tex;
    }

    // =========================
    // GZIP
    // =========================
    private static byte[] CompressGZip(byte[] data)
    {
        using (MemoryStream output = new MemoryStream())
        {
            using (GZipStream gzip = new GZipStream(output, System.IO.Compression.CompressionLevel.Optimal))
            {
                gzip.Write(data, 0, data.Length);
            }
            return output.ToArray();
        }
    }

    private static byte[] DecompressGZip(byte[] data)
    {
        using (MemoryStream input = new MemoryStream(data))
        using (GZipStream gzip = new GZipStream(input, CompressionMode.Decompress))
        using (MemoryStream output = new MemoryStream())
        {
            gzip.CopyTo(output);
            return output.ToArray();
        }
    }
}
using System;
using System.IO;
using System.Xml;
using UnityEngine;

public static class XmlToAtlasBinaryConverter
{
    public static void Convert(string xmlFileName, string outputFileName)
    {
        // =========================
        // INPUT PATH (StreamingAssets XML)
        // =========================
        string inputPath = Path.Combine(Application.streamingAssetsPath, xmlFileName + ".xml");

        if (!File.Exists(inputPath))
        {
            Debug.LogError("XML not found: " + inputPath);
            return;
        }

        XmlDocument doc = new XmlDocument();
        doc.Load(inputPath);

        XmlNodeList nodes = doc.GetElementsByTagName("SubTexture");

        // =========================
        // OUTPUT PATH (Persistent OR StreamingAssets warning)
        // =========================
        string outPath = Path.Combine(Application.streamingAssetsPath, outputFileName + ".atlas.dat");

        Directory.CreateDirectory(Path.GetDirectoryName(outPath));

        using (BinaryWriter writer = new BinaryWriter(File.Open(outPath, FileMode.Create)))
        {
            // HEADER
            writer.Write(nodes.Count);

            foreach (XmlNode node in nodes)
            {
                string name = node.Attributes["name"].Value;

                short x = short.Parse(node.Attributes["x"].Value);
                short y = short.Parse(node.Attributes["y"].Value);
                short w = short.Parse(node.Attributes["width"].Value);
                short h = short.Parse(node.Attributes["height"].Value);

                short fx = node.Attributes["frameX"] != null ? short.Parse(node.Attributes["frameX"].Value) : (short)0;
                short fy = node.Attributes["frameY"] != null ? short.Parse(node.Attributes["frameY"].Value) : (short)0;
                short fw = node.Attributes["frameWidth"] != null ? short.Parse(node.Attributes["frameWidth"].Value) : w;
                short fh = node.Attributes["frameHeight"] != null ? short.Parse(node.Attributes["frameHeight"].Value) : h;

                // =========================
                // SAFE STRING WRITE
                // =========================
                writer.Write(name);

                // =========================
                // FRAME DATA
                // =========================
                writer.Write(x);
                writer.Write(y);
                writer.Write(w);
                writer.Write(h);
                writer.Write(fx);
                writer.Write(fy);
                writer.Write(fw);
                writer.Write(fh);
            }
        }

        Debug.Log($"✔ Converted XML → DAT: {outPath}");
    }
}
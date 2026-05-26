using UnityEngine;
using System.IO;

/// <summary>
/// Utility functions to save/load Texture2D as PNG to persistent path.
/// Use for storing last drawn/colored textures between scenes.
/// </summary>
public static class TextureUtils
{
    /// <summary>
    /// Saves texture to persistentDataPath/fileName (PNG). Returns full path or null on error.
    /// </summary>
    public static string SaveTexturePNG(Texture2D tex, string fileName)
    {
        if (tex == null) return null;
        try
        {
            byte[] bytes = tex.EncodeToPNG();
            string dir = Path.Combine(Application.persistentDataPath, "textures");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, fileName);
            File.WriteAllBytes(path, bytes);
            return path;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"TextureUtils.SaveTexturePNG error: {ex}");
            return null;
        }
    }

    /// <summary>
    /// Loads a PNG file into a new Texture2D. Returns null on error.
    /// </summary>
    public static Texture2D LoadTexturePNG(string fullPath)
    {
        if (!File.Exists(fullPath)) { Debug.LogWarning($"Texture file not found: {fullPath}"); return null; }
        try
        {
            byte[] bytes = File.ReadAllBytes(fullPath);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(bytes);
            return tex;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"TextureUtils.LoadTexturePNG error: {ex}");
            return null;
        }
    }

    /// <summary>
    /// Creates a deep-clone of Texture2D (readable) — helps to avoid texture import/read-only issues.
    /// </summary>
    public static Texture2D CloneReadable(Texture2D source)
    {
        if (source == null) return null;
        RenderTexture rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
        Graphics.Blit(source, rt);
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        readable.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        readable.Apply();
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);
        return readable;
    }
}

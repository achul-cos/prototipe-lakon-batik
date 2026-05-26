using UnityEngine;

/// <summary>
/// Helper untuk pemetaan warna logis (BatikColor) ↔ nilai RGB.
/// Digunakan oleh DyeingManager & ResultManager.
/// </summary>
public static class ColorUtils
{
    public static Color ToRGB(BatikColor c)
    {
        switch (c)
        {
            case BatikColor.Merah: return new Color(0.85f, 0.15f, 0.15f);
            case BatikColor.Hijau: return new Color(0.20f, 0.75f, 0.30f);
            case BatikColor.Biru: return new Color(0.20f, 0.40f, 0.85f);
            case BatikColor.Kuning: return new Color(0.95f, 0.85f, 0.20f);
            case BatikColor.Ungu: return new Color(0.55f, 0.25f, 0.75f);
            case BatikColor.Oranye: return new Color(0.95f, 0.55f, 0.15f);
        }
        return Color.white;
    }

    /// <summary>
    /// Mengukur kemiripan dua warna (0..1). 1 = identik.
    /// Memakai Euclidean distance di ruang RGB sederhana.
    /// </summary>
    public static float Similarity(Color a, Color b)
    {
        float dr = a.r - b.r;
        float dg = a.g - b.g;
        float db = a.b - b.b;
        float dist = Mathf.Sqrt(dr * dr + dg * dg + db * db);
        float maxDist = Mathf.Sqrt(3f); // jarak maksimum di RGB unit cube
        return Mathf.Clamp01(1f - dist / maxDist);
    }
}
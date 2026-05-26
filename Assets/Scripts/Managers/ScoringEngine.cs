using UnityEngine;

/// <summary>
/// Mesin scoring terpusat. Memisahkan logika perhitungan dari UI agar mudah di-test
/// dan diubah formulanya tanpa menyentuh ResultManager.
/// 
/// Formula final score:
///   raw  = (drawing * 0.55) + (color * 0.30) + (dryQuality * 0.15)
///   final = raw * difficultyMultiplier
/// 
/// Tier reward berdasarkan finalScore:
///   < 50%    → 50% harga, protes keras
///   50–74%   → 100% harga, kritik halus
///   75–89%   → 100% harga, terima kasih
///   ≥ 90%    → 120% harga, pujian
/// </summary>
public static class ScoringEngine
{
    public enum ResultTier { Bad, Mediocre, Good, Excellent }

    public class ScoreResult
    {
        public float drawingScore;
        public float colorScore;
        public float dryQuality;       // 0..100 (opacity * 100)
        public float finalScore;       // 0..100
        public ResultTier tier;
        public int payment;
        public string customerResponse;
    }

    public static ScoreResult Compute(
        float drawingAccuracy,   // 0..100
        float colorAccuracy,     // 0..100
        float dryOpacity,        // 0..1
        BatikPattern pattern)
    {
        var r = new ScoreResult();
        r.drawingScore = Mathf.Clamp(drawingAccuracy, 0f, 100f);
        r.colorScore = Mathf.Clamp(colorAccuracy, 0f, 100f);
        r.dryQuality = Mathf.Clamp01(dryOpacity) * 100f;

        float raw = r.drawingScore * 0.55f
                  + r.colorScore * 0.30f
                  + r.dryQuality * 0.15f;

        float diff = pattern != null ? pattern.difficultyMultiplier : 1f;
        // difficultyMultiplier > 1 berarti pola sulit → bonus skor
        r.finalScore = Mathf.Clamp(raw * Mathf.Lerp(1f, diff, 0.5f), 0f, 100f);

        // Tentukan tier & bayaran
        int basePrice = pattern != null ? pattern.basePrice : 50000;
        if (r.finalScore < 50f)
        {
            r.tier = ResultTier.Bad;
            r.payment = Mathf.RoundToInt(basePrice * 0.5f);
            r.customerResponse = BuildResponse(r.tier, r);
        }
        else if (r.finalScore < 75f)
        {
            r.tier = ResultTier.Mediocre;
            r.payment = basePrice;
            r.customerResponse = BuildResponse(r.tier, r);
        }
        else if (r.finalScore < 90f)
        {
            r.tier = ResultTier.Good;
            r.payment = basePrice;
            r.customerResponse = BuildResponse(r.tier, r);
        }
        else
        {
            r.tier = ResultTier.Excellent;
            r.payment = Mathf.RoundToInt(basePrice * 1.2f);
            r.customerResponse = BuildResponse(r.tier, r);
        }

        return r;
    }

    /// <summary>
    /// Bangun respons pelanggan dengan kritik konkret pada aspek yang lemah.
    /// </summary>
    private static string BuildResponse(ResultTier tier, ScoreResult r)
    {
        string weakest = GetWeakestAspect(r);
        switch (tier)
        {
            case ResultTier.Bad:
                return $"Aduh... ini tidak seperti yang saya bayangkan. " +
                       $"Bagian {weakest} sangat mengecewakan. " +
                       $"Saya hanya bisa bayar separuh harga.";
            case ResultTier.Mediocre:
                return $"Lumayan, tapi {weakest}-nya masih kurang. " +
                       $"Mungkin lain kali bisa lebih teliti ya.";
            case ResultTier.Good:
                return $"Terima kasih banyak! Batiknya bagus, " +
                       $"meski {weakest}-nya bisa lebih sempurna.";
            case ResultTier.Excellent:
                return $"Luar biasa! Karya yang sungguh indah. " +
                       $"Ini bonus dari saya sebagai apresiasi.";
        }
        return string.Empty;
    }

    private static string GetWeakestAspect(ScoreResult r)
    {
        float d = r.drawingScore;
        float c = r.colorScore;
        float dr = r.dryQuality;
        if (d <= c && d <= dr) return "pola gambar";
        if (c <= d && c <= dr) return "pemilihan warna";
        return "proses penjemuran";
    }
}
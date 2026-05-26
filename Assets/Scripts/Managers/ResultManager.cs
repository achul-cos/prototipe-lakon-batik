using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Menampilkan hasil akhir membatik: skor, respons pelanggan, dan pembayaran.
/// Setelah ditutup, kembali ke scene Lobby untuk melayani pelanggan berikutnya.
/// </summary>
public class ResultManager : Singleton<ResultManager>
{
    protected override bool PersistBetweenScenes => false;

    [Header("UI")]
    public RawImage finalBatikPreview;
    public TMP_Text drawingScoreText;
    public TMP_Text colorScoreText;
    public TMP_Text dryScoreText;
    public TMP_Text finalScoreText;
    public TMP_Text tierLabel;
    public TMP_Text customerResponseText;
    public TMP_Text paymentText;
    public Button btnContinue;
    public Image tierBadge;

    [Header("Tier Colors")]
    public Color colorBad = new Color(0.85f, 0.25f, 0.25f);
    public Color colorMediocre = new Color(0.90f, 0.65f, 0.25f);
    public Color colorGood = new Color(0.30f, 0.75f, 0.40f);
    public Color colorExcellent = new Color(0.95f, 0.80f, 0.20f);

    private void Start()
    {
        // Tampilkan texture final
        Texture2D tex = BatikDatabase.Instance.lastColoredTexture;
        if (tex != null) finalBatikPreview.texture = tex;

        // Hitung skor
        var order = GameManager.Instance.currentOrder;
        var result = ScoringEngine.Compute(
            GameManager.Instance.batikAccuracy,
            GameManager.Instance.colorAccuracy,
            GameManager.Instance.dryOpacity,
            order.desiredPattern);

        StartCoroutine(AnimateResult(result));

        btnContinue.onClick.AddListener(ReturnToLobby);
    }

    /// <summary>
    /// Animasi ringan: angka skor "menghitung naik" agar terasa rewarding.
    /// </summary>
    private IEnumerator AnimateResult(ScoringEngine.ScoreResult r)
    {
        float dur = 1.2f;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / dur);
            drawingScoreText.text = $"Pola: {r.drawingScore * k:F1}%";
            colorScoreText.text = $"Warna: {r.colorScore * k:F1}%";
            dryScoreText.text = $"Jemur: {r.dryQuality * k:F1}%";
            finalScoreText.text = $"{r.finalScore * k:F1}%";
            yield return null;
        }

        // Set final value & visual tier
        drawingScoreText.text = $"Pola: {r.drawingScore:F1}%";
        colorScoreText.text = $"Warna: {r.colorScore:F1}%";
        dryScoreText.text = $"Jemur: {r.dryQuality:F1}%";
        finalScoreText.text = $"{r.finalScore:F1}%";
        tierLabel.text = r.tier.ToString().ToUpper();
        tierBadge.color = TierColor(r.tier);
        customerResponseText.text = r.customerResponse;
        paymentText.text = $"Bayaran: Rp {r.payment:N0}";

        // Tambahkan uang ke save
        GameManager.Instance.AddMoney(r.payment);
    }

    private Color TierColor(ScoringEngine.ResultTier tier)
    {
        switch (tier)
        {
            case ScoringEngine.ResultTier.Bad: return colorBad;
            case ScoringEngine.ResultTier.Mediocre: return colorMediocre;
            case ScoringEngine.ResultTier.Good: return colorGood;
            case ScoringEngine.ResultTier.Excellent: return colorExcellent;
        }
        return Color.white;
    }

    private void ReturnToLobby()
    {
        GameManager.Instance.ChangeState(GameState.Lobby);
        GameManager.Instance.LoadScene("02_Lobby");
    }
}

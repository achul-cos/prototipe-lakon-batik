using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manager scene menggambar. Mengelola tombol canting, indikator coverage,
/// dan transisi ke scene pewarnaan.
/// 
/// Tidak persist antar scene (cukup di scene 04_BatikDrawing).
/// </summary>
public class BatikDrawingManager : Singleton<BatikDrawingManager>
{
    protected override bool PersistBetweenScenes => false;

    [Header("Dependency")]
    public BatikCanvas canvas;

    [Header("UI")]
    public Button btnCantingKecil;
    public Button btnCantingSedang;
    public Button btnCantingBesar;
    public Button btnFinish;
    public Slider coverageBar;
    public TMP_Text coverageLabel;

    [Range(50f, 100f)] public float finishThreshold = 80f;

    private void Start()
    {
        if (btnCantingKecil) btnCantingKecil.onClick.AddListener(() => canvas.SetCanting(CantingSize.Kecil));
        if (btnCantingSedang) btnCantingSedang.onClick.AddListener(() => canvas.SetCanting(CantingSize.Sedang));
        if (btnCantingBesar) btnCantingBesar.onClick.AddListener(() => canvas.SetCanting(CantingSize.Besar));

        if (btnFinish)
        {
            btnFinish.gameObject.SetActive(false);
            btnFinish.onClick.AddListener(FinishDrawing);
        }
    }

    /// <summary>
    /// Dipanggil dari BatikCanvas tiap update coverage.
    /// </summary>
    public void NotifyCoverage(float coverage)
    {
        if (coverageBar) coverageBar.value = coverage / 100f;
        if (coverageLabel) coverageLabel.text = $"{coverage:F1}%";
        if (btnFinish && coverage >= finishThreshold && !btnFinish.gameObject.activeSelf)
            btnFinish.gameObject.SetActive(true);
    }

    private void FinishDrawing()
    {
        float finalCoverage = canvas.CalculateCoverage();
        GameManager.Instance.batikAccuracy = finalCoverage;

        // Simpan texture untuk dipakai di scene pewarnaan
        Texture2D tex = canvas.GetCurrentDrawingTexture();
        BatikDatabase.Instance.lastDrawnTexture = tex;
        string path = TextureUtils.SaveTexturePNG(tex, "last_drawing.png");
        if (GameManager.Instance.currentSave != null)
            GameManager.Instance.currentSave.lastDrawnTextureFile = path;

        GameManager.Instance.ChangeState(GameState.Dyeing);
        GameManager.Instance.LoadScene("05_Dyeing");
    }
}
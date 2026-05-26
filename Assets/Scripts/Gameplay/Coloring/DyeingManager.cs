using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Sistem pewarnaan dengan metode celup.
/// Alur:
/// 1. Player menggeser slider R/G/B → warna campuran terlihat di gelas.
/// 2. Player atur slider "kedalaman celup" (0..1).
/// 3. Tekan tombol Celup → piksel putih di kain (dari bawah ke atas, sesuai depth)
///    diganti warna campuran. Piksel hitam (pola) tidak terpengaruh.
/// 4. Tekan Selesai → simpan texture & lanjut ke jemur.
/// </summary>
public class DyeingManager : Singleton<DyeingManager>
{
    protected override bool PersistBetweenScenes => false;

    [Header("Mix Sliders (0..1)")]
    public Slider redSlider;
    public Slider greenSlider;
    public Slider blueSlider;
    public Image waterPreview;
    public TMP_Text colorNameLabel;

    [Header("Dipping")]
    public RawImage clothPreview;
    public Slider dipDepthSlider;       // 0 = ujung kain, 1 = full
    public Button btnDip;
    public Button btnFinish;

    [Header("Threshold")]
    [Range(0.5f, 1f)] public float whiteThreshold = 0.75f; // r,g,b semua > threshold dianggap putih

    private Color _mixed;
    private Texture2D _workingTex;

    private void Start()
    {
        // Hook slider events
        redSlider.onValueChanged.AddListener(_ => RefreshMix());
        greenSlider.onValueChanged.AddListener(_ => RefreshMix());
        blueSlider.onValueChanged.AddListener(_ => RefreshMix());
        btnDip.onClick.AddListener(PerformDip);
        btnFinish.onClick.AddListener(FinishDyeing);

        // Ambil texture hasil menggambar
        Texture2D src = BatikDatabase.Instance.lastDrawnTexture;
        if (src == null)
        {
            // fallback: load dari PNG
            string path = GameManager.Instance.currentSave?.lastDrawnTextureFile;
            if (!string.IsNullOrEmpty(path)) src = TextureUtils.LoadTexturePNG(path);
        }
        if (src == null)
        {
            Debug.LogError("[DyeingManager] Tidak ada texture hasil menggambar!");
            return;
        }

        _workingTex = TextureUtils.CloneReadable(src);
        clothPreview.texture = _workingTex;
        RefreshMix();
    }

    private void RefreshMix()
    {
        _mixed = new Color(redSlider.value, greenSlider.value, blueSlider.value, 1f);
        if (waterPreview) waterPreview.color = _mixed;
        if (colorNameLabel) colorNameLabel.text = ApproximateColorName(_mixed);
    }

    private string ApproximateColorName(Color c)
    {
        BatikColor best = BatikColor.Merah;
        float bestSim = -1f;
        foreach (BatikColor bc in System.Enum.GetValues(typeof(BatikColor)))
        {
            float sim = ColorUtils.Similarity(c, ColorUtils.ToRGB(bc));
            if (sim > bestSim) { bestSim = sim; best = bc; }
        }
        return $"~ {best} ({bestSim * 100f:F0}%)";
    }

    /// <summary>
    /// Celupkan kain: ganti piksel "putih" di area bawah sesuai kedalaman.
    /// </summary>
    private void PerformDip()
    {
        if (_workingTex == null) return;
        int w = _workingTex.width;
        int h = _workingTex.height;
        int depthRows = Mathf.RoundToInt(h * Mathf.Clamp01(dipDepthSlider.value));
        if (depthRows <= 0) return;

        Color32[] pixels = _workingTex.GetPixels32();
        Color32 newCol = _mixed;

        for (int y = 0; y < depthRows; y++)
        {
            int row = y * w;
            for (int x = 0; x < w; x++)
            {
                Color32 px = pixels[row + x];
                // hanya warnai piksel yang cukup putih (bukan pola hitam)
                if (px.r / 255f >= whiteThreshold &&
                    px.g / 255f >= whiteThreshold &&
                    px.b / 255f >= whiteThreshold)
                {
                    pixels[row + x] = newCol;
                }
            }
        }
        _workingTex.SetPixels32(pixels);
        _workingTex.Apply();
    }

    /// <summary>
    /// Menyelesaikan fase pewarnaan. Menyimpan texture hasil celup ke database
    /// + file PNG untuk dipakai pada scene jemur. Menghitung akurasi warna
    /// terhadap permintaan pelanggan, lalu transisi ke scene drying.
    /// </summary>
    private void FinishDyeing()
    {
        if (_workingTex == null)
        {
            Debug.LogError("[DyeingManager] Working texture NULL saat finish.");
            return;
        }

        // 1. Hitung kemiripan warna campuran dengan warna yang diinginkan pelanggan
        BatikColor desired = GameManager.Instance.currentOrder.desiredColor;
        Color desiredRGB = ColorUtils.ToRGB(desired);
        float similarity = ColorUtils.Similarity(_mixed, desiredRGB); // 0..1
        GameManager.Instance.colorAccuracy = similarity * 100f;

        // 2. Simpan texture sebagai referensi untuk scene berikutnya
        BatikDatabase.Instance.lastColoredTexture = _workingTex;
        TextureUtils.SaveTexturePNG(_workingTex, "last_colored.png");

        Debug.Log($"[DyeingManager] Color accuracy: {GameManager.Instance.colorAccuracy:F1}%");

        // 3. Lanjut ke scene menjemur
        GameManager.Instance.ChangeState(GameState.Drying);
        GameManager.Instance.LoadScene("06_Drying");
    }

    /// <summary>
    /// Reset texture ke kondisi awal (sebelum dicelup). Berguna jika player ingin
    /// mengulang pencelupan. Bisa di-hook ke tombol "Reset" di UI.
    /// </summary>
    public void ResetCloth()
    {
        Texture2D src = BatikDatabase.Instance.lastDrawnTexture;
        if (src == null) return;
        if (_workingTex != null) Destroy(_workingTex);
        _workingTex = TextureUtils.CloneReadable(src);
        clothPreview.texture = _workingTex;
    }
}
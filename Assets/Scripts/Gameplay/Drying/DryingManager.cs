using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Scene menjemur batik.
/// 
/// Aturan:
/// - Max dry time: 10 detik dunia nyata.
/// - Ideal window: ±0.75 detik di sekitar 5 detik (4.25s – 5.75s) → opacity 100%.
/// - Terlalu cepat (< 4.25s): kain masih basah → opacity menurun (warna lebih pucat).
/// - Terlalu lama (> 5.75s): kain over-dry → opacity & saturasi menurun bertahap.
/// - Jika tidak diangkat sampai 10 detik, otomatis selesai dengan nilai terburuk.
/// 
/// Hasil disimpan di GameManager.dryOpacity (0..1) dan dipakai ScoringEngine.
/// </summary>
public class DryingManager : Singleton<DryingManager>
{
    protected override bool PersistBetweenScenes => false;

    [Header("UI")]
    public RawImage clothPreview;
    public Slider timerBar;
    public TMP_Text timerLabel;
    public TMP_Text statusLabel;
    public Button btnTakeCloth;
    public Image idealZoneIndicator; // visual zona ideal (opsional)

    [Header("Timing (real-time seconds)")]
    public float maxDryTime = 10f;
    public float idealTime = 5f;
    public float idealWindow = 0.75f; // ±0.75s di sekitar idealTime

    [Header("Visual Tweak")]
    [Range(0.2f, 1f)] public float minOpacity = 0.4f;

    private float _elapsed;
    private bool _taken;
    private Texture2D _displayTex;
    private Color32[] _basePixels; // snapshot awal untuk efek pudar

    private void Start()
    {
        // Ambil texture hasil celup
        Texture2D src = BatikDatabase.Instance.lastColoredTexture;
        if (src == null)
        {
            string path = System.IO.Path.Combine(Application.persistentDataPath, "textures", "last_colored.png");
            src = TextureUtils.LoadTexturePNG(path);
        }
        if (src == null)
        {
            Debug.LogError("[DryingManager] Texture hasil pewarnaan tidak ditemukan!");
            return;
        }

        _displayTex = TextureUtils.CloneReadable(src);
        _basePixels = _displayTex.GetPixels32();
        clothPreview.texture = _displayTex;
        clothPreview.color = new Color(1f, 1f, 1f, 0.6f); // mulai agak basah

        btnTakeCloth.onClick.AddListener(TakeCloth);
        timerBar.minValue = 0f;
        timerBar.maxValue = maxDryTime;
    }

    private void Update()
    {
        if (_taken) return;

        _elapsed += Time.deltaTime;
        timerBar.value = _elapsed;
        timerLabel.text = $"{_elapsed:F1}s / {maxDryTime:F0}s";

        UpdateDryingVisual(_elapsed);
        UpdateStatusLabel(_elapsed);

        if (_elapsed >= maxDryTime) TakeCloth();
    }

    /// <summary>
    /// Memperbarui tampilan kain berdasarkan waktu jemur.
    /// </summary>
    private void UpdateDryingVisual(float t)
    {
        float opacity = CalculateOpacity(t);
        clothPreview.color = new Color(1f, 1f, 1f, opacity);

        // Jika over-dry, tampilkan piksel sedikit memudar (desaturate)
        if (t > idealTime + idealWindow)
        {
            float overRatio = Mathf.InverseLerp(idealTime + idealWindow, maxDryTime, t);
            ApplyFadeEffect(overRatio);
        }
    }

    /// <summary>
    /// Formula opacity sebagai indikator "kualitas jemur":
    /// - < idealTime - window : naik linear dari minOpacity → 1
    /// - dalam ideal window   : 1 (sempurna)
    /// - > idealTime + window : turun linear ke minOpacity
    /// </summary>
    private float CalculateOpacity(float t)
    {
        float lo = idealTime - idealWindow;
        float hi = idealTime + idealWindow;

        if (t < lo) return Mathf.Lerp(minOpacity, 1f, t / lo);
        if (t <= hi) return 1f;
        float overT = Mathf.InverseLerp(hi, maxDryTime, t);
        return Mathf.Lerp(1f, minOpacity, overT);
    }

    /// <summary>
    /// Memberi efek fade pada piksel (mensimulasikan kain pudar terkena matahari terlalu lama).
    /// Tidak dipanggil tiap frame agar ringan—di-throttle.
    /// </summary>
    private float _lastFadeApply;
    private void ApplyFadeEffect(float ratio)
    {
        if (Time.unscaledTime - _lastFadeApply < 0.25f) return;
        _lastFadeApply = Time.unscaledTime;

        Color32[] working = new Color32[_basePixels.Length];
        Color32 white = new Color32(255, 255, 255, 255);
        for (int i = 0; i < _basePixels.Length; i++)
        {
            Color32 b = _basePixels[i];
            working[i] = Color32.Lerp(b, white, ratio * 0.5f);
        }
        _displayTex.SetPixels32(working);
        _displayTex.Apply();
    }

    private void UpdateStatusLabel(float t)
    {
        if (statusLabel == null) return;
        float lo = idealTime - idealWindow;
        float hi = idealTime + idealWindow;

        if (t < lo) statusLabel.text = "Masih basah";
        else if (t <= hi) statusLabel.text = "Kering sempurna!";
        else if (t < maxDryTime * 0.85f) statusLabel.text = "Terlalu lama";
        else statusLabel.text = "Pudar terbakar matahari";
    }

    /// <summary>
    /// Mengangkat kain dari jemuran → finalisasi & lanjut ke result.
    /// </summary>
    public void TakeCloth()
    {
        if (_taken) return;
        _taken = true;

        float opacity = CalculateOpacity(_elapsed);
        GameManager.Instance.dryOpacity = opacity;

        // Simpan texture final
        BatikDatabase.Instance.lastColoredTexture = _displayTex;
        TextureUtils.SaveTexturePNG(_displayTex, "last_final.png");

        StartCoroutine(GoToResult());
    }

    private IEnumerator GoToResult()
    {
        yield return new WaitForSeconds(0.6f);
        GameManager.Instance.ChangeState(GameState.Result);
        GameManager.Instance.LoadScene("07_Result");
    }
}
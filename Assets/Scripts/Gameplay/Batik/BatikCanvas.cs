using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Enum ukuran canting (jari-jari brush dalam piksel).
/// </summary>
public enum CantingSize { Kecil = 3, Sedang = 7, Besar = 12 }

/// <summary>
/// Komponen utama scene menggambar batik.
/// 
/// Optimasi:
/// - Memakai Color32[] buffer (bukan SetPixel berulang) → lebih cepat.
/// - Texture.Apply() dipanggil sekali per frame (bukan per stroke).
/// - Akurasi dihitung dengan downsampling (step=2) untuk performa.
/// 
/// Fitur:
/// - Pola panduan muncul 5 detik di awal, lalu fade out.
/// - Jika player diam 2 detik, panduan muncul lagi (fade in 2 detik).
/// - Saat player mulai menggambar lagi, panduan langsung disembunyikan.
/// - Tombol "Selesai" muncul otomatis saat coverage ≥ 80%.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class BatikCanvas : MonoBehaviour
{
    [Header("Canvas Refs")]
    public RawImage drawSurface;
    public RawImage guideOverlay;
    public int textureSize = 512;

    [Header("Brush")]
    public Color inkColor = Color.black;
    public CantingSize currentCanting = CantingSize.Sedang;

    [Header("Guide Timing")]
    public float initialGuideDuration = 5f;
    public float idleBeforeReshow = 2f;
    public float fadeDuration = 2f;
    [Range(0f, 1f)] public float guideAlpha = 0.35f;

    [Header("Events")]
    public System.Action<float> OnCoverageUpdated;

    // --- Internal ---
    private Texture2D _canvasTex;
    private Color32[] _buffer;
    private Texture2D _maskTex;       // referensi pola dari BatikPattern
    private Vector2Int _lastPixel;
    private bool _isDrawing;
    private bool _hasLastPoint;
    private bool _dirtyTexture;       // apakah perlu Apply()
    private float _idleTimer;
    private Coroutine _guideFadeCo;
    private float _lastAccuracyCheck;
    private float _cachedCoverage;
    private const float COVERAGE_THRESHOLD = 80f;

    private void Awake()
    {
        InitializeTextures();
    }

    private void Start()
    {
        StartCoroutine(InitialGuideRoutine());
    }

    /// <summary>
    /// Inisialisasi texture kosong (putih) + load mask dari order.
    /// </summary>
    private void InitializeTextures()
    {
        _canvasTex = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        _canvasTex.filterMode = FilterMode.Bilinear;
        _buffer = new Color32[textureSize * textureSize];
        Color32 white = new Color32(255, 255, 255, 255);
        for (int i = 0; i < _buffer.Length; i++) _buffer[i] = white;
        _canvasTex.SetPixels32(_buffer);
        _canvasTex.Apply();
        drawSurface.texture = _canvasTex;

        // Load pattern mask
        var pattern = GameManager.Instance.currentOrder?.desiredPattern;
        if (pattern == null)
        {
            Debug.LogError("[BatikCanvas] Pattern NULL. Cek CustomerOrder!");
            return;
        }
        _maskTex = pattern.maskTexture;
        if (_maskTex != null)
        {
            guideOverlay.texture = _maskTex;
            SetGuideAlpha(guideAlpha);
        }
    }

    private IEnumerator InitialGuideRoutine()
    {
        SetGuideAlpha(guideAlpha);
        yield return new WaitForSeconds(initialGuideDuration);
        yield return StartCoroutine(FadeGuide(guideAlpha, 0f, 1f));
    }

    private void Update()
    {
        HandleInput();
        HandleIdleGuide();

        if (_dirtyTexture)
        {
            _canvasTex.SetPixels32(_buffer);
            _canvasTex.Apply();
            _dirtyTexture = false;
        }

        // Cek coverage secara periodik (tidak tiap frame untuk efisiensi)
        if (Time.unscaledTime - _lastAccuracyCheck > 0.4f)
        {
            _lastAccuracyCheck = Time.unscaledTime;
            _cachedCoverage = CalculateCoverage();
            OnCoverageUpdated?.Invoke(_cachedCoverage);
            BatikDrawingManager.Instance?.NotifyCoverage(_cachedCoverage);
        }
    }

    /// <summary>
    /// Mengonversi posisi mouse ke koordinat piksel canvas.
    /// </summary>
    private bool ScreenToCanvasPixel(Vector2 screenPos, out Vector2Int pixel)
    {
        pixel = default;
        RectTransform rt = drawSurface.rectTransform;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screenPos, null, out Vector2 local))
            return false;

        float u = (local.x + rt.rect.width / 2f) / rt.rect.width;
        float v = (local.y + rt.rect.height / 2f) / rt.rect.height;
        if (u < 0f || u > 1f || v < 0f || v > 1f) return false;

        pixel = new Vector2Int(
            Mathf.Clamp((int)(u * textureSize), 0, textureSize - 1),
            Mathf.Clamp((int)(v * textureSize), 0, textureSize - 1)
        );
        return true;
    }

    private void HandleInput()
    {
        bool mouseDown = Input.GetMouseButton(0);
        if (!mouseDown)
        {
            _isDrawing = false;
            _hasLastPoint = false;
            return;
        }

        if (!ScreenToCanvasPixel(Input.mousePosition, out Vector2Int p)) return;

        // Sembunyikan guide saat menggambar
        if (_guideFadeCo != null) { StopCoroutine(_guideFadeCo); _guideFadeCo = null; }
        SetGuideAlpha(0f);
        _idleTimer = 0f;
        _isDrawing = true;

        if (_hasLastPoint) DrawLine(_lastPixel, p);
        else DrawCircle(p.x, p.y);

        _lastPixel = p;
        _hasLastPoint = true;
        _dirtyTexture = true;
    }

    private void HandleIdleGuide()
    {
        if (_isDrawing) return;
        _idleTimer += Time.deltaTime;
        if (_idleTimer >= idleBeforeReshow && _guideFadeCo == null)
        {
            _guideFadeCo = StartCoroutine(FadeGuide(GetGuideAlpha(), guideAlpha, fadeDuration));
        }
    }

    // === BRUSH / DRAWING ===

    private void DrawCircle(int cx, int cy)
    {
        int r = (int)currentCanting;
        int r2 = r * r;
        for (int dy = -r; dy <= r; dy++)
        {
            int py = cy + dy;
            if (py < 0 || py >= textureSize) continue;
            for (int dx = -r; dx <= r; dx++)
            {
                if (dx * dx + dy * dy > r2) continue;
                int px = cx + dx;
                if (px < 0 || px >= textureSize) continue;
                _buffer[py * textureSize + px] = inkColor;
            }
        }
    }

    private void DrawLine(Vector2Int a, Vector2Int b)
    {
        int dist = Mathf.Max(Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y));
        if (dist == 0) { DrawCircle(a.x, a.y); return; }
        for (int i = 0; i <= dist; i++)
        {
            float t = (float)i / dist;
            int x = Mathf.RoundToInt(Mathf.Lerp(a.x, b.x, t));
            int y = Mathf.RoundToInt(Mathf.Lerp(a.y, b.y, t));
            DrawCircle(x, y);
        }
    }

    // === COVERAGE / ACCURACY ===

    /// <summary>
    /// Hitung persentase piksel pola yang sudah tertutup tinta player.
    /// Memakai downsampling (step=2) agar ringan.
    /// </summary>
    public float CalculateCoverage()
    {
        if (_maskTex == null) return 0f;
        int step = 2;
        int matched = 0;
        int totalMask = 0;

        for (int y = 0; y < textureSize; y += step)
        {
            for (int x = 0; x < textureSize; x += step)
            {
                float u = (float)x / textureSize;
                float v = (float)y / textureSize;
                Color m = _maskTex.GetPixelBilinear(u, v);
                if (m.r >= 0.3f) continue; // bukan area pola
                totalMask++;
                Color32 drawn = _buffer[y * textureSize + x];
                if (drawn.r < 80) matched++;
            }
        }
        return totalMask == 0 ? 0f : (matched / (float)totalMask) * 100f;
    }

    // === GUIDE FADE ===

    private void SetGuideAlpha(float a)
    {
        if (guideOverlay == null) return;
        var c = guideOverlay.color; c.a = a;
        guideOverlay.color = c;
    }

    private float GetGuideAlpha() => guideOverlay != null ? guideOverlay.color.a : 0f;

    private IEnumerator FadeGuide(float from, float to, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            SetGuideAlpha(Mathf.Lerp(from, to, t / dur));
            yield return null;
        }
        SetGuideAlpha(to);
        _guideFadeCo = null;
    }

    // === API EKSTERNAL ===

    public void SetCanting(CantingSize size) => currentCanting = size;
    public Texture2D GetCurrentDrawingTexture() => _canvasTex;
    public float GetCoverage() => _cachedCoverage;
}
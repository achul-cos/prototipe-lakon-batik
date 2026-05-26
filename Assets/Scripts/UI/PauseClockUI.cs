using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Sistem Pause + Pocket Clock (jam kantung/saku).
///
/// Fungsionalitas:
/// - Menekan tombol Escape akan men-toggle pause game.
/// - Saat pause, waktu game berhenti (Time.timeScale = 0).
/// - Muncul animasi pocket clock dari bawah/tengah layar dengan efek slide-in.
/// - Jam menampilkan jarum jam dan jarum menit layaknya jam dinding,
///   berdasarkan jam game saat ini (bukan jam sistem).
/// - Terdapat label digital sebagai pelengkap (opsional).
/// - Menekan Escape lagi atau tombol Resume akan menutup clock dan melanjutkan game.
///
/// Setup di Inspector:
/// - pocketClockRoot  : root GameObject jam kantung (di-set inactive di awal).
/// - hourHand         : RectTransform jarum jam. Pivot di bawah, rotasi Z = negatif.
/// - minuteHand       : RectTransform jarum menit. Pivot di bawah, rotasi Z = negatif.
/// - digitalTimeLabel : (opsional) TMP_Text untuk tampilan digital "HH:MM".
/// - resumeButton     : (opsional) tombol Resume di panel pause.
/// - backgroundOverlay: (opsional) semi-transparent overlay gelap saat pause.
///
/// Catatan:
/// - Script ini cukup diletakkan di scene Lobby (02_Lobby) dan scene lain yang
///   membutuhkan fitur pause.
/// - Animasi menggunakan Coroutine agar kompatibel dengan Time.timeScale = 0
///   (gunakan WaitForSecondsRealtime / unscaledDeltaTime).
/// </summary>
public class PauseClockUI : MonoBehaviour
{
    // =========================================================
    // INSPECTOR REFERENCES
    // =========================================================

    [Header("Root Container")]
    [Tooltip("Root GameObject jam kantung. Awalnya inactive.")]
    public GameObject pocketClockRoot;

    [Tooltip("Overlay gelap semi-transparan saat pause (opsional, bisa null).")]
    public Image backgroundOverlay;

    [Header("Jarum Jam")]
    [Tooltip("RectTransform jarum jam. Pivot harus di ujung bawah (0.5, 0).")]
    public RectTransform hourHand;

    [Tooltip("RectTransform jarum menit. Pivot harus di ujung bawah (0.5, 0).")]
    public RectTransform minuteHand;

    [Header("Label Digital (Opsional)")]
    [Tooltip("Label TMP untuk tampilan waktu digital, mis: '10:30'. Boleh null.")]
    public TMP_Text digitalTimeLabel;

    [Tooltip("Label 'PAUSED' atau teks status pause. Boleh null.")]
    public TMP_Text pauseStatusLabel;

    [Header("Tombol")]
    [Tooltip("Tombol Resume di panel pause. Boleh null; Escape juga bisa dipakai.")]
    public Button resumeButton;

    [Header("Animasi Slide-In")]
    [Tooltip("Durasi animasi clock muncul (detik, unscaled time).")]
    [Range(0.1f, 0.8f)]
    public float slideInDuration = 0.3f;

    [Tooltip("Posisi awal clock sebelum slide in (offset Y ke bawah dari posisi normal).")]
    public float slideOffsetY = -300f;

    [Header("Overlay")]
    [Tooltip("Warna target overlay saat pause.")]
    public Color overlayColor = new Color(0f, 0f, 0f, 0.55f);

    // =========================================================
    // PRIVATE STATE
    // =========================================================

    private Vector2 _clockRestPosition;   // posisi normal clock
    private Coroutine _animCoroutine;
    private bool _isPauseKeyEnabled = true; // bisa dinonaktifkan saat ada dialog, dsb.

    // =========================================================
    // UNITY LIFECYCLE
    // =========================================================

    private void Awake()
    {
        // Simpan posisi normal sebelum disembunyikan
        if (pocketClockRoot != null)
        {
            _clockRestPosition = pocketClockRoot.GetComponent<RectTransform>().anchoredPosition;
            pocketClockRoot.SetActive(false);
        }

        // Setup overlay
        if (backgroundOverlay != null)
            backgroundOverlay.color = new Color(0f, 0f, 0f, 0f);
    }

    private void Start()
    {
        if (resumeButton != null)
            resumeButton.onClick.AddListener(Resume);
    }

    private void Update()
    {
        // Toggle pause dengan Escape
        if (_isPauseKeyEnabled && Input.GetKeyDown(KeyCode.Escape))
        {
            if (GameManager.Instance.isPaused) Resume();
            else Pause();
        }

        // Update jarum jam secara real-time saat pause (unscaled)
        if (GameManager.Instance.isPaused && pocketClockRoot != null && pocketClockRoot.activeSelf)
        {
            UpdateClockHands(GameManager.Instance.currentGameHour);
        }
    }

    // =========================================================
    // PAUSE / RESUME
    // =========================================================

    /// <summary>
    /// Pause game: hentikan waktu, tampilkan pocket clock dengan animasi slide-in.
    /// </summary>
    public void Pause()
    {
        if (GameManager.Instance.isPaused) return;

        GameManager.Instance.TogglePause(); // sets Time.timeScale = 0

        if (pauseStatusLabel != null)
            pauseStatusLabel.text = "DIJEDA";

        // Update label digital sekali saat pause
        if (digitalTimeLabel != null)
            digitalTimeLabel.text = TimeManager.Instance.GetTimeString();

        // Update jarum jam
        UpdateClockHands(GameManager.Instance.currentGameHour);

        // Jalankan animasi tampil
        if (_animCoroutine != null) StopCoroutine(_animCoroutine);
        _animCoroutine = StartCoroutine(ShowClockAnimation());
    }

    /// <summary>
    /// Resume game: sembunyikan pocket clock, lanjutkan waktu.
    /// </summary>
    public void Resume()
    {
        if (!GameManager.Instance.isPaused) return;

        if (_animCoroutine != null) StopCoroutine(_animCoroutine);
        _animCoroutine = StartCoroutine(HideClockAnimation(() =>
        {
            GameManager.Instance.TogglePause(); // sets Time.timeScale = 1
        }));
    }

    /// <summary>
    /// Aktifkan/nonaktifkan tombol Escape untuk pause.
    /// Berguna saat scene dialog berjalan (tidak ingin player pause di tengah cutscene).
    /// </summary>
    public void SetPauseKeyEnabled(bool enabled)
    {
        _isPauseKeyEnabled = enabled;
    }

    // =========================================================
    // CLOCK HAND LOGIC
    // =========================================================

    /// <summary>
    /// Menghitung sudut rotasi jarum jam dan menit dari gameHour (float).
    /// Contoh: gameHour = 10.5f → 10 jam 30 menit.
    ///
    /// Rotasi jam analog:
    ///   - Jarum jam  : 360° / 12 jam = 30° per jam, plus kontribusi menit (0.5° per menit)
    ///   - Jarum menit: 360° / 60 menit = 6° per menit
    ///
    /// Unity: rotasi Z negatif = searah jarum jam (clockwise).
    /// </summary>
    private void UpdateClockHands(float gameHour)
    {
        // Normalisasi ke 12 jam
        float h12 = gameHour % 12f;
        float mins = (gameHour - Mathf.Floor(gameHour)) * 60f;

        // Sudut jarum jam (termasuk kontribusi menit supaya bergerak halus)
        float hourAngle = -(h12 * 30f + mins * 0.5f);

        // Sudut jarum menit
        float minuteAngle = -(mins * 6f);

        if (hourHand != null)
            hourHand.localEulerAngles = new Vector3(0f, 0f, hourAngle);

        if (minuteHand != null)
            minuteHand.localEulerAngles = new Vector3(0f, 0f, minuteAngle);
    }

    // =========================================================
    // ANIMASI COROUTINE
    // =========================================================

    /// <summary>
    /// Animasi clock muncul dari bawah (slide up) + fade in overlay.
    /// Menggunakan unscaledDeltaTime karena Time.timeScale = 0 saat pause.
    /// </summary>
    private IEnumerator ShowClockAnimation()
    {
        pocketClockRoot.SetActive(true);
        RectTransform rt = pocketClockRoot.GetComponent<RectTransform>();

        Vector2 startPos = _clockRestPosition + new Vector2(0f, slideOffsetY);
        rt.anchoredPosition = startPos;

        float elapsed = 0f;
        while (elapsed < slideInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / slideInDuration);
            rt.anchoredPosition = Vector2.Lerp(startPos, _clockRestPosition, t);

            // Fade overlay bersamaan
            if (backgroundOverlay != null)
                backgroundOverlay.color = Color.Lerp(new Color(0f, 0f, 0f, 0f), overlayColor, t);

            yield return null;
        }

        rt.anchoredPosition = _clockRestPosition;
        if (backgroundOverlay != null)
            backgroundOverlay.color = overlayColor;

        _animCoroutine = null;
    }

    /// <summary>
    /// Animasi clock menghilang (slide down) + fade out overlay.
    /// Callback dipanggil setelah animasi selesai (dipakai untuk memanggil TogglePause).
    /// </summary>
    private IEnumerator HideClockAnimation(System.Action onComplete = null)
    {
        RectTransform rt = pocketClockRoot.GetComponent<RectTransform>();
        Vector2 endPos = _clockRestPosition + new Vector2(0f, slideOffsetY);

        float elapsed = 0f;
        while (elapsed < slideInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / slideInDuration);
            rt.anchoredPosition = Vector2.Lerp(_clockRestPosition, endPos, t);

            if (backgroundOverlay != null)
                backgroundOverlay.color = Color.Lerp(overlayColor, new Color(0f, 0f, 0f, 0f), t);

            yield return null;
        }

        pocketClockRoot.SetActive(false);
        if (backgroundOverlay != null)
            backgroundOverlay.color = new Color(0f, 0f, 0f, 0f);

        _animCoroutine = null;
        onComplete?.Invoke();
    }
}
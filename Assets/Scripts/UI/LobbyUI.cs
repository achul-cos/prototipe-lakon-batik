using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// UI utama scene Lobby (toko batik). Menampilkan:
/// - Hari ke-N + nama hari
/// - Cuaca hari ini dan ramalan besok
/// - Jumlah pelanggan dilayani / total hari ini
/// - Uang saat ini dalam Rupiah
/// - Jam game yang berjalan real-time
/// - Jam operasional toko
///
/// Juga mengelola:
/// - Event saat jam toko tutup (17:00)
/// - Event saat semua pelanggan sudah terlayani
/// - Panel ringkasan akhir hari dan transisi ke hari berikutnya
///
/// Cara pakai:
/// - Letakkan komponen ini pada GameObject di scene 02_Lobby.
/// - Drag semua referensi UI melalui Inspector.
/// - Pastikan TimeManager, CustomerManager, GameManager, WeatherSystem
///   sudah ada sebagai Singleton (biasanya sudah DontDestroyOnLoad dari MainMenu).
/// </summary>
public class LobbyUI : MonoBehaviour
{
    // =========================================================
    // INSPECTOR REFERENCES
    // =========================================================

    [Header("Top Bar — Info Harian")]
    [Tooltip("Contoh: 'Hari 1 – Senin'")]
    public TMP_Text dayText;

    [Tooltip("Contoh: 'Cuaca: Cerah'")]
    public TMP_Text weatherText;

    [Tooltip("Contoh: 'Besok: Berawan'")]
    public TMP_Text forecastText;

    [Tooltip("Contoh: '3/10' (dilayani/total)")]
    public TMP_Text customerCounterText;

    [Tooltip("Contoh: 'Rp 125.000'")]
    public TMP_Text moneyText;

    [Tooltip("Jam game saat ini, update real-time. Contoh: '10:30'")]
    public TMP_Text timeText;

    [Tooltip("Label statis jam buka-tutup. Contoh: '09:00 – 17:00'")]
    public TMP_Text shopHoursText;

    [Header("Weather Icons")]
    [Tooltip("Array 5 sprite cuaca sesuai urutan enum WeatherType: Cerah, Berawan, Gerimis, Hujan, Badai")]
    public Sprite[] weatherIcons;

    [Tooltip("Image cuaca hari ini")]
    public Image weatherIconImage;

    [Tooltip("Image ramalan cuaca besok")]
    public Image forecastIconImage;

    [Header("Notifikasi Toko Tutup")]
    [Tooltip("Panel kecil yang muncul saat jam 17:00 mengingatkan toko akan tutup")]
    public GameObject shopClosingNoticePanel;

    [Tooltip("Berapa detik notice ditampilkan sebelum auto-hilang (0 = tidak auto-hilang)")]
    public float closingNoticeDuration = 4f;

    [Header("Panel Akhir Hari")]
    [Tooltip("Panel besar yang muncul setelah semua pelanggan dilayani DAN toko tutup")]
    public GameObject endOfDayPanel;

    [Tooltip("Ringkasan pendapatan hari ini, jumlah pelanggan, dsb.")]
    public TMP_Text endOfDaySummaryText;

    [Tooltip("Tombol untuk melanjutkan ke hari berikutnya")]
    public Button btnNextDay;

    // =========================================================
    // PRIVATE STATE
    // =========================================================

    private int _servedToday = 0;
    private int _totalToday = 0;
    private bool _shopClosed = false;
    private bool _allServed = false;
    private int _moneyAtStartOfDay = 0; // untuk hitung pendapatan hari ini

    // =========================================================
    // UNITY LIFECYCLE
    // =========================================================

    private void Start()
    {
        // Catat uang awal hari ini sebelum ada transaksi
        _moneyAtStartOfDay = GameManager.Instance.currentSave?.money ?? 0;

        // Hitung total pelanggan hari ini
        var save = GameManager.Instance.currentSave;
        if (save != null)
        {
            _totalToday = WeatherSystem.Instance.CalculateCustomerCount(
                save.currentDayOfWeek,
                save.todayWeather);
        }
        _servedToday = 0;

        // Mulai sistem waktu untuk hari ini
        TimeManager.Instance.ResetForNewDay();

        // Subscribe ke event
        TimeManager.Instance.OnHourChanged += HandleTimeChanged;
        TimeManager.Instance.OnShopClosed += HandleShopClosed;
        GameManager.Instance.OnMoneyChanged += HandleMoneyChanged;
        CustomerManager.Instance.OnAllCustomersServed += HandleAllCustomersServed;

        // Mulai spawning pelanggan
        CustomerManager.Instance.StartDay();

        // Setup tombol akhir hari
        if (btnNextDay != null)
            btnNextDay.onClick.AddListener(ProceedToNextDay);

        // Pastikan panel akhir hari tersembunyi di awal
        if (endOfDayPanel != null) endOfDayPanel.SetActive(false);
        if (shopClosingNoticePanel != null) shopClosingNoticePanel.SetActive(false);

        // Tampilkan data awal
        RefreshAllUI();
    }

    private void OnDestroy()
    {
        // Penting: unsubscribe agar tidak memory leak saat scene berganti
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnHourChanged -= HandleTimeChanged;
            TimeManager.Instance.OnShopClosed -= HandleShopClosed;
        }
        if (GameManager.Instance != null)
            GameManager.Instance.OnMoneyChanged -= HandleMoneyChanged;
        if (CustomerManager.Instance != null)
            CustomerManager.Instance.OnAllCustomersServed -= HandleAllCustomersServed;
    }

    // =========================================================
    // EVENT HANDLERS
    // =========================================================

    /// <summary>
    /// Dipanggil setiap frame oleh TimeManager saat jam game berubah.
    /// Update label waktu secara efisien (hanya string, tidak rebuild UI penuh).
    /// </summary>
    private void HandleTimeChanged(float gameHour)
    {
        if (timeText != null)
            timeText.text = TimeManager.Instance.GetTimeString();
    }

    /// <summary>
    /// Dipanggil tepat saat jam game menyentuh 17:00.
    /// Menampilkan notifikasi singkat dan mencatat status tutup.
    /// </summary>
    private void HandleShopClosed()
    {
        _shopClosed = true;
        Debug.Log("[LobbyUI] Toko tutup jam 17:00.");
        ShowClosingNotice();
        TryShowEndOfDay();
    }

    /// <summary>
    /// Dipanggil oleh GameManager setiap kali uang bertambah/berkurang.
    /// </summary>
    private void HandleMoneyChanged(int newAmount)
    {
        if (moneyText != null)
            moneyText.text = $"Rp {newAmount:N0}";
    }

    /// <summary>
    /// Dipanggil oleh CustomerManager saat queue harian habis dan
    /// semua pelanggan aktif sudah dilayani.
    /// </summary>
    private void HandleAllCustomersServed()
    {
        _allServed = true;
        Debug.Log("[LobbyUI] Semua pelanggan hari ini sudah dilayani.");
        TryShowEndOfDay();
    }

    // =========================================================
    // UI REFRESH
    // =========================================================

    /// <summary>
    /// Refresh seluruh label UI dari data GameManager. Dipanggil sekali di Start
    /// dan bisa dipanggil ulang jika ada update besar.
    /// </summary>
    private void RefreshAllUI()
    {
        var save = GameManager.Instance.currentSave;
        if (save == null) return;

        // Info hari
        if (dayText != null)
            dayText.text = $"Hari {save.currentDay} \u2013 {save.currentDayOfWeek}";

        // Cuaca
        if (weatherText != null)
            weatherText.text = $"Cuaca: {save.todayWeather}";

        if (forecastText != null)
            forecastText.text = $"Besok: {save.tomorrowWeather}";

        // Ikon cuaca
        if (weatherIcons != null && weatherIcons.Length >= 5)
        {
            if (weatherIconImage != null)
                weatherIconImage.sprite = weatherIcons[(int)save.todayWeather];
            if (forecastIconImage != null)
                forecastIconImage.sprite = weatherIcons[(int)save.tomorrowWeather];
        }

        // Uang
        if (moneyText != null)
            moneyText.text = $"Rp {save.money:N0}";

        // Jam toko
        if (shopHoursText != null)
            shopHoursText.text = "09:00 \u2013 17:00";

        // Counter pelanggan
        RefreshCustomerCounter();

        // Jam saat ini
        if (timeText != null)
            timeText.text = TimeManager.Instance.GetTimeString();
    }

    /// <summary>
    /// Memperbarui label counter pelanggan (format: dilayani/total).
    /// Dipanggil setiap kali CustomerManager menyelesaikan satu pelanggan.
    /// </summary>
    public void IncrementServedCount()
    {
        _servedToday++;
        RefreshCustomerCounter();
    }

    private void RefreshCustomerCounter()
    {
        if (customerCounterText != null)
            customerCounterText.text = $"{_servedToday}/{_totalToday}";
    }

    // =========================================================
    // SHOP CLOSING & END OF DAY
    // =========================================================

    /// <summary>
    /// Tampilkan notifikasi kecil bahwa toko akan tutup. Auto-sembunyikan
    /// setelah closingNoticeDuration detik jika nilainya > 0.
    /// </summary>
    private void ShowClosingNotice()
    {
        if (shopClosingNoticePanel == null) return;
        shopClosingNoticePanel.SetActive(true);
        if (closingNoticeDuration > 0f)
            StartCoroutine(HideAfterDelay(shopClosingNoticePanel, closingNoticeDuration));
    }

    private System.Collections.IEnumerator HideAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (obj != null) obj.SetActive(false);
    }

    /// <summary>
    /// Cek apakah kondisi untuk menampilkan panel akhir hari sudah terpenuhi:
    /// toko tutup DAN semua pelanggan sudah dilayani.
    /// </summary>
    private void TryShowEndOfDay()
    {
        if (!_shopClosed || !_allServed) return;
        ShowEndOfDayPanel();
    }

    /// <summary>
    /// Tampilkan panel ringkasan akhir hari dengan data pendapatan hari ini.
    /// </summary>
    private void ShowEndOfDayPanel()
    {
        if (endOfDayPanel == null) return;

        var save = GameManager.Instance.currentSave;
        int todayEarnings = (save?.money ?? 0) - _moneyAtStartOfDay;

        if (endOfDaySummaryText != null)
        {
            endOfDaySummaryText.text =
                $"<b>Hari {save?.currentDay} selesai!</b>\n\n" +
                $"Pelanggan dilayani : {_servedToday}/{_totalToday}\n" +
                $"Pendapatan hari ini: <color=#4CAF50>Rp {todayEarnings:N0}</color>\n" +
                $"Total uang         : Rp {save?.money:N0}";
        }

        endOfDayPanel.SetActive(true);
        Debug.Log($"[LobbyUI] Panel akhir hari ditampilkan. Pendapatan: Rp {todayEarnings:N0}");
    }

    /// <summary>
    /// Dipanggil saat tombol "Hari Berikutnya" ditekan.
    /// Memajukan state hari dan reload scene Lobby (sehingga data segar).
    /// </summary>
    private void ProceedToNextDay()
    {
        GameManager.Instance.EndDay();
        GameManager.Instance.LoadScene("02_Lobby");
    }
}
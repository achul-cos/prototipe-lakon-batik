using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Mengatur setting game: volume audio, kualitas grafis, resolusi layar, dan fullscreen.
///
/// Semua setting disimpan ke PlayerPrefs agar persisten lintas sesi.
/// Dipanggil dari MainMenuController dan panel Settings di mana saja.
///
/// Cara pakai:
/// 1. Buat AudioMixer di project (Assets > Create > Audio Mixer).
/// 2. Expose parameter "MasterVol", "MusicVol", "SFXVol" dari mixer
///    (klik kanan parameter di Mixer > Expose to Script).
/// 3. Drag mixer ke field audioMixer di Inspector.
/// 4. Drag semua slider/dropdown/toggle ke field masing-masing.
/// 5. SettingsManager bersifat DontDestroyOnLoad, jadi cukup ada di scene MainMenu.
///
/// Catatan:
/// - AudioMixer menggunakan skala logaritmik (desibel), bukan linear.
///   Rumus: dB = 20 * log10(linearValue). Nilai 0.0001f dipakai sebagai batas
///   bawah untuk menghindari log(0).
/// </summary>
public class SettingsManager : Singleton<SettingsManager>
{
    protected override bool PersistBetweenScenes => true;

    // =========================================================
    // INSPECTOR REFERENCES
    // =========================================================

    [Header("Audio Mixer")]
    [Tooltip("Drag AudioMixer utama game ke sini")]
    public AudioMixer audioMixer;

    [Header("Volume Sliders (nilai 0..1)")]
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;

    [Header("Label Nilai Volume (Opsional)")]
    [Tooltip("Menampilkan nilai volume dalam persen, mis: '75%'")]
    public TMP_Text masterVolumeLabel;
    public TMP_Text musicVolumeLabel;
    public TMP_Text sfxVolumeLabel;

    [Header("Grafis")]
    [Tooltip("Dropdown untuk Quality Level Unity (Very Low..Ultra). Diisi otomatis.")]
    public TMP_Dropdown qualityDropdown;

    [Tooltip("Dropdown resolusi layar. Diisi otomatis dari Screen.resolutions.")]
    public TMP_Dropdown resolutionDropdown;

    [Tooltip("Toggle Fullscreen / Windowed")]
    public Toggle fullscreenToggle;

    [Header("Tombol Panel")]
    [Tooltip("Tombol Apply — terapkan perubahan resolusi & grafis")]
    public Button applyButton;

    [Tooltip("Tombol Back/Close — tutup panel settings")]
    public Button backButton;

    [Tooltip("Panel settings itu sendiri, untuk menutup dari tombol Back")]
    public GameObject settingsPanel;

    // =========================================================
    // PRIVATE STATE
    // =========================================================

    // Kunci PlayerPrefs
    private const string KEY_MASTER = "vol_master";
    private const string KEY_MUSIC = "vol_music";
    private const string KEY_SFX = "vol_sfx";
    private const string KEY_QUALITY = "quality_idx";
    private const string KEY_RES_IDX = "resolution_idx";
    private const string KEY_FULLSCR = "fullscreen";

    // Cache resolusi layar yang tersedia
    private Resolution[] _availableResolutions;

    // Nilai yang belum di-apply (menunggu tombol Apply ditekan)
    private int _pendingResolutionIndex;
    private int _pendingQualityIndex;
    private bool _pendingFullscreen;

    // =========================================================
    // UNITY LIFECYCLE
    // =========================================================

    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        BuildResolutionDropdown();
        BuildQualityDropdown();
        LoadAndApplySettings();
        HookUIEvents();
    }

    // =========================================================
    // SETUP DROPDOWN
    // =========================================================

    /// <summary>
    /// Mengisi dropdown resolusi dari Screen.resolutions.
    /// Resolusi duplikat (beda refresh rate) di-filter agar tidak membingungkan.
    /// </summary>
    private void BuildResolutionDropdown()
    {
        if (resolutionDropdown == null) return;

        _availableResolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        var options = new List<string>();
        int currentIndex = 0;
        var seen = new HashSet<string>();

        for (int i = 0; i < _availableResolutions.Length; i++)
        {
            string label = $"{_availableResolutions[i].width} x {_availableResolutions[i].height}";
            if (seen.Contains(label)) continue;
            seen.Add(label);
            options.Add(label);

            // Tandai resolusi aktif sebagai default
            if (_availableResolutions[i].width == Screen.currentResolution.width &&
                _availableResolutions[i].height == Screen.currentResolution.height)
            {
                currentIndex = options.Count - 1;
            }
        }

        resolutionDropdown.AddOptions(options);

        // Load dari prefs, atau pakai resolusi aktif
        int saved = PlayerPrefs.GetInt(KEY_RES_IDX, currentIndex);
        saved = Mathf.Clamp(saved, 0, options.Count - 1);
        resolutionDropdown.value = saved;
        resolutionDropdown.RefreshShownValue();
        _pendingResolutionIndex = saved;
    }

    /// <summary>
    /// Mengisi dropdown kualitas grafis dari QualitySettings.names.
    /// </summary>
    private void BuildQualityDropdown()
    {
        if (qualityDropdown == null) return;

        qualityDropdown.ClearOptions();
        qualityDropdown.AddOptions(new List<string>(QualitySettings.names));

        int saved = PlayerPrefs.GetInt(KEY_QUALITY, QualitySettings.GetQualityLevel());
        saved = Mathf.Clamp(saved, 0, QualitySettings.names.Length - 1);
        qualityDropdown.value = saved;
        qualityDropdown.RefreshShownValue();
        _pendingQualityIndex = saved;
    }

    // =========================================================
    // HOOK UI EVENTS
    // =========================================================

    /// <summary>
    /// Menghubungkan semua elemen UI ke handler-nya.
    /// Volume slider langsung apply (tidak perlu Apply button).
    /// Grafis & resolusi baru apply saat tombol Apply ditekan.
    /// </summary>
    private void HookUIEvents()
    {
        // Volume — langsung apply
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);

        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);

        // Grafis & resolusi — simpan pending dulu
        if (qualityDropdown != null)
            qualityDropdown.onValueChanged.AddListener(idx => _pendingQualityIndex = idx);

        if (resolutionDropdown != null)
            resolutionDropdown.onValueChanged.AddListener(idx => _pendingResolutionIndex = idx);

        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.AddListener(val => _pendingFullscreen = val);

        // Tombol
        if (applyButton != null)
            applyButton.onClick.AddListener(ApplyGraphicsSettings);

        if (backButton != null)
            backButton.onClick.AddListener(ClosePanel);
    }

    // =========================================================
    // VOLUME HANDLERS
    // =========================================================

    private void OnMasterVolumeChanged(float linearValue)
    {
        SetMixerVolume("MasterVol", linearValue);
        PlayerPrefs.SetFloat(KEY_MASTER, linearValue);
        if (masterVolumeLabel != null)
            masterVolumeLabel.text = $"{Mathf.RoundToInt(linearValue * 100f)}%";
    }

    private void OnMusicVolumeChanged(float linearValue)
    {
        SetMixerVolume("MusicVol", linearValue);
        PlayerPrefs.SetFloat(KEY_MUSIC, linearValue);
        if (musicVolumeLabel != null)
            musicVolumeLabel.text = $"{Mathf.RoundToInt(linearValue * 100f)}%";
    }

    private void OnSFXVolumeChanged(float linearValue)
    {
        SetMixerVolume("SFXVol", linearValue);
        PlayerPrefs.SetFloat(KEY_SFX, linearValue);
        if (sfxVolumeLabel != null)
            sfxVolumeLabel.text = $"{Mathf.RoundToInt(linearValue * 100f)}%";
    }

    /// <summary>
    /// Mengkonversi nilai linear slider (0..1) ke desibel untuk AudioMixer.
    /// Batas bawah 0.0001f untuk menghindari log(0) = -infinity.
    /// </summary>
    private void SetMixerVolume(string exposedParam, float linearValue)
    {
        if (audioMixer == null) return;
        float db = Mathf.Log10(Mathf.Max(linearValue, 0.0001f)) * 20f;
        audioMixer.SetFloat(exposedParam, db);
    }

    // =========================================================
    // GRAPHICS SETTINGS
    // =========================================================

    /// <summary>
    /// Terapkan perubahan kualitas grafis dan resolusi layar.
    /// Dipanggil saat tombol Apply ditekan.
    /// </summary>
    public void ApplyGraphicsSettings()
    {
        // Kualitas grafis
        QualitySettings.SetQualityLevel(_pendingQualityIndex, true);
        PlayerPrefs.SetInt(KEY_QUALITY, _pendingQualityIndex);

        // Resolusi
        if (_availableResolutions != null && _pendingResolutionIndex < _availableResolutions.Length)
        {
            var res = _availableResolutions[_pendingResolutionIndex];
            Screen.SetResolution(res.width, res.height, _pendingFullscreen);
            PlayerPrefs.SetInt(KEY_RES_IDX, _pendingResolutionIndex);
        }

        // Fullscreen
        Screen.fullScreen = _pendingFullscreen;
        PlayerPrefs.SetInt(KEY_FULLSCR, _pendingFullscreen ? 1 : 0);

        PlayerPrefs.Save();
        Debug.Log($"[SettingsManager] Settings applied — Quality: {QualitySettings.names[_pendingQualityIndex]}, " +
                  $"Fullscreen: {_pendingFullscreen}");
    }

    // =========================================================
    // LOAD & APPLY SAVED SETTINGS
    // =========================================================

    /// <summary>
    /// Load semua setting dari PlayerPrefs dan terapkan ke game + UI.
    /// Dipanggil sekali di Start.
    /// </summary>
    public void LoadAndApplySettings()
    {
        // --- Volume ---
        float master = PlayerPrefs.GetFloat(KEY_MASTER, 0.75f);
        float music = PlayerPrefs.GetFloat(KEY_MUSIC, 0.75f);
        float sfx = PlayerPrefs.GetFloat(KEY_SFX, 0.75f);

        if (masterVolumeSlider != null) masterVolumeSlider.value = master;
        if (musicVolumeSlider != null) musicVolumeSlider.value = music;
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = sfx;

        // Terapkan ke mixer (event slider tidak selalu trigger saat set value via code)
        SetMixerVolume("MasterVol", master);
        SetMixerVolume("MusicVol", music);
        SetMixerVolume("SFXVol", sfx);

        // Update label
        if (masterVolumeLabel != null) masterVolumeLabel.text = $"{Mathf.RoundToInt(master * 100f)}%";
        if (musicVolumeLabel != null) musicVolumeLabel.text = $"{Mathf.RoundToInt(music * 100f)}%";
        if (sfxVolumeLabel != null) sfxVolumeLabel.text = $"{Mathf.RoundToInt(sfx * 100f)}%";

        // --- Fullscreen ---
        bool fullscreen = PlayerPrefs.GetInt(KEY_FULLSCR, 1) == 1;
        Screen.fullScreen = fullscreen;
        _pendingFullscreen = fullscreen;
        if (fullscreenToggle != null) fullscreenToggle.isOn = fullscreen;

        // --- Quality ---
        int quality = PlayerPrefs.GetInt(KEY_QUALITY, QualitySettings.GetQualityLevel());
        QualitySettings.SetQualityLevel(quality, true);
        _pendingQualityIndex = quality;
        if (qualityDropdown != null)
        {
            qualityDropdown.value = quality;
            qualityDropdown.RefreshShownValue();
        }

        Debug.Log("[SettingsManager] Settings loaded from PlayerPrefs.");
    }

    /// <summary>
    /// Reset semua setting ke nilai default.
    /// Berguna untuk tombol "Reset Default" di panel settings.
    /// </summary>
    public void ResetToDefaults()
    {
        if (masterVolumeSlider != null) masterVolumeSlider.value = 0.75f;
        if (musicVolumeSlider != null) musicVolumeSlider.value = 0.75f;
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = 0.75f;
        if (fullscreenToggle != null) fullscreenToggle.isOn = true;
        if (qualityDropdown != null) qualityDropdown.value = 3; // Medium
        if (resolutionDropdown != null) resolutionDropdown.value = resolutionDropdown.options.Count - 1;

        _pendingQualityIndex = 3;
        _pendingFullscreen = true;
        _pendingResolutionIndex = (_availableResolutions?.Length ?? 1) - 1;

        ApplyGraphicsSettings();
        Debug.Log("[SettingsManager] Settings reset to defaults.");
    }

    // =========================================================
    // PANEL MANAGEMENT
    // =========================================================

    public void OpenPanel()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void ClosePanel()
    {
        PlayerPrefs.Save(); // simpan sebelum tutup
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }
}
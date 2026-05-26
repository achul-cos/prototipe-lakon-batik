using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Text.RegularExpressions;

/// <summary>
/// Dialog Manager bergaya Visual Novel.
/// Tugas:
/// - Menampilkan teks dialog dengan efek typewriter.
/// - Meng-highlight keyword dari BatikPattern.keywords + nama warna otomatis.
/// - Memberi tombol "Lanjut" untuk transisi ke scene menggambar.
/// 
/// Catatan integrasi:
/// - Letakkan GameObject ini di scene 03_Dialog.
/// - Drag panel & TMP_Text & tombol melalui Inspector.
/// </summary>
public class DialogManager : Singleton<DialogManager>
{
    protected override bool PersistBetweenScenes => false; // hanya di scene Dialog

    [Header("UI References")]
    public GameObject dialogPanel;
    public TMP_Text speakerNameText;
    public TMP_Text dialogBodyText;
    public Image customerPortrait;
    public Button continueButton;
    public Button skipButton;

    [Header("Typewriter Settings")]
    [Range(0.005f, 0.1f)] public float typeSpeed = 0.025f;
    public AudioSource voiceBlipSource; // optional sound per karakter
    public AudioClip blipClip;

    [Header("Keyword Highlight")]
    public Color patternKeywordColor = new Color(0.95f, 0.45f, 0.2f); // oranye batik
    public Color colorKeywordColor = new Color(0.2f, 0.6f, 0.95f);  // biru

    private Coroutine _typingRoutine;
    private bool _isTyping;
    private string _fullDialogText;

    private void Start()
    {
        if (continueButton != null) continueButton.onClick.AddListener(OnContinue);
        if (skipButton != null) skipButton.onClick.AddListener(OnSkip);

        var order = GameManager.Instance.currentOrder;
        if (order == null)
        {
            Debug.LogError("[DialogManager] currentOrder NULL. Pastikan customer dipilih dari lobby.");
            return;
        }
        BeginDialog(order);
    }

    /// <summary>
    /// Mulai dialog dengan pelanggan. Highlight keyword dilakukan dinamis
    /// berdasarkan data BatikPattern + nama warna dari enum BatikColor.
    /// </summary>
    public void BeginDialog(CustomerOrder order)
    {
        dialogPanel.SetActive(true);
        speakerNameText.text = order.customerName;

        string highlighted = ApplyKeywordHighlight(order.requestDialog, order);
        _fullDialogText = highlighted;

        if (_typingRoutine != null) StopCoroutine(_typingRoutine);
        _typingRoutine = StartCoroutine(TypewriterRoutine(highlighted));
    }

    /// <summary>
    /// Memberi tag warna pada kata kunci pola batik & warna.
    /// Menggunakan boundary regex agar tidak menabrak substring.
    /// </summary>
    private string ApplyKeywordHighlight(string raw, CustomerOrder order)
    {
        string result = raw;

        // Highlight keyword pola
        if (order.desiredPattern != null)
        {
            string patternHex = ColorUtility.ToHtmlStringRGB(patternKeywordColor);
            foreach (string kw in order.desiredPattern.keywords)
            {
                if (string.IsNullOrEmpty(kw)) continue;
                string pattern = $@"\b({Regex.Escape(kw)})\b";
                result = Regex.Replace(result, pattern,
                    $"<color=#{patternHex}><b>$1</b></color>",
                    RegexOptions.IgnoreCase);
            }
        }

        // Highlight keyword warna (dari enum)
        string colorHex = ColorUtility.ToHtmlStringRGB(colorKeywordColor);
        foreach (BatikColor c in System.Enum.GetValues(typeof(BatikColor)))
        {
            string name = c.ToString().ToLower();
            string pattern = $@"\b({name})\b";
            result = Regex.Replace(result, pattern,
                $"<color=#{colorHex}><b>$1</b></color>",
                RegexOptions.IgnoreCase);
        }

        return result;
    }

    /// <summary>
    /// Efek mengetik karakter per karakter dengan dukungan rich text TMP.
    /// Memanfaatkan maxVisibleCharacters agar tag tidak rusak.
    /// </summary>
    private IEnumerator TypewriterRoutine(string text)
    {
        _isTyping = true;
        dialogBodyText.text = text;
        dialogBodyText.maxVisibleCharacters = 0;
        dialogBodyText.ForceMeshUpdate();
        int total = dialogBodyText.textInfo.characterCount;

        for (int i = 0; i <= total; i++)
        {
            dialogBodyText.maxVisibleCharacters = i;
            if (blipClip != null && voiceBlipSource != null && i % 2 == 0)
                voiceBlipSource.PlayOneShot(blipClip, 0.4f);
            yield return new WaitForSeconds(typeSpeed);
        }

        _isTyping = false;
    }

    private void OnSkip()
    {
        if (_isTyping)
        {
            if (_typingRoutine != null) StopCoroutine(_typingRoutine);
            dialogBodyText.maxVisibleCharacters = int.MaxValue;
            _isTyping = false;
        }
        else
        {
            OnContinue();
        }
    }

    private void OnContinue()
    {
        if (_isTyping) { OnSkip(); return; }
        GameManager.Instance.ChangeState(GameState.Drawing);
        GameManager.Instance.LoadScene("04_BatikDrawing");
    }
}
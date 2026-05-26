using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Controller scene Main Menu.
/// Menyediakan:
/// - Tombol Play → cek save: jika ada tampilkan list, jika tidak buat baru.
/// - Tombol New Save → input nama toko.
/// - Tombol Settings → buka panel settings.
/// - Tombol Quit.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject newSavePanel;
    public GameObject saveListPanel;
    public GameObject settingsPanel;

    [Header("New Save")]
    public TMP_InputField shopNameInput;
    public Button btnConfirmNewSave;
    public Button btnCancelNewSave;
    public TMP_Text validationLabel;

    [Header("Save List")]
    public Transform saveListContent;
    public GameObject saveEntryPrefab;
    public Button btnBackFromList;
    public Button btnNewSaveFromList;

    [Header("Main Buttons")]
    public Button btnPlay;
    public Button btnSettings;
    public Button btnQuit;

    private void Start()
    {
        // Hook buttons
        btnPlay.onClick.AddListener(OnPlay);
        btnSettings.onClick.AddListener(() => settingsPanel.SetActive(true));
        btnQuit.onClick.AddListener(QuitGame);

        btnConfirmNewSave.onClick.AddListener(OnConfirmNewSave);
        btnCancelNewSave.onClick.AddListener(() => newSavePanel.SetActive(false));
        btnBackFromList.onClick.AddListener(() => saveListPanel.SetActive(false));
        btnNewSaveFromList.onClick.AddListener(() => {
            saveListPanel.SetActive(false);
            newSavePanel.SetActive(true);
        });

        ShowOnly(mainPanel);
    }

    private void ShowOnly(GameObject panel)
    {
        newSavePanel.SetActive(panel == newSavePanel);
        saveListPanel.SetActive(panel == saveListPanel);
        settingsPanel.SetActive(panel == settingsPanel);
    }

    private void OnPlay()
    {
        //SaveSystem.Instance.DeleteSave("tes"); // testing
        var saves = SaveSystem.Instance.GetAllSaves();
        if (saves.Count == 0)
        {
            ShowOnly(newSavePanel);
        }
        else
        {
            RefreshSaveList(saves);
            ShowOnly(saveListPanel);
        }
    }

    private void RefreshSaveList(List<SaveData> saves)
    {
        foreach (Transform child in saveListContent) Destroy(child.gameObject);
        foreach (var s in saves)
        {
            GameObject entry = Instantiate(saveEntryPrefab, saveListContent);
            var label = entry.GetComponentInChildren<TMP_Text>();
            label.text = $"<b>{s.shopName}</b>\nDay {s.currentDay} • Rp {s.money:N0}\n<size=70%>{s.lastPlayed:yyyy-MM-dd HH:mm}</size>";

            var btn = entry.GetComponent<Button>();
            string capturedName = s.shopName;
            btn.onClick.AddListener(() => LoadSelectedSave(capturedName));

            // Tombol delete (opsional, child kedua)
            var deleteBtn = entry.transform.Find("DeleteButton")?.GetComponent<Button>();
            if (deleteBtn != null)
            {
                deleteBtn.onClick.AddListener(() => {
                    SaveSystem.Instance.DeleteSave(capturedName);
                    RefreshSaveList(SaveSystem.Instance.GetAllSaves());
                });
            }
        }
    }

    private void LoadSelectedSave(string shopName)
    {
        var data = SaveSystem.Instance.LoadGame(shopName);
        if (data == null)
        {
            Debug.LogError($"[MainMenu] Gagal load save: {shopName}");
            return;
        }
        GameManager.Instance.LoadGame(data);
    }

    private void OnConfirmNewSave()
    {
        string name = shopNameInput.text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            validationLabel.text = "Nama toko tidak boleh kosong!";
            return;
        }
        if (name.Length > 24)
        {
            validationLabel.text = "Nama maksimal 24 karakter.";
            return;
        }
        // Cek nama duplikat
        foreach (var s in SaveSystem.Instance.GetAllSaves())
        {
            if (s.shopName.Equals(name, System.StringComparison.OrdinalIgnoreCase))
            {
                validationLabel.text = "Nama sudah dipakai!";
                return;
            }
        }
        validationLabel.text = "";
        GameManager.Instance.StartNewGame(name);
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
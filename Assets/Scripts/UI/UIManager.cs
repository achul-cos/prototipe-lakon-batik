using UnityEngine;
using TMPro;

/// <summary>
/// Singleton ringan yang menyediakan akses ke elemen UI global lintas scene.
/// Saat ini dipakai oleh GameManager.AddMoney() untuk update label uang,
/// dan oleh GameManager.TogglePause() untuk memberi tahu PauseClockUI.
///
/// Tidak wajib jika Anda sudah pakai event OnMoneyChanged di GameManager.
/// Bisa dikembangkan menjadi pusat notifikasi (toast, popup, dll).
/// </summary>
public class UIManager : Singleton<UIManager>
{
    protected override bool PersistBetweenScenes => false; // setiap scene pasang sendiri

    [Tooltip("Label uang di HUD, jika ada di scene ini")]
    public TMP_Text moneyLabel;

    public void UpdateMoneyDisplay(int amount)
    {
        if (moneyLabel != null)
            moneyLabel.text = $"Rp {amount:N0}";
    }

    public void ShowPauseClock(bool show)
    {
        var clock = FindObjectOfType<PauseClockUI>();
        if (clock == null) return;
        if (show) clock.Pause();
        else clock.Resume();
    }
}
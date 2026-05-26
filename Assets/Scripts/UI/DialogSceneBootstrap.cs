using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Bootstrap scene 03_Dialog.
///
/// Tugasnya hanya SATU: mengambil currentOrder dari GameManager
/// lalu menyerahkannya ke DialogManager untuk dijalankan.
///
/// Kenapa dipisah dari DialogManager?
/// DialogManager adalah Singleton yang bisa ada di banyak scene.
/// Bootstrap ini khusus untuk setup awal scene 03_Dialog saja,
/// termasuk mengisi sprite karakter dan latar dari data order.
///
/// Setup:
/// - Pasang script ini pada GameObject kosong "DialogSceneBootstrap".
/// - Drag referensi UI ke Inspector.
/// - DialogManager akan menangani typewriter dan highlight keyword.
/// </summary>
public class DialogSceneBootstrap : MonoBehaviour
{
    [Header("Visual")]
    [Tooltip("Image sprite pelanggan di sebelah kiri layar")]
    public Image characterImage;

    [Tooltip("Image latar belakang scene dialog")]
    public Image backgroundImage;

    [Header("Sprite Fallback")]
    [Tooltip("Sprite default jika pelanggan tidak punya sprite khusus")]
    public Sprite defaultCharacterSprite;

    [Tooltip("Sprite latar default jika tidak ada latar khusus")]
    public Sprite defaultBackgroundSprite;

    private void Start()
    {
        var order = GameManager.Instance?.currentOrder;

        if (order == null)
        {
            Debug.LogError(
                "[DialogSceneBootstrap] currentOrder NULL! " +
                "Pastikan player memilih pelanggan dari Lobby sebelum masuk scene ini.");
            return;
        }

        // Pasang sprite karakter
        // Catatan: CustomerOrder bisa ditambah field 'characterSprite'
        // jika ingin tiap pelanggan punya tampilan berbeda.
        // Untuk sekarang gunakan defaultCharacterSprite sebagai placeholder.
        if (characterImage != null)
        {
            characterImage.sprite = defaultCharacterSprite;
            characterImage.gameObject.SetActive(characterImage.sprite != null);
        }

        // Pasang latar
        if (backgroundImage != null && defaultBackgroundSprite != null)
            backgroundImage.sprite = defaultBackgroundSprite;

        // Mulai dialog — serahkan ke DialogManager
        if (DialogManager.Instance != null)
            DialogManager.Instance.BeginDialog(order);
        else
            Debug.LogError("[DialogSceneBootstrap] DialogManager tidak ditemukan di scene!");
    }
}
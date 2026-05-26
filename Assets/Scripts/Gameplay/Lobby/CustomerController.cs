using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Komponen yang dipasang pada prefab pelanggan di area tunggu.
/// Saat di-klik, akan memuat scene Dialog & menyiapkan currentOrder.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CustomerController : MonoBehaviour, IPointerClickHandler
{
    public CustomerOrder order;
    public SpriteRenderer portrait; // visual sederhana

    public void SetOrder(CustomerOrder o)
    {
        order = o;
        // Bisa ditambah: ubah sprite berdasarkan profil pelanggan
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (GameManager.Instance.isPaused) return;

        GameManager.Instance.currentOrder = order;
        GameManager.Instance.ChangeState(GameState.Dialog);
        GameManager.Instance.LoadScene("03_Dialog");
    }
}
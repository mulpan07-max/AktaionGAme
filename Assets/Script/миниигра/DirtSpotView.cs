using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Повесить на КАЖДОЕ пятно (Image). Показывает прозрачность из OrganDirt.
// Tappable = true  -> по пятну можно тапать (панель осмотра)
// Tappable = false -> только показывает состояние (другое место, где стоит орган)
[RequireComponent(typeof(Image))]
public class DirtSpotView : MonoBehaviour, IPointerClickHandler
{
    [Tooltip("Номер пятна 0..3. Одно и то же пятно в разных местах имеет ОДИНАКОВЫЙ номер")]
    [SerializeField] private int spotIndex;
    [SerializeField] private bool tappable = true;

    private Image img;

    private void Awake()
    {
        img = GetComponent<Image>();
        img.raycastTarget = tappable;   // в неинтерактивном месте пятно не перехватывает нажатия
    }

    private void OnEnable()
    {
        if (OrganDirt.Instance != null) OrganDirt.Instance.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (OrganDirt.Instance != null) OrganDirt.Instance.Changed -= Refresh;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (tappable && OrganDirt.Instance != null) OrganDirt.Instance.Tap(spotIndex);
    }

    private void Refresh()
    {
        if (img == null || OrganDirt.Instance == null) return;
        Color c = img.color;
        c.a = OrganDirt.Instance.GetAlpha(spotIndex);
        img.color = c;
    }
}

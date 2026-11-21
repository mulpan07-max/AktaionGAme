using UnityEngine;
using UnityEngine.EventSystems;

// Повесить на лекарство на столе (Image с Raycast Target = ON).
// Тащим на орган: лекарство исчезает, орган светлеет. Промахнулись -> лекарство возвращается.
public class MedicineDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private RectTransform organZone;       // область органа (можно сам орган)
    [SerializeField] private OrganTreatment treatment;
    [Tooltip("Лекарство можно использовать только на чистом органе (после мытья)")]
    [SerializeField] private bool requireClean = true;

    private RectTransform rt;
    private Vector3 home;
    private int siblingIndex;
    private bool dragging;

    private void Awake()
    {
        rt = (RectTransform)transform;
        home = rt.localPosition;
    }

    private bool CanDrag()
    {
        if (treatment != null && treatment.IsTreated) return false;
        if (requireClean) return OrganDirt.Instance != null && OrganDirt.Instance.IsClean;
        return true;
    }

    public void OnBeginDrag(PointerEventData e)
    {
        dragging = CanDrag();
        if (!dragging) return;
        siblingIndex = rt.GetSiblingIndex();
        rt.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData e)
    {
        if (!dragging) return;
        var parent = rt.parent as RectTransform;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, e.position, e.pressEventCamera, out Vector2 local))
            rt.localPosition = local;
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (!dragging) return;
        dragging = false;

        bool onOrgan = organZone != null &&
            RectTransformUtility.RectangleContainsScreenPoint(organZone, e.position, e.pressEventCamera);

        if (onOrgan && treatment != null && treatment.TryApply())
        {
            rt.localPosition = home;                 // чтобы при следующей операции стояло на месте
            rt.SetSiblingIndex(siblingIndex);
            gameObject.SetActive(false);             // лекарство исчезает
        }
        else
        {
            rt.localPosition = home;
            rt.SetSiblingIndex(siblingIndex);
        }
    }
}
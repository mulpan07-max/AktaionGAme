using System;
using UnityEngine;
using UnityEngine.EventSystems;

// Добавляется на предметы внутри тела (кость, сердце, лёгкие, лист, крест, кольцо).
// Настраивается автоматически из ItemsStage. Image предмета: Raycast Target = ON.
public class ItemDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [NonSerialized] public RectTransform dropZone;     // контейнер, задаётся из ItemsStage
    [NonSerialized] public bool canDrag = true;

    public event Action<ItemDrag> Grabbed;             // предмет взяли
    public event Action<ItemDrag, bool> Released;      // отпустили (попал ли в контейнер)

    private RectTransform rt;
    private Vector3 home;
    private int siblingIndex;
    private bool inited, dragging;

    private void Init()
    {
        if (inited) return;
        inited = true;
        rt = (RectTransform)transform;
        home = rt.localPosition;
        siblingIndex = rt.GetSiblingIndex();
    }

    private void OnEnable() { Init(); }

    public void OnBeginDrag(PointerEventData e)
    {
        Init();
        dragging = canDrag;
        if (!dragging) return;
        siblingIndex = rt.GetSiblingIndex();
        rt.SetAsLastSibling();                         // поверх остальных
        Grabbed?.Invoke(this);
    }

    public void OnDrag(PointerEventData e)
    {
        if (!dragging || !canDrag) return;
        var parent = rt.parent as RectTransform;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, e.position, e.pressEventCamera, out Vector2 local))
            rt.localPosition = local;
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (!dragging) return;
        dragging = false;

        if (!canDrag) { ResetPosition(); return; }     // этап заблокировали во время перетаскивания

        bool inZone = dropZone != null &&
            RectTransformUtility.RectangleContainsScreenPoint(dropZone, e.position, e.pressEventCamera);

        if (!inZone) ResetPosition();                  // мимо контейнера: предмет возвращается
        Released?.Invoke(this, inZone);
    }

    public void ResetPosition()
    {
        Init();
        rt.localPosition = home;
        rt.SetSiblingIndex(siblingIndex);
    }

    public void SnapTo(RectTransform slot)
    {
        Init();
        if (slot != null) rt.position = slot.position;
    }
}
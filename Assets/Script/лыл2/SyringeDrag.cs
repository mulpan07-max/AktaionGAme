using System;
using UnityEngine;
using UnityEngine.EventSystems;

// Повесить на шприц (родительский объект с Image и Raycast Target = ON).
// Тащим к телу и отпускаем: если попали в dropZone - вызывается Released(true), иначе шприц возвращается.
public class SyringeDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public RectTransform dropZone;           // область тела, куда надо поднести шприц
    public bool canDrag = true;
    public event Action<bool> Released;      // попал ли в зону

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
    }

    private void OnEnable() { Init(); }

    public void OnBeginDrag(PointerEventData e)
    {
        Init();
        dragging = canDrag;
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

        bool inZone = dropZone != null &&
            RectTransformUtility.RectangleContainsScreenPoint(dropZone, e.position, e.pressEventCamera);

        if (!inZone)
        {
            rt.localPosition = home;
            rt.SetSiblingIndex(siblingIndex);
        }
        Released?.Invoke(inZone);
    }

    public void SnapTo(RectTransform point) { Init(); if (point != null) rt.position = point.position; }

    public void ResetState()
    {
        Init();
        canDrag = true;
        dragging = false;
        rt.localPosition = home;
    }
}
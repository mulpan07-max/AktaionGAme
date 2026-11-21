using System;
using UnityEngine;
using UnityEngine.EventSystems;

// Добавляется на круги автоматически из TapZonesStage. Реагирует на нажатие мгновенно.
public class TapZone: MonoBehaviour, IPointerDownHandler
{
    [NonSerialized] public int index;
    public event Action<int> Tapped;

    public void OnPointerDown(PointerEventData eventData)
    {
        Tapped?.Invoke(index);
    }
}

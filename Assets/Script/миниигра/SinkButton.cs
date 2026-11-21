using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

// ѕовесить на прозрачный Image (Raycast Target = ON), который лежит поверх раковины на столе.
// “ап -> вызываетс€ onTap (открыть панель крана).
public class SinkButton : MonoBehaviour, IPointerClickHandler
{
    [Tooltip("–аковина работает только после того, как тапнуты все 4 п€тна")]
    [SerializeField] private bool requireAllSpotsTouched = true;
    public UnityEvent onTap;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (requireAllSpotsTouched && (OrganDirt.Instance == null || !OrganDirt.Instance.AllSpotsTouched))
            return;
        onTap.Invoke();
    }
}

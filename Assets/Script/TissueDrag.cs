
using UnityEngine;
using UnityEngine.EventSystems;

public class TissueDrag : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [Header("Settings")]
    [SerializeField] private bool isLeftTissue;
    [SerializeField] private float requiredDistance = 110f;
    [SerializeField] private float maxDistance = 150f;

    [Header("Manager")]
    [SerializeField] private TissueOpeningManager openingManager;

    private RectTransform rectTransform;
    private RectTransform parentRect;

    private Vector2 initialPosition;
    private float currentDistance;
    private bool isOpened;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        parentRect = rectTransform.parent as RectTransform;
        initialPosition = rectTransform.anchoredPosition;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Drag begins here.
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isOpened || openingManager == null)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 pointerPosition))
        {
            return;
        }

        float direction = isLeftTissue ? -1f : 1f;

        float distance = direction *
            (pointerPosition.x - initialPosition.x);

        currentDistance = Mathf.Clamp(
            distance,
            0f,
            maxDistance
        );

        rectTransform.anchoredPosition =
            initialPosition +
            new Vector2(direction * currentDistance, 0f);

        if (currentDistance >= requiredDistance)
        {
            isOpened = true;
            openingManager.TissueOpened(isLeftTissue);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isOpened)
            return;

        // Return to original position if not opened enough.
        rectTransform.anchoredPosition = initialPosition;
        currentDistance = 0f;
    }

    public void ResetTissue()
    {
        isOpened = false;
        currentDistance = 0f;

        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        rectTransform.anchoredPosition = initialPosition;
    }
}


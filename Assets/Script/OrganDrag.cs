
using UnityEngine;
using UnityEngine.EventSystems;

public class OrganDrag : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [Header("References")]
    [SerializeField] private RectTransform organContainer;
    [SerializeField] private SurgeryGameManager gameManager;

    private RectTransform rectTransform;
    private RectTransform parentRect;
    private Canvas rootCanvas;

    private Vector2 initialPosition;
    private Vector2 pointerOffset;

    private bool canDrag;
    private bool extracted;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        parentRect = rectTransform.parent as RectTransform;
        rootCanvas = GetComponentInParent<Canvas>().rootCanvas;

        initialPosition = rectTransform.anchoredPosition;
    }

    public void EnableExtraction()
    {
        canDrag = true;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!canDrag || extracted)
            return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint))
        {
            pointerOffset =
                rectTransform.anchoredPosition - localPoint;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!canDrag || extracted)
            return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint))
        {
            rectTransform.anchoredPosition =
                localPoint + pointerOffset;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!canDrag || extracted)
            return;

        bool insideContainer =
            RectTransformUtility.RectangleContainsScreenPoint(
                organContainer,
                eventData.position,
                eventData.pressEventCamera
            );

        if (insideContainer)
        {
            extracted = true;
            canDrag = false;

            gameManager.CompleteOperation();
        }
        else
        {
            rectTransform.anchoredPosition = initialPosition;
        }
    }

    public void ResetOrgan()
    {
        canDrag = false;
        extracted = false;

        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        rectTransform.anchoredPosition = initialPosition;
    }
}


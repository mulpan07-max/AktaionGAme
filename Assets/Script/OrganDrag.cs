using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class OrganDrag : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private SurgeryGameManager gameManager;
    [SerializeField] private RectTransform container;      // зона контейнера
    [SerializeField] private TMP_Text instructionText;     // тот же текст-подсказка

    [Header("Pulse")]
    [SerializeField] private float pulseAmount = 0.05f;
    [SerializeField] private float pulseSpeed = 3f;

    [Header("Behaviour")]
    [SerializeField] private float returnSpeed = 12f;
    [SerializeField] private float completeDelay = 0.6f;

    private RectTransform rt;
    private RectTransform parentRect;
    private Vector3 startLocalPos;
    private Vector3 baseScale;
    private Vector2 grabOffset;
    private bool captured, grabbed, placed, returning;

    private void EnsureCaptured()
    {
        if (captured) return;
        rt = (RectTransform)transform;
        parentRect = rt.parent as RectTransform;
        startLocalPos = rt.localPosition;
        baseScale = rt.localScale;
        captured = true;
    }

    private void OnEnable()
    {
        EnsureCaptured();
        if (instructionText != null && !placed) instructionText.text = "TAP THE LIVER";
    }

    private void Update()
    {
        if (!captured || placed) return;

        if (returning)
        {
            rt.localPosition = Vector3.Lerp(rt.localPosition, startLocalPos, 1f - Mathf.Exp(-returnSpeed * Time.unscaledDeltaTime));
            if ((rt.localPosition - startLocalPos).sqrMagnitude < 0.25f)
            {
                rt.localPosition = startLocalPos;
                returning = false;
            }
        }

        // Пульсация-подсказка, пока орган не взят
        if (!grabbed)
            rt.localScale = baseScale * (1f + Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseAmount);
    }

    public void OnPointerDown(PointerEventData e)
    {
        EnsureCaptured();
        if (placed) return;

        grabbed = true;
        returning = false;
        rt.localScale = baseScale;
        rt.SetAsLastSibling(); // над контейнером

        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, e.position, e.pressEventCamera, out Vector2 local);
        grabOffset = (Vector2)rt.localPosition - local;

        if (instructionText != null) instructionText.text = "DRAG IT TO THE CONTAINER";
    }

    public void OnDrag(PointerEventData e)
    {
        if (!grabbed || placed) return;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, e.position, e.pressEventCamera, out Vector2 local))
            rt.localPosition = local + grabOffset;
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (!grabbed || placed) return;
        grabbed = false;

        if (container != null &&
            RectTransformUtility.RectangleContainsScreenPoint(container, e.position, e.pressEventCamera))
        {
            placed = true;
            rt.position = container.position; // ложим в центр контейнера
            if (instructionText != null) instructionText.text = "ORGAN EXTRACTED";
            Invoke(nameof(Finish), completeDelay);
        }
        else
        {
            returning = true;
            if (instructionText != null) instructionText.text = "DRAG IT TO THE CONTAINER";
        }
    }

    private void Finish()
    {
        if (gameManager != null) gameManager.CompleteOperation();
    }

    public void ResetOrgan()
    {
        EnsureCaptured();
        CancelInvoke();
        grabbed = placed = returning = false;
        rt.localPosition = startLocalPos;
        rt.localScale = baseScale;
    }
}


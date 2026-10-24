using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Плавно увеличивает кнопку при наведении и нажатии.
/// Повесьте на каждую кнопку (рядом с компонентом Button).
/// </summary>
public class ButtonScaleEffect : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private float hoverScale = 1.1f;   // при наведении мыши
    [SerializeField] private float pressedScale = 1.2f; // при нажатии
    [SerializeField] private float speed = 12f;         // скорость анимации

    private Vector3 baseScale;
    private float targetMultiplier = 1f;
    private bool isHovered;
    private bool isPressed;

    private void Awake()
    {
        baseScale = transform.localScale;
    }

    private void OnEnable()
    {
        isHovered = false;
        isPressed = false;
        targetMultiplier = 1f;
    }

    private void Update()
    {
        // Если какой-то другой скрипт (например, появление меню) ещё анимирует кнопку,
        // подхватываем масштаб, когда он стал нормальным.
        if (!isHovered && !isPressed && targetMultiplier == 1f
            && Mathf.Approximately(transform.localScale.x, 1f))
            baseScale = Vector3.one;

        Vector3 target = baseScale * targetMultiplier;
        transform.localScale = Vector3.Lerp(transform.localScale, target,
            Time.unscaledDeltaTime * speed);
    }

    private void UpdateTarget()
    {
        if (isPressed) targetMultiplier = pressedScale;
        else if (isHovered) targetMultiplier = hoverScale;
        else targetMultiplier = 1f;
    }

    public void OnPointerEnter(PointerEventData e) { isHovered = true; UpdateTarget(); }
    public void OnPointerExit(PointerEventData e) { isHovered = false; isPressed = false; UpdateTarget(); }
    public void OnPointerDown(PointerEventData e) { isPressed = true; UpdateTarget(); }
    public void OnPointerUp(PointerEventData e) { isPressed = false; UpdateTarget(); }
}

using UnityEngine;

// Выдвижная плашка: свайп вправо из левой части открывает, свайп влево закрывает
public class SideDrawer : MonoBehaviour
{
    [Header("Плашка")]
    public RectTransform drawer;

    [Header("Позиции (Anchored Position, как в Inspector)")]
    public Vector2 hiddenPosition = new Vector2(-1542.849f, -541.045f); // где плашка в начале (закрыта)
    [Tooltip("Включено: открытой считается позиция, в которой плашка стоит в сцене. " +
             "Выключено: берётся Shown Position ниже.")]
    public bool useSceneAsShownPosition = true;
    public Vector2 shownPosition = Vector2.zero;

    [Header("Настройки")]
    [Range(0.05f, 0.6f)] public float openZone = 0.3f;
    public float minSwipeDistance = 100f;
    public float slideSpeed = 12f;

    bool isOpen;
    bool isSwiping;
    bool startedInZone;
    Vector2 startPos;

    void Start()
    {
        if (drawer == null) return;

        // Якоря и pivot НЕ меняем: остаются такими, как настроены в Inspector
        if (useSceneAsShownPosition)
            shownPosition = drawer.anchoredPosition;

        drawer.anchoredPosition = hiddenPosition;
    }

    void Update()
    {
        if (drawer == null) return;

        ReadInput();

        Vector2 target = isOpen ? shownPosition : hiddenPosition;
        drawer.anchoredPosition = Vector2.Lerp(
            drawer.anchoredPosition, target,
            1f - Mathf.Exp(-slideSpeed * Time.unscaledDeltaTime));
    }

    void ReadInput()
    {
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began) BeginSwipe(t.position);
            else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) EndSwipe(t.position);
            return;
        }

#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0)) BeginSwipe(Input.mousePosition);
        else if (Input.GetMouseButtonUp(0)) EndSwipe(Input.mousePosition);
#endif
    }

    void BeginSwipe(Vector2 pos)
    {
        startPos = pos;
        isSwiping = true;
        startedInZone = pos.x <= Screen.width * openZone;
    }

    void EndSwipe(Vector2 pos)
    {
        if (!isSwiping) return;
        isSwiping = false;

        Vector2 delta = pos - startPos;
        if (Mathf.Abs(delta.x) < minSwipeDistance) return;
        if (Mathf.Abs(delta.x) < Mathf.Abs(delta.y)) return;

        if (delta.x > 0 && !isOpen && startedInZone) isOpen = true;
        else if (delta.x < 0 && isOpen) isOpen = false;
    }

    public void Open() => isOpen = true;
    public void Close() => isOpen = false;
}
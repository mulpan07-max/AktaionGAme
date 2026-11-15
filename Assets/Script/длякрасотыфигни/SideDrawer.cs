using UnityEngine;

// Выдвижная плашка слева: свайп вправо от левой части открывает, свайп влево закрывает
public class SideDrawer : MonoBehaviour
{
    [Header("Плашка")]
    public RectTransform drawer;          // сама плашка

    [Header("Настройки")]
    [Range(0.05f, 0.6f)] public float openZone = 0.3f;   // левая часть экрана (доля ширины), где свайп открывает
    public float minSwipeDistance = 100f;                // минимальная длина свайпа, px
    public float slideSpeed = 12f;                       // скорость анимации

    bool isOpen;
    bool isSwiping;
    bool startedInZone;
    Vector2 startPos;
    float hiddenX, shownX;

    void Start()
    {
        if (drawer == null) return;

        // Привязываем плашку к левому краю
        drawer.anchorMin = new Vector2(0f, drawer.anchorMin.y);
        drawer.anchorMax = new Vector2(0f, drawer.anchorMax.y);
        drawer.pivot = new Vector2(0f, drawer.pivot.y);

        shownX = 0f;
        hiddenX = -drawer.rect.width;
        drawer.anchoredPosition = new Vector2(hiddenX, drawer.anchoredPosition.y);
    }

    void Update()
    {
        if (drawer == null) return;

        ReadInput();

        // Плавное движение к нужной позиции
        float targetX = isOpen ? shownX : hiddenX;
        Vector2 pos = drawer.anchoredPosition;
        pos.x = Mathf.Lerp(pos.x, targetX, 1f - Mathf.Exp(-slideSpeed * Time.unscaledDeltaTime));
        drawer.anchoredPosition = pos;
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
        if (Mathf.Abs(delta.x) < Mathf.Abs(delta.y)) return; // вертикальный свайп игнорируем

        if (delta.x > 0 && !isOpen && startedInZone) isOpen = true;   // вправо из левой части -> открыть
        else if (delta.x < 0 && isOpen) isOpen = false;               // влево -> закрыть
    }

    // Можно вызывать с кнопок
    public void Open() => isOpen = true;
    public void Close() => isOpen = false;
}

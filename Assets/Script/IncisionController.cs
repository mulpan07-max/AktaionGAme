using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class IncisionController : MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IPointerUpHandler
{
    [Header("Incision Points")]
    [SerializeField] private RectTransform startPoint;
    [SerializeField] private RectTransform endPoint;
    [SerializeField] private RectTransform incisionArea;

    [Header("Settings")]
    [SerializeField] private float tolerance = 40f;
    [SerializeField] private float startRadius = 50f;
    [SerializeField] private float endRadius = 50f;

    [Header("UI")]
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private Image guideLine;
    [Tooltip("Прозрачность линии-подсказки: 0 = невидима, 1 = полностью видна.")]
    [SerializeField, Range(0f, 1f)] private float guideLineAlpha = 0f;

    [Header("Body Images")]
    [Tooltip("Image с картинкой тела. Должна быть ВНЕ объекта с этим скриптом.")]
    [SerializeField] private Image bodyImage;
    [Tooltip("Тело слегка разрезано (пока игрок режет).")]
    [SerializeField] private Sprite cutInProgressSprite;
    [Tooltip("Тело после полного разреза. Если пусто - вернётся обычная картинка.")]
    [SerializeField] private Sprite cutCompletedSprite;

    [Header("Game Manager")]
    [SerializeField] private SurgeryGameManager gameManager;
    [SerializeField] private GameObject tissueOpeningArea;

    private Sprite normalSprite;

    private bool isCutting = false;
    private bool incisionCompleted = false;
    private bool mistakeMade = false;

    private float lastProgress = 0f;

    private void Awake()
    {
        SetGuideColor(Color.yellow);
    }

    // Меняет цвет линии, но сохраняет заданную прозрачность
    private void SetGuideColor(Color color)
    {
        if (guideLine == null)
            return;

        color.a = guideLineAlpha;
        guideLine.color = color;
    }

    // Запоминаем обычную картинку тела один раз
    private void CacheNormalSprite()
    {
        if (normalSprite == null && bodyImage != null)
            normalSprite = bodyImage.sprite;
    }

    private void SetBodySprite(Sprite sprite)
    {
        if (bodyImage == null || sprite == null)
            return;

        bodyImage.sprite = sprite;
    }

    private void RestoreNormalBody()
    {
        CacheNormalSprite();
        SetBodySprite(normalSprite);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (incisionCompleted)
            return;

        if (!GetLocalPosition(eventData, out Vector2 position))
            return;

        Vector2 start = startPoint.anchoredPosition;

        if (Vector2.Distance(position, start) <= startRadius)
        {
            isCutting = true;
            mistakeMade = false;
            lastProgress = 0f;

            // Игрок начал резать - показываем слегка разрезанное тело
            CacheNormalSprite();
            SetBodySprite(cutInProgressSprite);

            instructionText.text = "FOLLOW THE LINE";
        }
        else
        {
            instructionText.text = "START FROM THE GREEN POINT";
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isCutting || incisionCompleted)
            return;

        if (!GetLocalPosition(eventData, out Vector2 position))
            return;

        Vector2 start = startPoint.anchoredPosition;
        Vector2 end = endPoint.anchoredPosition;

        Vector2 line = end - start;
        float lineLengthSquared = line.sqrMagnitude;

        if (lineLengthSquared < 0.001f)
            return;

        float progress = Vector2.Dot(position - start, line)
                         / lineLengthSquared;

        Vector2 closestPoint =
            start + line * Mathf.Clamp01(progress);

        float distance =
            Vector2.Distance(position, closestPoint);

        if (distance > tolerance)
        {
            MakeIncisionMistake();
            return;
        }

        // Prevent skipping most of the incision.
        if (progress > lastProgress + 0.25f)
        {
            instructionText.text = "MOVE SLOWLY ALONG THE LINE";
            return;
        }

        if (progress < lastProgress - 0.05f)
        {
            instructionText.text = "FOLLOW THE LINE DOWNWARD";
            return;
        }

        lastProgress = Mathf.Max(
            lastProgress,
            Mathf.Clamp01(progress)
        );

        if (lastProgress >= 0.95f &&
            Vector2.Distance(position, end) <= endRadius)
        {
            CompleteIncision();
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (incisionCompleted)
            return;

        if (isCutting)
        {
            isCutting = false;

            // Не дорезали - тело возвращается в обычное состояние
            RestoreNormalBody();

            instructionText.text = "INCOMPLETE CUT - TRY AGAIN";
        }
    }

    private bool GetLocalPosition(
        PointerEventData eventData,
        out Vector2 localPosition)
    {
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            incisionArea,
            eventData.position,
            eventData.pressEventCamera,
            out localPosition
        );
    }

    private void MakeIncisionMistake()
    {
        if (!mistakeMade)
        {
            mistakeMade = true;
            gameManager.MakeMistake();
        }

        isCutting = false;

        // Ошибка - начинаем заново, тело в обычном состоянии
        RestoreNormalBody();

        instructionText.text = "INCORRECT CUT! TRY AGAIN";
    }

    private void CompleteIncision()
    {
        incisionCompleted = true;
        isCutting = false;

        instructionText.text = "INCISION COMPLETE";

        SetGuideColor(Color.green);

        // Разрез закончен: если задана картинка "полностью разрезано" - ставим её,
        // иначе возвращаем обычную
        if (cutCompletedSprite != null)
            SetBodySprite(cutCompletedSprite);
        else
            RestoreNormalBody();

        // Hide incision interaction.
        gameObject.SetActive(false);

        // Show tissue opening stage.
        if (tissueOpeningArea != null)
            tissueOpeningArea.SetActive(true);

        instructionText.text = "DRAG THE TISSUES APART";
    }

    public void ResetIncision()
    {
        isCutting = false;
        incisionCompleted = false;
        mistakeMade = false;
        lastProgress = 0f;

        // Перезапуск операции - тело снова целое
        RestoreNormalBody();

        SetGuideColor(Color.yellow);

        instructionText.text = "DRAG FROM START TO END";
    }
}
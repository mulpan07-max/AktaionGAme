using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TissueOpeningManager : MonoBehaviour
{
    [Header("Tissues")]
    [SerializeField] private TissueDrag leftTissue;
    [SerializeField] private TissueDrag rightTissue;
    [Tooltip("Сделать LeftTissue и RightTissue невидимыми (нажатия при этом продолжают работать).")]
    [SerializeField] private bool hideTissues = true;

    [Header("Body (одна Image на всё тело)")]
    [Tooltip("Та же Image с телом, что и в IncisionController.")]
    [SerializeField] private Image bodyImage;
    [Tooltip("Нулевая стадия. Если пусто - берётся картинка, которая стоит на теле " +
             "в момент начала раскрытия (то есть последняя картинка из IncisionController).")]
    [SerializeField] private Sprite zeroStageSprite;

    [Header("Left Stages")]
    [Tooltip("1-й = Left 20, 2-й = Left 40, 3-й = Left 60, 4-й = Left 80, 5-й = Left 100.")]
    [SerializeField] private Sprite[] leftStages;

    [Header("Right Stages")]
    [Tooltip("1-й = Right 20, 2-й = Right 40, 3-й = Right 60, 4-й = Right 80, 5-й = Right 100.")]
    [SerializeField] private Sprite[] rightStages;

    [Header("Настройки стадий")]
    [Tooltip("Включено: как только игрок потянул ткань - сразу 1-я стадия (Left 20 / Right 20), " +
             "дальше стадии меняются каждые 20%. Выключено: 1-я стадия появляется только после 20%.")]
    [SerializeField] private bool firstStageOnGrab = true;
    [Tooltip("Необязательно: картинка, когда раскрыты ОБЕ стороны.")]
    [SerializeField] private Sprite bothOpenedSprite;

    [Header("Автоотслеживание (работает без правок в TissueDrag)")]
    [Tooltip("Менеджер сам следит, как далеко сдвинули ткань, и считает прогресс.")]
    [SerializeField] private bool autoTrackPosition = true;
    [Tooltip("На сколько пикселей нужно сдвинуть ткань от начальной точки, чтобы это было 100%.")]
    [SerializeField] private float openDistance = 300f;

    [Header("UI")]
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private GameObject vesselStage;

    private bool leftOpened;
    private bool rightOpened;

    private float leftProgress;
    private float rightProgress;

    private Sprite baseSprite; // нулевая стадия

    private void Start()
    {
        if (hideTissues)
        {
            HideGraphics(leftTissue);
            HideGraphics(rightTissue);
        }
    }

    private bool leftCaptured, rightCaptured;
    private Vector2 leftStart, rightStart;

    private void Update()
    {
        if (!autoTrackPosition) return;

        TrackTissue(leftTissue, true, ref leftCaptured, ref leftStart);
        TrackTissue(rightTissue, false, ref rightCaptured, ref rightStart);
    }

    // Считает прогресс по смещению ткани от начальной точки
    private void TrackTissue(TissueDrag tissue, bool isLeft, ref bool captured, ref Vector2 start)
    {
        if (tissue == null || !tissue.gameObject.activeInHierarchy)
            return;

        if (isLeft ? leftOpened : rightOpened)
            return;

        RectTransform rt = tissue.transform as RectTransform;
        if (rt == null)
            return;

        // Запоминаем закрытое положение
        if (!captured)
        {
            start = rt.anchoredPosition;
            captured = true;
            return;
        }

        float progress = Vector2.Distance(rt.anchoredPosition, start) / Mathf.Max(openDistance, 1f);
        float last = isLeft ? leftProgress : rightProgress;

        if (Mathf.Abs(Mathf.Clamp01(progress) - last) > 0.001f)
            ReportProgress(isLeft, progress);
    }

    // Делает картинку прозрачной, но оставляет Raycast Target, чтобы можно было тянуть
    private void HideGraphics(TissueDrag tissue)
    {
        if (tissue == null) return;

        foreach (Graphic g in tissue.GetComponentsInChildren<Graphic>(true))
        {
            Color c = g.color;
            c.a = 0f;
            g.color = c;
            g.raycastTarget = true;
        }
    }

    // ===== Прогресс раскрытия =====

    /// <summary>
    /// Вызывается из TissueDrag во время перетаскивания.
    /// progress: от 0 (закрыто) до 1 (полностью раскрыто).
    /// </summary>
    public void ReportProgress(bool isLeft, float progress)
    {
        if (bodyImage == null)
            return;

        // Запоминаем нулевую стадию ДО первого изменения картинки
        if (baseSprite == null)
            baseSprite = zeroStageSprite != null ? zeroStageSprite : bodyImage.sprite;

        progress = Mathf.Clamp01(progress);

        if (isLeft) leftProgress = progress;
        else rightProgress = progress;

        Sprite[] stages = isLeft ? leftStages : rightStages;
        if (stages == null || stages.Length == 0)
            return;

        int stage = GetStage(progress, stages.Length);

        if (stage == 0)
        {
            // Меньше первой стадии - нулевая стадия (картинка после разреза)
            if (baseSprite != null)
                bodyImage.sprite = baseSprite;
        }
        else if (stages[stage - 1] != null)
        {
            bodyImage.sprite = stages[stage - 1];
        }
    }

    // 0 = нулевая стадия, 1..count = стадии спрайтов
    private int GetStage(float progress, int count)
    {
        if (firstStageOnGrab)
        {
            if (progress <= 0.0001f) return 0;
            return Mathf.Clamp(Mathf.CeilToInt(progress * count), 1, count);
        }

        return Mathf.Clamp(Mathf.FloorToInt(progress * count + 0.0001f), 0, count);
    }

    // ===== Старая логика =====

    public void TissueOpened(bool isLeft)
    {
        if (isLeft)
            leftOpened = true;
        else
            rightOpened = true;

        // Сторона раскрыта полностью - показываем её последнюю стадию
        ReportProgress(isLeft, 1f);

        if (leftOpened && rightOpened)
        {
            if (bothOpenedSprite != null && bodyImage != null)
                bodyImage.sprite = bothOpenedSprite;

            instructionText.text = "CUT VESSEL 1";

            if (vesselStage != null)
                vesselStage.SetActive(true);
        }
        else
        {
            instructionText.text = "OPEN THE OTHER SIDE";
        }
    }

    public void ResetOpening()
    {
        leftOpened = false;
        rightOpened = false;
        leftProgress = 0f;
        rightProgress = 0f;
        baseSprite = null;
        leftCaptured = false;
        rightCaptured = false;

        if (leftTissue != null)
            leftTissue.ResetTissue();

        if (rightTissue != null)
            rightTissue.ResetTissue();

        // Картинку тела здесь не трогаем: при перезапуске её возвращает
        // IncisionController.ResetIncision() (он вызывается раньше).
    }
}
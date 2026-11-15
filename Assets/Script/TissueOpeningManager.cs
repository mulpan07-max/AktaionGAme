using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TissueOpeningManager : MonoBehaviour
{
    [Header("Tissues")]
    [SerializeField] private TissueDrag leftTissue;
    [SerializeField] private TissueDrag rightTissue;
    [SerializeField] private bool hideTissues = true;

    [Header("Body")]
    [SerializeField] private Image bodyImage;
    [SerializeField] private Sprite zeroStageSprite;

    [Header("Stages (Left/Right 20..100)")]
    [SerializeField] private Sprite[] leftStages;
    [SerializeField] private Sprite[] rightStages;
    [SerializeField] private bool firstStageOnGrab = true;
    [SerializeField] private Sprite bothOpenedSprite;

    [Header("Auto tracking")]
    [SerializeField] private bool autoTrackPosition = true;
    [SerializeField] private float openDistance = 300f;


    [Header("Stages when the other side is already open")]
    [SerializeField] private Sprite[] rightStagesWhenLeftOpen;
    [SerializeField] private Sprite[] leftStagesWhenRightOpen;

    [Header("UI / next stage")]
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private GameObject organStage;   // печень + контейнер

    private bool leftOpened, rightOpened;
    private float leftProgress, rightProgress;
    private Sprite baseSprite;
    private bool leftCaptured, rightCaptured;
    private Vector2 leftStart, rightStart;

    private void Start()
    {
        if (hideTissues)
        {
            HideGraphics(leftTissue);
            HideGraphics(rightTissue);
        }
    }

    private void Update()
    {
        if (!autoTrackPosition) return;
        TrackTissue(leftTissue, true, ref leftCaptured, ref leftStart);
        TrackTissue(rightTissue, false, ref rightCaptured, ref rightStart);
    }

    private void TrackTissue(TissueDrag tissue, bool isLeft, ref bool captured, ref Vector2 start)
    {
        if (tissue == null || !tissue.gameObject.activeInHierarchy) return;
        if (isLeft ? leftOpened : rightOpened) return;

        RectTransform rt = tissue.transform as RectTransform;
        if (rt == null) return;

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

    public void ReportProgress(bool isLeft, float progress)
    {
        if (bodyImage == null) return;
        if (baseSprite == null)
            baseSprite = zeroStageSprite != null ? zeroStageSprite : bodyImage.sprite;

        progress = Mathf.Clamp01(progress);
        if (isLeft) leftProgress = progress; else rightProgress = progress;

        bool otherOpened = isLeft ? rightOpened : leftOpened;

        // Выбираем набор: если другая сторона уже открыта, берём набор с ней
        Sprite[] stages = isLeft ? leftStages : rightStages;
        if (otherOpened)
        {
            Sprite[] combined = isLeft ? leftStagesWhenRightOpen : rightStagesWhenLeftOpen;
            if (combined != null && combined.Length > 0) stages = combined;
        }
        if (stages == null || stages.Length == 0) return;

        int stage = GetStage(progress, stages.Length);

        if (stage == 0)
        {
            // Сторона ещё не тронута: если другая открыта, показываем её финальный вид
            Sprite[] otherFinal = isLeft ? rightStages : leftStages;
            if (otherOpened && otherFinal != null && otherFinal.Length > 0 && otherFinal[otherFinal.Length - 1] != null)
                bodyImage.sprite = otherFinal[otherFinal.Length - 1];
            else if (baseSprite != null)
                bodyImage.sprite = baseSprite;
        }
        else if (stages[stage - 1] != null)
        {
            bodyImage.sprite = stages[stage - 1];
        }
    }

    private int GetStage(float progress, int count)
    {
        if (firstStageOnGrab)
        {
            if (progress <= 0.0001f) return 0;
            return Mathf.Clamp(Mathf.CeilToInt(progress * count), 1, count);
        }
        return Mathf.Clamp(Mathf.FloorToInt(progress * count + 0.0001f), 0, count);
    }

    // Вызывается из TissueDrag, когда сторона раскрыта
    public void TissueOpened(bool isLeft)
    {
        if (isLeft) leftOpened = true; else rightOpened = true;
        ReportProgress(isLeft, 1f);

        if (leftOpened && rightOpened)
        {
            if (bothOpenedSprite != null && bodyImage != null)
                bodyImage.sprite = bothOpenedSprite;

            if (instructionText != null) instructionText.text = "TAP THE LIVER";
            if (organStage != null) organStage.SetActive(true);
        }
        else if (instructionText != null)
        {
            instructionText.text = "OPEN THE OTHER SIDE";
        }
    }

    public void ResetOpening()
    {
        leftOpened = rightOpened = false;
        leftProgress = rightProgress = 0f;
        baseSprite = null;
        leftCaptured = rightCaptured = false;

        if (leftTissue != null) leftTissue.ResetTissue();
        if (rightTissue != null) rightTissue.ResetTissue();
    }
}
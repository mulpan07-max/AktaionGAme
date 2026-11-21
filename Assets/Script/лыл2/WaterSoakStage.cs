using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Раковина: лёгкое лежит под водой, индикатор сверху наполняется, вода желтеет.
// Когда индикатор в красной зоне - надо резко тапнуть по экрану: вода исчезает, старый орган
// заменяется новым чистым. Не успели - штраф к качеству.
// Повесить на ПОЛНОЭКРАННЫЙ прозрачный Image (Raycast Target = ON), который лежит ВЫШЕ органа и воды,
// но НИЖЕ кнопок (например, кнопки X).
public class WaterSoakStage : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private SurgeryGameManager surgery;
    [Tooltip("Этап не начнётся, пока не закончится очистка пузырей (необязательно)")]
    [SerializeField] private BubbleCleaningStage prerequisite;

    [Header("Индикатор сверху")]
    [SerializeField] private Slider bar;                       // значение 0..1, Interactable выключен
    [Tooltip("Красная зона: ДОЧЕРНИЙ объект бара (Image), скрипт сам растянет его")]
    [SerializeField] private RectTransform redZone;
    [Range(0.5f, 0.95f)][SerializeField] private float redZoneStart = 0.8f;
    [SerializeField] private float soakDuration = 8f;          // сколько секунд заполняется индикатор
    [SerializeField] private GameObject tapPrompt;             // "TAP NOW!" (необязательно, выключен)

    [Header("Вода")]
    [SerializeField] private Graphic water;                    // Image воды (Raycast Target OFF)
    [SerializeField] private Color cleanColor = new Color(1f, 1f, 1f, 0.55f);
    [SerializeField] private Color dirtyColor = new Color(0.95f, 0.78f, 0.1f, 0.75f);
    [SerializeField] private float waterFadeTime = 0.25f;

    [Header("Органы")]
    [SerializeField] private GameObject oldOrgan;
    [SerializeField] private GameObject newOrgan;              // чистый, выключен по умолчанию

    [Header("Штрафы")]
    [SerializeField] private int failPenalty = 5;              // не успели достать
    [SerializeField] private int earlyTapPenalty = 0;          // тап до красной зоны (0 = просто игнорируется)

    [Header("Звук (необязательно)")]
    [SerializeField] private AudioSource sfx;
    [SerializeField] private AudioClip successSound;
    [SerializeField] private AudioClip failSound;

    [Header("Старт и конец")]
    [SerializeField] private float startDelay = 1f;
    [SerializeField] private float completeDelay = 0.8f;       // пауза, чтобы увидеть чистый орган
    public UnityEvent onCompleted;

    private bool started, running, completed, resolving;
    private float progress, delayTimer, resolveT, waterStartAlpha;

    public bool IsCompleted => completed;

    private void Update()
    {
        // после тапа: вода растворяется, потом событие
        if (resolving)
        {
            resolveT += Time.deltaTime;
            float k = waterFadeTime > 0f ? Mathf.Clamp01(resolveT / waterFadeTime) : 1f;
            if (water != null)
            {
                Color c = water.color; c.a = waterStartAlpha * (1f - k); water.color = c;
                if (k >= 1f && water.gameObject.activeSelf) water.gameObject.SetActive(false);
            }
            if (resolveT >= waterFadeTime + completeDelay)
            {
                resolving = false;
                onCompleted.Invoke();
            }
            return;
        }

        if (completed) return;

        if (!started)
        {
            if (prerequisite != null && !prerequisite.IsCompleted) return;
            BeginSoak();
            return;
        }

        if (!running) return;
        if (delayTimer > 0f) { delayTimer -= Time.deltaTime; return; }

        progress = Mathf.Clamp01(progress + Time.deltaTime / soakDuration);
        UpdateVisuals();

        if (progress >= 1f) Resolve(false);   // красная зона прошла, не успели
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (!running || delayTimer > 0f) return;

        if (progress >= redZoneStart) Resolve(true);
        else if (earlyTapPenalty > 0 && surgery != null) surgery.ApplyPenalty(earlyTapPenalty);
    }

    private void BeginSoak()
    {
        started = true;
        running = true;
        progress = 0f;
        delayTimer = startDelay;
        ApplyInitialState();
    }

    private void Resolve(bool success)
    {
        running = false;
        completed = true;

        if (!success && surgery != null) surgery.ApplyPenalty(failPenalty);

        AudioClip clip = success ? successSound : failSound;
        if (sfx != null && clip != null) sfx.PlayOneShot(clip);

        if (tapPrompt != null) tapPrompt.SetActive(false);
        if (oldOrgan != null) oldOrgan.SetActive(false);   // старый орган заменяется новым
        if (newOrgan != null) newOrgan.SetActive(true);

        waterStartAlpha = water != null ? water.color.a : 1f;
        resolveT = 0f;
        resolving = true;                                   // вода плавно исчезает
    }

    private void UpdateVisuals()
    {
        if (bar != null) bar.value = progress;
        if (water != null) water.color = Color.Lerp(cleanColor, dirtyColor, progress);   // белая -> жёлтая
        if (tapPrompt != null) tapPrompt.SetActive(running && progress >= redZoneStart);
    }

    private void ApplyInitialState()
    {
        if (redZone != null)
        {
            redZone.anchorMin = new Vector2(redZoneStart, 0f);
            redZone.anchorMax = new Vector2(1f, 1f);
            redZone.offsetMin = redZone.offsetMax = Vector2.zero;
        }
        if (water != null) { water.gameObject.SetActive(true); water.color = cleanColor; }
        if (oldOrgan != null) oldOrgan.SetActive(true);
        if (newOrgan != null) newOrgan.SetActive(false);
        if (tapPrompt != null) tapPrompt.SetActive(false);
        if (bar != null) bar.value = 0f;
    }

    // Полный сброс (новая операция)
    public void ResetStage()
    {
        started = running = completed = resolving = false;
        progress = 0f;
        ApplyInitialState();
    }
}

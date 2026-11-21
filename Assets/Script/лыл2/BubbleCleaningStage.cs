using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using Unity.Android.Gradle;

// Очистка органа: на лёгком в случайных местах появляются пузыри, их надо успевать лопать.
// Лопнул - звук "пум". Промазал по органу - штраф к качеству. Чем дальше, тем чаще появляются пузыри.
// Повесить на прозрачный Image (Raycast Target = ON), который лежит поверх лёгкого.
public class BubbleCleaningStage : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private SurgeryGameManager surgery;

    [Header("Пузыри")]
    [SerializeField] private Bubble bubblePrefab;              // префаб с компонентом Bubble
    [SerializeField] private int bubblesToPop = 15;            // сколько лопнуть для победы
    [SerializeField] private float bubbleLifetime = 2.5f;      // сколько живёт пузырь
    [SerializeField] private int maxAlive = 5;                 // максимум пузырей одновременно
    [SerializeField] private Vector2 sizeRange = new Vector2(0.8f, 1.3f);
    [SerializeField] private float edgeMargin = 60f;           // отступ от края области
    [SerializeField] private float minDistanceBetween = 130f;  // пузыри не налезают друг на друга
    [SerializeField] private int expiredPenalty = 0;           // штраф за пропущенный пузырь (0 = нет)

    [Header("Скорость (с каждым разом быстрее)")]
    [SerializeField] private float startDelay = 1f;
    [SerializeField] private float startInterval = 1.4f;       // пауза между пузырями в начале
    [SerializeField] private float minInterval = 0.4f;         // быстрее этого не станет
    [Range(0.7f, 1f)][SerializeField] private float speedUpPerSpawn = 0.93f;

    [Header("Промах")]
    [SerializeField] private int missPenalty = 3;              // -3% качества
    [SerializeField] private RectTransform missPopup;          // текст "-3%" (необязательно, выключен)

    [Header("Звук")]
    [SerializeField] private AudioSource sfx;
    [SerializeField] private AudioClip popSound;
    [SerializeField] private AudioClip missSound;
    [SerializeField] private Vector2 popPitchRange = new Vector2(0.92f, 1.08f);

    [Header("Визуал очистки (необязательно)")]
    [Tooltip("Грязь на органе: её прозрачность уменьшается по мере очистки")]
    [SerializeField] private Graphic[] dirtOverlays;
    [SerializeField] private TMP_Text progressText;

    [Header("Старт и конец")]
    [Tooltip("Начать автоматически, когда объект впервые включается (например, когда открылся стол B)")]
    [SerializeField] private bool autoStart = true;
    public UnityEvent onCompleted;

    private readonly List<Bubble> alive = new List<Bubble>();
    private RectTransform area;
    private bool started, running, completed;
    private int popped;
    private float interval, spawnTimer;

    public bool IsCompleted => completed;

    private void Awake() { area = (RectTransform)transform; }

    private void OnEnable()
    {
        if (autoStart && !started && !completed) StartStage();
    }

    public void StartStage()
    {
        started = true;
        running = true;
        completed = false;
        popped = 0;
        interval = startInterval;
        spawnTimer = startDelay;
        UpdateVisuals();
    }

    // Полный сброс (новая операция)
    public void ResetStage()
    {
        ClearBubbles(false);
        started = false; running = false; completed = false;
        popped = 0;
        interval = startInterval;
        UpdateVisuals();
    }

    private void Update()
    {
        if (!running) return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f) return;

        bool needMore = popped + alive.Count < bubblesToPop;
        if (needMore && alive.Count < maxAlive)
        {
            Spawn();
            spawnTimer = interval;
            interval = Mathf.Max(minInterval, interval * speedUpPerSpawn);   // следующий появится быстрее
        }
        else
        {
            spawnTimer = 0.1f;   // подождём места / пока пузыри лопнут
        }
    }

    private void Spawn()
    {
        Rect r = area.rect;
        Vector2 pos = Vector2.zero;
        bool found = false;

        for (int attempt = 0; attempt < 12 && !found; attempt++)
        {
            pos = new Vector2(
                Random.Range(r.xMin + edgeMargin, r.xMax - edgeMargin),
                Random.Range(r.yMin + edgeMargin, r.yMax - edgeMargin));

            found = true;
            foreach (var b in alive)
                if (Vector2.Distance(b.transform.localPosition, pos) < minDistanceBetween) { found = false; break; }
        }

        Bubble bubble = Instantiate(bubblePrefab, area);
        RectTransform brt = (RectTransform)bubble.transform;
        brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.5f);
        brt.localPosition = pos;
        bubble.Init(this, bubbleLifetime, 0.12f, Random.Range(sizeRange.x, sizeRange.y));
        alive.Add(bubble);
    }

    // ---------- события пузырей ----------
    public void BubblePopped(Bubble b)
    {
        alive.Remove(b);
        popped++;

        if (sfx != null && popSound != null)
        {
            sfx.pitch = Random.Range(popPitchRange.x, popPitchRange.y);
            sfx.PlayOneShot(popSound);
        }

        UpdateVisuals();
        if (popped >= bubblesToPop) Complete();
    }

    public void BubbleExpired(Bubble b)
    {
        alive.Remove(b);
        if (expiredPenalty > 0 && surgery != null) surgery.ApplyPenalty(expiredPenalty);
    }

    // ---------- промах: нажатие по органу мимо пузыря ----------
    public void OnPointerDown(PointerEventData e)
    {
        if (!running) return;

        if (surgery != null) surgery.ApplyPenalty(missPenalty);
        if (sfx != null && missSound != null) { sfx.pitch = 1f; sfx.PlayOneShot(missSound); }

        if (missPopup != null &&
            RectTransformUtility.ScreenPointToWorldPointInRectangle(area, e.position, e.pressEventCamera, out Vector3 world))
        {
            StopCoroutine(nameof(MissPopupRoutine));
            StartCoroutine(MissPopupRoutine(world));
        }
    }

    private IEnumerator MissPopupRoutine(Vector3 world)
    {
        missPopup.gameObject.SetActive(true);
        CanvasGroup cg = missPopup.GetComponent<CanvasGroup>();
        if (cg == null) cg = missPopup.gameObject.AddComponent<CanvasGroup>();

        const float time = 0.6f;
        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            float k = t / time;
            missPopup.position = world + Vector3.up * (k * 40f);
            cg.alpha = 1f - k;
            yield return null;
        }
        missPopup.gameObject.SetActive(false);
    }

    // ---------- конец ----------
    private void Complete()
    {
        running = false;
        completed = true;
        ClearBubbles(true);
        UpdateVisuals();
        onCompleted.Invoke();
    }

    private void ClearBubbles(bool pop)
    {
        foreach (var b in alive) if (b != null) b.Kill(pop);
        alive.Clear();
    }

    private void UpdateVisuals()
    {
        float progress = bubblesToPop > 0 ? Mathf.Clamp01((float)popped / bubblesToPop) : 1f;

        foreach (var g in dirtOverlays)
        {
            if (g == null) continue;
            Color c = g.color; c.a = 1f - progress; g.color = c;
        }
        if (progressText != null) progressText.text = "CLEANED: " + popped + "/" + bubblesToPop;
    }
}

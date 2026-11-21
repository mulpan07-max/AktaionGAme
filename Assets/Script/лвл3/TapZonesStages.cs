using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

// Этап ПЕРЕД разрезом: на теле показываются круги, на них надо тапать несколько раз.
// Когда все зоны пройдены - тело "открывается" (смена спрайта, необязательно), и начинается разрез.
public class TapZonesStage : MonoBehaviour
{
    [Serializable]
    public class Zone
    {
        public string label;                 // просто название для списка
        public RectTransform circle;         // круг (Image с Raycast Target = ON)
        [Min(1)] public int tapsRequired = 3;
    }

    [Header("Зоны (нажмите + чтобы добавить)")]
    [SerializeField] private Zone[] zones;
    [Tooltip("Включено: круги показываются по одному, по порядку. Выключено: все сразу, в любом порядке")]
    [SerializeField] private bool sequential = true;

    [Header("Анимация кругов")]
    [SerializeField] private float idlePulse = 0.06f;      // лёгкое "дыхание" круга
    [SerializeField] private float punchScale = 1.3f;      // увеличение при тапе
    [SerializeField] private float vanishTime = 0.2f;      // как быстро исчезает пройденный круг

    [Header("Звук (необязательно)")]
    [SerializeField] private AudioSource sfx;
    [SerializeField] private AudioClip tapSound;
    [SerializeField] private AudioClip zoneDoneSound;
    [SerializeField] private Vector2 pitchRange = new Vector2(0.95f, 1.08f);

    [Header("Тело после зон (необязательно)")]
    [Tooltip("Image тела: после всех зон на нём меняется спрайт")]
    [SerializeField] private Image bodyImage;
    [SerializeField] private Sprite bodyOpenedSprite;

    [Header("Текст (необязательно)")]
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private string hintText = "TAP THE CIRCLES TO OPEN THE BODY";
    [SerializeField] private TMP_Text progressText;

    [SerializeField] private float completeDelay = 0.5f;
    public UnityEvent onCompleted;

    private Action callback;
    private bool running, completing;
    private float completeTimer;
    private int current;
    private int[] tapsLeft;
    private bool[] done;
    private float[] punch, vanish;
    private Vector3[] baseScale;
    private Sprite originalBodySprite;
    private bool bodySpriteCaptured;

    // Запускается из SurgeryGameManager.
    public void Begin(Action cb)
    {
        callback = cb;
        gameObject.SetActive(true);
        ResetState(false);          // спрайт тела здесь не трогаем
        running = true;

        for (int i = 0; i < zones.Length; i++)
        {
            RectTransform c = zones[i].circle;
            if (c == null) continue;

            Graphic g = c.GetComponent<Graphic>();
            if (g != null) g.raycastTarget = true;

            TapZone tz = c.GetComponent<TapZone>();
            if (tz == null) tz = c.gameObject.AddComponent<TapZone>();
            tz.index = i;
            tz.Tapped -= OnZoneTapped;
            tz.Tapped += OnZoneTapped;

            // последовательный режим: показываем только первый круг
            bool visible = !sequential || i == 0;
            c.gameObject.SetActive(visible);
            if (visible) punch[i] = punchScale;
        }

        if (instructionText != null) instructionText.text = hintText;
        UpdateProgress();
    }

    // Полный сброс (новая операция)
    public void ResetStage()
    {
        ResetState(true);
        gameObject.SetActive(false);
    }

    private void ResetState(bool restoreBody)
    {
        running = false;
        completing = false;
        current = 0;

        int n = zones != null ? zones.Length : 0;
        tapsLeft = new int[n];
        done = new bool[n];
        punch = new float[n];
        vanish = new float[n];
        if (baseScale == null || baseScale.Length != n)
        {
            baseScale = new Vector3[n];
            for (int i = 0; i < n; i++)
                baseScale[i] = zones[i].circle != null ? zones[i].circle.localScale : Vector3.one;
        }

        for (int i = 0; i < n; i++)
        {
            tapsLeft[i] = Mathf.Max(1, zones[i].tapsRequired);
            punch[i] = 1f;
            if (zones[i].circle != null)
            {
                zones[i].circle.localScale = baseScale[i];
                zones[i].circle.gameObject.SetActive(false);
            }
        }

        if (restoreBody)
        {
            if (!bodySpriteCaptured && bodyImage != null)
            {
                originalBodySprite = bodyImage.sprite;   // первый сброс происходит при старте сцены
                bodySpriteCaptured = true;
            }
            if (bodySpriteCaptured && bodyImage != null) bodyImage.sprite = originalBodySprite;
        }
    }

    private void OnZoneTapped(int i)
    {
        if (!running || done[i]) return;
        if (sequential && i != current) return;

        tapsLeft[i]--;
        punch[i] = punchScale;
        PlaySound(tapSound);

        if (tapsLeft[i] > 0) return;

        // зона пройдена
        done[i] = true;
        vanish[i] = vanishTime;
        PlaySound(zoneDoneSound);

        if (sequential)
        {
            current++;
            if (current < zones.Length && zones[current].circle != null)
            {
                zones[current].circle.gameObject.SetActive(true);
                punch[current] = punchScale;
            }
        }

        UpdateProgress();

        if (AllDone())
        {
            running = false;
            completing = true;
            completeTimer = completeDelay;
            if (bodyImage != null && bodyOpenedSprite != null) bodyImage.sprite = bodyOpenedSprite;   // тело открылось
        }
    }

    private void Update()
    {
        for (int i = 0; i < zones.Length; i++)
        {
            RectTransform c = zones[i].circle;
            if (c == null || !c.gameObject.activeSelf) continue;

            if (done[i])
            {
                vanish[i] -= Time.deltaTime;
                float k = vanishTime > 0f ? Mathf.Clamp01(vanish[i] / vanishTime) : 0f;
                c.localScale = baseScale[i] * k;
                if (k <= 0f) c.gameObject.SetActive(false);
            }
            else
            {
                punch[i] = Mathf.MoveTowards(punch[i], 1f, Time.deltaTime * 4f);
                float pulse = 1f + idlePulse * Mathf.Sin(Time.time * 4f + i);
                c.localScale = baseScale[i] * pulse * punch[i];
            }
        }

        if (completing)
        {
            completeTimer -= Time.deltaTime;
            if (completeTimer <= 0f) Finish();
        }
    }

    private void Finish()
    {
        completing = false;
        foreach (var z in zones) if (z.circle != null) z.circle.gameObject.SetActive(false);

        onCompleted.Invoke();
        callback?.Invoke();
        gameObject.SetActive(false);
    }

    private bool AllDone()
    {
        foreach (bool d in done) if (!d) return false;
        return true;
    }

    private void UpdateProgress()
    {
        if (progressText == null) return;
        int count = 0;
        foreach (bool d in done) if (d) count++;
        progressText.text = "ZONES: " + count + "/" + zones.Length;
    }

    private void PlaySound(AudioClip clip)
    {
        if (sfx == null || clip == null) return;
        sfx.pitch = UnityEngine.Random.Range(pitchRange.x, pitchRange.y);
        sfx.PlayOneShot(clip);
    }
}
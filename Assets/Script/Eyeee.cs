using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Эффект "глаз человека":
///  1) лёгкое покачивание картинки (камеры или UI-контейнера),
///  2) моргание примерно раз в 7 секунд (веки закрываются сверху и снизу).
/// Повесьте на пустой объект "EyeEffects" в игровой сцене.
/// </summary>
public class EyeEffects : MonoBehaviour
{
    [Header("Покачивание")]
    [Tooltip("Что качать: камера ИЛИ RectTransform-контейнер с игрой (для Canvas Overlay).")]
    [SerializeField] private Transform swayTarget;
    [SerializeField] private bool swayEnabled = true;
    [Tooltip("Сила сдвига для UI, в пикселях.")]
    [SerializeField] private float uiSwayPixels = 12f;
    [Tooltip("Сила сдвига для камеры, в единицах мира.")]
    [SerializeField] private float cameraSwayUnits = 0.05f;
    [Tooltip("Максимальный наклон в градусах.")]
    [SerializeField] private float swayRotation = 0.6f;
    [Tooltip("Скорость покачивания. Меньше = плавнее.")]
    [SerializeField] private float swaySpeed = 0.35f;
    [Tooltip("Лёгкое увеличение UI, чтобы при сдвиге не были видны края.")]
    [SerializeField] private float uiScaleBoost = 1.03f;

    [Header("Моргание")]
    [SerializeField] private bool blinkEnabled = true;
    [SerializeField] private RectTransform upperLid;
    [SerializeField] private RectTransform lowerLid;
    [SerializeField] private float blinkInterval = 7f;
    [Tooltip("Случайный разброс интервала (+/- секунд).")]
    [SerializeField] private float blinkIntervalRandom = 2f;
    [SerializeField] private float closeTime = 0.12f;
    [SerializeField] private float holdTime = 0.06f;
    [SerializeField] private float openTime = 0.18f;
    [Tooltip("Насколько закрываются веки. 1 = смыкаются ровно посередине, больше = с запасом.")]
    [SerializeField, Range(0.5f, 2f)] private float lidCoverage = 1.1f;
    [Tooltip("Автоматически переносить веки в корень Canvas и рисовать поверх всего.")]
    [SerializeField] private bool attachLidsToCanvas = true;

    private Vector3 basePosition;
    private Quaternion baseRotation;
    private Vector3 baseScale;
    private bool isUI;
    private float seed;

    private void Start()
    {
        seed = Random.Range(0f, 100f);

        if (swayTarget != null)
        {
            basePosition = swayTarget.localPosition;
            baseRotation = swayTarget.localRotation;
            baseScale = swayTarget.localScale;
            isUI = swayTarget is RectTransform;
        }

        if (attachLidsToCanvas)
            AttachLidsToCanvas();

        SetupLid(upperLid, true);
        SetupLid(lowerLid, false);
        SetLidOpenAmount(0f);

        if (blinkEnabled)
            StartCoroutine(BlinkRoutine());
    }

    // ===== Покачивание =====

    private void LateUpdate()
    {
        if (!swayEnabled || swayTarget == null) return;

        float t = Time.time * swaySpeed;

        // Плавные случайные значения от -1 до 1
        float nx = Noise(t, 0f);
        float ny = Noise(t, 10f);
        float nr = Noise(t, 20f);

        // "Иногда" качается сильнее, иногда почти спокойно
        float envelope = 0.4f + 0.6f * Mathf.PerlinNoise(seed + t * 0.3f, 50f);

        float amount = isUI ? uiSwayPixels : cameraSwayUnits;
        Vector3 offset = new Vector3(nx, ny, 0f) * amount * envelope;

        swayTarget.localPosition = basePosition + offset;
        swayTarget.localRotation = baseRotation * Quaternion.Euler(0f, 0f, nr * swayRotation * envelope);

        if (isUI)
            swayTarget.localScale = baseScale * uiScaleBoost;
    }

    private float Noise(float t, float offset)
    {
        return Mathf.PerlinNoise(seed + t, offset) * 2f - 1f;
    }

    // ===== Моргание =====

    private IEnumerator BlinkRoutine()
    {
        while (true)
        {
            float wait = blinkInterval + Random.Range(-blinkIntervalRandom, blinkIntervalRandom);
            yield return new WaitForSeconds(Mathf.Max(1f, wait));

            // Закрываем
            yield return AnimateLids(0f, 1f, closeTime);
            yield return new WaitForSeconds(holdTime);
            // Открываем
            yield return AnimateLids(1f, 0f, openTime);

            // Иногда моргаем дважды подряд
            if (Random.value < 0.15f)
            {
                yield return new WaitForSeconds(0.1f);
                yield return AnimateLids(0f, 1f, closeTime);
                yield return AnimateLids(1f, 0f, openTime);
            }
        }
    }

    private IEnumerator AnimateLids(float from, float to, float time)
    {
        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / Mathf.Max(time, 0.01f));
            SetLidOpenAmount(Mathf.Lerp(from, to, k));
            yield return null;
        }
        SetLidOpenAmount(to);
    }

    // amount: 0 = глаза открыты, 1 = закрыты
    private void SetLidOpenAmount(float amount)
    {
        float half = GetHalfHeight() * amount;

        if (upperLid != null)
            upperLid.sizeDelta = new Vector2(upperLid.sizeDelta.x, half);
        if (lowerLid != null)
            lowerLid.sizeDelta = new Vector2(lowerLid.sizeDelta.x, half);
    }

    // Переносим веки в корень Canvas, чтобы они были на весь экран
    // (раньше высота бралась от родителя, а он мог быть маленьким)
    private void AttachLidsToCanvas()
    {
        Canvas canvas = null;
        if (upperLid != null) canvas = upperLid.GetComponentInParent<Canvas>();
        if (canvas == null && lowerLid != null) canvas = lowerLid.GetComponentInParent<Canvas>();
        if (canvas == null) return;

        RectTransform root = canvas.rootCanvas.transform as RectTransform;

        if (upperLid != null)
        {
            upperLid.SetParent(root, false);
            upperLid.SetAsLastSibling();
        }
        if (lowerLid != null)
        {
            lowerLid.SetParent(root, false);
            lowerLid.SetAsLastSibling();
        }
    }

    private float GetHalfHeight()
    {
        RectTransform parent = (upperLid != null ? upperLid.parent : lowerLid != null ? lowerLid.parent : null) as RectTransform;
        if (parent == null) return 0f;
        // Половина высоты экрана * запас, чтобы веки точно сомкнулись
        return parent.rect.height * 0.5f * lidCoverage + 4f;
    }

    // Веки: растянуты по ширине, прижаты к верху/низу, высота меняется кодом
    private void SetupLid(RectTransform lid, bool top)
    {
        if (lid == null) return;

        lid.anchorMin = new Vector2(0f, top ? 1f : 0f);
        lid.anchorMax = new Vector2(1f, top ? 1f : 0f);
        lid.pivot = new Vector2(0.5f, top ? 1f : 0f);
        lid.anchoredPosition = Vector2.zero;
        lid.sizeDelta = new Vector2(0f, 0f);

        // Веки не должны перехватывать нажатия
        Graphic g = lid.GetComponent<Graphic>();
        if (g != null) g.raycastTarget = false;
    }

    // Моргнуть по требованию (например, при ошибке игрока)
    public void BlinkNow()
    {
        StartCoroutine(BlinkOnce());
    }

    private IEnumerator BlinkOnce()
    {
        yield return AnimateLids(0f, 1f, closeTime);
        yield return new WaitForSeconds(holdTime);
        yield return AnimateLids(1f, 0f, openTime);
    }
}
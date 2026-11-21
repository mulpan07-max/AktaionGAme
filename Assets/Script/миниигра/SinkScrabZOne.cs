using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

// Повесить на прозрачный Image (Raycast Target = ON) в панели крана, поверх органа.
// Водим пальцем -> пятна исчезают и играет звук воды. Остановили палец -> звук затихает.
// Когда чисто -> onCleaned (вернуться к столу).
public class SinkScrubZone : MonoBehaviour, IDragHandler
{
    [Tooltip("Сколько высот экрана надо провести пальцем, чтобы смыть всё с нуля. Больше = дольше мыть")]
    [SerializeField] private float screenHeightsToClean = 3f;
    [SerializeField] private float delayAfterClean = 0.6f;

    [Header("Звук воды")]
    [Tooltip("AudioSource с зацикленным звуком воды (Loop = ON, Play On Awake = OFF)")]
    [SerializeField] private AudioSource waterSource;
    [SerializeField] private float maxVolume = 1f;
    [Tooltip("Скорость нарастания/затухания звука")]
    [SerializeField] private float fadeSpeed = 8f;
    [Tooltip("Через сколько секунд после остановки пальца звук начинает затихать")]
    [SerializeField] private float stopDelay = 0.15f;

    public UnityEvent onCleaned;

    private RectTransform rt;
    private bool done;
    private float lastMoveTime = -10f;

    private void OnEnable()
    {
        // если уже чисто (вернулись в раковину позже) - ничего не делаем
        done = OrganDirt.Instance != null && OrganDirt.Instance.IsClean;
        lastMoveTime = -10f;
    }

    private void OnDisable()
    {
        if (waterSource != null) { waterSource.Stop(); waterSource.volume = 0f; }
    }

    public void OnDrag(PointerEventData e)
    {
        if (done || OrganDirt.Instance == null) return;
        if (rt == null) rt = (RectTransform)transform;
        if (!RectTransformUtility.RectangleContainsScreenPoint(rt, e.position, e.pressEventCamera)) return;

        if (e.delta.sqrMagnitude > 0.5f) lastMoveTime = Time.unscaledTime;   // палец реально движется

        OrganDirt.Instance.Scrub(e.delta.magnitude / (Screen.height * screenHeightsToClean));

        if (OrganDirt.Instance.IsClean)
        {
            done = true;
            StartCoroutine(FinishRoutine());
        }
    }

    private void Update()
    {
        if (waterSource == null) return;

        bool scrubbing = !done && Time.unscaledTime - lastMoveTime < stopDelay;
        float target = scrubbing ? maxVolume : 0f;

        if (scrubbing && !waterSource.isPlaying) waterSource.Play();
        waterSource.volume = Mathf.MoveTowards(waterSource.volume, target, fadeSpeed * Time.unscaledDeltaTime);
        if (!scrubbing && waterSource.volume <= 0.001f && waterSource.isPlaying) waterSource.Stop();
    }

    private IEnumerator FinishRoutine()
    {
        yield return new WaitForSeconds(delayAfterClean);
        onCleaned.Invoke();
    }
}

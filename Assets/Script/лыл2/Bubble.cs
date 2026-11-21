using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Повесить на ПРЕФАБ пузыря (UI Image с кружком, Raycast Target = ON).
// Появляется, живёт lifetime секунд, лопается от нажатия (PointerDown - реагирует мгновенно).
public class Bubble : MonoBehaviour, IPointerDownHandler
{
    private BubbleCleaningStage owner;
    private RectTransform rt;
    private Image img;
    private float lifetime, age, growTime;
    private Vector3 baseScale;
    private float baseAlpha = 1f;
    private bool done;

    public void Init(BubbleCleaningStage o, float life, float grow, float scale)
    {
        owner = o;
        lifetime = life;
        growTime = Mathf.Max(0.01f, grow);
        rt = (RectTransform)transform;
        img = GetComponent<Image>();
        if (img != null) baseAlpha = img.color.a;
        baseScale = Vector3.one * scale;
        rt.localScale = Vector3.zero;
    }

    private void OnEnable()
    {
        // если панель скрыли во время исчезновения - не оставляем "мёртвый" пузырь
        if (done) Destroy(gameObject);
    }

    private void Update()
    {
        if (done || owner == null) return;
        age += Time.deltaTime;

        float k = Mathf.Clamp01(age / growTime);
        rt.localScale = baseScale * Mathf.SmoothStep(0f, 1f, k);

        // в конце жизни пузырь бледнеет - игрок видит, что он скоро исчезнет
        if (img != null)
        {
            float fade = Mathf.InverseLerp(lifetime * 0.6f, lifetime, age);
            Color c = img.color; c.a = baseAlpha * Mathf.Lerp(1f, 0.45f, fade); img.color = c;
        }

        if (age >= lifetime)
        {
            done = true;
            owner.BubbleExpired(this);
            StartCoroutine(Vanish(0.2f, false));
        }
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (done || owner == null) return;
        done = true;
        owner.BubblePopped(this);
        StartCoroutine(Vanish(0.12f, true));
    }

    // Убрать пузырь без оповещения (конец этапа / сброс)
    public void Kill(bool pop)
    {
        if (done) { return; }
        done = true;
        if (gameObject.activeInHierarchy) StartCoroutine(Vanish(pop ? 0.12f : 0.15f, pop));
        else Destroy(gameObject);
    }

    private IEnumerator Vanish(float time, bool pop)
    {
        if (img != null) img.raycastTarget = false;
        Vector3 start = rt.localScale;
        float startAlpha = img != null ? img.color.a : 1f;

        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            float k = t / time;
            rt.localScale = start * (pop ? Mathf.Lerp(1f, 1.4f, k) : Mathf.Lerp(1f, 0.6f, k));
            if (img != null) { Color c = img.color; c.a = startAlpha * (1f - k); img.color = c; }
            yield return null;
        }
        Destroy(gameObject);
    }
}

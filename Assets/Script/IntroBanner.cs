using System.Collections;
using UnityEngine;

// Плашка при запуске: fade in -> пауза -> fade out
public class IntroBanner : MonoBehaviour
{
    public GameObject banner;                 // сама плашка
    public float fadeInTime = 0.5f;           // длительность появления
    public float showTime = 3f;               // сколько держится полностью видимой
    public float fadeOutTime = 0.5f;          // длительность затухания
    public bool allowTapToSkip = false;       // тап = сразу начать затухание

    private CanvasGroup group;

    private void Start()
    {
        if (banner == null) return;

        group = banner.GetComponent<CanvasGroup>();
        if (group == null) group = banner.AddComponent<CanvasGroup>();

        group.alpha = 0f;
        banner.SetActive(true);
        banner.transform.SetAsLastSibling();  // поверх всех панелей

        StartCoroutine(Play());
    }

    private IEnumerator Play()
    {
        // Fade in
        yield return Fade(0f, 1f, fadeInTime);

        // Пауза (с возможностью пропуска тапом)
        float t = 0f;
        while (t < showTime)
        {
            if (allowTapToSkip && TapPressed()) break;
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        // Fade out
        yield return Fade(1f, 0f, fadeOutTime);

        banner.SetActive(false);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (duration <= 0f) { group.alpha = to; yield break; }

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        group.alpha = to;
    }

    private static bool TapPressed()
    {
        return Input.GetMouseButtonDown(0) ||
               (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
    }
}

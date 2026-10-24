using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Главный экран: Splash (тап) -> Menu (Play и т.д.) с эффектами перехода.
/// Повесьте на пустой объект "MainMenuController".
/// </summary>
public class MainMenuController : MonoBehaviour
{
    public enum TransitionType { Fade, Zoom, SlideUp }

    [Header("Экраны (нужен CanvasGroup на каждом)")]
    [SerializeField] private CanvasGroup splashScreen;
    [SerializeField] private CanvasGroup menuScreen;

    [Header("Мигающая надпись на Splash (необязательно)")]
    [SerializeField] private CanvasGroup tapText;
    [SerializeField] private float pulseSpeed = 2f;

    [Header("Кнопки меню для поочерёдного появления (необязательно)")]
    [SerializeField] private RectTransform[] menuButtons;
    [SerializeField] private float buttonDelay = 0.12f;

    [Header("Эффект перехода")]
    [SerializeField] private TransitionType transition = TransitionType.Zoom;
    [SerializeField] private float duration = 0.6f;
    [SerializeField] private float zoomAmount = 0.8f;      // стартовый масштаб для Zoom
    [SerializeField] private float slideDistance = 600f;   // пиксели для SlideUp

    [Header("Сцена игры")]
    [SerializeField] private string gameSceneName = "Game";

    private bool splashActive = true;
    private bool isTransitioning;

    private void Awake()
    {
        SetScreen(splashScreen, true);
        SetScreen(menuScreen, false);
    }

    private void Update()
    {
        if (!splashActive) return;

        // Мигание надписи "Tap to start"
        if (tapText != null)
            tapText.alpha = 0.4f + 0.6f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * pulseSpeed));

        if (isTransitioning) return;

        bool tapped = Input.GetMouseButtonDown(0)
                      || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
                      || Input.anyKeyDown;

        if (tapped)
            StartCoroutine(SwitchScreens());
    }

    private IEnumerator SwitchScreens()
    {
        isTransitioning = true;
        splashActive = false;

        // Splash уходит
        yield return Animate(splashScreen, false);
        SetScreen(splashScreen, false);

        // Меню приходит
        PrepareButtons();
        menuScreen.gameObject.SetActive(true);
        menuScreen.alpha = 0f;
        menuScreen.interactable = false;
        menuScreen.blocksRaycasts = false;

        StartCoroutine(PopButtons());
        yield return Animate(menuScreen, true);

        menuScreen.alpha = 1f;
        menuScreen.interactable = true;
        menuScreen.blocksRaycasts = true;
        isTransitioning = false;
    }

    // Анимация появления (show = true) или исчезновения (show = false)
    private IEnumerator Animate(CanvasGroup group, bool show)
    {
        group.interactable = false;
        group.blocksRaycasts = false;

        RectTransform rt = group.transform as RectTransform;
        Vector2 basePos = rt.anchoredPosition;
        Vector3 baseScale = Vector3.one;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;
            float k = Mathf.Clamp01(t);
            float e = show ? EaseOutCubic(k) : EaseInCubic(k);
            float p = show ? e : 1f - e;   // 0 -> 1 при появлении, 1 -> 0 при исчезновении

            group.alpha = p;

            switch (transition)
            {
                case TransitionType.Zoom:
                    float s = Mathf.Lerp(zoomAmount, 1f, p);
                    // при исчезновении экран слегка увеличивается, при появлении "вырастает"
                    if (!show) s = Mathf.Lerp(1.15f, 1f, p);
                    rt.localScale = baseScale * s;
                    break;

                case TransitionType.SlideUp:
                    float offset = (1f - p) * slideDistance;
                    rt.anchoredPosition = basePos + new Vector2(0f, show ? -offset : offset);
                    break;
            }
            yield return null;
        }

        // Возвращаем значения в норму
        rt.localScale = baseScale;
        rt.anchoredPosition = basePos;
        group.alpha = show ? 1f : 0f;
    }

    // Кнопки меню прячем перед показом и выпрыгивают по очереди
    private void PrepareButtons()
    {
        if (menuButtons == null) return;
        foreach (var b in menuButtons)
            if (b != null) b.localScale = Vector3.zero;
    }

    private IEnumerator PopButtons()
    {
        if (menuButtons == null) yield break;

        yield return new WaitForSecondsRealtime(duration * 0.4f);

        foreach (var b in menuButtons)
        {
            if (b == null) continue;
            StartCoroutine(PopOne(b));
            yield return new WaitForSecondsRealtime(buttonDelay);
        }
    }

    private IEnumerator PopOne(RectTransform target)
    {
        float t = 0f;
        const float popTime = 0.35f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / popTime;
            target.localScale = Vector3.one * EaseOutBack(Mathf.Clamp01(t));
            yield return null;
        }
        target.localScale = Vector3.one;
    }

    private void SetScreen(CanvasGroup group, bool visible)
    {
        group.gameObject.SetActive(visible);
        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }

    // ===== Easing =====
    private static float EaseOutCubic(float x) => 1f - Mathf.Pow(1f - x, 3f);
    private static float EaseInCubic(float x) => x * x * x;
    private static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }

    // ===== Кнопки (привяжите в OnClick) =====
    public void OnPlayClicked() => SceneManager.LoadScene(gameSceneName);

    public void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
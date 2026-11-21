using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;

// Этап укола:
// шприц -> MP4-анимация шприца -> MP4 тела -> новый спрайт тела + сюжетный предмет -> дальше операция.
public class InjectionStage : MonoBehaviour
{
    [Header("Шприц")]
    [SerializeField] private SyringeDrag syringe;
    [Tooltip("Куда поставить шприц после отпускания. Можно оставить пустым.")]
    [SerializeField] private RectTransform injectionPoint;

    [Header("MP4-анимация шприца")]
    [Tooltip("Video Player для MP4 шприца. Объект с ним должен оставаться включённым.")]
    [SerializeField] private VideoPlayer syringeVideoPlayer;
    [Tooltip("Raw Image, который показывает Render Texture видео шприца.")]
    [SerializeField] private RawImage syringeVideoImage;
    [Tooltip("Обычный Image со статичным шприцем. Он выключается, когда появляется первый кадр видео.")]
    [SerializeField] private Image syringeStaticImage;
    [SerializeField] private float syringeVideoTimeout = 15f;

    [Header("Видео тела")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RawImage videoImage;
    [SerializeField] private float videoTimeout = 30f;

    [Header("Тело: замена изображения после видео")]
    [Tooltip("ТОТ ЖЕ Image, что указан как Body Image в TissueOpeningManager.")]
    [SerializeField] private Image bodyImage;
    [Tooltip("Заполни SpriteRenderer, если тело находится в обычной 2D-сцене. Иначе оставь пустым.")]
    [SerializeField] private SpriteRenderer bodySpriteRenderer;
    [Tooltip("Спрайт тела ПОСЛЕ укола и видео.")]
    [SerializeField] private Sprite body2Sprite;
    [Tooltip("TissueOpeningManager: нужен, чтобы ткани не перезаписывали спрайт тела после укола.")]
    [SerializeField] private TissueOpeningManager tissueManager;

    private Sprite originalBodyImageSprite;
    private Sprite originalBodyRendererSprite;
    private bool originalBodySpritesCaptured;

    [Header("Сюжетный предмет на столе")]
    [SerializeField] private GameObject storyItem;
    [SerializeField] private float popTime = 0.35f;
    [SerializeField] private float delayAfterStoryItem = 1f;

    [Header("Звук и подсказка (необязательно)")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip injectSound;
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private string dragText = "DRAG THE SYRINGE TO THE BODY";

    private Action onDone;
    private bool running;
    private Vector3 storyItemScale = Vector3.one;
    private bool scaleCaptured;

    // Запускается из TissueOpeningManager (или SurgeryGameManager).
    public void Begin(Action callback)
    {
        onDone = callback;
        gameObject.SetActive(true);
        ResetState(false);          // спрайт тела НЕ трогаем: сейчас на экране раскрытое тело
        running = true;

        if (syringe != null)
        {
            syringe.Released -= OnSyringeReleased;
            syringe.Released += OnSyringeReleased;
        }

        if (instructionText != null)
            instructionText.text = dragText;
    }

    // Полный сброс (новая операция): возвращает исходное тело.
    public void ResetStage()
    {
        ResetState(true);
        gameObject.SetActive(false);
    }

    private void ResetState(bool restoreBody)
    {
        StopAllCoroutines();
        running = false;

        if (syringe != null)
        {
            syringe.gameObject.SetActive(true);
            syringe.ResetState();
            syringe.Released -= OnSyringeReleased;
        }

        if (syringeVideoPlayer != null)
            syringeVideoPlayer.Stop();
        if (syringeVideoImage != null)
            syringeVideoImage.enabled = false;
        if (syringeStaticImage != null)
            syringeStaticImage.enabled = true;

        if (videoPlayer != null)
            videoPlayer.Stop();
        if (videoImage != null)
            videoImage.enabled = false;

        if (!scaleCaptured && storyItem != null)
        {
            storyItemScale = storyItem.transform.localScale;
            scaleCaptured = true;
        }

        if (storyItem != null)
        {
            storyItem.transform.localScale = storyItemScale;
            storyItem.SetActive(false);
        }

        if (restoreBody)
        {
            CaptureOriginalBodySprites();                       // первый сброс происходит при старте сцены
            if (tissueManager != null) tissueManager.FreezeBody(false);
            RestoreOriginalBodySprite();
        }
    }

    private void OnSyringeReleased(bool inZone)
    {
        if (!running || !inZone) return;

        running = false;
        if (syringe != null)
        {
            syringe.canDrag = false;
            syringe.SnapTo(injectionPoint);
        }

        StartCoroutine(Sequence());
    }

    private IEnumerator Sequence()
    {
        // 1. MP4-анимация шприца.
        if (sfxSource != null && injectSound != null)
            sfxSource.PlayOneShot(injectSound);

        if (syringeVideoPlayer != null)
        {
            yield return PlayVideo(
                syringeVideoPlayer,
                syringeVideoImage,
                syringeVideoTimeout,
                () =>
                {
                    if (syringeStaticImage != null)
                        syringeStaticImage.enabled = false;
                }
            );
        }

        if (syringe != null)
            syringe.gameObject.SetActive(false);

        // 2. MP4 тела.
        yield return PlayVideo(videoPlayer, videoImage, videoTimeout);

        // 3. Меняем спрайт тела на новый и замораживаем его.
        ApplyBody2Sprite();

        if (storyItem != null)
        {
            storyItem.SetActive(true);
            if (popTime > 0f)
            {
                for (float t = 0f; t < popTime; t += Time.deltaTime)
                {
                    float k = Mathf.SmoothStep(0f, 1f, t / popTime);
                    storyItem.transform.localScale = storyItemScale * k;
                    yield return null;
                }
            }
            storyItem.transform.localScale = storyItemScale;
        }

        if (delayAfterStoryItem > 0f)
            yield return new WaitForSeconds(delayAfterStoryItem);

        // 4. Дальше обычная операция.
        onDone?.Invoke();
        gameObject.SetActive(false);
    }

    // Запоминаем исходный спрайт, чтобы при новой операции вернуть первое тело.
    private void CaptureOriginalBodySprites()
    {
        if (originalBodySpritesCaptured) return;

        if (bodyImage != null)
            originalBodyImageSprite = bodyImage.sprite;

        if (bodySpriteRenderer != null)
            originalBodyRendererSprite = bodySpriteRenderer.sprite;

        originalBodySpritesCaptured = true;
    }

    private void RestoreOriginalBodySprite()
    {
        if (!originalBodySpritesCaptured) return;

        if (bodyImage != null)
            bodyImage.sprite = originalBodyImageSprite;

        if (bodySpriteRenderer != null)
            bodySpriteRenderer.sprite = originalBodyRendererSprite;
    }

    private void ApplyBody2Sprite()
    {
        if (body2Sprite == null)
        {
            Debug.LogWarning("InjectionStage: Body 2 Sprite is not assigned.", this);
            return;
        }

        bool applied = false;

        // Через TissueOpeningManager: заменяет "Both Opened Sprite", ставит спрайт на тело и замораживает его.
        if (tissueManager != null)
        {
            tissueManager.ReplaceBodySprite(body2Sprite);
            applied = true;
        }

        if (bodyImage != null)
        {
            bodyImage.sprite = body2Sprite;
            applied = true;
        }

        if (bodySpriteRenderer != null)
        {
            bodySpriteRenderer.sprite = body2Sprite;
            applied = true;
        }

        if (!applied)
            Debug.LogWarning("InjectionStage: assign either Body Image (UI Image) or Body Sprite Renderer.", this);
    }

    // Воспроизведение видео через Video Player + Render Texture + Raw Image.
    private IEnumerator PlayVideo(
        VideoPlayer player,
        RawImage targetImage,
        float timeout,
        Action onFirstFrame = null)
    {
        if (player == null)
            yield break;

        float safeTimeout = Mathf.Max(1f, timeout);

        if (targetImage != null)
            targetImage.enabled = false;

        player.playOnAwake = false;
        player.isLooping = false;
        player.Stop();
        player.Prepare();

        float timer = 0f;
        while (!player.isPrepared && timer < safeTimeout)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!player.isPrepared)
        {
            Debug.LogWarning("InjectionStage: video preparation timed out: " + player.name);
            yield break;
        }

        bool finished = false;
        bool firstFrameReceived = false;

        VideoPlayer.EventHandler endHandler = source => finished = true;
        VideoPlayer.ErrorEventHandler errorHandler = (source, message) =>
        {
            Debug.LogWarning("InjectionStage video error (" + source.name + "): " + message);
            finished = true;
        };
        VideoPlayer.FrameReadyEventHandler frameHandler = (source, frameIndex) =>
        {
            if (firstFrameReceived) return;
            firstFrameReceived = true;
            onFirstFrame?.Invoke();
            if (targetImage != null)
                targetImage.enabled = true;
        };

        player.loopPointReached += endHandler;
        player.errorReceived += errorHandler;
        player.frameReady += frameHandler;
        player.sendFrameReadyEvents = true;

        player.Play();

        timer = 0f;
        while (!finished && timer < safeTimeout)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!finished)
            Debug.LogWarning("InjectionStage: video playback timed out: " + player.name);

        player.sendFrameReadyEvents = false;
        player.frameReady -= frameHandler;
        player.loopPointReached -= endHandler;
        player.errorReceived -= errorHandler;
        player.Stop();

        if (targetImage != null)
            targetImage.enabled = false;
    }
}
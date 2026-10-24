using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Звук игровой сцены:
///  1) плейлист фоновой музыки (несколько треков, добавляются через + в инспекторе),
///  2) звук тапа по пустому месту экрана (по кнопкам не срабатывает),
///  3) звук нажатия на кнопки (подключается ко всем кнопкам сцены автоматически).
/// Повесьте на пустой объект "GameAudio" в игровой сцене.
/// </summary>
public class GameAudio : MonoBehaviour
{
    [System.Serializable]
    public class AmbientSound
    {
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;   // громкость именно этого звука
    }

    private readonly List<AudioSource> ambientSources = new List<AudioSource>();
    private float ambienceFade = 1f;
    [Header("Фоновая музыка (плейлист)")]
    [Tooltip("Добавляйте треки кнопкой +. Играют по очереди и по кругу.")]
    [SerializeField] private AudioClip[] backgroundTracks;
    [SerializeField] private bool shuffle = false;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.5f;
    [SerializeField] private float fadeTime = 1.5f;

    [Header("Звуки фона (атмосфера, отдельно от музыки)")]
    [Tooltip("Добавляйте звуки кнопкой +. Все играют одновременно и по кругу.")]
    [SerializeField] private AmbientSound[] ambientSounds;
    [Tooltip("Общая громкость всех звуков фона.")]
    [SerializeField, Range(0f, 1f)] private float ambienceVolume = 0.5f;
    [SerializeField] private float ambienceFadeIn = 2f;

    [Header("Звуки")]
    [SerializeField] private AudioClip screenTapSound;
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;

    private AudioSource musicSource;
    private AudioSource sfxSource;
    private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();

    private int currentIndex = -1;
    private Coroutine musicRoutine;

    private void Awake()
    {
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = false;
        musicSource.volume = 0f;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
    }

    private void Start()
    {
        HookAllButtons();

        if (backgroundTracks != null && backgroundTracks.Length > 0)
            musicRoutine = StartCoroutine(PlaylistRoutine());

        StartAmbience();
    }

    // ===== Звуки фона (атмосфера) =====

    private void StartAmbience()
    {
        if (ambientSounds == null) return;

        foreach (AmbientSound s in ambientSounds)
        {
            if (s == null || s.clip == null) continue;

            AudioSource src = gameObject.AddComponent<AudioSource>();
            src.clip = s.clip;
            src.loop = true;
            src.playOnAwake = false;
            src.volume = 0f;
            src.Play();
            ambientSources.Add(src);
        }

        StartCoroutine(FadeAmbienceIn());
    }

    private IEnumerator FadeAmbienceIn()
    {
        float t = 0f;
        while (t < ambienceFadeIn)
        {
            t += Time.unscaledDeltaTime;
            ambienceFade = Mathf.Clamp01(t / Mathf.Max(ambienceFadeIn, 0.01f));
            ApplyAmbienceVolume();
            yield return null;
        }
        ambienceFade = 1f;
        ApplyAmbienceVolume();
    }

    // Применяет громкость: общая * громкость конкретного звука * плавное нарастание
    private void ApplyAmbienceVolume()
    {
        int i = 0;
        foreach (AmbientSound s in ambientSounds)
        {
            if (s == null || s.clip == null) continue;
            if (i < ambientSources.Count)
                ambientSources[i].volume = ambienceVolume * s.volume * ambienceFade;
            i++;
        }
    }

    // Для будущих ползунков громкости в настройках
    public void SetAmbienceVolume(float value)
    {
        ambienceVolume = Mathf.Clamp01(value);
        ApplyAmbienceVolume();
    }

    public void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);
        musicSource.volume = musicVolume;
    }

    private void Update()
    {
        // Срабатывает и на мыши, и на первом касании пальцем
        if (Input.GetMouseButtonDown(0) && !IsPointerOverButton())
            PlaySfx(screenTapSound);
    }

    // ===== Музыка =====

    private IEnumerator PlaylistRoutine()
    {
        while (true)
        {
            currentIndex = GetNextIndex();
            AudioClip clip = backgroundTracks[currentIndex];

            if (clip == null)
            {
                yield return null; // пропускаем пустой слот
                continue;
            }

            musicSource.clip = clip;
            musicSource.Play();

            // Плавное нарастание громкости
            yield return FadeMusic(0f, musicVolume, fadeTime);

            // Ждём, пока трек почти закончится
            float remaining = clip.length - fadeTime * 2f;
            float t = 0f;
            while (t < remaining)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            // Плавное затухание перед следующим треком
            yield return FadeMusic(musicVolume, 0f, fadeTime);
            musicSource.Stop();
        }
    }

    private int GetNextIndex()
    {
        int count = backgroundTracks.Length;
        if (count == 1) return 0;

        if (shuffle)
        {
            int next;
            do { next = Random.Range(0, count); } while (next == currentIndex);
            return next;
        }

        return (currentIndex + 1) % count;
    }

    private IEnumerator FadeMusic(float from, float to, float time)
    {
        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(from, to, t / Mathf.Max(time, 0.01f));
            yield return null;
        }
        musicSource.volume = to;
    }

    // Переключить на следующий трек вручную (можно повесить на кнопку)
    public void NextTrack()
    {
        if (backgroundTracks == null || backgroundTracks.Length == 0) return;

        if (musicRoutine != null) StopCoroutine(musicRoutine);
        musicSource.Stop();
        musicRoutine = StartCoroutine(PlaylistRoutine());
    }

    // ===== Звуки кнопок и тапов =====

    // Подписываем звук на все кнопки сцены (в том числе в выключенных панелях)
    private void HookAllButtons()
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            foreach (Button b in root.GetComponentsInChildren<Button>(true))
                b.onClick.AddListener(() => PlaySfx(buttonClickSound));
        }
    }

    // Нажали на кнопку или на пустое место?
    private bool IsPointerOverButton()
    {
        if (EventSystem.current == null) return false;

        PointerEventData data = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        raycastResults.Clear();
        EventSystem.current.RaycastAll(data, raycastResults);

        foreach (RaycastResult r in raycastResults)
        {
            if (r.gameObject.GetComponentInParent<Button>() != null)
                return true;
        }
        return false;
    }

    public void PlaySfx(AudioClip clip)
    {
        if (clip != null)
            sfxSource.PlayOneShot(clip, sfxVolume);
    }
}

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Звук главного экрана:
///  1) фоновая музыка (играет только в этой сцене),
///  2) звук тапа по пустому месту экрана (по кнопкам не срабатывает),
///  3) звук нажатия на кнопки (подключается ко всем кнопкам сцены автоматически).
/// Повесьте на пустой объект "MainMenuAudio" в сцене главного меню.
/// </summary>
public class MainMenuAudio : MonoBehaviour
{
    [Header("Клипы")]
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private AudioClip screenTapSound;
    [SerializeField] private AudioClip buttonClickSound;

    [Header("Громкость")]
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.5f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
    [SerializeField] private float musicFadeInTime = 1.5f;

    private AudioSource musicSource;
    private AudioSource sfxSource;
    private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();

    private void Awake()
    {
        // Источник для музыки (зациклен)
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.clip = backgroundMusic;
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.volume = 0f;

        // Источник для коротких звуков
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
    }

    private void Start()
    {
        HookAllButtons();

        if (backgroundMusic != null)
        {
            musicSource.Play();
            StartCoroutine(FadeMusic(0f, musicVolume, musicFadeInTime));
        }
    }

    private void Update()
    {
        // GetMouseButtonDown(0) срабатывает и на мыши, и на первом касании пальцем
        if (Input.GetMouseButtonDown(0) && !IsPointerOverButton())
            PlaySfx(screenTapSound);
    }

    // Подписываем звук на все кнопки сцены (в том числе выключенные)
    private void HookAllButtons()
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            foreach (Button b in root.GetComponentsInChildren<Button>(true))
                b.onClick.AddListener(() => PlaySfx(buttonClickSound));
        }
    }

    // Проверка: нажали на кнопку или на пустое место?
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

    private IEnumerator FadeMusic(float from, float to, float time)
    {
        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(from, to, t / time);
            yield return null;
        }
        musicSource.volume = to;
    }
}

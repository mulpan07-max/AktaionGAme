using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class SceneNavigation : MonoBehaviour
{
    [Header("Мастерская: стол и холодильник")]
    public GameObject deskPanel;     // панель стола
    public GameObject fridgePanel;   // панель холодильника

    [Header("Комнаты (свайпы)")]
    public GameObject panel0;          // стартовая
    public GameObject panel1;          // комната с дверью
    public GameObject panel2;          // следующая вправо

    [Header("Дверь -> видео -> операция")]
    public GameObject videoRoot;       // RawImage с видео
    public VideoPlayer videoPlayer;
    public GameObject surgeryPanel;    // бывшая панель3
    public SurgeryGameManager gameManager; // необязательно: сбросит операцию при входе

    [Header("Остальные панели (кнопки)")]
    public GameObject patientOrderPanel;
    public GameObject chapterPanel;

    [Header("Свайп")]
    [SerializeField] private float minSwipeDistance = 100f;

    [Header("Звук перехода в операцию")]
    public AudioSource sfxSource;       // AudioSource для эффектов
    public AudioClip surgeryEnterSound; // звук, который играет один раз
    [Range(0f, 1f)] public float surgeryEnterVolume = 1f;

    private bool videoPlaying;
    private Vector2 startTouchPos;
    private bool isSwiping;

    private void Start()
    {
        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.loopPointReached += OnVideoFinished;
            videoPlayer.prepareCompleted += OnVideoPrepared;
            videoPlayer.Prepare();   // загружаем заранее, пока игрок в комнатах
        }
        ShowOnly(panel0);
    }
    private void OnVideoPrepared(VideoPlayer vp) { }
    private void OnDestroy()
    {
        if (videoPlayer != null) videoPlayer.loopPointReached -= OnVideoFinished;
        if (videoPlayer != null) videoPlayer.prepareCompleted -= OnVideoPrepared;

    }

    // ---------- Сцены ----------
    public void BackToMenu() => SceneManager.LoadScene("MainMenu");
    public void RestartScene() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);

    // ---------- Кнопки ----------
    public void OpenPanel0() => ShowOnly(panel0);
    public void OpenPanel1() => ShowOnly(panel1);   // крестик на SurgeryPanel
    public void OpenPanel2() => ShowOnly(panel2);
    public void OpenPatientOrderPanel() => ShowOnly(patientOrderPanel);
    public void OpenChapterPanel() => ShowOnly(chapterPanel);

    // Повесь на дверь в panel1
    private bool waitingFirstFrame;

    public void PlayDoorVideo()
    {
        if (videoPlaying) return;
        if (videoPlayer == null || videoRoot == null) { StartSurgery(); return; }

        videoPlaying = true;
        waitingFirstFrame = true;

        // Видео включаем ПОВЕРХ комнаты, комнату пока НЕ выключаем
        videoRoot.transform.SetAsLastSibling();
        videoRoot.SetActive(true);

        videoPlayer.sendFrameReadyEvents = true;
        videoPlayer.frameReady += OnFirstFrame;

        videoPlayer.Stop();
        videoPlayer.Play();
    }

    // Первый кадр видео готов: теперь можно спрятать комнату под ним
    private void OnFirstFrame(VideoPlayer vp, long frame)
    {
        vp.frameReady -= OnFirstFrame;
        vp.sendFrameReadyEvents = false;
        waitingFirstFrame = false;

        if (panel1 != null) panel1.SetActive(false);
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        videoPlaying = false;
        StartSurgery();
    }

    private void StartSurgery()
    {
        if (gameManager != null) gameManager.BeginOperation();
        else if (surgeryPanel != null) surgeryPanel.SetActive(true);

        // Звук один раз при входе в операцию
        if (sfxSource != null && surgeryEnterSound != null)
            sfxSource.PlayOneShot(surgeryEnterSound, surgeryEnterVolume);

        if (videoRoot != null) videoRoot.SetActive(false);
        if (videoPlayer != null) videoPlayer.Stop();

        if (panel0 != null) panel0.SetActive(false);
        if (panel1 != null) panel1.SetActive(false);
        if (panel2 != null) panel2.SetActive(false);
    }

    // ---------- Свайпы ----------
    private void Update()
    {
        if (IsActive(deskPanel) || IsActive(fridgePanel)) return;
        if (videoPlaying) return;
        if (surgeryPanel != null && surgeryPanel.activeSelf) return; // в операции свайпов нет

        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began) { startTouchPos = t.position; isSwiping = true; }
            else if (t.phase == TouchPhase.Ended && isSwiping) { isSwiping = false; HandleSwipe(t.position - startTouchPos); }
        }

#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0)) { startTouchPos = Input.mousePosition; isSwiping = true; }
        else if (Input.GetMouseButtonUp(0) && isSwiping) { isSwiping = false; HandleSwipe((Vector2)Input.mousePosition - startTouchPos); }
#endif
    }

    private void HandleSwipe(Vector2 delta)
    {
        if (Mathf.Abs(delta.x) < minSwipeDistance) return;
        if (Mathf.Abs(delta.x) < Mathf.Abs(delta.y)) return;

        bool right = delta.x > 0;

        // Текущая панель определяется по факту, а не по памяти
        if (IsActive(panel0)) { if (right) ShowOnly(panel1); }
        else if (IsActive(panel1)) { ShowOnly(right ? panel2 : panel0); }
        else if (IsActive(panel2)) { if (!right) ShowOnly(panel1); }
    }

    // ---------- Вспомогательное ----------
    private static bool IsActive(GameObject g) => g != null && g.activeSelf;

    private void HideAll()
    {
        GameObject[] all = { panel0, panel1, panel2, surgeryPanel, videoRoot,
                     patientOrderPanel, chapterPanel, deskPanel, fridgePanel };
        foreach (var g in all) if (g != null) g.SetActive(false);
    }

    private void ShowOnly(GameObject target)
    {
        if (target == null) return;
        if (videoPlayer != null) videoPlayer.Stop();
        videoPlaying = false;
        HideAll();
        target.SetActive(true);
    }

    public void OpenDesk()
    {
        if (fridgePanel != null) fridgePanel.SetActive(false);
        if (deskPanel != null) deskPanel.SetActive(true);
    }

    public void OpenFridge()
    {
        if (deskPanel != null) deskPanel.SetActive(false);
        if (fridgePanel != null) fridgePanel.SetActive(true);
    }

    // Для кнопок X на обеих панелях
    public void CloseOverlays()
    {
        if (deskPanel != null) deskPanel.SetActive(false);
        if (fridgePanel != null) fridgePanel.SetActive(false);
    }
}
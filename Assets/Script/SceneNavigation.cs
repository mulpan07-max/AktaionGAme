using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class SceneNavigation : MonoBehaviour

{

  
    public void BackToRoom0B() { useB = true; ShowOnly(room0B); }   // X на панели выбора

    [Header("Комнаты версии A (до операции)")]
    public GameObject room0A;
    public GameObject room1A;          // комната с дверью
    public GameObject room2A;

    [Header("Комнаты версии B (после операции)")]
    public GameObject room0B;
    public GameObject room1B;
    public GameObject room2B;

    [Header("Столы (в комнате 2)")]
    public GameObject deskA;
    public GameObject deskB;           // сюда попадаем после операции

    [Header("Экраны-вложения")]
    public GameObject sinkPanel;       // кран, X -> стол
    public GameObject containerPanel;  // контейнер, X -> стол
    public GameObject posterPanel;     // плакат, X -> комната 2
    public GameObject fridgePanel;     // холодильник (поверх комнаты)

    [Header("Дверь -> видео -> операция")]
    public GameObject videoRoot;
    public VideoPlayer videoPlayer;
    public GameObject surgeryPanel;
    public SurgeryGameManager gameManager;

    [Header("Остальные панели")]
    public GameObject patientOrderPanel;
    public GameObject chapterPanel;

    [Header("Свайп")]
    [SerializeField] private float minSwipeDistance = 100f;

    [Header("Звук перехода в операцию")]
    public AudioSource sfxSource;
    public AudioClip surgeryEnterSound;
    [Range(0f, 1f)] public float surgeryEnterVolume = 1f;



    private bool useB;                 // false = версия A, true = версия B
    private bool videoPlaying;
    private Vector2 startTouchPos;
    private bool isSwiping;

    // ---------- Текущая версия ----------
    private GameObject[] Rooms() => useB
        ? new[] { room0B, room1B, room2B }
        : new[] { room0A, room1A, room2A };

    private GameObject CurrentDesk() => useB ? deskB : deskA;

    private void Start()
    {
        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.loopPointReached += OnVideoFinished;
            videoPlayer.Prepare();
        }
        useB = false;
        ShowOnly(room0A);
    }

    private void OnDestroy()
    {
        if (videoPlayer != null) videoPlayer.loopPointReached -= OnVideoFinished;
    }

    // ---------- Сцены ----------
    public void BackToMenu() => SceneManager.LoadScene("MainMenu");
    public void RestartScene() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);

    public GameObject handoverPanel;   // панель с людьми
    public GameObject resultPanel;

    // ================= ВЕРСИЯ A (старые экраны) =================
    public void OpenRoom0A() { useB = false; ShowOnly(room0A); }
    public void OpenRoom1A() { useB = false; ShowOnly(room1A); }
    public void OpenRoom2A() { useB = false; ShowOnly(room2A); }
    public void OpenDeskA() { useB = false; ShowOnly(deskA); }
    public void BackToRoom2A() { useB = false; ShowOnly(room2A); }
    public void OpenHandoverPanel() => ShowOnly(handoverPanel);
    public void OpenResultPanel() => ShowOnly(resultPanel);
    public void OpenToRoom0B() { useB = true; ShowOnly(room0B); }   // X на столе A

    // ================= ВЕРСИЯ B (новые экраны) =================
    public void OpenRoom0B() { useB = true; ShowOnly(room0B); }
    public void OpenRoom1B() { useB = true; ShowOnly(room1B); }
    public void OpenRoom2B() { useB = true; ShowOnly(room2B); }
    public void OpenDeskB() { useB = true; ShowOnly(deskB); }
    public void BackToRoom2B() { useB = true; ShowOnly(room2B); }    // X на столе B

    // ============ ОБЩИЕ экраны (кран, контейнер, плакат) ============
    public void OpenSink() => ShowOnly(sinkPanel);
    public void OpenContainer() => ShowOnly(containerPanel);
    public void OpenPoster() => ShowOnly(posterPanel);

    // Явные кнопки «назад» для общих экранов, по версии
    public void BackToDeskA() { useB = false; ShowOnly(deskA); }
    public void BackToDeskB() { useB = true; ShowOnly(deskB); }
    public void BackToRoom2FromPosterA() { useB = false; ShowOnly(room2A); }
    public void BackToRoom2FromPosterB() { useB = true; ShowOnly(room2B); }

    // Холодильник поверх комнаты (как раньше)
    public void OpenFridge() { if (fridgePanel != null) fridgePanel.SetActive(true); }
    public void CloseFridge() { if (fridgePanel != null) fridgePanel.SetActive(false); }

    public void OpenPatientOrderPanel() => ShowOnly(patientOrderPanel);
    public void OpenChapterPanel() => ShowOnly(chapterPanel);


    // ---------- Конец операции (вызывает SurgeryGameManager) ----------
    public void OnOperationComplete()
    {
        useB = true;
        ShowOnly(deskB);
        
    }

    // ---------- Дверь и видео ----------
    public void PlayDoorVideo()
    {
        if (videoPlaying) return;
        if (videoPlayer == null || videoRoot == null) { StartSurgery(); return; }

        videoPlaying = true;
        videoRoot.transform.SetAsLastSibling();
        videoRoot.SetActive(true);

        videoPlayer.sendFrameReadyEvents = true;
        videoPlayer.frameReady += OnFirstFrame;
        videoPlayer.Stop();
        videoPlayer.Play();
    }

    private void OnFirstFrame(VideoPlayer vp, long frame)
    {
        vp.frameReady -= OnFirstFrame;
        vp.sendFrameReadyEvents = false;
        SetActive(Rooms()[1], false);
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        videoPlaying = false;
        StartSurgery();
    }

    private void StartSurgery()
    {
        if (gameManager != null) gameManager.BeginOperation();
        else SetActive(surgeryPanel, true);

        if (sfxSource != null && surgeryEnterSound != null)
            sfxSource.PlayOneShot(surgeryEnterSound, surgeryEnterVolume);

        SetActive(videoRoot, false);
        if (videoPlayer != null) videoPlayer.Stop();

        foreach (var g in AllRooms()) SetActive(g, false);
    }

    // ---------- Свайпы ----------
    private void Update()
    {
        if (videoPlaying) return;
        if (IsActive(surgeryPanel) || IsActive(fridgePanel)) return;

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

        GameObject[] rooms = Rooms();
        int i = -1;
        for (int k = 0; k < rooms.Length; k++) if (IsActive(rooms[k])) { i = k; break; }
        if (i < 0) return; // сейчас не в комнате (стол, кран, плакат...) - свайпов нет

        int next = i + (delta.x > 0 ? 1 : -1); // вправо: 0->1->2, влево: 2->1->0
        if (next < 0 || next >= rooms.Length) return;
        ShowOnly(rooms[next]);
    }

    // ---------- Вспомогательное ----------
    private static bool IsActive(GameObject g) => g != null && g.activeSelf;
    private static void SetActive(GameObject g, bool v) { if (g != null) g.SetActive(v); }

    private GameObject[] AllRooms() => new[] { room0A, room1A, room2A, room0B, room1B, room2B };

    private void HideAll()
    {
        GameObject[] others = { deskA, deskB, sinkPanel, containerPanel, posterPanel, fridgePanel,
                        surgeryPanel, videoRoot, patientOrderPanel, chapterPanel,
                        handoverPanel, resultPanel };
        foreach (var g in AllRooms()) SetActive(g, false);
        foreach (var g in others) SetActive(g, false);
    }

    private void ShowOnly(GameObject target)
    {
        if (target == null) return;
        if (videoPlayer != null) videoPlayer.Stop();
        videoPlaying = false;
        HideAll();
        target.SetActive(true);
    }
}
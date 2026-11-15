using System;
using UnityEngine;

// Глобальное состояние: текущая глава, пройденные главы, результат (качество). Сохраняется в PlayerPrefs.
public class GameState : MonoBehaviour
{
    public const int ChapterCount = 3;
    const string KeyChapter = "ug_chapter", KeyDone = "ug_done_", KeyQuality = "ug_quality_";

    static GameState instance;
    public static GameState Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<GameState>();
                if (instance == null) new GameObject("GameState").AddComponent<GameState>(); // Awake задаст instance
            }
            return instance;
        }
    }

    public event Action Changed; // UI подписывается и обновляется сам

    [SerializeField] int currentChapter = 1;
    readonly bool[] completed = new bool[ChapterCount];
    readonly int[] quality = new int[ChapterCount]; // -1 = нет результата

    public int CurrentChapter => currentChapter;

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
    }

    public bool IsCompleted(int chapter) => InRange(chapter) && completed[chapter - 1];
    public int GetQuality(int chapter) => InRange(chapter) ? quality[chapter - 1] : -1;
    public bool IsReached(int chapter) => chapter <= currentChapter || IsCompleted(chapter);

    public void SetChapter(int chapter)
    {
        currentChapter = Mathf.Clamp(chapter, 1, ChapterCount);
        Save();
    }

    // Отмечает главу пройденной; если это текущая глава, открывает следующую
    public void CompleteChapter(int chapter, int resultQuality = -1)
    {
        if (!InRange(chapter)) return;
        completed[chapter - 1] = true;
        quality[chapter - 1] = resultQuality;
        if (chapter == currentChapter && chapter < ChapterCount) currentChapter++;
        Save();
    }

    bool InRange(int c) => c >= 1 && c <= ChapterCount;

    void Save()
    {
        PlayerPrefs.SetInt(KeyChapter, currentChapter);
        for (int i = 0; i < ChapterCount; i++)
        {
            PlayerPrefs.SetInt(KeyDone + i, completed[i] ? 1 : 0);
            PlayerPrefs.SetInt(KeyQuality + i, quality[i]);
        }
        PlayerPrefs.Save();
        Changed?.Invoke();
    }

    void Load()
    {
        currentChapter = Mathf.Clamp(PlayerPrefs.GetInt(KeyChapter, 1), 1, ChapterCount);
        for (int i = 0; i < ChapterCount; i++)
        {
            completed[i] = PlayerPrefs.GetInt(KeyDone + i, 0) == 1;
            quality[i] = PlayerPrefs.GetInt(KeyQuality + i, -1);
        }
    }

    // ---- Debug (ПКМ по заголовку компонента в Inspector, в Play Mode) ----
    [ContextMenu("Debug/Chapter 1")] void DebugCh1() => SetChapter(1);
    [ContextMenu("Debug/Chapter 2")] void DebugCh2() => SetChapter(2);
    [ContextMenu("Debug/Chapter 3")] void DebugCh3() => SetChapter(3);
    [ContextMenu("Debug/Complete current (85%)")] void DebugComplete() => CompleteChapter(currentChapter, 85);
    [ContextMenu("Debug/Reset save")]
    void DebugReset()
    {
        PlayerPrefs.DeleteAll();
        currentChapter = 1;
        for (int i = 0; i < ChapterCount; i++) { completed[i] = false; quality[i] = -1; }
        Save();
    }
}

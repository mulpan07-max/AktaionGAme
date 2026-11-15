using UnityEngine;
using UnityEngine.Profiling;

/// <summary>
/// Performance overlay for the Assignment 4 phone measurement.
/// Shows: test time, current / average / minimum FPS, average and worst frame time (ms), memory (MB).
/// Add it to an empty GameObject in the MainMenu scene. It stays alive when SurgeryScene loads.
/// Tap the overlay to reset the measurement.
/// </summary>
public class PerfOverlay : MonoBehaviour
{
    [SerializeField] private int fontSize = 32;
    [SerializeField] private bool showOverlay = true;

    private static PerfOverlay instance;

    private float elapsed;
    private int frames;
    private float minFps = float.MaxValue;
    private float worstFrameMs;
    private float shownFps;
    private float fpsTimer;
    private int fpsFrames;

    private GUIStyle style;
    private Rect box = new Rect(20, 20, 560, 260);

    private void Awake()
    {
        if (instance != null) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        Application.targetFrameRate = 60;
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        elapsed += dt;
        frames++;

        float frameMs = dt * 1000f;
        if (elapsed > 1f && frameMs > worstFrameMs) worstFrameMs = frameMs;

        fpsTimer += dt;
        fpsFrames++;
        if (fpsTimer >= 0.5f)
        {
            shownFps = fpsFrames / fpsTimer;
            if (elapsed > 1f && shownFps < minFps) minFps = shownFps;
            fpsTimer = 0f;
            fpsFrames = 0;
        }

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            Vector2 p = Input.GetTouch(0).position;
            if (box.Contains(new Vector2(p.x, Screen.height - p.y))) ResetStats();
        }
    }

    private void ResetStats()
    {
        elapsed = 0f; frames = 0; minFps = float.MaxValue; worstFrameMs = 0f;
    }

    private void OnGUI()
    {
        if (!showOverlay) return;
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.box) { fontSize = fontSize, alignment = TextAnchor.UpperLeft };
            style.normal.textColor = Color.green;
        }

        float avgFps = elapsed > 0f ? frames / elapsed : 0f;
        float avgMs = frames > 0 ? elapsed * 1000f / frames : 0f;
        float usedMb = Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);
        float reservedMb = Profiler.GetTotalReservedMemoryLong() / (1024f * 1024f);
        int min = (int)(elapsed / 60f), sec = (int)(elapsed % 60f);

        string text =
            $"Time: {min:00}:{sec:00}\n" +
            $"FPS now: {shownFps:F0}\n" +
            $"FPS avg / min: {avgFps:F1} / {(minFps == float.MaxValue ? 0 : minFps):F0}\n" +
            $"Frame avg / worst: {avgMs:F1} / {worstFrameMs:F1} ms\n" +
            $"Memory used / reserved: {usedMb:F0} / {reservedMb:F0} MB";

        GUI.Box(box, text, style);
    }
}

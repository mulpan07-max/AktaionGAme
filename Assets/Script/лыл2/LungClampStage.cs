using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Этап операции на лёгком:
//   1) зажать бронх (тап по бронху)
//   2) выбрать НОЖНИЦЫ и тапами сделать 4 сосуда полупрозрачными (разрезать)
//   3) выбрать ПИНЦЕТ и тапами убрать сосуды (они исчезают)
// Выбранный инструмент исчезает со стола и возвращается на место после завершения шага.
// Неверный инструмент = штраф + кровотечение (качество падает, пока шаг не будет завершён).
public class LungClampStage : MonoBehaviour
{
    public enum ToolType { Scissors, Tweezers, MiniKnife }

    [Serializable]
    public class Tool
    {
        public string label;        // просто название для списка
        public ToolType type;
        public Button button;       // инструмент на панели (Image + Button)
    }

    private enum Step { Clamp, Cut, Remove }
    private enum VesselState { Intact, Cut, Removed }

    [SerializeField] private SurgeryGameManager surgery;       // для штрафов качества

    [Header("Сосуды")]
    [SerializeField] private Button[] vessels;                 // 4 сосуда: Image + Button
    [Range(0f, 1f)][SerializeField] private float cutAlpha = 0.4f;   // полупрозрачность после разреза

    [Header("Бронх")]
    [SerializeField] private Button bronchusButton;
    [SerializeField] private Image bronchusImage;
    [Tooltip("Если пусто - берётся спрайт, который стоит на бронхе в сцене")]
    [SerializeField] private Sprite bronchusNormal;
    [SerializeField] private Sprite bronchusClamped;           // успешное состояние
    [SerializeField] private Sprite bronchusBleeding;          // кровопролитие
    [Tooltip("Включено: сначала зажим бронха, потом сосуды. Выключено: сначала сосуды, бронх в конце")]
    [SerializeField] private bool clampBronchusFirst = true;

    [Header("Инструменты (нажмите + чтобы добавить)")]
    [SerializeField] private Tool[] tools;

    [Header("Кровотечение при неверном инструменте")]
    [SerializeField] private GameObject bleedingEffect;        // необязательно
    [SerializeField] private float bleedInterval = 1.5f;
    [SerializeField] private int maxBleedTicks = 8;

    [Header("Подсказки на экране")]
    [SerializeField] private TMP_Text instructionText;         // можно тот же, что у TissueOpeningManager
    [SerializeField] private string clampText = "CLAMP THE BRONCHUS";
    [SerializeField] private string cutText = "SELECT THE SCISSORS AND CUT THE VESSELS";
    [SerializeField] private string removeText = "SELECT THE TWEEZERS AND REMOVE THE VESSELS";

    [SerializeField] private float finishDelay = 0.8f;

    private Action onDone;
    private readonly List<Step> order = new List<Step>();
    private VesselState[] vesselStates;
    private int stepIndex;
    private Tool held;
    private bool active, bleeding, done, bronchusIsClamped;
    private float timer;
    private int ticks;
    private Sprite capturedNormal;

    public bool IsDone => done;
    private Step Current => order[Mathf.Clamp(stepIndex, 0, order.Count - 1)];

    // Запускает этап. callback вызывается, когда все шаги выполнены.
    public void Begin(Action callback)
    {
        onDone = callback;
        gameObject.SetActive(true);
        ResetState();
        active = true;

        bronchusButton.onClick.RemoveAllListeners();
        bronchusButton.onClick.AddListener(OnBronchusTapped);

        for (int i = 0; i < vessels.Length; i++)
        {
            int idx = i;
            vessels[i].onClick.RemoveAllListeners();
            vessels[i].onClick.AddListener(() => OnVesselTapped(idx));
        }
        for (int i = 0; i < tools.Length; i++)
        {
            int idx = i;
            tools[i].button.onClick.RemoveAllListeners();
            tools[i].button.onClick.AddListener(() => OnToolTapped(idx));
        }

        EnterStep(0);
    }

    // Вызывается из TissueOpeningManager.ResetOpening()
    public void ResetStage()
    {
        ResetState();
        gameObject.SetActive(false);
    }

    private void ResetState()
    {
        StopAllCoroutines();
        active = false; bleeding = false; done = false; bronchusIsClamped = false;
        timer = 0f; ticks = 0; stepIndex = 0; held = null;

        order.Clear();
        if (clampBronchusFirst) { order.Add(Step.Clamp); order.Add(Step.Cut); order.Add(Step.Remove); }
        else { order.Add(Step.Cut); order.Add(Step.Remove); order.Add(Step.Clamp); }

        if (capturedNormal == null && bronchusImage != null) capturedNormal = bronchusImage.sprite;
        SetBronchusSprite(bronchusNormal != null ? bronchusNormal : capturedNormal);

        vesselStates = new VesselState[vessels.Length];
        for (int i = 0; i < vessels.Length; i++)
        {
            vessels[i].gameObject.SetActive(true);
            SetVesselAlpha(i, 1f);
        }

        foreach (var t in tools) SetToolVisible(t, true);
        if (bleedingEffect != null) bleedingEffect.SetActive(false);
    }

    // ---------- шаги ----------
    private void EnterStep(int i)
    {
        stepIndex = i;
        if (i >= order.Count) { Finish(); return; }

        if (instructionText != null)
        {
            switch (order[i])
            {
                case Step.Clamp: instructionText.text = clampText; break;
                case Step.Cut: instructionText.text = cutText; break;
                case Step.Remove: instructionText.text = removeText; break;
            }
        }
    }

    private void CompleteStep()
    {
        StopBleeding();
        ReturnTool();
        EnterStep(stepIndex + 1);
    }

    private bool ToolRequired(out ToolType type)
    {
        type = ToolType.Scissors;
        if (Current == Step.Cut) { type = ToolType.Scissors; return true; }
        if (Current == Step.Remove) { type = ToolType.Tweezers; return true; }
        return false;   // на шаге с бронхом инструмент не нужен
    }

    // ---------- инструменты ----------
    private void OnToolTapped(int idx)
    {
        if (!active || done || held != null) return;
        if (!ToolRequired(out ToolType needed)) return;   // шаг зажима: инструменты не нужны

        Tool t = tools[idx];
        if (t.type == needed)
        {
            held = t;
            SetToolVisible(t, false);     // инструмент исчезает со стола
        }
        else
        {
            Mistake();                    // неверный инструмент
        }
    }

    private void ReturnTool()
    {
        if (held == null) return;
        SetToolVisible(held, true);       // инструмент возвращается на место
        held = null;
    }

    private static void SetToolVisible(Tool t, bool visible)
    {
        if (t == null || t.button == null) return;
        CanvasGroup cg = t.button.GetComponent<CanvasGroup>();
        if (cg == null) cg = t.button.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = visible ? 1f : 0f;      // место на панели сохраняется
        cg.blocksRaycasts = visible;
        cg.interactable = visible;
    }

    // ---------- сосуды ----------
    private void OnVesselTapped(int i)
    {
        if (!active || done || held == null) return;

        if (Current == Step.Cut && held.type == ToolType.Scissors && vesselStates[i] == VesselState.Intact)
        {
            vesselStates[i] = VesselState.Cut;
            SetVesselAlpha(i, cutAlpha);                     // полупрозрачный
            if (AllVessels(VesselState.Cut)) CompleteStep();
        }
        else if (Current == Step.Remove && held.type == ToolType.Tweezers && vesselStates[i] == VesselState.Cut)
        {
            vesselStates[i] = VesselState.Removed;
            vessels[i].gameObject.SetActive(false);          // исчезает
            if (AllVessels(VesselState.Removed)) CompleteStep();
        }
    }

    private bool AllVessels(VesselState s)
    {
        foreach (var v in vesselStates) if (v != s) return false;
        return true;
    }

    private void SetVesselAlpha(int i, float a)
    {
        Graphic g = vessels[i].targetGraphic != null ? vessels[i].targetGraphic : vessels[i].GetComponent<Graphic>();
        if (g == null) return;
        Color c = g.color; c.a = a; g.color = c;
    }

    // ---------- бронх ----------
    private void OnBronchusTapped()
    {
        if (!active || done || Current != Step.Clamp) return;
        bronchusIsClamped = true;
        SetBronchusSprite(bronchusClamped);
        CompleteStep();
    }

    private void SetBronchusSprite(Sprite s)
    {
        if (bronchusImage != null && s != null) bronchusImage.sprite = s;
    }

    // ---------- кровотечение ----------
    private void Mistake()
    {
        if (surgery != null) surgery.MakeMistake();
        StartBleeding();
    }

    private void StartBleeding()
    {
        if (bleeding) return;
        bleeding = true;
        timer = 0f;
        if (!bronchusIsClamped) SetBronchusSprite(bronchusBleeding);
        if (bleedingEffect != null) bleedingEffect.SetActive(true);
    }

    private void StopBleeding()
    {
        if (!bleeding) return;
        bleeding = false;
        if (!bronchusIsClamped) SetBronchusSprite(bronchusNormal != null ? bronchusNormal : capturedNormal);
        if (bleedingEffect != null) bleedingEffect.SetActive(false);
    }

    private void Update()
    {
        if (!active || !bleeding || done || ticks >= maxBleedTicks) return;

        timer += Time.deltaTime;
        if (timer >= bleedInterval)
        {
            timer = 0f;
            ticks++;
            if (surgery != null) surgery.MakeMistake();
        }
    }

    // ---------- конец ----------
    private void Finish()
    {
        done = true;
        StartCoroutine(FinishRoutine());
    }

    private IEnumerator FinishRoutine()
    {
        yield return new WaitForSeconds(finishDelay);
        active = false;
        onDone?.Invoke();
    }
}
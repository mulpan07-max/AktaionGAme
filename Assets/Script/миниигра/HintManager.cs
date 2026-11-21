using UnityEngine;
using TMPro;

// Плашка с подсказкой в углу экрана.
// Два режима:
//  - ручной:  HintManager.Instance.Show("текст")  (или объект с HintTrigger)
//  - авто:    после операции сама выбирает подсказку по состоянию игры
// Повесить на ВСЕГДА активный объект (GameManager).
[DefaultExecutionOrder(-50)]
public class HintManager : MonoBehaviour
{
    public static HintManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject panel;                 // плашка (по умолчанию выключена)
    [SerializeField] private TMP_Text hintText;
    [Tooltip("Если любой из этих объектов включён - плашка скрывается (видео, итоги и т.п.)")]
    [SerializeField] private GameObject[] hideWhenAnyActive;
    [SerializeField] private float fadeInTime = 0.3f;

    [Header("Для авто-режима")]
    [SerializeField] private FridgeMedicine fridge;
    [SerializeField] private OrganTreatment treatment;
    [SerializeField] private HandoverManager handover;
    [SerializeField] private GameObject handoverPanel;         // панель с людьми

    [Header("Тексты авто-подсказок")]
    [TextArea][SerializeField] private string hintInspect = "INSPECT THE ORGAN: TAP ALL 4 DARK SPOTS";
    [TextArea][SerializeField] private string hintWash = "WASH THE ORGAN: TAP THE SINK AND SWIPE OVER THE ORGAN";
    [TextArea][SerializeField] private string hintFridge = "GO TO THE FRIDGE AND TAKE THE MEDICINE";
    [TextArea][SerializeField] private string hintApply = "DRAG THE MEDICINE ONTO THE ORGAN";
    [TextArea][SerializeField] private string hintWait = "THE ORGAN IS BEING PACKAGED...";
    [TextArea][SerializeField] private string hintDeliver = "TAKE THE ORGAN TO ROOM 0 AND PRESS RESULT";
    [TextArea][SerializeField] private string hintChoose = "CHOOSE THE RIGHT PERSON";
    [Tooltip("Пусто = плашка скрывается после сдачи органа")]
    [TextArea][SerializeField] private string hintDelivered = "";

    private bool autoEnabled;
    private string manualText = "";
    private string current = null;
    private CanvasGroup group;
    private float fadeT = 1f;

    private void Awake()
    {
        Instance = this;
        if (panel != null)
        {
            group = panel.GetComponent<CanvasGroup>();
            if (group == null) group = panel.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;     // плашка никогда не перехватывает нажатия
            group.interactable = false;
        }
    }

    // Ручная подсказка (выключает авто-режим)
    public void Show(string text) { autoEnabled = false; manualText = text; }

    // Включить авто-подсказки (после операции)
    public void EnableAuto() { autoEnabled = true; }

    // Убрать подсказку
    public void Hide() { autoEnabled = false; manualText = ""; }

    private void Update()
    {
        if (panel == null || hintText == null) return;

        string t = autoEnabled ? ComputeAuto() : manualText;
        bool hidden = string.IsNullOrEmpty(t) || AnyBlockingActive();

        if (panel.activeSelf == hidden) panel.SetActive(!hidden);
        if (hidden) return;

        if (t != current)
        {
            current = t;
            hintText.text = t;
            fadeT = 0f;                       // плавно появляется при смене текста
        }

        if (group != null)
        {
            fadeT = fadeInTime > 0f ? Mathf.Min(1f, fadeT + Time.unscaledDeltaTime / fadeInTime) : 1f;
            group.alpha = fadeT;
        }
    }

    private bool AnyBlockingActive()
    {
        foreach (var g in hideWhenAnyActive) if (g != null && g.activeInHierarchy) return true;
        return false;
    }

    private string ComputeAuto()
    {
        if (handover != null && handover.Delivered) return hintDelivered;
        if (handoverPanel != null && handoverPanel.activeInHierarchy) return hintChoose;

        if (treatment != null)
        {
            if (treatment.IsPacked) return hintDeliver;
            if (treatment.IsTreated) return hintWait;
        }

        OrganDirt dirt = OrganDirt.Instance;
        if (dirt == null) return "";
        if (!dirt.AllSpotsTouched) return hintInspect;
        if (!dirt.IsClean) return hintWash;
        if (fridge != null && !fridge.Taken) return hintFridge;
        return hintApply;
    }
}

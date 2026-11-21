using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Комната 0: кнопка Result -> панель с людьми -> выбор -> (верно) экран итогов.
// Повесить на ВСЕГДА активный объект (GameManager).
public class HandoverManager : MonoBehaviour
{
    [Serializable]
    public class Person
    {
        public string label;            // просто название для списка в Inspector (необязательно)
        public Button button;           // кнопка с фото человека
        public Image frame;             // рамка/картинка для подсветки (если пусто - подсвечивается картинка кнопки)
        public bool isCorrect;          // галочка: этот человек - правильный выбор
        [NonSerialized] public bool locked;   // уже выбирали неверно
    }

    [SerializeField] private SceneNavigation navigation;
    [SerializeField] private SurgeryGameManager surgery;       // для штрафа за ошибку (можно пусто)
    [SerializeField] private OrganTreatment treatment;         // чтобы знать, что орган уже в пакете
    [SerializeField] private PatientData patient;              // данные пациента для экрана итогов

    [Header("Кнопка Result (комната 0)")]
    [Tooltip("Result работает только когда орган уже в пакете")]
    [SerializeField] private bool requirePacked = true;
    [SerializeField] private GameObject notReadyPopup;         // текст "Nothing to hand over yet" (можно пусто)
    [SerializeField] private float popupTime = 1.5f;

    [Header("Люди (нажмите + чтобы добавить)")]
    [SerializeField] private Person[] people;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color correctColor = new Color(0.3f, 0.9f, 0.35f, 1f);
    [SerializeField] private Color wrongColor = new Color(0.95f, 0.25f, 0.25f, 1f);
    [SerializeField] private float delayBeforeResult = 1f;
    [SerializeField] private bool penalizeWrong = true;

    [Header("Экран итогов")]
    [SerializeField] private TMP_Text destinationText;
    [SerializeField] private TMP_Text reasonText;
    [SerializeField] private TMP_Text qualityText;
    [SerializeField] private TMP_Text verdictText;
    [Tooltip("От этого качества и выше работа считается идеальной")]
    [SerializeField] private int perfectQuality = 95;
    [SerializeField] private PatientCardView patientCard;   // та же карточка, что на панели заказа



    private PatientData CurrentPatient =>
        (patientCard != null && patientCard.Current != null) ? patientCard.Current : patient;

    private bool busy;

    public bool Delivered { get; private set; }

    private void Start()
    {
        bool hasCorrect = false;
        for (int i = 0; i < people.Length; i++)
        {
            int idx = i;
            if (people[i].button != null) people[i].button.onClick.AddListener(() => Pick(idx));
            if (people[i].isCorrect) hasCorrect = true;
        }
        if (!hasCorrect) Debug.LogWarning("HandoverManager: ни у одного человека не стоит галочка Is Correct");
        if (notReadyPopup != null) notReadyPopup.SetActive(false);
        ResetHandover();
    }

    // Кнопка Result в комнате 0 -> OnClick -> HandoverManager.OnResultsButton
    public void OnResultsButton()
    {
        if (requirePacked && (treatment == null || !treatment.IsPacked))
        {
            if (notReadyPopup != null) StartCoroutine(PopupRoutine());
            return;
        }
        if (navigation != null) navigation.OpenHandoverPanel();
    }

    private IEnumerator PopupRoutine()
    {
        notReadyPopup.SetActive(true);
        yield return new WaitForSecondsRealtime(popupTime);
        notReadyPopup.SetActive(false);
    }

    private void Pick(int i)
    {
        Person p = people[i];
        if (busy || p.locked) return;

        if (p.isCorrect)
        {
            busy = true;
            Delivered = true;
            SetColor(p, correctColor);
            StartCoroutine(CorrectRoutine());
        }
        else
        {
            p.locked = true;
            SetColor(p, wrongColor);
            if (penalizeWrong && surgery != null) surgery.MakeMistake();
        }
    }

    private IEnumerator CorrectRoutine()
    {
        yield return new WaitForSecondsRealtime(delayBeforeResult);
        FillResult();
        busy = false;
        if (navigation != null) navigation.OpenResultPanel();
    }

    private void FillResult()
    {
        PatientData p = CurrentPatient;

        int quality = surgery != null ? surgery.OrganQuality : 100;
        int required = 0;
        if (p != null) int.TryParse(p.minQuality.Replace("%", "").Trim(), out required);

        if (destinationText != null)
            destinationText.text = p != null
                ? "ORGAN SENT TO: " + p.patientName + " (" + p.age + ", " + p.occupation + ")"
                : "ORGAN SENT";

        if (reasonText != null)
            reasonText.text = p != null
                ? "REASON: " + p.requiredOrgan + " TRANSPLANT - " + p.diagnosis
                : "";

        if (qualityText != null) qualityText.text = "ORGAN QUALITY: " + quality + "%";

        if (verdictText != null)
        {
            if (quality >= perfectQuality) verdictText.text = "PERFECT WORK";
            else if (quality >= required) verdictText.text = "GOOD WORK";
            else verdictText.text = "POOR QUALITY - BELOW REQUIRED " + required + "%";
        }
    }
    private void SetColor(Person p, Color c)
    {
        Image img = p.frame != null ? p.frame : (p.button != null ? p.button.image : null);
        if (img != null) img.color = c;
    }

    // Вызвать в начале новой операции/главы
    public void ResetHandover()
    {
        busy = false;
        Delivered = false;
        foreach (var p in people) { p.locked = false; SetColor(p, normalColor); }
    }
}
using UnityEngine;
using TMPro;

public class SurgeryGameManager : MonoBehaviour
{
    [SerializeField] private SceneNavigation navigation;
    [Header("Panels")]
    [SerializeField] private GameObject patientOrderPanel;
    [SerializeField] private GameObject surgeryPanel;
    [SerializeField] private GameObject workshopPanel;   // куда уходим после операции
    [SerializeField] private GameObject resultPanel;     // необязательно

    [Header("Level 3: тапы по зонам перед разрезом (необязательно)")]
    [SerializeField] private TapZonesStage tapZonesStage;
    [Header("UI Text")]
    [SerializeField] private TMP_Text qualityText;
    [SerializeField] private TMP_Text finalQualityText;  // необязательно
    [SerializeField] private TMP_Text resultTitle;       // необязательно

    [Header("Operation stages")]
    [SerializeField] private IncisionController incisionController;
    [SerializeField] private GameObject incisionArea;
    [SerializeField] private GameObject tissueOpeningArea;
    [SerializeField] private TissueOpeningManager tissueOpeningManager;
    [SerializeField] private GameObject organStage;      // печень + контейнер
    [SerializeField] private OrganDrag organDrag;

    [Header("Level 2: инъекция перед операцией (необязательно)")]
    [Tooltip("Если задан - операция начинается с шприца. Если пусто - всё как в Level 1.")]
    [SerializeField] private InjectionStage injectionStage;

    [Header("Quality")]
    [SerializeField] private int mistakePenalty = 5;     // -5% за срыв
    [SerializeField] private int minQuality = 60;

    [Header("After operation")]
    [Tooltip("Включено: сначала панель результата (кнопка Continue вызывает GoToWorkshop). " +
             "Выключено: сразу в мастерскую.")]
    [SerializeField] private bool showResultFirst = false;
   
    private int organQuality = 100;
    public int OrganQuality => organQuality;

    private void Start()
    {
        ResetOperationState();
    }

    private void ResetOperationState()
    {
        organQuality = 100;

        if (incisionArea != null) incisionArea.SetActive(true);
        if (tissueOpeningArea != null) tissueOpeningArea.SetActive(false);
        if (organStage != null) organStage.SetActive(false);

        if (incisionController != null) incisionController.ResetIncision();
        if (tissueOpeningManager != null) tissueOpeningManager.ResetOpening();
        if (organDrag != null) organDrag.ResetOrgan();

        // Level 2: до инъекции разрез недоступен
        if (injectionStage != null)
        {
            injectionStage.ResetStage();
            if (incisionArea != null) incisionArea.SetActive(false);
        }
        if (tapZonesStage != null)
        {
            tapZonesStage.ResetStage();
            if (incisionArea != null) incisionArea.SetActive(false);
        }
        UpdateQualityUI();
    }

    public void ShowPatientOrder()
    {
        ResetOperationState();
        SetPanels(patient: true);
    }
    public void ApplyPenalty(int percent)
    {
        organQuality = Mathf.Clamp(organQuality - percent, 0, 100);
        UpdateQualityUI();
    }
    public void BeginOperation()
    {
        ResetOperationState();
        SetPanels(surgery: true);

        if (injectionStage != null) injectionStage.Begin(OnInjectionDone);
        if (tapZonesStage != null) tapZonesStage.Begin(OnInjectionDone);
    }

    // Инъекция сделана -> начинается разрез
    private void OnInjectionDone()
    {
        if (incisionArea != null) incisionArea.SetActive(true);
    }

    // Вызывать при срыве линии разреза
    public void MakeMistake()
    {
        organQuality = Mathf.Clamp(organQuality - mistakePenalty, 0, 100);
        UpdateQualityUI();
    }

    private void UpdateQualityUI()
    {
        if (qualityText != null)
            qualityText.text = "ORGAN QUALITY: " + organQuality + "%";
    }

    // Вызывается из OrganDrag, когда орган лёг в контейнер
    public void CompleteOperation()
    {
        if (showResultFirst && resultPanel != null)
        {
            SetPanels(result: true);
            if (finalQualityText != null) finalQualityText.text = "FINAL QUALITY: " + organQuality + "%";
            if (resultTitle != null)
                resultTitle.text = organQuality >= minQuality ? "OPERATION SUCCESSFUL" : "OPERATION FAILED";
        }
        else
        {
            GoToWorkshop();
        }
    }

    // Можно повесить на кнопку Continue на панели результата
    public void GoToWorkshop()
    {
        if (navigation != null) navigation.OnOperationComplete(); // откроет СтолB
        else SetPanels(workshop: true);
    }

    public void RestartOperation()
    {
        ShowPatientOrder();

        if (OrganDirt.Instance != null) OrganDirt.Instance.ResetDirt();
    }

    private void SetPanels(bool patient = false, bool surgery = false, bool workshop = false, bool result = false)
    {
        if (patientOrderPanel != null) patientOrderPanel.SetActive(patient);
        if (surgeryPanel != null) surgeryPanel.SetActive(surgery);
        if (workshopPanel != null) workshopPanel.SetActive(workshop);
        if (resultPanel != null) resultPanel.SetActive(result);
    }
}
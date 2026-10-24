
using UnityEngine;
using TMPro;

public class SurgeryGameManager : MonoBehaviour
{
    [Header("Game Panels")]
    [SerializeField] private GameObject patientOrderPanel;
    [SerializeField] private GameObject surgeryPanel;
    [SerializeField] private GameObject resultPanel;

    [Header("UI Text")]
    [SerializeField] private TMP_Text qualityText;
    [SerializeField] private TMP_Text finalQualityText;
    [SerializeField] private TMP_Text resultTitle;

    [SerializeField] private GameObject incisionArea;
    [SerializeField] private GameObject tissueOpeningArea;
    [SerializeField] private TissueOpeningManager tissueOpeningManager;


    [SerializeField] private IncisionController incisionController;
    [SerializeField] private GameObject vesselStage;
    [SerializeField] private VesselCuttingManager vesselCuttingManager;
    [Header("Organ Extraction")]
    [SerializeField] private OrganDrag organDrag;
    [SerializeField] private GameObject organContainer;

    private int organQuality = 100;

    private void Start()
    {
        ShowPatientOrder();
    }

    public void ShowPatientOrder()
    {
        organQuality = 100;

        if (incisionArea != null)
            incisionArea.SetActive(true);

        if (tissueOpeningArea != null)
            tissueOpeningArea.SetActive(false);

        if (incisionController != null)
            incisionController.ResetIncision();

        if (tissueOpeningManager != null)
            tissueOpeningManager.ResetOpening();
        if (vesselCuttingManager != null)
            vesselCuttingManager.ResetVessels();

        if (vesselStage != null)
            vesselStage.SetActive(false);
        if (organDrag != null)
            organDrag.ResetOrgan();

        if (organContainer != null)
            organContainer.SetActive(false);
        patientOrderPanel.SetActive(true);
        surgeryPanel.SetActive(false);
        resultPanel.SetActive(false);
    }

    public void BeginOperation()
    {
        patientOrderPanel.SetActive(false);
        surgeryPanel.SetActive(true);
        resultPanel.SetActive(false);
        if (vesselStage != null)
            vesselStage.SetActive(false);
        if (organContainer != null)
            organContainer.SetActive(false);

        UpdateQualityUI();
    }

    public void MakeMistake()
    {
        organQuality -= 10;
        organQuality = Mathf.Clamp(organQuality, 0, 100);

        UpdateQualityUI();
    }

    private void UpdateQualityUI()
    {
        qualityText.text = "ORGAN QUALITY: " + organQuality + "%";
    }

    public void CompleteOperation()
    {
        patientOrderPanel.SetActive(false);
        surgeryPanel.SetActive(false);
        resultPanel.SetActive(true);

        finalQualityText.text = "FINAL QUALITY: " + organQuality + "%";

        if (organQuality >= 60)
        {
            resultTitle.text = "OPERATION SUCCESSFUL";
        }
        else
        {
            resultTitle.text = "OPERATION FAILED";
        }
    }

    public void RestartOperation()
    {
        ShowPatientOrder();
    }
}


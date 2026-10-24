
using UnityEngine;
using TMPro;

public class VesselCuttingManager : MonoBehaviour
{
    [Header("Vessels")]
    [SerializeField] private Vessel[] vessels;

    [Header("UI")]
    [SerializeField] private TMP_Text instructionText;

    [Header("Game Manager")]
    [SerializeField] private SurgeryGameManager gameManager;
    [Header("Organ Extraction")]
    [SerializeField] private OrganDrag organDrag;
    [SerializeField] private GameObject organContainer;

    private int nextVessel = 1;
    private bool stageCompleted;

    public void TryCutVessel(Vessel vessel)
    {
        if (stageCompleted)
            return;

        int selectedNumber = vessel.GetVesselNumber();

        if (selectedNumber == nextVessel)
        {
            vessel.Cut();
            nextVessel++;

            if (nextVessel > vessels.Length)
            {
                CompleteVesselStage();
            }
            else
            {
                instructionText.text =
                    "CUT VESSEL " + nextVessel;
            }
        }
        else
        {
            gameManager.MakeMistake();

            instructionText.text =
                "WRONG VESSEL! CUT VESSEL " + nextVessel;
        }
    }

    private void CompleteVesselStage()
    {
        stageCompleted = true;

        instructionText.text = "EXTRACT THE LIVER";

        if (organContainer != null)
            organContainer.SetActive(true);

        if (organDrag != null)
            organDrag.EnableExtraction();

        Debug.Log("All vessels cut. Organ extraction unlocked!");
    }

    public void ResetVessels()
    {
        nextVessel = 1;
        stageCompleted = false;

        foreach (Vessel vessel in vessels)
        {
            if (vessel != null)
                vessel.ResetVessel();
        }
    }
}

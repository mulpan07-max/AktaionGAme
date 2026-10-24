
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Vessel : MonoBehaviour, IPointerClickHandler
{
    [Header("Vessel Settings")]
    [SerializeField] private int vesselNumber;

    [Header("References")]
    [SerializeField] private VesselCuttingManager cuttingManager;
    [SerializeField] private Image vesselImage;

    private bool isCut;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isCut || cuttingManager == null)
            return;

        cuttingManager.TryCutVessel(this);
    }

    public int GetVesselNumber()
    {
        return vesselNumber;
    }

    public void Cut()
    {
        isCut = true;

        if (vesselImage != null)
            vesselImage.color = Color.gray;
    }

    public void ResetVessel()
    {
        isCut = false;

        if (vesselImage != null)
            vesselImage.color = Color.red;
    }
}


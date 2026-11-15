using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Вешать на объект, который НЕ отключается (например, Canvas или пустой TerminalSystem)
public class TerminalController : MonoBehaviour
{
    [Header("Куда вернуться при закрытии")]
    public GameObject returnPanel;   // например, стартовая панель

   
    [Header("Panels")]
    public GameObject patientPanel;      // PatientOrderPanel
    public GameObject manualPanel;       // MANUALPanel
    public GameObject root;              // общий родитель обеих панелей, если есть (можно пусто)

    [Header("Views")]
    public PatientCardView patientCard;
    public ManualView manualView;

    [Header("Buttons")]
    public Button helpButton;            // ?Button
    public Button patientCloseButton;    // CloseButton в PatientOrderPanel -> закрыть терминал
    public Button manualCloseButton;     // CloseButton в MANUALPanel -> назад на карточку

    public UnityEvent OnTerminalClosed;

    void Awake()
    {
        if (helpButton != null) helpButton.onClick.AddListener(ShowManual);
        if (patientCloseButton != null) patientCloseButton.onClick.AddListener(Close);
        if (manualCloseButton != null) manualCloseButton.onClick.AddListener(ShowPatients);
        if (manualView != null) manualView.FinishedReading += ShowPatients;
    }

    void OnDestroy()
    {
        if (manualView != null) manualView.FinishedReading -= ShowPatients;
    }

    public void Open()
    {
        if (root != null) root.SetActive(true);
        ShowPatients();
    }

    public void Close()
    {
        if (manualPanel != null) manualPanel.SetActive(false);
        if (patientPanel != null) patientPanel.SetActive(false);
        if (root != null) root.SetActive(false);

        // Явно показываем нужную панель
        if (returnPanel != null) returnPanel.SetActive(true);

        OnTerminalClosed?.Invoke(); ;
    }

    public void ShowPatients()
    {
        if (patientPanel != null) patientPanel.SetActive(true);
        if (manualPanel != null) manualPanel.SetActive(false);
        if (patientCard != null) patientCard.Refresh();
    }
 
    public void ShowManual()
    {
        if (patientPanel != null) patientPanel.SetActive(false);
        if (manualPanel != null) manualPanel.SetActive(true);
        if (manualView != null) manualView.Open();
    }
}

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Холодильник: нажали на лекарство -> панель с информацией -> OK -> лекарство появляется на столе у органа.
// Повесить на ВСЕГДА активный объект (например, GameManager).
public class FridgeMedicine : MonoBehaviour
{
    [Serializable]
    public class Item
    {
        public Button button;                    // лекарство в холодильнике
        public string title = "MEDICINE";
        [TextArea] public string description;    // текст на панели информации
        public Sprite sprite;                    // как лекарство выглядит на столе (можно оставить пустым)
    }

    [SerializeField] private SceneNavigation navigation;      // чтобы закрыть холодильник (можно пусто)
    [SerializeField] private Item[] items;

    [Header("Панель информации")]
    [SerializeField] private GameObject infoPanel;
    [SerializeField] private TMP_Text infoTitle;
    [SerializeField] private TMP_Text infoDescription;
    [SerializeField] private Image infoIcon;                  // необязательно
    [SerializeField] private Button okButton;
    [SerializeField] private Button cancelButton;             // необязательно

    [Header("Лекарство на столе")]
    [SerializeField] private Image medicineOnDesk;            // выключено по умолчанию

    private int pending = -1;
    public bool Taken { get; private set; }

    private void Start()
    {
        for (int i = 0; i < items.Length; i++)
        {
            int idx = i;
            items[i].button.onClick.AddListener(() => Open(idx));
        }
        okButton.onClick.AddListener(Take);
        if (cancelButton != null) cancelButton.onClick.AddListener(() => infoPanel.SetActive(false));
        ResetMedicine();
    }

    private void Open(int i)
    {
        if (Taken) return;
        pending = i;
        Item it = items[i];
        if (infoTitle != null) infoTitle.text = it.title;
        if (infoDescription != null) infoDescription.text = it.description;
        if (infoIcon != null && it.sprite != null) infoIcon.sprite = it.sprite;
        infoPanel.SetActive(true);
    }

    private void Take()
    {
        if (pending < 0 || Taken) return;
        Taken = true;
        infoPanel.SetActive(false);

        foreach (var it in items) it.button.interactable = false;    // можно взять только одно
        items[pending].button.gameObject.SetActive(false);           // взятое пропадает из холодильника

        if (medicineOnDesk != null)
        {
            if (items[pending].sprite != null) medicineOnDesk.sprite = items[pending].sprite;
            medicineOnDesk.gameObject.SetActive(true);
        }
        if (navigation != null) navigation.CloseFridge();
    }

    // Вызвать в начале новой операции/главы
    public void ResetMedicine()
    {
        Taken = false;
        pending = -1;
        infoPanel.SetActive(false);
        foreach (var it in items) { it.button.gameObject.SetActive(true); it.button.interactable = true; }
        if (medicineOnDesk != null) medicineOnDesk.gameObject.SetActive(false);
    }
}
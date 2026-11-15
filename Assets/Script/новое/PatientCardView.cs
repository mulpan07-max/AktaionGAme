using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Карточка пациента под иерархию PatientOrderPanel
public class PatientCardView : MonoBehaviour
{
    [Header("Data (3 assets, chapter 1..3)")]
    public PatientData[] patients = new PatientData[3];

    [Header("Тексты (пустые поля просто пропускаются)")]
    public TMP_Text patientIdText;     // PatientIDText
    public TMP_Text nameText;          // Name Text
    public TMP_Text ageText;           // Age Text
    public TMP_Text diseaseText;       // Disease текст
    public TMP_Text organText;         // REQUIRED ORGANText
    public TMP_Text minQualityText;    // MIN. QUALITYText
    public TMP_Text specimenText;      // SPECIMENText

    [Header("Optional")]
    public Image faceImage;            // Image (портрет)
    public Button beginButton;
    public UnityEvent OnBeginOperation;

    void Awake()
    {
        if (beginButton != null) beginButton.onClick.AddListener(() => OnBeginOperation?.Invoke());
    }

    void OnEnable()
    {
        GameState.Instance.Changed += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        if (GameState.Instance != null) GameState.Instance.Changed -= Refresh;
    }

    public void Refresh()
    {
        int ch = GameState.Instance.CurrentChapter;
        if (patients == null || ch < 1 || ch > patients.Length || patients[ch - 1] == null) return;
        PatientData p = patients[ch - 1];

        Set(nameText, p.patientName);
        Set(ageText, "AGE: " + p.age);
        Set(diseaseText, p.showDiagnosis ? p.diagnosis : "PENDING");
        Set(patientIdText, "PATIENT ID: " + p.patientId);
        Set(organText, "REQUIRED ORGAN: " + p.requiredOrgan);
        Set(minQualityText, "MIN. QUALITY: " + p.minQuality);
        Set(specimenText, "SPECIMEN: " + p.specimen);

        // Портрет меняем только если спрайт задан, иначе остаётся твой
        if (faceImage != null && p.face != null) faceImage.sprite = p.face;
    }

    static void Set(TMP_Text t, string v) { if (t != null) t.text = v; }
}
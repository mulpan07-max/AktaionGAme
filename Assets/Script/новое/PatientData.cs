using UnityEngine;

// Данные одного пациента (один ассет на главу)
[CreateAssetMenu(fileName = "Patient_01", menuName = "Underground Surgeon/Patient Data")]
public class PatientData : ScriptableObject
{
    public string patientId;
    public string patientName;
    public string age;
    public string occupation;
    public string diagnosis;
    public string symptoms;
    public string requiredOrgan;
    public string minQuality;
    public string specimen;
    public string payment;
    public string delivery;
    public bool showDiagnosis = true; // false -> в карточке "PENDING"
    public Sprite face;

    // Заполнение значениями по умолчанию: ПКМ по заголовку ассета
    [ContextMenu("Fill defaults/Chapter 1")]
    void Ch1() => Fill("001", "NURLAN K.", "48", "TAXI DRIVER", "CIRRHOSIS", "WEAK IMMUNITY, YELLOW SKIN", "LIVER", "60%", "BLOODSUCKER", "150", "STANDARD / EXPRESS", true);
    [ContextMenu("Fill defaults/Chapter 2")]
    void Ch2() => Fill("002", "AIGERIM S.", "31", "TEACHER", "PULMONARY FIBROSIS", "SHORTNESS OF BREATH, STIFF LUNG TISSUE", "LUNGS", "70%", "BILE HULK", "220", "EXPRESS ADVISED", false);
    [ContextMenu("Fill defaults/Chapter 3")]
    void Ch3() => Fill("003", "MADINA R.", "29", "UNKNOWN", "CARDIOMYOPATHY", "IRREGULAR HEARTBEAT, WEAK PULSE", "HEART", "80%", "CARAPACE", "300", "EXPRESS ONLY", false);

    void Fill(string id, string n, string a, string o, string d, string s, string org, string q, string sp, string p, string del, bool show)
    {
        patientId = id; patientName = n; age = a; occupation = o; diagnosis = d; symptoms = s;
        requiredOrgan = org; minQuality = q; specimen = sp; payment = p; delivery = del; showDiagnosis = show;
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}

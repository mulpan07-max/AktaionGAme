using UnityEngine;
using UnityEngine.UI;

// Состояние органа после лекарства: чистый -> светлеет -> через packDelay сек становится "органом в пакете".
// Повесить на ВСЕГДА активный объект (GameManager): время идёт, даже если игрок вышел в другую комнату.
public class OrganTreatment : MonoBehaviour
{
    [SerializeField] private Image organImage;            // орган на столе
    [SerializeField] private GameObject packedOrgan;      // орган в пакете (выключен по умолчанию)

    [Tooltip("Начальный оттенок органа. Image.color умеет только затемнять, поэтому 'светлее' = от этого оттенка к белому")]
    [SerializeField] private Color startTint = new Color(0.8f, 0.8f, 0.8f, 1f);
    [SerializeField] private float lightenDuration = 1f;  // за сколько секунд орган светлеет
    [SerializeField] private float packDelay = 3f;        // через сколько секунд после лекарства орган в пакете

    private enum State { Idle, Treated, Packed }
    private State state = State.Idle;
    private float treatedAt;

    public bool IsTreated => state != State.Idle;
    public bool IsPacked => state == State.Packed;

    private void Start() { ResetTreatment(); }

    // Вызывает MedicineDrag, когда лекарство отпустили на органе
    public bool TryApply()
    {
        if (state != State.Idle) return false;
        state = State.Treated;
        treatedAt = Time.time;
        return true;
    }

    private void Update()
    {
        if (state != State.Treated) return;
        float t = Time.time - treatedAt;
        organImage.color = Color.Lerp(startTint, Color.white, Mathf.Clamp01(t / lightenDuration));
        if (t >= packDelay) Pack();
    }

    private void Pack()
    {
        state = State.Packed;
        organImage.gameObject.SetActive(false);
        if (packedOrgan != null) packedOrgan.SetActive(true);
    }

    // Вызвать в начале новой операции/главы
    public void ResetTreatment()
    {
        state = State.Idle;
        organImage.gameObject.SetActive(true);
        organImage.color = startTint;
        if (packedOrgan != null) packedOrgan.SetActive(false);
    }
}
using UnityEngine;
using UnityEngine.UI;

// Level 2: упаковка органа стартует сама, когда игрок ВЕРНУЛСЯ на стол после крана.
// Повесить на любой объект внутри панели стола B (он активен, только когда игрок на столе).
// Использует OrganTreatment: через packDelay секунд орган станет "органом в пакете".
public class PackWhenReady : MonoBehaviour
{
    [Tooltip("Упаковка начнётся, когда этот этап завершён (вода/кран)")]
    [SerializeField] private WaterSoakStage prerequisite;
    [SerializeField] private OrganTreatment treatment;

    [Header("Орган на столе (необязательно)")]
    [Tooltip("Если задать оба поля, при возвращении на стол орган получит этот чистый спрайт")]
    [SerializeField] private Image deskOrganImage;
    [SerializeField] private Sprite cleanOrganSprite;

    [Header("Подсказка (необязательно)")]
    [SerializeField] private string packingHint = "PACKAGING THE ORGAN...";

    private bool done;

    private void Update()
    {
        if (done || treatment == null) return;
        if (prerequisite != null && !prerequisite.IsCompleted) return;

        done = true;

        if (deskOrganImage != null && cleanOrganSprite != null)
            deskOrganImage.sprite = cleanOrganSprite;

        if (HintManager.Instance != null && !string.IsNullOrEmpty(packingHint))
            HintManager.Instance.Show(packingHint);

        treatment.TryApply();   // запускает упаковку: через packDelay секунд орган "в пакете"
    }

    // Вызвать при новой операции, чтобы упаковка могла запуститься снова
    public void ResetState() { done = false; }
}

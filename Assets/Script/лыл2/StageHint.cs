using UnityEngine;

// Подсказка для этапа Level 2. Показывается, когда объект ВКЛЮЧАЕТСЯ,
// но только если условия выполнены (этап ещё не завершён и т.п.).
// Повесить на объект, который включается вместе с нужным экраном (стол B, панель крана).
public class StageHint : MonoBehaviour
{
    [TextArea][SerializeField] private string text;

    [Header("Условия (все необязательные)")]
    [Tooltip("Не показывать, если пузыри уже лопнуты")]
    [SerializeField] private BubbleCleaningStage hideAfterBubbles;
    [Tooltip("Не показывать, если кран уже пройден")]
    [SerializeField] private WaterSoakStage hideAfterSoak;
    [Tooltip("Показывать, только когда пузыри уже лопнуты")]
    [SerializeField] private BubbleCleaningStage requireBubblesDone;

    private void OnEnable()
    {
        if (HintManager.Instance == null) return;
        if (hideAfterBubbles != null && hideAfterBubbles.IsCompleted) return;
        if (hideAfterSoak != null && hideAfterSoak.IsCompleted) return;
        if (requireBubblesDone != null && !requireBubblesDone.IsCompleted) return;

        HintManager.Instance.Show(text);
    }
}

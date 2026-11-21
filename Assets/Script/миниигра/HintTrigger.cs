using UnityEngine;

// Повесить на любой объект/панель. Когда он ВКЛЮЧАЕТСЯ, плашка меняет подсказку.
// Mode = ShowText    -> показать текст из поля Text (для частей игры до стола)
// Mode = EnableAuto  -> включить авто-подсказки (повесить на стол после операции)
public class HintTrigger : MonoBehaviour
{
    public enum Mode { ShowText, EnableAuto }

    [SerializeField] private Mode mode = Mode.ShowText;
    [TextArea][SerializeField] private string text;

    private void OnEnable()
    {
        if (HintManager.Instance == null) return;
        if (mode == Mode.EnableAuto) HintManager.Instance.EnableAuto();
        else HintManager.Instance.Show(text);
    }
}
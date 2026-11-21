using UnityEngine;
using UnityEngine.EventSystems;

// Добавьте на UI-объект кольца. Нажатие запускает Trigger в Animator.
public class RingTap : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Animator animator;
    [SerializeField] private string triggerName = "Open";
    [SerializeField] private bool playOnlyOnce = true;

    private bool played;

    private void OnEnable()
    {
        played = false;
        if (animator != null)
            animator.ResetTrigger(triggerName);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (animator == null)
        {
            Debug.LogWarning("RingTap: Animator is not assigned.", this);
            return;
        }

        if (playOnlyOnce && played)
            return;

        played = true;
        animator.SetTrigger(triggerName);
    }
}

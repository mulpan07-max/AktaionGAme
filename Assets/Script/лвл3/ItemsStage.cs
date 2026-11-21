using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

// Этап: перетаскиваем закрывающие предметы в одну зону удаления.
// Каждый предмет исчезает после успешного сброса. Следующий предмет
// открывается через requiresItem. Когда все предметы убраны, появляется кольцо.
public class ItemsStage : MonoBehaviour
{
    [Serializable]
    public class Item
    {
        public string label;
        public ItemDrag drag;
        [Tooltip("Индекс предмета, который нужно убрать раньше. -1 = доступен сразу.")]
        public int requiresItem = -1;
        [NonSerialized] public bool removed;
    }

    [Header("Зона, куда перетаскивают предметы")]
    [SerializeField] private RectTransform dropZone;

    [Header("Предметы, которые нужно убрать")]
    [SerializeField] private Item[] items;

    [Header("Кольцо на сердце")]
    [SerializeField] private GameObject ringObject;

    [Header("Подсказка (необязательно)")]
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private string hintText = "DRAG THE OBJECTS INTO THE REMOVE AREA";
    [SerializeField] private string ringHintText = "TAP THE RING";

    [Header("Событие после удаления всех предметов (необязательно)")]
    [SerializeField] private UnityEvent onAllItemsRemoved;

    private bool running;
    private bool completed;

    private void OnEnable()
    {
        Begin();
    }

    public void Begin()
    {
        running = true;
        completed = false;

        if (ringObject != null)
            ringObject.SetActive(false);

        if (items != null)
        {
            foreach (Item item in items)
            {
                if (item == null || item.drag == null) continue;

                item.removed = false;
                item.drag.gameObject.SetActive(true);
                item.drag.dropZone = dropZone;
                item.drag.canDrag = true;
                item.drag.ResetPosition();
                item.drag.Released -= OnItemReleased;
                item.drag.Released += OnItemReleased;
            }
        }

        RefreshLocks();

        if (instructionText != null)
            instructionText.text = hintText;
    }

    public void ResetStage()
    {
        running = false;
        completed = false;

        if (ringObject != null)
            ringObject.SetActive(false);

        if (items == null) return;

        foreach (Item item in items)
        {
            if (item == null || item.drag == null) continue;

            item.removed = false;
            item.drag.Released -= OnItemReleased;
            item.drag.gameObject.SetActive(true);
            item.drag.dropZone = dropZone;
            item.drag.canDrag = true;
            item.drag.ResetPosition();
        }

        if (instructionText != null)
            instructionText.text = hintText;
    }

    private void OnItemReleased(ItemDrag drag, bool inDropZone)
    {
        if (!running || completed || !inDropZone || drag == null)
            return;

        int index = IndexOf(drag);
        if (index < 0 || items[index].removed || !IsUnlocked(items[index]))
            return;

        items[index].removed = true;
        drag.canDrag = false;

        // Предмет исчезает после того, как его отпустили внутри зоны.
        drag.gameObject.SetActive(false);

        RefreshLocks();

        if (AllItemsRemoved())
        {
            running = false;
            completed = true;

            if (ringObject != null)
                ringObject.SetActive(true);

            if (instructionText != null)
                instructionText.text = ringHintText;

            onAllItemsRemoved?.Invoke();
        }
    }

    private int IndexOf(ItemDrag drag)
    {
        if (items == null) return -1;

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null && items[i].drag == drag)
                return i;
        }

        return -1;
    }

    private bool IsUnlocked(Item item)
    {
        if (item.requiresItem < 0)
            return true;

        if (items == null || item.requiresItem >= items.Length || items[item.requiresItem] == null)
            return false;

        return items[item.requiresItem].removed;
    }

    private void RefreshLocks()
    {
        if (items == null) return;

        foreach (Item item in items)
        {
            if (item == null || item.drag == null) continue;
            item.drag.canDrag = running && !item.removed && IsUnlocked(item);
        }
    }

    private bool AllItemsRemoved()
    {
        if (items == null || items.Length == 0)
            return false;

        foreach (Item item in items)
        {
            if (item == null || !item.removed)
                return false;
        }

        return true;
    }
}

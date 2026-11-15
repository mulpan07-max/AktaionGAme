using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Мануал под иерархию: ManualInfoText + ManualNext. Тап = следующая страница / следующий раздел
public class ManualView : MonoBehaviour
{
    public ManualData manual;

    public TMP_Text infoText;          // ManualInfoText (Overflow = Page)
    public Button nextButton;          // ManualNext (растяни на область текста)
    public TMP_Text pageIndicator;     // опционально, "1/3"

    public event Action FinishedReading; // дочитали всё -> вернуться на карточку

    readonly List<int> unlocked = new List<int>();
    int sectionPos, page, pageCount;

    void Awake()
    {
        if (infoText != null) infoText.overflowMode = TextOverflowModes.Page;
        if (nextButton != null) nextButton.onClick.AddListener(Next);
    }

    // Начать чтение с первого открытого раздела
    public void Open()
    {
        unlocked.Clear();
        if (manual != null)
            for (int i = 0; i < manual.sections.Count; i++)
                if (GameState.Instance.IsReached(manual.sections[i].unlockChapter))
                    unlocked.Add(i);

        sectionPos = 0;
        ShowSection();
    }

    void ShowSection()
    {
        if (infoText == null || unlocked.Count == 0) return;

        var sec = manual.sections[unlocked[sectionPos]];
        infoText.text = (unlocked[sectionPos] + 1) + " " + sec.title + "\n\n" + sec.body;
        infoText.pageToDisplay = 1;

        Canvas.ForceUpdateCanvases();
        infoText.ForceMeshUpdate();
        pageCount = Mathf.Max(1, infoText.textInfo.pageCount);
        page = 1;
        UpdatePage();
    }

    void Next()
    {
        if (page < pageCount) { page++; UpdatePage(); return; }

        if (sectionPos < unlocked.Count - 1) { sectionPos++; ShowSection(); }
        else FinishedReading?.Invoke();
    }

    void UpdatePage()
    {
        infoText.pageToDisplay = page;
        if (pageIndicator != null)
            pageIndicator.text = page + "/" + pageCount + "  [" + (sectionPos + 1) + "/" + unlocked.Count + "]";
    }
}

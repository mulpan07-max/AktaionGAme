using System;
using System.Collections.Generic;
using UnityEngine;

// Разделы мануала: заголовок, текст, глава, с которой раздел открыт
[CreateAssetMenu(fileName = "ManualData", menuName = "Underground Surgeon/Manual Data")]
public class ManualData : ScriptableObject
{
    [Serializable]
    public class Section
    {
        public string title;
        [TextArea(3, 10)] public string body;
        public int unlockChapter = 1;
        public Section(string t, string b, int c) { title = t; body = b; unlockChapter = c; }
    }

    public List<Section> sections = new List<Section>();

    [ContextMenu("Fill defaults")]
    void FillDefaults()
    {
        sections = new List<Section>
        {
            new Section("BASICS", "Each chapter is one patient.\nRead the file. Prepare. Operate. Clean the organ. Send it.", 1),
            new Section("WORKSHOP", "Swipe to move between rooms.\nFridge: buy reagents.\nBook: read patient files.\nTerminal: patient list and delivery.", 1),
            new Section("OPERATION", "Swipe along the line to cut.\nUse clamps to open the tissue.\nCut the vessels in the correct order.\nDrag the organ into the container.\nEvery mistake lowers quality.", 1),
            new Section("INSPECTION", "Tap the dark spots to mark infection.\nMissed spots lower purity.", 1),
            new Section("CLEANING", "Rub the organ under water to remove grime.\nPlace it in the bath and pull it out in time.\nToo long ruins it.", 1),
            new Section("MATCHING", "Read the symptoms in the patient book.\nChoose the additive that fits.\nWrong choice lowers compatibility.", 1),
            new Section("PACKING", "Place the organ in the container.\nAdd gel. Hold to seal.\nWrite the batch number on the label.", 1),
            new Section("DELIVERY", "Choose a courier.\nExpress costs more and keeps the organ fresh.\nStandard is cheaper and lowers freshness.", 1),
            new Section("FRIDGE", "Reagents cost money.\nYour budget is limited.\nBuy only what the patient needs.", 1),
            new Section("QUALITY AND PAY", "Final quality = operation + cleaning + compatibility.\nFreshness is applied after delivery.\nHigher quality pays more.\nBelow minimum quality, the order fails.", 1),
            new Section("BELLOWS", "Tap in rhythm to pump the wash through the lungs.\nClear water means clean.", 2),
            new Section("INFLATION", "Hold to inflate the lungs.\nRelease inside the green zone.\nOverinflate and the organ bursts.", 2),
            new Section("SHELL", "Find the weak points in the carapace.\nStrike in rhythm.\nA wrong strike damages the heart.", 3),
            new Section("PULSE", "Repeat the rhythm shown in the patient file.\nThree correct series in a row.", 3),
        };
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}
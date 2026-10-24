
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChapterManager : MonoBehaviour
{
    [Header("Chapter Settings")]
    [SerializeField] private string chapterOneScene = "SurgeryScene";

    // Start the first available chapter
    public void StartChapterOne()
    {
        SceneManager.LoadScene(chapterOneScene);
    }
}


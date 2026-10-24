
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject chapterPanel;
    [SerializeField] private GameObject optionsPanel;

    // PLAY: Load gameplay scene
    public void PlayGame()
    {
        SceneManager.LoadScene("SurgeryScene");
    }

    // SELECT CHAPTER: Open chapter panel
    public void OpenChapters()
    {
        chapterPanel.SetActive(true);
        optionsPanel.SetActive(false);
    }

    // OPTIONS: Open settings panel
    public void OpenOptions()
    {
        optionsPanel.SetActive(true);
        chapterPanel.SetActive(false);
    }

    // CLOSE: Close all panels
    public void ClosePanels()
    {
        chapterPanel.SetActive(false);
        optionsPanel.SetActive(false);
    }

    // QUIT GAME
    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit();
    }
}


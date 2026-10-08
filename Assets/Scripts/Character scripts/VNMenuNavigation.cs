using UnityEngine;
using UnityEngine.SceneManagement;

public class VNMenuNavigation : MonoBehaviour
{
    [SerializeField] private string mainMenuScene = "MainMenu";

    public void ReturnToMainMenu()
    {
        if (!Application.CanStreamedLevelBeLoaded(mainMenuScene))
        {
            Debug.LogError(
                $"Add '{mainMenuScene}' to the Build Profiles Scene List.");
            return;
        }

        Time.timeScale = 1f;
        AudioListener.pause = false;

        SceneManager.LoadScene(mainMenuScene);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
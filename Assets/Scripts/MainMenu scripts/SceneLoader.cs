using UnityEngine;
using UnityEngine.SceneManagement; // Required for loading scenes

public class SceneLoader : MonoBehaviour
{
    [Header("Target Scene")]
    [Tooltip("Type the exact name of the scene file you want to switch to")]
    public string targetSceneName;

    // Attach this method to your UI Button's OnClick event
    public void LoadTargetScene()
    {
        if (!string.IsNullOrEmpty(targetSceneName))
        {
            SceneManager.LoadScene(targetSceneName);
        }
        else
        {
            Debug.LogWarning("No scene name specified on " + gameObject.name);
        }
    }

    // Optional: Call this directly to quit the game
    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit();
    }
}
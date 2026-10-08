using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Created automatically by VNDialogueManager; no component setup is required.
public sealed class VNSceneTransition : MonoBehaviour
{
    private static VNSceneTransition active;
    public static bool IsBusy => active != null;
    private CanvasGroup curtain;
    private float previousVolume;
    private bool ownsVolume;
    private bool hasKarma;
    private int karma;
    private string target;
    private bool newGame;
    private string newPlayerName;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetForPlay() { active = null; }

    public static bool TryBegin(string sceneName, float fadeSeconds)
    {
        return Begin(sceneName, fadeSeconds, false, null, 0);
    }

    public static bool TryBeginNewGame(string sceneName, float fadeSeconds, string playerName, int startingKarma)
    {
        return Begin(sceneName, fadeSeconds, true, playerName, startingKarma);
    }

    private static bool Begin(string sceneName, float fadeSeconds, bool startFresh, string playerName, int startingKarma)
    {
        if (IsBusy) return false;
        sceneName = (sceneName ?? "").Trim();
        if (string.IsNullOrEmpty(sceneName)) return false;
        Scene current = SceneManager.GetActiveScene();
        if (string.Equals(sceneName, current.name, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(sceneName, current.path, StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogError("Next Scene Name points to this same scene. Choose the following scene or leave the field empty.");
            return false;
        }
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError("Cannot continue to '" + sceneName + "'. Add it to Build Profiles > Scene List, check its box, and check the spelling of Next Scene Name.");
            return false;
        }

        var root = new GameObject("VN Scene Transition (Runtime)");
        DontDestroyOnLoad(root);
        active = root.AddComponent<VNSceneTransition>();
        active.target = sceneName;
        active.newGame = startFresh;
        active.newPlayerName = playerName;
        active.hasKarma = startFresh || KarmaManager.Instance != null;
        if (startFresh) active.karma = startingKarma;
        else if (active.hasKarma) active.karma = KarmaManager.Instance.CurrentKarma;
        active.previousVolume = AudioListener.volume;
        active.ownsVolume = true;
        active.StartCoroutine(active.Run(Mathf.Max(0f, fadeSeconds)));
        return true;
    }

    private IEnumerator Run(float duration)
    {
        try
        {
            BuildCurtain();
            yield return Fade(0f, 1f, duration);
            SceneManager.sceneLoaded += OnSceneLoaded;
            AsyncOperation load = null;
            try { load = SceneManager.LoadSceneAsync(target, LoadSceneMode.Single); }
            catch (Exception error) { Debug.LogError("Scene transition failed: " + error); }
            if (load != null) yield return load;
            else Debug.LogError("The next scene could not be loaded. Check the Console and Next Scene Name.");
            yield return Fade(1f, 0f, duration);
        }
        finally
        {
            Release();
            Destroy(gameObject);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!string.Equals(scene.name, target, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(scene.path, target, StringComparison.OrdinalIgnoreCase)) return;
        // sceneLoaded runs after the new scene's Awake calls and before Start.
        // Commit a fresh playthrough only after the destination actually loads.
        if (newGame)
        {
            VNSaveSystem.ResetForNewGame(newPlayerName);
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }
        if (hasKarma)
        {
            if (KarmaManager.Instance != null) KarmaManager.Instance.SetKarma(karma);
            else Debug.LogError("The new scene needs a KarmaManager to carry the previous karma forward.");
        }
        // PlayerName and Decisions are already session-wide in VNSaveSystem.
    }

    private void BuildCurtain()
    {
        var canvasObject = new GameObject("Black Fade", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasGroup), typeof(UnityEngine.UI.GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingLayerID = 0;
        canvas.sortingOrder = 32760;
        curtain = canvasObject.GetComponent<CanvasGroup>();
        curtain.alpha = 0f;
        curtain.blocksRaycasts = true;
        curtain.interactable = true;

        var imageObject = new GameObject("Black", typeof(RectTransform), typeof(CanvasRenderer),
            typeof(UnityEngine.UI.Image));
        imageObject.transform.SetParent(canvasObject.transform, false);
        var image = imageObject.GetComponent<UnityEngine.UI.Image>();
        image.color = Color.black;
        image.raycastTarget = true;
        var rect = image.rectTransform;
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed = Mathf.Min(duration, elapsed + Time.unscaledDeltaTime);
            float t = duration <= 0f ? 1f : elapsed / duration;
            t = t * t * (3f - 2f * t);
            SetFade(Mathf.Lerp(from, to, t));
            yield return null;
        }
        SetFade(to);
    }

    private void SetFade(float opacity)
    {
        curtain.alpha = opacity;
        AudioListener.volume = previousVolume * (1f - opacity);
    }

    private void Release()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (ownsVolume) { AudioListener.volume = previousVolume; ownsVolume = false; }
        if (active == this) active = null;
        if (curtain != null) curtain.gameObject.SetActive(false);
    }

    private void OnDestroy() { Release(); }
}

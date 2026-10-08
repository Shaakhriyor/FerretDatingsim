using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class VNNewGameMenu : MonoBehaviour
{
    [Header("Main menu connections")]
    [SerializeField] private VNHeartMenu loadMenu;
    [Tooltip("Optional: CanvasGroup on your teammate's main-menu buttons. The generated name panel uses its own canvas.")]
    [SerializeField] private CanvasGroup mainMenuButtons;
    [SerializeField] private TMP_FontAsset font;

    [Header("New playthrough")]
    [SerializeField] private string firstSceneName = "Prologue_Club";
    [SerializeField] private int startingKarma = 50;
    [Min(0f)] [SerializeField] private float fadeDuration = 0.75f;

    private GameObject panelRoot;
    private TMP_InputField nameInput;
    private TMP_Text errorText;
    private UnityEngine.UI.Button startButton, cancelButton;
    private bool busy, buttonsLocked, previousInteractable;
    private bool loadMenuDisabled, previousLoadMenuEnabled;
    private bool introInProgress;
    private System.Action introCancelled;
    public bool IsNamePanelOpen => panelRoot != null && panelRoot.activeSelf;
    private const int NameLimit = 24;

    // Opens immediately. For the eye-rush animation, wire New Game to SceneLoader.PlayNewGame instead.
    public void OpenNewGame()
    {
        if (!isActiveAndEnabled || introInProgress || busy || VNSceneTransition.IsBusy ||
            (loadMenu != null && loadMenu.IsOpen)) return;
        if (panelRoot != null && panelRoot.activeSelf) return;
        if (panelRoot == null) BuildPanel();
        LockMainButtons();
        if (loadMenu != null && !loadMenuDisabled)
        {
            previousLoadMenuEnabled = loadMenu.enabled;
            loadMenuDisabled = true;
            loadMenu.enabled = false;
        }
        panelRoot.SetActive(true);
        nameInput.text = "";
        errorText.text = "";
        SetInputEnabled(true);
        nameInput.Select();
        nameInput.ActivateInputField();
    }

    // SceneLoader reserves the UI before playing its animation.
    public bool TryBeginAnimatedIntro(System.Action onCancelled)
    {
        if (!isActiveAndEnabled || busy || introInProgress || IsNamePanelOpen ||
            VNSceneTransition.IsBusy || (loadMenu != null && loadMenu.IsOpen)) return false;
        introInProgress = true;
        introCancelled = onCancelled;
        LockMainButtons();
        if (loadMenu != null && !loadMenuDisabled)
        {
            previousLoadMenuEnabled = loadMenu.enabled;
            loadMenuDisabled = true;
            loadMenu.enabled = false;
        }
        return true;
    }

    public bool CompleteAnimatedIntro()
    {
        if (!introInProgress || !isActiveAndEnabled) return false;
        introInProgress = false;
        OpenNewGame();
        return IsNamePanelOpen;
    }

    private void NotifyIntroCancelled()
    {
        introInProgress = false;
        var callback = introCancelled;
        introCancelled = null;
        callback?.Invoke();
    }

    // Wire your teammate's Load Game button to this method.
    public void OpenLoadGame()
    {
        if (!isActiveAndEnabled || introInProgress || busy || VNSceneTransition.IsBusy ||
            (panelRoot != null && panelRoot.activeSelf)) return;
        if (loadMenu == null)
        {
            Debug.LogError("Assign the MAINMENU copy of VNHeartMenu to Load Menu on VNNewGameMenu.", this);
            return;
        }
        if (loadMenu.IsOpen) return;
        LockMainButtons();
        loadMenu.OpenLoad();
        if (loadMenu.IsOpen) StartCoroutine(WaitForLoadMenu());
        else RestoreMainButtons();
    }

    private IEnumerator WaitForLoadMenu()
    {
        while (loadMenu != null && loadMenu.IsOpen) yield return null;
        RestoreMainButtons();
    }

    public void Cancel()
    {
        if (busy) return;
        if (panelRoot != null) panelRoot.SetActive(false);
        RestoreLoadMenu();
        RestoreMainButtons();
        NotifyIntroCancelled();
        if (UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
    }

    public void StartNewGame()
    {
        if (busy || VNSceneTransition.IsBusy || panelRoot == null || !panelRoot.activeSelf) return;
        string chosenName = (nameInput.text ?? "").Trim();
        string error = ValidateName(chosenName);
        if (error != null) { errorText.text = error; return; }
        if (string.IsNullOrWhiteSpace(firstSceneName) || !Application.CanStreamedLevelBeLoaded(firstSceneName.Trim()))
        {
            errorText.text = "The opening scene is missing from the Build Profiles scene list.";
            return;
        }
        if (!VNSceneTransition.TryBeginNewGame(firstSceneName, fadeDuration, chosenName, startingKarma))
        {
            errorText.text = "Could not start the game. Check the Unity Console.";
            return;
        }
        busy = true;
        errorText.text = "Starting...";
        SetInputEnabled(false);
        StartCoroutine(WatchForLoadFailure());
    }

    public static string ValidateName(string value)
    {
        value = (value ?? "").Trim();
        if (value.Length == 0) return "Please enter a name.";
        if (value.Length > NameLimit) return "Please use 24 characters or fewer.";
        foreach (char character in value)
            if (char.IsControl(character) || character == '<' || character == '>')
                return "Please enter a name without line breaks or < > symbols.";
        return null;
    }

    private IEnumerator WatchForLoadFailure()
    {
        yield return null;
        while (VNSceneTransition.IsBusy) yield return null;
        // Successful loading unloads this scene and stops this coroutine.
        busy = false;
        SetInputEnabled(true);
        errorText.text = "The opening scene could not be loaded. Check the Unity Console.";
    }

    private void Update()
    {
        if (!busy && (introInProgress || IsNamePanelOpen) && Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame) Cancel();
    }

    private void LockMainButtons()
    {
        if (buttonsLocked || mainMenuButtons == null) return;
        previousInteractable = mainMenuButtons.interactable;
        buttonsLocked = true;
        mainMenuButtons.interactable = false;
    }

    private void RestoreMainButtons()
    {
        if (buttonsLocked && mainMenuButtons != null) mainMenuButtons.interactable = previousInteractable;
        buttonsLocked = false;
    }

    private void RestoreLoadMenu()
    {
        if (loadMenuDisabled && loadMenu != null) loadMenu.enabled = previousLoadMenuEnabled;
        loadMenuDisabled = false;
    }

    private void SetInputEnabled(bool value)
    {
        nameInput.interactable = value;
        startButton.interactable = value;
        cancelButton.interactable = value;
    }

    private void BuildPanel()
    {
        panelRoot = new GameObject("New Game Name Panel (Runtime)", typeof(RectTransform), typeof(Canvas),
            typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
        // Configure the TMP input before its first OnEnable runs.
        panelRoot.SetActive(false);
        var canvas = panelRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;
        var scaler = panelRoot.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var dimmer = MakeImage("Dimmer", panelRoot.transform, new Color(0f, 0f, 0f, 0.65f), 0, 0, 0, 0);
        dimmer.rectTransform.anchorMin = Vector2.zero; dimmer.rectTransform.anchorMax = Vector2.one;
        dimmer.rectTransform.offsetMin = Vector2.zero; dimmer.rectTransform.offsetMax = Vector2.zero;
        var border = MakeImage("Pink Border", panelRoot.transform, new Color32(227, 169, 191, 255), 0, 0, 780, 440);
        var card = MakeImage("Card", border.transform, new Color32(255, 249, 246, 255), 0, 0, 772, 432);
        MakeLabel("Your name", card.transform, 0, 145, 680, 65, 42, new Color32(45, 35, 44, 255));
        MakeLabel("What should we call you?", card.transform, 0, 95, 680, 40, 24, new Color32(95, 79, 90, 255));

        var inputImage = MakeImage("Name Input", card.transform, new Color32(247, 230, 237, 255), 0, 25, 630, 70);
        nameInput = inputImage.gameObject.AddComponent<TMP_InputField>();
        nameInput.targetGraphic = inputImage;
        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(UnityEngine.UI.RectMask2D)).GetComponent<RectTransform>();
        viewport.SetParent(inputImage.transform, false);
        viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(18, 8); viewport.offsetMax = new Vector2(-18, -8);
        var text = MakeLabel("", viewport, 0, 0, 0, 0, 30, new Color32(45, 35, 44, 255));
        Stretch(text.rectTransform);
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.richText = false;
        var placeholder = MakeLabel("Enter your name", viewport, 0, 0, 0, 0, 30, new Color32(132, 105, 119, 255));
        Stretch(placeholder.rectTransform);
        placeholder.alignment = TextAlignmentOptions.MidlineLeft;
        nameInput.textViewport = viewport;
        nameInput.textComponent = text;
        nameInput.placeholder = placeholder;
        nameInput.lineType = TMP_InputField.LineType.SingleLine;
        nameInput.contentType = TMP_InputField.ContentType.Standard;
        nameInput.characterLimit = NameLimit;
        nameInput.richText = false;
        nameInput.onValueChanged.AddListener(_ => { if (errorText != null) errorText.text = ""; });

        errorText = MakeLabel("", card.transform, 0, -45, 690, 55, 22, new Color32(154, 36, 69, 255));
        cancelButton = MakeButton("Cancel", card.transform, -165, -135, new Color32(237, 222, 229, 255), new Color32(45, 35, 44, 255));
        startButton = MakeButton("Start", card.transform, 165, -135, new Color32(198, 87, 127, 255), Color.white);
        cancelButton.onClick.AddListener(Cancel);
        startButton.onClick.AddListener(StartNewGame);
    }

    private UnityEngine.UI.Button MakeButton(string label, Transform parent, float x, float y, Color background, Color foreground)
    {
        var image = MakeImage(label, parent, background, x, y, 285, 60);
        var button = image.gameObject.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = image;
        var caption = MakeLabel(label, image.transform, 0, 0, 285, 60, 27, foreground);
        return button;
    }

    private UnityEngine.UI.Image MakeImage(string label, Transform parent, Color color, float x, float y, float width, float height)
    {
        var obj = new GameObject(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
        obj.transform.SetParent(parent, false);
        var image = obj.GetComponent<UnityEngine.UI.Image>();
        image.color = color;
        Place(image.rectTransform, x, y, width, height);
        return image;
    }

    private TMP_Text MakeLabel(string value, Transform parent, float x, float y, float width, float height, float size, Color color)
    {
        var obj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);
        var text = obj.GetComponent<TextMeshProUGUI>();
        text.font = font != null ? font : TMP_Settings.defaultFontAsset;
        text.text = value; text.fontSize = size; text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        Place(text.rectTransform, x, y, width, height);
        return text;
    }

    private static void Place(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        if (panelRoot != null) panelRoot.SetActive(false);
        RestoreLoadMenu();
        RestoreMainButtons();
        busy = false;
        NotifyIntroCancelled();
    }

    private void OnDestroy()
    {
        if (panelRoot != null) Destroy(panelRoot);
    }
}

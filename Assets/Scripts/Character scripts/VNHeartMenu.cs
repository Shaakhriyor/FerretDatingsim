using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class VNHeartMenu : MonoBehaviour
{
    [Serializable] public class SlotView
    {
        public UnityEngine.UI.Button button;
        public UnityEngine.UI.RawImage thumbnail;
        public TMP_Text number;
        public TMP_Text details;
        public TMP_Text emptyLabel;
    }
    public enum Page { Main, Save, Load }
    [SerializeField] private Canvas menuCanvas;
    [SerializeField] private CanvasGroup overlay;
    [SerializeField] private CanvasGroup content;
    [SerializeField] private RectTransform contentRect;
    [SerializeField] private GameObject mainPage;
    [SerializeField] private GameObject slotsPage;
    [SerializeField] private TMP_Text pageTitle;
    [SerializeField] private TMP_Text pageLabel;
    [SerializeField] private TMP_Text status;
    [SerializeField] private UnityEngine.UI.Button[] menuButtons;
    [SerializeField] private UnityEngine.UI.Button[] saveButtons;
    [SerializeField] private UnityEngine.UI.Button[] loadButtons;
    [SerializeField] private UnityEngine.UI.Button resumeButton;
    [SerializeField] private UnityEngine.UI.Button backButton;
    [SerializeField] private UnityEngine.UI.Button[] pageButtons;
    [SerializeField] private SlotView[] slots;
    [SerializeField] private GameObject confirmation;
    [SerializeField] private TMP_Text confirmationText;
    [SerializeField] private UnityEngine.UI.Button confirmButton;
    [SerializeField] private UnityEngine.UI.Button cancelButton;
    [SerializeField] private float transitionDuration = 0.24f;
    [Header("Main Menu Use")]
    [Tooltip("Enable ONLY on the menu copy in MAINMENU. Shows Load, hides the in-game toolbar, and returns directly to the main menu on Back/Esc.")]
    [SerializeField] private bool usedOnMainMenu;
    private readonly List<Texture2D> thumbnails = new List<Texture2D>();
    private bool open;
    public bool IsOpen => open;
    private bool busy;
    private bool pauseApplied;
    private float oldTimeScale;
    private bool oldAudioPause;
    private bool oldDialoguePause;
    private VNDialogueManager dialogue;
    private VNSaveData snapshot;
    private string thumbnailPng;
    private Page page;
    private int pageIndex;
    private Action confirmationAction;

    private void Awake()
    {
        if (menuCanvas == null || overlay == null || content == null)
        { Debug.LogError("Use Tools > VN > Build Heart Save Menu to configure this component.", this); enabled = false; return; }
        overlay.alpha = 0f;
        overlay.gameObject.SetActive(false);
        confirmation.SetActive(false);
        foreach (var button in menuButtons) button.onClick.AddListener(OpenMain);
        foreach (var button in saveButtons) button.onClick.AddListener(OpenSave);
        foreach (var button in loadButtons) button.onClick.AddListener(OpenLoad);
        resumeButton.onClick.AddListener(Close);
        backButton.onClick.AddListener(GoBack);
        cancelButton.onClick.AddListener(CancelConfirmation);
        confirmButton.onClick.AddListener(Confirm);
        for (int i = 0; i < slots.Length; i++)
        {
            int index = i;
            slots[i].button.onClick.AddListener(() => SelectSlot(index));
            var rightClick = slots[i].button.GetComponent<VNSaveSlotRightClick>();
            if (rightClick == null)
                rightClick = slots[i].button.gameObject.AddComponent<VNSaveSlotRightClick>();
            rightClick.Configure(this, index);
        }
        for (int i = 0; i < pageButtons.Length; i++) { int index = i; pageButtons[i].onClick.AddListener(() => ChangePage(index)); }
        if (usedOnMainMenu)
        {
            HideToolbar(menuButtons);
            HideToolbar(saveButtons);
            HideToolbar(loadButtons);
        }
    }

    private void HideToolbar(UnityEngine.UI.Button[] buttons)
    {
        foreach (var button in buttons)
            if (button != null && !button.transform.IsChildOf(overlay.transform)) button.gameObject.SetActive(false);
    }

    private void GoBack()
    {
        if (usedOnMainMenu) Close();
        else OpenMain();
    }

    private void Update()
    {
        if (VNSceneTransition.IsBusy) return;
        if (busy || Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
        if (!open) { if (!usedOnMainMenu) OpenMain(); }
        else if (confirmation.activeSelf) CancelConfirmation();
        else if (usedOnMainMenu) Close();
        else if (page != Page.Main) OpenMain();
        else Close();
    }

    public void OpenMain() { RequestPage(Page.Main); }
    public void OpenSave() { RequestPage(Page.Save); }
    public void OpenLoad() { RequestPage(Page.Load); }
    private void RequestPage(Page requested)
    {
        if (VNSceneTransition.IsBusy) return;
        if (usedOnMainMenu && requested != Page.Load) return;
        if (busy || !isActiveAndEnabled) return;
        if (open) StartCoroutine(ChangeView(requested));
        else StartCoroutine(OpenRoutine(requested));
    }

    private IEnumerator OpenRoutine(Page requested)
    {
        busy = true;
        open = true;
        SetToolbarInteractable(false);
        status.text = "";
        snapshot = null;
        thumbnailPng = "";
        dialogue = VNDialogueManager.Instance;
        try { if (!usedOnMainMenu) snapshot = VNSaveSystem.Capture(dialogue); }
        catch (Exception error) { status.text = error.Message; Debug.LogWarning(error.Message); }
        oldTimeScale = Time.timeScale;
        oldAudioPause = AudioListener.pause;
        oldDialoguePause = dialogue != null && dialogue.IsPaused;
        pauseApplied = true;
        if (dialogue != null) dialogue.SetPaused(true);
        Time.timeScale = 0f;
        AudioListener.pause = true;

        // Capture the story without any of this menu or its bottom toolbar.
        menuCanvas.enabled = false;
        yield return new WaitForEndOfFrame();
        try { thumbnailPng = CaptureThumbnail(); }
        catch (Exception error) { Debug.LogWarning("Save thumbnail unavailable: " + error.Message); }
        menuCanvas.enabled = true;
        overlay.gameObject.SetActive(true);
        overlay.blocksRaycasts = true;
        overlay.interactable = false;
        SetPage(requested);
        yield return AnimateOverlay(true);
        overlay.interactable = true;
        busy = false;
        SelectDefaultControl();
    }

    private IEnumerator ChangeView(Page requested)
    {
        busy = true;
        overlay.interactable = false;
        CancelConfirmation();
        yield return Fade(content, content.alpha, 0f, 0.08f);
        status.text = "";
        SetPage(requested);
        yield return Fade(content, 0f, 1f, 0.16f);
        overlay.interactable = true;
        busy = false;
        SelectDefaultControl();
    }

    private void SetPage(Page requested)
    {
        page = requested;
        mainPage.SetActive(page == Page.Main);
        slotsPage.SetActive(page != Page.Main);
        content.alpha = 1f;
        if (page != Page.Main) RefreshSlots();
    }

    private void ChangePage(int index)
    {
        if (busy || confirmation.activeSelf) return;
        pageIndex = Mathf.Clamp(index, 0, 9);
        RefreshSlots();
    }

    private void RefreshSlots()
    {
        ClearThumbnails();
        pageTitle.text = page == Page.Save ? "SAVE YOUR STORY" : "LOAD YOUR STORY";
        pageLabel.text = "PAGE " + (pageIndex + 1).ToString("D2") + " / 10";
        for (int i = 0; i < pageButtons.Length; i++) pageButtons[i].interactable = i != pageIndex;
        for (int i = 0; i < slots.Length; i++)
        {
            SlotView view = slots[i];
            int slot = pageIndex * 6 + i + 1;
            view.number.text = slot.ToString("D2");
            view.thumbnail.texture = null;
            view.thumbnail.gameObject.SetActive(false);
            view.emptyLabel.gameObject.SetActive(true);
            view.emptyLabel.text = "EMPTY SLOT";
            view.details.text = page == Page.Save ? "Save here" : "No saved story";
            view.button.interactable = page == Page.Save && snapshot != null;
            try
            {
                var data = VNSaveSystem.Read(slot);
                if (data == null) continue;
                view.button.interactable = page == Page.Load || snapshot != null;
                view.emptyLabel.text = data.sceneName;
                string date = DateTime.TryParse(data.savedUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out var utc)
                    ? utc.ToLocalTime().ToString("dd MMM yyyy  HH:mm") : "Saved game";
                view.details.text = date + "  /  " + data.sceneName + (data.hasKarma ? "  /  Karma " + data.karma : "");
                if (!string.IsNullOrEmpty(data.thumbnailPng))
                {
                    try
                    {
                        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                        thumbnails.Add(texture);
                        if (texture.LoadImage(Convert.FromBase64String(data.thumbnailPng)))
                        {
                            view.thumbnail.texture = texture; view.thumbnail.gameObject.SetActive(true);
                            view.emptyLabel.gameObject.SetActive(false);
                        }
                    }
                    catch (Exception error) { Debug.LogWarning("Thumbnail unavailable: " + error.Message); }
                }
            }
            catch (Exception error)
            {
                view.emptyLabel.gameObject.SetActive(true);
                view.emptyLabel.text = "UNREADABLE SAVE";
                view.details.text = "This slot could not be read";
                view.button.interactable = page == Page.Save && snapshot != null;
                Debug.LogWarning("Slot " + slot + ": " + error.Message);
            }
        }
    }

    private void SelectSlot(int index)
    {
        if (busy || confirmation.activeSelf) return;
        int slot = pageIndex * 6 + index + 1;
        try
        {
            if (page == Page.Save)
            {
                if (VNSaveSystem.Exists(slot)) Ask("Overwrite slot " + slot.ToString("D2") + "?", () => Save(slot));
                else Save(slot);
            }
            else
            {
                var data = VNSaveSystem.Read(slot);
                if (data != null) Ask("Load slot " + slot.ToString("D2") + "?\nUnsaved progress will be replaced.", () => Load(data));
            }
        }
        catch (Exception error) { status.text = error.Message; }
    }

    private void Save(int slot)
    {
        try { VNSaveSystem.Write(slot, snapshot, thumbnailPng); RefreshSlots(); status.text = "Saved to slot " + slot.ToString("D2") + "."; }
        catch (Exception error) { status.text = "Could not save: " + error.Message; }
    }

    private void Load(VNSaveData data)
    {
        try
        {
            if (VNSaveSystem.Load(data)) { busy = true; overlay.interactable = false; status.text = "Loading..."; }
            else Close();
        }
        catch (Exception error)
        {
            status.text = "Could not load: " + error.Message;
            Debug.LogError("SAVE LOAD FAILED: " + error, this);
        }
    }

    public void RequestDeleteSlot(int index)
    {
        if (!isActiveAndEnabled || !open || busy || page == Page.Main ||
            !slotsPage.activeInHierarchy || !overlay.interactable ||
            confirmation.activeSelf || index < 0 || index >= slots.Length)
            return;

        // Capture the absolute slot now; do not recalculate it on confirmation.
        int slot = pageIndex * 6 + index + 1;
        try
        {
            // No deserialization: even outdated or unreadable saves can be deleted.
            if (!VNSaveSystem.Exists(slot)) return;
            Ask("Delete slot " + slot.ToString("D2") + "?\nThis cannot be undone.",
                () => DeleteSlot(slot));
        }
        catch (Exception error)
        {
            status.text = "Could not delete: " + error.Message;
            Debug.LogError("SAVE DELETE FAILED: " + error, this);
        }
    }

    private void DeleteSlot(int slot)
    {
        try
        {
            VNSaveSlotFiles.Delete(VNSaveSystem.SaveDirectory, slot, VNSaveSystem.SlotCount);
            RefreshSlots();
            status.text = "Deleted slot " + slot.ToString("D2") + ".";
            SelectControl(backButton);
        }
        catch (Exception error)
        {
            status.text = "Could not delete: " + error.Message;
            Debug.LogError("SAVE DELETE FAILED: " + error, this);
        }
    }

    private void Ask(string message, Action action)
    {
        confirmationAction = action;
        confirmationText.text = message;
        SetPageInteractable(false);
        confirmation.SetActive(true);
        SelectControl(cancelButton);
    }
    private void Confirm()
    {
        Action action = confirmationAction;
        CancelConfirmation();
        action?.Invoke();
    }
    private void CancelConfirmation()
    {
        confirmationAction = null;
        if (confirmation != null) confirmation.SetActive(false);
        SetPageInteractable(true);
    }

    public void Close()
    {
        if (!open || busy) return;
        StartCoroutine(CloseRoutine());
    }
    private IEnumerator CloseRoutine()
    {
        busy = true;
        overlay.interactable = false;
        CancelConfirmation();
        yield return AnimateOverlay(false);
        overlay.blocksRaycasts = false;
        overlay.gameObject.SetActive(false);
        ClearThumbnails();
        ReleasePause();
        open = false;
        busy = false;
        SetToolbarInteractable(true);
        if (UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
    }

    private IEnumerator AnimateOverlay(bool opening)
    {
        float elapsed = 0f;
        float from = overlay.alpha;
        float duration = Mathf.Max(0.01f, transitionDuration);
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);
            overlay.alpha = Mathf.Lerp(from, opening ? 1f : 0f, t);
            float scale = Mathf.Lerp(opening ? 0.98f : 1f, opening ? 1f : 0.98f, t);
            contentRect.localScale = Vector3.one * scale;
            yield return null;
        }
        overlay.alpha = opening ? 1f : 0f;
        contentRect.localScale = Vector3.one;
    }
    private static IEnumerator Fade(CanvasGroup group, float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        group.alpha = to;
    }

    private static string CaptureThumbnail()
    {
        Texture2D full = null;
        Texture2D small = null;
        RenderTexture target = null;
        RenderTexture previous = RenderTexture.active;
        try
        {
            full = ScreenCapture.CaptureScreenshotAsTexture();
            target = RenderTexture.GetTemporary(384, 216, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(full, target);
            RenderTexture.active = target;
            small = new Texture2D(384, 216, TextureFormat.RGB24, false);
            small.ReadPixels(new Rect(0, 0, 384, 216), 0, 0);
            small.Apply();
            return Convert.ToBase64String(small.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous;
            if (target != null) RenderTexture.ReleaseTemporary(target);
            if (full != null) UnityEngine.Object.Destroy(full);
            if (small != null) UnityEngine.Object.Destroy(small);
        }
    }
    private void ClearThumbnails()
    {
        foreach (var texture in thumbnails) if (texture != null) Destroy(texture);
        thumbnails.Clear();
    }
    private void SelectDefaultControl() { SelectControl(page == Page.Main ? resumeButton : backButton); }
    private void SetPageInteractable(bool value)
    {
        if (mainPage != null && mainPage.TryGetComponent<CanvasGroup>(out var mainGroup)) mainGroup.interactable = value;
        if (slotsPage != null && slotsPage.TryGetComponent<CanvasGroup>(out var slotGroup)) slotGroup.interactable = value;
    }
    private void SetToolbarInteractable(bool value)
    {
        // Only the bottom toolbar sits outside the overlay; keep keyboard navigation inside the open menu.
        foreach (var button in menuButtons) if (!button.transform.IsChildOf(overlay.transform)) button.interactable = value;
        foreach (var button in saveButtons) if (!button.transform.IsChildOf(overlay.transform)) button.interactable = value;
        foreach (var button in loadButtons) if (!button.transform.IsChildOf(overlay.transform)) button.interactable = value;
    }
    private static void SelectControl(UnityEngine.UI.Button button)
    {
        if (button != null && UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(button.gameObject);
    }
    private void ReleasePause()
    {
        if (!pauseApplied) return;
        Time.timeScale = oldTimeScale;
        AudioListener.pause = oldAudioPause;
        if (dialogue != null) dialogue.SetPaused(oldDialoguePause);
        pauseApplied = false;
    }
    private void OnDisable()
    {
        StopAllCoroutines();
        if (menuCanvas != null) menuCanvas.enabled = true;
        if (overlay != null) { overlay.alpha = 0; overlay.gameObject.SetActive(false); }
        open = false; busy = false;
        ClearThumbnails();
        ReleasePause();
    }
}

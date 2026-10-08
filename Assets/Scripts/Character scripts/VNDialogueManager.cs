using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class VNDialogueManager : MonoBehaviour
{
    public static VNDialogueManager Instance { get; private set; }

    [Header("Dialogue UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private GameObject continueIndicator;

    [Header("Scene Picture")]
    [Tooltip("Assign the full-scene UI Image behind the dialogue box.")]
    [SerializeField] private UnityEngine.UI.Image scenePictureImage;

    [Header("Narration")]
    [SerializeField] private TMP_FontAsset narratorFont;
    [SerializeField] private Color narratorColor = Color.white;

    [Header("Choice UI")]
    [SerializeField] private GameObject choicePanel;
    [SerializeField] private Transform choiceButtonContainer;
    [SerializeField] private Button choiceButtonPrefab;

    [Header("Typewriter")]
    [SerializeField] private float typingSpeed = 0.03f;
    [SerializeField] private float punctuationPause = 0.06f;

    [Header("Typing Bubble Sound")]
    [SerializeField] private AudioClip letterBubbleSound;

    [Range(0f, 1f)]
    [SerializeField] private float letterBubbleVolume = 0.4f;

    [Header("Audio")]
    [SerializeField] private AudioSource typingAudioSource;
    [SerializeField] private AudioSource voiceAudioSource;
    [SerializeField] private AudioSource soundEffectAudioSource;

    [SerializeField] private AudioClip choiceSelectSound;

    [Header("Testing")]
    [SerializeField] private VNStoryNode startingNode;
    [SerializeField] private bool playStartingNodeOnStart = true;

    [Header("Next Scene")]
    [Tooltip("When the whole dialogue chain ends, continue to this Unity scene. Leave empty to stay here. Example: Prologue_Doctor")]
    [SerializeField] private string nextSceneName;
    [Min(0f)]
    [Tooltip("Seconds for each half of the transition: fade to black, then fade into the next scene.")]
    [SerializeField] private float sceneFadeDuration = 0.75f;

    private readonly Dictionary<string, VNDialogueSpeaker> speakers =
        new Dictionary<string, VNDialogueSpeaker>(
            StringComparer.OrdinalIgnoreCase);

    private VNStoryNode currentNode;
    private int currentLineIndex;

    private Coroutine typingCoroutine;

    private bool isTyping;
    private bool choicesAreShowing;
    private bool dialogueIsRunning;

    private string currentFullText = "";
    private bool stopCurrentLineSoundEffect;

    private float pictureFadeSecondsRemaining;

    public bool IsPaused { get; private set; }
    private int resumeInputBlockedThroughFrame = -1;
    private readonly List<UnityEngine.EventSystems.RaycastResult> uiHits =
        new List<UnityEngine.EventSystems.RaycastResult>();

    public void SetPaused(bool paused)
    {
        IsPaused = paused;
        if (!paused)
            resumeInputBlockedThroughFrame = Time.frameCount + 1;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        SetupAudioSources();
        RegisterSpeakers();

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        if (choicePanel != null)
            choicePanel.SetActive(false);

        if (continueIndicator != null)
            continueIndicator.SetActive(false);
    }

    private IEnumerator Start()
    {
        if (VNSaveSystem.TryRestorePending(this)) yield break;
        while (VNSceneTransition.IsBusy) yield return null;

        if (playStartingNodeOnStart &&
            startingNode != null)
        {
            StartDialogue(startingNode);
        }
    }

    private void Update()
    {
        if (VNSceneTransition.IsBusy) return;
        UpdatePictureFade();

        if (IsPaused || Time.frameCount <= resumeInputBlockedThroughFrame || !dialogueIsRunning)
            return;

        if (choicesAreShowing)
            return;

        if (AdvancePressed())
        {
            Advance();
        }
    }

    // =========================================================
    // START / END
    // =========================================================

    public void StartDialogue(VNStoryNode node)
    {
        if (node == null)
            return;

        StopCurrentLineAudio();

        RegisterSpeakers();

        StopTyping();

        currentNode = node;
        currentLineIndex = 0;

        dialogueIsRunning = true;
        choicesAreShowing = false;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        HideChoices();

        if (currentNode.lines == null ||
            currentNode.lines.Count == 0)
        {
            ResolveEndOfNode();
            return;
        }

        ShowCurrentLine();
    }

    public void EndDialogue()
    {
        StopCurrentLineAudio();
        StopTyping();
        HideChoices();

        currentNode = null;
        currentLineIndex = 0;

        dialogueIsRunning = false;
        choicesAreShowing = false;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        if (continueIndicator != null)
            continueIndicator.SetActive(false);
    }

    // =========================================================
    // LINES
    // =========================================================

    private void Advance()
    {
        if (isTyping)
        {
            FinishCurrentLineImmediately();
            return;
        }

        StopCurrentLineAudio();
        currentLineIndex++;

        if (currentNode == null)
        {
            EndDialogue();
            return;
        }

        if (currentLineIndex >= currentNode.lines.Count)
        {
            ResolveEndOfNode();
            return;
        }

        ShowCurrentLine();
    }

    private void ShowCurrentLine()
    {
        VNStoryNode.DialogueLine line =
            currentNode.lines[currentLineIndex];

        if (line.endDoctorIntro)
        {
            foreach (var intro in FindObjectsByType<VNDoctorIntro>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (intro.isActiveAndEnabled) intro.FadeOutNow();
        }

        // Empty picture fields keep the picture from the previous line.
        if (line.scenePicture != null && scenePictureImage != null)
        {
            pictureFadeSecondsRemaining = 0f;
            scenePictureImage.sprite = line.scenePicture;
            scenePictureImage.color = Color.white;
        }

        if (line.fadeToBlack)
            BeginPictureFade(line.fadeDuration);

        ConfigureSpeaker(line);

        PlayLineAudio(line);

        currentFullText = VNSaveSystem.Format(line.text);

        typingCoroutine =
            StartCoroutine(
                TypeCurrentLine(currentFullText));
    }

    private void BeginPictureFade(float seconds)
    {
        if (scenePictureImage == null)
        {
            Debug.LogWarning("Assign Scene Picture Image on VNDialogueManager to use Fade To Black.", this);
            return;
        }

        pictureFadeSecondsRemaining = Mathf.Max(0f, seconds);
        if (pictureFadeSecondsRemaining == 0f)
            scenePictureImage.color = Color.black;
    }

    private void UpdatePictureFade()
    {
        if (IsPaused || pictureFadeSecondsRemaining <= 0f)
            return;

        if (scenePictureImage == null)
        {
            pictureFadeSecondsRemaining = 0f;
            return;
        }

        // Interpolate from the current shade using the remaining time. This also
        // resumes correctly when a save is restored halfway through the fade.
        float step = Mathf.Min(Time.deltaTime, pictureFadeSecondsRemaining);
        if (step <= 0f) return;
        scenePictureImage.color = Color.Lerp(
            scenePictureImage.color, Color.black, step / pictureFadeSecondsRemaining);
        pictureFadeSecondsRemaining -= step;

        if (pictureFadeSecondsRemaining <= 0f)
            scenePictureImage.color = Color.black;
    }

    // =========================================================
    // SPEAKERS
    // =========================================================

    private void ConfigureSpeaker(
        VNStoryNode.DialogueLine line, bool applyCharacter = true)
    {
        if (string.IsNullOrWhiteSpace(line.speakerId))
        {
            ShowNarrationStyle();
            return;
        }

        VNDialogueSpeaker speaker =
            GetSpeaker(line.speakerId);

        if (speaker == null)
        {
            Debug.LogWarning(
                $"Speaker '{line.speakerId}' was not found.");

            return;
        }

        if (speakerNameText != null)
        {
            speakerNameText.gameObject.SetActive(true);
            speakerNameText.text = string.Equals(line.speakerId, "MC", StringComparison.OrdinalIgnoreCase) ? VNSaveSystem.Format("{playerName}") : speaker.DisplayName;
            speakerNameText.color = speaker.NameColor;

            if (speaker.NameFont != null)
                speakerNameText.font = speaker.NameFont;
        }

        if (dialogueText != null)
        {
            dialogueText.color = speaker.DialogueColor;

            if (speaker.DialogueFont != null)
                dialogueText.font = speaker.DialogueFont;
        }

        if (applyCharacter && !string.IsNullOrWhiteSpace(line.expression))
        {
            speaker.ApplyExpression(line.expression);
        }

        if (applyCharacter && !string.IsNullOrWhiteSpace(line.pose))
        {
            speaker.ApplyPose(line.pose);
        }
    }

    private void ShowNarrationStyle()
    {
        if (speakerNameText != null)
        {
            speakerNameText.gameObject.SetActive(false);
        }

        if (dialogueText != null)
        {
            dialogueText.color = narratorColor;

            if (narratorFont != null)
                dialogueText.font = narratorFont;
        }
    }

    // =========================================================
    // TYPEWRITER
    // =========================================================

    private IEnumerator TypeCurrentLine(string fullText, int startVisible = 0)
    {
        isTyping = true;

        if (continueIndicator != null)
            continueIndicator.SetActive(false);

        dialogueText.text = fullText;
        dialogueText.maxVisibleCharacters = startVisible;

        dialogueText.ForceMeshUpdate();

        int characterCount =
            dialogueText.textInfo.characterCount;

        for (int i = Mathf.Clamp(startVisible, 0, characterCount); i < characterCount; i++)
        {
            while (IsPaused)
                yield return null;

            dialogueText.maxVisibleCharacters = i + 1;

            char character =
                dialogueText.textInfo
                    .characterInfo[i]
                    .character;

            PlayLetterSound(character);

            float delay =
                Mathf.Max(0f, typingSpeed);

            if (IsPunctuation(character))
            {
                delay +=
                    Mathf.Max(0f, punctuationPause);
            }

            if (delay > 0f)
            {
                float elapsed = 0f;
                while (elapsed < delay)
                {
                    yield return null;
                    if (!IsPaused)
                        elapsed += Time.unscaledDeltaTime;
                }
            }
            else
            {
                yield return null;
            }
        }

        while (IsPaused)
            yield return null;

        dialogueText.maxVisibleCharacters =
            int.MaxValue;

        isTyping = false;
        typingCoroutine = null;

        if (continueIndicator != null)
            continueIndicator.SetActive(true);
    }

    private void PlayLetterSound(char character)
    {
        if (letterBubbleSound == null ||
            typingAudioSource == null)
        {
            return;
        }

        if (!char.IsLetterOrDigit(character))
            return;

        typingAudioSource.PlayOneShot(
            letterBubbleSound,
            letterBubbleVolume);
    }

    private bool IsPunctuation(char character)
    {
        return character == '.' ||
               character == ',' ||
               character == '!' ||
               character == '?' ||
               character == ';' ||
               character == ':';
    }

    private void FinishCurrentLineImmediately()
    {
        StopTyping();

        dialogueText.text =
            currentFullText;

        dialogueText.maxVisibleCharacters =
            int.MaxValue;

        if (continueIndicator != null)
            continueIndicator.SetActive(true);
    }

    private void StopTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        isTyping = false;
    }

    // =========================================================
    // AUDIO
    // =========================================================

    private void PlayLineAudio(
        VNStoryNode.DialogueLine line)
    {
        StopCurrentLineAudio();
        stopCurrentLineSoundEffect = line.soundEffect != null && line.stopSoundEffectOnAdvance;

        if (!string.IsNullOrWhiteSpace(
            line.voiceCue))
        {
            VNDialogueSpeaker speaker =
                GetSpeaker(line.speakerId);

            if (speaker != null)
            {
                AudioClip clip =
                    speaker.GetVoiceCue(
                        line.voiceCue);

                if (clip != null)
                {
                    voiceAudioSource.PlayOneShot(
                        clip, Mathf.Clamp01(line.voiceCueVolume));
                }
            }
        }

        if (line.soundEffect != null)
        {
            soundEffectAudioSource.PlayOneShot(
                line.soundEffect, Mathf.Clamp01(line.soundEffectVolume));
        }
    }

    private void StopCurrentLineAudio()
    {
        if (voiceAudioSource != null)
            voiceAudioSource.Stop();

        if (stopCurrentLineSoundEffect && soundEffectAudioSource != null)
            soundEffectAudioSource.Stop();

        stopCurrentLineSoundEffect = false;
    }

    private void OnDisable()
    {
        StopCurrentLineAudio();
    }

    // =========================================================
    // NODE / CHOICES
    // =========================================================

    private void ResolveEndOfNode()
    {
        if (currentNode == null)
        {
            EndDialogue();
            return;
        }

        if (currentNode.choices != null &&
            currentNode.choices.Count > 0)
        {
            DisplayChoices();
            return;
        }

        if (currentNode.nextNode != null)
        {
            StartDialogue(currentNode.nextNode);
            return;
        }

        FinishStory();
    }

    private void FinishStory()
    {
        if (string.IsNullOrWhiteSpace(nextSceneName))
        {
            EndDialogue();
            return;
        }
        if (VNSceneTransition.TryBegin(nextSceneName, sceneFadeDuration))
            EndDialogue();
        else if (currentNode != null && currentNode.lines != null && currentNode.lines.Count > 0)
            currentLineIndex = Mathf.Clamp(currentLineIndex, 0, currentNode.lines.Count - 1);
    }

    private void DisplayChoices()
    {
        if (choicePanel == null ||
            choiceButtonContainer == null ||
            choiceButtonPrefab == null)
        {
            Debug.LogError(
                "Choice UI is not completely assigned.");

            return;
        }

        choicesAreShowing = true;

        ClearChoiceButtons();

        choicePanel.SetActive(true);

        if (continueIndicator != null)
            continueIndicator.SetActive(false);

        // Put the prompt in the NORMAL dialogue box.
        if (dialogueText != null)
        {
            dialogueText.text =
                VNSaveSystem.Format(currentNode.choicePrompt);

            dialogueText.maxVisibleCharacters =
                int.MaxValue;
        }

        if (speakerNameText != null)
        {
            speakerNameText.gameObject.SetActive(false);
        }

        foreach (VNStoryNode.Choice choice in
                 currentNode.choices)
        {
            VNStoryNode.Choice capturedChoice =
                choice;

            Button button =
                Instantiate(
                    choiceButtonPrefab,
                    choiceButtonContainer);

            TMP_Text buttonText =
                button.GetComponentInChildren<TMP_Text>(
                    true);

            if (buttonText != null)
            {
                buttonText.text =
                    VNSaveSystem.Format(capturedChoice.choiceText);
            }

            button.onClick.RemoveAllListeners();

            button.onClick.AddListener(
                () => SelectChoice(capturedChoice));
        }
    }

    private void SelectChoice(
        VNStoryNode.Choice choice)
    {
        if (IsPaused || !choicesAreShowing || choice == null)
            return;

        VNSaveSystem.RecordChoice(currentNode, currentNode.choices.IndexOf(choice));

        if (choiceSelectSound != null)
        {
            soundEffectAudioSource.PlayOneShot(
                choiceSelectSound);
        }

        if (KarmaManager.Instance != null)
        {
            KarmaManager.Instance.AdjustKarma(
                choice.karmaChange);
        }

        HideChoices();

        if (choice.nextNode != null)
        {
            StartDialogue(choice.nextNode);
        }
        else
        {
            FinishStory();
        }
    }

    private void HideChoices()
    {
        choicesAreShowing = false;

        ClearChoiceButtons();

        if (choicePanel != null)
            choicePanel.SetActive(false);
    }

    private void ClearChoiceButtons()
    {
        if (choiceButtonContainer == null)
            return;

        for (int i =
             choiceButtonContainer.childCount - 1;
             i >= 0;
             i--)
        {
            Destroy(
                choiceButtonContainer
                    .GetChild(i)
                    .gameObject);
        }
    }

    // =========================================================
    // SPEAKER REGISTRY
    // =========================================================

    private void RegisterSpeakers()
    {
        speakers.Clear();

        VNDialogueSpeaker[] found =
            FindObjectsByType<VNDialogueSpeaker>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

        foreach (VNDialogueSpeaker speaker in found)
        {
            if (speaker == null)
                continue;

            if (string.IsNullOrWhiteSpace(
                speaker.SpeakerId))
            {
                continue;
            }

            speakers[speaker.SpeakerId] =
                speaker;
        }
    }

    private VNDialogueSpeaker GetSpeaker(
        string speakerId)
    {
        if (string.IsNullOrWhiteSpace(
            speakerId))
        {
            return null;
        }

        speakers.TryGetValue(
            speakerId,
            out VNDialogueSpeaker speaker);

        return speaker;
    }

    // =========================================================
    // INPUT
    // =========================================================

    private bool PointerIsOverControl()
    {
        UnityEngine.EventSystems.EventSystem events = UnityEngine.EventSystems.EventSystem.current;
        if (events == null || Mouse.current == null)
            return false;

        // Use this frame's pointer position, including on a button's first click.
        var pointer = new UnityEngine.EventSystems.PointerEventData(events);
        pointer.position = Mouse.current.position.ReadValue();
        uiHits.Clear();
        events.RaycastAll(pointer, uiHits);
        foreach (var hit in uiHits)
        {
            if (hit.gameObject != null &&
                hit.gameObject.GetComponentInParent<UnityEngine.UI.Selectable>() != null)
                return true;
        }
        return false;
    }

    private bool AdvancePressed()
    {
        bool mouse =
            Mouse.current != null &&
            Mouse.current.leftButton
                .wasPressedThisFrame &&
            !PointerIsOverControl();

        bool space =
            Keyboard.current != null &&
            Keyboard.current.spaceKey
                .wasPressedThisFrame;

        bool enter =
            Keyboard.current != null &&
            (
                Keyboard.current.enterKey
                    .wasPressedThisFrame ||
                Keyboard.current.numpadEnterKey
                    .wasPressedThisFrame
            );

        bool controller =
            Gamepad.current != null &&
            Gamepad.current.buttonSouth
                .wasPressedThisFrame;

        return mouse ||
               space ||
               enter ||
               controller;
    }

    // =========================================================
    // AUDIO SOURCES
    // =========================================================

    public VNSaveData CaptureSave(VNSaveCatalog catalog)
    {
        return new VNSaveData {
            nodeId = catalog.Id(currentNode), nodeRevision = VNSaveSystem.NodeRevision(currentNode, catalog), lineIndex = currentLineIndex,
            running = dialogueIsRunning, choicesShowing = choicesAreShowing,
            typing = isTyping, visibleCharacters = dialogueText != null ? dialogueText.maxVisibleCharacters : 0,
            previewText = dialogueText != null && dialogueIsRunning ? dialogueText.text : "Between conversations",
            pictureId = scenePictureImage != null ? catalog.Id(scenePictureImage.sprite) : "",
            pictureColor = scenePictureImage != null ? scenePictureImage.color : Color.white,
            pictureEnabled = scenePictureImage != null && scenePictureImage.enabled,
            pictureFadeSecondsRemaining = scenePictureImage != null ? pictureFadeSecondsRemaining : 0f
        };
    }

    public void RestoreSave(VNSaveData data, VNSaveCatalog catalog)
    {
        StopCurrentLineAudio();
        StopTyping();
        HideChoices();
        if (typingAudioSource != null) typingAudioSource.Stop();
        if (voiceAudioSource != null) voiceAudioSource.Stop();
        if (soundEffectAudioSource != null) soundEffectAudioSource.Stop();
        RegisterSpeakers();
        currentNode = catalog.Resolve<VNStoryNode>(data.nodeId);
        currentLineIndex = data.lineIndex;
        dialogueIsRunning = data.running;
        pictureFadeSecondsRemaining = Mathf.Max(0f, data.pictureFadeSecondsRemaining);
        if (scenePictureImage != null)
        {
            scenePictureImage.sprite = catalog.Resolve<Sprite>(data.pictureId);
            scenePictureImage.color = data.pictureColor;
            scenePictureImage.enabled = data.pictureEnabled;
        }
        if (dialoguePanel != null) dialoguePanel.SetActive(dialogueIsRunning);
        if (continueIndicator != null) continueIndicator.SetActive(false);
        resumeInputBlockedThroughFrame = Time.frameCount + 1;
        if (!dialogueIsRunning) return;
        if (data.choicesShowing)
        {
            ShowNarrationStyle();
            DisplayChoices();
            return;
        }
        var line = currentNode.lines[currentLineIndex];
        ConfigureSpeaker(line, false);
        currentFullText = VNSaveSystem.Format(line.text);
        if (dialogueText != null)
        {
            dialogueText.text = currentFullText;
            dialogueText.ForceMeshUpdate();
            int visible = Mathf.Clamp(data.visibleCharacters, 0, dialogueText.textInfo.characterCount);
            dialogueText.maxVisibleCharacters = data.typing ? visible : int.MaxValue;
            if (data.typing) typingCoroutine = StartCoroutine(TypeCurrentLine(currentFullText, visible));
            else if (continueIndicator != null) continueIndicator.SetActive(true);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void SetupAudioSources()
    {
        if (typingAudioSource == null)
        {
            typingAudioSource =
                gameObject.AddComponent<AudioSource>();

            ConfigureAudioSource(
                typingAudioSource);
        }

        if (voiceAudioSource == null)
        {
            voiceAudioSource =
                gameObject.AddComponent<AudioSource>();

            ConfigureAudioSource(
                voiceAudioSource);
        }

        if (soundEffectAudioSource == null)
        {
            soundEffectAudioSource =
                gameObject.AddComponent<AudioSource>();

            ConfigureAudioSource(
                soundEffectAudioSource);
        }
    }

    private void ConfigureAudioSource(
        AudioSource source)
    {
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
    }
}

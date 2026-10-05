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

    [Header("Narration")]
    [Tooltip("Font used when Speaker ID is left blank.")]
    [SerializeField] private TMP_FontAsset narratorFont;

    [SerializeField] private Color narratorColor = Color.white;

    [Header("Choice UI")]
    [SerializeField] private GameObject choicePanel;
    [SerializeField] private TMP_Text choicePromptText;
    [SerializeField] private Transform choiceButtonContainer;
    [SerializeField] private Button choiceButtonPrefab;

    [Header("Typewriter")]
    [Tooltip("Seconds between letters. 0.03 is a good starting point.")]
    [SerializeField] private float typingSpeed = 0.03f;

    [Tooltip("Extra pause after punctuation.")]
    [SerializeField] private float punctuationPause = 0.06f;

    [Header("Audio")]
    [SerializeField] private AudioSource voiceAudioSource;
    [SerializeField] private AudioSource soundEffectAudioSource;

    [Tooltip("Optional sound when selecting a choice.")]
    [SerializeField] private AudioClip choiceSelectSound;

    [Header("Testing / Starting Story")]
    [SerializeField] private VNStoryNode startingNode;
    [SerializeField] private bool playStartingNodeOnStart = true;

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

    public bool DialogueIsRunning => dialogueIsRunning;
    public bool IsTyping => isTyping;

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

    private void Start()
    {
        if (playStartingNodeOnStart &&
            startingNode != null)
        {
            StartDialogue(startingNode);
        }
    }

    private void Update()
    {
        if (!dialogueIsRunning)
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
        {
            Debug.LogWarning(
                "Tried to start a null VNStoryNode.",
                this);

            return;
        }

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
        StopTyping();
        HideChoices();

        currentNode = null;
        currentLineIndex = 0;

        dialogueIsRunning = false;
        choicesAreShowing = false;

        currentFullText = "";

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        if (continueIndicator != null)
            continueIndicator.SetActive(false);
    }

    // =========================================================
    // LINE ADVANCEMENT
    // =========================================================

    private void Advance()
    {
        if (isTyping)
        {
            FinishCurrentLineImmediately();
            return;
        }

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
        if (currentNode == null)
            return;

        if (currentLineIndex < 0 ||
            currentLineIndex >= currentNode.lines.Count)
        {
            ResolveEndOfNode();
            return;
        }

        StopTyping();

        VNStoryNode.DialogueLine line =
            currentNode.lines[currentLineIndex];

        ConfigureSpeaker(line);

        PlayLineAudio(line);

        currentFullText =
            line.text ?? "";

        typingCoroutine =
            StartCoroutine(
                TypeCurrentLine(currentFullText));
    }

    // =========================================================
    // SPEAKER / CHARACTER
    // =========================================================

    private void ConfigureSpeaker(
        VNStoryNode.DialogueLine line)
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
                $"Could not find speaker ID '{line.speakerId}'.",
                currentNode);

            if (speakerNameText != null)
            {
                speakerNameText.gameObject.SetActive(true);
                speakerNameText.text = line.speakerId;
            }

            return;
        }

        if (speakerNameText != null)
        {
            speakerNameText.gameObject.SetActive(true);
            speakerNameText.text = speaker.DisplayName;
            speakerNameText.color = speaker.NameColor;

            if (speaker.NameFont != null)
            {
                speakerNameText.font =
                    speaker.NameFont;
            }
        }

        if (dialogueText != null)
        {
            dialogueText.color =
                speaker.DialogueColor;

            if (speaker.DialogueFont != null)
            {
                dialogueText.font =
                    speaker.DialogueFont;
            }
        }

        // Blank means "do not change it".
        if (!string.IsNullOrWhiteSpace(line.expression))
        {
            speaker.ApplyExpression(
                line.expression);
        }

        if (!string.IsNullOrWhiteSpace(line.pose))
        {
            speaker.ApplyPose(
                line.pose);
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
            dialogueText.color =
                narratorColor;

            if (narratorFont != null)
            {
                dialogueText.font =
                    narratorFont;
            }
        }
    }

    // =========================================================
    // TYPEWRITER
    // =========================================================

    private IEnumerator TypeCurrentLine(
        string fullText)
    {
        isTyping = true;

        if (continueIndicator != null)
            continueIndicator.SetActive(false);

        dialogueText.text = fullText;

        dialogueText.maxVisibleCharacters = 0;

        dialogueText.ForceMeshUpdate();

        int characterCount =
            dialogueText.textInfo.characterCount;

        for (int i = 0; i < characterCount; i++)
        {
            dialogueText.maxVisibleCharacters =
                i + 1;

            char character =
                dialogueText.textInfo
                    .characterInfo[i]
                    .character;

            float delay =
                Mathf.Max(0f, typingSpeed);

            if (IsPunctuation(character))
            {
                delay +=
                    Mathf.Max(
                        0f,
                        punctuationPause);
            }

            if (delay > 0f)
            {
                yield return
                    new WaitForSecondsRealtime(delay);
            }
            else
            {
                yield return null;
            }
        }

        dialogueText.maxVisibleCharacters =
            int.MaxValue;

        isTyping = false;
        typingCoroutine = null;

        if (continueIndicator != null)
            continueIndicator.SetActive(true);
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
        if (!isTyping)
            return;

        StopTyping();

        dialogueText.text =
            currentFullText;

        dialogueText.maxVisibleCharacters =
            int.MaxValue;

        isTyping = false;

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
        // Voice cue only plays if YOU entered one.
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

                if (clip != null &&
                    voiceAudioSource != null)
                {
                    voiceAudioSource.Stop();
                    voiceAudioSource.PlayOneShot(
                        clip);
                }
            }
        }

        // Completely separate from character voice.
        if (line.soundEffect != null &&
            soundEffectAudioSource != null)
        {
            soundEffectAudioSource.PlayOneShot(
                line.soundEffect);
        }
    }

    // =========================================================
    // NODE END / CHOICES
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
            StartDialogue(
                currentNode.nextNode);

            return;
        }

        EndDialogue();
    }

    private void DisplayChoices()
    {
        if (choicePanel == null ||
            choiceButtonContainer == null ||
            choiceButtonPrefab == null)
        {
            Debug.LogError(
                "Choice UI is not fully assigned on VNDialogueManager.",
                this);

            return;
        }

        choicesAreShowing = true;

        if (continueIndicator != null)
            continueIndicator.SetActive(false);

        ClearChoiceButtons();

        choicePanel.SetActive(true);

        if (choicePromptText != null)
        {
            bool hasPrompt =
                !string.IsNullOrWhiteSpace(
                    currentNode.choicePrompt);

            choicePromptText.gameObject.SetActive(
                hasPrompt);

            if (hasPrompt)
            {
                choicePromptText.text =
                    currentNode.choicePrompt;
            }
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
                    capturedChoice.choiceText;
            }

            button.onClick.RemoveAllListeners();

            button.onClick.AddListener(
                () =>
                {
                    SelectChoice(
                        capturedChoice);
                });
        }
    }

    private void SelectChoice(
        VNStoryNode.Choice choice)
    {
        if (choiceSelectSound != null &&
            soundEffectAudioSource != null)
        {
            soundEffectAudioSource.PlayOneShot(
                choiceSelectSound);
        }

        HideChoices();

        if (choice.nextNode != null)
        {
            StartDialogue(
                choice.nextNode);
        }
        else
        {
            EndDialogue();
        }
    }

    private void HideChoices()
    {
        choicesAreShowing = false;

        ClearChoiceButtons();

        if (choicePanel != null)
        {
            choicePanel.SetActive(false);
        }
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
    // SPEAKERS
    // =========================================================

    private void RegisterSpeakers()
    {
        speakers.Clear();

        VNDialogueSpeaker[] foundSpeakers =
            FindObjectsByType<VNDialogueSpeaker>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

        foreach (VNDialogueSpeaker speaker in
                 foundSpeakers)
        {
            if (speaker == null)
                continue;

            if (string.IsNullOrWhiteSpace(
                speaker.SpeakerId))
            {
                continue;
            }

            if (speakers.ContainsKey(
                speaker.SpeakerId))
            {
                Debug.LogWarning(
                    $"Duplicate Speaker ID '{speaker.SpeakerId}'.",
                    speaker);

                continue;
            }

            speakers.Add(
                speaker.SpeakerId,
                speaker);
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

    private bool AdvancePressed()
    {
        bool mouse =
            Mouse.current != null &&
            Mouse.current.leftButton
                .wasPressedThisFrame;

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
    // AUDIO SETUP
    // =========================================================

    private void SetupAudioSources()
    {
        if (voiceAudioSource == null)
        {
            voiceAudioSource =
                gameObject.AddComponent<AudioSource>();

            Configure2DAudioSource(
                voiceAudioSource);
        }

        if (soundEffectAudioSource == null)
        {
            soundEffectAudioSource =
                gameObject.AddComponent<AudioSource>();

            Configure2DAudioSource(
                soundEffectAudioSource);
        }
    }

    private void Configure2DAudioSource(
        AudioSource source)
    {
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
    }
}
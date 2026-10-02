using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ChoiceController : MonoBehaviour
{
    public static ChoiceController Instance { get; private set; }

    [Header("Story Sequence")]
    public StoryNode startingNode;
    private StoryNode currentNode;
    private int currentLineIndex = 0;

    [Header("UI Panels Configuration")]
    public GameObject dialoguePanel;

    [Header("UI Text Fields Configuration")]
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI leftButtonText;
    public TextMeshProUGUI rightButtonText;

    [Header("UI Buttons Configuration")]
    public Button optionButtonLeft;
    public Button optionButtonRight;

    [Header("Typewriter Settings")]
    [Tooltip("Time delay between each letter appearing. Lower numbers = faster text typing.")]
    public float typingSpeed = 0.03f;

    private Coroutine typingCoroutine;
    private bool isTyping = false;
    private string currentFullLineText = "";

    [Header("Audio Settings")]
    [Tooltip("Optional: Drag your SoundPlayer AudioSource here. If left empty, the script will automatically create one!")]
    public AudioSource audioSource;
    [Tooltip("Sound played every time a letter prints on screen (keep it short!)")]
    public AudioClip textLetterBlipSound;
    [Tooltip("Sound played when clicking the background screen to advance text")]
    public AudioClip screenClickAdvanceSound;
    [Tooltip("Sound played when selecting a choice button option")]
    public AudioClip choiceButtonSelectSound;

    [Header("Continuous Writing Sound")]
    [Tooltip("An .mp3 clip (like pencil writing) that loops CONTINUOUSLY while text is actively printing.")]
    public AudioClip continuousWritingSound;

    [Header("Randomization Setting")]
    public bool shuffleButtonSides = true;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // --- AUTOMATIC AUDIO FIX: If you forgot to drag the AudioSource, find or create one right here! ---
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            // Configure it safely for 2D UI sound
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            audioSource.volume = 1f;
        }
    }

    private void Start()
    {
        if (startingNode != null)
        {
            StartNewNode(startingNode);
        }
    }

    public void StartNewNode(StoryNode targetNode)
    {
        currentNode = targetNode;
        currentLineIndex = 0;

        if (optionButtonLeft != null) optionButtonLeft.gameObject.SetActive(false);
        if (optionButtonRight != null) optionButtonRight.gameObject.SetActive(false);

        if (dialoguePanel != null) dialoguePanel.SetActive(true);

        ShowCurrentLine();
    }

    public void OnScreenClicked()
    {
        if (isTyping)
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            dialogueText.text = currentFullLineText;
            isTyping = false;

            StopLoopingSound();
            PlaySound(screenClickAdvanceSound, "Screen Advance (Fast-Forward)");
            return;
        }

        if (optionButtonLeft != null && optionButtonLeft.gameObject.activeSelf) return;

        currentLineIndex++;

        if (currentLineIndex < currentNode.storyLines.Length)
        {
            PlaySound(screenClickAdvanceSound, "Screen Advance");
            ShowCurrentLine();
        }
        else
        {
            DisplayBinaryChoices();
        }
    }

    private void ShowCurrentLine()
    {
        if (currentNode != null && currentNode.storyLines.Length > 0 && currentLineIndex < currentNode.storyLines.Length)
        {
            currentFullLineText = currentNode.storyLines[currentLineIndex];

            if (typingCoroutine != null) StopCoroutine(typingCoroutine);

            typingCoroutine = StartCoroutine(TypeTextAnimation(currentFullLineText));
        }
    }

    private IEnumerator TypeTextAnimation(string fullLine)
    {
        isTyping = true;
        dialogueText.text = "";

        if (continuousWritingSound != null)
        {
            Debug.Log($"[Audio Logic] Starting continuous loop: {continuousWritingSound.name}");
            StartLoopingSound(continuousWritingSound);
        }

        foreach (char letter in fullLine.ToCharArray())
        {
            dialogueText.text += letter;

            if (letter != ' ' && textLetterBlipSound != null)
            {
                // PlayOneShot handles character overlays smoothly
                audioSource.PlayOneShot(textLetterBlipSound);
            }

            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
        StopLoopingSound();
    }

    private void StartLoopingSound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.clip = clip;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    private void StopLoopingSound()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.loop = false;
        }
    }

    private void DisplayBinaryChoices()
    {
        if (string.IsNullOrEmpty(currentNode.leftButtonText) && string.IsNullOrEmpty(currentNode.rightButtonText))
        {
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            return;
        }

        optionButtonLeft.onClick.RemoveAllListeners();
        optionButtonRight.onClick.RemoveAllListeners();

        if (optionButtonLeft != null) optionButtonLeft.gameObject.SetActive(true);
        if (optionButtonRight != null) optionButtonRight.gameObject.SetActive(true);

        bool standardOrder = shuffleButtonSides ? (Random.value > 0.5f) : true;

        if (standardOrder)
        {
            if (leftButtonText != null) leftButtonText.text = currentNode.leftButtonText;
            if (rightButtonText != null) rightButtonText.text = currentNode.rightButtonText;

            optionButtonLeft.onClick.AddListener(() => ResolveChoice(currentNode.leftButtonPoints, currentNode.leftNextNode));
            optionButtonRight.onClick.AddListener(() => ResolveChoice(currentNode.rightButtonPoints, currentNode.rightNextNode));
        }
        else
        {
            if (leftButtonText != null) leftButtonText.text = currentNode.rightButtonText;
            if (rightButtonText != null) rightButtonText.text = currentNode.leftButtonText;

            optionButtonLeft.onClick.AddListener(() => ResolveChoice(currentNode.rightButtonPoints, currentNode.rightNextNode));
            optionButtonRight.onClick.AddListener(() => ResolveChoice(currentNode.leftButtonPoints, currentNode.leftNextNode));
        }
    }

    private void ResolveChoice(int pointsReward, StoryNode nextStoryBlock)
    {
        PlaySound(choiceButtonSelectSound, "Choice Button Clicked");

        if (alignmanager.Instance != null)
        {
            alignmanager.Instance.AdjustAlignment(pointsReward);
        }

        if (optionButtonLeft != null) optionButtonLeft.gameObject.SetActive(false);
        if (optionButtonRight != null) optionButtonRight.gameObject.SetActive(false);

        if (nextStoryBlock != null)
        {
            StartNewNode(nextStoryBlock);
        }
        else
        {
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            Debug.Log("Story branch complete.");
        }
    }

    private void PlaySound(AudioClip clip, string soundName)
    {
        if (clip != null && audioSource != null)
        {
            Debug.Log($"[Audio Logic] Playing sound effect: {soundName} ({clip.name})");
            audioSource.PlayOneShot(clip);
        }
        else if (clip == null)
        {
            Debug.LogWarning($"[Audio Logic] Tried to play '{soundName}' but no .mp3 clip was assigned in the Canvas slots!");
        }
    }
}
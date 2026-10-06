using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "NewStoryNode",
    menuName = "Dating Sim/VN Story Node")]
public class VNStoryNode : ScriptableObject
{
    [Serializable]
    public class DialogueLine
    {
        [Header("Speaker")]
        [Tooltip("Example: Mistella. Leave blank for narration.")]
        public string speakerId;

        [TextArea(3, 8)]
        public string text;

        [Header("Character")]
        [Tooltip("Leave blank if expression should stay unchanged.")]
        public string expression;

        [Tooltip("Leave blank if pose should stay unchanged.")]
        public string pose;

        [Header("Optional Character Sound")]
        [Tooltip("Example: Hmph, Laugh, Sigh. Leave blank for silence.")]
        public string voiceCue;

        [Header("Optional SFX")]
        public AudioClip soundEffect;
    }

    [Serializable]
    public class Choice
    {
        [TextArea(2, 4)]
        public string choiceText;

        [Header("Karma")]
        [Tooltip("Positive = karma rises. Negative = karma falls.")]
        public int karmaChange;

        [Header("Branch")]
        public VNStoryNode nextNode;
    }

    [Header("Dialogue")]
    public List<DialogueLine> lines = new();

    [Header("Choices")]
    [TextArea(1, 3)]
    public string choicePrompt = "What should I say?";

    public List<Choice> choices = new();

    [Header("Continue Without Choice")]
    public VNStoryNode nextNode;
}
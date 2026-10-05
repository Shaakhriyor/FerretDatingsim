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
        [Tooltip("Example: Mistella, MC, Klaus. Leave blank for narration.")]
        public string speakerId;

        [TextArea(3, 8)]
        public string text;

        [Header("Character Animation")]
        [Tooltip("Leave blank to keep the current expression.")]
        public string expression;

        [Tooltip("Leave blank to keep the current pose.")]
        public string pose;

        [Header("Optional Character Voice")]
        [Tooltip("Example: Hmph, Laugh, Sigh. Leave blank for no voice sound.")]
        public string voiceCue;

        [Header("Optional Sound Effect")]
        [Tooltip("Non-character SFX such as a phone ring, glass, door, etc.")]
        public AudioClip soundEffect;
    }

    [Serializable]
    public class Choice
    {
        [TextArea(2, 4)]
        public string choiceText;

        [Tooltip("Story node that begins after selecting this choice.")]
        public VNStoryNode nextNode;
    }

    [Header("Dialogue Lines")]
    public List<DialogueLine> lines = new();

    [Header("Choices")]
    [TextArea(1, 3)]
    public string choicePrompt;

    public List<Choice> choices = new();

    [Header("Automatic Continuation")]
    [Tooltip(
        "If this node has no choices, automatically continue to this node. " +
        "Leave empty to end the conversation.")]
    public VNStoryNode nextNode;
}
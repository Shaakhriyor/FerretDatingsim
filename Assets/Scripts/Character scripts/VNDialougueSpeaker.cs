using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class VNDialogueSpeaker : MonoBehaviour
{
    [Serializable]
    public class VoiceCue
    {
        public string cueName;
        public AudioClip clip;
    }

    [Header("Identity")]
    [SerializeField] private string speakerId = "Character";
    [SerializeField] private string displayName = "Character";

    [Header("Fonts")]
    [SerializeField] private TMP_FontAsset nameFont;
    [SerializeField] private TMP_FontAsset dialogueFont;

    [Header("Text Colors")]
    [SerializeField] private Color nameColor = Color.white;
    [SerializeField] private Color dialogueColor = Color.white;

    [Header("Character Controller")]
    [SerializeField] private VNCharacter characterController;

    [Header("Optional Voice Cues")]
    [SerializeField] private List<VoiceCue> voiceCues = new();

    public string SpeakerId => speakerId;
    public string DisplayName => displayName;

    public TMP_FontAsset NameFont => nameFont;
    public TMP_FontAsset DialogueFont => dialogueFont;

    public Color NameColor => nameColor;
    public Color DialogueColor => dialogueColor;

    private void Reset()
    {
        characterController = GetComponent<VNCharacter>();
    }

    private void OnValidate()
    {
        if (characterController == null)
        {
            characterController = GetComponent<VNCharacter>();
        }
    }

    public void ApplyExpression(string expressionName)
    {
        if (characterController == null)
            return;

        if (string.IsNullOrWhiteSpace(expressionName))
            return;

        characterController.SetExpression(expressionName);
    }

    public void ApplyPose(string poseName)
    {
        if (characterController == null)
            return;

        if (string.IsNullOrWhiteSpace(poseName))
            return;

        characterController.SetPose(poseName);
    }

    public AudioClip GetVoiceCue(string cueName)
    {
        if (string.IsNullOrWhiteSpace(cueName))
            return null;

        foreach (VoiceCue cue in voiceCues)
        {
            if (cue == null)
                continue;

            if (string.Equals(
                cue.cueName,
                cueName,
                StringComparison.OrdinalIgnoreCase))
            {
                return cue.clip;
            }
        }

        return null;
    }
}
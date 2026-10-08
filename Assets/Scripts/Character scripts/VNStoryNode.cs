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

        [Header("Scene Picture")]
        [Tooltip("Optional: change the full-scene picture when this line begins. Leave empty to keep the previous picture.")]
        public Sprite scenePicture;

        [Header("Picture Fade")]
        [Tooltip("Fade the full-scene picture to black when this line starts. Dialogue stays visible.")]
        public bool fadeToBlack;

        [Min(0f)]
        [Tooltip("Fade duration in seconds. Zero cuts immediately to black.")]
        public float fadeDuration = 1.5f;

        [Header("Doctor Intro")]
        [Tooltip("Start fading out the doctor's ringing and whole-screen blur when this line appears.")]
        public bool endDoctorIntro;

        [Header("Character")]
        [Tooltip("Leave blank if expression should stay unchanged.")]
        public string expression;

        [Tooltip("Leave blank if pose should stay unchanged.")]
        public string pose;

        [Header("Optional Character Sound")]
        [Tooltip("Example: Hmph, Laugh, Sigh. Leave blank for silence.")]
        public string voiceCue;

        [Range(0f, 1f)]
        [Tooltip("Volume for this line's voice cue: 0 = silent, 1 = normal full volume.")]
        public float voiceCueVolume = 1f;

        [Header("Optional SFX")]
        public AudioClip soundEffect;

        [Range(0f, 1f)]
        [Tooltip("Volume for this line's sound effect: 0 = silent, 1 = normal full volume.")]
        public float soundEffectVolume = 1f;

        [Tooltip("Tick for spoken audio placed in Sound Effect: stop it when leaving this line. Voice Cues always stop automatically.")]
        public bool stopSoundEffectOnAdvance;
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

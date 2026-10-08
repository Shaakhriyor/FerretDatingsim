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
        [Tooltip("Use MC for the player. Leave blank for narration.")]
        public string speakerId;

        [TextArea(3, 8)]
        public string text;

        [Header("Scene Picture")]
        [Tooltip("Change the picture when this line begins. Leave empty to keep the previous picture.")]
        public Sprite scenePicture;

        [Header("Picture Fade")]
        [Tooltip("Fade the scene picture to black when this line starts. Dialogue stays visible.")]
        public bool fadeToBlack;

        [Min(0f)]
        [Tooltip("Fade duration in seconds. Zero cuts immediately to black.")]
        public float fadeDuration = 1.5f;

        [Header("Doctor Intro")]
        [Tooltip("Start fading out the ringing and whole-screen blur when this line appears.")]
        public bool endDoctorIntro;

        [Header("Character")]
        [Tooltip("Leave blank to keep the current expression.")]
        public string expression;

        [Tooltip("Leave blank to keep the current pose.")]
        public string pose;

        [Header("Optional Character Sound")]
        [Tooltip("Example: Hmph, Laugh, Sigh. Leave blank for silence.")]
        public string voiceCue;

        [Range(0f, 1f)]
        public float voiceCueVolume = 1f;

        [Header("Optional SFX")]
        public AudioClip soundEffect;

        [Range(0f, 1f)]
        public float soundEffectVolume = 1f;

        [Tooltip("Stop this line's sound effect when advancing. Voice cues always stop automatically.")]
        public bool stopSoundEffectOnAdvance;
    }

    [Serializable]
    public class Choice
    {
        [TextArea(2, 4)]
        public string choiceText;

        [Header("Karma")]
        [Tooltip("Positive raises karma. Negative lowers it.")]
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

    [Header("Scene After This Branch")]
    [Tooltip("Use this node's scene destination instead of the dialogue manager's. Choices and Next Node still take priority.")]
    public bool overrideNextScene;

    [Tooltip("Used when Override Next Scene is checked. Enter a Unity scene name, or leave empty to finish here.")]
    public string nextSceneName;
}
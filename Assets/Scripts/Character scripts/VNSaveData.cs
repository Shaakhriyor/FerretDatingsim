using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class VNSaveData
{
    public int version = 1;
    public string savedUtc;
    public string scenePath;
    public string sceneName;
    public string thumbnailPng;
    public string playerName = "MC";
    public bool hasKarma;
    public int karma;
    public string nodeId;
    public string nodeRevision;
    public int lineIndex;
    public bool running;
    public bool choicesShowing;
    public bool typing;
    public int visibleCharacters;
    public string previewText;
    public string pictureId;
    public Color pictureColor = Color.white;
    public bool pictureEnabled;
    public List<VNSaveChoice> decisions = new List<VNSaveChoice>();
    public List<VNCharacterSnapshot> characters = new List<VNCharacterSnapshot>();
    public List<VNAudioSnapshot> audio = new List<VNAudioSnapshot>();
}

[Serializable]
public class VNSaveChoice
{
    public string nodeId;
    public int choiceIndex;
}

[Serializable]
public class VNCharacterSnapshot
{
    public string key;
    public Vector3 position;
    public Vector3 rotation;
    public Vector3 scale;
    public bool active;
    public string expression;
    public string targetPose;
    public bool moving;
    public List<VNCharacter.SavedPartPose> parts = new List<VNCharacter.SavedPartPose>();
}

[Serializable]
public class VNAudioSnapshot
{
    public string key;
    public string clipId;
    public int samples;
    public float volume;
    public float pitch;
    public bool playing;
}

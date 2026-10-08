using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class VNSaveSystem
{
    public const int SlotCount = 60;
    public static string PlayerName = "MC";
    public static List<VNSaveChoice> Decisions = new List<VNSaveChoice>();
    private static VNSaveCatalog catalog;
    private static VNSaveData pending;
    public static bool HasPendingLoad => pending != null;
    public static string SaveDirectory => Path.Combine(Application.persistentDataPath, "VN_Saves");

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetForPlay()
    {
        catalog = null; pending = null;
        PlayerName = "MC";
        Decisions = new List<VNSaveChoice>();
    }

    public static string NodeRevision(VNStoryNode node, VNSaveCatalog assets)
    {
        if (node == null) return "";
        var text = new StringBuilder();
        if (node.lines != null) foreach (var line in node.lines)
        {
            if (line == null) { text.Append("null;"); continue; }
            text.Append(line.speakerId).Append('\n').Append(line.text).Append('\n')
                .Append(line.expression).Append('\n').Append(line.pose).Append('\n')
                .Append(line.voiceCue).Append('\n').Append(assets.Id(line.scenePicture)).Append('\n');
        }
        text.Append(node.choicePrompt).Append('\n');
        if (node.choices != null) foreach (var choice in node.choices)
        {
            if (choice == null) { text.Append("null;"); continue; }
            text.Append(choice.choiceText).Append('\n').Append(choice.karmaChange).Append('\n').Append(assets.Id(choice.nextNode)).Append('\n');
        }
        text.Append(assets.Id(node.nextNode));
        using (var hash = System.Security.Cryptography.SHA256.Create())
            return Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(text.ToString())));
    }

    public static VNSaveCatalog Catalog
    {
        get
        {
            if (catalog == null) catalog = Resources.Load<VNSaveCatalog>("VNSaveCatalog");
            if (catalog == null) throw new InvalidOperationException("Create the heart menu or refresh its save catalog first.");
            return catalog;
        }
    }

    public static string Format(string text)
    {
        // Prevent a typed name from being interpreted as TMP rich-text markup.
        string name = (PlayerName ?? "MC").Replace("<", "").Replace(">", "");
        return (text ?? "").Replace("{playerName}", name);
    }

    public static VNSaveData Capture(VNDialogueManager manager)
    {
        if (manager == null) throw new InvalidOperationException("The dialogue manager is missing.");
        VNSaveData data = manager.CaptureSave(Catalog);
        Scene scene = SceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(scene.path)) throw new InvalidOperationException("Save this Unity scene before creating a game save.");
        data.scenePath = scene.path;
        data.sceneName = scene.name;
        data.playerName = PlayerName;
        data.hasKarma = KarmaManager.Instance != null;
        if (data.hasKarma) data.karma = KarmaManager.Instance.CurrentKarma;
        foreach (var decision in Decisions)
            data.decisions.Add(new VNSaveChoice { nodeId = decision.nodeId, choiceIndex = decision.choiceIndex });
        foreach (VNCharacter character in UnityEngine.Object.FindObjectsByType<VNCharacter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            data.characters.Add(character.CaptureSave(Key(character.transform)));
        foreach (AudioSource source in UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!source.loop || source.clip == null) continue;
            data.audio.Add(new VNAudioSnapshot {
                key = AudioKey(source), clipId = Catalog.Id(source.clip), samples = source.timeSamples,
                volume = source.volume, pitch = source.pitch, playing = source.isPlaying
            });
        }
        return data;
    }

    public static void RecordChoice(VNStoryNode node, int index)
    {
        Decisions.Add(new VNSaveChoice { nodeId = Catalog.Id(node), choiceIndex = index });
    }

    private static string SlotPath(int slot)
    {
        if (slot < 1 || slot > SlotCount) throw new ArgumentOutOfRangeException(nameof(slot));
        return Path.Combine(SaveDirectory, "slot_" + slot.ToString("D3") + ".json");
    }

    public static bool Exists(int slot) => File.Exists(SlotPath(slot));

    public static VNSaveData Read(int slot)
    {
        string path = SlotPath(slot);
        if (!File.Exists(path)) return null;
        VNSaveData data = JsonUtility.FromJson<VNSaveData>(File.ReadAllText(path, Encoding.UTF8));
        if (data == null || data.version != 1 || string.IsNullOrEmpty(data.scenePath) || string.IsNullOrEmpty(data.savedUtc))
            throw new InvalidDataException("This slot is damaged or belongs to an unsupported save version.");
        return data;
    }

    public static void Write(int slot, VNSaveData snapshot, string thumbnail)
    {
        if (snapshot == null) throw new InvalidOperationException("No story moment was captured. Close the menu and open it again.");
        Directory.CreateDirectory(SaveDirectory);
        // Clone so slot timestamps don't mutate the paused snapshot.
        VNSaveData data = JsonUtility.FromJson<VNSaveData>(JsonUtility.ToJson(snapshot));
        data.savedUtc = DateTime.UtcNow.ToString("o");
        data.thumbnailPng = thumbnail ?? "";
        string path = SlotPath(slot);
        string temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonUtility.ToJson(data), new UTF8Encoding(false));
        try
        {
            if (File.Exists(path)) File.Replace(temporary, path, path + ".bak");
            else File.Move(temporary, path);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    public static bool Load(VNSaveData data)
    {
        Validate(data);
        if (SceneManager.GetActiveScene().path == data.scenePath)
        {
            Apply(data, VNDialogueManager.Instance);
            return false;
        }
        if (!Application.CanStreamedLevelBeLoaded(data.scenePath))
            throw new InvalidOperationException("Add the saved scene to Build Profiles > Scene List before loading it from another scene.");
        pending = data;
        try
        {
            if (SceneManager.LoadSceneAsync(data.scenePath, LoadSceneMode.Single) == null)
                throw new InvalidOperationException("The saved scene could not be opened.");
        }
        catch { pending = null; throw; }
        return true;
    }

    public static bool TryRestorePending(VNDialogueManager manager)
    {
        if (pending == null) return false;
        VNSaveData data = pending;
        pending = null;
        try { Apply(data, manager); }
        catch (Exception error) { Debug.LogError("Could not restore save: " + error.Message); }
        // Do not silently start a new game over a failed load.
        return true;
    }

    private static void Validate(VNSaveData data)
    {
        if (data == null || data.version != 1) throw new InvalidDataException("Unsupported save file.");
        VNStoryNode node = Catalog.Resolve<VNStoryNode>(data.nodeId);
        if (data.running)
        {
            if (node == null) throw new InvalidDataException("The saved story node is missing.");
            if (data.nodeRevision != NodeRevision(node, Catalog))
                throw new InvalidDataException("This story node has been edited since this save. Make a new test save for the updated dialogue.");
            if (data.choicesShowing)
            {
                if (node.choices == null || node.choices.Count == 0) throw new InvalidDataException("The choices in this save have changed.");
            }
            else if (node.lines == null || data.lineIndex < 0 || data.lineIndex >= node.lines.Count)
                throw new InvalidDataException("The dialogue in this save has changed.");
        }
        Catalog.Resolve<Sprite>(data.pictureId);
        if (data.audio != null)
            foreach (var audio in data.audio) Catalog.Resolve<AudioClip>(audio.clipId);
    }

    private static void Apply(VNSaveData data, VNDialogueManager manager)
    {
        if (manager == null) throw new InvalidOperationException("The saved scene needs a VNDialogueManager.");
        Validate(data);
        // Check required scene objects before changing the story.
        var characters = new Dictionary<string, VNCharacter>();
        foreach (var character in UnityEngine.Object.FindObjectsByType<VNCharacter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            characters[Key(character.transform)] = character;
        var sources = new Dictionary<string, AudioSource>();
        foreach (var source in UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            sources[AudioKey(source)] = source;
        if (data.hasKarma && KarmaManager.Instance == null) throw new InvalidOperationException("The saved scene needs its KarmaManager.");
        if (data.characters != null) foreach (var state in data.characters)
            if (!characters.ContainsKey(state.key)) throw new InvalidOperationException("A saved character has been moved or removed from this scene.");
        if (data.audio != null) foreach (var state in data.audio)
            if (!sources.ContainsKey(state.key)) throw new InvalidOperationException("A saved music or ambience object has been moved or removed.");

        PlayerName = string.IsNullOrWhiteSpace(data.playerName) ? "MC" : data.playerName;
        Decisions = data.decisions != null ? new List<VNSaveChoice>(data.decisions) : new List<VNSaveChoice>();
        if (data.hasKarma) KarmaManager.Instance.SetKarma(data.karma);
        manager.RestoreSave(data, Catalog);
        if (data.characters != null) foreach (var state in data.characters) characters[state.key].RestoreSave(state);
        foreach (var source in sources.Values) if (source.loop) source.Stop();
        if (data.audio != null) foreach (var state in data.audio)
        {
            AudioSource source = sources[state.key];
            source.Stop();
            source.clip = Catalog.Resolve<AudioClip>(state.clipId);
            source.volume = Mathf.Clamp01(state.volume);
            source.pitch = state.pitch;
            source.loop = true;
            if (state.playing && source.gameObject.activeInHierarchy && source.enabled)
            {
                source.Play();
                source.timeSamples = Mathf.Clamp(state.samples, 0, Mathf.Max(0, source.clip.samples - 1));
            }
        }
    }

    public static string Key(Transform transform)
    {
        int occurrence = 0;
        if (transform.parent != null)
        {
            for (int i = 0; i < transform.GetSiblingIndex(); i++)
                if (transform.parent.GetChild(i).name == transform.name) occurrence++;
        }
        else
        {
            foreach (var root in transform.gameObject.scene.GetRootGameObjects())
            {
                if (root.transform == transform) break;
                if (root.name == transform.name) occurrence++;
            }
        }
        string segment = Uri.EscapeDataString(transform.name) + ":" + occurrence;
        return transform.parent == null ? segment : Key(transform.parent) + "/" + segment;
    }

    private static string AudioKey(AudioSource source)
    {
        AudioSource[] sameObject = source.GetComponents<AudioSource>();
        return Key(source.transform) + "@audio:" + Array.IndexOf(sameObject, source);
    }
}

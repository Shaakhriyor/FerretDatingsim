#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class VNSaveCatalogEditor
{
    [MenuItem("Tools/VN/Refresh Save Assets")]
    public static void Refresh()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("Stop Play mode before refreshing save assets.");
            return;
        }

        const string path = "Assets/Resources/VNSaveCatalog.asset";
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");

        var catalog = AssetDatabase.LoadAssetAtPath<VNSaveCatalog>(path);
        if (catalog == null)
        {
            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
            {
                Debug.LogError("Another asset already occupies " + path);
                return;
            }
            catalog = ScriptableObject.CreateInstance<VNSaveCatalog>();
            AssetDatabase.CreateAsset(catalog, path);
        }

        var assets = new HashSet<UnityEngine.Object>();
        foreach (var entry in catalog.entries)
            if (entry != null && entry.asset != null) assets.Add(entry.asset);

        foreach (string guid in AssetDatabase.FindAssets("t:VNStoryNode", new[] { "Assets" }))
        {
            var node = AssetDatabase.LoadAssetAtPath<VNStoryNode>(AssetDatabase.GUIDToAssetPath(guid));
            if (node == null) continue;
            assets.Add(node);
            if (node.lines == null) continue;
            foreach (var line in node.lines)
            {
                if (line == null) continue;
                if (line.scenePicture != null) assets.Add(line.scenePicture);
                if (line.soundEffect != null) assets.Add(line.soundEffect);
            }
        }

        foreach (var source in Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (source.clip != null) assets.Add(source.clip);

        foreach (var manager in Object.FindObjectsByType<VNDialogueManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var field = new SerializedObject(manager).FindProperty("scenePictureImage");
            var image = field?.objectReferenceValue as UnityEngine.UI.Image;
            if (image != null && image.sprite != null) assets.Add(image.sprite);
        }

        Undo.RecordObject(catalog, "Refresh VN Save Assets");
        catalog.entries.Clear();
        foreach (var asset in assets)
        {
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long localId))
                catalog.entries.Add(new VNSaveCatalog.Entry { id = guid + ":" + localId, asset = asset });
        }
        catalog.entries.Sort((a, b) => string.CompareOrdinal(a.id, b.id));
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        Debug.Log("Save assets refreshed: " + catalog.entries.Count + " registered assets.");
    }
}
#endif

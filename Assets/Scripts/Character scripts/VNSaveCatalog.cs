using System;
using System.Collections.Generic;
using UnityEngine;

public class VNSaveCatalog : ScriptableObject
{
    [Serializable] public class Entry { public string id; public UnityEngine.Object asset; }
    public List<Entry> entries = new List<Entry>();

    public string Id(UnityEngine.Object asset)
    {
        if (asset == null) return "";
        foreach (Entry entry in entries)
            if (entry.asset == asset) return entry.id;
        throw new InvalidOperationException("An asset is missing from the save catalog: " + asset.name + ". Run Tools > VN > Refresh Save Catalog.");
    }

    public T Resolve<T>(string id) where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(id)) return null;
        foreach (Entry entry in entries)
            if (entry.id == id && entry.asset is T asset) return asset;
        throw new InvalidOperationException("This save refers to an asset that is no longer available. Refresh the save catalog after adding story assets.");
    }
}

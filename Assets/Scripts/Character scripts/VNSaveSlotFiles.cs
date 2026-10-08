using System;
using System.IO;

public static class VNSaveSlotFiles
{
    // Called only after the menu's confirmation. No folder-wide deletion.
    public static void Delete(string directory, int slot, int slotCount)
    {
        if (slot < 1 || slot > slotCount)
            throw new ArgumentOutOfRangeException(nameof(slot));
        if (string.IsNullOrWhiteSpace(directory))
            throw new ArgumentException("The save directory is missing.", nameof(directory));

        string path = Path.Combine(directory, "slot_" + slot.ToString("D3",
            System.Globalization.CultureInfo.InvariantCulture) + ".json");

        // Remove backups first. If that fails, retain the playable save.
        // File.Delete safely does nothing when an individual file is absent.
        File.Delete(path + ".bak");
        File.Delete(path + ".tmp");
        File.Delete(path);
    }
}

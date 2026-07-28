using System.IO;
using UnityEngine;

/// <summary>
/// Reads/writes a <see cref="SaveData"/> to a per-slot JSON file in Application.persistentDataPath.
/// Static and stateless. Files are named save_0.json, save_1.json, ... one per save slot.
/// </summary>
public static class SaveSystem
{
    private static string FilePath(int slot) => Path.Combine(Application.persistentDataPath, $"save_{slot}.json");

    public static bool HasSave(int slot = 0) => File.Exists(FilePath(slot));

    public static void Save(SaveData data, int slot = 0)
    {
        if (data == null) return;
        try
        {
            string json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(FilePath(slot), json);
            Debug.Log($"[SaveSystem] Saved slot {slot} to {FilePath(slot)}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveSystem] Save failed: {e.Message}");
        }
    }

    public static SaveData Load(int slot = 0)
    {
        if (!HasSave(slot)) return null;
        try
        {
            return JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath(slot)));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveSystem] Load failed: {e.Message}");
            return null;
        }
    }

    public static void Delete(int slot = 0)
    {
        try
        {
            if (HasSave(slot)) File.Delete(FilePath(slot));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveSystem] Delete failed: {e.Message}");
        }
    }
}

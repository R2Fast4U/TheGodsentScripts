using System.IO;
using UnityEngine;

/// <summary>
/// Reads/writes a <see cref="SaveData"/> to a JSON file in Application.persistentDataPath.
/// Static and stateless — call from anywhere. On desktop the file lives at:
/// C:/Users/&lt;user&gt;/AppData/LocalLow/&lt;company&gt;/&lt;product&gt;/save.json
/// </summary>
public static class SaveSystem
{
    private const string FileName = "save.json";

    private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    public static bool HasSave() => File.Exists(FilePath);

    public static void Save(SaveData data)
    {
        if (data == null) return;
        try
        {
            string json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(FilePath, json);
            Debug.Log($"[SaveSystem] Saved to {FilePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveSystem] Save failed: {e.Message}");
        }
    }

    public static SaveData Load()
    {
        if (!HasSave()) return null;
        try
        {
            string json = File.ReadAllText(FilePath);
            return JsonUtility.FromJson<SaveData>(json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveSystem] Load failed: {e.Message}");
            return null;
        }
    }

    public static void Delete()
    {
        try
        {
            if (HasSave()) File.Delete(FilePath);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveSystem] Delete failed: {e.Message}");
        }
    }
}

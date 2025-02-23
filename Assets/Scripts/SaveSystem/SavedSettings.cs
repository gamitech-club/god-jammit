using System.IO;
using UnityEngine;
using Newtonsoft.Json;

public struct SavedSettings
{
    public static readonly string FilePath = Path.Combine(Application.persistentDataPath, "Settings.json");
    public static readonly SavedSettings Default = new() {
        Version = 1,
        MasterVolume = .8f,
        MusicVolume = .8f,
        SFXVolume = .8f,
        CameraShakeEnabled = true
    };
    
    public static SavedSettings Instance = LoadOrDefault();

    // Serialized fields
    public int Version;
    public float MasterVolume;
    public float MusicVolume;
    public float SFXVolume;
    public bool CameraShakeEnabled;

    /// <summary>
    /// Saves settings to file.
    /// </summary>
    public void Save()
    {
        Debug.Log($"[{nameof(SavedSettings)}/Save] Saving player settings..");

        var jsonText = JsonConvert.SerializeObject(this, Formatting.Indented);
        File.WriteAllText(FilePath, jsonText);
    }

    /// <summary>
    /// Loads settings from file. Returns default values if file doesn't exist.
    /// </summary>
    public static SavedSettings LoadOrDefault()
    {
        string speaker = $"{nameof(SavedSettings)}/Load";
        if (!File.Exists(FilePath))
        {
            Debug.Log($"[{speaker}] Settings file not found at '{FilePath}'. Using default values.");
            return Default;
        }

        Debug.Log($"[{speaker}] Loading player settings..");

        var jsonString = File.ReadAllText(FilePath);
        var loaded = Default;

        try {
            loaded = JsonConvert.DeserializeObject<SavedSettings>(jsonString);
        } catch (System.Exception e) {
            Debug.LogError($"[{speaker}/Load] Failed to load settings: {e.Message}");
        }

        return loaded;
    }

    public static void ResetInstance()
        => Instance = Default;

    public static void DeleteFile()
    {
        if (File.Exists(FilePath))
            File.Delete(FilePath);
    }
}

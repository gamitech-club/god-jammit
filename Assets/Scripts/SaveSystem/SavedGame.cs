using System.IO;
using UnityEngine;
using Newtonsoft.Json;

public struct SavedGame
{
    private const bool IsEncrypted = true;
    private const string Password = "47wtPrg3D";

    public static readonly string FilePath = Path.Combine(Application.persistentDataPath, "SaveGame");
    public static readonly SavedGame Default = new() {
        Version = 1,
        HighScore = 0
    };

    public static SavedGame Instance = LoadOrDefault();

    // Serialized fields
    public int Version;
    public int HighScore;

    /// <summary>
    /// Saves game data to file.
    /// </summary>
    public void Save()
    {
        // Debug.Log($"[{nameof(SavedGame)}/Save] Saving game..");

        string content = JsonConvert.SerializeObject(this, Formatting.Indented);
        if (IsEncrypted)
            content = EncryptDecrypt(content);
        File.WriteAllText(FilePath, content);
    }

    /// <summary>
    /// Loads saved game data from file. Returns default values if file doesn't exist.
    /// </summary>
    public static SavedGame LoadOrDefault()
    {
        string speaker = $"{nameof(SavedGame)}/Load";
        if (!File.Exists(FilePath))
        {
            Debug.Log($"[{speaker}] Save file not found at '{FilePath}'. Using default values.");
            return Default;
        }

        Debug.Log($"[{speaker}] Loading player saved game..");

        SavedGame loaded = Default;
        string content = File.ReadAllText(FilePath);

        if (IsEncrypted)
            content = EncryptDecrypt(content);

        try {
            loaded = JsonConvert.DeserializeObject<SavedGame>(content);
        } catch (System.Exception e) {
            Debug.LogError($"[{speaker}/Load] Failed to load save data: {e.Message}");
        }

        return loaded;
    }

    private static string EncryptDecrypt(string content)
    {
        string modified = "";
        for (int i = 0; i < content.Length; i++)
            modified += (char)(content[i] ^ Password[i % Password.Length]);
        return modified;
    }
}

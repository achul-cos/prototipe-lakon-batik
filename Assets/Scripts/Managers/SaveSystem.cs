using UnityEngine;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Simple file-based save system using JsonUtility.
/// Files placed in Application.persistentDataPath/saves.
/// Includes basic error handling and list API.
/// </summary>
public class SaveSystem : Singleton<SaveSystem>
{
    private string SaveFolder => Path.Combine(Application.persistentDataPath, "saves");

    protected override void Awake()
    {
        base.Awake();
        if (!Directory.Exists(SaveFolder)) Directory.CreateDirectory(SaveFolder);
    }

    public bool SaveGame(SaveData data)
    {
        if (data == null) return false;
        try
        {
            data.lastPlayed = System.DateTime.Now;
            string json = JsonUtility.ToJson(data, true);
            string path = Path.Combine(SaveFolder, $"{SanitizeFileName(data.shopName)}.json");
            File.WriteAllText(path, json);
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"SaveSystem.SaveGame error: {ex}");
            return false;
        }
    }

    public SaveData LoadGame(string shopName)
    {
        try
        {
            string path = Path.Combine(SaveFolder, $"{SanitizeFileName(shopName)}.json");
            if (!File.Exists(path)) return null;
            string json = File.ReadAllText(path);
            var data = JsonUtility.FromJson<SaveData>(json);
            return data;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"SaveSystem.LoadGame error: {ex}");
            return null;
        }
    }

    public List<SaveData> GetAllSaves()
    {
        List<SaveData> list = new List<SaveData>();
        try
        {
            if (!Directory.Exists(SaveFolder)) return list;
            var files = Directory.GetFiles(SaveFolder, "*.json");
            foreach (var f in files)
            {
                string json = File.ReadAllText(f);
                var data = JsonUtility.FromJson<SaveData>(json);
                list.Add(data);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"SaveSystem.GetAllSaves error: {ex}");
        }
        return list;
    }

    public bool DeleteSave(string shopName)
    {
        try
        {
            string path = Path.Combine(SaveFolder, $"{SanitizeFileName(shopName)}.json");
            if (File.Exists(path)) File.Delete(path);
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"SaveSystem.DeleteSave error: {ex}");
            return false;
        }
    }

    private string SanitizeFileName(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
        return name;
    }
}
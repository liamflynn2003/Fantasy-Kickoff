using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

public class LocalDataManager : MonoBehaviour
{
    private string filePath;
    private string cacheFilePath;

    private void Awake()
    {
        filePath = Path.Combine(Application.persistentDataPath, "players.json");
        cacheFilePath = Path.Combine(Application.persistentDataPath, "playerCache.json");
    }

    // Save Player Data Locally (for individual player data)
    public void SavePlayerData(List<PlayerListManager.PlayerData> playerData)
    {
        try
        {
            string json = JsonConvert.SerializeObject(playerData, Formatting.Indented);
            File.WriteAllText(filePath, json);
            Debug.Log("Player data saved locally.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save player data: {e.Message}");
        }
    }

    // Load Player Data from Local File
    public List<PlayerListManager.PlayerData> LoadPlayerData()
    {
        if (File.Exists(filePath))
        {
            try
            {
                string json = File.ReadAllText(filePath);
                var playerData = JsonConvert.DeserializeObject<List<PlayerListManager.PlayerData>>(json);
                Debug.Log("Player data loaded from local file.");
                return playerData;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load player data: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning("No local player data file found.");
        }
        return new List<PlayerListManager.PlayerData>();
    }

    // Check if Data Exists Locally
    public bool HasLocalData()
    {
        return File.Exists(filePath);
    }

    // Save the player cache to a file (for caching)
    public void SaveCache(Dictionary<int, List<PlayerListManager.PlayerData>> playerCache)
    {
        try
        {
            string json = JsonConvert.SerializeObject(playerCache, Formatting.Indented);
            File.WriteAllText(cacheFilePath, json);
            Debug.Log("Cache saved.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save cache: {e.Message}");
        }
    }

    // Load the cache from a file (for caching)
    public Dictionary<int, List<PlayerListManager.PlayerData>> LoadCache()
    {
        if (File.Exists(cacheFilePath))
        {
            try
            {
                string json = File.ReadAllText(cacheFilePath);
                var playerCache = JsonConvert.DeserializeObject<Dictionary<int, List<PlayerListManager.PlayerData>>>(json);
                Debug.Log("Cache loaded.");
                return playerCache;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load cache: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning("No cache file found.");
        }
        return new Dictionary<int, List<PlayerListManager.PlayerData>>();
    }

    // Check if Cache Exists
    public bool HasCache()
    {
        return File.Exists(cacheFilePath);
    }
}

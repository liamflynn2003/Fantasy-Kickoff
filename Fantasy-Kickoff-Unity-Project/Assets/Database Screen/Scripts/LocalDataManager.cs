using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

public class LocalDataManager : MonoBehaviour
{
    private string filePath;

    private void Awake()
    {
        filePath = Path.Combine(Application.persistentDataPath, "players.json");
    }

    // Save Player Data Locally
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
}

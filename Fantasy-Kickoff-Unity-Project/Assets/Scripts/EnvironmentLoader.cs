using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EnvironmentLoader : MonoBehaviour
{
    private static bool isEnvLoaded = false;

    private void Awake()
    {
        // Ensure this GameObject persists across scene loads
        DontDestroyOnLoad(gameObject);
    }

    // Called when any scene is loaded
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        LoadEnv();
    }

    private void LoadEnv()
    {
        if (isEnvLoaded) return;

        string envPath = Application.dataPath + "/../.env";
        if (File.Exists(envPath))
        {
            foreach (var line in File.ReadAllLines(envPath))
            {
                if (!string.IsNullOrWhiteSpace(line) && line.Contains('='))
                {
                    var split = line.Split('=', 2);
                    Environment.SetEnvironmentVariable(split[0], split[1]);
                }
            }
            isEnvLoaded = true;
            Debug.Log("✅ API Key Loaded: " + Environment.GetEnvironmentVariable("API_KEY"));
        }
        else
        {
            Debug.LogWarning("⚠️ .env file not found at: " + envPath);
        }
    }
}

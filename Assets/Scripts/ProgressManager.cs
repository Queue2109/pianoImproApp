using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine;

public class SongProgressManager : MonoBehaviour
{
    private AllSongsProgress allSongsProgress;

    private static SongProgressManager instance;
    public static SongProgressManager Instance
    {
        get
        {
            if (instance == null)
            {
                // In case you want a singleton pattern:
                GameObject obj = new GameObject("SongProgressManager");
                instance = obj.AddComponent<SongProgressManager>();
                DontDestroyOnLoad(obj);
            }
            return instance;
        }
    }

    private void Awake()
    {
        // Enforce singleton if desired
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(this.gameObject);

        // Load existing progress from file when manager is created
        LoadProgress();
    }

    /// <summary>
    /// Returns the path to the JSON file where progress is stored.
    /// </summary>
    private string GetProgressFilePath()
    {
        // E.g. "C:/Users/You/AppData/LocalLow/CompanyName/ProductName/progress.json" on Windows
        return Path.Combine(Application.persistentDataPath, "progress.json");
    }

    /// <summary>
    /// Loads the progress from a JSON file. If none exists, creates a fresh AllSongsProgress.
    /// </summary>
    public void LoadProgress()
    {
        string filePath = GetProgressFilePath();
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            allSongsProgress = JsonUtility.FromJson<AllSongsProgress>(json);
        }
        else
        {
            allSongsProgress = new AllSongsProgress();
        }
    }

    /// <summary>
    /// Saves the current AllSongsProgress data to a JSON file.
    /// </summary>
    public void SaveProgress()
    {
        string filePath = GetProgressFilePath();
        string json = JsonUtility.ToJson(allSongsProgress, prettyPrint: true);
        File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// Marks a given song as cleared in the specified hand mode.
    /// </summary>
    public void MarkSongCleared(string songId, string handMode)
    {
        // 1. Look up existing progress for this song
        SongProgress progress = allSongsProgress.songs.Find(p => p.songId == songId);
        if (progress == null)
        {
            // Create new if we don't have it yet
            progress = new SongProgress();
            progress.songId = songId;
            allSongsProgress.songs.Add(progress);
        }

        // 2. Update the relevant hand mode flag(s)
        switch (handMode)
        {
            case "Left":
                progress.leftHandCleared = true;
                break;
            case "Right":
                progress.rightHandCleared = true;
                break;
            case "Both":
                progress.bothHandsCleared = true;
                break;
            default:
                Debug.LogWarning("Unknown hand mode: " + handMode);
                break;
        }

        // 3. Save changes to disk
        SaveProgress();
    }

    /// <summary>
    /// Checks if the player has cleared a specific hand mode for the given song.
    /// </summary>
    public bool IsSongCleared(string songId, string handMode)
    {
        SongProgress progress = allSongsProgress.songs.Find(p => p.songId == songId);
        if (progress == null) return false;

        switch (handMode)
        {
            case "Left":
                return progress.leftHandCleared;
            case "Right":
                return progress.rightHandCleared;
            case "Both":
                return progress.bothHandsCleared;
            default:
                return false;
        }
    }

    /// <summary>
    /// Example: Check if *all* hand modes are cleared for a given song.
    /// </summary>
    public bool IsSongFullyCleared(string songId)
    {
        SongProgress progress = allSongsProgress.songs.Find(p => p.songId == songId);
        if (progress == null) return false;

        return (progress.leftHandCleared &&
                progress.rightHandCleared &&
                progress.bothHandsCleared);
    }
}

[Serializable]
public class SongProgress
{
    public string songId;            // Unique identifier for the song
    public bool leftHandCleared;
    public bool rightHandCleared;
    public bool bothHandsCleared;
}

[Serializable]
public class AllSongsProgress
{
    public List<SongProgress> songs = new List<SongProgress>();
}

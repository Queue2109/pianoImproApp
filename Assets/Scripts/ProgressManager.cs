using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine;
using UnityEditor;

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

    public SongProgress GetSongProgress(string songId)
    {
        return allSongsProgress.songs.Find(p => p.songId == songId);
    }

    public float GetOverallProgress(string songId)
    {
        SongProgress progress = allSongsProgress.songs.Find(p => p.songId == songId);
        if (progress == null) return 0f;
        return (progress.leftHandScore + progress.rightHandScore) / 2;
    }

    public void SaveSongProgress(string songId, float leftHandScore, float rightHandsScore,
                             int allImprovisedNotes, int wrongImprovisedNotes)
    {
        // Try to find existing entry by songId
        SongProgress progress = allSongsProgress.songs.Find(p => p.songId == songId);

        // If none is found, create a new one and add to the list
        if (progress == null)
        {
            progress = new SongProgress();
            progress.songId = songId;
            allSongsProgress.songs.Add(progress);
        }

        // Update the fields
        progress.leftHandScore = leftHandScore;
        progress.rightHandScore = rightHandsScore;
        progress.numberOfWrongImprovisedNotes = wrongImprovisedNotes;
        progress.numberOfImprovisedNotes = allImprovisedNotes;

        // Finally, save
        SaveProgress();
    }

    public void ResetProgress()
    {
        // Create a new blank progress object
        allSongsProgress = new AllSongsProgress();

        // Convert to JSON
        string json = JsonUtility.ToJson(allSongsProgress, true);

        // Overwrite the existing progress file
        File.WriteAllText(GetProgressFilePath(), json);

        Debug.Log("All songs progress reset.");
    }
} 

[Serializable]
public class SongProgress
{
    public string songId;
    public float leftHandScore;
    public float rightHandScore;
    public int numberOfImprovisedNotes;
    public int numberOfWrongImprovisedNotes;
}

[Serializable]
public class AllSongsProgress
{
    public List<SongProgress> songs = new List<SongProgress>();
}

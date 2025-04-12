using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using TMPro;
using System.Threading.Tasks;
using Melanchall.DryWetMidi.Core;

public class MidiFileManager : MonoBehaviour
{
    public GameObject lastPlayedSongContainer;
    public GameObject lastPlayedSongGameObject;
    public GameObject songContainerPrefab; // A UI prefab containing NoteImage, SongAuthor, and SongTitle
    public GameObject scoreBoard;

    public GameObject songNameText;
    public GameObject accuracyAccompanimentScoreText;
    public GameObject accuracyMelodyText;
    public GameObject accuracyOverallText;

    public PanelManagerSongList panelManagerSongList;

    public Transform contentPanel; // The content panel of the scroll view to hold song containers
    public MidiFileNoteReader midiPlayer; // Reference to the MidiPlayer component
    private string rootPath = Path.Combine(Application.streamingAssetsPath, "MidiFiles");

    private List<GameObject> songContainers = new List<GameObject>(); // Store all song containers
    private Dictionary<string, (string accompaniment, string melody)> songFilePaths = new Dictionary<string, (string, string)>();

    public Color normalColor;
    public Color highlightColor;
    public Color yellowStar;
    public Color grayStar;

    private Coroutine blinkRoutine;
    private string lastClickedSongKey; // Key format: "Author-SongName"
    private Image lastSelectedContainerImage;
    public Color blinkColor;

    public async void LogMidiFilesAsync()
    {
        Debug.Log("Scanning MIDI files...");

        if (Directory.Exists(rootPath))
        {
            var songFolders = await Task.Run(() =>
            {
                return Directory.GetDirectories(rootPath, "*", SearchOption.AllDirectories)
                    .SelectMany(authorFolder => Directory.GetDirectories(authorFolder))
                    .ToList();
            });

            foreach (var songFolder in songFolders)
            {
                DetectMidiFilesInSongFolder(songFolder);
            }

            LoadLastPlayedSong();
        }
        else
        {
            Debug.LogError("MidiFiles directory not found in StreamingAssets.");
        }
    }

    private void DetectMidiFilesInSongFolder(string songFolder)
    {
        string author = ExtractAuthorFromPath(songFolder);
        string songName = ExtractSongFromPath(songFolder);
        string songKey = $"{author}-{songName}";

        string accompanimentPath = Directory.GetFiles(songFolder, $"Accompaniment{songName}.midi", SearchOption.TopDirectoryOnly).FirstOrDefault()
                                   ?? Directory.GetFiles(songFolder, $"Accompaniment{songName}.mid", SearchOption.TopDirectoryOnly).FirstOrDefault();
        string melodyPath = Directory.GetFiles(songFolder, $"Melody{songName}.midi", SearchOption.TopDirectoryOnly).FirstOrDefault()
                            ?? Directory.GetFiles(songFolder, $"Melody{songName}.mid", SearchOption.TopDirectoryOnly).FirstOrDefault();

        if (accompanimentPath == null || melodyPath == null)
        {
            Debug.LogWarning($"Missing one or more MIDI files in {songFolder}. Skipping...");
            return;
        }

        songFilePaths[songKey] = (accompanimentPath, melodyPath);
        CreateSongContainer(songKey, author, songName);
    }

    private void CreateSongContainer(string songKey, string author, string songName)
    {
        GameObject container = Instantiate(songContainerPrefab, contentPanel);
        container.SetActive(true);
        container.name = songKey;

        container.transform.Find("Image/SongAuthor").GetComponent<TextMeshProUGUI>().text = author != "Unknown" ? author : string.Empty;
        container.transform.Find("Image/SongTitle").GetComponent<TextMeshProUGUI>().text = songName;


        Image[] images = container.transform.Find("StarRow").GetComponentsInChildren<Image>();
        SetStarColors(container.name, images);

        Button button = container.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(() =>
            {
                OnContainerClicked(container);
                ShowScoreBoardFromSavedData($"{author}-{songName}");
                lastClickedSongKey = songKey;
                PlaySong(songName, author);
            });
        }

        songContainers.Add(container);
    }

    public void SetStarColors(string songId, Image[] stars)
    {
        Debug.Log("Setting stars for song: " + songId);

        float highScore = SongProgressManager.Instance.GetOverallProgress(songId);

        Image leftStar = stars.FirstOrDefault(i => i.name == "Star1");
        Image middleStar = stars.FirstOrDefault(i => i.name == "Star2");
        Image rightStar = stars.FirstOrDefault(i => i.name == "Star3");

        if (leftStar) leftStar.color = highScore >= 70 ? yellowStar : grayStar;
        if (middleStar) middleStar.color = highScore >= 95 ? yellowStar : grayStar;
        if (rightStar) rightStar.color = highScore >= 80 ? yellowStar : grayStar;
    }

    private void ShowScoreBoardFromSavedData(string songId)
    {
        MainThreadDispatcher.Enqueue(() =>
        {
            scoreBoard.SetActive(true);

            SongProgress progress = SongProgressManager.Instance.GetSongProgress(songId);
            songNameText.GetComponent<TextMeshProUGUI>().text = songId;

            if (progress != null)
            {
                accuracyAccompanimentScoreText.GetComponent<TextMeshProUGUI>().text = $"{progress.leftHandScore * 100}%";
                accuracyMelodyText.GetComponent<TextMeshProUGUI>().text = $"{progress.rightHandScore * 100}%";
                accuracyOverallText.GetComponent<TextMeshProUGUI>().text = $"{SongProgressManager.Instance.GetOverallProgress(songId) * 100}%";
            } else
            {
                accuracyAccompanimentScoreText.GetComponent<TextMeshProUGUI>().text = $"0%";
                accuracyMelodyText.GetComponent<TextMeshProUGUI>().text = $"0%";
                accuracyOverallText.GetComponent<TextMeshProUGUI>().text = $"0%";
            }

            SetStarColors(songId, GameObject.Find("StarRowScoring").GetComponentsInChildren<Image>());
        });
    }


    public void ShowScoreBoardWithCurrentData(string songId, float accompanimentScore, float melodyScore, float overallScore)
    {
        MainThreadDispatcher.Enqueue(() =>
        {
            scoreBoard.SetActive(true);

            songNameText.GetComponent<TextMeshProUGUI>().text = songId;
            accuracyAccompanimentScoreText.GetComponent<TextMeshProUGUI>().text = $"{accompanimentScore * 100}%";
            accuracyMelodyText.GetComponent<TextMeshProUGUI>().text = $"{melodyScore * 100}%";
            accuracyOverallText.GetComponent<TextMeshProUGUI>().text = $"{overallScore * 100}%";

            SetStarColors(songId, GameObject.Find("StarRowScoring").GetComponentsInChildren<Image>());
        });
    }

    public void SaveLastPlayedSongToPlayerPrefs()
    {
        PlayerPrefs.SetString("LastPlayedSong", lastClickedSongKey);
    }

    public void LoadLastPlayedSong()
    {
        string lastPlayedKey = PlayerPrefs.GetString("LastPlayedSong", null);
        if (string.IsNullOrEmpty(lastPlayedKey) || !songFilePaths.ContainsKey(lastPlayedKey))
        {
            lastPlayedSongGameObject.SetActive(false);
            return;
        }

        lastPlayedSongGameObject.SetActive(true);
        string[] parts = lastPlayedKey.Split('-');
        if (parts.Length < 2) return;

        string author = parts[0];
        string songName = parts[1];

        lastPlayedSongContainer.transform.Find("Image/SongAuthor").GetComponent<TextMeshProUGUI>().text = author != "Unknown" ? author : string.Empty;
        lastPlayedSongContainer.transform.Find("Image/SongTitle").GetComponent<TextMeshProUGUI>().text = songName;
        lastPlayedSongContainer.name = lastPlayedKey;

        Button button = lastPlayedSongContainer.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(() =>
            {
                OnContainerClicked(lastPlayedSongContainer);
                lastClickedSongKey = lastPlayedKey;
                ShowScoreBoardFromSavedData($"{author}-{songName}");
                PlaySong(songName, author);
            });
        }

        Image[] images = lastPlayedSongContainer.transform.Find("StarRow").GetComponentsInChildren<Image>();
        SetStarColors(lastPlayedKey, images);
    }

    public void OnStartPlayingClicked()
    {
        StopCoroutine(blinkRoutine);
        blinkRoutine = null;
    }

    private void OnContainerClicked(GameObject clickedContainer)
    {
        if (lastSelectedContainerImage != null)
        {
            lastSelectedContainerImage.color = normalColor;
        }

        Image newImage = clickedContainer.GetComponent<Image>();
        if (newImage != null)
        {
            newImage.color = highlightColor;
            lastSelectedContainerImage = newImage;
        }

        panelManagerSongList.MakePanelVisible(false);

        blinkRoutine = StartCoroutine(BlinkColorRoutine(GameObject.Find("StartPlayingButton").GetComponent<Image>()));

    }

    private System.Collections.IEnumerator BlinkColorRoutine(Image targetImage)
    {
        bool toggle = false;

        while (true)
        {
            toggle = !toggle;
            // Alternate between blinkColor and normalColor
            targetImage.color = toggle ? blinkColor : normalColor;

            yield return new WaitForSeconds(0.5f);
        }
    }

    private string ExtractAuthorFromPath(string folderPath)
    {
        string relativePath = folderPath.Replace(rootPath, "").Trim(Path.DirectorySeparatorChar);
        string[] pathParts = relativePath.Split(Path.DirectorySeparatorChar);
        return (pathParts.Length >= 2) ? pathParts[0] : "Unknown";
    }

    private string ExtractSongFromPath(string folderPath)
    {
        string relativePath = folderPath.Replace(rootPath, "").Trim(Path.DirectorySeparatorChar);
        string[] pathParts = relativePath.Split(Path.DirectorySeparatorChar);
        return (pathParts.Length >= 2) ? pathParts[1] : "Unknown";
    }

    public void PlaySong(string songName, string author)
    {
        midiPlayer.fileName = songName;
        midiPlayer.author = author;
        midiPlayer.PlaybackPreview();
    }
}

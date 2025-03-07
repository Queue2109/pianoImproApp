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
    public Transform contentPanel; // The content panel of the scroll view to hold song containers
    public MidiFileNoteReader midiPlayer; // Reference to the MidiPlayer component
    string rootPath = Path.Combine(Application.streamingAssetsPath, "MidiFiles");

    private List<GameObject> songContainers = new List<GameObject>(); // Store all song 

    public Color normalColor;
    public Color highlightColor;

    public Color yellowStar;
    public Color grayStar;

    private Coroutine blinkRoutine;
    private string lastClickedSong;

    // Keep track of the last selected container’s Image, to revert color when a new container is clicked
    private Image lastSelectedContainerImage;
    public Color blinkColor;


    public async void LogMidiFilesAsync()
    {
        Debug.Log("In the Log Midi files function");
        if (Directory.Exists(rootPath))
        {
            var midiFiles = await Task.Run(() =>
            {
                var files = new List<string>();
                files.AddRange(Directory.GetFiles(rootPath, "*.midi", SearchOption.AllDirectories));
                files.AddRange(Directory.GetFiles(rootPath, "*.mid", SearchOption.AllDirectories));
                return files;
            });

            foreach (var midiFile in midiFiles)
            {
                CreateSongContainer(midiFile);
            }
            LoadLastPlayedSong();
        }
        else
        {
            Debug.LogError("MidiFiles directory not found in StreamingAssets.");
        }
    }

    private void CreateSongContainer(string midiFile)
    {
        string author = ExtractAuthorFromPath(midiFile, rootPath);
        string fileName = Path.GetFileNameWithoutExtension(midiFile);

        GameObject container = Instantiate(songContainerPrefab, contentPanel);
        container.SetActive(true);
        container.name = $"{author}-{fileName}";

        container.transform.Find("Image/SongAuthor").GetComponent<TextMeshProUGUI>().text = author != "Unknown" ? author : string.Empty;
        container.transform.Find("Image/SongTitle").GetComponent<TextMeshProUGUI>().text = fileName;

        SetStarColors(container.name, container.transform.Find("StarRow"));

        Button button = container.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(() =>
            {
                OnContainerClicked(container);
                PlaySong(fileName, author);
                lastClickedSong = midiFile;
            });
        }

        songContainers.Add(container);
    }

    public void SetStarColors(string songId, Transform songRow)
    {
        Debug.Log("Song id is in midifilemanager " + songId);

        bool leftCleared = SongProgressManager.Instance.IsSongCleared(songId, "Left");
        bool rightCleared = SongProgressManager.Instance.IsSongCleared(songId, "Right");
        bool bothCleared = SongProgressManager.Instance.IsSongCleared(songId, "Both");
        Image[] images = songRow.GetComponentsInChildren<Image>();

        // Assuming images are only Star1, Star2, Star3 in the correct order
        // or you can find by name:
        Image leftStar = images.FirstOrDefault(i => i.name == "Star1");
        Image middleStar = images.FirstOrDefault(i => i.name == "Star2");
        Image rightStar = images.FirstOrDefault(i => i.name == "Star3");

        // Left star if left-hand mode is cleared
        leftStar.color = leftCleared ? yellowStar : grayStar;

        // Middle star if both-hands mode is cleared
        middleStar.color = bothCleared ? yellowStar : grayStar;

        // Right star if right-hand mode is cleared
        rightStar.color = rightCleared ? yellowStar : grayStar;
    }

    public void SaveLastPlayedSongToPlayerPrefs()
    {
        PlayerPrefs.SetString("LastPlayedSong", lastClickedSong);
    }

    private void LoadLastPlayedSong()
    {
        string file = PlayerPrefs.GetString("LastPlayedSong");
        if(file == null) {
            return;
        }
        lastPlayedSongGameObject.SetActive(true);   
        string fileName = Path.GetFileNameWithoutExtension(file);
        string author = ExtractAuthorFromPath(file, rootPath);

        bool isRightCleared = SongProgressManager.Instance.IsSongCleared($"{author}-{fileName}", "Right");
        if (isRightCleared)
        {
            // Show a check mark, or color the UI element differently
        }
        lastPlayedSongContainer.transform.Find("Image/SongAuthor").GetComponent<TextMeshProUGUI>().text = author != "Unknown" ? author : string.Empty;
        lastPlayedSongContainer.transform.Find("Image/SongTitle").GetComponent<TextMeshProUGUI>().text = fileName;
        lastPlayedSongContainer.name = $"{author}-{fileName}";
        Button button = lastPlayedSongContainer.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(() =>
            {
                OnContainerClicked(lastPlayedSongContainer);
                PlaySong(fileName, author);
            });
        }
        SetStarColors($"{author}-{fileName}", lastPlayedSongContainer.transform.Find("StarRow"));
    }

    public void OnStartPlayingClicked()
    {
        GameObject.Find("CurrentlyPlayingText").GetComponent<TextMeshProUGUI>().text = "Song started playing";
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
        GameObject.Find("CurrentlyPlayingText").GetComponent<TextMeshProUGUI>().text = "Playing preview. Press the button to show all features";
        GameObject.Find("StartPlayingButton").gameObject.SetActive(true);
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

    private string ExtractAuthorFromPath(string filePath, string rootPath)
    {
        string relativePath = filePath.Replace(rootPath, string.Empty).Trim(Path.DirectorySeparatorChar);
        string[] pathParts = relativePath.Split(Path.DirectorySeparatorChar);

        foreach (var part in pathParts)
        {
            Debug.Log("Part is " + part);
            return part;
        }

        return "Unknown";
    }

    public void PlaySong(string fileName, string author)
    {
        midiPlayer.fileName = fileName;
        midiPlayer.author = author;
        midiPlayer.PlaybackPreview();
    }
}

using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using TMPro;
using System.Threading.Tasks;

public class MidiFileManager : MonoBehaviour
{
    public GameObject songContainerPrefab; // A UI prefab containing NoteImage, SongAuthor, and SongTitle
    public Transform contentPanel; // The content panel of the scroll view to hold song containers
    public MidiFileNoteReader midiPlayer; // Reference to the MidiPlayer component

    private List<GameObject> songContainers = new List<GameObject>(); // Store all song 

    public Color normalColor;
    public Color highlightColor;

    // Keep track of the last selected container’s Image, to revert color when a new container is clicked
    private Image lastSelectedContainerImage;
    public Color blinkColor;


    public async void LogMidiFilesAsync()
    {
        Debug.Log("In the Log Midi files function");
        string rootPath = Path.Combine(Application.streamingAssetsPath, "MidiFiles");
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
                CreateSongContainer(midiFile, rootPath);
            }
        }
        else
        {
            Debug.LogError("MidiFiles directory not found in StreamingAssets.");
        }
    }

    private void CreateSongContainer(string midiFile, string rootPath)
    {
        string author = ExtractAuthorFromPath(midiFile, rootPath);
        string fileName = Path.GetFileNameWithoutExtension(midiFile);

        GameObject container = Instantiate(songContainerPrefab, contentPanel);
        container.SetActive(true);

        container.transform.Find("Image/SongAuthor").GetComponent<TextMeshProUGUI>().text = author != "Unknown" ? author : string.Empty;
        container.transform.Find("Image/SongTitle").GetComponent<TextMeshProUGUI>().text = fileName;

        Button button = container.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(() =>
            {
                OnContainerClicked(container);
                PlaySong(fileName, author);
            });
        }

        songContainers.Add(container);
    }

    public void OnStartPlayingClicked()
    {
        StopCoroutine(BlinkColorRoutine(GameObject.Find("StartPlayingButton").GetComponent<Image>()));
        lastSelectedContainerImage = null;
        GameObject.Find("StartPlayingText").GetComponent<TextMeshProUGUI>().text = "Choose another song to switch it up!";
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
        StartCoroutine(BlinkColorRoutine(GameObject.Find("StartPlayingButton").GetComponent<Image>()));
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
            return part;
        }

        return "Unknown";
    }

    public void PlaySong(string fileName, string author)
    {
        midiPlayer.fileName = fileName;
        midiPlayer.PlaybackPreview();
    }
}

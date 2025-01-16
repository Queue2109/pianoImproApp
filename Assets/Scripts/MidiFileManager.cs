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

    private HashSet<string> knownAuthors = new HashSet<string>
    {
        // List of known authors
        "Adam Birnbaum", "Adam Makowicz", "Alan Broadbent", "Alan Farnham", "Alan Pasqua", "Andy Laverne", "Barry Harris",
        "Bill Charlap", "Bill Cunliffe", "Bill Evans", "Bill Mays", "Billy Taylor", "Brad Mehldau", "Bobby Timmons",
        "Buddy Montgomery", "Cedar Walton", "Chick Corea", "Dave McKenna", "David Berkman", "Denny Zeitlin",
        "Dick Hyman", "Don Friedman", "Donald Brown", "Duke Jordan", "Earl Hines", "Edward Simon", "Ellis Larkins",
        "Ellis Marsalis", "Eric Reed", "Erroll Garner", "Ethan Iverson", "Fred Hersch", "Gene Harris", "Geoffrey Keezer",
        "George Cables", "George Colligan", "George Shearing", "Gerald Clayton", "Geri Allen", "Hal Galper", "Hank Jones",
        "Harold Mabern", "Herbie Hancock", "Hiromi", "Jacky Terrasson", "Jaki Byard", "Jason Moran", "Jessica Williams",
        "Jim McNeely", "Joanne Brackeen", "John Campbell", "John Colianni", "John Hicks", "John Taylor", "Johnny O'Neal",
        "Junior Mance", "Justin Kauflin", "Keith Jarrett", "Kenny Barron", "Kenny Drew", "Kenny Drew Jr", "Kenny Werner",
        "Kirk Lightsey", "Larry Goldings", "Lennie Tristano", "Marian McPartland", "Marcus Roberts", "Makoto Ozone",
        "Mary Lou Williams", "McCoy Tyner", "Michel Camilo", "Michel Legrand", "Michel Petrucciani", "Mike Wofford",
        "Mulgrew Miller", "Monty Alexander", "Oscar Peterson", "Phineas Newborn Jr", "Ramsey Lewis", "Randy Weston",
        "Ray Bryant", "Red Garland", "Renee Rosnes", "Richard Beirach", "Robi Botos", "Roger Kellaway", "Roland Hanna",
        "Ryo Fukui", "Stanley Cowell", "Steve Kuhn", "Teddy Wilson", "Tete Montoliu", "Thelonious Monk", "Tigran Hamasyan",
        "Tommy Flanagan", "Vijay Iyer", "Walter Norris"
    };

    private List<GameObject> songContainers = new List<GameObject>(); // Store all song 

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
            button.onClick.AddListener(() => PlaySong(fileName, author));
        }

        songContainers.Add(container);
    }

    public void openKeyboard()
    {
        TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default);
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

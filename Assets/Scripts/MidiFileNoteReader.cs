using UnityEngine;
using System.Collections;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using System.IO;
using UnityEngine.UI;
using TMPro;
using Melanchall.DryWetMidi.Interaction;
using System.Threading;
using System.Linq;

public class MidiFileNoteReader : MonoBehaviour
{
    private IOutputDevice outputDevice;
    private Playback playback;
    public bool visualizeNotes = true;
    private PianoFunctions pianoFunctions;
    public Slider slider;
    private TextMeshProUGUI timeText;
    private TextMeshProUGUI songName;
    private float playbackSpeed = 1f;
    private long currentPlaybackTime;
    public PanelManagerSongList panelManagerSongList;

    private TempoMap tempoMap;
    private bool[] channelSelections;
    private bool isPlaying = false;

    void Start()
    {

        pianoFunctions = GetComponent<PianoFunctions>();
        if (pianoFunctions == null)
        {
            Debug.LogError("PianoFunctions script not found on the GameObject.");
        }

        // Initialize channel selections
        channelSelections = new bool[16]; // Assuming 16 channels
        for (int i = 0; i < channelSelections.Length; i++)
        {
            channelSelections[i] = true; // By default, all channels are selected
        }
    }

    void Update()
    {
        if (isPlaying && playback != null && slider != null)
        {
            double currentTime = playback.GetCurrentTime<MetricTimeSpan>().TotalSeconds;
            double totalTime = playback.GetDuration<MetricTimeSpan>().TotalSeconds;

            // Calculate the normalized progress
            float progress = (float)(currentTime / totalTime);

            // Update the slider and time text
            slider.value = progress;
            timeText.text = FormatTime(playback.GetCurrentTime<MetricTimeSpan>()) + " / " + FormatTime(playback.GetDuration<MetricTimeSpan>());
        }
    }

    public static string FindMidiFile(string fileName, string rootPath)
    {
        var files = Directory.GetFiles(rootPath, "*.mid", SearchOption.AllDirectories).Concat(Directory.GetFiles(rootPath, "*.midi", SearchOption.AllDirectories));

        foreach (var file in files)
        {
            if (Path.GetFileNameWithoutExtension(file).Equals(fileName, System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log("Found file: " + file);
                return file;
            }
        }
        return null;
    }


    public void PlayMidi(string fileName, string author)
    {
        StopPlayback();
        var filePath = FindMidiFile(fileName, Path.Combine(Application.streamingAssetsPath, "MidiFiles"));
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            Debug.LogError("MIDI file not found: " + filePath);
            return;
        }
        panelManagerSongList.filePath = filePath;
        MidiInstrumentChecker.CheckInstruments(filePath);
        Debug.Log("Reading MIDI file: " + filePath);
        MidiFile midiFile = MidiFile.Read(filePath);
        tempoMap = midiFile.GetTempoMap();
        outputDevice = OutputDevice.GetByName("Microsoft GS Wavetable Synth");

        if (outputDevice == null)
        {
            Debug.LogError("Output device not found.");
            return;
        }
        if(songName != null)
        songName.text = author == "Unknown" ? fileName : author + " - " + fileName;

        playback = midiFile.GetPlayback(outputDevice);
        playback.NotesPlaybackStarted += OnNotesPlaybackStarted;
        playback.NotesPlaybackFinished += OnNotesPlaybackFinished;
        //playback.Finished += OnPlaybackFinished;
        playback.Speed = playbackSpeed;
        playback.Start();
        isPlaying = true;
    }

    public void TogglePlayPause()
    {
        if (playback == null)
        {
            Debug.LogError("Playback is not initialized.");
            return;
        }

        if (isPlaying)
        {
            playback.Stop();
        }
        else
        {
            playback.Start();
        }
        isPlaying = !isPlaying;
    }

    public void SpeedUp()
    {
        playbackSpeed += 0.1f;
        if (playback != null)
        {
            playback.Speed = playbackSpeed;
        }
    }

    public void SlowDown()
    {
        playbackSpeed = Mathf.Max(0.1f, playbackSpeed - 0.1f);
        if (playback != null)
        {
            playback.Speed = playbackSpeed;
        }
    }

    public void Seek()
    {
        float value = slider.value;
        if (playback != null)
        {
            playback.MoveToTime(new MetricTimeSpan(0, 0, (int)value));
        }
    }

    public void SetChannelSelections(bool[] selections)
    {
        channelSelections = selections;
    }

    private string FormatTime(MetricTimeSpan timeSpan)
    {
        return string.Format("{0:D2}:{1:D2}", timeSpan.Minutes, timeSpan.Seconds);
    }

    private IEnumerator PlayMidiCoroutine()
    {
        playback.Start();
        Debug.Log("Playback has started");
        while (playback.IsRunning)
        {
            yield return null;
        }

        Debug.Log("Playback coroutine finished");
        playback.Stop();
    }

    private void OnNotesPlaybackStarted(object sender, NotesEventArgs e)
    {
        MainThreadDispatcher.Enqueue(() =>
        {
            foreach (var note in e.Notes)
            {
                if (MidiInstrumentChecker.selectedChannels.Contains(note.Channel))
                {
                    if (visualizeNotes)
                    {
                        var keyName = pianoFunctions.NoteNameToKeyName(note.NoteName.ToString(), note.Octave.ToString());
                        pianoFunctions.ColorKey(keyName);
                    }
                }
            }
        });
    }

    private void OnNotesPlaybackFinished(object sender, NotesEventArgs e)
    {
        MainThreadDispatcher.Enqueue(() =>
        {
            foreach (var note in e.Notes)
            {
                if (MidiInstrumentChecker.selectedChannels.Contains(note.Channel))
                {
                    if (visualizeNotes)
                    {
                        var keyName = pianoFunctions.NoteNameToKeyName(note.NoteName.ToString(), note.Octave.ToString());
                        pianoFunctions.ResetKeyColor(keyName);
                    }
                }
            }
        });
    }

    private void OnPlaybackFinished(object sender, System.EventArgs e)
    {
        Debug.Log("Playback finished.");
        StopPlayback();
    }

    private void StopPlayback()
    {
        if (playback != null)
        {
            if (playback.IsRunning)
            {
                playback.Stop();
            }
            playback.Dispose();
            playback = null;
            Debug.Log("Playback stopped and disposed.");
        }

        if (outputDevice != null)
        {
            outputDevice.Dispose();
            outputDevice = null;
            Debug.Log("Output device disposed.");
        }
    }

    private void OnDestroy()
    {
        StopPlayback();
    }
}

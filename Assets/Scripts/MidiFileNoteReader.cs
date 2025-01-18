using UnityEngine;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using System.IO;
using UnityEngine.UI;
using TMPro;
using Melanchall.DryWetMidi.Interaction;
using System.Linq;
using System;
using System.Collections.Generic;

public class MidiFileNoteReader : MonoBehaviour
{
    //private IOutputDevice outputDevice;
    private Playback playback;
    public PianoFunctions pianoFunctions;
    public Slider slider;
    private TextMeshProUGUI timeText;
    private TextMeshProUGUI speedText;
    private float playbackSpeed = 1f;
    public PanelManagerSongList panelManagerSongList;

    private bool[] channelSelections;
    private bool isPlaying = false;
    public string fileName = "";
    public string author = "";
    public GameObject logoPause;
    public GameObject logoPlay;
    public AudioClip clip;
    private OutputDevice outputDevice;

    private MidiFile fullFile;
    private MidiFile leftHandFile;
    private MidiFile rightHandFile;
    double totalTime;
    MetricTimeSpan totalDuration;
    MetricTimeSpan currentTime;

    public TextMeshProUGUI songName;

    private bool colorLeftHand = true;
    private bool colorRightHand = true;

    public void Setup()
    {   
        outputDevice = OutputDevice.GetAll().FirstOrDefault();
        if (outputDevice == null)
        {
            Debug.LogError("No output device found.");
            return;
        }
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

        timeText = GameObject.Find("Time").GetComponent<TextMeshProUGUI>();
        speedText = GameObject.Find("Speed").GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        if (panelManagerSongList.currentPanel == 2 && isPlaying)
        {
            currentTime = playback.GetCurrentTime<MetricTimeSpan>();

            // Calculate the normalized progress
            float progress = (float)(currentTime.TotalSeconds / totalTime);

            // Update the slider and time text
            slider.value = progress;
            timeText.text = FormatTime(playback.GetCurrentTime<MetricTimeSpan>()) + " / " + FormatTime(totalDuration);
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

    public void PlayMidiFunction()
    {
        CacheFilteredFiles();
        switch (currentMode)
        {
            case PlaybackMode.LeftHand:
                PlayLeftHandOnly();
                break;
            case PlaybackMode.RightHand:
                PlayRightHandOnly();
                break;
            default:
                PlayBothHandsPlayback(); // Play full accompaniment (both hands)
                break;
        }

        UpdatePlayPauseButtons();
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
            isPlaying = false;
            Debug.Log("Playback stopped.");
        }
        else
        {
            ResumePlayback(); // Resume based on current mode
            isPlaying = true;
            Debug.Log("Playback started.");
        }
        UpdatePlayPauseButtons();
    }

    private void UpdatePlayPauseButtons()
    {
        if (logoPlay != null && logoPause != null)
        {
            if (isPlaying)
            {
                logoPlay.SetActive(false);
                logoPause.SetActive(true);
            }
            else
            {
                logoPlay.SetActive(true);
                logoPause.SetActive(false);
            }
        }
    }

    public void Rewind()
    {
        if (playback != null)
        {
            // Calculate new time, 10 seconds back, but not less than 0
            double newTimeInSeconds = Mathf.Max(0, (float)(currentTime.TotalSeconds - 10));
            currentTime = new MetricTimeSpan(0, 0, (int)newTimeInSeconds);

            // Move the playback to the new time and update the slider
            playback?.MoveToTime(currentTime);
            UpdateUI();
        }
    }

    public void FastForward()
    {
        if (playback != null)
        {
            // Calculate new time, 10 seconds forward, but not more than the total time
            double newTimeInSeconds = Mathf.Min((float)totalTime, (float)(currentTime.TotalSeconds + 10));
            currentTime = new MetricTimeSpan(0, 0, (int)newTimeInSeconds);

            // Move the playback to the new time and update the slider
            playback?.MoveToTime(currentTime);
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        // Calculate the normalized progress
        float progress = (float)(currentTime.TotalSeconds / totalTime);

        // Update the slider and time text
        slider.value = progress;
        timeText.text = FormatTime(currentTime) + " / " + FormatTime(totalDuration);
    }


    public void SpeedUp()
    {
        playbackSpeed += 0.1f;
        if (playback != null)
        {
            playback.Speed = playbackSpeed;
            playback?.MoveToTime(currentTime);
            UpdateSpeedTextUI();
        }
    }

    public void SlowDown()
    {
        playbackSpeed = Mathf.Max(0.1f, playbackSpeed - 0.1f);
        if (playback != null)
        {
            playback.Speed = playbackSpeed;
            playback?.MoveToTime(currentTime);
            UpdateSpeedTextUI();
        }
    }

    private void UpdateSpeedTextUI()
    {
        speedText.text = "Speed: " + playbackSpeed.ToString("0.0x");
    }

    private string FormatTime(MetricTimeSpan timeSpan)
    {
        return string.Format("{0:D2}:{1:D2}", timeSpan.Minutes, timeSpan.Seconds);
    }

    public void ColorLeftKeys()
    {
        this.colorLeftHand = true;
        this.colorRightHand = false;

    }

    public void ColorRightKeys()
    {
        this.colorLeftHand = false;
        this.colorRightHand = true;
    }

    public void ColorAllKeys()
    {
        this.colorLeftHand = true;
        this.colorRightHand = true;

    }

    private MidiFile FilterNotes(MidiFile midiFile, bool isLeftHand, TempoMap tempoMap)
    {
        var filteredFile = midiFile.Clone();
        foreach (var trackChunk in filteredFile.GetTrackChunks())
        {
            var eventsToRemove = new HashSet<MidiEvent>();

            foreach (var midiEvent in trackChunk.Events)
            {
                if (midiEvent is NoteOnEvent noteOnEvent)
                {
                    bool shouldKeep = isLeftHand
                        ? noteOnEvent.NoteNumber < 60
                        : noteOnEvent.NoteNumber >= 60;

                    if (!shouldKeep)
                    {
                        eventsToRemove.Add(noteOnEvent);

                        var correspondingNoteOff = trackChunk.Events
                            .OfType<NoteOffEvent>()
                            .FirstOrDefault(e => e.NoteNumber == noteOnEvent.NoteNumber);

                        if (correspondingNoteOff != null)
                        {
                            eventsToRemove.Add(correspondingNoteOff);
                        }
                    }
                }
            }

            foreach (var eventToRemove in eventsToRemove)
            {
                trackChunk.Events.Remove(eventToRemove);
            }
        }

        return filteredFile;
    }

    private void CacheFilteredFiles()
    {
        string filePath = FindMidiFile(fileName, Path.Combine(Application.streamingAssetsPath, "MidiFiles"));
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            Debug.LogError("MIDI file not found: " + filePath);
            return;
        }

        var midiFile = MidiFile.Read(filePath);
        var tempoMap = midiFile.GetTempoMap();

        // Filter for different modes
        fullFile = midiFile.Clone();
        leftHandFile = FilterNotes(midiFile, isLeftHand: true, tempoMap: tempoMap);
        rightHandFile = FilterNotes(midiFile, isLeftHand: false, tempoMap: tempoMap);

        Debug.Log("Filtered files cached successfully.");
    }

    public void PlayLeftHandOnly()
    {
        if (playback != null && currentMode == PlaybackMode.LeftHand)
        {
            Debug.Log("Already playing left-hand only.");
            return;
        }
        TogglePlayPause();
        playback?.Dispose();
        playback = leftHandFile?.GetPlayback(outputDevice);

        if (playback != null)
        {
            playback.Speed = playbackSpeed;
            playback?.MoveToTime(currentTime);
            currentMode = PlaybackMode.LeftHand;

            Debug.Log("Playing left-hand only.");
        }
    }

    public void PlayRightHandOnly()
    {
        if (playback != null && currentMode == PlaybackMode.RightHand)
        {
            Debug.Log("Already playing right-hand only.");
            return;
        }

        TogglePlayPause();
        playback?.Dispose();
        playback = rightHandFile?.GetPlayback(outputDevice);

        if (playback != null)
        {
            playback.Speed = playbackSpeed;
            playback?.MoveToTime(currentTime);
            currentMode = PlaybackMode.RightHand;

            Debug.Log("Playing right-hand only.");
        }
    }

    public void PlayBothHandsPlayback()
    {
        if (playback != null && currentMode == PlaybackMode.Full)
        {
            Debug.Log("Already playing both hands.");
            return;
        }

        TogglePlayPause();
        playback?.Dispose();
        playback = fullFile?.GetPlayback(outputDevice);

        if (playback != null)
        {
            playback.Speed = playbackSpeed;
            playback?.MoveToTime(currentTime);

            // Update total time and duration
            totalTime = playback.GetDuration<MetricTimeSpan>().TotalSeconds;
            totalDuration = playback.GetDuration<MetricTimeSpan>();

            currentMode = PlaybackMode.Full;

            Debug.Log("Playing both hands.");
        }
    }
    private void GetFileForPlayback()
    {
        // Get the file path
        var filePath = FindMidiFile(fileName, Path.Combine(Application.streamingAssetsPath, "MidiFiles"));
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            Debug.LogError($"MIDI file not found: {filePath}. Ensure the fileName '{fileName}' is correct and the file exists in the specified directory.");
            return;
        }

        Debug.Log($"Reading MIDI file for preview: {filePath}");
        // Read the MIDI file
        MidiFile midiFile;
        try
        {
            midiFile = MidiFile.Read(filePath);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error reading MIDI file: {ex.Message}");
            return;
        }

        // Initialize or reuse the output device
        outputDevice ??= OutputDevice.GetAll().FirstOrDefault();
        if (outputDevice == null)
        {
            Debug.LogError("No output device found. Ensure a valid MIDI output device is available.");
            return;
        }

        // Create playback
        playback = midiFile.GetPlayback(outputDevice);
    }

    public void PlaybackPreview()
    {
        playback?.Dispose();
        GetFileForPlayback();
        CacheFilteredFiles();

        currentMode = PlaybackMode.Full;

        // Attach event handlers
        playback.NotesPlaybackStarted += OnNotesPlaybackStarted;
        playback.NotesPlaybackFinished += OnNotesPlaybackFinished;
        playback.Finished += OnPlaybackFinished;

        // Set playback speed and start
        playback.Speed = 1f; // Default speed for preview
        playback?.Start();

        // Debugging
        Debug.Log("Playback preview started successfully.");
    }

    public void StartPlaybackFromBeginning()
    {
        playback?.Stop();
        playback.MoveToTime(new MetricTimeSpan());
        songName.text = $"{author} {fileName}";


        // Set playback speed and start from the beginning
        playback.Speed = playbackSpeed;
        playback.Start();

        // Update total time and duration
        totalTime = playback.GetDuration<MetricTimeSpan>().TotalSeconds;
        totalDuration = playback.GetDuration<MetricTimeSpan>();

        // Set the mode to Full and update play state
        currentMode = PlaybackMode.Full;
        isPlaying = true;
        UpdatePlayPauseButtons();

        Debug.Log("Playback started from the beginning.");
    }

    public void ResumePlayback()
    {
        playback?.MoveToTime(currentTime);
        playback?.Start();
    }


    private void OnNotesPlaybackStarted(object sender, NotesEventArgs e)
    {
        MainThreadDispatcher.Enqueue(() =>
        {
            foreach (var note in e.Notes)
            {

                // Debug.Log("Channel of the note and channel in the selectedChannels: " + note.Channel + " in selected: " + MidiInstrumentChecker.selectedChannels.ToArray());
                if (MidiInstrumentChecker.selectedChannels.Contains(note.Channel))
                {
                    if (panelManagerSongList.currentPanel == 2)
                    {
                        var keyName = pianoFunctions.NoteNameToKeyName(note.NoteName.ToString(), note.Octave.ToString());
                        if(colorLeftHand && note.NoteNumber < 60 || colorRightHand && note.NoteNumber >= 60)
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
                    if (panelManagerSongList.currentPanel == 2)
                    {
                        var keyName = pianoFunctions.NoteNameToKeyName(note.NoteName.ToString(), note.Octave.ToString());
                        if (colorLeftHand && note.NoteNumber < 60 || colorRightHand && note.NoteNumber >= 60)
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

    public void StopPlayback()
    {
        if (playback != null)
        {
            if (playback.IsRunning)
            {
                playback?.Stop();
            }
            playback?.Dispose();
            playback = null;
            Debug.Log("Playback stopped and disposed.");
        }

        if (outputDevice != null)
        {
            outputDevice?.Dispose();
            outputDevice = null;
            Debug.Log("Output device disposed.");
        }

        isPlaying = false; // Reset play state
        UpdatePlayPauseButtons(); // Sync buttons
    }

    private void OnDestroy()
    {
        StopPlayback();
    }

    private enum PlaybackMode
    {
        Full,
        LeftHand,
        RightHand
    }

    private PlaybackMode currentMode = PlaybackMode.Full;

}

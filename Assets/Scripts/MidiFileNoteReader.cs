using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;
using System.Linq;
using System.IO;
using System;
using System.Collections.Generic;
using Melanchall.DryWetMidi.Common;

public class MidiFileNoteReader : MonoBehaviour
{
    // References to other scripts / UI
    public HttpHandler httpHandler;
    public PianoFunctions pianoFunctions;
    public PanelManagerSongList panelManagerSongList;
    public MidiFileManager midiFileManager;
    private MidiInstrumentChecker instrumentChecker = new MidiInstrumentChecker();
    private ScoringLogic scoringManager = new ScoringLogic();

    [Header("UI")]
    public Slider slider;
    public GameObject logoPause;
    public GameObject logoPlay;
    public TextMeshPro songName;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI speedText;
    public TextMeshPro playAccompanimentButtonText;
    public TextMeshPro playMelodyButtonText;
    public TextMeshPro colorAccompanimentKeysButtonText;
    public TextMeshPro colorMelodyKeysButtonText;

    private Playback accompanimentPlayback;
    private Playback melodyPlayback;
    private OutputDevice outputDevice;
    private bool isPlaying = false;
    private PlaybackMode currentMode = PlaybackMode.FullSong;
    private float playbackSpeed = 1f;
    private HashSet<int> activeNotes = new HashSet<int>();
    private List<int> channelSelections = new List<int>();
    private bool colorAccompaniment = true;
    private bool colorMelody = true;

    // Filtered MIDI files for each mode
    private MidiFile accompanimentFile;
    private MidiFile melodyFile;

    // Times
    private double totalTime;
    private MetricTimeSpan totalDuration;
    private MetricTimeSpan currentTime;

    // For usage in searching the file
    public string fileName = "";
    public string author = "";

    private bool isSongReady = false;
    private int timesPlayed = 0;

    private enum PlaybackMode
    {
        FullSong,
        Accompaniment,
        Melody
    }

    private List<MidiNoteData> allNotes = new List<MidiNoteData>();   // All notes for scoring
    private float timingWindow = 0.25f; // ±0.25s window to count note as correct

    private int totalLeft = 0;
    private int totalRight = 0;
    private int correctLeft = 0;
    private int correctRight = 0;

    #region Setup / Lifecycle

    void Start()
    {
         httpHandler.scoringManager = scoringManager; // Link scoring logic to HTTP handler
        scoringManager.SetScoringMode(ScoringMode.Melody);
    }
    public void Setup()
    {
        // Make sure the UI references exist
        if (!timeText) timeText = GameObject.Find("Time")?.GetComponent<TextMeshProUGUI>();
        if (!speedText) speedText = GameObject.Find("Speed")?.GetComponent<TextMeshProUGUI>();
    }

    private void Update()
    {
        if (!isSongReady) return;
        if (!isPlaying || melodyPlayback == null || accompanimentPlayback == null) return;
        if (panelManagerSongList != null && panelManagerSongList.currentPanel != 0) return;
        if (!slider || !speedText || !timeText) return;

        // Now safe to update UI
        if(currentMode == PlaybackMode.Accompaniment)
        {
            currentTime = accompanimentPlayback.GetCurrentTime<MetricTimeSpan>();
        } else
        {
            currentTime = melodyPlayback.GetCurrentTime<MetricTimeSpan>();
        }
        float progress = (float)(currentTime.TotalSeconds / totalTime);
        slider.value = progress;
        timeText.text = FormatTime(currentTime) + " / " + FormatTime(totalDuration);
    }

    private void OnDestroy()
    {
        // Ensure resources freed
        StopPlayback();
        DisposeDevice();
    }
    #endregion

    #region Public Methods For UI

    public void StartFullSongPlayback()
    {
        StartPlayback(PlaybackMode.FullSong);
    }
    public void StartMelodyPlayback()
    {
        if (currentMode == PlaybackMode.FullSong)
        {
            StartPlayback(PlaybackMode.Accompaniment);

        }
        else if (currentMode == PlaybackMode.Accompaniment)
        {
            StartPlayback(PlaybackMode.FullSong);
        }
    }

    public void StartAccompanimentPlayback()
    {
        if(currentMode == PlaybackMode.FullSong)
        {
            StartPlayback(PlaybackMode.Melody);

        } else if(currentMode == PlaybackMode.Melody)
        {
            StartPlayback(PlaybackMode.FullSong);
        }
    }

    public void TogglePlayPause()
    {

        if (currentMode == PlaybackMode.FullSong)
        {
            if (accompanimentPlayback?.IsRunning == true)
            {
                accompanimentPlayback.Stop();
                melodyPlayback.Stop();
                isPlaying = false;
            }
            else
            {
                accompanimentPlayback.Start();
                melodyPlayback.Start();
                isPlaying = true;
            }
        }
        else if (currentMode == PlaybackMode.Accompaniment)
        {
            if (accompanimentPlayback?.IsRunning == true)
            {
                accompanimentPlayback.Stop();
                isPlaying = false;
            }
            else
            {
                accompanimentPlayback.Start();
                isPlaying = true;
            }
        }
        else if (currentMode == PlaybackMode.Melody)
        {
            if (melodyPlayback?.IsRunning == true)
            {
                melodyPlayback.Stop();
                isPlaying = false;
            }
            else
            {
                melodyPlayback.Start();
                isPlaying = true;
            }
        }

        UpdatePlayPauseButtons();
    }

    public void Rewind()
    {
        // Pause while rewinding
        if (isPlaying) TogglePlayPause();

        // 10 seconds back
        double newTimeInSec = Math.Max(0, currentTime.TotalSeconds - 10);
        currentTime = new MetricTimeSpan(0, 0, (int)newTimeInSec);
        switch(currentMode)
        {
            case PlaybackMode.Accompaniment:
                accompanimentPlayback.MoveToTime(currentTime);
                break;
            case PlaybackMode.Melody:
                melodyPlayback.MoveToTime(currentTime);
                break;
            case PlaybackMode.FullSong:
                melodyPlayback.MoveToTime(currentTime);
                accompanimentPlayback.MoveToTime(currentTime);
                break;
        }

        UpdateUI();
        TogglePlayPause(); // resume
    }

    public void FastForward()
    {
        // Pause while fast-forwarding
        if (isPlaying) TogglePlayPause();

        // 10 seconds forward
        double newTimeInSec = Math.Min(totalTime, currentTime.TotalSeconds + 10);
        currentTime = new MetricTimeSpan(0, 0, (int)newTimeInSec);
        switch (currentMode)
        {
            case PlaybackMode.Accompaniment:
                accompanimentPlayback.MoveToTime(currentTime);
                break;
            case PlaybackMode.Melody:
                melodyPlayback.MoveToTime(currentTime);
                break;
            case PlaybackMode.FullSong:
                melodyPlayback.MoveToTime(currentTime);
                accompanimentPlayback.MoveToTime(currentTime);
                break;
        }

        UpdateUI();
        TogglePlayPause(); // resume
    }

    public void SpeedUp()
    {
        playbackSpeed += 0.1f;
        switch (currentMode)
        {
            case PlaybackMode.Accompaniment:
                accompanimentPlayback.Speed = playbackSpeed;
                accompanimentPlayback.MoveToTime(currentTime);
                break;
            case PlaybackMode.Melody:
                melodyPlayback.Speed = playbackSpeed;
                melodyPlayback.MoveToTime(currentTime);
                    break;
            case PlaybackMode.FullSong:
                melodyPlayback.Speed = playbackSpeed;
                melodyPlayback.MoveToTime(currentTime);
                accompanimentPlayback.Speed = playbackSpeed;
                accompanimentPlayback.MoveToTime(currentTime);
                break;
        }

        UpdateSpeedTextUI();
    }

    public void SlowDown()
    {
        playbackSpeed = Mathf.Max(0.1f, playbackSpeed - 0.1f);
        switch (currentMode)
        {
            case PlaybackMode.Accompaniment:
                accompanimentPlayback.Speed = playbackSpeed;
                accompanimentPlayback.MoveToTime(currentTime);
                break;
            case PlaybackMode.Melody:
                melodyPlayback.Speed = playbackSpeed;
                melodyPlayback.MoveToTime(currentTime);
                break;
            case PlaybackMode.FullSong:
                melodyPlayback.Speed = playbackSpeed;
                melodyPlayback.MoveToTime(currentTime);
                accompanimentPlayback.Speed = playbackSpeed;
                accompanimentPlayback.MoveToTime(currentTime);
                break;
        }

        UpdateSpeedTextUI();
    }
    private void StartPlayback(PlaybackMode mode)
    {
        Debug.Log("In the startPlayback");
        StopPlayback();
        DisposeDevice();

        outputDevice = OutputDevice.GetAll().FirstOrDefault();
        if (outputDevice == null)
        {
            Debug.LogError("No MIDI output device found. Cannot start playback.");
            return;
        }

        currentMode = mode;

        if (mode == PlaybackMode.FullSong)
        {
            if (accompanimentFile == null || melodyFile == null)
            {
                Debug.LogError("Both Accompaniment and Melody files are required for FullSong mode.");
                return;
            }

            accompanimentPlayback = accompanimentFile.GetPlayback(outputDevice);
            melodyPlayback = melodyFile.GetPlayback(outputDevice);

            // Attach event handlers
            accompanimentPlayback.NotesPlaybackStarted += OnNotesPlaybackStarted;
            accompanimentPlayback.NotesPlaybackFinished += OnNotesPlaybackFinished;
            melodyPlayback.NotesPlaybackStarted += OnNotesPlaybackStarted;
            melodyPlayback.NotesPlaybackFinished += OnNotesPlaybackFinished;
            AttachPlaybackCycleEvents();

            playAccompanimentButtonText.text = "Playing accompaniment: ON";
            playMelodyButtonText.text = "Playing melody: ON";

            accompanimentPlayback.Start();
            melodyPlayback.Start();
            ColorAllKeys();
        }
        else if (mode == PlaybackMode.Accompaniment)
        {
            if (accompanimentFile == null)
            {
                Debug.LogError("Accompaniment file not found.");
                return;
            }
            accompanimentPlayback = accompanimentFile.GetPlayback(outputDevice);
            accompanimentPlayback.NotesPlaybackStarted += OnNotesPlaybackStarted;
            accompanimentPlayback.NotesPlaybackFinished += OnNotesPlaybackFinished;
            AttachPlaybackCycleEvents();

            playAccompanimentButtonText.text = "Playing accompaniment: ON";
            playMelodyButtonText.text = "Playing melody: OFF";

            accompanimentPlayback.Start();
            ColorAccompanimentKeys();
        }
        else if (mode == PlaybackMode.Melody)
        {
            if (melodyFile == null)
            {
                Debug.LogError("Melody file not found.");
                return;
            }
            melodyPlayback = melodyFile.GetPlayback(outputDevice);
            melodyPlayback.NotesPlaybackStarted += OnNotesPlaybackStarted;
            melodyPlayback.NotesPlaybackFinished += OnNotesPlaybackFinished;
            AttachPlaybackCycleEvents();

            playAccompanimentButtonText.text = "Playing accompaniment: OFF";
            playMelodyButtonText.text = "Playing melody: ON";

            melodyPlayback.Start();
            ColorMelodyKeys();
        }

        isPlaying = true;
        isSongReady = true;
        totalTime = accompanimentPlayback?.GetDuration<MetricTimeSpan>().TotalSeconds ?? melodyPlayback.GetDuration<MetricTimeSpan>().TotalSeconds;
        totalDuration = accompanimentPlayback?.GetDuration<MetricTimeSpan>() ?? melodyPlayback.GetDuration<MetricTimeSpan>();

        pianoFunctions.ResetAllKeysToDefaultColor();
        UpdatePlayPauseButtons();
        UpdateUI();
        Debug.Log($"Started playback in {mode} mode.");
    }


    public void StopPlayback()
    {
        if (accompanimentPlayback != null)
        {
            if (accompanimentPlayback.IsRunning) accompanimentPlayback.Stop();
            accompanimentPlayback.Dispose();
            accompanimentPlayback = null;
        }

        if (melodyPlayback != null)
        {
            if (melodyPlayback.IsRunning) melodyPlayback.Stop();
            melodyPlayback.Dispose();
            melodyPlayback = null;
        }

        isPlaying = false;
        isSongReady = false;
        pianoFunctions.ResetSelectedKeysToDefaultColors(activeNotes);
        UpdatePlayPauseButtons();

        Debug.Log("Playback fully stopped.");
    }

    // (Optional) For a quick "preview" approach
    public void PlaybackPreview()
    {
        timesPlayed = 1;
        StopPlayback();
        DisposeDevice();

        outputDevice = OutputDevice.GetAll().FirstOrDefault();
        if (outputDevice == null)
        {
            Debug.LogError("No MIDI output device found. Cannot start playback.");
            return;
        }

        CacheFilteredFiles();

        
        if (accompanimentFile == null || melodyFile == null)
        {
            Debug.LogError("Both Accompaniment and Melody files are required for FullSong mode.");
            return;
        }

        accompanimentPlayback = accompanimentFile.GetPlayback(outputDevice);
        melodyPlayback = melodyFile.GetPlayback(outputDevice);

        accompanimentPlayback.Speed = playbackSpeed;
        melodyPlayback.Speed = playbackSpeed;

        ParseAllNotes(accompanimentFile, melodyFile);

        accompanimentPlayback.Start();
        melodyPlayback.Start();


        Debug.Log("Playback preview started.");
    }

    public void AttachPlaybackCycleEvents()
    {
        // Attach event so we know when the playback ends
        if (accompanimentPlayback != null)
            accompanimentPlayback.Finished += OnPlaybackFinishedCycle;
        if (melodyPlayback != null)
            melodyPlayback.Finished += OnPlaybackFinishedCycle;

        Debug.Log("In the AttachPlaybackCycleEvents");

    }


    #endregion

    #region ParseAllNotes for Scoring
    /// <summary>
    /// Read the entire MIDI file (e.g. fullFile) into allNotes for scoring.
    /// </summary>
    private void ParseAllNotes(MidiFile accompanimentMidiFile, MidiFile melodyMidiFile)
    {
        if (accompanimentFile == null || melodyMidiFile == null)
        {
            Debug.LogWarning("ParseAllNotes called with null midiFile.");
            return;
        }

        // Clear old data
        allNotes.Clear();
        correctLeft = 0;
        correctRight = 0;

        var accompanimentTempoMap = accompanimentMidiFile.GetTempoMap();
        var melodyTempoMap = melodyMidiFile.GetTempoMap();
        var accompanimentNotes = accompanimentMidiFile.GetNotes(); 
        var melodyNotes = melodyMidiFile.GetNotes();
        foreach (var note in accompanimentNotes)
        {
            var startTime = note.TimeAs<MetricTimeSpan>(accompanimentTempoMap);
            double startSec = startTime.TotalMicroseconds / 1_000_000.0;


            allNotes.Add(new MidiNoteData()
            {
                StartTimeSeconds = startSec,
                NoteNumber = note.NoteNumber,
                IsLeftHand = true,
                WasPlayed = false
            });
        }
        foreach (var note in melodyNotes)
        {
            var startTime = note.TimeAs<MetricTimeSpan>(melodyTempoMap);
            double startSec = startTime.TotalMicroseconds / 1_000_000.0;


            allNotes.Add(new MidiNoteData()
            {
                StartTimeSeconds = startSec,
                NoteNumber = note.NoteNumber,
                IsLeftHand = false,
                WasPlayed = false
            });
        }
        // We can store how many left vs right total
        totalLeft = allNotes.Count(n => n.IsLeftHand);

        totalRight = allNotes.Count(n => !n.IsLeftHand);

        Debug.Log($"ParseAllNotes => totalLeft={totalLeft}, totalRight={totalRight}, totalAll={allNotes.Count}");
    }
    #endregion

    #region MIDI Reading / Filtering

    /// <summary>
    /// Read the main MIDI file from streaming assets.
    /// Then create leftHandFile, rightHandFile by removing notes.
    /// Also sets fullFile = the original unfiltered clone.
    /// </summary>
    private void CacheFilteredFiles()
    {
        string accompanimentPath = FindMidiFile($"Accompaniment{fileName}");
        string melodyPath = FindMidiFile($"Melody{fileName}");

        if (string.IsNullOrEmpty(accompanimentPath) || string.IsNullOrEmpty(melodyPath))
        {
            Debug.LogError("One or more required MIDI files are missing.");
            return;
        }



        accompanimentFile = MidiFile.Read(accompanimentPath);
        melodyFile = MidiFile.Read(melodyPath);

        ShiftDrumsToCymbal(accompanimentFile);
        ShiftDrumsToCymbal(melodyFile);
        channelSelections = instrumentChecker.CheckInstruments(accompanimentFile);
        channelSelections.AddRange(instrumentChecker.CheckInstruments(melodyFile));

        Debug.Log($"Piano channels detected: {string.Join(", ", channelSelections)}");
    }

    private string FindMidiFile(string fileName)
    {
        Debug.Log("Path name is" + fileName);
        string rootPath = Path.Combine(Application.streamingAssetsPath, "MidiFiles");
        Debug.Log(Directory.GetFiles(rootPath, $"{fileName}.midi", SearchOption.AllDirectories)
            .FirstOrDefault());
        return Directory.GetFiles(rootPath, $"{fileName}.midi", SearchOption.AllDirectories)
            .FirstOrDefault() ?? Directory.GetFiles(rootPath, $"{fileName}.mid", SearchOption.AllDirectories).FirstOrDefault();
    }

    #endregion

    #region Event Handlers

    private void OnNotesPlaybackStarted(object sender, NotesEventArgs e)
    {
        MainThreadDispatcher.Enqueue(() =>
        {
            foreach (var note in e.Notes)
            {
                string keyName = pianoFunctions.NoteNameToKeyName(note.NoteName.ToString(), note.Octave.ToString());
                int channel = note.Channel;
                int noteNumber = note.NoteNumber;

                Debug.Log($" Note played on channel {channel}, note: {noteNumber} (Octave {note.Octave})");

                bool isAccompanimentNote = accompanimentPlayback != null && sender == accompanimentPlayback;
                bool isMelodyNote = melodyPlayback != null && sender == melodyPlayback;

                if(channel == 9)
                {
                    noteNumber = 51;
                }

                if (channelSelections.Contains(channel))
                {
                    if (isAccompanimentNote)
                    {
                        activeNotes.Add(note.NoteNumber);

                        // ?? Keep only the last 5 notes
                        if (activeNotes.Count > 5)
                        {
                            activeNotes = activeNotes.Skip(activeNotes.Count - 5).ToHashSet();
                        }
                    }

                    if (isAccompanimentNote && colorAccompaniment)
                    {
                        pianoFunctions.ColorKey(keyName, true);
                    }

                    if (isMelodyNote && colorMelody)
                    {
                        pianoFunctions.ColorKey(keyName, false);
                    }

                    IdentifyCurrentChord();
                }
            }
        });
    }

    private void ShiftDrumsToCymbal(MidiFile midiFile)
    {
        foreach (var trackChunk in midiFile.GetTrackChunks())
        {
            foreach (var midiEvent in trackChunk.Events)
            {
                if (midiEvent is NoteOnEvent noteOn && noteOn.Channel == 9)
                {
                    noteOn.NoteNumber = (SevenBitNumber)51; // Remap to Crash Cymbal 1
                    Debug.Log($"Remapped drum note {noteOn.NoteNumber} on channel 9 to Cymbal (MIDI 51)");
                }
            }
        }
    }

    private void OnNotesPlaybackFinished(object sender, NotesEventArgs e)
    {
        MainThreadDispatcher.Enqueue(() =>
        {

            foreach (var note in e.Notes)
            {
                string keyName = pianoFunctions.NoteNameToKeyName(note.NoteName.ToString(), note.Octave.ToString());
                int channel = note.Channel;

                bool isAccompanimentNote = accompanimentPlayback != null && sender == accompanimentPlayback;
                bool isMelodyNote = melodyPlayback != null && sender == melodyPlayback;

                // Only reset if it belongs to a piano instrument
                if (channelSelections.Contains(channel))
                {

                    if (isAccompanimentNote)
                    {
                        activeNotes.Remove(note.NoteNumber);
                    }

                    if (isAccompanimentNote && colorAccompaniment)
                    {
                        pianoFunctions.ResetKeyColor(keyName);
                    }

                    if (isMelodyNote && colorMelody)
                    {
                        pianoFunctions.ResetKeyColor(keyName);
                    }

                    IdentifyCurrentChord();
                }
            }
        });
    }

    private void OnPlaybackFinishedCycle(object sender, EventArgs e)
    {
        MainThreadDispatcher.Enqueue(() =>
        {
            timesPlayed++;
            Debug.Log($"In the OnPlaybackFinishedCycle times PLayed {timesPlayed}");
            // Second iteration: improvisation
            if (timesPlayed == 2)
            {
                scoringManager.SetScoringMode(ScoringMode.Improvisation);
                StartPlayback(currentMode);
            }
            // Third iteration: scoring ON again
            else if (timesPlayed == 3)
            {
                scoringManager.SetScoringMode(ScoringMode.Melody);
                StartPlayback(currentMode);
            }
            else
            {
                // After the 3rd time, we're done. Optionally unhook events or do any cleanup.
                Debug.Log("Song has now played all 3 passes.");
                if (accompanimentPlayback != null)
                {
                    accompanimentPlayback.Finished -= OnPlaybackFinishedCycle;
                }
                if (melodyPlayback != null)
                {
                    melodyPlayback.Finished -= OnPlaybackFinishedCycle;
                }
                OnPlaybackFinished();
            }
        });
    }

    public void IdentifyCurrentChord()
    {
        var currentNotes = activeNotes.OrderByDescending(n => n).ToList();
        scoringManager.SetCurrentChord(currentNotes);
        httpHandler?.getChordName(currentNotes);
    }

    private void OnPlaybackFinished()
    {
        MainThreadDispatcher.Enqueue(() =>
        {
            Debug.Log("Playback reached the end. Stopping.");

            StopPlayback();

            // calculate accuracy
            float leftAccuracy = totalLeft == 0 ? 1f : (float)correctLeft / totalLeft;
            float rightAccuracy = totalRight == 0 ? 1f : (float)correctRight / totalRight;
            float overallAccuracy = (float)(correctLeft + correctRight) / (totalLeft + totalRight);

            Debug.Log($"Song End. LeftAccuracy={leftAccuracy:P2}, RightAccuracy={rightAccuracy:P2}, Overall={overallAccuracy:P2}");

            string songId = $"{author}-{fileName}";
            Transform songRow = GameObject.Find(songId)?.transform.Find("StarRow");

            // Star logic
            // If user got 90% left, star for left
            // If user got 90% right, star for right
            // If user got 90% overall, star for both
            if (leftAccuracy >= 0.90f)
                SongProgressManager.Instance.MarkSongCleared(songId, "Accompaniment");

            if (rightAccuracy >= 0.90f)
                SongProgressManager.Instance.MarkSongCleared(songId, "Melody");

            if (overallAccuracy >= 0.90f)
                SongProgressManager.Instance.MarkSongCleared(songId, "Full");

            midiFileManager.SetStarColors(songId, songRow);
        });
    }

    #endregion


    #region Utilities
    private void UpdateUI()
    {
        pianoFunctions.ResetSelectedKeysToDefaultColors(activeNotes);
        // Recalc progress  
        if (currentTime != null)
        {
            float progress = (float)(currentTime.TotalSeconds / totalTime);
            slider.value = progress;
        }

        if (timeText != null && totalDuration != null)
            timeText.text = FormatTime(currentTime) + " / " + FormatTime(totalDuration);
    }

    private void UpdatePlayPauseButtons()
    {
        if (!logoPlay || !logoPause) return;

        logoPlay.SetActive(!isPlaying);
        logoPause.SetActive(isPlaying);
    }

    private void UpdateSpeedTextUI()
    {
        if (speedText != null)
            speedText.text = $"Speed: {playbackSpeed:0.0}x";
    }

    private string FormatTime(MetricTimeSpan mts)
    {
        return $"{mts.Minutes:D2}:{mts.Seconds:D2}";
    }

    private void ColorAllKeys()
    {
        pianoFunctions.ResetAllKeysToDefaultColor();
        colorAccompaniment = true;
        colorMelody = true;

        colorAccompanimentKeysButtonText.text = "Accompaniment key coloring: ON";
        colorMelodyKeysButtonText.text = "Melody key coloring: ON";
        ReplayActiveNotes();
    }

    public void ColorAccompanimentKeys()
    {
        pianoFunctions.ResetAllKeysToDefaultColor();
        colorAccompaniment = !colorAccompaniment;
        if(colorAccompaniment)
            colorAccompanimentKeysButtonText.text = "Accompaniment key coloring: ON";
        else
            colorAccompanimentKeysButtonText.text = "Accompaniment key coloring: OFF";

        ReplayActiveNotes();
    }
    public void ColorMelodyKeys()
    {
        pianoFunctions.ResetAllKeysToDefaultColor();
        colorMelody = !colorMelody;
        if (colorMelody)
            colorMelodyKeysButtonText.text = "Melody key coloring: ON";
        else
            colorMelodyKeysButtonText.text = "Melody key coloring: OFF";
        ReplayActiveNotes();
    }

    private void ReplayActiveNotes()
    {
        foreach (var noteNumber in activeNotes)
        {
            string keyName = pianoFunctions.NoteNameToKeyName(noteNumber.ToString(), ""); // Adjust if octave is needed
            if (colorAccompaniment)
            {
                pianoFunctions.ColorKey(keyName, true);
            }
            if (colorMelody)
            {
                pianoFunctions.ColorKey(keyName, false);
            }
        }
    }

    private void DisposeDevice()
    {
        if (outputDevice != null)
        {
            outputDevice.Dispose();
            outputDevice = null;
            Debug.Log("Disposed old output device to avoid conflicts.");
        }
    }
    #endregion

    #region Public Scoring Method
    public void SetScoringMode(ScoringMode mode)
    {
        scoringManager.SetScoringMode(mode);
    }

    public void LoadNotesForScoring()
    {
        scoringManager.LoadNotes(allNotes);
    }
    public void CheckUserNote(int noteNumber)
    {
        if (!isPlaying || !isSongReady) return;

        double currentSec = 0;
        if (currentMode == PlaybackMode.FullSong)
        {
            double accomTime = accompanimentPlayback?.GetCurrentTime<MetricTimeSpan>().TotalMicroseconds / 1_000_000.0 ?? 0;
            double melodyTime = melodyPlayback?.GetCurrentTime<MetricTimeSpan>().TotalMicroseconds / 1_000_000.0 ?? 0;
            currentSec = Math.Max(accomTime, melodyTime);
        }
        else if (melodyPlayback != null)
        {
            currentSec = melodyPlayback.GetCurrentTime<MetricTimeSpan>().TotalMicroseconds / 1_000_000.0;
        }

        // Instead of doing the scoring here, just delegate to ScoringLogic
        scoringManager.CheckUserNote(noteNumber, currentSec);
    }

    #endregion
}
public class MidiNoteData
{
    public double StartTimeSeconds;  // exact time (in seconds) when note should begin
    public int NoteNumber;           // MIDI pitch (0–127)
    public bool IsLeftHand;          // true if note < 60 (for example)
    public bool WasPlayed;           // whether user has correctly played it
}

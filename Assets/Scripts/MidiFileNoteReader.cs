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
using System.Collections;
using UnityEditor;
using System.Text.RegularExpressions;

public class MidiFileNoteReader : MonoBehaviour
{
    // References to other scripts / UI
    public HttpHandler httpHandler;
    public PianoFunctions pianoFunctions;
    public PanelManagerSongList panelManagerSongList;
    public MidiFileManager midiFileManager;
    private MidiInstrumentChecker instrumentChecker = new MidiInstrumentChecker();
    public ScoringLogic scoringManager;
    public CountDownTimer countDownTimer;

    [Header("UI")]
    public Slider slider;
    public GameObject logoPause;
    public GameObject logoPlay;
    public GameObject pianoSettingsUI;
    public GameObject songChoiceUI;
    public GameObject slowDownButton;
    public GameObject speedUpButton;
    public GameObject fastForwardButton;
    public GameObject rewindButton;
    public GameObject restartButton;
    public GameObject scoreBoard;
    public GameObject mainContent;
    public GameObject bpmPreview;
    public TextMeshPro songName;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI speedText;
    public TextMeshProUGUI songTitleOnBpmPreview;
    public TextMeshProUGUI modeTitleOnBpmPreview;
    public TextMeshProUGUI modeDescriptionOnBpmPreview;
    public TextMeshProUGUI bpmValue;
    public TextMeshProUGUI dialogTitle;
    public TextMeshProUGUI dialogText;
    public TextMeshPro playAccompanimentButtonText;
    public TextMeshPro playMelodyButtonText;
    public TextMeshPro colorAccompanimentKeysButtonText;
    public TextMeshPro colorMelodyKeysButtonText;

    public GameObject songNameText;
    public GameObject accuracyAccompanimentScoreText;
    public GameObject accuracyMelodyText;
    public GameObject accuracyOverallText;
    public GameObject totalImprovisedNotesText;
    public GameObject wrongImprovisedNotesText;

    public bool countdown = true;

    public TextMeshProUGUI scoringModeTitle;
    public TextMeshPro playModeText;

    public Playback accompanimentPlayback;
    private Playback melodyPlayback;
    private Playback chordsPlayback;
    private OutputDevice outputDevice;
    public bool isPlaying = false;
    private PlaybackMode currentMode = PlaybackMode.FullSong;
    double originalBpm = 0;
    double userSetBpm = 0;
    private HashSet<int> activeNotes = new HashSet<int>();
    private HashSet<int> chordNotes = new HashSet<int>();
    private List<int> channelSelections = new List<int>();
    private bool colorAccompaniment = true;
    private bool colorMelody = true;

    private EventHandler<MidiEventPlayedEventArgs> accompanimentHandler;
    private EventHandler<MidiEventPlayedEventArgs> melodyHandler;


    // Filtered MIDI files for each mode
    private MidiFile accompanimentFile;
    private MidiFile melodyFile;
    private MidiFile chordsFile;

    // Times
    private double totalTime;
    private MetricTimeSpan totalDuration;
    private MetricTimeSpan currentTime;

    // For usage in searching the file
    public string fileName = "";
    public string author = "";

    private bool isSongReady = false;
    private int timesPlayed = 0;
    public PlayMode playMode = PlayMode.PracticeMode;

    private bool muteMelodyPlayback = false;
    private bool muteAccompanimentPlayback = false;
    
    private enum PlaybackMode
    {
        FullSong,
        Accompaniment,
        Melody
    }

    private List<MidiNoteData> allAccompanimentNotes = new List<MidiNoteData>();   // All notes for scoring
    private List<MidiNoteData> allMelodyNotes = new List<MidiNoteData>();   // All notes for scoring

    #region Setup / Lifecycle

    void Start()
    {
        httpHandler.scoringManager = scoringManager; // Link scoring logic to HTTP handler
        scoringManager.SetScoringMode(ScoringMode.Melody);
    }
    public void Setup()
    {
        // Make sure the UI references exist
        if (!slider) slider = GameObject.Find("Slider")?.GetComponent<Slider>();
        if (!timeText) timeText = GameObject.Find("Time")?.GetComponent<TextMeshProUGUI>();
        if (!speedText) speedText = GameObject.Find("Speed")?.GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        if (melodyPlayback != null && accompanimentPlayback != null && totalDuration != null)
        {
            if (currentMode == PlaybackMode.Melody)
            {
                currentTime = melodyPlayback.GetCurrentTime<MetricTimeSpan>();
            }
            else
            {
                currentTime = accompanimentPlayback.GetCurrentTime<MetricTimeSpan>();
            }

            if (currentTime != null)
            {
                float progress = (float)(currentTime.TotalSeconds / totalTime);
                slider.value = progress;

                if (timeText != null && totalDuration != null)
                    timeText.text = FormatTime(currentTime) + " / " + FormatTime(totalDuration);
            }
        }
    }

    private void OnDestroy()
    {
        StopPlayback();
        DisposeDevice();
    }
    #endregion

    #region Public Methods For UI

    public void StartFullSongPlayback()
    {
        StopPlayback();
        DisposeDevice();
        UpdateSpeedTextUI();
        currentMode = PlaybackMode.FullSong;
        scoringManager.ResetScoring();
        if (playMode != PlayMode.ImprovisationMode)
        {
            scoringManager.SetScoringMode(ScoringMode.Melody);
        } else
        {
            scoringManager.SetScoringMode(ScoringMode.Improvisation);
        }
        timesPlayed = 1;
        InitializePlaybacks();
        ColorAllKeys();
    }
    public void StartMelodyPlayback()
    {
        if (currentMode == PlaybackMode.FullSong)
        {
            StartCoroutine(StartPlayback(PlaybackMode.Accompaniment));

        }
        else if (currentMode == PlaybackMode.Accompaniment)
        {
            StartCoroutine( StartPlayback(PlaybackMode.FullSong));
        }
    }

    public void StartAccompanimentPlayback()
    {
        if(currentMode == PlaybackMode.FullSong)
        {
           StartCoroutine( StartPlayback(PlaybackMode.Melody));

        } else if(currentMode == PlaybackMode.Melody)
        {
           StartCoroutine( StartPlayback(PlaybackMode.FullSong));
        }
    }

    public void TogglePlayPause()
    {

        if (accompanimentPlayback == null || melodyPlayback == null)
        {
            StartCoroutine( StartPlayback(currentMode));
            return;
        }
        if (accompanimentPlayback?.IsRunning == true)
        {
            accompanimentPlayback.Stop();
            melodyPlayback.Stop();
            chordsPlayback.Stop();
            isPlaying = false;
        }
        else
        {
            accompanimentPlayback.Start();
            melodyPlayback.Start();
            chordsPlayback.Start();

            isPlaying = true;

        }
        Debug.Log($"Song ready {isSongReady} isPlaying {isPlaying}");
        midiFileManager.LoadLastPlayedSong();
        songChoiceUI.SetActive(!isPlaying);
        UpdatePlayPauseButtons();
    }

    public void Rewind()
    {
        if (accompanimentPlayback == null) return;

        // Pause while rewinding
        if (isPlaying) TogglePlayPause();

        // 10 seconds back
        double newTimeInSec = Math.Max(0, currentTime.TotalSeconds - 10);
        currentTime = new MetricTimeSpan(0, 0, (int)newTimeInSec);
        chordsPlayback.MoveToTime(currentTime);
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
        if (accompanimentPlayback == null) return;

        // Pause while fast-forwarding
        if (isPlaying) TogglePlayPause();

        // 10 seconds forward
        double newTimeInSec = Math.Min(totalTime, currentTime.TotalSeconds + 10);
        currentTime = new MetricTimeSpan(0, 0, (int)newTimeInSec);
        chordsPlayback.MoveToTime(currentTime);

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
        if (accompanimentPlayback == null) return;
        userSetBpm += 5;
        chordsPlayback.Speed = userSetBpm / originalBpm;
        chordsPlayback.MoveToTime(currentTime);
        switch (currentMode)
        {
            case PlaybackMode.Accompaniment:
                accompanimentPlayback.Speed = userSetBpm / originalBpm;
                accompanimentPlayback.MoveToTime(currentTime);
                break;
            case PlaybackMode.Melody:
                melodyPlayback.Speed = userSetBpm / originalBpm;
                melodyPlayback.MoveToTime(currentTime);
                    break;
            case PlaybackMode.FullSong:
                melodyPlayback.Speed = userSetBpm / originalBpm;
                melodyPlayback.MoveToTime(currentTime);
                accompanimentPlayback.Speed = userSetBpm / originalBpm;
                accompanimentPlayback.MoveToTime(currentTime);
                break;
        }

        UpdateSpeedTextUI();
    }
    
    public void TogglePanelVisibility()
    {
        if(isSongReady && !isPlaying)
        {
            panelManagerSongList.MakePanelVisible(true);
        }
    }

    public void SlowDown()
    {
        if (accompanimentPlayback == null) return;

        userSetBpm = Math.Max(10, (userSetBpm - 5));
        chordsPlayback.MoveToTime(currentTime);
        chordsPlayback.Speed = userSetBpm / originalBpm;
        switch (currentMode)
        {
            case PlaybackMode.Accompaniment:
                accompanimentPlayback.Speed = userSetBpm / originalBpm;
                accompanimentPlayback.MoveToTime(currentTime);
                break;
            case PlaybackMode.Melody:
                melodyPlayback.Speed = userSetBpm / originalBpm;
                melodyPlayback.MoveToTime(currentTime);
                break;
            case PlaybackMode.FullSong:
                melodyPlayback.Speed = userSetBpm / originalBpm;
                melodyPlayback.MoveToTime(currentTime);
                accompanimentPlayback.Speed = userSetBpm / originalBpm;
                accompanimentPlayback.MoveToTime(currentTime);
                break;
        }

        UpdateSpeedTextUI();
    }

    public void TogglePracticeMode()
    {
        if (playMode == PlayMode.PracticeMode)
        {
            playMode = PlayMode.ImprovisationMode;
        } else if (playMode == PlayMode.ImprovisationMode)
        {
            playMode = PlayMode.ScoringMode;
            userSetBpm = originalBpm;
        } else {
            playMode = PlayMode.PracticeMode;
        }
      
        //PlaybackPreview();
        OnModePreviewUI();
        mainContent.SetActive(false);
    }

    public void UpdatePracticeModeUI()
    {
        if (playMode == PlayMode.PracticeMode)
        {
            playModeText.text = "Mode: Practice";
            dialogTitle.text = "Practice mode";
            dialogText.text = "In this mode, scoring system is disabled. You can focus on practicing the song and switch to another mode when you feel ready.";
            scoringManager.StopScoring();
        }
        else if (playMode == PlayMode.ScoringMode)
        {
            dialogTitle.text = "Classic mode";
            dialogText.text = "In this mode, your playing will be scored. Press play when you feel ready and play the song from start to end. Your score will be visible at the end of the song.";
            playModeText.text = "Mode: Classic";
            scoringManager.StartScoring();
        }
        else
        {
            playModeText.text = "Mode: Improvisation";
            dialogTitle.text = "Improvisation mode";
            dialogText.text = "In this mode, scoring system is disabled. You can focus on the improvising and switch to another mode when you feel ready.";
            scoringManager.StopScoring();
        }

        restartButton.SetActive(playMode == PlayMode.ScoringMode);
        slowDownButton.SetActive(playMode == PlayMode.ImprovisationMode || playMode == PlayMode.PracticeMode);
        speedUpButton.SetActive(playMode == PlayMode.ImprovisationMode || playMode == PlayMode.PracticeMode);
        rewindButton.SetActive(playMode == PlayMode.ImprovisationMode || playMode == PlayMode.PracticeMode);
        fastForwardButton.SetActive(playMode == PlayMode.ImprovisationMode || playMode == PlayMode.PracticeMode);
    }

    private void InitializePlaybacks()
    {
        StopPlayback();
        DisposeDevice();

        outputDevice = OutputDevice.GetAll().FirstOrDefault();
        if (outputDevice == null)
        {
            Debug.LogError("No MIDI output device found.");
            return;
        }

        accompanimentPlayback = accompanimentFile.GetPlayback();
        melodyPlayback = melodyFile.GetPlayback();
        chordsPlayback = chordsFile.GetPlayback();

        accompanimentPlayback.Speed = userSetBpm / originalBpm;
        melodyPlayback.Speed = userSetBpm / originalBpm;
        chordsPlayback.Speed = userSetBpm / originalBpm;

        accompanimentPlayback.NotesPlaybackStarted += OnNotesPlaybackStarted;
        accompanimentPlayback.NotesPlaybackFinished += OnNotesPlaybackFinished;
        melodyPlayback.NotesPlaybackStarted += OnNotesPlaybackStarted;
        melodyPlayback.NotesPlaybackFinished += OnNotesPlaybackFinished;
        chordsPlayback.NotesPlaybackStarted += OnChordsPlaybackNotePlayed;
        chordsPlayback.NotesPlaybackFinished += OnChordsPlaybackNoteFinished;

        SubscribeToMutePlaybacks();

        AttachPlaybackCycleEvents();

        StartCoroutine(StartPlayback(currentMode));
    }

    private IEnumerator StartPlayback(PlaybackMode mode)
    {
        if (!scoringModeTitle) scoringModeTitle = GameObject.Find("ScoringModeTitle")?.GetComponent<TextMeshProUGUI>();

        if (timesPlayed >= 4)
        {
            timesPlayed = 1;
            scoringManager.ResetScoring();
            InitializePlaybacks();
        }

        if((playMode == PlayMode.PracticeMode || playMode == PlayMode.ImprovisationMode) && slowDownButton.activeSelf == false)
        {
            restartButton.SetActive(playMode == PlayMode.ScoringMode);
            slowDownButton.SetActive(playMode == PlayMode.ImprovisationMode || playMode == PlayMode.PracticeMode);
            speedUpButton.SetActive(playMode == PlayMode.ImprovisationMode || playMode == PlayMode.PracticeMode);
            rewindButton.SetActive(playMode == PlayMode.ImprovisationMode || playMode == PlayMode.PracticeMode);
            fastForwardButton.SetActive(playMode == PlayMode.ImprovisationMode || playMode == PlayMode.PracticeMode);
        }
        currentMode = mode;
        if (accompanimentPlayback == null || melodyPlayback == null)
        {
            Debug.LogError("Both Accompaniment and Melody files are required for FullSong mode.");
            InitializePlaybacks();
            yield break;
        }

        muteAccompanimentPlayback = (mode == PlaybackMode.Melody);
        muteMelodyPlayback = (mode == PlaybackMode.Accompaniment || scoringManager.GetScoringMode() == ScoringMode.Improvisation);

        Debug.Log($"timesPLayed {timesPlayed}");

        playAccompanimentButtonText.text = $"Playing accompaniment: {(muteAccompanimentPlayback ? "OFF" : "ON")}";
        playMelodyButtonText.text = $"Playing melody: {(muteMelodyPlayback ? "OFF" : "ON")}";

        if (!isPlaying)
        {
            Debug.Log($"timesPLayed {isPlaying}");
            melodyPlayback.MoveToTime(new MetricTimeSpan(0));
            accompanimentPlayback.MoveToTime(new MetricTimeSpan(0));
            chordsPlayback.MoveToTime(new MetricTimeSpan(0));
            if (countdown)
            {
                yield return countDownTimer.StartTimer(accompanimentFile.GetTempoMap(), userSetBpm / originalBpm);
                countdown = false;
            }

            accompanimentPlayback.Start();
            melodyPlayback.Start();
            chordsPlayback.Start();
            isPlaying = true;
            isSongReady = true;
        }
        Debug.Log($"timesPLayed {isPlaying}");
        
            
        songName.text = $"{author}: {fileName}";
        scoringModeTitle.text = $"{(scoringManager.GetScoringMode() == ScoringMode.Melody ? "Play melody and accompaniment" : "Time to improvise!")}";

        totalTime = accompanimentPlayback?.GetDuration<MetricTimeSpan>().TotalSeconds ?? melodyPlayback.GetDuration<MetricTimeSpan>().TotalSeconds;
        totalDuration = accompanimentPlayback?.GetDuration<MetricTimeSpan>() ?? melodyPlayback.GetDuration<MetricTimeSpan>();
        pianoFunctions.ResetAllKeysToDefaultColor();

        UpdatePlayPauseButtons();
        UpdateUI();

        Debug.Log($"Started playback in {mode} mode.");
    }

    private void SubscribeToMutePlaybacks()
    {
        // Unsubscribe if already assigned
        if (accompanimentHandler != null)
            accompanimentPlayback.EventPlayed -= accompanimentHandler;

        if (melodyHandler != null)
            melodyPlayback.EventPlayed -= melodyHandler;

        // Create new handlers that reflect current mute flags
        accompanimentHandler = (_, e) =>
        {
            if (!muteAccompanimentPlayback)
                outputDevice.SendEvent(e.Event);
        };

        melodyHandler = (_, e) =>
        {
            if (!muteMelodyPlayback)
                outputDevice.SendEvent(e.Event);
        };

        accompanimentPlayback.EventPlayed += accompanimentHandler;
        melodyPlayback.EventPlayed += melodyHandler;
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

        if (chordsPlayback != null)
        {
            if (chordsPlayback.IsRunning) chordsPlayback.Stop();
            chordsPlayback.Dispose();
            chordsPlayback = null;
        }

        isPlaying = false;
        isSongReady = false;
        Debug.Log($"Song ready {isSongReady} isPlaying {isPlaying}");

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

        playMode = PlayMode.PracticeMode;
        scoringManager.SetScoringMode(ScoringMode.Melody);
        UpdateSpeedTextUI();

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


        var tempoMap = accompanimentPlayback.TempoMap;
        originalBpm = tempoMap
            .GetTempoAtTime((MidiTimeSpan)0)
            .BeatsPerMinute;

        userSetBpm = originalBpm;
        accompanimentPlayback.Speed = userSetBpm / originalBpm;
        melodyPlayback.Speed = userSetBpm / originalBpm;

        ParseAllNotes(accompanimentFile, melodyFile);
        LoadNotesForScoring();

        accompanimentPlayback.Start();
        melodyPlayback.Start();

        Debug.Log("Playback preview started.");
    }

    public void AttachPlaybackCycleEvents()
    {
        // Attach event so we know when the playback ends
        if (accompanimentPlayback != null)
            accompanimentPlayback.Finished += OnPlaybackFinishedCycle;

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
        allAccompanimentNotes.Clear();
        allMelodyNotes.Clear();

        var accompanimentTempoMap = accompanimentMidiFile.GetTempoMap();
        var melodyTempoMap = melodyMidiFile.GetTempoMap();
        var accompanimentNotes = accompanimentMidiFile.GetNotes(); 
        var melodyNotes = melodyMidiFile.GetNotes();

        foreach (var note in accompanimentNotes)
        {
            var startTime = note.TimeAs<MetricTimeSpan>(accompanimentTempoMap);
            double startSec = startTime.TotalMicroseconds / 1_000_000.0;


            allAccompanimentNotes.Add(new MidiNoteData()
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


            allMelodyNotes.Add(new MidiNoteData()
            {
                StartTimeSeconds = startSec,
                NoteNumber = note.NoteNumber,
                IsLeftHand = false,
                WasPlayed = false
            });
        }
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
        string chordsPath = FindMidiFile($"Chords{fileName}");

        if (string.IsNullOrEmpty(accompanimentPath) || string.IsNullOrEmpty(melodyPath))
        {
            Debug.LogError("One or more required MIDI files are missing.");
            return;
        }

        accompanimentFile = MidiFile.Read(accompanimentPath);
        melodyFile = MidiFile.Read(melodyPath);
        chordsFile = MidiFile.Read(chordsPath);

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
                    activeNotes.Add(note.NoteNumber);
                    if (scoringManager.GetScoringMode() == ScoringMode.Melody)
                    {

                        if (isAccompanimentNote && colorAccompaniment)
                        {
                            pianoFunctions.ColorKey(keyName, true);
                        }

                        if (isMelodyNote && colorMelody)
                        {
                            pianoFunctions.ColorKey(keyName, false);
                        }
                    }
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
                    activeNotes.Remove(note.NoteNumber);

                    if (scoringManager.GetScoringMode() == ScoringMode.Melody)
                    {

                        if (isAccompanimentNote && colorAccompaniment)
                        {
                            pianoFunctions.ResetKeyColor(keyName);
                        }

                        if (isMelodyNote && colorMelody)
                        {
                            pianoFunctions.ResetKeyColor(keyName);
                        }
                    }
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
                currentTime = new MetricTimeSpan(0, 0, 0);
                accompanimentPlayback?.MoveToTime(currentTime);
                melodyPlayback?.MoveToTime(currentTime);
                chordsPlayback?.MoveToTime(currentTime);
                isPlaying = false;
                StartCoroutine(StartPlayback(currentMode));
            }
            // Third iteration: scoring ON again
            else if (timesPlayed == 3)
            {
                if (playMode != PlayMode.ImprovisationMode)
                {
                    scoringManager.SetScoringMode(ScoringMode.Melody);
                } else
                {
                    scoringManager.SetScoringMode(ScoringMode.Improvisation);
                }
                currentTime = new MetricTimeSpan(0, 0, 0);
                accompanimentPlayback?.MoveToTime(currentTime);
                melodyPlayback?.MoveToTime(currentTime);
                chordsPlayback?.MoveToTime(currentTime);
                isPlaying = false;
                StartCoroutine(StartPlayback(currentMode));
            }
            else
            {
                // After the 3rd time, we're done. Optionally unhook events or do any cleanup.
                Debug.Log("Song has now played all 3 passes.");
                if (accompanimentPlayback != null)
                {
                    accompanimentPlayback.Finished -= OnPlaybackFinishedCycle;
                }
                OnPlaybackFinished();
            }
        });
    }
    public void IdentifyCurrentChord()
    {
        httpHandler?.getChordName(chordNotes.ToList());
    }
    private void OnPlaybackFinished()
    {
        MainThreadDispatcher.Enqueue(() =>
        {
            Debug.Log("Playback reached the end. Stopping.");
            songChoiceUI.SetActive(true);
            StopPlayback();
            if (playMode == PlayMode.ImprovisationMode || playMode == PlayMode.PracticeMode)
                return;

            // calculate accuracy
            float leftAccuracy = scoringManager.GetAccompanimentScore();
            float rightAccuracy = scoringManager.GetMelodyScore();
            float overallAccuracy = (float)(leftAccuracy + rightAccuracy) / 2;

            Debug.Log($"Song End. LeftAccuracy={leftAccuracy:P2},  {scoringManager.GetMelodyScore()} RightAccuracy={rightAccuracy:P2}, Overall={overallAccuracy:P2} total notes: {scoringManager.totalImprovisedNotes} wrong notes: {scoringManager.wrongImprovisedNotes}");

            string songId = $"{author}-{fileName}";

            Transform songRow = GameObject.Find(songId)?.transform.Find("StarRow");

            SongProgress savedProgress = SongProgressManager.Instance.GetSongProgress(songId);

            if (savedProgress != null && SongProgressManager.Instance.GetOverallProgress(songId) < overallAccuracy || savedProgress == null)
            {
                SongProgressManager.Instance.SaveSongProgress(songId, leftAccuracy, rightAccuracy, scoringManager.totalImprovisedNotes, scoringManager.wrongImprovisedNotes);
            }

            Image[] images = songRow.GetComponentsInChildren<Image>();
            midiFileManager.SetStarColors(songId, images);

            scoreBoard.SetActive(true);
            mainContent.SetActive(false);

            songNameText.GetComponent<TextMeshProUGUI>().text = songId;
            accuracyAccompanimentScoreText.GetComponent<TextMeshProUGUI>().text = $"{(leftAccuracy * 100f).ToString("F1")}%";
            accuracyMelodyText.GetComponent<TextMeshProUGUI>().text = $"{(rightAccuracy * 100f).ToString("F1")}%";
            accuracyOverallText.GetComponent<TextMeshProUGUI>().text = $"{(overallAccuracy * 100f).ToString("F1")}%";
            totalImprovisedNotesText.GetComponent<TextMeshProUGUI>().text = $"{scoringManager.totalImprovisedNotes}";
            wrongImprovisedNotesText.GetComponent<TextMeshProUGUI>().text = $"{scoringManager.wrongImprovisedNotes}";

            midiFileManager.SetStarColors(songId, GameObject.Find("StarRowScoringFinal").GetComponentsInChildren<Image>());
           
        });

    }

    private void OnChordsPlaybackNotePlayed(object sender, NotesEventArgs e)
    {
        MainThreadDispatcher.Enqueue(() =>
        {
            foreach (var note in e.Notes)
            {
                chordNotes.Add(note.NoteNumber);
                Debug.Log("Chord notes areeeee" + note);
                IdentifyCurrentChord();
            }
        });
    }

    private void OnChordsPlaybackNoteFinished(object sender, NotesEventArgs e)
    {
        MainThreadDispatcher.Enqueue(() =>
        {
            foreach (var note in e.Notes)
            {
                chordNotes.Remove(note.NoteNumber);
            }
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

            if (timeText != null && totalDuration != null)
                timeText.text = FormatTime(currentTime) + " / " + FormatTime(totalDuration);
        }
    }

    public void OnModePreviewUI()
    {
        bpmPreview.SetActive(true);
        songTitleOnBpmPreview.text = $"{author}: {fileName}";
        modeTitleOnBpmPreview.text = playMode.ToString();
        string description = "";
        if (playMode == PlayMode.ImprovisationMode)
        {
            description = "Sharpen your improvisation skills: as the song repeats three times playing only the accompaniment, you have the perfect chance to practice just the improvisation.";
        } else if (playMode == PlayMode.ScoringMode)
        {
            description = "Your performance will be scored. The song repeats three times - during the first and third repeats, play the accompaniment and melody. On the second repeat, improvise using the displayed blues scale.";
        } else
        {
            description = "Time to practice! The song repeats three times - during the first and third repeats, play the accompaniment and melody. On the second repeat, improvise using the displayed blues scale.";
        }
        modeDescriptionOnBpmPreview.text = description;
        bpmValue.text = $"{(int) userSetBpm} BPM";
    }

    public void OnBpmButtonClick(int value)
    {
        userSetBpm += value;
        bpmValue.text = $"{(int) userSetBpm} BPM";

        accompanimentPlayback.Speed = userSetBpm / originalBpm;
        melodyPlayback.Speed = userSetBpm / originalBpm;
    }   

    private void UpdatePlayPauseButtons()
    {
        if (!logoPlay || !logoPause) return;

        logoPlay.SetActive(!isPlaying);
        logoPause.SetActive(isPlaying);

        pianoSettingsUI.SetActive(!isPlaying);
    }

    private void UpdateSpeedTextUI()
    {
        if (speedText != null)
            speedText.text = $"Speed: {(int) userSetBpm} BPM";
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

    public void LoadNotesForScoring()
    {
        scoringManager.LoadNotes(allAccompanimentNotes, true);
        scoringManager.LoadNotes(allMelodyNotes, false);
    }

    public void CheckUserNote(int noteNumber)
    {
        if (!isPlaying || !isSongReady) return;

        
        if (melodyPlayback != null)
        {
            double currentSec = melodyPlayback.GetCurrentTime<MetricTimeSpan>().TotalMicroseconds / 1_000_000.0;

            // Instead of doing the scoring here, just delegate to ScoringLogic
            scoringManager.CheckUserNote(noteNumber, currentSec);
        }
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

public enum PlayMode
{
    PracticeMode,
    ImprovisationMode,
    ScoringMode
}
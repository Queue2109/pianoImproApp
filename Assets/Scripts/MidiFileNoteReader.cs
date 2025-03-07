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

    public class MidiFileNoteReader : MonoBehaviour
    {
        // References to other scripts / UI
        public HttpHandler httpHandler;
        public PianoFunctions pianoFunctions;
        public PanelManagerSongList panelManagerSongList;
        public MidiFileManager midiFileManager;

        [Header("UI")]
        public Slider slider;
        public GameObject logoPause;
        public GameObject logoPlay;
        public TextMeshProUGUI songName;
        public TextMeshProUGUI timeText;
        public TextMeshProUGUI speedText;

        // Internal Fields
        private Playback playback;
        private OutputDevice outputDevice;
        private bool isPlaying = false;
        private PlaybackMode currentMode = PlaybackMode.Full;
        private float playbackSpeed = 1f;
        private HashSet<int> activeNotes = new HashSet<int>();
        private List<int> channelSelections;
        private bool colorLeftHand = true;
        private bool colorRightHand = true;

        // Filtered MIDI files for each mode
        private MidiFile fullFile;
        private MidiFile leftHandFile;
        private MidiFile rightHandFile;

        // Times
        private double totalTime;
        private MetricTimeSpan totalDuration;
        private MetricTimeSpan currentTime;

        // For usage in searching the file
        public string fileName = "";
        public string author = "";

        private bool isSongReady = false;

        private enum PlaybackMode
        {
            Full,
            LeftHand,
            RightHand
        }

        private List<MidiNoteData> allNotes = new List<MidiNoteData>();   // All notes for scoring
        private float timingWindow = 0.25f; // ±0.25s window to count note as correct

        private int totalLeft = 0;
        private int totalRight = 0;
        private int correctLeft = 0;
        private int correctRight = 0;

        #region Setup / Lifecycle

        public void Setup()
        {
            // Make sure the UI references exist
            if (!timeText) timeText = GameObject.Find("Time")?.GetComponent<TextMeshProUGUI>();
            if (!speedText) speedText = GameObject.Find("Speed")?.GetComponent<TextMeshProUGUI>();
        }

        private void Update()
        {
            if (!isSongReady) return;
            if (!isPlaying || playback == null) return;
            if (panelManagerSongList != null && panelManagerSongList.currentPanel != 0) return;
            if (!slider || !speedText || !timeText) return;

            // Now safe to update UI
            currentTime = playback.GetCurrentTime<MetricTimeSpan>();
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

        /// <summary>
        /// Master function: Start the full/both-hands playback from the beginning,
        /// ensuring no device conflicts, all UI updates, etc.
        /// Call this from your UI button to "play the song" normally.
        /// </summary>
        public void StartFullSongPlayback()
        {
            if (currentMode == PlaybackMode.Full && isSongReady == true)
            {
                return;
            }
            // 1. Stop any old playback
            StopPlayback();

            // 2. Dispose any old device
            DisposeDevice();

            // 3. Acquire a new device
            outputDevice = OutputDevice.GetAll().FirstOrDefault();
            if (outputDevice == null)
            {
                Debug.LogError("No MIDI output device found. Cannot start playback.");
                return;
            }

            // 4. Cache or read the MIDI file (and filter if needed)
            CacheFilteredFiles();

            ParseAllNotes(fullFile);

            // 5. color both hands by default
            ColorAllKeys();

            // 6. Setup a brand new playback from the fullFile
            if (fullFile == null)
            {
                Debug.LogError("No fullFile loaded. Make sure your fileName is correct.");
                return;
            }

            playback = fullFile.GetPlayback(outputDevice);
            if (playback == null)
            {
                Debug.LogError("Could not create playback for the full file.");
                return;
            }

            // 7. Subscribe event handlers
            playback.NotesPlaybackStarted += OnNotesPlaybackStarted;
            playback.NotesPlaybackFinished += OnNotesPlaybackFinished;
            playback.Finished += OnPlaybackFinished;

            // 8. Configure
            playback.Speed = playbackSpeed;
            currentTime = new MetricTimeSpan(); // start from 0
            playback.MoveToTime(currentTime);

            // 9. Update totalTime/duration for UI
            totalTime = playback.GetDuration<MetricTimeSpan>().TotalSeconds;
            totalDuration = playback.GetDuration<MetricTimeSpan>();

            // 10. Update UI if needed (like show the name, set slider = 0, etc.)
            if (songName) songName.text = $"{author} {fileName}";
            slider.value = 0f;
            UpdateSpeedTextUI();

            // 11. Start playing
            playback.Start();
            isSongReady = true;
            isPlaying = true;
            currentMode = PlaybackMode.Full;
            UpdatePlayPauseButtons();

            Debug.Log("Started full/both-hands playback successfully.");
        }

        public void TogglePlayPause()
        {
            if (playback == null)
            {
                Debug.LogError("TogglePlayPause called but no playback is present.");
                return;
            }

            if (isPlaying)
            {
                // Pause
                playback.Stop();
                isPlaying = false;
                pianoFunctions.ResetSelectedKeysToDefaultColors(activeNotes);
                Debug.Log("Playback paused.");
            }
            else
            {
                // Resume
                playback.MoveToTime(currentTime);
                playback.Start();
                isPlaying = true;
                Debug.Log("Playback resumed.");
            }
            UpdatePlayPauseButtons();
        }

        public void Rewind()
        {
            if (playback == null) return;

            // Pause while rewinding
            if (isPlaying) TogglePlayPause();

            // 10 seconds back
            double newTimeInSec = Math.Max(0, currentTime.TotalSeconds - 10);
            currentTime = new MetricTimeSpan(0, 0, (int)newTimeInSec);
            playback.MoveToTime(currentTime);

            UpdateUI();
            TogglePlayPause(); // resume
        }

        public void FastForward()
        {
            if (playback == null) return;

            // Pause while fast-forwarding
            if (isPlaying) TogglePlayPause();

            // 10 seconds forward
            double newTimeInSec = Math.Min(totalTime, currentTime.TotalSeconds + 10);
            currentTime = new MetricTimeSpan(0, 0, (int)newTimeInSec);
            playback.MoveToTime(currentTime);

            UpdateUI();
            TogglePlayPause(); // resume
        }

        public void SpeedUp()
        {
            playbackSpeed += 0.1f;
            if (playback != null)
            {
                playback.Speed = playbackSpeed;
                playback.MoveToTime(currentTime);
            }
            UpdateSpeedTextUI();
        }

        public void SlowDown()
        {
            playbackSpeed = Mathf.Max(0.1f, playbackSpeed - 0.1f);
            if (playback != null)
            {
                playback.Speed = playbackSpeed;
                playback.MoveToTime(currentTime);
            }
            UpdateSpeedTextUI();
        }

        public void StopPlayback()
        {
            if (playback != null)
            {
                if (playback.IsRunning) playback.Stop();
                playback.Dispose();
                playback = null;
            }
            isPlaying = false;
            isSongReady = false;
            // Reset any colored notes
            pianoFunctions.ResetSelectedKeysToDefaultColors(activeNotes);
            UpdatePlayPauseButtons();
            Debug.Log("Playback fully stopped.");
        }

        // (Optional) For a quick "preview" approach
        public void PlaybackPreview()
        {
            // stop anything playing
            StopPlayback();
            DisposeDevice();

            // reacquire device
            outputDevice = OutputDevice.GetAll().FirstOrDefault();
            if (outputDevice == null)
            {
                Debug.LogError("No MIDI device for preview.");
                return;
            }

            // read file
            string filePath = FindMidiFile(fileName, Path.Combine(Application.streamingAssetsPath, "MidiFiles"));
            if (string.IsNullOrEmpty(filePath))
            {
                Debug.LogError("Cannot preview. File not found.");
                return;
            }
            var midiFile = MidiFile.Read(filePath);

            // create playback
            playback = midiFile.GetPlayback(outputDevice);
            playback.Speed = 1f;
            playback.Start();

            isPlaying = true;
            UpdatePlayPauseButtons();

            Debug.Log("Playback preview started.");
        }

    #endregion

    #region ParseAllNotes for Scoring
    /// <summary>
    /// Read the entire MIDI file (e.g. fullFile) into allNotes for scoring.
    /// </summary>
    private void ParseAllNotes(MidiFile midiFile)
    {
        if (midiFile == null)
        {
            Debug.LogWarning("ParseAllNotes called with null midiFile.");
            return;
        }

        // Clear old data
        allNotes.Clear();
        correctLeft = 0;
        correctRight = 0;

        var tempoMap = midiFile.GetTempoMap();
        var notes = midiFile.GetNotes(); // from Melanchall.DryWetMidi
        foreach (var note in notes)
        {
            var startTime = note.TimeAs<MetricTimeSpan>(tempoMap);
            double startSec = startTime.TotalMicroseconds / 1_000_000.0;

            bool isLeft = (note.NoteNumber < 60); // your own logic

            allNotes.Add(new MidiNoteData()
            {
                StartTimeSeconds = startSec,
                NoteNumber = note.NoteNumber,
                IsLeftHand = isLeft,
                WasPlayed = false
            });
        }

        // We can store how many left vs right total
        totalLeft = allNotes.Count(n => n.IsLeftHand);
        totalRight = allNotes.Count(n => !n.IsLeftHand);
        Debug.Log($"ParseAllNotes => totalLeft={totalLeft}, totalRight={totalRight}, totalAll={allNotes.Count}");
    }
    #endregion

    #region Left / Right Hand

    // Example if user wants a "LeftHandOnly" button
    public void StartLeftHandPlayback()
        {

            if (currentMode == PlaybackMode.LeftHand)
            {
                return;
            }

            StopPlayback();
            DisposeDevice();

            outputDevice = OutputDevice.GetAll().FirstOrDefault();
            if (outputDevice == null)
            {
                Debug.LogError("No device found for left-hand playback.");
                return;
            }

            CacheFilteredFiles(); // ensures leftHandFile is available
            ColorLeftKeys();

            if (leftHandFile == null)
            {
                Debug.LogError("LeftHandFile is null. Possibly filtering failed?");
                return;
            }

            playback = leftHandFile.GetPlayback(outputDevice);
            playback.NotesPlaybackStarted += OnNotesPlaybackStarted;
            playback.NotesPlaybackFinished += OnNotesPlaybackFinished;
            playback.Finished += OnPlaybackFinished;

            playback.Speed = playbackSpeed;
            currentTime = new MetricTimeSpan();
            playback.MoveToTime(currentTime);

            totalTime = playback.GetDuration<MetricTimeSpan>().TotalSeconds;
            totalDuration = playback.GetDuration<MetricTimeSpan>();
            // Optionally set a flag so Update() recognizes we can show time
            isSongReady = true;

            playback.Start();
            isPlaying = true;
            currentMode = PlaybackMode.LeftHand;

            UpdatePlayPauseButtons();
            Debug.Log("Started left-hand only playback.");
        }

        // Similarly for RightHand
        public void StartRightHandPlayback()
        {
            if (currentMode == PlaybackMode.RightHand)
            {
                return;
            }

            StopPlayback();
            DisposeDevice();

            outputDevice = OutputDevice.GetAll().FirstOrDefault();
            if (outputDevice == null)
            {
                Debug.LogError("No device found for right-hand playback.");
                return;
            }

            CacheFilteredFiles();
            ColorRightKeys();

            if (rightHandFile == null)
            {
                Debug.LogError("RightHandFile is null. Possibly filtering failed?");
                return;
            }

            playback = rightHandFile.GetPlayback(outputDevice);
            playback.NotesPlaybackStarted += OnNotesPlaybackStarted;
            playback.NotesPlaybackFinished += OnNotesPlaybackFinished;
            playback.Finished += OnPlaybackFinished;

            playback.Speed = playbackSpeed;
            currentTime = new MetricTimeSpan();
            playback.MoveToTime(currentTime);

            totalTime = playback.GetDuration<MetricTimeSpan>().TotalSeconds;
            totalDuration = playback.GetDuration<MetricTimeSpan>();

            totalTime = playback.GetDuration<MetricTimeSpan>().TotalSeconds;
            totalDuration = playback.GetDuration<MetricTimeSpan>();
            // Optionally set a flag so Update() recognizes we can show time
            isSongReady = true;

            playback.Start();
            isPlaying = true;
            currentMode = PlaybackMode.RightHand;

            UpdatePlayPauseButtons();
            Debug.Log("Started right-hand only playback.");
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
            string filePath = FindMidiFile(fileName, Path.Combine(Application.streamingAssetsPath, "MidiFiles"));
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                Debug.LogError("MIDI file not found: " + filePath);
                return;
            }
            var midiFile = MidiFile.Read(filePath);

            fullFile = midiFile.Clone();
            leftHandFile = FilterNotes(midiFile, isLeftHand: true);
            rightHandFile = FilterNotes(midiFile, isLeftHand: false);

            // Optionally, update channelSelections from MidiInstrumentChecker
            MidiInstrumentChecker.CheckInstruments(fullFile);
            channelSelections = MidiInstrumentChecker.GetPianoChannels();

            Debug.Log("Filtered left, right, full files, plus updated channelSelections from instrument checker." + channelSelections);
        }

        private MidiFile FilterNotes(MidiFile source, bool isLeftHand)
        {
            var cloned = source.Clone();
            foreach (var trackChunk in cloned.GetTrackChunks())
            {
                var removeEvents = new HashSet<MidiEvent>();
                foreach (var ev in trackChunk.Events)
                {
                    if (ev is NoteOnEvent noteOn)
                    {
                        bool keep = isLeftHand
                                    ? (noteOn.NoteNumber < 60)
                                    : (noteOn.NoteNumber >= 60);

                        if (!keep)
                        {
                            removeEvents.Add(noteOn);
                            var offEv = trackChunk.Events
                                .OfType<NoteOffEvent>()
                                .FirstOrDefault(o => o.NoteNumber == noteOn.NoteNumber
                                                  && o.Channel == noteOn.Channel);
                            if (offEv != null) removeEvents.Add(offEv);
                        }
                    }
                }

                foreach (var evToRemove in removeEvents)
                {
                    trackChunk.Events.Remove(evToRemove);
                }
            }
            return cloned;
        }

        public static string FindMidiFile(string fileName, string rootPath)
        {
            var files = Directory.GetFiles(rootPath, "*.mid", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(rootPath, "*.midi", SearchOption.AllDirectories));

            foreach (var file in files)
            {
                if (Path.GetFileNameWithoutExtension(file)
                    .Equals(fileName, StringComparison.OrdinalIgnoreCase))
                {
                    Debug.Log("Found MIDI file: " + file);
                    return file;
                }
            }
            return null;
        }

        #endregion

        #region Event Handlers

        private void OnNotesPlaybackStarted(object sender, NotesEventArgs e)
        {
            // Callback from DryWetMIDI on the main thread or not, so we use dispatcher.
            MainThreadDispatcher.Enqueue(() =>
            {
                foreach (var note in e.Notes)
                {
                    string keyName = pianoFunctions.NoteNameToKeyName(note.NoteName.ToString(), note.Octave.ToString());  

                    // Only color if channel is in the selected set,
                    // AND it belongs to left or right hand as we prefer.
                    if (channelSelections.Contains(note.Channel))
                    {
                        Debug.Log("Note " + note.NoteName);
                        // left-hand if note < 60
                        // right-hand if note >= 60
                        if ((colorLeftHand && note.NoteNumber < 60) ||
                            (colorRightHand && note.NoteNumber >= 60))
                        {
                            pianoFunctions.ColorKey(keyName, note.NoteNumber < 60);
                        }

                        if (note.NoteNumber < 60)
                        {
                            activeNotes.Add(note.NoteNumber);
                            IdentifyCurrentChord();
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
                    string keyName = pianoFunctions.NoteNameToKeyName(note.NoteName.ToString(), note.Octave.ToString());

                    if (channelSelections.Contains(note.Channel))
                    {
                        if ((colorLeftHand && note.NoteNumber < 60) ||
                            (colorRightHand && note.NoteNumber >= 60))
                        {
                            pianoFunctions.ResetKeyColor(keyName);
                        }

                        if (note.NoteNumber < 60)
                        {
                            activeNotes.Remove(note.NoteNumber);
                            IdentifyCurrentChord();
                        }
                    }
                }
            });
        }

        private void IdentifyCurrentChord()
        {
            // Convert the active notes to a list
            var currentNotes = activeNotes.ToList();
            // If you want chord analysis via HttpHandler
            httpHandler?.getChordName(currentNotes);
        }

        private void OnPlaybackFinished(object sender, EventArgs e)
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
                    SongProgressManager.Instance.MarkSongCleared(songId, "Left");

                if (rightAccuracy >= 0.90f)
                    SongProgressManager.Instance.MarkSongCleared(songId, "Right");

                if (overallAccuracy >= 0.90f)
                    SongProgressManager.Instance.MarkSongCleared(songId, "Both");

                midiFileManager.SetStarColors(songId, songRow);
            });
        }

    #endregion

    #region Public Scoring Method
    /// <summary>
    /// Called by MidiScript whenever user presses a note.
    /// We check if it matches an expected note in a timeWindow.
    /// </summary>
    public void CheckUserNote(int noteNumber)
    {
        if (!isPlaying || playback == null || !isSongReady) return;

        var metric = playback.GetCurrentTime<MetricTimeSpan>();
        double currentSec = metric.TotalMicroseconds / 1_000_000.0;

        // Find a matching note in allNotes that is not yet played
        // and is within ±timingWindow, same noteNumber
        MidiNoteData bestCandidate = null;
        double bestDiff = double.MaxValue;

        foreach (var noteData in allNotes)
        {
            if (noteData.WasPlayed) continue;
            if (noteData.NoteNumber != noteNumber) continue;

            double diff = Math.Abs(noteData.StartTimeSeconds - currentSec);
            if (diff <= timingWindow && diff < bestDiff)
            {
                bestCandidate = noteData;
                bestDiff = diff;
            }
        }

        if (bestCandidate != null)
        {
            // Mark as correctly played
            bestCandidate.WasPlayed = true;
            if (bestCandidate.IsLeftHand) correctLeft++;
            else correctRight++;

            Debug.Log($"Correct note={noteNumber}, diff={bestDiff:F2}s (Left? {bestCandidate.IsLeftHand})");
        }
        else
        {
            // Could log or ignore
            Debug.Log($"User played note={noteNumber} but no match found. Time={currentSec:F2}s");
        }
    }
    #endregion

    #region Utilities
    private void UpdateUI()
        {
            pianoFunctions.ResetSelectedKeysToDefaultColors(activeNotes);
            // Recalc progress
            float progress = (float)(currentTime.TotalSeconds / totalTime);
            slider.value = progress;

            if (timeText)
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

        public void ColorLeftKeys()
        {
            colorLeftHand = true;
            colorRightHand = false;
        }
        public void ColorRightKeys()
        {
            colorLeftHand = false;
            colorRightHand = true;
        }
        public void ColorAllKeys()
        {
            colorLeftHand = true;
            colorRightHand = true;
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
}
public class MidiNoteData
{
    public double StartTimeSeconds;  // exact time (in seconds) when note should begin
    public int NoteNumber;           // MIDI pitch (0–127)
    public bool IsLeftHand;          // true if note < 60 (for example)
    public bool WasPlayed;           // whether user has correctly played it
}

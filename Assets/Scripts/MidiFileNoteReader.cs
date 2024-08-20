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
using System.Collections.Generic;
using System.Globalization;
using System.IO.Enumeration;
using System;

public class MidiFileNoteReader : MonoBehaviour
{
    //private IOutputDevice outputDevice;
    private Playback playback;
    public bool visualizeNotes = true;
    private PianoFunctions pianoFunctions;
    public Slider slider;
    private TextMeshProUGUI timeText;
    private TextMeshProUGUI songName;
    private float playbackSpeed = 1f;
    public PanelManagerSongList panelManagerSongList;

    private bool[] channelSelections;
    private bool isPlaying = false;
    private string fileName = "fly me";
    private string author = "";
    private GameObject logoPause;
    private GameObject logoPlay;
    public AudioClip clip;
    private OutputDevice outputDevice;

    void Start()
    {

        foreach (var outputDevice in OutputDevice.GetAll())
        {
            Debug.Log( "Outputdevice name " + outputDevice.Name);
        }
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

        songName = GameObject.Find("SongName").GetComponent<TextMeshProUGUI>();
        timeText = GameObject.Find("Time").GetComponent<TextMeshProUGUI>();
        logoPause = GameObject.Find("LogoPause");
        logoPlay = GameObject.Find("LogoPlay");
        PlayMidiPreview(fileName, author);
    }

    void Update()
    {
        if (isPlaying && playback != null && slider != null && timeText != null)
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


        public void PlayMidiPreview(string fileName, string author)
        {
            Debug.Log("PlayMidiPreview called with fileName: " + fileName);
            StopPlayback();
            this.fileName = fileName;
            this.author = author;
            var filePath = FindMidiFile(fileName, Path.Combine(Application.streamingAssetsPath, "MidiFiles"));
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                Debug.LogError("MIDI file not found: " + filePath);
                return;
            }
            Debug.Log("Reading MIDI file: " + filePath);
            MidiFile midiFile = MidiFile.Read(filePath);
        outputDevice = OutputDevice.GetByName("Microsoft GS Wavetable Synth");


            if (outputDevice == null)
            {
                Debug.LogError("Output device not found.");
                return;
            }
        

            playback = midiFile.GetPlayback(outputDevice);
            playback.NotesPlaybackStarted += OnNotesPlaybackStarted;
            playback.NotesPlaybackFinished += OnNotesPlaybackFinished;
            //playback.Finished += OnPlaybackFinished;
            playback.Speed = playbackSpeed;
            playback.Start();

            if (playback.IsRunning)
            {
                Debug.Log("Playback is running.");
            } else
            {
                Debug.Log("Playback is not running.");
            }

            isPlaying = true;
            panelManagerSongList.filePath = filePath;
        }

    public void PlayMidiFunction()
    {


        Debug.Log("In the playMidi function");

        songName.text = author == "Unknown" ? fileName : author + " - " + fileName;
        logoPlay.SetActive(false);
        logoPause.SetActive(true);

        if (!playback.IsRunning)
        {
            playback.Start();
            isPlaying = true;
        }
        Debug.Log("Is playing is " + isPlaying + ", playback is " + playback + ", timeText is " + timeText + " and slider is" + slider);
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
            logoPlay.SetActive(true);
            logoPause.SetActive(false);
        }
        else
        {
            playback.Start();
            logoPlay.SetActive(false);
            logoPause.SetActive(true);
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

    private string FormatTime(MetricTimeSpan timeSpan)
    {
        return string.Format("{0:D2}:{1:D2}", timeSpan.Minutes, timeSpan.Seconds);
    }

    private void OnNotesPlaybackStarted(object sender, NotesEventArgs e)
    {
        MainThreadDispatcher.Enqueue(() =>
        {
            foreach (var note in e.Notes)
            {
                Debug.Log("Channel of the note and channel in the selectedChannels: " + note.Channel + " in selected: " + MidiInstrumentChecker.selectedChannels.ToArray());
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

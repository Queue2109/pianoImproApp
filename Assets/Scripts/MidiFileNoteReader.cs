using UnityEngine;
using System.Collections;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using System.IO;

public class MidiFileNoteReader : MonoBehaviour
{
    private IOutputDevice outputDevice;
    private Playback playback;
    public bool visualizeNotes = true;

    void Start()
    {
    }

    public static string FindMidiFile(string fileName, string rootPath)
    {
        var files = Directory.GetFiles(rootPath, "*.midi", SearchOption.AllDirectories);
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

    public void PlayMidi(string fileName)
    {
        StopPlayback();

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
        playback.Finished += OnPlaybackFinished;

        Debug.Log("Starting playback");
        StartCoroutine(PlayMidiCoroutine());
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
        StopPlayback();
    }

    private void OnNotesPlaybackStarted(object sender, NotesEventArgs e)
    {
        Debug.Log("Notes playback started");
        foreach (var note in e.Notes)
        {
            Debug.Log("Note played: " + note.NoteName + note.Octave);
            // Add your note visualization code here
        }
    }

    private void OnNotesPlaybackFinished(object sender, NotesEventArgs e)
    {
        Debug.Log("Notes playback finished");
        foreach (var note in e.Notes)
        {
            Debug.Log("Note finished: " + note.NoteName + note.Octave);
            // Add your note reset code here
        }
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

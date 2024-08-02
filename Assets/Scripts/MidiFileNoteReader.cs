using UnityEngine;
using System.Collections;
using System.Linq;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.MusicTheory;
using Melanchall.DryWetMidi.Multimedia;
using System.Collections.Generic;

public class MidiPlayer : MonoBehaviour
{
    private string midiFileName = "I Hear A Rhapsody - Live At Maybeck Recital Hall  Berkeley, CA.midi"; // Your MIDI file name
    private MidiFile midiFile;
    private IOutputDevice outputDevice;
    private Playback playback;

    void Start()
    {
        string filePath = System.IO.Path.Combine(Application.streamingAssetsPath, midiFileName);
        outputDevice = OutputDevice.GetByName("Microsoft GS Wavetable Synth");

        if (outputDevice == null)
        {
            Debug.LogError("Output device not found.");
            return;
        }

        midiFile = MidiFile.Read(filePath);
        playback = midiFile.GetPlayback(outputDevice);
        playback.NotesPlaybackStarted += OnNotesPlaybackStarted;

        StartCoroutine(PlayMidi());
    }

    private IEnumerator PlayMidi()
    {
        playback.Start();
        while (playback.IsRunning)
        {
            yield return null;
        }

        Debug.Log("Playback stopped or finished.");

        outputDevice.Dispose();
        playback.Dispose();
    }

    private void OnNotesPlaybackStarted(object sender, NotesEventArgs e)
    {
        var noteNames = e.Notes.Select(n => n.NoteName).ToArray();
        Debug.Log($"Chord Played: {string.Join(", ", noteNames)}");
    }

    private void OnDestroy()
    {
        // Ensure proper disposal of resources
        if (playback != null && playback.IsRunning)
        {
            playback.Stop();
        }

        if (outputDevice != null)
        {
            outputDevice.Dispose();
        }
    }
}



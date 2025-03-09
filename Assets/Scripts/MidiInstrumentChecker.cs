using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Common;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Discovers all instrument channels in a given MIDI file
/// (filters out only the drum channel 9).
/// </summary>
public class MidiInstrumentChecker
{
    private List<int> instrumentChannels = new List<int>();

    /// <summary>
    /// Reads the MIDI file and identifies all instrument channels,
    /// excluding channel 9 (drums).
    /// </summary>
    public List<int> CheckInstruments(MidiFile midiFile)
    {
        // Clear previous data
        instrumentChannels.Clear();
        HashSet<int> detectedChannels = new HashSet<int>();

        Debug.Log(" Scanning MIDI file for channels...");

        foreach (var trackChunk in midiFile.GetTrackChunks())
        {
            foreach (var midiEvent in trackChunk.Events)
            {
                if (midiEvent is ProgramChangeEvent pce)
                {
                    int channel = pce.Channel;
                    if (channel != 9) // Ignore drum channel
                    {
                        detectedChannels.Add(channel);
                    }
                }

                // Also track channels from Note events
                if (midiEvent is NoteOnEvent noteOn)
                {
                    int channel = noteOn.Channel;
                    if (channel != 9) // Ignore drum channel
                    {
                        detectedChannels.Add(channel);
                    }
                }
            }
        }

        // Convert HashSet to List and sort for consistency
        instrumentChannels = new List<int>(detectedChannels);
        instrumentChannels.Sort();

        Debug.Log($" Detected channels (excluding drums): {string.Join(", ", instrumentChannels)}");

        return instrumentChannels;
    }

    /// <summary>
    /// Returns all non-drum channels found.
    /// </summary>
    public List<int> GetInstrumentChannels()
    {
        return new List<int>(instrumentChannels);
    }

    #region Private Helpers

    /// <summary>
    /// Finds the first ProgramChange event for each channel
    /// and returns (channel -> programNumber).
    /// </summary>
    private static Dictionary<int, int> GetProgramChanges(MidiFile midiFile)
    {
        var programChanges = new Dictionary<int, int>();

        foreach (var trackChunk in midiFile.GetTrackChunks())
        {
            foreach (var midiEvent in trackChunk.Events)
            {
                if (midiEvent is ProgramChangeEvent pce)
                {
                    int channel = pce.Channel;
                    int programNumber = pce.ProgramNumber;

                    // Save the first program change per channel
                    if (!programChanges.ContainsKey(channel))
                    {
                        programChanges[channel] = programNumber;
                    }
                }
            }
        }

        if (programChanges.Count == 0)
        {
            Debug.LogWarning(" No ProgramChange events found in the MIDI file!");
        }

        return programChanges;
    }

    #endregion
}

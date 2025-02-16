using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Common;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Discovers piano-based channels in a given MIDI file
/// (skips GM channel 10 (index 9) for drums and anything not in GM piano range 0..7).
/// </summary>
public static class MidiInstrumentChecker
{
    // Holds channels identified as piano-based
    private static readonly List<int> pianoChannels = new List<int>();

    /// <summary>
    /// Reads the MIDI file at 'filePath' and identifies channels
    /// that have a piano instrument ProgramChange (0..7 in GM).
    /// Skips channel 9 (drum channel).
    /// </summary>
    public static void CheckInstruments(MidiFile midiFile)
    {
        // Clear any old data
        pianoChannels.Clear();
        Dictionary<int, int> programChanges = GetProgramChanges(midiFile);

        foreach (var kvp in programChanges)
        {
            int channel = kvp.Key;        // 0-based channel
            int programNumber = kvp.Value; // GM instrument program

            // Skip channel 9 (drums) and non-piano instruments
            if (channel == 9)
                continue;

            // Add to list of piano channels (avoid duplicates)
            if (!pianoChannels.Contains(channel))
                pianoChannels.Add(channel);
        }
    }

    /// <summary>
    /// Returns a copy of all discovered piano-based channels.
    /// </summary>
    public static List<int> GetPianoChannels()
    {
        return new List<int>(pianoChannels);
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

                    // Save the first program change we see for that channel
                    if (!programChanges.ContainsKey(channel))
                    {
                        programChanges[channel] = programNumber;
                    }
                }
            }
        }
        return programChanges;
    }

    /// <summary>
    /// Returns true if the GM program number is in the piano range (0..7).
    /// </summary>
    private static bool IsPianoInstrument(int programNumber)
    {
        // GM standard: 0..7 are acoustic & electric pianos
        return programNumber >= 0 && programNumber <= 7;
    }

    #endregion
}

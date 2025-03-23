using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum ScoringMode
{
    Melody,
    Improvisation
}

public class ScoringLogic
{
    private ScoringMode currentScoringMode = ScoringMode.Melody;
    private List<MidiNoteData> allAccompanimentNotes = new List<MidiNoteData>();
    private List<MidiNoteData> allMelodyNotes = new List<MidiNoteData>();
    private float timingWindow = 0.25f; // ±0.25s window to count note as correct
    private int correctMelodyNotes = 0;
    private int correctAccompanimentNotes = 0;
    private int correctImprovisedNotes = 0;
    private int currentChordRoot = 60; // Default C
    private bool isCurrentChordMajor = true; // Default major

    public void SetScoringMode(ScoringMode mode)
    {
        currentScoringMode = mode;
        Debug.Log($"Scoring mode set to: {mode}");
    }

    public void LoadNotes(List<MidiNoteData> notes, bool isAccompaniment)
    {
        if(isAccompaniment)
            allAccompanimentNotes = new List<MidiNoteData>(notes);
        else
            allMelodyNotes = new List<MidiNoteData>(notes);
        Debug.Log($"Loaded {notes.Count} notes for scoring.");
    }

    public void CheckUserNote(int noteNumber, double currentSec, string currentPlaybackSource)
    {
        if (currentPlaybackSource == "Accompaniment")
        {
            MidiNoteData bestCandidate = allAccompanimentNotes
              .Where(noteData => !noteData.WasPlayed && noteData.NoteNumber == noteNumber)
              .OrderBy(noteData => Math.Abs(noteData.StartTimeSeconds - currentSec))
              .FirstOrDefault();

            if (bestCandidate != null && Math.Abs(bestCandidate.StartTimeSeconds - currentSec) <= timingWindow)
            {
                bestCandidate.WasPlayed = true;
                correctAccompanimentNotes++;
                Debug.Log($"🎵 Melody Mode, accompaniment: Correct note={noteNumber}, diff={(bestCandidate.StartTimeSeconds - currentSec):F2}s");
            }
        } else {
            if (currentScoringMode == ScoringMode.Melody)
            {
                MidiNoteData bestCandidate = allMelodyNotes
                    .Where(noteData => !noteData.WasPlayed && noteData.NoteNumber == noteNumber)
                    .OrderBy(noteData => Math.Abs(noteData.StartTimeSeconds - currentSec))
                    .FirstOrDefault();

                if (bestCandidate != null && Math.Abs(bestCandidate.StartTimeSeconds - currentSec) <= timingWindow)
                {
                    bestCandidate.WasPlayed = true;
                    correctMelodyNotes++;
                    Debug.Log($"🎵 Melody Mode: Correct note={noteNumber}, diff={(bestCandidate.StartTimeSeconds - currentSec):F2}s");
                }
            }
            else if (currentScoringMode == ScoringMode.Improvisation)
            {
                HashSet<int> dynamicBluesScale = GetDynamicBluesScale(currentChordRoot, isCurrentChordMajor);

                if (dynamicBluesScale.Contains(noteNumber))
                {
                    correctImprovisedNotes++;
                    Debug.Log($"🎷 Improvisation Mode: 🎶 Correct improvisation note={noteNumber} (Blues Scale)");
                }
                else
                {
                    Debug.Log($"❌ Improvisation Mode: Note {noteNumber} is outside the blues scale.");
                }
            }
        }
    }

    private HashSet<int> GetDynamicBluesScale(int rootNote, bool isMajorChord)
    {
        int bluesRoot = isMajorChord ? rootNote - 3 : rootNote;

        return new HashSet<int>
    {
        bluesRoot,             // 1st (Root)
        bluesRoot + 3,         // ♭3 (Minor Third)
        bluesRoot + 5,         // 4th (Perfect Fourth)
        bluesRoot + 6,         // ♭5 (Diminished Fifth)
        bluesRoot + 7,         // 5th (Perfect Fifth)
        bluesRoot + 10,        // ♭7 (Minor Seventh)
        bluesRoot + 12         // Octave
    };
    }

    public void SetCurrentChord(List<int> notes)
    {
        if (notes == null || notes.Count < 3)
        {
            Debug.LogWarning("SetCurrentChord requires at least 3 notes to determine the chord type.");
            return;
        }

        notes.Sort(); // Ensure ascending order
        currentChordRoot = notes[0];

        int interval1 = notes[1] - notes[0];
        int interval2 = notes[2] - notes[0];

        // Detect major or minor based on intervals
        if (interval1 == 4 && interval2 == 7)
        {
            isCurrentChordMajor = true;
        }
        else if (interval1 == 3 && interval2 == 7)
        {
            isCurrentChordMajor = false;
        }
        else
        {
            Debug.LogWarning("Chord type could not be confidently determined.");
            isCurrentChordMajor = true; // fallback default
        }

        Debug.Log($"🎵 New Chord Set: {currentChordRoot} {(isCurrentChordMajor ? "Major" : "Minor")}");
    }

    public int GetCorrectMelodyNotes() => correctMelodyNotes;
    public int GetCorrectImprovisedNotes() => correctImprovisedNotes;

    public int GetCorrectAccompanimentNotes() => correctAccompanimentNotes;
}

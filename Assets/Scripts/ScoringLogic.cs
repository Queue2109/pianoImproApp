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

    private float timingWindow = 0.25f;

    private int correctMelodyNotes = 0;
    private int correctAccompanimentNotes = 0;
    private int totalMelodyNotesPlayed = 0;

    private int currentChordRoot = 60;
    private bool isCurrentChordMajor = true;

    private int totalImprovisedNotes = 0;
    private int wrongImprovisedNotes = 0;
    private bool isScoringActive = false;

    private List<int> wrongMelodyNotes = new List<int>();
    private List<int> wrongAccompanimentNotes = new List<int>();
    private List<int> wrongImprovisationNotes = new List<int>();



    public void SetScoringMode(ScoringMode mode)
    {
        currentScoringMode = mode;
        Debug.Log($"Scoring mode set to: {mode}");
    }

    public ScoringMode GetScoringMode()
    {
        return currentScoringMode;
    }

    public void LoadNotes(List<MidiNoteData> notes, bool isAccompaniment)
    {
        if (isAccompaniment)
            allAccompanimentNotes = new List<MidiNoteData>(notes);
        else
            allMelodyNotes = new List<MidiNoteData>(notes);
        Debug.Log($"Loaded {notes.Count} notes for scoring.");
    }

    public void CheckUserNote(int noteNumber, double currentSec, string currentPlaybackSource)
    {
        if(!isScoringActive)
        {
            return;
        }
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
                Debug.Log($"🎵 Accompaniment Mode, accompaniment: Correct note={noteNumber}, diff={(bestCandidate.StartTimeSeconds - currentSec):F2}s");
            }
            else
            {
                wrongAccompanimentNotes.Add(noteNumber);
                Debug.Log($"❌ Accompaniment: Incorrect note={noteNumber}");
            }
        } else {
            if (currentScoringMode == ScoringMode.Melody)
            {
                MidiNoteData bestCandidate = allMelodyNotes
                    .Where(noteData => !noteData.WasPlayed && noteData.NoteNumber == noteNumber)
                    .OrderBy(noteData => Math.Abs(noteData.StartTimeSeconds - currentSec))
                    .FirstOrDefault();
                totalMelodyNotesPlayed++;

                if (bestCandidate != null && Math.Abs(bestCandidate.StartTimeSeconds - currentSec) <= timingWindow)
                {
                    bestCandidate.WasPlayed = true;
                    correctMelodyNotes++;
                    Debug.Log($"🎵 Melody Mode: Correct note={noteNumber}, diff={(bestCandidate.StartTimeSeconds - currentSec):F2}s");
                }
                else
                {
                    wrongMelodyNotes.Add(noteNumber);
                    Debug.Log($"❌  Melody Mode: Incorrect note={noteNumber}");
                }
            }
            else if (currentScoringMode == ScoringMode.Improvisation)
            {
                totalImprovisedNotes++;

                HashSet<int> dynamicBluesScale = GetDynamicBluesScale(currentChordRoot, isCurrentChordMajor);

                if (dynamicBluesScale.Contains(noteNumber))
                {
                    Debug.Log($"🎷 Improvisation Mode: 🎶 Correct note={noteNumber} (Blues Scale)");
                }
                else
                {
                    wrongImprovisedNotes++;
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

    public float GetImprovisationScore()
    {
        if (totalImprovisedNotes == 0) return 0f;
        float penaltyPerWrongNote = 100f / totalImprovisedNotes;
        Debug.Log($"Penalty per wrong note in improvisation mode: {penaltyPerWrongNote}");
        float score = 100f - (wrongImprovisedNotes * penaltyPerWrongNote);

        return Mathf.Max(0f, score);
    }

    public float GetAccompanimentScore()
    {
        if (allAccompanimentNotes.Count == 0) return 0f;
        float rawScore = (correctAccompanimentNotes - (wrongAccompanimentNotes.Count / 2f)) / allAccompanimentNotes.Count;
        return Mathf.Clamp01(rawScore);
    }

    public float GetMelodyScore()
    {
        float rawScore = (correctMelodyNotes - (wrongMelodyNotes.Count / 2)) / allMelodyNotes.Count;
        return Mathf.Clamp01(rawScore);
    }
    public void ResetScoring()
    {
        // Clears all counters, wasPlayed flags, and "wrong" lists
        correctMelodyNotes = 0;
        correctAccompanimentNotes = 0;
        totalMelodyNotesPlayed = 0;

        totalImprovisedNotes = 0;
        wrongImprovisedNotes = 0;

        wrongMelodyNotes.Clear();
        wrongAccompanimentNotes.Clear();
        wrongImprovisationNotes.Clear();

        // Also reset 'WasPlayed' flags on your note lists
        foreach (var note in allAccompanimentNotes) note.WasPlayed = false;
        foreach (var note in allMelodyNotes) note.WasPlayed = false;

        Debug.Log("Scoring has been reset.");
    }
    public void StartScoring()
    {
        ResetScoring();
        isScoringActive = true;
        Debug.Log("Scoring started.");
    }

    public void StopScoring()
    {
        isScoringActive = false;
        Debug.Log("Scoring stopped.");
    }

}

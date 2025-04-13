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

    private ChronologicalNoteList accompChronoList;
    private ChronologicalNoteList melodyChronoList;

    private float timingWindow = 0.25f;

    private int correctMelodyNotes = 0;
    private int correctAccompanimentNotes = 0;
    private int wrongMelodyNotes = 0;
    private int wrongAccompanimentNotes = 0;

    private int currentChordRoot = 60;
    private bool isCurrentChordMajor = true;

    public int totalImprovisedNotes = 0;
    public int wrongImprovisedNotes = 0;
    private bool isScoringActive = false;

    public void SetScoringMode(ScoringMode mode)
    {
        currentScoringMode = mode;
        PrepareForNextCycle();
        Debug.Log($"played Scoring mode set to: {mode}");
    }

    public ScoringMode GetScoringMode()
    {
        return currentScoringMode;
    }

    public void LoadNotes(List<MidiNoteData> notes, bool isAccompaniment)
    {
        if (isAccompaniment)
        {
            accompChronoList = new ChronologicalNoteList(notes);
        }
        else
        {
            melodyChronoList = new ChronologicalNoteList(notes);
        }
        Debug.Log($"Loaded {notes.Count} notes ({(isAccompaniment ? "Accompaniment" : "Melody")}).");
    }
    public void CheckUserNote(int noteNumber, double currentSec)
    {
        if (!isScoringActive) return;

        if(ScoringMode.Improvisation == currentScoringMode)
        {
            totalImprovisedNotes++;

            HashSet<int> dynamicBluesScale = GetDynamicBluesScale(currentChordRoot, isCurrentChordMajor);
            if (dynamicBluesScale.Contains(noteNumber))
            {
                Debug.Log($"🎷 Improvisation: Correct note={noteNumber} (Blues Scale)");
            }
            else
            {
                wrongImprovisedNotes++;
                Debug.Log($"❌ Improvisation: Note {noteNumber} is outside the blues scale.");
            }
            return;
        }

        // 1) Attempt to find a melody match
        MidiNoteData melodyCandidate = melodyChronoList?.FindBestMatchingNote(noteNumber, currentSec, timingWindow);
        double melodyDiff = melodyCandidate == null
            ? double.MaxValue
            : Math.Abs(melodyCandidate.StartTimeSeconds - currentSec);

        // 2) Attempt to find an accompaniment match
        MidiNoteData accompCandidate = accompChronoList?.FindBestMatchingNote(noteNumber, currentSec, timingWindow);
        double accompDiff = accompCandidate == null
            ? double.MaxValue
            : Math.Abs(accompCandidate.StartTimeSeconds - currentSec);

        // 3) Decide which track to credit
        if (melodyCandidate == null && accompCandidate == null)
        {
            // Neither track has a match within the timing window.
            // We'll figure out which track is *closest in time* overall:
            var melodyClosest = melodyChronoList?.FindClosestNote(noteNumber, currentSec);
            var accompClosest = accompChronoList?.FindClosestNote(noteNumber, currentSec);

            // Compute their absolute time diffs if they exist
            double melodyIgnoreDiff = (melodyClosest == null)
                ? double.MaxValue
                : Math.Abs(melodyClosest.StartTimeSeconds - currentSec);

            double accompIgnoreDiff = (accompClosest == null)
                ? double.MaxValue
                : Math.Abs(accompClosest.StartTimeSeconds - currentSec);

            if (melodyIgnoreDiff < accompIgnoreDiff)
            {
                // It's "closer" to a melody note in time, so mark it as a melody error
                wrongMelodyNotes++;
                Debug.Log("❌ Wrong for MELODY");
            }
            else
            {
                wrongAccompanimentNotes++;
                Debug.Log("❌ Wrong for ACCOMPANIMENT");
            }
        }
        else if (melodyCandidate != null && accompCandidate == null)
        {
            // Only melody matched
            melodyCandidate.WasPlayed = true;
            correctMelodyNotes++;
            Debug.Log($"🎵 Matched to MELODY note={noteNumber}, diff={melodyDiff:F2}s");
        }
        else if (melodyCandidate == null && accompCandidate != null)
        {
            // Only accompaniment matched
            accompCandidate.WasPlayed = true;
            correctAccompanimentNotes++;
            Debug.Log($"🎵 Matched to ACCOMPANIMENT note={noteNumber}, diff={accompDiff:F2}s");
        }
        else
        {
            // Both candidates are non-null, pick whichever is closer
            if (melodyDiff < accompDiff)
            {
                melodyCandidate.WasPlayed = true;
                correctMelodyNotes++;
                Debug.Log($"🎵 Matched to MELODY note={noteNumber}, diff={melodyDiff:F2}s");
            }
            else
            {
                accompCandidate.WasPlayed = true;
                correctAccompanimentNotes++;
                Debug.Log($"🎵 Matched to ACCOMPANIMENT note={noteNumber}, diff={accompDiff:F2}s");
            }
        }
    }

    private HashSet<int> GetDynamicBluesScale(int rootNote, bool isMajorChord)
    {
        // If major chord, shift root down by 3
        // so it has more of a "major-blues" feel
        int bluesRoot = isMajorChord ? rootNote - 3 : rootNote;

        // These are the semitone offsets from the root
        // (including the major 7 you added at +11)
        int[] intervals = { 0, 3, 5, 6, 7, 10, 11, 12 };

        var fullRangeSet = new HashSet<int>();

        // Loop across a range of octaves so we cover 0..127 in all directions
        // The bounds ±10 are arbitrary but wide enough for typical chord roots.
        for (int octaveShift = -10; octaveShift <= 10; octaveShift++)
        {
            foreach (int interval in intervals)
            {
                int note = bluesRoot + interval + (12 * octaveShift);

                // Only add if it's a valid MIDI note
                if (note >= 0 && note <= 127)
                {
                    fullRangeSet.Add(note);
                }
            }
        }

        Debug.Log($"Improvisation for blues {bluesRoot}");

        return fullRangeSet;
    }

    public void SetCurrentChord(List<int> notes)
    {
        if (notes == null || notes.Count < 3)
        {
            Debug.Log("SetCurrentChord requires at least 3 notes to determine the chord type.");
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
            Debug.Log("Chord type could not be confidently determined.");
            isCurrentChordMajor = true; // fallback default
        }

        Debug.Log($"🎵 New Chord Set: {currentChordRoot} {(isCurrentChordMajor ? "Major" : "Minor")}");
    }

    public float GetAccompanimentScore()
    {
        if (accompChronoList.Count == 0)
            return 0f;

        float rawScore = (correctAccompanimentNotes - (wrongAccompanimentNotes / 2f))
                         / (accompChronoList.Count * 2);
        return Mathf.Clamp01(rawScore);
    }

    public float GetMelodyScore()
    {
        if (melodyChronoList.Count == 0)
            return 0f;

        float rawScore = (correctMelodyNotes - (wrongMelodyNotes / 2f))
                         / (melodyChronoList.Count * 2);
        return Mathf.Clamp01(rawScore);
    }

    private void PrepareForNextCycle()
    {
        accompChronoList?.Reset();
        melodyChronoList?.Reset();
    }

    public void ResetScoring()
    {
        // Clears all counters, wasPlayed flags, and "wrong" lists
        correctMelodyNotes = 0;
        wrongMelodyNotes = 0;
        correctAccompanimentNotes = 0;
        wrongAccompanimentNotes = 0;
        totalImprovisedNotes = 0;
        wrongImprovisedNotes = 0;

        // Reset pointer-based lists
        accompChronoList?.Reset();
        melodyChronoList?.Reset();

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

    private class ChronologicalNoteList
    {
        private List<MidiNoteData> sortedNotes;
        private int pointerIndex = 0; // The next candidate note index

        public ChronologicalNoteList(List<MidiNoteData> notes)
        {
            // Sort by start time
            sortedNotes = notes.OrderBy(n => n.StartTimeSeconds).ToList();
        }

        /// <summary>
        /// Finds the best matching note with the given noteNumber
        /// around currentSec (± timingWindow). Advances pointer as needed.
        /// Returns null if not found.
        /// </summary>
        public MidiNoteData FindBestMatchingNote(int noteNumber, double currentSec, float timingWindow)
        {
            // Step 1: Advance pointer past notes that are already played or too old
            while (pointerIndex < sortedNotes.Count)
            {
                var note = sortedNotes[pointerIndex];
                if (note.WasPlayed || note.StartTimeSeconds < currentSec - timingWindow)
                {
                    pointerIndex++;
                }
                else
                {
                    // We found a note that might be relevant, so break out
                    break;
                }
            }

            // Step 2: From pointerIndex forward, find the candidate with the smallest time diff
            MidiNoteData bestCandidate = null;
            double bestDiff = double.MaxValue;

            for (int i = pointerIndex; i < sortedNotes.Count; i++)
            {
                var note = sortedNotes[i];
                if (note.WasPlayed)
                {
                    Debug.Log("Note was played.");
                    continue;
                }

                Debug.Log($"Curent sec {currentSec}");

                double noteTime = note.StartTimeSeconds;
                // If we've exceeded currentSec + timingWindow, no need to check further
                if (noteTime > currentSec + timingWindow)
                    break;

                if (note.NoteNumber == noteNumber)
                {
                    double diff = Math.Abs(noteTime - currentSec);
                    if (diff < bestDiff && diff <= timingWindow)
                    {
                        bestDiff = diff;
                        bestCandidate = note;
                    }
                }
            }

            return bestCandidate;
        }
        public MidiNoteData FindClosestNote(int noteNumber, double currentSec)
        {
            MidiNoteData bestCandidate = null;
            double bestDiff = double.MaxValue;

            // We won't increment pointerIndex here because it's "just checking."
            // We'll scan the entire list for the pitch, ignoring WasPlayed or timing window.
            foreach (var note in sortedNotes)
            {
                if (note.NoteNumber != noteNumber)
                    continue;

                double diff = Math.Abs(note.StartTimeSeconds - currentSec);
                if (diff < bestDiff)
                {
                    bestDiff = diff;
                    bestCandidate = note;
                }
            }
            return bestCandidate;
        }

        /// <summary>
        /// Resets pointer and WasPlayed flags so we can start scoring fresh.
        /// </summary>
        public void Reset()
        {
            pointerIndex = 0;
            foreach (var note in sortedNotes)
            {
                note.WasPlayed = false;
            }
        }

        // If you need the total note count
        public int Count => sortedNotes.Count;
        public List<MidiNoteData> GetAllNotes() => sortedNotes;
    }

}

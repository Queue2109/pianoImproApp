using System.Collections;
using System.Collections.Generic;
using Melanchall.DryWetMidi.MusicTheory;
using UnityEngine;

public class PianoFunctions : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void ColorKey(string key)
    {
        GameObject noteKey = GameObject.Find(key);
        if (noteKey == null)
        {
            return;
        }
        if (noteKey.TryGetComponent<Renderer>(out var renderer))
        {
            for (int i = 0; i < renderer.materials.Length; i++)
            {
                renderer.materials[i].SetColor("_Color", new Color(0.545f, 0.769f, 0.910f, 0.33f));
            }
        }
    }

    public void ResetKeyColor(string key)
    {

        GameObject noteKey = GameObject.Find(key);
        if (noteKey == null)
        {
            return;
        }
        if (noteKey.TryGetComponent<Renderer>(out var renderer))
        {
            for (int i = 0; i < renderer.materials.Length; i++)
            {
                if (key.Contains("Sharp"))
                {
                    renderer.materials[i].SetColor("_Color", new Color(0f, 0f, 0f, 1f));

                }
                else
                {
                    renderer.materials[i].SetColor("_Color", new Color(1f, 1f, 1f, 1f));

                }
            }
        }
    }

    public string NoteNameToKeyName(string note, string octave)
    {
        // check which note it is
        string newNote = note.Substring(0, 1);
        if (note.Contains("Sharp"))
        {
            newNote += "-Sharp";
        }
        newNote += octave;
        return newNote;
    }

    public void SetKeyCount(int keyCount)
    {
        // Validate input
        if (keyCount != 49 && keyCount != 61 && keyCount != 72 && keyCount != 88)
        {
            Debug.LogError("Invalid key count! Please provide 49, 61, 72, or 88.");
            return;
        }

        // Define starting notes for different key counts
        string startNote = keyCount switch
        {
            49 => "C2",
            61 => "C2",
            72 => "E1",
            88 => "A0",
            _ => "A0" // Default, though validation prevents reaching here
        };

        // Create the expected set of keys
        List<string> allKeys = GenerateKeys(startNote, keyCount);

        // Set keys active or inactive based on required key count
        foreach (Transform child in transform)
        {
            if (allKeys.Contains(child.name))
            {
                child.gameObject.SetActive(true);
            }
            else
            {
                child.gameObject.SetActive(false);
            }
        }

        Debug.Log($"Piano adjusted to {keyCount} keys starting from {startNote}.");
    }
    private List<string> GenerateKeys(string startNote, int keyCount)
    {
        string[] notes = { "C", "C-Sharp", "D", "D-Sharp", "E", "F", "F-Sharp", "G", "G-Sharp", "A", "A-Sharp", "B" };
        List<string> keys = new List<string>();

        // Parse the starting note
        string baseNote = startNote.Contains("-Sharp") ? startNote.Split('-')[0] + "-Sharp" : startNote.Substring(0, startNote.Length - 1);
        int startIndex = System.Array.IndexOf(notes, baseNote);
        if (startIndex == -1)
        {
            Debug.LogError($"Invalid start note: {startNote}. Make sure it follows the format (e.g., 'C2' or 'A0').");
            return keys;
        }

        int startOctave = int.Parse(startNote.Substring(startNote.Length - 1));

        // Generate the keys
        for (int i = 0; i < keyCount; i++)
        {
            string note = notes[(startIndex + i) % notes.Length];
            int octave = startOctave + (startIndex + i) / notes.Length;
            keys.Add(note + octave);
        }

        return keys;
    }


    public void MoveObject(Vector3 direction)
    {
        transform.position += direction;
    }

    public void ScaleObject(float scaleFactor)
    {
        transform.localScale *= scaleFactor;
    }

    public void ScaleWhiteKeys(float scaleFactor)
    {
        foreach (Transform child in transform)
        {
            if (!child.name.Contains("Sharp"))
            {
                child.localScale *= scaleFactor;
            }
        }
    }

    public void ScaleBlackKeys(float scaleFactor)
    {
        foreach (Transform child in transform)
        {
            if (child.name.Contains("Sharp"))
            {
                child.localScale *= scaleFactor;
            }
        }
    }

    public void MoveLeft()
    {
        MoveObject(Vector3.left * 0.1f);
    }

    public void MoveRight()
    {
        MoveObject(Vector3.right * 0.1f);
    }

    public void MoveForward()
    {
        MoveObject(Vector3.forward * 0.1f);
    }

    public void MoveBackward()
    {
        MoveObject(Vector3.back * 0.1f);
    }
}
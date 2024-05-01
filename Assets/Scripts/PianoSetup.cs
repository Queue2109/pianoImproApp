using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PianoSetup : MonoBehaviour
{
    public string lowestNote = "A1";
    public string highestNote = "C6";
    public GameObject pianoKeyboard;
    private Transform keyboardTransform;

    // Note ordering from low to high
    private List<string> noteOrder = new List<string> { "C", "C-Sharp", "D", "D-Sharp", "E", "F", "F-Sharp", "G", "G-Sharp", "A", "A-Sharp", "B" };

    void Start()
    {
        pianoKeyboard.SetActive(true);
        keyboardTransform = pianoKeyboard.GetComponent<Transform>();
        DisableLowerKeys(lowestNote);
        DisableHigherKeys(highestNote);
        AdjustCollider();
    }

    void DisableLowerKeys(string lowestNote)
    {
        string lowestNoteName = ParseNoteName(lowestNote);
        int lowestOctave = ParseOctave(lowestNote);

        foreach (Transform key in keyboardTransform)
        {
            string keyName = key.name;
            string keyNoteName = ParseNoteName(keyName);
            int keyOctave = ParseOctave(keyName);

            if (!IsLowerKeyActive(keyNoteName, keyOctave, lowestNoteName, lowestOctave))
            {
                key.gameObject.SetActive(false);
            }
        }
    }

    void DisableHigherKeys(string highestNote)
    {
        string highestNoteName = ParseNoteName(highestNote);
        int highestOctave = ParseOctave(highestNote);

        foreach (Transform key in keyboardTransform)
        {
            string keyName = key.name;
            string keyNoteName = ParseNoteName(keyName);
            int keyOctave = ParseOctave(keyName);

            if (!IsHigherKeyActive(keyNoteName, keyOctave, highestNoteName, highestOctave))
            {
                key.gameObject.SetActive(false);
            }
        }
    }

    bool IsLowerKeyActive(string keyNoteName, int keyOctave, string lowestNoteName, int lowestOctave)
    {
        if (keyOctave < lowestOctave) return false;
        if (keyOctave > lowestOctave) return true;

        int keyNoteIndex = noteOrder.IndexOf(keyNoteName);
        int lowestNoteIndex = noteOrder.IndexOf(lowestNoteName);

        return keyNoteIndex >= lowestNoteIndex;
    }

    bool IsHigherKeyActive(string keyNoteName, int keyOctave, string highestNoteName, int highestOctave)
    {
        if (keyOctave > highestOctave) return false;
        if (keyOctave < highestOctave) return true;

        int keyNoteIndex = noteOrder.IndexOf(keyNoteName);
        int highestNoteIndex = noteOrder.IndexOf(highestNoteName);

        return keyNoteIndex <= highestNoteIndex;
    }

    string ParseNoteName(string note)
    {
        return note.Substring(0, note.Length - 1);
    }

    int ParseOctave(string note)
    {
        return int.Parse(note.Substring(note.Length - 1));
    }

    void AdjustCollider()
    {
        BoxCollider collider = pianoKeyboard.GetComponent<BoxCollider>();
        if (collider == null)
        {
            Debug.LogError("BoxCollider component not found on the piano keyboard object.");
            return;
        }

        Vector3 lowPos = ((Transform)pianoKeyboard.transform.Find(lowestNote)).localPosition;
        Vector3 highPos = ((Transform)pianoKeyboard.transform.Find(highestNote)).localPosition;
        Vector3 midpoint = (lowPos + highPos) / 2;

        float sizeX = Mathf.Abs(highPos.x - lowPos.x);
        Vector3 size = new Vector3(sizeX, collider.size.y, collider.size.z);

        collider.center = midpoint;
        collider.size = size;
    }

}

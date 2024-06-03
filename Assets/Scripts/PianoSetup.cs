using System;
using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction.Samples;
using Oculus.Interaction;
using Unity.VisualScripting;
using UnityEngine;
using Oculus.Interaction.HandGrab;

public class PianoSetup : MonoBehaviour
{
    public string lowestNote = "C2";
    public string highestNote = "C5";
    private GameObject pianoKeyboard;
    private Transform keyboardTransform;
    public Material blackMaterial;
    public Material whiteMaterial;
    public HandGrabInteractable handGrabInteractable;


    // Note ordering from low to high
    private readonly List<string> noteOrder = new List<string> { "C", "C-Sharp", "D", "D-Sharp", "E", "F", "F-Sharp", "G", "G-Sharp", "A", "A-Sharp", "B" };

    void Start()
    {
        pianoKeyboard = GameObject.FindWithTag("Piano");
        if (pianoKeyboard != null)
        {
            pianoKeyboard.SetActive(false);
            keyboardTransform = pianoKeyboard.GetComponent<Transform>();
        }
        else
        {
            Debug.LogError("Piano GameObject not found!");
        }
    }

    public void Setup()
    {
        if (pianoKeyboard == null) return;

        pianoKeyboard.SetActive(true);

        Vector3 lowPos = keyboardTransform.Find(lowestNote).position;
        Vector3 highPos = keyboardTransform.Find(highestNote).position;
        Vector3 midpoint = (lowPos + highPos) / 2;

        DisableLowerKeys(lowestNote);
        DisableHigherKeys(highestNote);

        PivotTo(midpoint);
        AdjustCollider();
        AssignMaterials();
    }

    public void PivotTo(Vector3 position)
    {
        if (pianoKeyboard == null) return;

        Vector3 offset = pianoKeyboard.transform.position - position;
        foreach (Transform child in pianoKeyboard.transform)
        {
            child.position += offset;
        }
        pianoKeyboard.transform.position = position;
    }

    public void DoneSetup()
    {
        if (pianoKeyboard != null)
        {
            Grabbable grabbable = pianoKeyboard.GetComponent<Grabbable>();
            if (grabbable != null)
            {
                grabbable.enabled = false;
            }
            else
            {
                Debug.LogError("Grabbable component not found on the piano keyboard object.");
            }
        }
    }

    void DisableLowerKeys(string lowestNote)
    {
        if (keyboardTransform == null) return;
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
        if (keyboardTransform == null) return;
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
        return note[..^1];
    }

    int ParseOctave(string note)
    {
        return int.Parse(note[^1..]);
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
        Vector3 size = new(sizeX, collider.size.y, collider.size.z);

        collider.center = midpoint;
        collider.size = size;
    }

    public void AssignMaterials()
    {

        foreach (Transform child in pianoKeyboard.transform)
        {

            Renderer renderer = child.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material[] materials = new Material[renderer.materials.Length];
                for (int i = 0; i < materials.Length; i++)
                {
                    if (child.gameObject.name.Contains("Sharp"))
                    {
                        materials[i] = blackMaterial;
                    } else
                    {
                        materials[i] = whiteMaterial;
                    }
                }
                renderer.materials = materials;
            }
            else
            {
                Debug.Log("Renderer not found on " + child.name);
            }
        }

        Debug.Log("Materials assigned based on children names.");
    }
}


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
    public string lowestNote = "C1";
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
        AdjustCollider();
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
}


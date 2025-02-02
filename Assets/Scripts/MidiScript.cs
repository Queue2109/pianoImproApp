using System.Collections;
using System.Collections.Generic;
using Minis;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class MidiScript : MonoBehaviour
{
    public delegate void MidiNoteEvent(int noteNumber);
    public event MidiNoteEvent OnNoteOn;
    public event MidiNoteEvent OnNoteOff;

    private readonly List<string> noteOrder = new() { "A", "A-Sharp", "B", "C", "C-Sharp", "D", "D-Sharp", "E", "F", "F-Sharp", "G", "G-Sharp" };
    private GameObject pianoKeyboard;
    private bool _midiDeviceConnected;
    private float _lastCheckTime;
    private const float CheckInterval = 0.5f; // Check every second
    private List<int> pressedNotes = new List<int>();
    public TextMeshProUGUI textMeshProUGUI;

    void Start()
    {
        pianoKeyboard = GameObject.FindGameObjectWithTag("Piano");
        textMeshProUGUI.text = "MIDI device not detected. Make sure your cable is connected and press any key on your piano keyboard";
        StartCoroutine(CheckForMidiDeviceConnection());
    }

    IEnumerator CheckForMidiDeviceConnection()
    {
        while (!_midiDeviceConnected)
        {
            // Check for MIDI devices
            _midiDeviceConnected = ListenForDevice();

            if (_midiDeviceConnected)
            {
                textMeshProUGUI.text = "MIDI device detected.";
                Debug.Log("MIDI device connected. Starting to listen for notes.");
                EnableMidiListeners();
            }

            yield return new WaitForSeconds(CheckInterval); // Check every second
        }
    }

    public bool ListenForDevice()
    {
        // Get all available MIDI devices
        var devices = InputSystem.devices;

        // Filter and count MIDI devices only
        foreach (var device in devices)
        {
            if (device is Minis.MidiDevice)
            {
                return true; // A MIDI device is connected
            }
        }

        Debug.Log("No MIDI devices detected.");
        return false; // No MIDI device connected
    }

    void EnableMidiListeners()
    {
        foreach (var device in InputSystem.devices)
        {
            if (device is Minis.MidiDevice midiDevice)
            {
                Debug.Log($"Registering listeners for MIDI device: {midiDevice.description.product}");

                midiDevice.onWillNoteOn += (note, velocity) =>
                {
                    Debug.Log($"Note On: {note.noteNumber}, Velocity: {velocity}");
                    OnNoteOn?.Invoke(note.noteNumber);
                    AddNoteToPressedList(note.noteNumber);
                    //ColorKey(note.noteNumber, new Color(0.545f, 0.769f, 0.910f, 0.33f)); // Blue color
                };

                midiDevice.onWillNoteOff += (note) =>
                {
                    Debug.Log($"Note Off: {note.noteNumber}");
                    OnNoteOff?.Invoke(note.noteNumber);
                    RemoveNoteFromPressedList(note.noteNumber);
                    //ResetKeyColor(note.noteNumber);
                };

                Debug.Log("Listeners registered successfully.");
            }
        }
    }



    void AddNoteToPressedList(int noteNumber)
    {
        if (!pressedNotes.Contains(noteNumber))
        {
            pressedNotes.Add(noteNumber);
        }
    }

    void RemoveNoteFromPressedList(int noteNumber)
    {
        if (pressedNotes.Contains(noteNumber))
        {
            pressedNotes.Remove(noteNumber);
        }
    }

    void ColorKey(int noteNumber, Color color)
    {
        string noteName = NoteNameConverter(noteNumber);
        GameObject noteKey = GameObject.Find(noteName);
        if (noteKey == null) return;

        if (noteKey.TryGetComponent<Renderer>(out var renderer))
        {
            foreach (var material in renderer.sharedMaterials)
            {
                material.SetColor("_Color", color);
            }
        }
    }

    void ResetKeyColor(int noteNumber)
    {
        string noteName = NoteNameConverter(noteNumber);
        GameObject noteKey = GameObject.Find(noteName);
        if (noteKey == null) return;

        if (noteKey.TryGetComponent<Renderer>(out var renderer))
        {
            foreach (var material in renderer.sharedMaterials)
            {
                // Set default colors based on sharp/flat keys
                if (noteName.Contains("Sharp"))
                {
                    material.SetColor("_Color", new Color(0, 0, 0, 0.8f)); // Black
                }
                else
                {
                    material.SetColor("_Color", new Color(1, 1, 1, 0.2509f)); // White
                }
            }
        }
    }

    public string NoteNameConverter(int midiNote)
    {
        int octaveNumber = (int)Mathf.Floor((midiNote - 24) / 12)-1;
        string noteName = noteOrder[(midiNote - 21) % 12].ToString();
        noteName += octaveNumber;
        return noteName;
    }
}

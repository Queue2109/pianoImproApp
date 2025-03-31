using System.Collections;
using System.Collections.Generic;
using Minis;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MidiScript : MonoBehaviour
{
    public delegate void MidiNoteEvent(int noteNumber);
    public event MidiNoteEvent OnNoteOn;
    public event MidiNoteEvent OnNoteOff;
    public MidiFileNoteReader midiFileNoteReader;
    public GameObject midiListenerUIObject;

    private readonly List<string> noteOrder = new() { "A", "A-Sharp", "B", "C", "C-Sharp", "D", "D-Sharp", "E", "F", "F-Sharp", "G", "G-Sharp" };
    private bool _midiDeviceConnected;
    private const float CheckInterval = 0.5f; // Check every second
    private List<int> pressedNotes = new List<int>();
    public TextMeshProUGUI textMeshProUGUI;

    private System.Action<Minis.MidiNoteControl, float> noteOnHandler;
    private System.Action<Minis.MidiNoteControl> noteOffHandler;

    void Start()
    {
        StartCoroutine(CheckForMidiDeviceConnection());
    }

    IEnumerator CheckForMidiDeviceConnection()
    {
        while (true)
        {
            bool isCurrentlyConnected = IsMidiDeviceAvailable();

            if (isCurrentlyConnected && !_midiDeviceConnected)
            {
               
                _midiDeviceConnected = true;
                Debug.Log("MIDI device connected.");
                textMeshProUGUI.text = "MIDI device successfully connected";

                EnableMidiListeners();

                // Hide UI after delay
                yield return new WaitForSeconds(5f);
                MainThreadDispatcher.Enqueue(() => midiListenerUIObject.SetActive(false));
            }
            else if (!isCurrentlyConnected && _midiDeviceConnected)
            {
                _midiDeviceConnected = false;
                midiListenerUIObject.SetActive(true);
                Debug.LogWarning("MIDI device disconnected.");
                textMeshProUGUI.text = "MIDI device disconnected.";
                DisableMidiListeners();

                // Show UI again if needed
                MainThreadDispatcher.Enqueue(() => midiListenerUIObject.SetActive(true));
            }

            yield return new WaitForSeconds(CheckInterval); // Check every few seconds
        }
    }


    public bool IsMidiDeviceAvailable()
    {
        foreach (var device in InputSystem.devices)
        {
            if (device is Minis.MidiDevice)
            {
                return true;
            }
        }

        return false;
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

                // Save handlers so we can remove them later
                noteOnHandler = (note, velocity) =>
                {
                    Debug.Log($"Note On: {note.noteNumber}, Velocity: {velocity}");
                    OnNoteOn?.Invoke(note.noteNumber);
                    AddNoteToPressedList(note.noteNumber);

                    if (midiFileNoteReader != null)
                    {
                        midiFileNoteReader.CheckUserNote(note.noteNumber);
                    }
                };

                noteOffHandler = (note) =>
                {
                    Debug.Log($"Note Off: {note.noteNumber}");
                    OnNoteOff?.Invoke(note.noteNumber);
                    RemoveNoteFromPressedList(note.noteNumber);
                };

                midiDevice.onWillNoteOn += noteOnHandler;
                midiDevice.onWillNoteOff += noteOffHandler;

                Debug.Log("Listeners registered successfully.");
            }
        }
    }

    void DisableMidiListeners()
    {
        foreach (var device in InputSystem.devices)
        {
            if (device is Minis.MidiDevice midiDevice)
            {
                if (noteOnHandler != null)
                {
                    midiDevice.onWillNoteOn -= noteOnHandler;
                }

                if (noteOffHandler != null)
                {
                    midiDevice.onWillNoteOff -= noteOffHandler;
                }

                Debug.Log($"Listeners removed for MIDI device: {midiDevice.description.product}");
            }
        }

        noteOnHandler = null;
        noteOffHandler = null;
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

    public string NoteNameConverter(int midiNote)
    {
        int octaveNumber = (int)Mathf.Floor((midiNote - 24) / 12)-1;
        string noteName = noteOrder[(midiNote - 21) % 12].ToString();
        noteName += octaveNumber;
        return noteName;
    }
}

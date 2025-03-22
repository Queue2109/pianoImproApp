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

    private readonly List<string> noteOrder = new() { "A", "A-Sharp", "B", "C", "C-Sharp", "D", "D-Sharp", "E", "F", "F-Sharp", "G", "G-Sharp" };
    private bool _midiDeviceConnected;
    private const float CheckInterval = 0.5f; // Check every second
    private List<int> pressedNotes = new List<int>();
    public TextMeshProUGUI textMeshProUGUI;
    private TextMeshProUGUI midiButtonText;
    public Button button;

    private bool listenersRegistered = false;

    void Start()
    {
        midiButtonText = button.GetComponentInChildren<TextMeshProUGUI>();
        textMeshProUGUI.text = "MIDI device not detected. Make sure your cable is connected and press any key on your piano keyboard";
        midiButtonText.text = "Start listening";
        button.enabled = true;

        ListenForMIDIDevice();
    }

    public void ListenForMIDIDevice()
    {
        StartCoroutine(CheckForMidiDeviceConnection());
    }

    IEnumerator CheckForMidiDeviceConnection()
    {
        while (!listenersRegistered)
        {
            float timeElapsed = 0f;
            float timeout = 10f;

            textMeshProUGUI.text = "Listening for MIDI device. Make sure you are pressing keys on your piano keyboard.";
            button.enabled = false;
            while (!_midiDeviceConnected)
            {
                // Check for MIDI devices
                _midiDeviceConnected = ListenForDevice();

                if (_midiDeviceConnected)
                {
                    textMeshProUGUI.text = "MIDI device detected.";
                    Debug.Log("MIDI device connected. Starting to listen for notes.");
                    EnableMidiListeners();
                    yield break;
                }

                // If we're here, no device yet. Wait then accumulate elapsed time.
                yield return new WaitForSeconds(CheckInterval);
                timeElapsed += CheckInterval;

                // Stop looking after 10 seconds
                if (timeElapsed >= timeout)
                {
                    textMeshProUGUI.text = "No MIDI device detected within 10 seconds. Try again";
                    button.enabled = true;
                    Debug.LogWarning("Stopping MIDI device check after 10 seconds.");
                    yield break;
                }
            }
            yield return new WaitForSeconds(CheckInterval);
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

                    if (midiFileNoteReader != null)
                    {
                        midiFileNoteReader.CheckUserNote(note.noteNumber);
                    }
                };

                midiDevice.onWillNoteOff += (note) =>
                {
                    Debug.Log($"Note Off: {note.noteNumber}");
                    OnNoteOff?.Invoke(note.noteNumber);
                    RemoveNoteFromPressedList(note.noteNumber);
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

    public string NoteNameConverter(int midiNote)
    {
        int octaveNumber = (int)Mathf.Floor((midiNote - 24) / 12)-1;
        string noteName = noteOrder[(midiNote - 21) % 12].ToString();
        noteName += octaveNumber;
        return noteName;
    }
}

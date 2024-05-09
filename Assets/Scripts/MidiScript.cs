using System.Collections;
using System.Collections.Generic;
using Minis;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.InputSystem;

public class MidiScript : MonoBehaviour
{
    // midi note number of lowest key in your midi device
    // 21: A0
    int keyOffset = 21;
    PianoSetup pianoSetup;
    public delegate void MidiNoteEvent(int noteNumber);
    public event MidiNoteEvent OnNoteOn;
    public event MidiNoteEvent OnNoteOff;
    private List<string> noteOrder = new List<string> { "A", "A-Sharp", "B", "C", "C-Sharp", "D", "D-Sharp", "E", "F", "F-Sharp", "G", "G-Sharp" };

    // this is used for visualizing the notes
    //[SerializeField] GameObject barManager;

    void Start()
    {
        InputSystem.onDeviceChange += (device, change) =>
        {
            if (change != InputDeviceChange.Added) return;
            var midiDevice = device as Minis.MidiDevice;
            if (midiDevice == null) return;

            midiDevice.onWillNoteOn += (note, velocity) =>
            {
                Debug.Log($"Note On: {note.noteNumber}");
                OnNoteOn?.Invoke(note.noteNumber);
            };

            midiDevice.onWillNoteOff += (note) =>
            {
                Debug.Log($"Note Off: {note.noteNumber}");
                OnNoteOff?.Invoke(note.noteNumber);
            };
        };
    }

    void Listen()
    {
        InputSystem.onDeviceChange += (device, change) =>
        {
            if (change != InputDeviceChange.Added) return;
            var midiDevice = device as Minis.MidiDevice;
            if (midiDevice == null) return;

            midiDevice.onWillNoteOn += (note, velocity) => {
                Debug.Log(string.Format(
                    "Note On #{0} ({1}) vel:{2:0.00} ch:{3} dev:'{4}'",
                    note.noteNumber,
                    note.noteNumber.GetType(),
                    note.shortDisplayName,
                    velocity,
                    velocity.GetType(),
                    (note.device as Minis.MidiDevice)?.channel,
                    note.device.description.product
                ));

                //barManager.GetComponent<BarScript>().onNoteOn(note.noteNumber - keyOffset, velocity);
            };

            midiDevice.onWillNoteOff += (note) => {
                Debug.Log(string.Format(
                    "Note Off #{0} ({1}) ch:{2} dev:'{3}'",
                    note.noteNumber,
                    note.shortDisplayName,
                    (note.device as Minis.MidiDevice)?.channel,
                    note.device.description.product
                ));

                //barManager.GetComponent<BarScript>().onNoteOff(note.noteNumber - keyOffset);
            };
        };
    }

    public bool ListenForDevice()
    {
        // Get all available MIDI devices
        var devices = InputSystem.devices;

        // Filter and count MIDI devices only
        int midiDeviceCount = 0;
        foreach (var device in devices)
        {
            if (device is Minis.MidiDevice)
                midiDeviceCount++;
        }

        // Check if there are any MIDI devices connected
        if (midiDeviceCount > 0)
        {
            Debug.Log("Connected MIDI devices found: " + midiDeviceCount);
            return true; // There are one or more MIDI devices connected
        }
        else
        {
            Debug.Log("No MIDI devices detected.");
            return false; // No MIDI devices are connected
        }
    }
    public string NoteNameConverter(int midiNote)
    {
        // midi number 21 = A0, there are 
        // my child objects of keyboard have either a name A0 or A-Sharp0
        int octaveNumber = (int) Mathf.Floor((midiNote - 21) / 12);

        string noteName = this.noteOrder[(midiNote - 21) % 12].ToString();
        noteName += octaveNumber;
        return noteName;
    }

}